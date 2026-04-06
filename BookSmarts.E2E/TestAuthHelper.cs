using Daisi.Protos.V1;
using Daisi.SDK.Clients.V1.Orc;
using Daisi.SDK.Models;
using Daisi.SDK.Web.Services;

namespace BookSmarts.E2E;

/// <summary>
/// Creates authenticated test sessions by generating a user-scoped clientKey via the ORC
/// and building an SSO ticket that BookSmarts' /sso/callback endpoint will accept.
/// This uses the exact same SSO flow as production — no backdoors, no bypasses.
/// </summary>
public static class TestAuthHelper
{
    /// <summary>
    /// Generates an encrypted SSO ticket for a specific user that can be passed
    /// to BookSmarts' /sso/callback endpoint to establish an authenticated session.
    /// </summary>
    public static string CreateSsoTicketForUser(TestUserProfile user)
    {
        // Configure SDK to talk to local ORC
        DaisiStaticSettings.OrcIpAddressOrDomain = TestConfig.OrcHost;
        DaisiStaticSettings.OrcPort = TestConfig.OrcPort;
        DaisiStaticSettings.OrcUseSSL = TestConfig.OrcUseSsl;
        DaisiStaticSettings.SecretKey = TestConfig.SecretKey;
        DaisiStaticSettings.SsoSigningKey = TestConfig.SsoSigningKey;

        // Create a user-scoped clientKey via the ORC gRPC API
        var authClient = new AuthClientFactory().Create();
        var response = authClient.CreateClientKey(new CreateClientKeyRequest
        {
            SecretKey = TestConfig.SecretKey,
            OwnerId = user.UserId,
            OwnerName = user.UserName,
            OwnerRole = SystemRoles.User
        });

        // Build an encrypted SSO ticket using the same SsoTicketService that all DAISI apps use
        var ssoService = new SsoTicketService();
        var ticket = ssoService.CreateTicket(
            clientKey: response.ClientKey,
            keyExpiration: response.KeyExpiration.ToDateTime().ToString("O"),
            userName: user.UserName,
            userRole: user.UserRole,
            accountName: user.AccountName,
            accountId: user.AccountId,
            userId: user.UserId
        );

        return ticket;
    }
}

/// <summary>
/// Represents a test user for E2E scenarios. Configure these in TestConfig or per-scenario.
/// </summary>
public record TestUserProfile
{
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public required string AccountId { get; init; }
    public required string AccountName { get; init; }
    public string UserRole { get; init; } = "Owner";
    public string? EncryptionPin { get; init; }
}
