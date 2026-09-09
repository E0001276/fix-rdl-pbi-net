using System.Text.Json;

namespace FixRdlPbi.App.Models;

public class PowerBiGatewayResponse
{
    public List<PowerBiGateway> Value { get; set; } = new();
}

public class PowerBiGateway
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
}

public class PowerBiGatewayDatasourceResponse
{
    public List<PowerBiGatewayDatasource> Value { get; set; } = new();
}

public class PowerBiGatewayDatasource
{
    public string Id { get; set; } = string.Empty;

    public string GatewayId { get; set; } = string.Empty;

    public string DatasourceType { get; set; } = string.Empty;

    public string DatasourceName { get; set; } = string.Empty;

    public string ConnectionDetails { get; set; } = string.Empty;

    public PowerBiDatasourceConnectionDetails ParseConnectionDetails()
    {
        if (string.IsNullOrWhiteSpace(ConnectionDetails))
        {
            return new PowerBiDatasourceConnectionDetails();
        }

        try
        {
            return JsonSerializer.Deserialize<PowerBiDatasourceConnectionDetails>(
                ConnectionDetails,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new PowerBiDatasourceConnectionDetails();
        }
        catch (JsonException)
        {
            return new PowerBiDatasourceConnectionDetails();
        }
    }
}
