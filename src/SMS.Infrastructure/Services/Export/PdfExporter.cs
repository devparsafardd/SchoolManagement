using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SMS.Application.Common.Export;

namespace SMS.Infrastructure.Services.Export;

public class PdfExporter : IPdfExporter
{
    private static readonly string _fontFamily = LoadFontFamily();
    private static string LoadFontFamily()
    {
        try
        {
            var fontPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "fonts", "Vazirmatn-Regular.ttf");
            if (File.Exists(fontPath))
            {
                QuestPDF.Drawing.FontManager.RegisterFont(File.OpenRead(fontPath));
                var bold = Path.Combine(AppContext.BaseDirectory, "wwwroot", "fonts", "Vazirmatn-Bold.ttf");
                if (File.Exists(bold))
                    QuestPDF.Drawing.FontManager.RegisterFont(File.OpenRead(bold));
                return "Vazirmatn";
            }
        }
        catch { }
        return "Tahoma";
    }

    public byte[] ExportTable(string title, IList<string> columns, IEnumerable<object?[]> rows, string? subtitle = null)
    {
        var rowList = rows.ToList();
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(_fontFamily));
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(title).FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                    if (!string.IsNullOrEmpty(subtitle))
                        col.Item().AlignCenter().Text(subtitle).FontSize(11).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        foreach (var _ in columns) c.RelativeColumn();
                    });

                    table.Header(h =>
                    {
                        foreach (var col in columns)
                            h.Cell().Background(Colors.Blue.Darken2).Padding(6).Text(col).Bold().FontColor(Colors.White);
                    });

                    int i = 0;
                    foreach (var row in rowList)
                    {
                        var bg = (i++ % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;
                        foreach (var cell in row)
                            table.Cell().Background(bg).Padding(5).Text(cell?.ToString() ?? "");
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("صفحه ").FontSize(9);
                    x.CurrentPageNumber().FontSize(9);
                    x.Span(" از ").FontSize(9);
                    x.TotalPages().FontSize(9);
                });
            });
        }).GeneratePdf();
    }
}
