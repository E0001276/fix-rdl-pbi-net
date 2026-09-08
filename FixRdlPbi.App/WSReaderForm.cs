using FixRdlPbi.App.Business;
using FixRdlPbi.App.Clients;
using FixRdlPbi.App.Models;
using FixRdlPbi.App.Services;

namespace FixRdlPbi.App;

public partial class WSReaderForm : Form
{
    private readonly WorkspaceBusiness _workspaceBusiness;
    private readonly ReportBusiness _reportBusiness;
    private readonly PaginatedReportBusiness _paginatedReportBusiness;
    private readonly ReportDefinitionBusiness _reportDefinitionBusiness;

    public WSReaderForm()
    {
        InitializeComponent();

        var httpClient = new HttpClient();

        var tokenProvider = new FabricTokenProvider();

        var fabricApiClient = new FabricApiClient(httpClient, tokenProvider);

        _workspaceBusiness = new WorkspaceBusiness(fabricApiClient);
        _reportBusiness = new ReportBusiness(fabricApiClient);
        _paginatedReportBusiness = new PaginatedReportBusiness(fabricApiClient);
        _reportDefinitionBusiness = new ReportDefinitionBusiness(fabricApiClient);

        Load += WSReaderForm_Load;
    }

    private async void WSReaderForm_Load(object sender, EventArgs e)
    {
        try
        {
            await LoadWorkspacesAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task LoadWorkspacesAsync()
    {
        SetLoadingState(true);

        try
        {
            List<Workspace> workspaces = await _workspaceBusiness.GetWorkspacesAsync();

            cboWorkspaces.DataSource = workspaces;

            cboWorkspaces.DisplayMember = nameof(Workspace.DisplayName);

            cboWorkspaces.ValueMember = nameof(Workspace.Id);

            cboWorkspaces.SelectedIndex = -1;

            propertyGridWorkspace.SelectedObject = null;

            ClearArtifactGrids();
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private async void cboWorkspaces_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cboWorkspaces.SelectedItem is not Workspace workspace)
        {
            propertyGridWorkspace.SelectedObject = null;

            ClearArtifactGrids();

            return;
        }

        propertyGridWorkspace.SelectedObject = workspace;

        try
        {
            await LoadWorkspaceArtifactsAsync(workspace);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task LoadWorkspaceArtifactsAsync(Workspace workspace)
    {
        dgvReports.DataSource = null;
        dgvPaginatedReports.DataSource = null;

        lblStatus.Text = "Loading reports...";

        try
        {
            Task<List<Report>> reportsTask = _reportBusiness.GetReportsAsync(workspace.Id);

            Task<List<PaginatedReport>> paginatedReportsTask = _paginatedReportBusiness.GetPaginatedReportsAsync(workspace.Id);

            await Task.WhenAll(reportsTask, paginatedReportsTask);

            List<Report> reports = await reportsTask;

            List<PaginatedReport> paginatedReports = await paginatedReportsTask;

            dgvReports.DataSource = reports;

            dgvPaginatedReports.DataSource = paginatedReports;

            lblStatus.Text = $"Reports: {reports.Count} | " + $"Paginated Reports: {paginatedReports.Count}";
        }
        catch
        {
            lblStatus.Text = "Error loading workspace artifacts.";

            throw;
        }
    }


    private void SetLoadingState(bool loading)
    {
        cboWorkspaces.Enabled = !loading;

        Cursor = loading
            ? Cursors.WaitCursor
            : Cursors.Default;

        lblStatus.Text = loading
            ? "Loading workspaces..."
            : "Ready";
    }

    private void ClearArtifactGrids()
    {
        dgvReports.DataSource = null;
        dgvPaginatedReports.DataSource = null;
    }

    private void btnRefresh_Click(object sender, EventArgs e)
    {
        _ = RefreshWorkspacesAsync();
    }

    private async Task RefreshWorkspacesAsync()
    {
        try
        {
            await LoadWorkspacesAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private static void ShowError(Exception ex)
    {
        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private async void dgvReports_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
        {
            return;
        }

        if (dgvReports.Rows[e.RowIndex].DataBoundItem is not Report report)
        {
            return;
        }

        if (cboWorkspaces.SelectedItem is not Workspace workspace)
        {
            return;
        }

        try
        {
            lblStatus.Text = "Reading report definition...";

            List<RdlVisualReference> references =
                await _reportDefinitionBusiness.GetRdlVisualReferencesAsync(workspace.Id, workspace.DisplayName, report.Id, report.DisplayName);

            if (references.Count == 0)
            {
                MessageBox.Show("The selected report does not contain RDL Visuals.", "Report Definition", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }

            string message =
                string.Join(
                    Environment.NewLine + Environment.NewLine,
                    references.Select(x =>
                        $"Page: {x.PageName}" +
                        Environment.NewLine +
                        $"Workspace ID: {x.WorkspaceId}" +
                        Environment.NewLine +
                        $"Paginated Report ID: {x.ReportId}"
                    )
                );

            MessageBox.Show(message, "RDL Visual References", MessageBoxButtons.OK, MessageBoxIcon.Information);

            lblStatus.Text = $"RDL Visuals found: {references.Count}";
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Error reading report definition.";

            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}