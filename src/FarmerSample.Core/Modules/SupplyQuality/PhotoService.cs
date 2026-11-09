using FarmerSample.Core.Modules.SupplyQuality.Contracts;
using FarmerSample.Core.Persistence;
using FarmerSample.Core.SharedKernel;
using FarmerSample.Core.Storage;
using Microsoft.EntityFrameworkCore;

namespace FarmerSample.Core.Modules.SupplyQuality;

public interface IPhotoService
{
    Task<IReadOnlyList<FarmPhotoDto>> ListAsync(Guid farmId, CancellationToken ct);
    Task<FarmPhotoDto> UploadAsync(Guid farmId, byte[] content, string fileName, string? caption, bool showsPeople, string uploadedBy, CancellationToken ct);
    Task<FarmPhotoDto> ReviewAsync(Guid photoId, PhotoReviewRequest review, string reviewer, CancellationToken ct);
    /// <summary>Staff preview: the rendition (never the original).</summary>
    Task<(Stream, string)?> GetStaffImageAsync(Guid photoId, CancellationToken ct);
    /// <summary>Public image: only buyer-safe photos of published farms with active photo consent.</summary>
    Task<(Stream, string)?> GetPublicImageAsync(Guid publicId, CancellationToken ct);
}

internal sealed class PhotoService(AppDbContext db, IBlobStore blobs) : IPhotoService
{
    public const int MaxBytes = 15 * 1024 * 1024;

    public async Task<IReadOnlyList<FarmPhotoDto>> ListAsync(Guid farmId, CancellationToken ct)
    {
        var farm = await LoadFarmAsync(farmId, ct);
        var photos = await db.FarmPhotos.AsNoTracking().Where(p => p.FarmId == farmId).OrderBy(p => p.UploadedAt).ToListAsync(ct);
        return photos.Select(p => ToDto(p, farm)).ToList();
    }

    public async Task<FarmPhotoDto> UploadAsync(Guid farmId, byte[] content, string fileName, string? caption, bool showsPeople, string uploadedBy, CancellationToken ct)
    {
        if (content.Length == 0) throw new DomainException("The file is empty.");
        if (content.Length > MaxBytes) throw new DomainException("Photos can be at most 15 MB.");
        var farm = await LoadFarmAsync(farmId, ct);

        ImageRenditions.Rendition rendition;
        try { rendition = ImageRenditions.Create(content); }
        catch (InvalidDataException e) { throw new DomainException(e.Message); }

        var photo = new FarmPhoto
        {
            FarmId = farmId, OriginalBlobName = $"{farmId}/{Guid.NewGuid()}{SafeExtension(fileName)}", RenditionBlobName = $"{farmId}/{Guid.NewGuid()}.webp",
            Width = rendition.Width, Height = rendition.Height, Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim()[..Math.Min(caption.Trim().Length, 300)],
            ShowsPeople = showsPeople, UploadedBy = uploadedBy,
        };
        await blobs.PutAsync(Containers.PhotoOriginals, photo.OriginalBlobName, new MemoryStream(content), "application/octet-stream", ct);
        await blobs.PutAsync(Containers.PhotoRenditions, photo.RenditionBlobName, new MemoryStream(rendition.WebP), "image/webp", ct);
        db.FarmPhotos.Add(photo);
        await db.SaveChangesAsync(ct);
        return ToDto(photo, farm);
    }

    public async Task<FarmPhotoDto> ReviewAsync(Guid photoId, PhotoReviewRequest r, string reviewer, CancellationToken ct)
    {
        var photo = await db.FarmPhotos.FirstOrDefaultAsync(p => p.Id == photoId, ct) ?? throw new DomainException("Unknown photo.");
        if (r.BuyerSafe && !r.CheckedNoIdentifyingDetails)
            throw new DomainException("Confirm that no names, signs, number plates or phone numbers are visible before marking the photo buyer-safe.");
        if (!r.BuyerSafe && string.IsNullOrWhiteSpace(r.Note)) throw new DomainException("Say why the photo is rejected.");
        photo.Status = r.BuyerSafe ? PhotoStatus.BuyerSafe : PhotoStatus.Rejected;
        photo.ReviewedBy = reviewer;
        photo.ReviewedAt = DateTimeOffset.UtcNow;
        photo.ReviewNote = string.IsNullOrWhiteSpace(r.Note) ? null : r.Note.Trim();
        await db.SaveChangesAsync(ct);
        return ToDto(photo, await LoadFarmAsync(photo.FarmId, ct));
    }

    public async Task<(Stream, string)?> GetStaffImageAsync(Guid photoId, CancellationToken ct)
    {
        var photo = await db.FarmPhotos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == photoId, ct);
        return photo is null ? null : await blobs.GetAsync(Containers.PhotoRenditions, photo.RenditionBlobName, ct);
    }

    public async Task<(Stream, string)?> GetPublicImageAsync(Guid publicId, CancellationToken ct)
    {
        var photo = await db.FarmPhotos.AsNoTracking().FirstOrDefaultAsync(p => p.PublicId == publicId && p.Status == PhotoStatus.BuyerSafe, ct);
        if (photo is null) return null;
        var farm = await LoadFarmAsync(photo.FarmId, ct);
        return PublicBlocker(photo, farm) is null ? await blobs.GetAsync(Containers.PhotoRenditions, photo.RenditionBlobName, ct) : null;
    }

    /// <summary>Why a photo is not shown to buyers, or null when it is.</summary>
    internal static string? PublicBlocker(FarmPhoto p, Farm farm)
    {
        if (p.Status == PhotoStatus.PendingReview) return "Waiting for review.";
        if (p.Status == PhotoStatus.Rejected) return "Rejected in review.";
        if (farm.Status != FarmStatus.Published) return "The farm is not published.";
        var consents = farm.Farmer!.Consents;
        if (!consents.Any(c => c.IsActiveFor(ConsentPurpose.PublicProfile))) return "No public-profile consent.";
        if (!consents.Any(c => c.IsActiveFor(ConsentPurpose.Photos))) return "No photo consent with a signed form.";
        return null;
    }

    private async Task<Farm> LoadFarmAsync(Guid farmId, CancellationToken ct) =>
        await db.Farms.AsNoTracking().Include(f => f.Farmer!).ThenInclude(f => f.Consents).FirstOrDefaultAsync(f => f.Id == farmId, ct)
        ?? throw new DomainException("Unknown farm.");

    private static string SafeExtension(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".webp" or ".heic" ? Path.GetExtension(fileName).ToLowerInvariant() : ".bin";

    private static FarmPhotoDto ToDto(FarmPhoto p, Farm farm)
    {
        var blocker = PublicBlocker(p, farm);
        return new FarmPhotoDto(p.Id, $"/api/v1/staff/photos/{p.Id}/image", p.Width, p.Height, p.Caption, p.ShowsPeople, p.Status.ToString(),
            p.UploadedBy, p.UploadedAt, p.ReviewedBy, p.ReviewNote, blocker is null, blocker);
    }
}
