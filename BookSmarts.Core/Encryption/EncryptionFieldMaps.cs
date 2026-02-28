using BookSmarts.Core.Models;

namespace BookSmarts.Core.Encryption;

/// <summary>
/// Maps each encryptable model type to its sensitive property names.
/// Only these properties are serialized into EncryptedPayload.
/// </summary>
public static class EncryptionFieldMaps
{
    private static readonly Dictionary<Type, string[]> Maps = new()
    {
        [typeof(Organization)] = ["Name"],
        [typeof(Company)] = ["Name", "TaxId", "Address", "Phone", "Email"],
        [typeof(Division)] = ["Name"],
        [typeof(ChartOfAccountEntry)] = ["Name", "Description"],
        [typeof(JournalEntry)] = ["Description", "Memo", "Lines"],
        [typeof(FiscalYear)] = ["Name", "Periods"],
        [typeof(BankConnection)] = ["InstitutionName", "Accounts", "EncryptedAccessToken"],
        [typeof(BankTransaction)] = ["MerchantName", "Name", "Amount", "PlaidCategories", "CategorizedAccountName"],
        [typeof(CategorizationRule)] = ["MerchantNameContains", "PlaidCategory", "TargetAccountName"],
        [typeof(Customer)] = ["Name", "Email", "Phone", "Address", "ContactPerson", "Notes"],
        [typeof(Vendor)] = ["Name", "Email", "Phone", "Address", "ContactPerson", "Notes"],
        [typeof(Invoice)] = ["CustomerName", "Lines", "Notes", "Memo"],
        [typeof(Bill)] = ["VendorName", "Lines", "Notes", "Memo", "VendorReferenceNumber"],
        [typeof(Payment)] = ["CustomerName", "VendorName", "Allocations", "Notes", "Amount"],
    };

    /// <summary>
    /// Gets the sensitive field names for a given model type.
    /// Returns empty array if the type is not registered.
    /// </summary>
    public static string[] GetFields(Type type)
    {
        return Maps.TryGetValue(type, out var fields) ? fields : [];
    }

    /// <summary>
    /// Gets the sensitive field names for a given model type.
    /// </summary>
    public static string[] GetFields<T>() => GetFields(typeof(T));
}
