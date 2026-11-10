using System.Security.Claims;
using FarmerSample.Core.Modules.Identity;
using FarmerSample.Core.Modules.Identity.Contracts;
using FarmerSample.Core.SharedKernel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FarmerSample.Api.Endpoints;

/// <summary>Staff account management. Admins only (and, outside local development, only with two-factor sign-in).</summary>
public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/users")
            .RequireAuthorization(Policies.Admin)
            .AddEndpointFilter<AntiforgeryFilter>()
            .AddEndpointFilter<DomainExceptionFilter>()
            .WithTags("Admin");

        admin.MapGet("", async (UserManager<AppUser> users, CancellationToken ct) =>
        {
            var result = new List<StaffUserDto>();
            foreach (var u in await users.Users.OrderBy(u => u.Email).ToListAsync(ct))
            {
                var roles = (await users.GetRolesAsync(u)).Where(Roles.StaffRoles.Contains).ToArray();
                if (roles.Length > 0) result.Add(ToDto(u, roles));
            }
            return result;
        });

        admin.MapPost("", async (CreateStaffUserRequest r, UserManager<AppUser> users) =>
        {
            if (!Roles.StaffRoles.Contains(r.Role)) throw new DomainException("Choose a role: Admin, Staff, QualityController or FieldAgent.");
            if (string.IsNullOrWhiteSpace(r.DisplayName)) throw new DomainException("Name is required.");
            var email = r.Email?.Trim() ?? "";
            var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = r.DisplayName.Trim() };
            Ensure(await users.CreateAsync(user, r.Password));
            Ensure(await users.AddToRoleAsync(user, r.Role));
            return ToDto(user, [r.Role]);
        });

        // Sets a new temporary password (e.g. forgotten password). Existing sessions of that user end.
        admin.MapPost("/{id:guid}/password", async (Guid id, SetPasswordRequest r, UserManager<AppUser> users) =>
        {
            var user = await Find(users, id);
            Ensure(await users.ResetPasswordAsync(user, await users.GeneratePasswordResetTokenAsync(user), r.Password));
            await users.UpdateSecurityStampAsync(user);
            return Results.NoContent();
        });

        // Lost phone: the user sets up two-factor sign-in again at the next sign-in.
        admin.MapPost("/{id:guid}/reset-mfa", async (Guid id, UserManager<AppUser> users) =>
        {
            var user = await Find(users, id);
            Ensure(await users.SetTwoFactorEnabledAsync(user, false));
            Ensure(await users.ResetAuthenticatorKeyAsync(user));
            await users.UpdateSecurityStampAsync(user);
            return Results.NoContent();
        });

        admin.MapPost("/{id:guid}/disable", async (Guid id, ClaimsPrincipal me, UserManager<AppUser> users) =>
        {
            var user = await Find(users, id);
            if (users.GetUserId(me) == user.Id.ToString()) throw new DomainException("You cannot disable your own account.");
            Ensure(await users.SetLockoutEnabledAsync(user, true));
            Ensure(await users.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue));
            await users.UpdateSecurityStampAsync(user);
            return Results.NoContent();
        });

        admin.MapPost("/{id:guid}/enable", async (Guid id, UserManager<AppUser> users) =>
        {
            var user = await Find(users, id);
            Ensure(await users.SetLockoutEndDateAsync(user, null));
            Ensure(await users.ResetAccessFailedCountAsync(user));
            return Results.NoContent();
        });
    }

    private static async Task<AppUser> Find(UserManager<AppUser> users, Guid id) =>
        await users.FindByIdAsync(id.ToString()) ?? throw new DomainException("Unknown user.");

    internal static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new DomainException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    private static StaffUserDto ToDto(AppUser u, string[] roles) =>
        new(u.Id, u.Email!, u.DisplayName, roles, u.TwoFactorEnabled, u.LockoutEnd is { } end && end > DateTimeOffset.UtcNow.AddYears(1));
}
