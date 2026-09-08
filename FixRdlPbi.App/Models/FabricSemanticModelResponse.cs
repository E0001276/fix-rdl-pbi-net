namespace FixRdlPbi.App.Models;

public class FabricSemanticModelResponse
{
    public List<SemanticModel> Value { get; set; } = new();

    public string ContinuationToken { get; set; } = string.Empty;

    public string ContinuationUri { get; set; } = string.Empty;
}
