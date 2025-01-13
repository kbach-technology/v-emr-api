using System;
using System.Collections.Generic;

namespace EMR.Shared.Wrapper;

public class PaginatedResult<T> : Result
{
    public PaginatedResult(List<T> data)
    {
        Data = data;
    }

    public PaginatedResult(bool succeeded, List<T> data = default, List<string> messages = null, int count = 0,
        int page = 1, int pageSize = 10)
    {
        Data = data;
        CurrentPage = page;
        Succeeded = succeeded;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
    }

    public PaginatedResult(bool succeeded, List<T> data = default, List<string> messages = null, int count = 0,
        int todayCount = 0, int page = 1, int pageSize = 10)
    {
        Data = data;
        CurrentPage = page;
        Succeeded = succeeded;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        TodayCount = todayCount;
    }

    public List<T> Data { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }

    public int TodayCount { get; set; }
    public int PageSize { get; set; }

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;

    public static PaginatedResult<T> Failure(List<string> messages)
    {
        return new PaginatedResult<T>(false, default, messages, 0, 1, 10);
    }

    public static PaginatedResult<T> Success(List<T> data, int count, int page, int pageSize)
    {
        return new PaginatedResult<T>(true, data, null, count, page, pageSize);
    }
}