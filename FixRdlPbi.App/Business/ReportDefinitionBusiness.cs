using System.Text;
using System.Text.Json;
using FixRdlPbi.App.Clients;
using FixRdlPbi.App.Models;

namespace FixRdlPbi.App.Business;

public class ReportDefinitionBusiness
{
    private readonly FabricApiClient _fabricApiClient;

    public ReportDefinitionBusiness(FabricApiClient fabricApiClient)
    {
        _fabricApiClient = fabricApiClient;
    }

    // public async Task<List<RdlVisualReference>> GetRdlVisualReferencesAsync(string workspaceId, string reportId)
    // {
    //     ReportDefinitionResponse definition = await GetReportDefinitionAsync(workspaceId, reportId);

    //     foreach (ReportDefinitionPart part in definition.Definition.Parts)
    //     {
    //         System.Diagnostics.Debug.WriteLine(part.Path);
    //     }

    //     return GetRdlVisualReferences(definition);
    // }

    public async Task<List<RdlVisualReference>> GetRdlVisualReferencesAsync(string workspaceId, string workspaceName, string reportId, string reportName)
    {
        ReportDefinitionResponse definition = await GetReportDefinitionAsync(workspaceId, reportId);

        await SaveDecodedDefinitionAsync(definition, workspaceName, reportName);

        return GetRdlVisualReferences(definition);
    }

    private static async Task SaveDecodedDefinitionAsync(ReportDefinitionResponse definition, string workspaceName, string reportName)
    {
        string definitionDirectory = Path.Combine(Directory.GetCurrentDirectory(), "definition");

        Directory.CreateDirectory(definitionDirectory);

        string safeWorkspaceName = SanitizeFileName(workspaceName);
        string safeReportName = SanitizeFileName(reportName);

        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        string fileName = $"{safeWorkspaceName}-{safeReportName}-{timestamp}.json";

        string filePath = Path.Combine(definitionDirectory, fileName);

        bool isPbirLegacy = definition.Definition.Parts.Any(x => string.Equals(x.Path, "report.json", StringComparison.OrdinalIgnoreCase));

        if (isPbirLegacy)
        {
            ReportDefinitionPart reportPart = definition.Definition.Parts
                .First(
                    x => string.Equals(x.Path, "report.json", StringComparison.OrdinalIgnoreCase)
                );

            string decodedJson = DecodeBase64Utf8(reportPart.Payload);

            using JsonDocument document = JsonDocument.Parse(decodedJson);

            string formattedJson = JsonSerializer.Serialize(
                document.RootElement,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            await File.WriteAllTextAsync(filePath, formattedJson, Encoding.UTF8);

            return;
        }

        Dictionary<string, JsonElement> decodedParts = new();

        foreach (ReportDefinitionPart part in definition.Definition.Parts)
        {
            if (!part.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string decodedJson = DecodeBase64Utf8(part.Payload);

            try
            {
                using JsonDocument document = JsonDocument.Parse(decodedJson);

                decodedParts[part.Path] = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                // Ignoramos partes que no sean JSON válido.
            }
        }

        string outputJson = JsonSerializer.Serialize(
            decodedParts,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );

        await File.WriteAllTextAsync(filePath, outputJson, Encoding.UTF8);
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidChar, '-');
        }

        return value.Trim();
    }

    private async Task<ReportDefinitionResponse> GetReportDefinitionAsync(string workspaceId, string reportId)
    {
        string endpoint = $"workspaces/{workspaceId}/reports/{reportId}/getDefinition";

        using HttpResponseMessage response = await _fabricApiClient.PostAsync(endpoint);

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            string json = await response.Content.ReadAsStringAsync();

            ReportDefinitionResponse result = JsonSerializer.Deserialize<ReportDefinitionResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            if (result == null)
            {
                throw new InvalidOperationException("Fabric returned an empty report definition.");
            }

            return result;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
        {
            return await ProcessLongRunningOperationAsync(response);
        }

        string error = await response.Content.ReadAsStringAsync();

        throw new HttpRequestException(
            $"Error getting report definition. HTTP {(int)response.StatusCode}: {error}"
        );
    }

    private async Task<ReportDefinitionResponse> ProcessLongRunningOperationAsync(HttpResponseMessage initialResponse)
    {
        string operationId = GetOperationId(initialResponse);

        int retryAfter = GetRetryAfter(initialResponse);

        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(retryAfter));

            FabricOperation operation = await _fabricApiClient.GetAsync<FabricOperation>(
                $"operations/{operationId}"
            );

            if (operation == null)
            {
                throw new InvalidOperationException("Fabric returned an empty operation response.");
            }

            if (string.Equals(operation.Status, "Succeeded", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.Equals(operation.Status, "Failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(operation.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Fabric operation finished with status: {operation.Status}"
                );
            }

            retryAfter = 2;
        }

        ReportDefinitionResponse result = await _fabricApiClient.GetAsync<ReportDefinitionResponse>(
            $"operations/{operationId}/result"
        );

        if (result == null)
        {
            throw new InvalidOperationException("Fabric returned an empty operation result.");
        }

        return result;
    }

    private static string GetOperationId(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("x-ms-operation-id", out IEnumerable<string> values))
        {
            return values.First();
        }

        if (response.Headers.Location != null)
        {
            string location = response.Headers.Location.ToString();

            string operationId = location
                .TrimEnd('/')
                .Split('/')
                .Last();

            if (!string.IsNullOrWhiteSpace(operationId))
            {
                return operationId;
            }
        }

        throw new InvalidOperationException("Fabric did not return x-ms-operation-id.");
    }

    private static int GetRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Retry-After", out IEnumerable<string> values))
        {
            string value = values.First();

            if (int.TryParse(value, out int seconds))
            {
                return seconds;
            }
        }

        return 2;
    }

    private static List<RdlVisualReference> GetRdlVisualReferences(ReportDefinitionResponse definition)
    {
        bool isPbir = definition.Definition.Parts.Any(
            x => x.Path.StartsWith(
                "definition/pages/",
                StringComparison.OrdinalIgnoreCase
            )
        );

        bool isPbirLegacy = definition.Definition.Parts.Any(
            x => string.Equals(
                x.Path,
                "report.json",
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (isPbir)
        {
            System.Diagnostics.Debug.WriteLine("Report definition format: PBIR");

            return GetPbirRdlVisualReferences(definition);
        }

        if (isPbirLegacy)
        {
            System.Diagnostics.Debug.WriteLine("Report definition format: PBIR-Legacy");

            return GetPbirLegacyRdlVisualReferences(definition);
        }

        throw new InvalidOperationException("Unsupported report definition format.");
    }

    // ============================================================
    // PBIR
    // ============================================================

    private static List<RdlVisualReference> GetPbirRdlVisualReferences(ReportDefinitionResponse definition)
    {
        List<RdlVisualReference> result = new();

        List<ReportDefinitionPart> visualParts = definition.Definition.Parts
            .Where(x =>
                x.Path.StartsWith(
                    "definition/pages/",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                x.Path.EndsWith(
                    "/visual.json",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .ToList();

        foreach (ReportDefinitionPart visualPart in visualParts)
        {
            string visualJson = DecodeBase64Utf8(visualPart.Payload);

            using JsonDocument visualDocument = JsonDocument.Parse(visualJson);

            JsonElement visualRoot = visualDocument.RootElement;

            string visualType = GetPbirVisualType(visualRoot);

            if (!string.Equals(visualType, "rdlVisual", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            System.Diagnostics.Debug.WriteLine("============================================================");
            System.Diagnostics.Debug.WriteLine($"RDL Visual: {visualPart.Path}");
            System.Diagnostics.Debug.WriteLine(visualJson);
            System.Diagnostics.Debug.WriteLine("============================================================");

            string workspaceId = FindLiteralValueRecursive(visualRoot, "workspaceId");

            //string reportId = FindLiteralValueRecursive(visualRoot, "reportId");
            string itemId = FindLiteralValueRecursive(visualRoot, "itemId");

            string pageName = GetPageNameFromVisualPath(visualPart.Path, definition);

            result.Add(
                new RdlVisualReference
                {
                    PageName = pageName,
                    WorkspaceId = workspaceId,
                    ReportId = itemId
                }
            );
        }

        return result;
    }

    private static string GetPbirVisualType(JsonElement visualRoot)
    {
        if (visualRoot.TryGetProperty("visual", out JsonElement visual))
        {
            if (visual.TryGetProperty("visualType", out JsonElement visualType))
            {
                string value = visualType.GetString();

                return string.IsNullOrWhiteSpace(value)
                    ? string.Empty
                    : value;
            }
        }

        if (visualRoot.TryGetProperty("visualType", out JsonElement rootVisualType))
        {
            string value = rootVisualType.GetString();

            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value;
        }

        return string.Empty;
    }

    private static string FindLiteralValueRecursive(JsonElement element, string propertyName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:

                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        string value = ExtractStringValue(property.Value);

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }

                    string nestedValue = FindLiteralValueRecursive(property.Value, propertyName);

                    if (!string.IsNullOrWhiteSpace(nestedValue))
                    {
                        return nestedValue;
                    }
                }

                break;

            case JsonValueKind.Array:

                foreach (JsonElement item in element.EnumerateArray())
                {
                    string nestedValue = FindLiteralValueRecursive(item, propertyName);

                    if (!string.IsNullOrWhiteSpace(nestedValue))
                    {
                        return nestedValue;
                    }
                }

                break;
        }

        return string.Empty;
    }

    private static string ExtractStringValue(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            string value = element.GetString();

            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim('\'');
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("expr", out JsonElement expr) &&
                expr.TryGetProperty("Literal", out JsonElement literal) &&
                literal.TryGetProperty("Value", out JsonElement value))
            {
                string literalValue = value.GetString();

                if (string.IsNullOrWhiteSpace(literalValue))
                {
                    return string.Empty;
                }

                return literalValue.Trim('\'');
            }

            if (element.TryGetProperty("value", out JsonElement directValue))
            {
                return directValue
                    .ToString()
                    .Trim('\'');
            }
        }

        return string.Empty;
    }

    private static string GetPageNameFromVisualPath(
        string visualPath,
        ReportDefinitionResponse definition)
    {
        string[] parts = visualPath.Split('/');

        if (parts.Length < 3)
        {
            return string.Empty;
        }

        string pageFolder = parts[2];

        string pagePath = $"definition/pages/{pageFolder}/page.json";

        ReportDefinitionPart pagePart = definition.Definition.Parts
            .FirstOrDefault(
                x => string.Equals(
                    x.Path,
                    pagePath,
                    StringComparison.OrdinalIgnoreCase
                )
            );

        if (pagePart == null)
        {
            return pageFolder;
        }

        string pageJson = DecodeBase64Utf8(pagePart.Payload);

        using JsonDocument pageDocument = JsonDocument.Parse(pageJson);

        JsonElement root = pageDocument.RootElement;

        if (root.TryGetProperty("displayName", out JsonElement displayName))
        {
            string value = displayName.GetString();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return pageFolder;
    }

    // ============================================================
    // PBIR-Legacy
    // ============================================================

    private static List<RdlVisualReference> GetPbirLegacyRdlVisualReferences(
        ReportDefinitionResponse definition)
    {
        ReportDefinitionPart reportPart = definition.Definition.Parts
            .FirstOrDefault(
                x => string.Equals(
                    x.Path,
                    "report.json",
                    StringComparison.OrdinalIgnoreCase
                )
            );

        if (reportPart == null)
        {
            throw new InvalidOperationException("report.json was not found.");
        }

        string decodedJson = DecodeBase64Utf8(reportPart.Payload);

        using JsonDocument document = JsonDocument.Parse(decodedJson);

        List<RdlVisualReference> result = new();

        JsonElement root = document.RootElement;

        if (!root.TryGetProperty("sections", out JsonElement sections))
        {
            return result;
        }

        foreach (JsonElement section in sections.EnumerateArray())
        {
            string pageName = GetStringProperty(section, "displayName");

            if (!section.TryGetProperty("visualContainers", out JsonElement visualContainers))
            {
                continue;
            }

            foreach (JsonElement visualContainer in visualContainers.EnumerateArray())
            {
                if (!visualContainer.TryGetProperty("config", out JsonElement configElement))
                {
                    continue;
                }

                string configJson = configElement.GetString();

                if (string.IsNullOrWhiteSpace(configJson))
                {
                    continue;
                }

                using JsonDocument configDocument = JsonDocument.Parse(configJson);

                JsonElement configRoot = configDocument.RootElement;

                if (!TryGetLegacyRdlVisualIds(
                    configRoot,
                    out string workspaceId,
                    out string paginatedReportId))
                {
                    continue;
                }

                result.Add(
                    new RdlVisualReference
                    {
                        PageName = pageName,
                        WorkspaceId = workspaceId,
                        ReportId = paginatedReportId
                    }
                );
            }
        }

        return result;
    }

    private static bool TryGetLegacyRdlVisualIds(
        JsonElement config,
        out string workspaceId,
        out string reportId)
    {
        workspaceId = string.Empty;
        reportId = string.Empty;

        if (!config.TryGetProperty("singleVisual", out JsonElement singleVisual))
        {
            return false;
        }

        string visualType = GetStringProperty(singleVisual, "visualType");

        if (!string.Equals(visualType, "rdlVisual", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!singleVisual.TryGetProperty("objects", out JsonElement objects) ||
            !objects.TryGetProperty("reportInfo", out JsonElement reportInfo) ||
            reportInfo.ValueKind != JsonValueKind.Array ||
            reportInfo.GetArrayLength() == 0)
        {
            return false;
        }

        JsonElement reportInfoItem = reportInfo[0];

        if (!reportInfoItem.TryGetProperty("properties", out JsonElement properties))
        {
            return false;
        }

        workspaceId = GetLegacyLiteralValue(properties, "workspaceId");

        reportId = GetLegacyLiteralValue(properties, "reportId");

        return
            !string.IsNullOrWhiteSpace(workspaceId) &&
            !string.IsNullOrWhiteSpace(reportId);
    }

    private static string GetLegacyLiteralValue(JsonElement properties, string propertyName)
    {
        if (!properties.TryGetProperty(propertyName, out JsonElement property))
        {
            return string.Empty;
        }

        if (!property.TryGetProperty("expr", out JsonElement expr) ||
            !expr.TryGetProperty("Literal", out JsonElement literal) ||
            !literal.TryGetProperty("Value", out JsonElement value))
        {
            return string.Empty;
        }

        string result = value.GetString();

        if (string.IsNullOrWhiteSpace(result))
        {
            return string.Empty;
        }

        return result.Trim('\'');
    }

    private static string GetStringProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property))
        {
            return string.Empty;
        }

        string value = property.GetString();

        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value;
    }

    private static string DecodeBase64Utf8(string base64)
    {
        byte[] bytes = Convert.FromBase64String(base64);

        return Encoding.UTF8.GetString(bytes);
    }
}