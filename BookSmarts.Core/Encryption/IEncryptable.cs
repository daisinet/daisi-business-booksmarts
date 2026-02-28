namespace BookSmarts.Core.Encryption;

/// <summary>
/// Marker interface for models that support field-level encryption.
/// When EncryptedPayload is non-null, sensitive fields have been encrypted into it.
/// </summary>
public interface IEncryptable
{
    string? EncryptedPayload { get; set; }
}
