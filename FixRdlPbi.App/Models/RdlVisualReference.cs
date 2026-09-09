namespace FixRdlPbi.App.Models;

public class RdlVisualReference
{
    public string PageName { get; set; } = string.Empty;

    public string VisualPath { get; set; } = string.Empty;

    public string WorkspaceId { get; set; } = string.Empty;

    public string ReportId { get; set; } = string.Empty;

    public string ReferenceKind { get; set; } = string.Empty;

    public bool IsCanonicalItemLocation { get; set; }
}
