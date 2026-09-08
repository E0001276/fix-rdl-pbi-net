using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using FixRdlPbi.App.Clients;
using FixRdlPbi.App.Models;

namespace FixRdlPbi.App.Business;

public class PaginatedReportDefinitionBusiness
{
    private readonly FabricApiClient _fabricApiClient;

    public PaginatedReportDefinitionBusiness(FabricApiClient fabricApiClient)
    {
        _fabricApiClient = fabricApiClient;
    }

    public async Task<PaginatedReportInspectionResult> GetInspectionAsync(
        string workspaceId,
        string paginatedReportId)
    {
        ReportDefinitionResponse definition = await GetDefinitionAsync(
            workspaceId,
            paginatedReportId
        );

        return new PaginatedReportInspectionResult
        {
            DataSources = GetDataSources(definition)
        };
    }

    private async Task<ReportDefinitionResponse> GetDefinitionAsync(
        string workspaceId,
        string paginatedReportId)
    {
        string endpoint =
            $"workspaces/{workspaceId}/paginatedReports/{paginatedReportId}/getDefinition";

        using HttpResponseMessage response = await _fabricApiClient.PostAsync(endpoint);

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            return await DeserializeDefinitionAsync(response);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
        {
            return await ProcessLongRunningOperationAsync(response);
        }

        string error = await response.Content.ReadAsStringAsync();

        throw new HttpRequestException(
            $"Error getting paginated report definition. HTTP {(int)response.StatusCode}: {error}"
        );
    }

    private static async Task<ReportDefinitionResponse> DeserializeDefinitionAsync(
        HttpResponseMessage response)
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
            throw new InvalidOperationException(
                "Fabric returned an empty paginated report definition."
            );
        }

        return result;
    }

    private async Task<ReportDefinitionResponse> ProcessLongRunningOperationAsync(
        HttpResponseMessage initialResponse)
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
                throw new InvalidOperationException(
                    "Fabric returned an empty operation response."
                );
            }

            if (string.Equals(
                operation.Status,
                "Succeeded",
                StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.Equals(
                    operation.Status,
                    "Failed",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    operation.Status,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Fabric operation finished with status: {operation.Status}"
                );
            }

            retryAfter = 2;
        }

        ReportDefinitionResponse result =
            await _fabricApiClient.GetAsync<ReportDefinitionResponse>(
                $"operations/{operationId}/result"
            );

        if (result == null)
        {
            throw new InvalidOperationException(
                "Fabric returned an empty operation result."
            );
        }

        return result;
    }

    private static List<PaginatedDataSource> GetDataSources(
        ReportDefinitionResponse definition)
    {
        ReportDefinitionPart rdlPart = definition.Definition.Parts
            .FirstOrDefault(
                x => x.Path.EndsWith(".rdl", StringComparison.OrdinalIgnoreCase)
            );

        if (rdlPart == null)
        {
            return new List<PaginatedDataSource>();
        }

        XDocument document = LoadRdlDocument(rdlPart);

        XElement dataSourcesElement = document
            .Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "DataSources");

        if (dataSourcesElement == null)
        {
            return new List<PaginatedDataSource>();
        }

        List<PaginatedDataSource> result = new();

        foreach (XElement dataSourceElement in dataSourcesElement
            .Elements()
            .Where(x => x.Name.LocalName == "DataSource"))
        {
            XElement connectionProperties = dataSourceElement
                .Elements()
                .FirstOrDefault(x => x.Name.LocalName == "ConnectionProperties");

            PaginatedDataSource dataSource = new()
            {
                Name = GetAttributeValue(dataSourceElement, "Name"),
                DataProvider = GetChildValue(connectionProperties, "DataProvider"),
                ConnectionString = GetChildValue(connectionProperties, "ConnectString"),
                DataSourceReference = GetChildValue(
                    dataSourceElement,
                    "DataSourceReference"
                ),
                DataSourceId = GetChildValue(dataSourceElement, "DataSourceID")
            };

            result.Add(dataSource);
        }

        return result;
    }

    private static string GetAttributeValue(XElement element, string attributeName)
    {
        XAttribute attribute = element.Attributes()
            .FirstOrDefault(
                x => string.Equals(
                    x.Name.LocalName,
                    attributeName,
                    StringComparison.OrdinalIgnoreCase
                )
            );

        return attribute == null
            ? string.Empty
            : attribute.Value;
    }

    private static string GetChildValue(XElement element, string childName)
    {
        if (element == null)
        {
            return string.Empty;
        }

        XElement child = element.Elements()
            .FirstOrDefault(
                x => string.Equals(
                    x.Name.LocalName,
                    childName,
                    StringComparison.OrdinalIgnoreCase
                )
            );

        return child == null
            ? string.Empty
            : child.Value;
    }

    private static string GetOperationId(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues(
            "x-ms-operation-id",
            out IEnumerable<string> values))
        {
            return values.First();
        }

        if (response.Headers.Location != null)
        {
            string operationId = response.Headers.Location
                .ToString()
                .TrimEnd('/')
                .Split('/')
                .Last();

            if (!string.IsNullOrWhiteSpace(operationId))
            {
                return operationId;
            }
        }

        throw new InvalidOperationException(
            "Fabric did not return x-ms-operation-id."
        );
    }

    private static int GetRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues(
            "Retry-After",
            out IEnumerable<string> values))
        {
            string value = values.First();

            if (int.TryParse(value, out int seconds))
            {
                return seconds;
            }
        }

        return 2;
    }

    private static XDocument LoadRdlDocument(ReportDefinitionPart rdlPart)
    {
        if (string.IsNullOrWhiteSpace(rdlPart.Payload))
        {
            throw new InvalidOperationException(
                "Fabric returned an empty RDL payload."
            );
        }

        byte[] bytes;

        try
        {
            bytes = Convert.FromBase64String(rdlPart.Payload);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"The RDL payload is not valid Base64. Payload type: {rdlPart.PayloadType}",
                ex
            );
        }

        try
        {
            using MemoryStream stream = new(bytes, writable: false);

            // XDocument.Load(Stream) lets the XML parser detect the real encoding
            // from the BOM/XML declaration. RDL files can be UTF-8 or UTF-16.
            return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }
        catch (Exception ex) when (ex is System.Xml.XmlException || ex is InvalidOperationException)
        {
            string prefix = GetDecodedPrefix(bytes);

            throw new InvalidOperationException(
                "The paginated report definition was returned, but the RDL could not be parsed as XML."
                + Environment.NewLine
                + $"RDL part: {rdlPart.Path}"
                + Environment.NewLine
                + $"Payload type: {rdlPart.PayloadType}"
                + Environment.NewLine
                + $"Decoded prefix: {prefix}",
                ex
            );
        }
    }

    private static string GetDecodedPrefix(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return "(empty)";
        }

        int length = Math.Min(bytes.Length, 120);

        string text;

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            text = Encoding.Unicode.GetString(bytes, 0, length - (length % 2));
        }
        else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            text = Encoding.BigEndianUnicode.GetString(bytes, 0, length - (length % 2));
        }
        else
        {
            text = Encoding.UTF8.GetString(bytes, 0, length);
        }

        return text
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();
    }
}
