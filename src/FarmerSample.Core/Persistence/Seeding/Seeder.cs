using FarmerSample.Core.Modules.Admin;
using FarmerSample.Core.Modules.Identity;
using FarmerSample.Core.Modules.Identity.Contracts;
using FarmerSample.Core.Modules.SupplyQuality;
using FarmerSample.Core.ReferenceData;
using FarmerSample.Core.SharedKernel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FarmerSample.Core.Persistence.Seeding;

/// <summary>Idempotent: safe to run on every start.</summary>
/// <summary>Reference data always; fictional demo data only when asked.</summary>
public sealed class Seeder(AppDbContext db, UserManager<AppUser> users, RoleManager<IdentityRole<Guid>> roles, IConfiguration config)
{
    public async Task SeedAsync(bool demoData)
    {
        await SeedRolesAsync();
        await SeedLegalEntitiesAsync();
        await SeedRegionsAsync();
        if (!demoData) return;
        await SeedDemoUsersAsync();
        await SeedDemoFarmsAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles.All)
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole<Guid>(role));
    }

    private async Task SeedLegalEntitiesAsync()
    {
        if (!await db.LegalEntities.AnyAsync(x => x.Code == "VSR-ES"))
            db.LegalEntities.Add(new LegalEntity { Code = "VSR-ES", Name = "VSR Partners SL", CountryCode = "ES", Role = LegalEntityRole.EuropeanSeller });
        if (!await db.LegalEntities.AnyAsync(x => x.Code == "SAJ-LK"))
            db.LegalEntities.Add(new LegalEntity { Code = "SAJ-LK", Name = "Saj Company", CountryCode = "LK", Role = LegalEntityRole.SourcingAndExport });
        await db.SaveChangesAsync();
    }

    private async Task SeedRegionsAsync()
    {
        // Pepper regions as named in the business plan (to be confirmed with Saj).
        string[] names = ["Gampola / Kandy", "Bibile", "Middeniya", "Maspanna"];
        foreach (var name in names)
            if (!await db.Regions.AnyAsync(r => r.CountryCode == "LK" && r.Name == name))
                db.Regions.Add(new Region { CountryCode = "LK", Name = name });
        await db.SaveChangesAsync();
    }

    private async Task SeedDemoUsersAsync()
    {
        var password = config["Seed:DemoPassword"] ?? "FarmerSample-local-1";
        await EnsureUserAsync("admin@farmersample.local", "Demo Admin", password, Roles.Admin, Roles.Staff);
        await EnsureUserAsync("qc@farmersample.local", "Demo Quality Controller", password, Roles.QualityController);
        await EnsureUserAsync("field@farmersample.local", "Demo Field Agent", password, Roles.FieldAgent);
        Console.WriteLine();
        Console.WriteLine("Local demo logins (fictional, local only):");
        Console.WriteLine("  admin@farmersample.local / " + password);
        Console.WriteLine("  qc@farmersample.local    / " + password);
        Console.WriteLine("  field@farmersample.local / " + password);
        Console.WriteLine();
    }

    private async Task EnsureUserAsync(string email, string name, string password, params string[] userRoles)
    {
        if (await users.FindByEmailAsync(email) is not null) return;
        var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = name };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        await users.AddToRolesAsync(user, userRoles);
    }

    /// <summary>FICTIONAL farms for demos and tests. Never put real farmer data in the repository.</summary>
    private async Task SeedDemoFarmsAsync()
    {
        if (await db.Farms.AnyAsync()) return;
        var regions = await db.Regions.ToDictionaryAsync(r => r.Name, r => r.Id);

        Farm Make(string farmer, string farm, string region, decimal ha, string story, FarmStatus status, ConsentPurpose consent, params FarmProduct[] products)
        {
            var f = new Farmer { PublicName = farmer, PreferredLanguage = "si", PhoneInternal = "+94 70 000 0000", LegalName = farmer + " (Pvt) Ltd (demo)" };
            if (consent != ConsentPurpose.None)
                f.Consents.Add(new FarmerConsent
                {
                    Purposes = consent, Language = "si", ConsentTextVersion = "demo-2026-11", Evidence = ConsentEvidence.SignedFormPhoto,
                    ObtainedBy = "Demo Field Agent", ObtainedAt = DateTimeOffset.UtcNow,
                });
            return new Farm
            {
                PublicCode = PublicCode.New(PublicCode.FarmPrefix), Farmer = f, Name = farm, RegionId = regions[region], Hectares = ha, Story = story,
                Status = status, PublishedAt = status == FarmStatus.Published ? DateTimeOffset.UtcNow : null,
                LatitudeInternal = 7.25, LongitudeInternal = 80.6, Products = products.ToList(),
            };
        }

        var all = ConsentPurpose.PublicProfile | ConsentPurpose.Photos | ConsentPurpose.FamilyStory;
        db.Farms.AddRange(
            Make("Nimal P.", "Hillside Pepper Garden", "Gampola / Kandy", 2.5m,
                "A family garden on the slopes above Gampola, picking pepper by hand from December.", FarmStatus.Published, all,
                new FarmProduct { Product = "Black pepper", Grade = "550 g/L", HarvestMonths = [12, 1, 2], EstimatedVolumeKg = 1800 }),
            Make("Kumari S.", "Riverside Spice Estate", "Bibile", 6m,
                "Mixed pepper and cinnamon estate near Bibile; the main crop comes in May.", FarmStatus.Published, all,
                new FarmProduct { Product = "Black pepper", Grade = "550 g/L", HarvestMonths = [5, 6], EstimatedVolumeKg = 4200 },
                new FarmProduct { Product = "Ceylon cinnamon", Grade = "C5", HarvestMonths = [1, 2, 3, 4, 5, 8, 9, 10, 11, 12] }),
            Make("Sunil W.", "Southern Cinnamon Plot", "Middeniya", 1.2m,
                "Smallholding waiting for consent before going public.", FarmStatus.NeedsReview, ConsentPurpose.None,
                new FarmProduct { Product = "Ceylon cinnamon", Grade = "Alba", HarvestMonths = [3, 4, 9, 10] }));
        db.Farms.Local.First(f => f.Name == "Hillside Pepper Garden").Certifications.Add(new Certification { Scheme = "EU organic" });
        await db.SaveChangesAsync();
    }
}
