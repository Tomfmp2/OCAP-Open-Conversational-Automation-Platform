namespace OCAP.DeploymentManager.Services;

public class DockerComposeHelper
{
    public bool ValidateDockerComposeExists(string path) => File.Exists(path);

    public string BuildUpCommand(string composePath)
        => $"docker compose -f \"{composePath}\" up -d --build";

    public string GetStartDockerCommand(string composePath) => BuildUpCommand(composePath);
}
