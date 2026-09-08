using FixRdlPbi.App.Clients;
using FixRdlPbi.App.Models;

namespace FixRdlPbi.App.Business;

public class ReportBusiness
{
    private readonly FabricApiClient _fabricApiClient;

    public ReportBusiness(FabricApiClient fabricApiClient)
    {
        _fabricApiClient = fabricApiClient;
    }

    public async Task<List<Report>> GetReportsAsync(
        string workspaceId)
    {
        FabricReportResponse response =
            await _fabricApiClient.GetAsync<FabricReportResponse>(
                $"workspaces/{workspaceId}/reports"
            );

        if (response == null)
        {
            return new List<Report>();
        }

        return response.Value
            .OrderBy(x => x.DisplayName)
            .ToList();
    }
}