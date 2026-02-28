using BookSmarts.Core.Encryption;
using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

/// <summary>
/// A user within a BookSmarts account, linked to a Daisinet SSO user.
/// Stored in the Organizations container, partitioned by AccountId.
/// </summary>
public class BookSmartsUser : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(BookSmartsUser);
    public string AccountId { get; set; } = "";

    /// <summary>
    /// Links to the Daisinet SSO user (ClaimTypes.Sid from auth).
    /// </summary>
    public string DaisinetUserId { get; set; } = "";

    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public BookSmartsRole Role { get; set; } = BookSmartsRole.Viewer;

    /// <summary>
    /// Company IDs this user can access. Empty list for Owner means access to ALL companies.
    /// Non-Owner roles must have explicit company assignments.
    /// </summary>
    public List<string> CompanyIds { get; set; } = new();

    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public string? EncryptedPayload { get; set; }
}
