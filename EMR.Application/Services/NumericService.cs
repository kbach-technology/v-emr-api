using EMR.Domain.Contracts;

namespace EMR.Application.Services;

public class NumericService : INumericService
{
    private readonly IUnitOfWork<string> _unitOfWork;

    public NumericService(IUnitOfWork<string> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerateNumberAsync<T>(
        Expression<Func<T, string>> numberSelector,
        string prefix,
        int numericLength = 6)
        where T : AuditableEntity<string>
    {
        try
        {
            string newNumber;
            bool numberExists;

            do
            {
                // Get the last number in a separate query within the loop
                var lastNumber = await _unitOfWork.Repository<T>()
                    .Entities
                    .OrderByDescending(numberSelector)
                    .Select(numberSelector)
                    .FirstOrDefaultAsync();

                var currentNumber = 1;

                if (!string.IsNullOrEmpty(lastNumber) &&
                    lastNumber.StartsWith(prefix) &&
                    lastNumber.Length == (prefix.Length + numericLength))
                {
                    var numericPart = lastNumber.Substring(prefix.Length);
                    if (int.TryParse(numericPart, out int lastSeq))
                    {
                        currentNumber = lastSeq + 1;
                    }
                }

                newNumber = $"{prefix}{currentNumber.ToString().PadLeft(numericLength, '0')}";

                // Verify uniqueness
                numberExists = await IsNumberExistsAsync(newNumber, numberSelector);
            } while (numberExists);

            return newNumber;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error generating number for {typeof(T).Name}", ex);
        }
    }

    private async Task<bool> IsNumberExistsAsync<T>(
        string number,
        Expression<Func<T, string>> numberSelector)
        where T : AuditableEntity<string>
    {
        var parameter = numberSelector.Parameters[0];
        var equality = Expression.Equal(numberSelector.Body, Expression.Constant(number));
        var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);

        return await _unitOfWork.Repository<T>()
            .Entities
            .AnyAsync(lambda);
    }
}