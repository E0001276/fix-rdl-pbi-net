namespace FixRdlPbi.App.Models;

public class PaginatedDataSource
{
    public string Name { get; set; } = string.Empty;

    public string DataProvider { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;

    public string DataSourceReference { get; set; } = string.Empty;

    public string DataSourceId { get; set; } = string.Empty;

    public string PowerBIWorkspaceName { get; set; } = string.Empty;

    public string PowerBIDatasetName { get; set; } = string.Empty;
}
