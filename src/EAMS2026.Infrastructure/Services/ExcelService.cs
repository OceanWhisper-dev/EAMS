using EAMS2026.Domain.DTOs.Erp.Report;
using OfficeOpenXml;
using System.Data;

namespace EAMS2026.Infrastructure.Services;

public class ExcelService
{
    public ExcelService()
    {
        ExcelPackage.License.SetNonCommercialOrganization("EAMS2026");
    }

    public byte[] GenerateExcel(DataTable data, string sheetName = "Sheet1")
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);
        worksheet.Cells["A1"].LoadFromDataTable(data, true);
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    public byte[] GenerateExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1") where T : class
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);
        worksheet.Cells["A1"].LoadFromCollection(data, true);
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    public DataTable ReadExcel(byte[] fileBytes, string sheetName = "Sheet1")
    {
        using var stream = new MemoryStream(fileBytes);
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets[sheetName] ?? package.Workbook.Worksheets[0];
        var dataTable = new DataTable();

        var headerRow = worksheet.Cells[1, 1, 1, worksheet.Dimension.Columns];
        foreach (var cell in headerRow)
        {
            dataTable.Columns.Add(cell.Text.Trim());
        }

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            var dataRow = dataTable.NewRow();
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                dataRow[col - 1] = worksheet.Cells[row, col].Text;
            }
            dataTable.Rows.Add(dataRow);
        }

        return dataTable;
    }

    public byte[] CreateTemplate(string[] columns, string sheetName = "Template")
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);
        for (int i = 0; i < columns.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = columns[i];
        }
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    /// <summary>
    /// 大数据量自动列宽阈值，超过此行数时跳过 AutoFit 以避免性能问题
    /// </summary>
    private const int AutoFitRowThreshold = 5000;

    /// <summary>
    /// 报表导出
    /// </summary>
    public byte[] ExportToExcel(List<ReportColumnDto> columns, List<Dictionary<string, object?>> rows, string sheetName = "Report")
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        // 写入表头
        for (int i = 0; i < columns.Count; i++)
        {
            worksheet.Cells[1, i + 1].Value = columns[i].Title;
        }

        // 写入数据行
        for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            var row = rows[rowIdx];
            for (int colIdx = 0; colIdx < columns.Count; colIdx++)
            {
                var field = columns[colIdx].Field;
                if (row.TryGetValue(field, out var val))
                {
                    worksheet.Cells[rowIdx + 2, colIdx + 1].Value = val;
                }
            }
        }

        // 自动列宽（大数据量时跳过以避免性能问题）
        if (worksheet.Dimension != null && rows.Count <= AutoFitRowThreshold)
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        return package.GetAsByteArray();
    }
}