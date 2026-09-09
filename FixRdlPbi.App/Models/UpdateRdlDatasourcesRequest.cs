namespace FixRdlPbi.App.Models;

public class UpdateRdlDatasourcesRequest
{
    public List<UpdateRdlDatasourceDetail> UpdateDetails { get; set; } = new();
}

public class UpdateRdlDatasourceDetail
{
    public string DatasourceName { get; set; } = string.Empty;

    public RdlDatasourceConnectionDetails ConnectionDetails { get; set; } = new();
}

public class RdlDatasourceConnectionDetails
{
    public string Server { get; set; } = string.Empty;

    public string Database { get; set; } = string.Empty;
}
