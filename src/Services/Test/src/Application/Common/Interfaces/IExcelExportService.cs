using User.Domain.Enums;

namespace User.Application.Common.Interfaces
{
    public interface IExcelExportService
    {
        byte[] Export<T>(IEnumerable<T> data, ExcelUseFormat format, string reportName);
    }
}
