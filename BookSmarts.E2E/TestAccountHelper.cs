using Daisi.Protos.V1;
using Daisi.SDK.Clients.V1.Orc;
using Daisi.SDK.Models;

namespace BookSmarts.E2E;

/// <summary>
/// Programmatically creates DAISI users via ORC gRPC for E2E scenario tests.
/// New users are created under the same DAISI account as the SecretKey,
/// which is the same account BookSmarts uses for SSO.
/// Each user gets a fresh BookSmarts experience — no companies, no data.
/// </summary>
public static class TestAccountHelper
{
    private static void EnsureOrcConfigured()
    {
        DaisiStaticSettings.OrcIpAddressOrDomain = TestConfig.OrcHost;
        DaisiStaticSettings.OrcPort = TestConfig.OrcPort;
        DaisiStaticSettings.OrcUseSSL = TestConfig.OrcUseSsl;
        DaisiStaticSettings.SecretKey = TestConfig.SecretKey;
        DaisiStaticSettings.SsoSigningKey = TestConfig.SsoSigningKey;
    }

    /// <summary>
    /// Creates a new DAISI user under the existing account via ORC gRPC.
    /// Returns a TestUserProfile ready for SSO ticket generation.
    /// </summary>
    public static TestUserProfile CreateUser(string ownerName, string email, string phone)
    {
        EnsureOrcConfigured();

        // Get an app-level clientKey so we can call the Accounts API
        var authClientFactory = new AuthClientFactory();
        authClientFactory.CreateStaticClientKey();

        // Create a new user under the existing account (same as SecretKey's account)
        var accountClient = new AccountClientFactory().Create();
        var response = accountClient.CreateUser(new CreateUserRequest
        {
            User = new User
            {
                Name = ownerName,
                EmailAddress = email,
                Phone = phone,
                Role = UserRoles.Owner,
                AllowedToLogin = true,
                AllowEmail = true,
                AllowSMS = true
            }
        });

        if (!response.Success)
            throw new InvalidOperationException(
                $"Failed to create test user '{ownerName}': {response.Message}");

        var user = response.User;

        // Get account info using a user-scoped clientKey
        var authClient = authClientFactory.Create();
        var keyResponse = authClient.CreateClientKey(new CreateClientKeyRequest
        {
            SecretKey = TestConfig.SecretKey,
            OwnerId = user.Id,
            OwnerName = user.Name,
            OwnerRole = SystemRoles.User
        });

        DaisiStaticSettings.ClientKey = keyResponse.ClientKey;
        var userAccountClient = new AccountClientFactory().Create();
        var accountResponse = userAccountClient.Get(new GetAccountRequest());

        return new TestUserProfile
        {
            UserId = user.Id,
            UserName = user.Name,
            AccountId = accountResponse.Account.Id,
            AccountName = accountResponse.Account.Name,
            UserRole = "Owner"
        };
    }
}
