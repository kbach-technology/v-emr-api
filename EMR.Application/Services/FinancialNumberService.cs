using EMR.Domain.Contracts;

namespace EMR.Application.Services;

public class FinancialNumberService : IFinancialNumberService
{
    private readonly IUnitOfWork<string> _unitOfWork;

    public FinancialNumberService(IUnitOfWork<string> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerateInvoiceNumberAsync<T>(
        Expression<Func<T, string>> numberSelector,
        string prefix = "INV")
        where T : AuditableEntity<string>
    {
        return await GenerateFinancialNumberAsync<T>(numberSelector, prefix);
    }

    public async Task<string> GenerateReceiptNumberAsync<T>(
        Expression<Func<T, string>> numberSelector,
        string prefix = "RCP")
        where T : AuditableEntity<string>
    {
        return await GenerateFinancialNumberAsync<T>(numberSelector, prefix);
    }

    private async Task<string> GenerateFinancialNumberAsync<T>(
        Expression<Func<T, string>> numberSelector,
        string prefix)
        where T : AuditableEntity<string>
    {
        try
        {
            var today = DateTime.UtcNow;
            var yearMonth = today.ToString("yyyyMM");
            var periodPrefix = $"{prefix}-{yearMonth}";

            var memberExp = (MemberExpression)numberSelector.Body;
            var propertyName = memberExp.Member.Name;
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyName);

            var startsWith = Expression.Call(
                property,
                typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!,
                Expression.Constant(periodPrefix));
            var latestNumberPredicate = Expression.Lambda<Func<T, bool>>(startsWith, parameter);

            var lastNumber = await _unitOfWork.Repository<T>()
                .Entities
                .Where(latestNumberPredicate)
                .OrderByDescending(numberSelector)
                .Select(numberSelector)
                .FirstOrDefaultAsync();

            var currentSequence = 1;

            if (!string.IsNullOrEmpty(lastNumber))
            {
                var sequencePart = lastNumber.Split('-').Last();
                if (int.TryParse(sequencePart, out int lastSequence))
                {
                    currentSequence = lastSequence + 1;
                }
            }

            string newNumber;
            bool numberExists;

            do
            {
                newNumber = $"{periodPrefix}-{currentSequence:D5}";

                var equalityCheck = Expression.Equal(property, Expression.Constant(newNumber));
                var equalityPredicate = Expression.Lambda<Func<T, bool>>(equalityCheck, parameter);

                numberExists = await _unitOfWork.Repository<T>()
                    .Entities
                    .AnyAsync(equalityPredicate);

                if (numberExists)
                {
                    currentSequence++;
                }
            } while (numberExists);

            return newNumber;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error generating {prefix} number", ex);
        }
    }
}