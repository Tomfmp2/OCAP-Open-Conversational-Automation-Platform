using OCAP.Providers.Google.Abstractions;
using OCAP.Providers.Google.Calendar;
using OCAP.Providers.Google.Gmail;
using OCAP.Providers.Google.Sheets;
using OCAP.Tools.Google;
using OCAP.Tools.Abstractions;

namespace OCAP.Api.Extensions;

public static partial class ApiServiceExtensions
{
    /// <summary>Google Workspace: adaptadores reales con token OAuth o en memoria en desarrollo, y sus tools.</summary>
    private static void AddGoogleWorkspaceServices(IServiceCollection services, IConfiguration configuration)
    {
        // Google Workspace usa adaptadores reales cuando hay token. En producción sin
        // credenciales también se registran los adaptadores HTTP para evitar éxitos ficticios.
        var googleSection = configuration.GetSection(GoogleWorkspaceOptions.SectionName);
        var googleOptions = googleSection.Get<GoogleWorkspaceOptions>() ?? new GoogleWorkspaceOptions();
        services.Configure<GoogleWorkspaceOptions>(googleSection);

        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environments.Production;
        var isDevelopmentOrTesting =
            environmentName.Equals(Environments.Development, StringComparison.OrdinalIgnoreCase) ||
            environmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase);
        var useInMemoryGoogle =
            googleOptions.UseInMemory ||
            (string.IsNullOrWhiteSpace(googleOptions.AccessToken) && isDevelopmentOrTesting);

        if (useInMemoryGoogle)
        {
            services.AddSingleton<ICalendarProvider, InMemoryCalendarProvider>();
            services.AddSingleton<IEmailProvider, InMemoryEmailProvider>();
            services.AddSingleton<ISpreadsheetProvider, InMemorySpreadsheetProvider>();
        }
        else
        {
            services.AddScoped<IGoogleAccessTokenProvider, OCAP.Api.Services.OAuthGoogleAccessTokenProvider>();
            services.AddHttpClient<ICalendarProvider, GoogleCalendarHttpProvider>()
                .AddStandardResilienceHandler();
            services.AddHttpClient<IEmailProvider, GoogleGmailHttpProvider>()
                .AddStandardResilienceHandler();
            services.AddHttpClient<ISpreadsheetProvider, GoogleSheetsHttpProvider>()
                .AddStandardResilienceHandler();
        }

        // Registrar Google Tools en DI
        services.AddTransient<ITool, CreateCalendarEventTool>();
        services.AddTransient<ITool, SendEmailTool>();
        services.AddTransient<ITool, ListEmailsTool>();
        services.AddTransient<ITool, AppendSpreadsheetRowTool>();
    }
}
