using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;

namespace OCAP.Infrastructure.Persistence.Context;

public class OCAPDbContextFactory : IDesignTimeDbContextFactory<OCAPDbContext>
{
    public OCAPDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OCAPDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=ocap_db;Username=postgres;Password=postgres",
            npgsql => npgsql.UseVector());

        return new OCAPDbContext(optionsBuilder.Options);
    }
}
