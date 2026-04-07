namespace BookSmarts.E2E;

/// <summary>
/// Base class for tests that need a brand-new DAISI user with a clean BookSmarts slate.
/// Creates a fresh user via ORC gRPC during setup, then SSO's into BookSmarts.
/// The user arrives as Owner with no companies and no data — perfect for
/// business scenario tests that model a new customer's complete journey.
/// </summary>
public abstract class FreshAccountTestBase : AuthenticatedTestBase
{
    private TestUserProfile? _freshUser;

    /// <summary>
    /// Override these in subclasses to customize the test user identity.
    /// </summary>
    protected virtual string ScenarioOwnerName => "E2E Test Owner";
    protected virtual string ScenarioEmail => $"e2e-{Guid.NewGuid():N}@test.booksmarts.local";
    protected virtual string ScenarioPhone => "555-000-0000";

    protected override TestUserProfile TestUser =>
        _freshUser ?? throw new InvalidOperationException("Fresh user not yet created.");

    public override async Task InitializeAsync()
    {
        // Create a new user via ORC gRPC before browser setup
        _freshUser = TestAccountHelper.CreateUser(
            ScenarioOwnerName,
            ScenarioEmail,
            ScenarioPhone
        ) with { EncryptionPin = TestConfig.TestUserEncryptionPin };

        // Now run the normal auth flow (browser init + SSO ticket + PIN if needed)
        await base.InitializeAsync();
    }
}
