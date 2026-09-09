namespace FixRdlPbi.App.Models;

public class PowerBiDatasourceResponse
{
    public List<PowerBiDatasource> Value { get; set; } = new();
}

public class PowerBiDatasource
{
    public string DatasourceType { get; set; } = string.Empty;

    public string DatasourceId { get; set; } = string.Empty;

    public string GatewayId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public PowerBiDatasourceConnectionDetails ConnectionDetails { get; set; } = new();
}

public class PowerBiDatasourceConnectionDetails
{
    public string Server { get; set; } = string.Empty;

    public string Database { get; set; } = string.Empty;
}
