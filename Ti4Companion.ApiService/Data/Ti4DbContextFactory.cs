using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ti4Companion.ApiService.Data;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> can build the model. Uses the same SQLite
/// provider as the app; the file here is only used for SQL generation.
/// </summary>
public class Ti4DbContextFactory : IDesignTimeDbContextFactory<Ti4DbContext>
{
    public Ti4DbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<Ti4DbContext>()
            .UseSqlite("Data Source=ti4.db")
            .Options;
        return new Ti4DbContext(options);
    }
}
