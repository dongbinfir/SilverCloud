using ClosedXML.Excel;
using User.Application.Common.Interfaces;
using User.Domain.Attributes;
using User.Domain.Enums;

namespace User.Infrastructure.ExternalServices.ExcelExportServices.Services
{
    public class ExcelExportService : IExcelExportService
    {
        public byte[] Export<T>(IEnumerable<T> data, ExcelUseFormat format, string reportName)
        {
            return format switch
            {
                ExcelUseFormat.ClosedXML => ExportWithClosedXML(data),
                ExcelUseFormat.MiniExcel => ExportWithMiniExcel(data),
                ExcelUseFormat.NPOI => ExportWithNPOI(data),
                _ => throw new NotImplementedException(),
            };
        }

        private byte[] ExportWithClosedXML<T>(IEnumerable<T> data)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sheet1");
            var dataList = data.ToList();

            // 1️ 获取属性 + Attribute 信息
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .Select(p => new
                {
                    Property = p,
                    Attr = p.GetCustomAttributes(typeof(ExcelColumnAttribute), true)
                            .FirstOrDefault() as ExcelColumnAttribute
                })
                .Where(x => x.Attr?.Ignore != true) // 支持忽略
                .OrderBy(x => x.Attr?.Order ?? int.MaxValue) // 优先用 Order
                .ThenBy(x => x.Property.MetadataToken) // 再按声明顺序
                .ToList();

            // 2 写表头
            for (int col = 0; col < properties.Count; col++)
            {
                var headerName = properties[col].Attr?.Name ?? properties[col].Property.Name;

                var cell = worksheet.Cell(1, col + 1);
                cell.Value = headerName;

                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // 3️ 写数据（可控格式）
            for (int row = 0; row < dataList.Count; row++)
            {
                var item = dataList[row];

                for (int col = 0; col < properties.Count; col++)
                {
                    var value = properties[col].Property.GetValue(item);
                    var cell = worksheet.Cell(row + 2, col + 1);

                    if (value == null)
                    {
                        cell.Value = "";
                        continue;
                    }

                    // ⭐ 类型控制（关键）
                    switch (value)
                    {
                        case DateTime dt:
                            cell.Value = dt;
                            cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                            break;

                        case Enum e:
                            cell.Value = e.ToString();
                            break;

                        default:
                            cell.Value = (XLCellValue)value;
                            break;
                    }
                }
            }

            // 4️ 美化
            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            var range = worksheet.Range(1, 1, dataList.Count + 1, properties.Count);
            range.CreateTable();

            // 5️ 输出
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        private byte[] ExportWithMiniExcel<T>(IEnumerable<T> data)
        {
            return new byte[0];
        }

        private byte[] ExportWithNPOI<T>(IEnumerable<T> data)
        {
            return new byte[0];
        }
    }
}
