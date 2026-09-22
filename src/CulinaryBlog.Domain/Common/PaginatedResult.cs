namespace CulinaryBlog.Domain.Common;

public sealed class PaginatedResult<T>
{
    public PaginatedResult(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    public IReadOnlyList<T> Items { get; }

    public int TotalCount { get; }

    public int PageIndex { get; }

    public int PageSize { get; }

    public int TotalPages { get; }
}