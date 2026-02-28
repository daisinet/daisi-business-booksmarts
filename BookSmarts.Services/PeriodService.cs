using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class PeriodService(BookSmartsCosmo cosmo, EncryptionContext encryption)
{
    public async Task<List<FiscalYear>> GetFiscalYearsAsync(string companyId)
    {
        var years = await cosmo.GetFiscalYearsAsync(companyId);
        return DecryptAll(years);
    }

    public async Task<FiscalYear?> GetFiscalYearAsync(string id, string companyId)
    {
        var fy = await cosmo.GetFiscalYearAsync(id, companyId);
        return fy != null ? Decrypt(fy) : null;
    }

    /// <summary>
    /// Creates a fiscal year with 12 monthly periods.
    /// </summary>
    public async Task<FiscalYear> CreateFiscalYearAsync(string companyId, int startYear, int startMonth = 1)
    {
        var start = new DateTime(startYear, startMonth, 1);
        var end = start.AddYears(1).AddDays(-1);

        var existing = await cosmo.GetFiscalYearsAsync(companyId);
        if (existing.Any(fy => fy.StartDate <= end && fy.EndDate >= start))
            throw new InvalidOperationException("A fiscal year already exists that overlaps with this date range.");

        var fy = new FiscalYear
        {
            CompanyId = companyId,
            Name = startMonth == 1 ? $"FY {startYear}" : $"FY {startYear}-{startYear + 1}",
            StartDate = start,
            EndDate = end,
            Periods = GenerateMonthlyPeriods(start)
        };

        Encrypt(fy);
        return Decrypt(await cosmo.CreateFiscalYearAsync(fy));
    }

    /// <summary>
    /// Closes a fiscal period, preventing new postings.
    /// </summary>
    public async Task<FiscalYear> ClosePeriodAsync(string fiscalYearId, string periodId, string companyId)
    {
        var fy = Decrypt(await cosmo.GetFiscalYearAsync(fiscalYearId, companyId)
            ?? throw new InvalidOperationException("Fiscal year not found."));

        var period = fy.Periods.FirstOrDefault(p => p.PeriodId == periodId)
            ?? throw new InvalidOperationException("Period not found.");

        if (period.Status == FiscalPeriodStatus.Closed)
            throw new InvalidOperationException("Period is already closed.");

        period.Status = FiscalPeriodStatus.Closed;
        period.ClosedUtc = DateTime.UtcNow;

        Encrypt(fy);
        return Decrypt(await cosmo.UpdateFiscalYearAsync(fy));
    }

    /// <summary>
    /// Reopens a closed period.
    /// </summary>
    public async Task<FiscalYear> ReopenPeriodAsync(string fiscalYearId, string periodId, string companyId)
    {
        var fy = Decrypt(await cosmo.GetFiscalYearAsync(fiscalYearId, companyId)
            ?? throw new InvalidOperationException("Fiscal year not found."));

        var period = fy.Periods.FirstOrDefault(p => p.PeriodId == periodId)
            ?? throw new InvalidOperationException("Period not found.");

        if (period.Status == FiscalPeriodStatus.Locked)
            throw new InvalidOperationException("Locked periods cannot be reopened.");

        period.Status = FiscalPeriodStatus.Open;
        period.ClosedUtc = null;

        Encrypt(fy);
        return Decrypt(await cosmo.UpdateFiscalYearAsync(fy));
    }

    /// <summary>
    /// Closes the entire fiscal year and locks all periods.
    /// </summary>
    public async Task<FiscalYear> CloseFiscalYearAsync(string fiscalYearId, string companyId)
    {
        var fy = Decrypt(await cosmo.GetFiscalYearAsync(fiscalYearId, companyId)
            ?? throw new InvalidOperationException("Fiscal year not found."));

        fy.IsClosed = true;
        foreach (var period in fy.Periods)
        {
            period.Status = FiscalPeriodStatus.Locked;
            period.ClosedUtc ??= DateTime.UtcNow;
        }

        Encrypt(fy);
        return Decrypt(await cosmo.UpdateFiscalYearAsync(fy));
    }

    private static List<FiscalPeriod> GenerateMonthlyPeriods(DateTime yearStart)
    {
        var periods = new List<FiscalPeriod>();
        for (int i = 0; i < 12; i++)
        {
            var periodStart = yearStart.AddMonths(i);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);
            periods.Add(new FiscalPeriod
            {
                PeriodId = BookSmartsCosmo.GenerateId("fp"),
                PeriodNumber = i + 1,
                Name = periodStart.ToString("MMMM yyyy"),
                StartDate = periodStart,
                EndDate = periodEnd
            });
        }
        return periods;
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
