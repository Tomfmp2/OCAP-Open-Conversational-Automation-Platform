using OCAP.DeploymentManager.Models;
using OCAP.DeploymentManager.Services;

Console.WriteLine("=================================================");
Console.WriteLine("        OCAP DEPLOYMENT MANAGER FOUNDATION       ");
Console.WriteLine("   Open Conversational Automation Platform v0.9.0 ");
Console.WriteLine("=================================================");
Console.WriteLine();

var config = new DeploymentConfiguration();
var generator = new EnvironmentGenerator();
var validator = new DeploymentValidator();

Console.WriteLine("Seleccione el modo de instalación:");
Console.WriteLine("[1] Desarrollo Local (Valores predeterminados)");
Console.WriteLine("[2] Servidor Personal");
Console.WriteLine("[3] Servidor Empresarial");
Console.Write("Opción [1-3] (Por defecto: 1): ");

var choice = Console.ReadLine();
if (choice == "2") config.Mode = InstallationMode.PersonalServer;
else if (choice == "3") config.Mode = InstallationMode.EnterpriseServer;
else config.Mode = InstallationMode.LocalDevelopment;

Console.WriteLine($"\n[Configurando para modo: {config.Mode}]");

// Validar la configuración
var (isValid, errors) = validator.Validate(config);

if (!isValid)
{
    Console.WriteLine("\n❌ Errores en la configuración:");
    foreach (var err in errors)
    {
        Console.WriteLine($" - {err}");
    }
    return;
}

Console.WriteLine("\n✓ Configuración validada correctamente.");

var envContent = generator.GenerateEnvironmentFileContent(config);
var outputPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ".env");
System.IO.File.WriteAllText(outputPath, envContent);

Console.WriteLine($"✓ Archivo .env generado exitosamente en: {outputPath}");
Console.WriteLine("\n[Próximos pasos de despliegue]");
Console.WriteLine("Ejecute el siguiente comando para iniciar los contenedores:");
Console.WriteLine("  docker-compose up -d --build");
Console.WriteLine("\n¡Despliegue configurado con éxito!");
