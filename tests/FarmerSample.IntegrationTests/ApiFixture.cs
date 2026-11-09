using FarmerSample.Core.Modules.SupplyQuality;
using FarmerSample.Core.Persistence;
using FarmerSample.Core.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FarmerSample.IntegrationTests;

/// <summary>Real PostgreSQL in a container + the real API, seeded with reference data and privacy canaries.</summary>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Markers that must NEVER appear in any public or buyer response.
    public const string CanaryPhone = "+94 77 CANARY 0001";
    public const string CanaryLegalName = "CANARY Plantations (Pvt) Ltd";
    public const double CanaryLatitude = 7.123456789;

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:17").Build();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        var seeder = ActivatorUtilities.CreateInstance<FarmerSample.Core.Persistence.Seeding.Seeder>(scope.ServiceProvider);
        await seeder.SeedAsync(demoData: false);

        var region = await db.Regions.FirstAsync();
        var farmer = new Farmer { PublicName = "Canary Farmer", LegalName = CanaryLegalName, PhoneInternal = CanaryPhone };
        farmer.Consents.Add(new FarmerConsent
        {
            Purposes = ConsentPurpose.PublicProfile | ConsentPurpose.FamilyStory, Language = "en", ConsentTextVersion = "test",
            Evidence = ConsentEvidence.SignedFormPhoto, ObtainedBy = "test", ObtainedAt = DateTimeOffset.UtcNow,
        });
        db.Farms.Add(new Farm
        {
            PublicCode = PublicCode.New("FM"), Farmer = farmer, Name = "Canary Farm", RegionId = region.Id, Status = FarmStatus.Published,
            Story = "Public story", LatitudeInternal = CanaryLatitude, LongitudeInternal = CanaryLatitude,
            Products = [new FarmProduct { Product = "Black pepper", Grade = "550 g/L", HarvestMonths = [12] }],
        });
        db.Farms.Add(new Farm
        {
            PublicCode = PublicCode.New("FM"), Farmer = new Farmer { PublicName = "No Consent Farmer" }, Name = "Hidden Farm",
            RegionId = region.Id, Status = FarmStatus.Published,
        });
        await db.SaveChangesAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _db.GetConnectionString());
        builder.UseSetting("Auth:RequireStaffMfa", "true");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }
}
