using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ti4Companion.ApiService.Data;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> can build the model without a running
/// database or an Aspire-provided connection string. The connection string here is a dummy used
/// only for SQL generation; the real one is injected by Aspire at runtime.
/// </summary>
public class Ti4DbContextFactory : IDesignTimeDbContextFactory<Ti4DbContext>
{
    public Ti4DbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<Ti4DbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=ti4db;Username=postgres;Password=***")
            .Options;
        return new Ti4DbContext(options);
    }
}
