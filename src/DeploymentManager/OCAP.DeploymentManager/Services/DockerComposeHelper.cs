namespace OCAP.DeploymentManager.Services;

// Asistente para la validación y despliegue de contenedores Docker de OCAP.
public class DockerComposeHelper
{
    public bool ValidateDockerComposeExists(string path)
    {
        return System.IO.File.Exists(path);
    }

    public string GetStartDockerCommand(string composePath)
    {
        return $"docker-compose -f {composePath} up -d --build";
    }
}
