using System.Collections.Concurrent;
using EMR.Application.Interfaces.Repositories;
using EMR.Domain.Contracts;
using EMR.Domain.Enums;
using EMR.Persistence.Contexts;
using EMR.Shared.Interfaces;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Repositories;

public class UnitOfWork<TId> : IUnitOfWork<TId>
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppCache _cache;
    private readonly ILogger<UnitOfWork<TId>> _logger;
    private readonly ConcurrentDictionary<string, object> _repositories;
    private bool _disposed;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(
        AppDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppCache cache,
        ILogger<UnitOfWork<TId>> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositories = new ConcurrentDictionary<string, object>();
    }

    public IRepositoryAsync<TEntity, TId> Repository<TEntity>() where TEntity : AuditableEntity<TId>
    {
        var type = typeof(TEntity).Name;

        return (IRepositoryAsync<TEntity, TId>)_repositories.GetOrAdd(type, _ =>
        {
            var repositoryType = typeof(RepositoryAsync<,>);
            return Activator.CreateInstance(
                repositoryType.MakeGenericType(typeof(TEntity), typeof(TId)),
                _dbContext)!;
        });
    }

    public async Task<int> Commit(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dbContext.SaveChangesAsync(cancellationToken);
            return result > 0 ? (int)ChangeStatus.Changed : (int)ChangeStatus.UnChanged;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error during commit operation");
            throw;
        }
    }

    public async Task<int> CommitAndRemoveCache(CancellationToken cancellationToken, params string[] cacheKeys)
    {
        var result = await Commit(cancellationToken);

        foreach (var cacheKey in cacheKeys)
        {
            try
            {
                _cache.Remove(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove cache key: {CacheKey}", cacheKey);
            }
        }

        return result;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            return _currentTransaction;
        }

        _currentTransaction = await _dbContext.Database.BeginTransactionAsync();
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
            }
        }
        catch (Exception)
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task Rollback()
    {
        _dbContext.ChangeTracker.Clear();
        if (_currentTransaction != null)
        {
            await RollbackTransactionAsync();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
        }

        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _currentTransaction?.Dispose();
            _dbContext?.Dispose();
        }

        _disposed = true;
    }
}