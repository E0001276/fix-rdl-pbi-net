using FixRdlPbi.App.Clients;
using FixRdlPbi.App.Models;

namespace FixRdlPbi.App.Business;

public class SemanticModelBusiness
{
    private readonly FabricApiClient _fabricApiClient;

    public SemanticModelBusiness(FabricApiClient fabricApiClient)
    {
        _fabricApiClient = fabricApiClient;
    }

    public async Task<List<SemanticModel>> GetSemanticModelsAsync(string workspaceId)
    {
        List<SemanticModel> result = new();
        string endpoint = $"workspaces/{workspaceId}/semanticModels";

        while (!string.IsNullOrWhiteSpace(endpoint))
        {
            FabricSemanticModelResponse response =
                await _fabricApiClient.GetAsync<FabricSemanticModelResponse>(endpoint);

            if (response == null)
            {
                break;
            }

            result.AddRange(response.Value);
            endpoint = response.ContinuationUri;
        }

        return result
            .OrderBy(x => x.DisplayName)
            .ToList();
    }
}
