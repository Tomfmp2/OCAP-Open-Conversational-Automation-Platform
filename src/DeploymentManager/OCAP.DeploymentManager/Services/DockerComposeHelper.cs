using System.Diagnostics;

namespace OCAP.DeploymentManager.Services;

public class DockerComposeHelper
{
    public bool ValidateDockerComposeExists(string path) => File.Exists(path);

    public string BuildUpCommand(string composePath)
        => $"docker compose -f \"{composePath}\" up -d --build";

    public string GetStartDockerCommand(string composePath) => BuildUpCommand(composePath);

    public async Task<(bool Success, string Output)> RunUpAsync(
        string composePath,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose -f \"{composePath}\" up -d --build",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process is null)
            return (false, "No se pudo iniciar el proceso docker.");

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var output = string.IsNullOrWhiteSpace(stdout) ? stderr : $"{stdout}\n{stderr}";
        return (process.ExitCode == 0, output.Trim());
    }

    public async Task<bool> WaitForHttpOkAsync(string url, int retries = 60, int delayMs = 5000, CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        for (var i = 0; i < retries; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var response = await http.GetAsync(url, cancellationToken);
                if ((int)response.StatusCode is >= 200 and < 500)
                    return true;
            }
            catch
            {
                // retry
            }

            await Task.Delay(delayMs, cancellationToken);
        }

        return false;
    }
}
