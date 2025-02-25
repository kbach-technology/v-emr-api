using System.Collections.Generic;
using System.Reflection;
using EMR.Application.Exceptions;
using EMR.Application.Specifications.Base;
using EMR.Domain.Contracts;

namespace EMR.Application.Extensions;

public static class QueryableExtensions
{
    public static async Task<PaginatedResult<T>> ToPaginatedListAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) where T : class
    {
        if (source == null)
            throw new ApiException("Source cannot be null");

        pageNumber = Math.Max(1, pageNumber);
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<T>.Success(items, count, pageNumber, pageSize);
    }

    public static IQueryable<T> Specify<T>(
        this IQueryable<T> query,
        ISpecification<T> spec) where T : class, IEntity
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));
        if (spec == null)
            throw new ArgumentNullException(nameof(spec));

        var queryWithIncludes = spec.Includes
            .Aggregate(query, (current, include) => current.Include(include));

        var queryWithIncludeStrings = spec.IncludeStrings
            .Aggregate(queryWithIncludes, (current, include) => current.Include(include));

        return queryWithIncludeStrings.Where(spec.Criteria);
    }

    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        return condition ? query.Where(predicate) : query;
    }


    // How to use ApplyOrdering:
    // var query = _unitOfWork.Repository<Device>().Entities
    //     .ApplyOrdering(request.OrderBy, request.Ascending);
    public static IQueryable<T> ApplyOrdering<T>(
        this IQueryable<T> query,
        string? orderBy,
        bool ascending = true)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
            return query;

        var property =
            typeof(T).GetProperty(orderBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
            return query;

        var parameter = Expression.Parameter(typeof(T));
        var propertyAccess = Expression.Property(parameter, property);
        var lambda = Expression.Lambda(propertyAccess, parameter);

        var methodName = ascending ? "OrderBy" : "OrderByDescending";
        var method = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), property.PropertyType);

        return (IQueryable<T>)method.Invoke(null, new object[] { query, lambda })!;
    }


    // How to use ApplyFilter:
    // var query = _unitOfWork.Repository<Device>().Entities
    //     .ApplyFilter(x => x.UserId == _currentUserService.UserId)
    //     .ApplyFilter(x => x.Platform == request.Platform);
    public static IQueryable<T> ApplyFilter<T>(
        this IQueryable<T> query,
        Expression<Func<T, bool>>? filter)
    {
        return filter == null ? query : query.Where(filter);
    }

    public static async Task<bool> AnyWithNoLockAsync<T>(
        this IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        return await query
            .TagWith("WITH (NOLOCK)")
            .AnyAsync(cancellationToken);
    }


    // How to use:
    // var query = _unitOfWork.Repository<Device>().Entities
    //     .IncludeMultiple(x => x.User, x => x.DeviceType)
    //     .Where(x => x.UserId == _currentUserService.UserId);
    public static IQueryable<T> IncludeMultiple<T>(
        this IQueryable<T> query,
        params Expression<Func<T, object>>[] includes) where T : class
    {
        return includes.Aggregate(query, (current, include) => current.Include(include));
    }


    // How to use ToListWithNoLockAsync:
    // var query = _unitOfWork.Repository<Device>().Entities
    //     .Where(x => x.UserId == _currentUserService.UserId);
    // var devices = await query.ToListWithNoLockAsync();
    public static async Task<List<T>> ToListWithNoLockAsync<T>(
        this IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        return await query
            .TagWith("WITH (NOLOCK)")
            .ToListAsync(cancellationToken);
    }

    public static IQueryable<T> WithPagination<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize)
    {
        return query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }


    // How to use WhereIfNotNull:
    // var query = _unitOfWork.Repository<Device>().Entities
    //     .WhereIfNotNull(request.Platform, x => x.Platform == request.Platform)
    public static IQueryable<T> WhereIfNotNull<T>(
        this IQueryable<T> query,
        object? value,
        Expression<Func<T, bool>> predicate)
    {
        return value == null ? query : query.Where(predicate);
    }


    // How to use WhereIfNotNull:
    // var query = _unitOfWork.Repository<Device>().Entities
    //     .WhereIfNotNull(request.Platform, x => x.Platform == request.Platform)
    public static IQueryable<T> WhereIfNotNull<T>(
        this IQueryable<T> query,
        string? value,
        Expression<Func<T, bool>> predicate)
    {
        return string.IsNullOrWhiteSpace(value) ? query : query.Where(predicate);
    }
}