namespace RutaSV.Application.Common.DTO;

public sealed record PagedResponse<T>(
    IEnumerable<T> Data,
    int TotalRecords,
    int TotalPages,
    int CurrentPage,
    int PageSize)
{
    public static PagedResponse<T> Create(
        IEnumerable<T> data,
        int totalRecords,
        int currentPage,
        int pageSize)
    {
        int totalPages = pageSize > 0
            ? (int)Math.Ceiling(totalRecords / (double)pageSize)
            : 0;

        return new PagedResponse<T>(
            data,
            totalRecords,
            totalPages,
            currentPage,
            pageSize);
    }
}
