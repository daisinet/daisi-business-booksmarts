using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using Microsoft.Azure.Cosmos;

namespace BookSmarts.Data;

public partial class BookSmartsCosmo
{
    public const string ArApContainerName = "ArAp";
    public const string ArApPartitionKeyName = "CompanyId";

    public const string CustomerIdPrefix = "cust";
    public const string VendorIdPrefix = "vend";
    public const string InvoiceIdPrefix = "inv";
    public const string BillIdPrefix = "bl";
    public const string PaymentIdPrefix = "pmt";

    public PartitionKey GetArApPartitionKey(string companyId) => new(companyId);

    // ── Customer CRUD ──

    public virtual async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        if (string.IsNullOrEmpty(customer.id))
            customer.id = GenerateId(CustomerIdPrefix);
        customer.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(ArApContainerName);
        var response = await container.CreateItemAsync(customer, GetArApPartitionKey(customer.CompanyId));
        return response.Resource;
    }

    public virtual async Task<Customer?> GetCustomerAsync(string id, string companyId)
    {
        try
        {
            var container = await GetContainerAsync(ArApContainerName);
            var response = await container.ReadItemAsync<Customer>(id, GetArApPartitionKey(companyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<Customer>> GetCustomersAsync(string companyId, bool activeOnly = false)
    {
        var container = await GetContainerAsync(ArApContainerName);
        var sql = "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'Customer'";
        if (activeOnly)
            sql += " AND c.IsActive = true";
        sql += " ORDER BY c.Name";

        var query = new QueryDefinition(sql).WithParameter("@companyId", companyId);
        var results = new List<Customer>();
        using var iterator = container.GetItemQueryIterator<Customer>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        customer.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(ArApContainerName);
        var response = await container.UpsertItemAsync(customer, GetArApPartitionKey(customer.CompanyId));
        return response.Resource;
    }

    public virtual async Task DeleteCustomerAsync(string id, string companyId)
    {
        var container = await GetContainerAsync(ArApContainerName);
        await container.DeleteItemAsync<Customer>(id, GetArApPartitionKey(companyId));
    }

    // ── Vendor CRUD ──

    public virtual async Task<Vendor> CreateVendorAsync(Vendor vendor)
    {
        if (string.IsNullOrEmpty(vendor.id))
            vendor.id = GenerateId(VendorIdPrefix);
        vendor.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(ArApContainerName);
        var response = await container.CreateItemAsync(vendor, GetArApPartitionKey(vendor.CompanyId));
        return response.Resource;
    }

    public virtual async Task<Vendor?> GetVendorAsync(string id, string companyId)
    {
        try
        {
            var container = await GetContainerAsync(ArApContainerName);
            var response = await container.ReadItemAsync<Vendor>(id, GetArApPartitionKey(companyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<Vendor>> GetVendorsAsync(string companyId, bool activeOnly = false)
    {
        var container = await GetContainerAsync(ArApContainerName);
        var sql = "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'Vendor'";
        if (activeOnly)
            sql += " AND c.IsActive = true";
        sql += " ORDER BY c.Name";

        var query = new QueryDefinition(sql).WithParameter("@companyId", companyId);
        var results = new List<Vendor>();
        using var iterator = container.GetItemQueryIterator<Vendor>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<Vendor> UpdateVendorAsync(Vendor vendor)
    {
        vendor.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(ArApContainerName);
        var response = await container.UpsertItemAsync(vendor, GetArApPartitionKey(vendor.CompanyId));
        return response.Resource;
    }

    public virtual async Task DeleteVendorAsync(string id, string companyId)
    {
        var container = await GetContainerAsync(ArApContainerName);
        await container.DeleteItemAsync<Vendor>(id, GetArApPartitionKey(companyId));
    }

    // ── Invoice CRUD ──

    public virtual async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
    {
        if (string.IsNullOrEmpty(invoice.id))
            invoice.id = GenerateId(InvoiceIdPrefix);
        invoice.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(ArApContainerName);
        var response = await container.CreateItemAsync(invoice, GetArApPartitionKey(invoice.CompanyId));
        return response.Resource;
    }

    public virtual async Task<Invoice?> GetInvoiceAsync(string id, string companyId)
    {
        try
        {
            var container = await GetContainerAsync(ArApContainerName);
            var response = await container.ReadItemAsync<Invoice>(id, GetArApPartitionKey(companyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<Invoice>> GetInvoicesAsync(string companyId, InvoiceStatus? status = null, string? customerId = null, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 100)
    {
        var container = await GetContainerAsync(ArApContainerName);
        var sql = "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'Invoice'";
        if (status.HasValue)
            sql += " AND c.Status = @status";
        if (!string.IsNullOrEmpty(customerId))
            sql += " AND c.CustomerId = @customerId";
        if (fromDate.HasValue)
            sql += " AND c.InvoiceDate >= @fromDate";
        if (toDate.HasValue)
            sql += " AND c.InvoiceDate <= @toDate";
        sql += " ORDER BY c.InvoiceDate DESC";

        var query = new QueryDefinition(sql).WithParameter("@companyId", companyId);
        if (status.HasValue)
            query = query.WithParameter("@status", (int)status.Value);
        if (!string.IsNullOrEmpty(customerId))
            query = query.WithParameter("@customerId", customerId);
        if (fromDate.HasValue)
            query = query.WithParameter("@fromDate", fromDate.Value);
        if (toDate.HasValue)
            query = query.WithParameter("@toDate", toDate.Value);

        var results = new List<Invoice>();
        using var iterator = container.GetItemQueryIterator<Invoice>(query, requestOptions: new QueryRequestOptions { MaxItemCount = maxItems });
        while (iterator.HasMoreResults && results.Count < maxItems)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results.Take(maxItems).ToList();
    }

    public virtual async Task<Invoice> UpdateInvoiceAsync(Invoice invoice)
    {
        invoice.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(ArApContainerName);
        var response = await container.UpsertItemAsync(invoice, GetArApPartitionKey(invoice.CompanyId));
        return response.Resource;
    }

    public virtual async Task<int> GetNextInvoiceNumberAsync(string companyId)
    {
        var container = await GetContainerAsync(ArApContainerName);
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.CompanyId = @companyId AND c.Type = 'Invoice'")
            .WithParameter("@companyId", companyId);

        using var iterator = container.GetItemQueryIterator<int>(query);
        var response = await iterator.ReadNextAsync();
        return response.FirstOrDefault() + 1;
    }

    public virtual async Task<List<Invoice>> GetOpenInvoicesAsync(string companyId, string? customerId = null)
    {
        var container = await GetContainerAsync(ArApContainerName);
        var sql = "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'Invoice' AND c.Status IN (1, 2, 4)";
        if (!string.IsNullOrEmpty(customerId))
            sql += " AND c.CustomerId = @customerId";
        sql += " ORDER BY c.InvoiceDate DESC";

        var query = new QueryDefinition(sql).WithParameter("@companyId", companyId);
        if (!string.IsNullOrEmpty(customerId))
            query = query.WithParameter("@customerId", customerId);

        var results = new List<Invoice>();
        using var iterator = container.GetItemQueryIterator<Invoice>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task PatchInvoicePaymentAsync(string id, string companyId, decimal amountPaid, InvoiceStatus status)
    {
        var container = await GetContainerAsync(ArApContainerName);
        var operations = new List<PatchOperation>
        {
            PatchOperation.Set("/AmountPaid", amountPaid),
            PatchOperation.Set("/Status", (int)status),
            PatchOperation.Set("/UpdatedUtc", DateTime.UtcNow)
        };
        await container.PatchItemAsync<Invoice>(id, GetArApPartitionKey(companyId), operations);
    }

    // ── Bill CRUD ──

    public virtual async Task<Bill> CreateBillAsync(Bill bill)
    {
        if (string.IsNullOrEmpty(bill.id))
            bill.id = GenerateId(BillIdPrefix);
        bill.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(ArApContainerName);
        var response = await container.CreateItemAsync(bill, GetArApPartitionKey(bill.CompanyId));
        return response.Resource;
    }

    public virtual async Task<Bill?> GetBillAsync(string id, string companyId)
    {
        try
        {
            var container = await GetContainerAsync(ArApContainerName);
            var response = await container.ReadItemAsync<Bill>(id, GetArApPartitionKey(companyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<Bill>> GetBillsAsync(string companyId, BillStatus? status = null, string? vendorId = null, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 100)
    {
        var container = await GetContainerAsync(ArApContainerName);
        var sql = "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'Bill'";
        if (status.HasValue)
            sql += " AND c.Status = @status";
        if (!string.IsNullOrEmpty(vendorId))
            sql += " AND c.VendorId = @vendorId";
        if (fromDate.HasValue)
            sql += " AND c.BillDate >= @fromDate";
        if (toDate.HasValue)
            sql += " AND c.BillDate <= @toDate";
        sql += " ORDER BY c.BillDate DESC";

        var query = new QueryDefinition(sql).WithParameter("@companyId", companyId);
        if (status.HasValue)
            query = query.WithParameter("@status", (int)status.Value);
        if (!string.IsNullOrEmpty(vendorId))
            query = query.WithParameter("@vendorId", vendorId);
        if (fromDate.HasValue)
            query = query.WithParameter("@fromDate", fromDate.Value);
        if (toDate.HasValue)
            query = query.WithParameter("@toDate", toDate.Value);

        var results = new List<Bill>();
        using var iterator = container.GetItemQueryIterator<Bill>(query, requestOptions: new QueryRequestOptions { MaxItemCount = maxItems });
        while (iterator.HasMoreResults && results.Count < maxItems)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results.Take(maxItems).ToList();
    }

    public virtual async Task<Bill> UpdateBillAsync(Bill bill)
    {
        bill.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(ArApContainerName);
        var response = await container.UpsertItemAsync(bill, GetArApPartitionKey(bill.CompanyId));
        return response.Resource;
    }

    public virtual async Task<int> GetNextBillNumberAsync(string companyId)
    {
        var container = await GetContainerAsync(ArApContainerName);
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.CompanyId = @companyId AND c.Type = 'Bill'")
            .WithParameter("@companyId", companyId);

        using var iterator = container.GetItemQueryIterator<int>(query);
        var response = await iterator.ReadNextAsync();
        return response.FirstOrDefault() + 1;
    }

    public virtual async Task<List<Bill>> GetOpenBillsAsync(string companyId, string? vendorId = null)
    {
        var container = await GetContainerAsync(ArApContainerName);
        var sql = "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'Bill' AND c.Status IN (1, 2, 4)";
        if (!string.IsNullOrEmpty(vendorId))
            sql += " AND c.VendorId = @vendorId";
        sql += " ORDER BY c.BillDate DESC";

        var query = new QueryDefinition(sql).WithParameter("@companyId", companyId);
        if (!string.IsNullOrEmpty(vendorId))
            query = query.WithParameter("@vendorId", vendorId);

        var results = new List<Bill>();
        using var iterator = container.GetItemQueryIterator<Bill>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task PatchBillPaymentAsync(string id, string companyId, decimal amountPaid, BillStatus status)
    {
        var container = await GetContainerAsync(ArApContainerName);
        var operations = new List<PatchOperation>
        {
            PatchOperation.Set("/AmountPaid", amountPaid),
            PatchOperation.Set("/Status", (int)status),
            PatchOperation.Set("/UpdatedUtc", DateTime.UtcNow)
        };
        await container.PatchItemAsync<Bill>(id, GetArApPartitionKey(companyId), operations);
    }

    // ── Payment CRUD ──

    public virtual async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        if (string.IsNullOrEmpty(payment.id))
            payment.id = GenerateId(PaymentIdPrefix);
        payment.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(ArApContainerName);
        var response = await container.CreateItemAsync(payment, GetArApPartitionKey(payment.CompanyId));
        return response.Resource;
    }

    public virtual async Task<Payment?> GetPaymentAsync(string id, string companyId)
    {
        try
        {
            var container = await GetContainerAsync(ArApContainerName);
            var response = await container.ReadItemAsync<Payment>(id, GetArApPartitionKey(companyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<Payment>> GetPaymentsAsync(string companyId, PaymentType? type = null, PaymentStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 100)
    {
        var container = await GetContainerAsync(ArApContainerName);
        var sql = "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'Payment'";
        if (type.HasValue)
            sql += " AND c.PaymentType = @paymentType";
        if (status.HasValue)
            sql += " AND c.Status = @status";
        if (fromDate.HasValue)
            sql += " AND c.PaymentDate >= @fromDate";
        if (toDate.HasValue)
            sql += " AND c.PaymentDate <= @toDate";
        sql += " ORDER BY c.PaymentDate DESC";

        var query = new QueryDefinition(sql).WithParameter("@companyId", companyId);
        if (type.HasValue)
            query = query.WithParameter("@paymentType", (int)type.Value);
        if (status.HasValue)
            query = query.WithParameter("@status", (int)status.Value);
        if (fromDate.HasValue)
            query = query.WithParameter("@fromDate", fromDate.Value);
        if (toDate.HasValue)
            query = query.WithParameter("@toDate", toDate.Value);

        var results = new List<Payment>();
        using var iterator = container.GetItemQueryIterator<Payment>(query, requestOptions: new QueryRequestOptions { MaxItemCount = maxItems });
        while (iterator.HasMoreResults && results.Count < maxItems)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results.Take(maxItems).ToList();
    }

    public virtual async Task<Payment> UpdatePaymentAsync(Payment payment)
    {
        payment.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(ArApContainerName);
        var response = await container.UpsertItemAsync(payment, GetArApPartitionKey(payment.CompanyId));
        return response.Resource;
    }

    public virtual async Task<int> GetNextPaymentNumberAsync(string companyId)
    {
        var container = await GetContainerAsync(ArApContainerName);
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.CompanyId = @companyId AND c.Type = 'Payment'")
            .WithParameter("@companyId", companyId);

        using var iterator = container.GetItemQueryIterator<int>(query);
        var response = await iterator.ReadNextAsync();
        return response.FirstOrDefault() + 1;
    }
}
