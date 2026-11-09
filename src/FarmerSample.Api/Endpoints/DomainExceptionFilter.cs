using FarmerSample.Core.SharedKernel;

namespace FarmerSample.Api.Endpoints;

/// <summary>Turns a broken business rule into a 422 with a message staff can act on.</summary>
public sealed class DomainExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        try { return await next(ctx); }
        catch (DomainException e) { return Results.Problem(e.Message, statusCode: StatusCodes.Status422UnprocessableEntity, title: "Not possible"); }
    }
}
