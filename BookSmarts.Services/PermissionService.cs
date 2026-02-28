using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;

namespace BookSmarts.Services;

/// <summary>
/// Static helper for deriving permissions from BookSmarts roles.
/// </summary>
public static class PermissionService
{
    public static bool CanManageUsers(BookSmartsRole role) => role >= BookSmartsRole.Owner;

    public static bool CanManageEncryption(BookSmartsRole role) => role >= BookSmartsRole.Owner;

    public static bool CanManageOrganization(BookSmartsRole role) => role >= BookSmartsRole.Owner;

    public static bool CanWriteJournals(BookSmartsRole role) => role >= BookSmartsRole.Bookkeeper;

    public static bool CanWriteInvoicesAndBills(BookSmartsRole role) => role >= BookSmartsRole.Bookkeeper;

    public static bool CanWriteBanking(BookSmartsRole role) => role >= BookSmartsRole.Bookkeeper;

    public static bool CanWriteBudgets(BookSmartsRole role) => role >= BookSmartsRole.Accountant;

    public static bool CanViewReports(BookSmartsRole role) => true; // All roles

    public static bool CanUseAiFeatures(BookSmartsRole role) => role >= BookSmartsRole.Bookkeeper;

    /// <summary>
    /// Checks if a user has access to a specific company.
    /// Owners with empty CompanyIds have access to all companies.
    /// </summary>
    public static bool HasCompanyAccess(BookSmartsUser user, string companyId)
    {
        if (user.Role == BookSmartsRole.Owner && user.CompanyIds.Count == 0)
            return true;

        return user.CompanyIds.Contains(companyId);
    }

    /// <summary>
    /// Filters a list of companies to only those the user has access to.
    /// </summary>
    public static List<Company> FilterCompanies(BookSmartsUser user, List<Company> companies)
    {
        if (user.Role == BookSmartsRole.Owner && user.CompanyIds.Count == 0)
            return companies;

        return companies.Where(c => user.CompanyIds.Contains(c.id)).ToList();
    }
}
