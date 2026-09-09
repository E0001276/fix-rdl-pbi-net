namespace FixRdlPbi.App.Models;

public class FixRdlAnalysis
{
    public Workspace SourceWorkspace { get; set; }

    public Workspace TargetWorkspace { get; set; }

    public Report SourceReport { get; set; }

    public Report TargetReport { get; set; }

    public SemanticModel SourceSemanticModel { get; set; }

    public SemanticModel TargetSemanticModel { get; set; }

    public ReportInspectionResult SourceInspection { get; set; }

    public ReportInspectionResult TargetInspection { get; set; }

    public Dictionary<string, PaginatedReport> SourcePaginatedById { get; set; } = new();

    public Dictionary<string, PaginatedReport> TargetPaginatedByName { get; set; } = new();

    public Dictionary<string, string> SourcePaginatedIdToTargetId { get; set; } = new();

    public Dictionary<string, string> TargetCurrentPaginatedIdToTargetId { get; set; } = new();

    public List<PaginatedReport> TargetPaginatedReports { get; set; } = new();

    public List<FixRdlPlanRow> Rows { get; set; } = new();

    public bool HasChanges => Rows.Any(x => string.Equals(x.Status, "Needs fix", StringComparison.OrdinalIgnoreCase));
}
