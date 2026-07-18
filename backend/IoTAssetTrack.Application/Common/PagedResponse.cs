namespace IoTAssetTrack.Application.Common;

public class PagedResponse<T>(IEnumerable<T> data, int totalRecords, int page, int pageSize)
{
    public IEnumerable<T> Data { get; set; } = data;
    public int TotalRecords { get; set; } = totalRecords;
    public int Page { get; set; } = page;
    public int PageSize { get; set; } = pageSize;
}
