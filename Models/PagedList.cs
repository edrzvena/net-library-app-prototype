namespace LibraryAppPrototype.Models;

// Bukan DTO — cuma bentuk data yang tidak punya tabel (PRD 3.4).
public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static PagedList<T> Empty(int page, int pageSize) =>
        new() { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
}
