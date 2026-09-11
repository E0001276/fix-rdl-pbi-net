using System.Text;
using System.Text.Json;

namespace FixRdlPbi.App.Services;

public sealed class DiagnosticSession
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DiagnosticSession(string operationName)
    {
        string safeName = SanitizeFileName(operationName);
        DirectoryPath = Path.Combine(
            AppContext.BaseDirectory,
            "diagnostics",
            $"{DateTime.Now:yyyyMMdd-HHmmss}-{safeName}"
        );

        Directory.CreateDirectory(DirectoryPath);
    }

    public string DirectoryPath { get; }

    public void WriteText(string fileName, string content)
    {
        File.WriteAllText(
            Path.Combine(DirectoryPath, fileName),
            content ?? string.Empty,
            new UTF8Encoding(false)
        );
    }

    public void WriteJson(string fileName, object value)
    {
        string json = JsonSerializer.Serialize(value, _jsonOptions);
        WriteText(fileName, json);
    }

    public void WriteSummaryLine(string text)
    {
        File.AppendAllText(
            Path.Combine(DirectoryPath, "summary.txt"),
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}{Environment.NewLine}",
            new UTF8Encoding(false)
        );
    }

    public static string SanitizeFileName(string value)
    {
        string result = value ?? string.Empty;

        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(result) ? "operation" : result;
    }
}
