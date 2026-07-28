using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Core.Ports;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Infrastructure.Persistence.Repositories;

namespace OCAP.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var useInMemory = string.Equals(configuration["UseInMemory"], "true", StringComparison.OrdinalIgnoreCase);

        // Audit Trail Interceptor
        services.AddSingleton(sp => new OCAP.Infrastructure.Persistence.Interceptors.AuditSaveChangesInterceptor(sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()));

        if (useInMemory)
        {
            var dbName = configuration["InMemoryDbName"] ?? "OCAP_InMemory_Db";
            services.AddDbContext<OCAPDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<OCAP.Infrastructure.Persistence.Interceptors.AuditSaveChangesInterceptor>();
                options.UseInMemoryDatabase(dbName).AddInterceptors(interceptor);
            });
        }
        else
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<OCAPDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<OCAP.Infrastructure.Persistence.Interceptors.AuditSaveChangesInterceptor>();
                options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(OCAPDbContext).Assembly.FullName))
                       .AddInterceptors(interceptor);
            });
        }

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<SessionRepository>(); // Registered directly as no port currently exists in Core

        // Caching Foundation
        services.AddDistributedMemoryCache();
        services.AddSingleton<OCAP.Core.Caching.ICacheService, OCAP.Infrastructure.Caching.DistributedCacheService>();

        // Background Jobs Foundation
        services.AddSingleton<OCAP.Infrastructure.BackgroundJobs.IBackgroundTaskQueue>(ctx => new OCAP.Infrastructure.BackgroundJobs.BackgroundTaskQueue(100));
        services.AddHostedService<OCAP.Infrastructure.BackgroundJobs.BackgroundWorkerService>();
        services.AddHostedService<OCAP.Infrastructure.BackgroundJobs.OutboxProcessorBackgroundService>();

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
