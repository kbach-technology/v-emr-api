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
        const int maxRetries = 10;
        int attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                // Get the last number with proper ordering
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

                var newNumber = $"{prefix}{currentNumber.ToString().PadLeft(numericLength, '0')}";

                // Verify uniqueness before returning
                var exists = await IsNumberExistsAsync(newNumber, numberSelector);
                if (!exists)
                {
                    return newNumber;
                }

                // If the number exists, it means another thread grabbed it
                // Add a small random delay and retry
                attempt++;
                if (attempt < maxRetries)
                {
                    await Task.Delay(Random.Shared.Next(10, 50));
                }
            }
            catch (Exception ex) when (attempt < maxRetries - 1)
            {
                // If we hit a database error (like deadlock), retry with exponential backoff
                attempt++;
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 50));
            }
        }

        throw new ApplicationException(
            $"Failed to generate unique number for {typeof(T).Name} after {maxRetries} attempts. " +
            "This may indicate high concurrency or a configuration issue.");
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