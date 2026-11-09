using FarmerSample.Core.Modules.Catalogue;
using FarmerSample.Core.Modules.SupplyQuality;
using FarmerSample.Core.Modules.SupplyQuality.Contracts;
using FarmerSample.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FarmerSample.Core;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddFarmerSampleCore(this IServiceCollection services, string connectionString, string? blobConnectionString = null)
    {
        // Azurite's well-known local development account when nothing is configured.
        services.AddSingleton(new Azure.Storage.Blobs.BlobServiceClient(blobConnectionString ?? "UseDevelopmentStorage=true"));
        services.AddSingleton<FarmerSample.Core.Storage.IBlobStore, FarmerSample.Core.Storage.AzureBlobStore>();
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IPublicFarmReader, PublicFarmReader>();
        services.AddScoped<IFarmService, FarmService>();
        services.AddScoped<IQualityService, QualityService>();
        services.AddScoped<ILotService, LotService>();
        services.AddScoped<IPublicLotReader, PublicLotReader>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ISampleRequestService, SampleRequestService>();
        return services;
    }
}
