namespace FixRdlPbi.App.Models;

public class DatasetRefreshHistoryResponse
{
    public List<DatasetRefreshHistoryEntry> Value { get; set; } = new();
}

public class DatasetRefreshHistoryEntry
{
    public string RequestId { get; set; }

    public long Id { get; set; }

    public string RefreshType { get; set; }

    public string StartTime { get; set; }

    public string EndTime { get; set; }

    public string Status { get; set; }

    public string ServiceExceptionJson { get; set; }
}
