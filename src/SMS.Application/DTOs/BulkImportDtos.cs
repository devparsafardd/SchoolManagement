namespace SMS.Application.DTOs;

public class BulkImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<BulkImportError> Errors { get; set; } = new();
}

public class BulkImportError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = "";
    public string Message { get; set; } = "";
    public string? RowData { get; set; }
}
