using System.Collections.Generic;
using EMR.Domain.Contracts;

namespace EMR.Application.Interfaces.Repositories;

public interface IRepositoryAsync<T, in TId> where T : class, IEntity<TId>
{
    IQueryable<T> Entities { get; }

    Task<T> GetByIdAsync(TId id);

    Task<List<T>> GetAllAsync();

    Task<List<T>> GetPagedResponseAsync(int pageNumber, int pageSize);

    Task<T> AddAsync(T entity);

    Task<List<T>> AddRangeAsync(IEnumerable<T> entities);

    Task UpdateAsync(T entity);

    Task DeleteAsync(T entity);
}