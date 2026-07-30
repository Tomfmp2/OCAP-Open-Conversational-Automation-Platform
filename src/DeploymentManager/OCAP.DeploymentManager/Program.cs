using OCAP.DeploymentManager.Models;
using OCAP.DeploymentManager.Services;

Console.WriteLine("=================================================");
Console.WriteLine("        OCAP DEPLOYMENT MANAGER                  ");
Console.WriteLine("   Open Conversational Automation Platform       ");
Console.WriteLine("=================================================");
Console.WriteLine();

var config = new DeploymentConfiguration();
var generator = new EnvironmentGenerator();
var validator = new DeploymentValidator();
var composeHelper = new DockerComposeHelper();

Console.WriteLine("Seleccione el modo de instalación:");
Console.WriteLine("[1] Desarrollo Local (Valores predeterminados)");
Console.WriteLine("[2] Servidor Personal");
Console.WriteLine("[3] Servidor Empresarial");
Console.Write("Opción [1-3] (Por defecto: 1): ");

var choice = Console.ReadLine();
if (choice == "2") config.Mode = InstallationMode.PersonalServer;
else if (choice == "3") config.Mode = InstallationMode.EnterpriseServer;
else config.Mode = InstallationMode.LocalDevelopment;

if (config.Mode == InstallationMode.LocalDevelopment)
{
    config.EventBusProvider = "InMemory";
    config.PostgresPort = 5433;
}

Console.WriteLine($"\n[Configurando para modo: {config.Mode}]");

var composePath = Path.GetFullPath(config.ComposeFilePath);
var report = await validator.ValidateInfrastructureAsync(config, composePath);

Console.WriteLine("\n--- Validación de infraestructura ---");
Console.WriteLine(report.ToJson());

if (!report.ConfigValid)
{
    Console.WriteLine("\nErrores de configuración:");
    foreach (var err in report.ConfigErrors)
    {
        Console.WriteLine($" - {err}");
    }
    return 1;
}

var envContent = generator.GenerateEnvironmentFileContent(config);
var outputPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
await File.WriteAllTextAsync(outputPath, envContent);

Console.WriteLine($"\nArchivo .env generado: {outputPath}");

if (report.ComposeFileExists && report.ComposeAvailable)
{
    var upCommand = composeHelper.BuildUpCommand(composePath);
    Console.WriteLine("\nComando sugerido (no ejecutado automáticamente):");
    Console.WriteLine($"  {upCommand}");
}
else
{
    Console.WriteLine("\nDocker Compose no disponible o docker-compose.yml ausente.");
}

Console.WriteLine(report.IsReady
    ? "\nValidación lista para despliegue."
    : "\nValidación incompleta: revise Docker/Compose/red antes de levantar servicios.");

return report.IsReady ? 0 : 2;
