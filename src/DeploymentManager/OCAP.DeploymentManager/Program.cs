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

static string Read(string label, string defaultValue = "")
{
    Console.Write(string.IsNullOrEmpty(defaultValue) ? $"{label}: " : $"{label} [{defaultValue}]: ");
    var value = Console.ReadLine();
    return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
}

static bool ReadYesNo(string label, bool defaultValue = false)
{
    var hint = defaultValue ? "S/n" : "s/N";
    Console.Write($"{label} ({hint}): ");
    var value = Console.ReadLine()?.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(value)) return defaultValue;
    return value is "s" or "si" or "sí" or "y" or "yes";
}

static int ReadInt(string label, int defaultValue)
{
    var raw = Read(label, defaultValue.ToString());
    return int.TryParse(raw, out var n) ? n : defaultValue;
}

Console.WriteLine("Seleccione el modo de instalación:");
Console.WriteLine("[1] Desarrollo Local");
Console.WriteLine("[2] Servidor Personal");
Console.WriteLine("[3] Servidor Empresarial");
var modeChoice = Read("Opción [1-3]", "1");
config.Mode = modeChoice switch
{
    "2" => InstallationMode.PersonalServer,
    "3" => InstallationMode.EnterpriseServer,
    _ => InstallationMode.LocalDevelopment
};

Console.WriteLine("\nDestino de despliegue:");
Console.WriteLine("[1] Local (puertos)");
Console.WriteLine("[2] Web (URLs públicas)");
var targetChoice = Read("Opción [1-2]", config.Mode == InstallationMode.LocalDevelopment ? "1" : "2");
config.Target = targetChoice == "2" ? DeploymentTarget.Web : DeploymentTarget.Local;

if (config.Target == DeploymentTarget.Local)
{
    config.FrontendHostPort = ReadInt("Puerto frontend (panel admin)", config.FrontendHostPort);
    config.ApiHostPort = ReadInt("Puerto API", config.ApiHostPort);
    config.PublicApiUrl = $"http://localhost:{config.ApiHostPort}";
    config.PublicPanelUrl = $"http://localhost:{config.FrontendHostPort}";
}
else
{
    config.PublicApiUrl = Read("URL pública API", config.PublicApiUrl);
    config.PublicPanelUrl = Read("URL pública panel admin", config.PublicPanelUrl);
}

if (config.Mode == InstallationMode.LocalDevelopment)
{
    config.EventBusProvider = Read("EventBus provider (RabbitMQ/InMemory)", "InMemory");
    config.PostgresPort = 5433;
}

Console.WriteLine("\n--- PostgreSQL ---");
config.PostgresHost = Read("Host", config.PostgresHost);
config.PostgresPort = ReadInt("Puerto host", config.PostgresPort);
config.PostgresDbName = Read("Base de datos", config.PostgresDbName);
config.PostgresUsername = Read("Usuario", config.PostgresUsername);
config.PostgresPassword = Read("Contraseña", config.PostgresPassword);

Console.WriteLine("\n--- Admin / Tenant ---");
config.BootstrapTenantName = Read("Nombre organización", config.BootstrapTenantName);
config.BootstrapTenantSlug = Read("Slug", config.BootstrapTenantSlug);
config.BootstrapAdminEmail = Read("Email admin", config.BootstrapAdminEmail);
config.BootstrapAdminPassword = Read("Contraseña admin", config.BootstrapAdminPassword);

Console.WriteLine("\n--- Google Workspace ---");
config.EnableGoogleWorkspace = ReadYesNo("¿Activar Google Workspace?", true);
if (config.EnableGoogleWorkspace)
{
    config.GoogleClientId = Read("Google Client ID", config.GoogleClientId);
    config.GoogleClientSecret = Read("Google Client Secret", config.GoogleClientSecret);
    config.GoogleRedirectUri = Read(
        "Redirect URI (vacío = auto)",
        config.ResolveGoogleRedirectUri());
}

Console.WriteLine("\n--- Proveedor IA ---");
config.AiProvider = Read("Proveedor (OpenAI/Gemini/Claude/Ollama)", config.AiProvider);
config.AiModelName = Read("Modelo", config.AiModelName);
config.AiApiKey = Read("API key", config.AiApiKey);
config.AiBaseUrl = Read("Base URL (opcional)", config.AiBaseUrl);

Console.WriteLine("\n--- Canales opcionales ---");
config.EnableWhatsApp = ReadYesNo("¿Activar WhatsApp (Evolution)?", false);
if (config.EnableWhatsApp)
{
    config.EvolutionApiUrl = Read("Evolution URL", config.EvolutionApiUrl);
    config.EvolutionApiKey = Read("Evolution API key", config.EvolutionApiKey);
}
config.EnableTelegram = ReadYesNo("¿Activar Telegram?", false);
if (config.EnableTelegram)
{
    config.TelegramBotToken = Read("Telegram bot token", config.TelegramBotToken);
}

config.ApiHealthUrl = $"{config.ResolvePublicApiUrl()}/health/ready";

Console.WriteLine($"\n[Configurando modo={config.Mode} target={config.Target}]");

var composePath = Path.GetFullPath(config.ComposeFilePath);
var report = await validator.ValidateInfrastructureAsync(config, composePath);

Console.WriteLine("\n--- Validación de infraestructura ---");
Console.WriteLine(report.ToJson());

if (!report.ConfigValid)
{
    Console.WriteLine("\nErrores de configuración:");
    foreach (var err in report.ConfigErrors)
        Console.WriteLine($" - {err}");
    return 1;
}

var envContent = generator.GenerateEnvironmentFileContent(config);
var outputPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
await File.WriteAllTextAsync(outputPath, envContent);
Console.WriteLine($"\nArchivo .env generado: {outputPath}");

var configDir = Path.Combine(Directory.GetCurrentDirectory(), "config");
Directory.CreateDirectory(configDir);
await File.WriteAllTextAsync(Path.Combine(configDir, "generated.env"), envContent);

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
    ? "\nValidación lista para despliegue. Tras levantar el stack abre /installer para diagnóstico o reconfiguración."
    : "\nValidación incompleta: revise Docker/Compose/red antes de levantar servicios.");

return report.IsReady ? 0 : 2;
