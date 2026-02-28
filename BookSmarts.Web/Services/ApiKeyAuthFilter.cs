using Daisi.SDK.Clients.V1.Orc;
using Daisi.SDK.Models;

namespace BookSmarts.Web.Services;

/// <summary>
/// Validates the x-daisi-client-key header for API requests.
/// </summary>
public class ApiKeyAuthFilter(AuthClientFactory authClientFactory) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var clientKey = httpContext.Request.Headers["x-daisi-client-key"].FirstOrDefault();

        if (string.IsNullOrEmpty(clientKey))
            return Results.Unauthorized();

        try
        {
            var client = authClientFactory.Create();
            var response = client.ValidateClientKey(new Daisi.Protos.V1.ValidateClientKeyRequest
            {
                SecretKey = DaisiStaticSettings.SecretKey ?? "",
                ClientKey = clientKey
            });

            if (response?.IsValid != true)
                return Results.Unauthorized();

            httpContext.Items["accountId"] = response.UserAccountId;
            return await next(context);
        }
        catch
        {
            return Results.Unauthorized();
        }
    }
}
