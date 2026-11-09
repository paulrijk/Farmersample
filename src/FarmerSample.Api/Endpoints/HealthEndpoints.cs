using FarmerSample.Core.Persistence;

namespace FarmerSample.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/health", async (AppDbContext db, CancellationToken ct) =>
            await db.Database.CanConnectAsync(ct)
                ? Results.Ok(new { status = "ok" })
                : Results.Json(new { status = "database_unreachable" }, statusCode: 503))
            .AllowAnonymous().ExcludeFromDescription();
    }
}
