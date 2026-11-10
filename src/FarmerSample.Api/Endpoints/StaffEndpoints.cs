using FarmerSample.Core.ReferenceData;
using FarmerSample.Core.Modules.Admin.Contracts;
using FarmerSample.Core.Modules.Catalogue;
using FarmerSample.Core.Modules.SupplyQuality;
using FarmerSample.Core.Modules.SupplyQuality.Contracts;
using FarmerSample.Core.Persistence;
using FarmerSample.Core.SharedKernel;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace FarmerSample.Api.Endpoints;

public static class StaffEndpoints
{
    public static void MapStaffEndpoints(this WebApplication app)
    {
        var staff = app.MapGroup("/api/v1/staff")
            .RequireAuthorization(Policies.Staff)
            .AddEndpointFilter<AntiforgeryFilter>()
            .AddEndpointFilter<DomainExceptionFilter>()
            .WithTags("Staff");

        // Base URL that QR codes and printed labels point to (configurable for tunnels and test environments).
        staff.MapGet("/settings", (IConfiguration config) =>
            new { publicBaseUrl = (config["PublicBaseUrl"] ?? "http://localhost:8080").TrimEnd('/') });

        staff.MapGet("/legal-entities", async (AppDbContext db, CancellationToken ct) =>
            await db.LegalEntities.AsNoTracking().OrderBy(x => x.Name)
                .Select(x => new LegalEntityDto(x.Id, x.Code, x.Name, x.CountryCode, x.Role.ToString())).ToListAsync(ct));

        staff.MapGet("/regions", async (AppDbContext db, CancellationToken ct) =>
            await db.Regions.AsNoTracking().OrderBy(x => x.CountryCode).ThenBy(x => x.Name)
                .Select(x => new RegionDto(x.Id, x.CountryCode, x.Name)).ToListAsync(ct));

        staff.MapGet("/farms", (IFarmService farms, CancellationToken ct) => farms.ListAsync(ct));

        staff.MapPost("/farms", async (CreateFarmRequest r, IFarmService farms, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(r.FarmName) || string.IsNullOrWhiteSpace(r.FarmerPublicName))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["farm"] = ["Farm name and farmer name are required."] });
            try { return Results.Ok(await farms.CreateAsync(r, ct)); }
            catch (ArgumentException e) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["regionId"] = [e.Message] }); }
        });

        staff.MapPost("/farms/{id:guid}/consents", async (Guid id, RecordConsentRequest r, IFarmService farms, CancellationToken ct) =>
        {
            try { return await farms.RecordConsentAsync(id, r, ct) ? Results.NoContent() : Results.NotFound(); }
            catch (ArgumentException e) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["consent"] = [e.Message] }); }
        });

        staff.MapPost("/farms/{id:guid}/publish", async (Guid id, IFarmService farms, CancellationToken ct) =>
        {
            var (ok, error) = await farms.PublishAsync(id, ct);
            return ok ? Results.NoContent()
                : error == "not_found" ? Results.NotFound()
                : Results.Problem(error, statusCode: StatusCodes.Status409Conflict);
        });

        staff.MapPost("/farms/{id:guid}/unpublish", async (Guid id, IFarmService farms, CancellationToken ct) =>
            await farms.UnpublishAsync(id, ct) ? Results.NoContent() : Results.NotFound());

        // ---- farm photos ----
        staff.MapGet("/farms/{id:guid}/photos", (Guid id, IPhotoService photos, CancellationToken ct) => photos.ListAsync(id, ct));

        staff.MapPost("/farms/{id:guid}/photos", async (Guid id, HttpRequest request, IPhotoService photos, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest();
            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null) return Results.Problem("Choose a photo to upload.", statusCode: 422);
            if (file.Length > 15 * 1024 * 1024) return Results.Problem("Photos can be at most 15 MB.", statusCode: 422);
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);
            return Results.Ok(await photos.UploadAsync(id, buffer.ToArray(), file.FileName, form["caption"], form["showsPeople"] == "true", Who(user), ct));
        }).DisableAntiforgery();   // our AntiforgeryFilter (header token) still runs for this group

        staff.MapPost("/photos/{id:guid}/review", (Guid id, PhotoReviewRequest r, IPhotoService photos, ClaimsPrincipal user, CancellationToken ct) =>
            photos.ReviewAsync(id, r, Who(user), ct));

        staff.MapGet("/photos/{id:guid}/image", async (Guid id, IPhotoService photos, CancellationToken ct) =>
            await photos.GetStaffImageAsync(id, ct) is var (stream, type) ? Results.Stream(stream, type) : Results.NotFound());

        // ---- lot photos (the product itself) ----
        staff.MapGet("/lots/{id:guid}/photos", (Guid id, ILotPhotoService photos, CancellationToken ct) => photos.ListAsync(id, ct));

        staff.MapPost("/lots/{id:guid}/photos", async (Guid id, HttpRequest request, ILotPhotoService photos, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest();
            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null) return Results.Problem("Choose a photo to upload.", statusCode: 422);
            if (file.Length > 15 * 1024 * 1024) return Results.Problem("Photos can be at most 15 MB.", statusCode: 422);
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);
            return Results.Ok(await photos.UploadAsync(id, buffer.ToArray(), file.FileName, form["caption"], Who(user), ct));
        }).DisableAntiforgery();

        staff.MapPost("/lot-photos/{id:guid}/review", (Guid id, PhotoReviewRequest r, ILotPhotoService photos, ClaimsPrincipal user, CancellationToken ct) =>
            photos.ReviewAsync(id, r, Who(user), ct));

        staff.MapGet("/lot-photos/{id:guid}/image", async (Guid id, ILotPhotoService photos, CancellationToken ct) =>
            await photos.GetStaffImageAsync(id, ct) is var (stream, type) ? Results.Stream(stream, type) : Results.NotFound());

        // ---- sample batches and grading ----
        staff.MapGet("/sample-batches", (IQualityService q, CancellationToken ct) => q.ListBatchesAsync(ct));
        staff.MapPost("/sample-batches", (CreateSampleBatchRequest r, IQualityService q, CancellationToken ct) => q.CreateBatchAsync(r, ct));
        staff.MapPost("/sample-batches/{id:guid}/gradings", (Guid id, GradingInput r, IQualityService q, CancellationToken ct) => q.GradeBatchAsync(id, r, ct));

        // ---- lots ----
        staff.MapGet("/lots", (ILotService lots, CancellationToken ct) => lots.ListAsync(ct));
        staff.MapGet("/lots/{id:guid}", (Guid id, ILotService lots, CancellationToken ct) => lots.GetAsync(id, ct));
        staff.MapPost("/lots", (CreateLotRequest r, ILotService lots, CancellationToken ct) => lots.CreateAsync(r, ct));
        staff.MapPost("/lots/{id:guid}/composite-gradings", (Guid id, GradingInput r, ILotService lots, CancellationToken ct) => lots.GradeCompositeAsync(id, r, ct));
        staff.MapPost("/lots/{id:guid}/publish", (Guid id, ILotService lots, CancellationToken ct) => lots.PublishAsync(id, ct));
        staff.MapPost("/lots/{id:guid}/withdraw", (Guid id, ILotService lots, CancellationToken ct) => lots.WithdrawAsync(id, ct));

        staff.MapGet("/sample-requests", (ISampleRequestService s, CancellationToken ct) => s.ListAsync(ct));
    }

    private static string Who(ClaimsPrincipal user) => user.Identity?.Name ?? "unknown";
}
