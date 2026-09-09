using System.Diagnostics;

namespace FixRdlPbi.App.Services;

public class FabricTokenProvider
{
    private const string FabricResource = "https://api.fabric.microsoft.com";
    private const string PowerBiResource = "https://analysis.windows.net/powerbi/api";

    private const string AzureCliPath = @"C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd";

    public Task<string> GetAccessTokenAsync()
    {
        return GetAccessTokenForResourceAsync(FabricResource);
    }

    public Task<string> GetPowerBiAccessTokenAsync()
    {
        return GetAccessTokenForResourceAsync(PowerBiResource);
    }

    private async Task<string> GetAccessTokenForResourceAsync(string resource)
    {
        if (!File.Exists(AzureCliPath))
        {
            throw new FileNotFoundException($"No se encontró Azure CLI en: {AzureCliPath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = AzureCliPath,
            Arguments = $"account get-access-token "
                + $"--resource {resource} "
                + $"--query accessToken "
                + $"-o tsv",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();

        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"No fue posible obtener el token para {resource}." + Environment.NewLine + error);
        }

        string token = output.Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Azure CLI no devolvió un access token.");
        }

        return token;
    }
}