namespace BookSmarts.Core.Models;

/// <summary>
/// Stores encryption configuration for an account.
/// Lives in the Organizations container, partitioned by AccountId.
/// Contains wrapped copies of the ADK (never plaintext).
/// </summary>
public class EncryptionConfig
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(EncryptionConfig);
    public string AccountId { get; set; } = "";

    // PIN-derived key wrapping
    public string PinSalt { get; set; } = "";
    public string PinEncryptedAdk { get; set; } = "";
    public int PinIterations { get; set; }

    // Recovery phrase-derived key wrapping
    public string RecoverySalt { get; set; } = "";
    public string RecoveryEncryptedAdk { get; set; } = "";
    public int RecoveryIterations { get; set; }

    public bool IsEnabled { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
}
