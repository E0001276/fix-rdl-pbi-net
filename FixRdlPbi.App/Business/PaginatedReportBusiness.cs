using FixRdlPbi.App.Clients;
using FixRdlPbi.App.Models;

namespace FixRdlPbi.App.Business;

public class PaginatedReportBusiness
{
    private readonly FabricApiClient _fabricApiClient;

    public PaginatedReportBusiness(
        FabricApiClient fabricApiClient)
    {
        _fabricApiClient = fabricApiClient;
    }

    public async Task<List<PaginatedReport>> GetPaginatedReportsAsync(
        string workspaceId)
    {
        FabricPaginatedReportResponse response =
            await _fabricApiClient
                .GetAsync<FabricPaginatedReportResponse>(
                    $"workspaces/{workspaceId}/paginatedReports"
                );

        if (response == null)
        {
            return new List<PaginatedReport>();
        }

        return response.Value
            .OrderBy(x => x.DisplayName)
            .ToList();
    }
}