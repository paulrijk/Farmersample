using FarmerSample.Core.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;

namespace FarmerSample.Api;

public static class Policies
{
    public const string Staff = "staff";
    public const string Admin = "admin";

    /// <summary>Staff must have signed in with a second factor ("amr" = "mfa") unless MFA is switched off for local development.</summary>
    public static void ConfigureStaff(AuthorizationPolicyBuilder p, bool requireMfa)
    {
        p.RequireAuthenticatedUser().RequireRole(Roles.StaffRoles);
        if (requireMfa) p.RequireClaim("amr", "mfa");
    }

    public static void ConfigureAdmin(AuthorizationPolicyBuilder p, bool requireMfa)
    {
        p.RequireAuthenticatedUser().RequireRole(Roles.Admin);
        if (requireMfa) p.RequireClaim("amr", "mfa");
    }
}

public static class RateLimits
{
    public const string Public = "public";
    public const string Sensitive = "sensitive";
}
