using EMR.Domain.Contracts;

namespace EMR.Application.Interfaces.Services;

public interface IFinancialNumberService
{
    Task<string> GenerateInvoiceNumberAsync<T>(
        Expression<Func<T, string>> numberSelector,
        string prefix = "INV") 
        where T : AuditableEntity<string>;

    Task<string> GenerateReceiptNumberAsync<T>(
        Expression<Func<T, string>> numberSelector,
        string prefix = "RCP") 
        where T : AuditableEntity<string>;
}