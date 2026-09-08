namespace FixRdlPbi.App.Models;

public class FabricOperation
{
    public string Status { get; set; } = string.Empty;

    public int PercentComplete { get; set; }

    public object Error { get; set; }
}