namespace SMS.Application.Common.Export;

public interface IExcelExporter
{
    /// <summary>
    /// خروجی Excel از یک لیست با ستون‌های مشخص
    /// </summary>
    /// <param name="title">عنوان شیت و سرستون اصلی</param>
    /// <param name="columns">نام ستون‌ها به فارسی</param>
    /// <param name="rows">داده‌ها به ترتیب ستون‌ها</param>
    byte[] Export(string title, IList<string> columns, IEnumerable<object?[]> rows);
}
