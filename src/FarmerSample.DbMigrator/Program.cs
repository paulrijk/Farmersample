using FarmerSample.Core;
using FarmerSample.Core.Modules.Identity;
using FarmerSample.Core.Persistence;
using FarmerSample.Core.Persistence.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Applies all migrations, then seeds reference data (always) and fictional demo data (only when Seed:DemoData=true).
// Content root = the build output, so settings are found whichever folder the command is started from.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = args, ContentRootPath = AppContext.BaseDirectory });
builder.Services.AddFarmerSampleCore(builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured."));
builder.Services.AddIdentityCore<AppUser>(o => { o.User.RequireUniqueEmail = true; o.Password.RequiredLength = 12; o.Password.RequireNonAlphanumeric = false; })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddScoped<Seeder>();

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();
var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbMigrator");
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

for (var attempt = 1; ; attempt++)
{
    try { await db.Database.MigrateAsync(); break; }
    catch (Exception e) when (attempt < 10)
    {
        log.LogWarning("Database not ready ({Message}); retry {Attempt}/10", e.Message, attempt);
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}
log.LogInformation("Migrations applied");

await scope.ServiceProvider.GetRequiredService<Seeder>().SeedAsync(builder.Configuration.GetValue("Seed:DemoData", false));
log.LogInformation("Seed complete");
