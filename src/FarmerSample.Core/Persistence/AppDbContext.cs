using FarmerSample.Core.ReferenceData;
using FarmerSample.Core.Modules.Admin;
using FarmerSample.Core.Modules.Catalogue;
using FarmerSample.Core.Modules.Identity;
using FarmerSample.Core.Modules.SupplyQuality;
using FarmerSample.Core.Outbox;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FarmerSample.Core.Persistence;

/// <summary>One DbContext, one PostgreSQL schema per module (architecture D1).</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<LegalEntity> LegalEntities => Set<LegalEntity>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Farmer> Farmers => Set<Farmer>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<FarmerConsent> FarmerConsents => Set<FarmerConsent>();
    public DbSet<SampleRequest> SampleRequests => Set<SampleRequest>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Identity tables → "identity" schema
        foreach (var entity in b.Model.GetEntityTypes().Where(e => e.ClrType.Namespace?.StartsWith("Microsoft.AspNetCore.Identity") == true || e.ClrType == typeof(AppUser)))
            entity.SetSchema("identity");
        b.Entity<AppUser>().Property(u => u.DisplayName).HasMaxLength(200);

        // Admin
        b.Entity<LegalEntity>(e =>
        {
            e.ToTable("legal_entities", "admin");
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(40);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.CountryCode).HasMaxLength(2);
        });
        b.Entity<Region>(e =>
        {
            e.ToTable("regions", "admin");
            e.HasIndex(x => new { x.CountryCode, x.Name }).IsUnique();
            e.Property(x => x.CountryCode).HasMaxLength(2);
            e.Property(x => x.Name).HasMaxLength(100);
        });

        // Supply & Quality
        b.Entity<Farmer>(e =>
        {
            e.ToTable("farmers", "supply");
            e.Property(x => x.PublicName).HasMaxLength(200);
            e.Property(x => x.LegalName).HasMaxLength(200);
            e.Property(x => x.PhoneInternal).HasMaxLength(40);
            e.HasMany(x => x.Farms).WithOne(x => x.Farmer).HasForeignKey(x => x.FarmerId);
            e.HasMany(x => x.Consents).WithOne().HasForeignKey(x => x.FarmerId);
        });
        b.Entity<Farm>(e =>
        {
            e.ToTable("farms", "supply");
            e.HasIndex(x => x.PublicCode).IsUnique();
            e.Property(x => x.PublicCode).HasMaxLength(12);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Hectares).HasPrecision(9, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne<Region>().WithMany().HasForeignKey(x => x.RegionId);
            e.HasMany(x => x.Products).WithOne().HasForeignKey(x => x.FarmId);
            e.HasMany(x => x.Certifications).WithOne().HasForeignKey(x => x.FarmId);
            e.Property<uint>("xmin").IsRowVersion();   // optimistic concurrency (PostgreSQL xmin)
        });
        b.Entity<FarmProduct>(e => e.ToTable("farm_products", "supply"));
        b.Entity<Certification>(e => e.ToTable("certifications", "supply"));
        b.Entity<FarmerConsent>(e =>
        {
            e.ToTable("farmer_consents", "supply");
            e.Property(x => x.Purposes).HasConversion<int>();
            e.Property(x => x.Evidence).HasConversion<string>().HasMaxLength(30);
        });

        // Catalogue
        b.Entity<SampleRequest>(e =>
        {
            e.ToTable("sample_requests", "catalogue");
            e.HasIndex(x => x.CreatedAt);
        });

        // Infrastructure
        b.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages", "infra");
            e.Property(x => x.Payload).HasColumnType("jsonb");
            e.HasIndex(x => x.ProcessedAt).HasFilter("\"ProcessedAt\" IS NULL");
        });
    }
}
