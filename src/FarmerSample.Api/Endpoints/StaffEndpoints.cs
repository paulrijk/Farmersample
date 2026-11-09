using FarmerSample.Core.ReferenceData;
using FarmerSample.Core.Modules.Admin.Contracts;
using FarmerSample.Core.Modules.Catalogue;
using FarmerSample.Core.Modules.SupplyQuality;
using FarmerSample.Core.Modules.SupplyQuality.Contracts;
using FarmerSample.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmerSample.Api.Endpoints;

public static class StaffEndpoints
{
    public static void MapStaffEndpoints(this WebApplication app)
    {
        var staff = app.MapGroup("/api/v1/staff")
            .RequireAuthorization(Policies.Staff)
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithTags("Staff");

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

        staff.MapGet("/sample-requests", (ISampleRequestService s, CancellationToken ct) => s.ListAsync(ct));
    }
}
