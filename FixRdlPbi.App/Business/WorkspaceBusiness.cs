using FixRdlPbi.App.Clients;
using FixRdlPbi.App.Models;

namespace FixRdlPbi.App.Business;

public class WorkspaceBusiness
{
    private readonly FabricApiClient _fabricApiClient;

    public WorkspaceBusiness(FabricApiClient fabricApiClient)
    {
        _fabricApiClient = fabricApiClient;
    }

    public async Task<List<Workspace>> GetWorkspacesAsync()
    {
        FabricWorkspaceResponse response = await _fabricApiClient.GetAsync<FabricWorkspaceResponse>("workspaces");

        if (response == null)
        {
            return new List<Workspace>();
        }

        return response.Value
            .OrderBy(x => x.DisplayName)
            .ToList();
    }
}