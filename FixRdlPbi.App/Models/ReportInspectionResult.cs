namespace FixRdlPbi.App.Models;

public class ReportInspectionResult
{
    public List<RdlVisualReference> RdlVisualReferences { get; set; } = new();

    public SemanticModel SemanticModel { get; set; }

    public string SemanticModelReferenceType { get; set; } = string.Empty;

    public string SemanticModelReference { get; set; } = string.Empty;
}
