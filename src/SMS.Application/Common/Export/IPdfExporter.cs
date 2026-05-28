namespace SMS.Application.Common.Export;

public interface IPdfExporter
{
    /// <summary>خروجی PDF از یک جدول ساده</summary>
    byte[] ExportTable(string title, IList<string> columns, IEnumerable<object?[]> rows, string? subtitle = null);
}
