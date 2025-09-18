using PureGIS_Geo_QC.Models; // Exports.Models가 아닌 Models 네임스페이스 사용
using System.Threading.Tasks;

namespace PureGIS_Geo_QC.Exports
{
    public interface IReportExporter
    {
        string FileExtension { get; }
        string FileFilter { get; }
        string ExporterName { get; }
        Task<bool> ExportAsync(MultiFileReport multiReport, string filePath);
        bool Export(MultiFileReport multiReport, string filePath);
    }
}