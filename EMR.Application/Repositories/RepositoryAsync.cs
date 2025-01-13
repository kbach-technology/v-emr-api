using System.Collections.Generic;
using EMR.Application.Interfaces.Repositories;
using EMR.Domain.Contracts;
using EMR.Persistence.Contexts;

namespace EMR.Application.Repositories;

public class RepositoryAsync<T, TId>(AppDbContext dbContext) : IRepositoryAsync<T, TId>
    where T : AuditableEntity<TId>
{
    public IQueryable<T> Entities => dbContext.Set<T>();

    public async Task<T> AddAsync(T entity)
    {
        await dbContext.Set<T>().AddAsync(entity);
        return entity;
    }

    public Task DeleteAsync(T entity)
    {
        dbContext.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await dbContext
            .Set<T>()
            .ToListAsync();
    }

    public async Task<T> GetByIdAsync(TId id)
    {
        return await dbContext.Set<T>().FindAsync(id);
    }

    public async Task<List<T>> GetPagedResponseAsync(int pageNumber, int pageSize)
    {
        return await dbContext
            .Set<T>()
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public Task UpdateAsync(T entity)
    {
        var exist = dbContext.Set<T>().Find(entity.Id);
        if (exist != null) dbContext.Entry((object)exist).CurrentValues.SetValues(entity);
        return Task.CompletedTask;
    }
}