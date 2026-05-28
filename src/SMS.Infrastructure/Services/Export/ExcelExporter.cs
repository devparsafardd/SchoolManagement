using ClosedXML.Excel;
using SMS.Application.Common.Export;

namespace SMS.Infrastructure.Services.Export;

public class ExcelExporter : IExcelExporter
{
    public byte[] Export(string title, IList<string> columns, IEnumerable<object?[]> rows)
    {
        using var wb = new XLWorkbook();
        var sheetName = SanitizeSheetName(title);
        var ws = wb.Worksheets.Add(sheetName);

        // راست به چپ
        ws.RightToLeft = true;

        // عنوان اصلی (مرج شده)
        var lastCol = Math.Max(1, columns.Count);
        var titleRange = ws.Range(1, 1, 1, lastCol).Merge();
        titleRange.Value = title;
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 14;
        titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2c3e50");
        titleRange.Style.Font.FontColor = XLColor.White;
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 28;

        // سرستون‌ها
        for (int i = 0; i < columns.Count; i++)
        {
            var cell = ws.Cell(2, i + 1);
            cell.Value = columns[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#34495e");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // داده‌ها
        int rowIdx = 3;
        foreach (var row in rows)
        {
            for (int i = 0; i < row.Length && i < columns.Count; i++)
            {
                var cell = ws.Cell(rowIdx, i + 1);
                SetCellValue(cell, row[i]);
            }
            rowIdx++;
        }

        // استایل کلی
        var dataRange = ws.Range(2, 1, Math.Max(2, rowIdx - 1), lastCol);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        if (value is null) { cell.Value = ""; return; }
        switch (value)
        {
            case string s: cell.Value = s; break;
            case int i: cell.Value = i; break;
            case long l: cell.Value = l; break;
            case decimal m: cell.Value = m; break;
            case double d: cell.Value = d; break;
            case bool b: cell.Value = b ? "بله" : "خیر"; break;
            case DateTime dt: cell.Value = dt; cell.Style.DateFormat.Format = "yyyy/MM/dd"; break;
            default: cell.Value = value.ToString(); break;
        }
    }

    private static string SanitizeSheetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Sheet1";
        var invalid = new[] { '\\', '/', '*', '?', ':', '[', ']' };
        foreach (var c in invalid) name = name.Replace(c, '_');
        return name.Length > 31 ? name[..31] : name;
    }
}
