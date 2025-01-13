using System.Collections;
using EMR.Application.Interfaces.Repositories;
using EMR.Domain.Contracts;
using EMR.Domain.Enums;
using EMR.Persistence.Contexts;
using EMR.Shared.Interfaces;
using LazyCache;

namespace EMR.Application.Repositories;

public class UnitOfWork<TId>(AppDbContext dbContext, ICurrentUserService currentUserService, IAppCache cache)
    : IUnitOfWork<TId>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private Hashtable _repositories;
    private bool disposed;

    public IRepositoryAsync<TEntity, TId> Repository<TEntity>() where TEntity : AuditableEntity<TId>
    {
        if (_repositories == null)
            _repositories = new Hashtable();

        var type = typeof(TEntity).Name;

        if (!_repositories.ContainsKey(type))
        {
            var repositoryType = typeof(RepositoryAsync<,>);

            var repositoryInstance =
                Activator.CreateInstance(repositoryType.MakeGenericType(typeof(TEntity), typeof(TId)), _dbContext);

            _repositories.Add(type, repositoryInstance);
        }

        return (IRepositoryAsync<TEntity, TId>)_repositories[type];
    }

    public async Task<int> Commit(CancellationToken cancellationToken)
    {
        var result = await _dbContext.SaveChangesAsync(cancellationToken);
        result = result switch
        {
            > 0 => (int)ChangeStatus.Changed,
            _ => (int)ChangeStatus.UnChanged
        };
        return result;
    }

    public async Task<int> CommitAndRemoveCache(CancellationToken cancellationToken, params string[] cacheKeys)
    {
        var result = await _dbContext.SaveChangesAsync(cancellationToken);
        foreach (var cacheKey in cacheKeys) cache.Remove(cacheKey);
        return result;
    }

    public Task Rollback()
    {
        _dbContext.ChangeTracker.Entries().ToList().ForEach(x => x.Reload());
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
            if (disposing)
                //dispose managed resources
                _dbContext.Dispose();
        //dispose unmanaged resources
        disposed = true;
    }
}