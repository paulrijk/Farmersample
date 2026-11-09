using System.Security.Claims;
using System.Text.Encodings.Web;
using FarmerSample.Core.Modules.Identity;
using FarmerSample.Core.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

namespace FarmerSample.Api.Endpoints;

public sealed record LoginRequest(string Email, string Password);
public sealed record MfaCodeRequest(string Code);
public sealed record LoginResult(string Status);
public sealed record MfaSetupDto(string SharedKey, string AuthenticatorUri);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/v1/auth").WithTags("Auth");

        // Issues the XSRF-TOKEN cookie; the SPA calls this once on start.
        auth.MapGet("/antiforgery", (IAntiforgery af, HttpContext http) =>
        {
            AntiforgeryFilter.IssueToken(af, http);
            return Results.NoContent();
        }).AllowAnonymous();

        auth.MapPost("/login", async (LoginRequest r, SignInManager<AppUser> signIn, IAntiforgery af, HttpContext http) =>
        {
            var result = await signIn.PasswordSignInAsync(r.Email, r.Password, isPersistent: false, lockoutOnFailure: true);
            if (result.RequiresTwoFactor) return Results.Ok(new LoginResult("mfa_required"));
            if (result.IsLockedOut) return Results.Ok(new LoginResult("locked_out"));
            if (!result.Succeeded) return Results.Ok(new LoginResult("invalid"));
            AntiforgeryFilter.IssueToken(af, http);   // new identity → new token
            return Results.Ok(new LoginResult("ok"));
        }).AllowAnonymous().RequireRateLimiting(RateLimits.Sensitive).AddEndpointFilter<AntiforgeryFilter>();

        auth.MapPost("/login/mfa", async (MfaCodeRequest r, SignInManager<AppUser> signIn, IAntiforgery af, HttpContext http) =>
        {
            var code = r.Code.Replace(" ", "").Replace("-", "");
            var result = await signIn.TwoFactorAuthenticatorSignInAsync(code, isPersistent: false, rememberClient: false);
            if (!result.Succeeded) return Results.Ok(new LoginResult(result.IsLockedOut ? "locked_out" : "invalid"));
            AntiforgeryFilter.IssueToken(af, http);
            return Results.Ok(new LoginResult("ok"));
        }).AllowAnonymous().RequireRateLimiting(RateLimits.Sensitive).AddEndpointFilter<AntiforgeryFilter>();

        auth.MapPost("/logout", async (SignInManager<AppUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<AntiforgeryFilter>();

        auth.MapGet("/me", async (ClaimsPrincipal principal, UserManager<AppUser> users) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            var roles = (await users.GetRolesAsync(user)).ToArray();
            return Results.Ok(new CurrentUserDto(user.Id, user.Email!, user.DisplayName, roles,
                MfaEnabled: await users.GetTwoFactorEnabledAsync(user),
                MfaSatisfied: principal.HasClaim("amr", "mfa")));
        }).RequireAuthorization();

        // ---- TOTP set-up (authenticator app) ----
        auth.MapGet("/mfa/setup", async (ClaimsPrincipal principal, UserManager<AppUser> users, IConfiguration config) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            var key = await users.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await users.ResetAuthenticatorKeyAsync(user);
                key = await users.GetAuthenticatorKeyAsync(user);
            }
            var issuer = UrlEncoder.Default.Encode(config["Auth:MfaIssuer"] ?? "FarmerSample");
            var uri = $"otpauth://totp/{issuer}:{UrlEncoder.Default.Encode(user.Email!)}?secret={key}&issuer={issuer}&digits=6";
            return Results.Ok(new MfaSetupDto(key!, uri));
        }).RequireAuthorization();

        auth.MapPost("/mfa/enable", async (MfaCodeRequest r, ClaimsPrincipal principal, UserManager<AppUser> users, SignInManager<AppUser> signIn, IAntiforgery af, HttpContext http) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            var code = r.Code.Replace(" ", "").Replace("-", "");
            var valid = await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, code);
            if (!valid) return Results.Ok(new LoginResult("invalid"));
            await users.SetTwoFactorEnabledAsync(user, true);
            // The current session has now proven a second factor.
            await signIn.SignInWithClaimsAsync(user, isPersistent: false, [new Claim("amr", "mfa")]);
            AntiforgeryFilter.IssueToken(af, http);
            return Results.Ok(new LoginResult("ok"));
        }).RequireAuthorization().RequireRateLimiting(RateLimits.Sensitive).AddEndpointFilter<AntiforgeryFilter>();
    }
}
