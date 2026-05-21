using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CampRegistrationApp.Data;

namespace CampRegistrationApp.SeleniumTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DbName { get; } = $"TestDb_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("DATABASE_SKIP", "true");
        builder.UseEnvironment("Development");
        builder.UseUrls("http://127.0.0.1:0");

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(DbName));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
            TestDataSeeder.Seed(db);
        });
    }

    public string GetServerUrl()
    {
        var server = Server;
        var addresses = server.Features.Get<IServerAddressesFeature>();
        return addresses!.Addresses.First();
    }
}
