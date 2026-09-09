using System.Text.Json.Serialization;

namespace FixRdlPbi.App.Models;

public class ReportDefinition
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Format { get; set; }

    public List<ReportDefinitionPart> Parts { get; set; } = new();
}