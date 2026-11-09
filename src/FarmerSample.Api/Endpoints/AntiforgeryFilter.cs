using Microsoft.AspNetCore.Antiforgery;

namespace FarmerSample.Api.Endpoints;

/// <summary>
/// CSRF protection for cookie-authenticated JSON endpoints: the SPA reads the XSRF-TOKEN cookie and
/// echoes it in the X-XSRF-TOKEN header (Angular HttpClient does this automatically).
/// </summary>
public sealed class AntiforgeryFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var method = ctx.HttpContext.Request.Method;
        if (HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method))
        {
            if (!await antiforgery.IsRequestValidAsync(ctx.HttpContext))
                return Results.Problem("Missing or invalid anti-forgery token.", statusCode: StatusCodes.Status400BadRequest);
        }
        return await next(ctx);
    }

    /// <summary>Issues the token cookie the SPA reads. Called after every sign-in change and on app start.</summary>
    public static void IssueToken(IAntiforgery antiforgery, HttpContext http)
    {
        var tokens = antiforgery.GetAndStoreTokens(http);
        http.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false, SameSite = SameSiteMode.Lax, Secure = http.Request.IsHttps, Path = "/",
        });
    }
}
