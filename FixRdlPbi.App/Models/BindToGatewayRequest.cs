using System.Text.Json.Serialization;

namespace FixRdlPbi.App.Models;

public class BindToGatewayRequest
{
    public string GatewayObjectId { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string> DatasourceObjectIds { get; set; }
}

public class SemanticModelGatewayMapping
{
    public PowerBiDatasource ModelDatasource { get; set; }

    public PowerBiGateway Gateway { get; set; }

    public PowerBiGatewayDatasource GatewayDatasource { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    public bool NeedsFix => string.Equals(Status, "Needs fix", StringComparison.OrdinalIgnoreCase);
}
