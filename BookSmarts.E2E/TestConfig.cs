using System.Text.Json;

namespace BookSmarts.E2E;

/// <summary>
/// Configuration for E2E tests. Loads from testsettings.json (gitignored),
/// with environment variable overrides for CI.
/// Copy testsettings.template.json to testsettings.json and fill in your values.
/// </summary>
public static class TestConfig
{
    private static readonly JsonElement _root;

    static TestConfig()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "testsettings.json");
        if (!File.Exists(path))
        {
            // Fall back to project directory (when running from IDE)
            path = Path.Combine(FindProjectDir(), "testsettings.json");
        }

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            _root = JsonDocument.Parse(json).RootElement;
        }
        else
        {
            _root = JsonDocument.Parse("{}").RootElement;
        }
    }

    private static string FindProjectDir()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "BookSmarts.E2E.csproj")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return Directory.GetCurrentDirectory();
    }

    private static string Get(string section, string key, string? fallback = null)
    {
        // Environment variable override (e.g. DAISI_SECRET_KEY)
        var envKey = $"{section}_{key}".ToUpperInvariant().Replace(":", "_");
        var envVal = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrEmpty(envVal)) return envVal;

        // JSON file
        if (_root.TryGetProperty(section, out var sectionEl) &&
            sectionEl.TryGetProperty(key, out var val))
        {
            return val.ToString();
        }

        return fallback ?? throw new InvalidOperationException(
            $"Missing config: {section}:{key}. " +
            "Copy testsettings.template.json to testsettings.json and fill in your values.");
    }

    private static string? GetOptional(string section, string key)
    {
        var envKey = $"{section}_{key}".ToUpperInvariant().Replace(":", "_");
        var envVal = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrEmpty(envVal)) return envVal;

        if (_root.TryGetProperty(section, out var sectionEl) &&
            sectionEl.TryGetProperty(key, out var val))
        {
            return val.ToString();
        }

        return null;
    }

    // --- BookSmarts App ---
    public static string BaseUrl => Get("BookSmarts", "BaseUrl", "https://localhost:5201");

    // --- Manager (SSO authority, registration) ---
    public static string ManagerUrl => Get("Manager", "BaseUrl", "https://localhost:7150");

    // --- Browser ---
    public static bool Headless => Environment.GetEnvironmentVariable("BOOKSMARTS_HEADED") != "1";
    public static float SlowMo =>
        float.TryParse(Environment.GetEnvironmentVariable("BOOKSMARTS_SLOWMO"), out var v) ? v : 0;
    public static string ScreenshotDir =>
        Environment.GetEnvironmentVariable("BOOKSMARTS_SCREENSHOTS") ?? "TestResults/Screenshots";

    // --- ORC Connection ---
    public static string OrcHost => Get("Daisi", "OrcHost", "localhost");
    public static int OrcPort => int.TryParse(Get("Daisi", "OrcPort", "5001"), out var p) ? p : 5001;
    public static bool OrcUseSsl => Get("Daisi", "OrcUseSsl", "true") != "false";
    public static string SecretKey => Get("Daisi", "SecretKey");
    public static string SsoSigningKey => Get("Daisi", "SsoSigningKey");

    // --- Test User ---
    public static string TestUserId => Get("TestUser", "UserId");
    public static string TestUserName => Get("TestUser", "UserName", "E2E Test User");
    public static string TestAccountId => Get("TestUser", "AccountId");
    public static string TestAccountName => Get("TestUser", "AccountName", "E2E Test Account");
    public static string TestUserRole => Get("TestUser", "UserRole", "Owner");
    public static string? TestUserEncryptionPin => GetOptional("TestUser", "EncryptionPin");

    public static TestUserProfile DefaultTestUser => new()
    {
        UserId = TestUserId,
        UserName = TestUserName,
        AccountId = TestAccountId,
        AccountName = TestAccountName,
        UserRole = TestUserRole,
        EncryptionPin = TestUserEncryptionPin
    };
}
