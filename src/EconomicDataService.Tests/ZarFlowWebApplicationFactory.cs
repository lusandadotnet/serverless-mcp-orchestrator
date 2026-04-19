using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EconomicDataService.Tests;

/// <summary>
/// Ensures ASP.NET Core sees Environment=Testing and a unique in-memory database name
/// before <see cref="Program"/> configures Entity Framework.
/// </summary>
internal sealed class ZarFlowWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _inMemoryDatabaseName = Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Testing:InMemoryDatabaseName"] = _inMemoryDatabaseName
            });
        });
    }
}
