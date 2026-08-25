namespace TableFlow.Api.DTOs
{
    public record PagedResult<T>
    (
        IReadOnlyList<T> Items,
        int PageNumber,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasNextPage
    );
}
