namespace EMR.Application.Extensions;

public static class UnitOfWorkExtensions
{
    public static async Task CommitAndThrowOnFailure<TId>(
        this IUnitOfWork<TId> unitOfWork,
        CancellationToken cancellationToken)
    {
        if (unitOfWork == null)
            throw new ArgumentNullException(nameof(unitOfWork));

        var result = await unitOfWork.Commit(cancellationToken);
        if (result <= 0)
            throw new ApplicationException("Failed to save changes to database");
    }
}