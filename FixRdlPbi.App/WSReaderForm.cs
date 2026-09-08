using System.Text;
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
    private readonly PaginatedReportDefinitionBusiness _paginatedReportDefinitionBusiness;

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
        _paginatedReportDefinitionBusiness =
            new PaginatedReportDefinitionBusiness(fabricApiClient);

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
            txtReportDetails.Clear();
            txtPaginatedReportDetails.Clear();
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private async void cboWorkspaces_SelectedIndexChanged(object sender, EventArgs e)
    {
        txtReportDetails.Clear();
        txtPaginatedReportDetails.Clear();

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
        txtReportDetails.Clear();
        txtPaginatedReportDetails.Clear();

        lblStatus.Text = "Loading reports...";

        try
        {
            Task<List<Report>> reportsTask =
                _reportBusiness.GetReportsAsync(workspace.Id);

            Task<List<PaginatedReport>> paginatedReportsTask =
                _paginatedReportBusiness.GetPaginatedReportsAsync(workspace.Id);

            await Task.WhenAll(reportsTask, paginatedReportsTask);

            List<Report> reports = await reportsTask;
            List<PaginatedReport> paginatedReports = await paginatedReportsTask;

            dgvReports.DataSource = reports;
            dgvPaginatedReports.DataSource = paginatedReports;

            lblStatus.Text =
                $"Reports: {reports.Count} | Paginated Reports: {paginatedReports.Count}";
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

    private void ShowError(Exception ex)
    {
        lblStatus.Text = "Error.";
        txtReportDetails.Text = BuildErrorDetails(
            "Application",
            string.Empty,
            ex
        );
        txtPaginatedReportDetails.Clear();
    }

    private async void dgvReports_CellDoubleClick(
        object sender,
        DataGridViewCellEventArgs e)
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
            SetArtifactLoadingState(true, "Reading Power BI report definition...");

            ReportInspectionResult inspection =
                await _reportDefinitionBusiness.GetInspectionAsync(
                    workspace.Id,
                    workspace.DisplayName,
                    report.Id,
                    report.DisplayName
                );

            txtReportDetails.Text = BuildReportDetails(
                workspace,
                report,
                inspection
            );

            lblStatus.Text =
                $"RDL Visuals found: {inspection.RdlVisualReferences.Count}";

            MessageBox.Show(
                "The Power BI report definition was read successfully.",
                "Completed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Error reading report definition.";
            txtReportDetails.Text = BuildErrorDetails(
                "Power BI Report",
                report.DisplayName,
                ex
            );
        }
        finally
        {
            SetArtifactLoadingState(false);
        }
    }

    private async void dgvPaginatedReports_CellDoubleClick(
        object sender,
        DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
        {
            return;
        }

        if (dgvPaginatedReports.Rows[e.RowIndex].DataBoundItem
            is not PaginatedReport report)
        {
            return;
        }

        if (cboWorkspaces.SelectedItem is not Workspace workspace)
        {
            return;
        }

        try
        {
            SetArtifactLoadingState(true, "Reading paginated report definition...");

            PaginatedReportInspectionResult inspection =
                await _paginatedReportDefinitionBusiness.GetInspectionAsync(
                    workspace.Id,
                    report.Id
                );

            txtPaginatedReportDetails.Text = BuildPaginatedReportDetails(
                workspace,
                report,
                inspection
            );

            lblStatus.Text = $"Data sources found: {inspection.DataSources.Count}";

            MessageBox.Show(
                "The paginated report definition was read successfully.",
                "Completed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Error reading paginated report definition.";
            txtPaginatedReportDetails.Text = BuildErrorDetails(
                "Paginated Report",
                report.DisplayName,
                ex
            );
        }
        finally
        {
            SetArtifactLoadingState(false);
        }
    }

    private void SetArtifactLoadingState(bool loading, string statusText = "")
    {
        dgvReports.Enabled = !loading;
        dgvPaginatedReports.Enabled = !loading;
        cboWorkspaces.Enabled = !loading;
        btnRefresh.Enabled = !loading;

        Cursor = loading
            ? Cursors.WaitCursor
            : Cursors.Default;

        if (loading && !string.IsNullOrWhiteSpace(statusText))
        {
            lblStatus.Text = statusText;
        }
    }

    private static string BuildReportDetails(
        Workspace workspace,
        Report report,
        ReportInspectionResult inspection)
    {
        StringBuilder result = new();

        result.AppendLine("POWER BI REPORT");
        result.AppendLine(new string('=', 70));
        result.AppendLine($"Name             : {DisplayValue(report.DisplayName)}");
        result.AppendLine($"Id               : {DisplayValue(report.Id)}");
        result.AppendLine($"Workspace        : {DisplayValue(workspace.DisplayName)}");
        result.AppendLine($"Workspace Id     : {DisplayValue(workspace.Id)}");
        result.AppendLine($"Type             : {DisplayValue(report.Type)}");
        result.AppendLine($"Description      : {DisplayValue(report.Description)}");
        result.AppendLine();

        result.AppendLine("SEMANTIC MODEL");
        result.AppendLine(new string('-', 70));

        if (inspection.SemanticModel == null)
        {
            result.AppendLine("No semantic model reference could be resolved.");
            result.AppendLine(
                $"Reference type   : {DisplayValue(inspection.SemanticModelReferenceType)}"
            );
            result.AppendLine(
                $"Reference        : {DisplayValue(inspection.SemanticModelReference)}"
            );
        }
        else
        {
            result.AppendLine(
                $"Name             : {DisplayValue(inspection.SemanticModel.DisplayName)}"
            );
            result.AppendLine(
                $"Id               : {DisplayValue(inspection.SemanticModel.Id)}"
            );
            result.AppendLine(
                $"Type             : {DisplayValue(inspection.SemanticModel.Type)}"
            );
            result.AppendLine(
                $"Workspace Id     : {DisplayValue(inspection.SemanticModel.WorkspaceId)}"
            );
            result.AppendLine(
                $"Reference type   : {DisplayValue(inspection.SemanticModelReferenceType)}"
            );
            result.AppendLine(
                $"Reference        : {DisplayValue(inspection.SemanticModelReference)}"
            );
        }

        result.AppendLine();
        result.AppendLine("RDL VISUAL REFERENCES");
        result.AppendLine(new string('-', 70));

        if (inspection.RdlVisualReferences.Count == 0)
        {
            result.AppendLine("The report does not contain RDL Visuals.");
        }
        else
        {
            for (int index = 0;
                 index < inspection.RdlVisualReferences.Count;
                 index++)
            {
                RdlVisualReference reference = inspection.RdlVisualReferences[index];

                result.AppendLine($"[{index + 1}]");
                result.AppendLine(
                    $"Page             : {DisplayValue(reference.PageName)}"
                );
                result.AppendLine(
                    $"Workspace Id     : {DisplayValue(reference.WorkspaceId)}"
                );
                result.AppendLine(
                    $"Paginated Report : {DisplayValue(reference.ReportId)}"
                );

                if (index < inspection.RdlVisualReferences.Count - 1)
                {
                    result.AppendLine();
                }
            }
        }

        return result.ToString();
    }

    private static string BuildPaginatedReportDetails(
        Workspace workspace,
        PaginatedReport report,
        PaginatedReportInspectionResult inspection)
    {
        StringBuilder result = new();

        result.AppendLine("PAGINATED REPORT");
        result.AppendLine(new string('=', 70));
        result.AppendLine($"Name             : {DisplayValue(report.DisplayName)}");
        result.AppendLine($"Id               : {DisplayValue(report.Id)}");
        result.AppendLine($"Workspace        : {DisplayValue(workspace.DisplayName)}");
        result.AppendLine($"Workspace Id     : {DisplayValue(workspace.Id)}");
        result.AppendLine($"Type             : {DisplayValue(report.Type)}");
        result.AppendLine($"Description      : {DisplayValue(report.Description)}");
        result.AppendLine($"Folder Id        : {DisplayValue(report.FolderId)}");
        result.AppendLine();

        result.AppendLine("DATA SOURCES");
        result.AppendLine(new string('-', 70));

        if (inspection.DataSources.Count == 0)
        {
            result.AppendLine("No data sources were found in the RDL definition.");
        }
        else
        {
            for (int index = 0; index < inspection.DataSources.Count; index++)
            {
                PaginatedDataSource dataSource = inspection.DataSources[index];

                result.AppendLine($"[{index + 1}]");
                result.AppendLine(
                    $"Name             : {DisplayValue(dataSource.Name)}"
                );
                result.AppendLine(
                    $"Data provider    : {DisplayValue(dataSource.DataProvider)}"
                );
                result.AppendLine(
                    $"Connection string: {DisplayValue(dataSource.ConnectionString)}"
                );
                result.AppendLine(
                    $"Shared reference : {DisplayValue(dataSource.DataSourceReference)}"
                );
                result.AppendLine(
                    $"DataSource Id    : {DisplayValue(dataSource.DataSourceId)}"
                );

                if (index < inspection.DataSources.Count - 1)
                {
                    result.AppendLine();
                }
            }
        }

        return result.ToString();
    }

    private static string BuildErrorDetails(
        string artifactType,
        string artifactName,
        Exception ex)
    {
        StringBuilder result = new();

        result.AppendLine(artifactType.ToUpperInvariant());
        result.AppendLine(new string('=', 70));
        result.AppendLine($"Name  : {DisplayValue(artifactName)}");
        result.AppendLine("Status: Error reading definition");
        result.AppendLine();
        result.AppendLine(ex.Message);

        return result.ToString();
    }

    private static string DisplayValue(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "(not available)"
            : value;
    }
}
