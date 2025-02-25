using EMR.Domain.Contracts;

namespace EMR.Application.Interfaces.Services;

public interface INumericService
{
    Task<string> GenerateNumberAsync<T>(
        Expression<Func<T, string>> numberSelector,
        string prefix,
        int numericLength = 6)
        where T : AuditableEntity<string>;
}

// How to use
// var number = await _numericService.GenerateNumberAsync<YourEntity>(x => x.Number, "prefix");