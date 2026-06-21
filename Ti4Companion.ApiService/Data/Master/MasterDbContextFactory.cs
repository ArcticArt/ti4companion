using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ti4Companion.ApiService.Data;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations add &lt;Name&gt; --context MasterDbContext</c> can build
/// the master-DB model against SQLite (same provider as the app). The file here is only used for SQL
/// generation. Because the project now has two contexts, <c>dotnet ef</c> commands must pass
/// <c>--context</c> to disambiguate.
/// </summary>
public class MasterDbContextFactory : IDesignTimeDbContextFactory<MasterDbContext>
{
    public MasterDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlite("Data Source=ti4master.db")
            .Options;
        return new MasterDbContext(options);
    }
}
