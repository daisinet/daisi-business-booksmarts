namespace BookSmarts.Core.Enums;

/// <summary>
/// Roles for BookSmarts users, ordered by privilege level for >= comparisons.
/// </summary>
public enum BookSmartsRole
{
    Viewer = 0,
    Bookkeeper = 1,
    Accountant = 2,
    Owner = 3
}
