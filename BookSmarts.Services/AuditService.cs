using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class AuditService(BookSmartsCosmo cosmo)
{
    public async Task LogAsync(string companyId, string accountId, string action,
        string entityType, string entityId, string? entityLabel = null,
        string? description = null, string? userId = null, string? userName = null)
    {
        var entry = new AuditEntry
        {
            CompanyId = companyId,
            AccountId = accountId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityLabel = entityLabel,
            Description = description,
            UserId = userId,
            UserName = userName
        };

        try
        {
            await cosmo.CreateAuditEntryAsync(entry);
        }
        catch
        {
            // Audit logging should never block the primary operation
        }
    }

    public async Task<List<AuditEntry>> GetAuditLogAsync(
        string companyId, DateTime? fromDate = null, DateTime? toDate = null,
        string? entityType = null, string? entityId = null, int maxItems = 100)
    {
        return await cosmo.GetAuditEntriesAsync(companyId, fromDate, toDate, entityType, entityId, maxItems);
    }
}
