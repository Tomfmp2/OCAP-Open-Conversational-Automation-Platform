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

        // Fallback tenant context for non-HTTP hosts; API replaces with HttpTenantContext.
        if (!services.Any(d => d.ServiceType == typeof(ITenantContext)))
        {
            services.AddScoped<ITenantContext>(_ => SystemTenantContext.Instance);
        }

        // Audit Trail Interceptor
        services.AddSingleton(sp => new AuditSaveChangesInterceptor(sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()));
        services.AddScoped<TenantSaveChangesInterceptor>();

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
                options.UseNpgsql(connectionString, b =>
                       {
                           b.MigrationsAssembly(typeof(OCAPDbContext).Assembly.FullName);
                           b.UseVector();
                       })
                       .AddInterceptors(audit, tenant);
            });
        }

        // Allow Knowledge EF repositories / PgVector to resolve the abstract DbContext.
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<OCAPDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<SessionRepository>(); // Registered directly as no port currently exists in Core
        services.AddScoped<OCAP.Agents.Abstractions.Ports.IAgentRepository, AgentRepository>();
        services.AddScoped<IToolExecutionRepository, ToolExecutionRepository>();
        services.AddScoped<IAiExecutionLogRepository, AiExecutionLogRepository>();
        services.AddScoped<IAiConversationMemoryRepository, AiConversationMemoryRepository>();

        // Caching Foundation
        services.AddDistributedMemoryCache();
        services.AddSingleton<OCAP.Core.Caching.ICacheService, OCAP.Infrastructure.Caching.DistributedCacheService>();

        // Retention & Maintenance Options
        services.Configure<OCAP.Infrastructure.Options.RetentionOptions>(configuration.GetSection(OCAP.Infrastructure.Options.RetentionOptions.SectionName));

        // Background Jobs Foundation
        services.AddSingleton<OCAP.Infrastructure.BackgroundJobs.IBackgroundTaskQueue>(ctx => new OCAP.Infrastructure.BackgroundJobs.BackgroundTaskQueue(100));
        services.AddHostedService<OCAP.Infrastructure.BackgroundJobs.BackgroundWorkerService>();
        services.AddHostedService<OCAP.Infrastructure.BackgroundJobs.OutboxProcessorBackgroundService>();
        services.AddHostedService<OCAP.Infrastructure.BackgroundJobs.AuditAndOutboxRetentionBackgroundService>();

        // Real-Time Distributed Event Bus Foundation (CAP-20)
        services.AddSingleton<OCAP.Core.Events.Distributed.IEventSerializer, OCAP.Infrastructure.Events.Distributed.JsonEventSerializer>();
        services.AddSingleton<OCAP.Core.Events.Distributed.IEventTransport, OCAP.Infrastructure.Events.Distributed.InMemoryEventTransport>();
        services.AddScoped<OCAP.Core.Events.Distributed.IOutboxStore, OCAP.Infrastructure.Events.Distributed.EfOutboxStore>();
        services.AddScoped<OCAP.Core.Events.Distributed.IInboxStore, OCAP.Infrastructure.Events.Distributed.EfInboxStore>();
        services.AddScoped<OCAP.Core.Events.Distributed.IMessageDeadLetterHandler, OCAP.Infrastructure.Events.Distributed.MessageDeadLetterHandler>();
        services.AddSingleton<OCAP.Core.Events.IEventBus, OCAP.Infrastructure.Events.Distributed.DistributedEventBus>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
        
        // Auto Migration Strategy
        if (context.Database.IsRelational())
        {
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await context.Database.MigrateAsync();
            }
        }
    }
}
