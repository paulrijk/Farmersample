using FarmerSample.Core.Modules.SupplyQuality.Contracts;
using FarmerSample.Core.Persistence;
using FarmerSample.Core.SharedKernel;
using FarmerSample.Core.Storage;
using Microsoft.EntityFrameworkCore;

namespace FarmerSample.Core.Modules.SupplyQuality;

public interface ILotPhotoService
{
    Task<IReadOnlyList<FarmPhotoDto>> ListAsync(Guid lotId, CancellationToken ct);
    Task<FarmPhotoDto> UploadAsync(Guid lotId, byte[] content, string fileName, string? caption, string uploadedBy, CancellationToken ct);
    Task<FarmPhotoDto> ReviewAsync(Guid photoId, PhotoReviewRequest review, string reviewer, CancellationToken ct);
    Task<(Stream, string)?> GetStaffImageAsync(Guid photoId, CancellationToken ct);
    /// <summary>Public image: only buyer-safe photos of published lots.</summary>
    Task<(Stream, string)?> GetPublicImageAsync(Guid publicId, CancellationToken ct);
}

internal sealed class LotPhotoService(AppDbContext db, IBlobStore blobs) : ILotPhotoService
{
    public async Task<IReadOnlyList<FarmPhotoDto>> ListAsync(Guid lotId, CancellationToken ct)
    {
        var lot = await LoadLotAsync(lotId, ct);
        var photos = await db.LotPhotos.AsNoTracking().Where(p => p.LotId == lotId).OrderBy(p => p.UploadedAt).ToListAsync(ct);
        return photos.Select(p => ToDto(p, lot)).ToList();
    }

    public async Task<FarmPhotoDto> UploadAsync(Guid lotId, byte[] content, string fileName, string? caption, string uploadedBy, CancellationToken ct)
    {
        if (content.Length == 0) throw new DomainException("The file is empty.");
        if (content.Length > PhotoService.MaxBytes) throw new DomainException("Photos can be at most 15 MB.");
        var lot = await LoadLotAsync(lotId, ct);

        ImageRenditions.Rendition rendition;
        try { rendition = ImageRenditions.Create(content); }
        catch (InvalidDataException e) { throw new DomainException(e.Message); }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var photo = new LotPhoto
        {
            LotId = lotId, OriginalBlobName = $"lots/{lotId}/{Guid.NewGuid()}{(ext is ".jpg" or ".jpeg" or ".png" or ".webp" ? ext : ".bin")}",
            RenditionBlobName = $"lots/{lotId}/{Guid.NewGuid()}.webp", Width = rendition.Width, Height = rendition.Height,
            Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim()[..Math.Min(caption.Trim().Length, 300)], UploadedBy = uploadedBy,
        };
        await blobs.PutAsync(Containers.PhotoOriginals, photo.OriginalBlobName, new MemoryStream(content), "application/octet-stream", ct);
        await blobs.PutAsync(Containers.PhotoRenditions, photo.RenditionBlobName, new MemoryStream(rendition.WebP), "image/webp", ct);
        db.LotPhotos.Add(photo);
        await db.SaveChangesAsync(ct);
        return ToDto(photo, lot);
    }

    public async Task<FarmPhotoDto> ReviewAsync(Guid photoId, PhotoReviewRequest r, string reviewer, CancellationToken ct)
    {
        var photo = await db.LotPhotos.FirstOrDefaultAsync(p => p.Id == photoId, ct) ?? throw new DomainException("Unknown photo.");
        if (r.BuyerSafe && !r.CheckedNoIdentifyingDetails)
            throw new DomainException("Confirm that no people, names, signs, number plates or phone numbers are recognisable before marking the photo buyer-safe.");
        if (!r.BuyerSafe && string.IsNullOrWhiteSpace(r.Note)) throw new DomainException("Say why the photo is rejected.");
        photo.Status = r.BuyerSafe ? PhotoStatus.BuyerSafe : PhotoStatus.Rejected;
        photo.ReviewedBy = reviewer;
        photo.ReviewedAt = DateTimeOffset.UtcNow;
        photo.ReviewNote = string.IsNullOrWhiteSpace(r.Note) ? null : r.Note.Trim();
        await db.SaveChangesAsync(ct);
        return ToDto(photo, await LoadLotAsync(photo.LotId, ct));
    }

    public async Task<(Stream, string)?> GetStaffImageAsync(Guid photoId, CancellationToken ct)
    {
        var photo = await db.LotPhotos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == photoId, ct);
        return photo is null ? null : await blobs.GetAsync(Containers.PhotoRenditions, photo.RenditionBlobName, ct);
    }

    public async Task<(Stream, string)?> GetPublicImageAsync(Guid publicId, CancellationToken ct)
    {
        var photo = await db.LotPhotos.AsNoTracking().FirstOrDefaultAsync(p => p.PublicId == publicId && p.Status == PhotoStatus.BuyerSafe, ct);
        if (photo is null) return null;
        var published = await db.Lots.AnyAsync(l => l.Id == photo.LotId && l.Status == LotStatus.Published, ct);
        return published ? await blobs.GetAsync(Containers.PhotoRenditions, photo.RenditionBlobName, ct) : null;
    }

    private async Task<Lot> LoadLotAsync(Guid lotId, CancellationToken ct) =>
        await db.Lots.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lotId, ct) ?? throw new DomainException("Unknown lot.");

    private static FarmPhotoDto ToDto(LotPhoto p, Lot lot)
    {
        var blocker = p.Status == PhotoStatus.PendingReview ? "Waiting for review."
            : p.Status == PhotoStatus.Rejected ? "Rejected in review."
            : lot.Status != LotStatus.Published ? "The lot is not published." : null;
        return new FarmPhotoDto(p.Id, $"/api/v1/staff/lot-photos/{p.Id}/image", p.Width, p.Height, p.Caption, false, p.Status.ToString(),
            p.UploadedBy, p.UploadedAt, p.ReviewedBy, p.ReviewNote, blocker is null, blocker);
    }
}
