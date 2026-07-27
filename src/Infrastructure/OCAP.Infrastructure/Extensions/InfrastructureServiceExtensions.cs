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

        if (useInMemory)
        {
            var dbName = configuration["InMemoryDbName"] ?? "OCAP_InMemory_Db";
            services.AddDbContext<OCAPDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        }
        else
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<OCAPDbContext>(options =>
                options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(OCAPDbContext).Assembly.FullName)));
        }

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<SessionRepository>(); // Registered directly as no port currently exists in Core

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
