using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

/// <summary>
/// Manages BookSmarts user records with encryption support.
/// Follows the same pattern as OrganizationService.
/// </summary>
public class UserManagementService(BookSmartsCosmo cosmo, EncryptionContext encryption)
{
    public async Task<BookSmartsUser> CreateUserAsync(BookSmartsUser user)
    {
        Encrypt(user);
        return Decrypt(await cosmo.CreateBookSmartsUserAsync(user));
    }

    public async Task<BookSmartsUser?> GetUserAsync(string id, string accountId)
    {
        var user = await cosmo.GetBookSmartsUserAsync(id, accountId);
        return user != null ? Decrypt(user) : null;
    }

    public async Task<BookSmartsUser?> GetUserByDaisinetIdAsync(string daisinetUserId, string accountId)
    {
        var user = await cosmo.GetBookSmartsUserByDaisinetIdAsync(daisinetUserId, accountId);
        return user != null ? Decrypt(user) : null;
    }

    public async Task<List<BookSmartsUser>> GetUsersAsync(string accountId, bool activeOnly = false)
    {
        var users = await cosmo.GetBookSmartsUsersAsync(accountId, activeOnly);
        return DecryptAll(users);
    }

    public async Task<BookSmartsUser> UpdateUserAsync(BookSmartsUser user)
    {
        Encrypt(user);
        return Decrypt(await cosmo.UpdateBookSmartsUserAsync(user));
    }

    /// <summary>
    /// Deactivates a user. Prevents deactivating the last active Owner.
    /// </summary>
    public async Task<BookSmartsUser> DeactivateUserAsync(string id, string accountId)
    {
        var user = await cosmo.GetBookSmartsUserAsync(id, accountId)
            ?? throw new InvalidOperationException("User not found.");

        if (user.Role == BookSmartsRole.Owner)
        {
            var allUsers = await cosmo.GetBookSmartsUsersAsync(accountId, activeOnly: true);
            var activeOwners = allUsers.Count(u => u.Role == BookSmartsRole.Owner && u.id != id);
            if (activeOwners == 0)
                throw new InvalidOperationException("Cannot deactivate the last active Owner.");
        }

        user.IsActive = false;
        return Decrypt(await cosmo.UpdateBookSmartsUserAsync(user));
    }

    /// <summary>
    /// Reactivates a previously deactivated user.
    /// </summary>
    public async Task<BookSmartsUser> ReactivateUserAsync(string id, string accountId)
    {
        var user = await cosmo.GetBookSmartsUserAsync(id, accountId)
            ?? throw new InvalidOperationException("User not found.");

        user.IsActive = true;
        return Decrypt(await cosmo.UpdateBookSmartsUserAsync(user));
    }

    // ── Encryption helpers ──

    private void Encrypt<T>(T model) where T : class
    {
        var adk = encryption.GetAdkOrNull();
        if (adk != null)
            FieldEncryption.EncryptFields(model, adk);
    }

    private T Decrypt<T>(T model) where T : class
    {
        var adk = encryption.GetAdkOrNull();
        if (adk != null)
            FieldEncryption.DecryptFields(model, adk);
        return model;
    }

    private List<T> DecryptAll<T>(List<T> models) where T : class
    {
        var adk = encryption.GetAdkOrNull();
        if (adk != null)
            FieldEncryption.DecryptAll(models, adk);
        return models;
    }
}
