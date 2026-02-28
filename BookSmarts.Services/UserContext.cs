using BookSmarts.Core.Models;

namespace BookSmarts.Services;

/// <summary>
/// Scoped service holding the current BookSmarts user for the session.
/// Set by the layout after authentication and user lookup.
/// </summary>
public class UserContext
{
    public BookSmartsUser? CurrentUser { get; set; }

    public string? UserId => CurrentUser?.id;
    public string? UserName => CurrentUser?.Name;
}
