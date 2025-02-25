using EMR.Domain.Contracts;
using Microsoft.EntityFrameworkCore.Storage;

namespace EMR.Application.Interfaces.Repositories;

public interface IUnitOfWork<TId> : IDisposable, IAsyncDisposable
{
    IRepositoryAsync<T, TId> Repository<T>() where T : AuditableEntity<TId>;
    Task<int> Commit(CancellationToken cancellationToken);
    Task<int> CommitAndRemoveCache(CancellationToken cancellationToken, params string[] cacheKeys);
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    Task Rollback();
}