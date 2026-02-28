using BookSmarts.Services;
using Daisi.SDK.Clients.V1.Orc;
using Daisi.SDK.Models;

namespace BookSmarts.Web.Services;

/// <summary>
/// Validates the x-daisi-client-key header for API requests.
/// In Development mode, requests are allowed through without validation.
/// After authentication, unlocks encryption so API responses include decrypted fields.
/// </summary>
public class ApiKeyAuthFilter(AuthClientFactory authClientFactory, IWebHostEnvironment env) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        if (env.IsDevelopment())
        {
            httpContext.Items["accountId"] = httpContext.Request.Headers["x-account-id"].FirstOrDefault() ?? "dev";
            await UnlockEncryptionAsync(httpContext);
            return await next(context);
        }

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
            if (response.HasUserId)
                httpContext.Items["userId"] = response.UserId;
            if (response.HasUserName)
                httpContext.Items["userName"] = response.UserName;

            await UnlockEncryptionAsync(httpContext);
            return await next(context);
        }
        catch
        {
            return Results.Unauthorized();
        }
    }

    private static async Task UnlockEncryptionAsync(HttpContext httpContext)
    {
        var pin = httpContext.Request.Headers["x-encryption-pin"].FirstOrDefault();
        if (string.IsNullOrEmpty(pin)) return;

        var accountId = httpContext.Items["accountId"] as string;
        if (string.IsNullOrEmpty(accountId)) return;

        var encryptionSvc = httpContext.RequestServices.GetRequiredService<EncryptionSetupService>();
        await encryptionSvc.UnlockWithPinAsync(accountId, pin);
    }
}
