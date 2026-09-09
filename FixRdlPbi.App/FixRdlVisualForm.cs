using FixRdlPbi.App.Business;
using FixRdlPbi.App.Clients;
using FixRdlPbi.App.Models;
using FixRdlPbi.App.Services;

namespace FixRdlPbi.App;

public partial class FixRdlVisualForm : Form
{
    private readonly WorkspaceBusiness _workspaceBusiness;
    private readonly ReportBusiness _reportBusiness;
    private readonly FixRdlVisualBusiness _fixBusiness;

    private FixRdlAnalysis _analysis;

    public FixRdlVisualForm()
    {
        InitializeComponent();

        var tokenProvider = new FabricTokenProvider();
        var fabricApiClient = new FabricApiClient(new HttpClient(), tokenProvider);
        var powerBiApiClient = new PowerBiApiClient(new HttpClient(), tokenProvider);

        _workspaceBusiness = new WorkspaceBusiness(fabricApiClient);
        _reportBusiness = new ReportBusiness(fabricApiClient);
        _fixBusiness = new FixRdlVisualBusiness(fabricApiClient, powerBiApiClient);

        Load += FixRdlVisualForm_Load;
    }

    private async void FixRdlVisualForm_Load(object sender, EventArgs e)
    {
        await ExecuteSafeAsync(LoadWorkspacesAsync);
    }

    private async Task LoadWorkspacesAsync()
    {
        SetBusy(true, "Loading workspaces...");

        try
        {
            List<Workspace> workspaces = await _workspaceBusiness.GetWorkspacesAsync();

            cboSourceWorkspace.DataSource = workspaces.ToList();
            cboSourceWorkspace.DisplayMember = nameof(Workspace.DisplayName);
            cboSourceWorkspace.ValueMember = nameof(Workspace.Id);
            cboSourceWorkspace.SelectedIndex = -1;

            cboTargetWorkspace.DataSource = workspaces.ToList();
            cboTargetWorkspace.DisplayMember = nameof(Workspace.DisplayName);
            cboTargetWorkspace.ValueMember = nameof(Workspace.Id);
            cboTargetWorkspace.SelectedIndex = -1;

            cboSourceReport.DataSource = null;
            ClearAnalysis();
            AppendLog("Workspaces loaded.");
        }
        finally
        {
            SetBusy(false, "Ready");
        }
    }

    private async void cboSourceWorkspace_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cboSourceWorkspace.SelectedItem is not Workspace workspace)
        {
            cboSourceReport.DataSource = null;
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            SetBusy(true, "Loading source reports...");

            try
            {
                List<Report> reports = await _reportBusiness.GetReportsAsync(workspace.Id);
                cboSourceReport.DataSource = reports;
                cboSourceReport.DisplayMember = nameof(Report.DisplayName);
                cboSourceReport.ValueMember = nameof(Report.Id);
                cboSourceReport.SelectedIndex = -1;
                ClearAnalysis();
            }
            finally
            {
                SetBusy(false, "Ready");
            }
        });
    }

    private void cboTargetWorkspace_SelectedIndexChanged(object sender, EventArgs e)
    {
        ClearAnalysis();
    }

    private void cboSourceReport_SelectedIndexChanged(object sender, EventArgs e)
    {
        ClearAnalysis();
    }

    private async void btnRefresh_Click(object sender, EventArgs e)
    {
        await ExecuteSafeAsync(LoadWorkspacesAsync);
    }

    private async void btnAnalyze_Click(object sender, EventArgs e)
    {
        await ExecuteSafeAsync(AnalyzeAsync);
    }

    private async Task AnalyzeAsync()
    {
        if (cboSourceWorkspace.SelectedItem is not Workspace sourceWorkspace)
        {
            throw new InvalidOperationException("Select the source workspace.");
        }

        if (cboTargetWorkspace.SelectedItem is not Workspace targetWorkspace)
        {
            throw new InvalidOperationException("Select the target workspace.");
        }

        if (string.Equals(sourceWorkspace.Id, targetWorkspace.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Source and target workspaces must be different.");
        }

        if (cboSourceReport.SelectedItem is not Report sourceReport)
        {
            throw new InvalidOperationException("Select the source Power BI report.");
        }

        SetBusy(true, "Analyzing source and target links...");
        ClearAnalysis();

        try
        {
            _analysis = await _fixBusiness.AnalyzeAsync(
                sourceWorkspace,
                targetWorkspace,
                sourceReport
            );

            dgvPlan.DataSource = _analysis.Rows;
            lblTargetReportValue.Text = $"{_analysis.TargetReport.DisplayName} ({_analysis.TargetReport.Id})";
            lblTargetModelValue.Text = $"{_analysis.TargetSemanticModel.DisplayName} ({_analysis.TargetSemanticModel.Id})";
            btnApply.Enabled = _analysis.HasChanges &&
                !_analysis.Rows.Any(x => string.Equals(x.Status, "Error", StringComparison.OrdinalIgnoreCase));

            int changes = _analysis.Rows.Count(x => string.Equals(x.Status, "Needs fix", StringComparison.OrdinalIgnoreCase));
            int errors = _analysis.Rows.Count(x => string.Equals(x.Status, "Error", StringComparison.OrdinalIgnoreCase));

            lblStatus.Text = $"Analysis completed. Changes: {changes}. Errors: {errors}.";
            AppendLog($"Analysis completed for '{sourceReport.DisplayName}'. Changes: {changes}. Errors: {errors}.");
        }
        finally
        {
            SetBusy(false, lblStatus.Text);
        }
    }

    private async void btnApply_Click(object sender, EventArgs e)
    {
        await ExecuteSafeAsync(ApplyAsync);
    }

    private async Task ApplyAsync()
    {
        if (_analysis == null)
        {
            throw new InvalidOperationException("Run Analyze before applying fixes.");
        }

        if (_analysis.Rows.Any(x => string.Equals(x.Status, "Error", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("The analysis contains errors. Resolve them before applying fixes.");
        }

        if (!_analysis.HasChanges)
        {
            MessageBox.Show(
                "No changes are required.",
                "Fix RDL Visual",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            return;
        }

        DialogResult answer = MessageBox.Show(
            $"This will update item definitions in target workspace '{_analysis.TargetWorkspace.DisplayName}'.\r\n\r\n" +
            "The operation will:\r\n" +
            "- Rebind the Power BI report to the target semantic model.\r\n" +
            "- Replace RDL Visual references with target paginated report IDs.\r\n" +
            "- Rebind the target paginated reports to the target semantic model.\r\n\r\n" +
            "A local backup of the current target definitions will be created first.\r\n\r\nContinue?",
            "Confirm remediation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2
        );

        if (answer != DialogResult.Yes)
        {
            return;
        }

        SetBusy(true, "Applying fixes to target workspace...");

        try
        {
            string result = await _fixBusiness.ApplyAsync(_analysis);
            AppendLog(result);

            MessageBox.Show(
                result,
                "Fix RDL Visual",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            await AnalyzeAsync();
        }
        finally
        {
            SetBusy(false, "Ready");
        }
    }

    private void ClearAnalysis()
    {
        _analysis = null;
        dgvPlan.DataSource = null;
        lblTargetReportValue.Text = "-";
        lblTargetModelValue.Text = "-";
        btnApply.Enabled = false;
    }

    private void SetBusy(bool busy, string status)
    {
        cboSourceWorkspace.Enabled = !busy;
        cboTargetWorkspace.Enabled = !busy;
        cboSourceReport.Enabled = !busy;
        btnRefresh.Enabled = !busy;
        btnAnalyze.Enabled = !busy;
        btnApply.Enabled = !busy && _analysis != null && _analysis.HasChanges &&
            !_analysis.Rows.Any(x => string.Equals(x.Status, "Error", StringComparison.OrdinalIgnoreCase));

        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        lblStatus.Text = status;
    }

    private async Task ExecuteSafeAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Error.";
            AppendLog(ex.ToString());

            MessageBox.Show(
                ex.Message,
                "Fix RDL Visual - Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void AppendLog(string text)
    {
        if (txtLog.TextLength > 0)
        {
            txtLog.AppendText(Environment.NewLine);
        }

        txtLog.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}");
    }
}
