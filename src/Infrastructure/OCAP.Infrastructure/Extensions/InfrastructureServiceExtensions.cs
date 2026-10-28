using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Core.Ports;
using OCAP.Intelligence.Abstractions;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Infrastructure.Persistence.Interceptors;
using OCAP.Infrastructure.Persistence.Repositories;
using OCAP.Infrastructure.Persistence.Tenancy;
using OCAP.Security.Abstractions;
using Pgvector.EntityFrameworkCore;

namespace OCAP.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var useInMemory = string.Equals(configuration["UseInMemory"], "true", StringComparison.OrdinalIgnoreCase);
        var environment = configuration[Microsoft.Extensions.Hosting.HostDefaults.EnvironmentKey]
            ?? configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"] ?? "Production";
        if (useInMemory && string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "UseInMemory=true no está permitido en Production: los datos se perderían al reiniciar. Configura ConnectionStrings:DefaultConnection.");
        }
        services.AddObjectStorage(configuration);

        // Fallback tenant context for non-HTTP hosts; API replaces with HttpTenantContext.
        if (!services.Any(d => d.ServiceType == typeof(ITenantContext)))
        {
            services.AddScoped<ITenantContext>(_ => SystemTenantContext.Instance);
        }

        // Audit Trail Interceptor
        services.AddSingleton(sp => new AuditSaveChangesInterceptor(sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()));
        services.AddScoped<TenantSaveChangesInterceptor>();
        services.AddScoped<TenantRlsConnectionInterceptor>();

        if (useInMemory)
        {
            var dbName = configuration["InMemoryDbName"] ?? "OCAP_InMemory_Db";
            services.AddDbContext<OCAPDbContext>((sp, options) =>
            {
                var audit = sp.GetRequiredService<AuditSaveChangesInterceptor>();
                var tenant = sp.GetRequiredService<TenantSaveChangesInterceptor>();
                options.UseInMemoryDatabase(dbName).AddInterceptors(audit, tenant);
            });
        }
        else
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<OCAPDbContext>((sp, options) =>
            {
                var audit = sp.GetRequiredService<AuditSaveChangesInterceptor>();
                var tenant = sp.GetRequiredService<TenantSaveChangesInterceptor>();
                var rls = sp.GetRequiredService<TenantRlsConnectionInterceptor>();
                options.UseNpgsql(connectionString, b =>
                       {
                           b.MigrationsAssembly(typeof(OCAPDbContext).Assembly.FullName);
                           b.UseVector();
                       })
                       .AddInterceptors(audit, tenant, rls);
            });
        }

        // Allow Knowledge EF repositories / PgVector to resolve the abstract DbContext.
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<OCAPDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<SessionRepository>(); // Registered directly as no port currently exists in Core
        services.AddScoped<OCAP.Agents.Abstractions.Ports.IAgentRepository, AgentRepository>();
        services.AddScoped<OCAP.Agents.Abstractions.Contracts.IOcapSystemContextProvider, OCAP.Infrastructure.Services.OcapSystemContextProvider>();
        services.AddScoped<IToolExecutionRepository, ToolExecutionRepository>();
        services.AddScoped<IAiExecutionLogRepository, AiExecutionLogRepository>();
        services.AddScoped<IAiConversationMemoryRepository, AiConversationMemoryRepository>();

        // Retention & Maintenance Options
        services.Configure<OCAP.Infrastructure.Options.RetentionOptions>(configuration.GetSection(OCAP.Infrastructure.Options.RetentionOptions.SectionName));

        // Trabajos en segundo plano: el estado vive en la base (outbox, retención), nunca en colas de memoria.
        services.AddHostedService<OCAP.Infrastructure.BackgroundJobs.OutboxProcessorBackgroundService>();
        services.AddHostedService<OCAP.Infrastructure.BackgroundJobs.AuditAndOutboxRetentionBackgroundService>();

        // Distributed Event Bus (InMemory | RabbitMQ | NATS)
        services.Configure<OCAP.Infrastructure.Events.Distributed.EventBusOptions>(
            configuration.GetSection(OCAP.Infrastructure.Events.Distributed.EventBusOptions.SectionName));
        services.PostConfigure<OCAP.Infrastructure.Events.Distributed.EventBusOptions>(opts =>
        {
            var isDev = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(environment, "Testing", StringComparison.OrdinalIgnoreCase);
            // Fuera de desarrollo los eventos no pueden vivir solo en memoria del proceso (se pierden al reiniciar y
            // no llegan a otras réplicas).
            if (!isDev && string.Equals(opts.Provider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                opts.Provider = "RabbitMQ";
            }

            if (string.Equals(opts.Provider, "RabbitMQ", StringComparison.OrdinalIgnoreCase)
                || string.Equals(opts.Provider, "NATS", StringComparison.OrdinalIgnoreCase))
            {
                opts.ImmediateDispatch = false;
            }
        });

        services.AddSingleton<OCAP.Core.Events.Distributed.IEventSerializer, OCAP.Infrastructure.Events.Distributed.JsonEventSerializer>();
        services.AddSingleton<OCAP.Core.Events.Distributed.IMessageRetryPolicy, OCAP.Infrastructure.Events.Distributed.ExponentialBackoffRetryPolicy>();
        services.AddSingleton<OCAP.Core.Events.Distributed.IEventTransport>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OCAP.Infrastructure.Events.Distributed.EventBusOptions>>().Value;
            var serializer = sp.GetRequiredService<OCAP.Core.Events.Distributed.IEventSerializer>();
            return opts.Provider.Trim().ToUpperInvariant() switch
            {
                "RABBITMQ" => ActivatorUtilities.CreateInstance<OCAP.Infrastructure.Events.Distributed.RabbitMqEventTransport>(sp),
                "NATS" => ActivatorUtilities.CreateInstance<OCAP.Infrastructure.Events.Distributed.NatsJetStreamEventTransport>(sp),
                _ => new OCAP.Infrastructure.Events.Distributed.InMemoryEventTransport(serializer)
            };
        });
        services.AddScoped<OCAP.Core.Events.Distributed.IOutboxStore, OCAP.Infrastructure.Events.Distributed.EfOutboxStore>();
        services.AddScoped<OCAP.Core.Events.Distributed.IInboxStore, OCAP.Infrastructure.Events.Distributed.EfInboxStore>();
        services.AddScoped<OCAP.Core.Events.Distributed.IMessageDeadLetterHandler, OCAP.Infrastructure.Events.Distributed.MessageDeadLetterHandler>();
        services.AddSingleton<OCAP.Core.Events.IEventBus, OCAP.Infrastructure.Events.Distributed.DistributedEventBus>();
        services.AddHostedService<OCAP.Infrastructure.Events.Distributed.EventBusConnectionHostedService>();

        return services;
    }

    /// <summary>
    /// Migra el esquema, aplica las políticas RLS y, si Database:AppRole está configurado, crea o actualiza el rol sin
    /// privilegios con el que corre la API. Necesita la conexión del dueño de la base (en Docker, el contenedor
    /// ocap-migrate); la API con el rol limitado arranca con Database:ApplyMigrationsOnStartup=false.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
        if (!context.Database.IsRelational()) return;

        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        await RowLevelSecurity.ApplyPoliciesAsync(context, cancellationToken);

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var appRole = configuration["Database:AppRole"];
        if (!string.IsNullOrWhiteSpace(appRole))
        {
            await RowLevelSecurity.ProvisionAppRoleAsync(context, appRole, configuration["Database:AppPassword"] ?? string.Empty, cancellationToken);
        }
    }
}
