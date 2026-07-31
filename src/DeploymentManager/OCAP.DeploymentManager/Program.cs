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

Console.WriteLine("Seleccione el modo de instalación:");
Console.WriteLine("[1] Desarrollo Local (Docker Compose, puertos 3000/5000)");
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
Console.WriteLine("[1] Local (Docker Compose — panel :3000 / API :5000)");
Console.WriteLine("[2] Web (URLs públicas)");
var targetChoice = Read("Opción [1-2]", config.Mode == InstallationMode.LocalDevelopment ? "1" : "2");
config.Target = targetChoice == "2" ? DeploymentTarget.Web : DeploymentTarget.Local;

if (config.Target == DeploymentTarget.Local)
{
    // Puertos fijos para que el stack siempre sea alcanzable tras compose up.
    config.FrontendHostPort = 3000;
    config.ApiHostPort = 5000;
    config.PublicApiUrl = "http://localhost:5000";
    config.PublicPanelUrl = "http://localhost:3000";
    Console.WriteLine("Puertos Local fijados: frontend 3000, API 5000.");
}
else
{
    config.PublicApiUrl = Read("URL pública API", config.PublicApiUrl);
    config.PublicPanelUrl = Read("URL pública panel admin", config.PublicPanelUrl);
}

if (config.Mode == InstallationMode.LocalDevelopment)
{
    config.EventBusProvider = "RabbitMQ";
    config.PostgresPort = 5433;
    config.PostgresHost = "localhost";
}

Console.WriteLine("\n--- PostgreSQL (Compose usa defaults; cambiar password requiere down -v) ---");
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

var workingDir = Directory.GetCurrentDirectory();
var composePath = Path.GetFullPath(Path.Combine(workingDir, config.ComposeFilePath));
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
var outputPath = Path.Combine(workingDir, ".env");
await File.WriteAllTextAsync(outputPath, envContent);
Console.WriteLine($"\nArchivo .env generado: {outputPath}");

var configDir = Path.Combine(workingDir, "config");
Directory.CreateDirectory(configDir);
await File.WriteAllTextAsync(Path.Combine(configDir, "generated.env"), envContent);

if (!(report.ComposeFileExists && report.ComposeAvailable))
{
    Console.WriteLine("\nDocker Compose no disponible o docker-compose.yml ausente.");
    return 2;
}

var runCompose = ReadYesNo("¿Ejecutar docker compose up --build -d ahora?", true);
if (!runCompose)
{
    Console.WriteLine("\nComando sugerido:");
    Console.WriteLine($"  {composeHelper.BuildUpCommand(composePath)}");
    Console.WriteLine("O: ./scripts/ocap-up.sh");
    return 0;
}

Console.WriteLine("\n--- Montando stack ---");
var (upOk, upOutput) = await composeHelper.RunUpAsync(composePath, workingDir);
Console.WriteLine(upOutput);
if (!upOk)
{
    Console.WriteLine("docker compose up falló.");
    return 3;
}

Console.WriteLine("Esperando API healthy...");
var apiOk = await composeHelper.WaitForHttpOkAsync(config.ApiHealthUrl, retries: 72, delayMs: 5000);
Console.WriteLine("Esperando frontend...");
var frontOk = await composeHelper.WaitForHttpOkAsync(config.ResolvePublicPanelUrl() + "/", retries: 72, delayMs: 5000);

if (!apiOk || !frontOk)
{
    Console.WriteLine($"API ready: {apiOk}, Frontend: {frontOk}. Revisa docker compose logs.");
    return 4;
}

Console.WriteLine();
Console.WriteLine("==============================================");
Console.WriteLine("  OCAP montado");
Console.WriteLine($"  Panel:      {config.ResolvePublicPanelUrl()}");
Console.WriteLine($"  Instalador: {config.ResolvePublicPanelUrl()}/installer");
Console.WriteLine($"  API:        {config.ResolvePublicApiUrl()}");
Console.WriteLine("==============================================");
return 0;
