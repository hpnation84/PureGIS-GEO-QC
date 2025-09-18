using System;
using System.IO;
using System.Threading.Tasks;
using PureGIS_Geo_QC.Exports.Models;
using PureGIS_Geo_QC.Models;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PureGIS_Geo_QC.Exports
{
    public class QuestPdfExporter : IReportExporter
    {
        public string FileExtension => ".pdf";
        public string FileFilter => "PDF 파일 (*.pdf)|*.pdf";
        public string ExporterName => "QuestPDF";

        static QuestPdfExporter()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            try
            {
                string fontPath = "C:/Windows/Fonts/malgun.ttf";
                if (File.Exists(fontPath))
                {
                    FontManager.RegisterFont(File.OpenRead(fontPath));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"폰트 등록 오류: {ex.Message}");
            }
        }

        public async Task<bool> ExportAsync(MultiFileReport multiReport, string filePath)
        {
            return await Task.Run(() => Export(multiReport, filePath));
        }

        public bool Export(MultiFileReport multiReport, string filePath)
        {
            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontFamily("Malgun Gothic"));

                        page.Header().Element(header => Header(header, multiReport.ProjectName));
                        page.Content().Element(content => Content(content, multiReport));
                        page.Footer().Element(Footer);
                    });
                }).GeneratePdf(filePath);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"QuestPDF Export Error: {ex.Message}");
                return false;
            }
        }

        private void Header(IContainer container, string projectName)
        {
            container.Column(column =>
            {
                column.Item().Text($"[{projectName}] SHP데이터 형식 결과 보고서")
                    .FontSize(20).Bold().FontColor(Colors.Blue.Darken1).AlignCenter();
                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });
        }

        private void Content(IContainer container, MultiFileReport multiReport)
        {
            container.PaddingTop(20).Column(column =>
            {
                column.Item().Element(content => OverallSummary(content, multiReport));
                column.Item().PaddingTop(20);

                foreach (var reportData in multiReport.FileResults)
                {
                    // ✨ 오류 수정: .KeepTogether()를 삭제하고 바로 .Element()를 호출합니다.
                    // 페이지 나뉨은 자연스럽게 처리하도록 둡니다.
                    column.Item().Element(content => FileDetailSection(content, reportData));
                    column.Item().PaddingTop(20);
                }
            });
        }

        private void OverallSummary(IContainer container, MultiFileReport multiReport)
        {
            container.Column(column =>
            {
                column.Item().Text("전체 검사 요약").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(10);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns => { columns.RelativeColumn(2); columns.RelativeColumn(5); });
                    AddInfoRow(table, "검사 실행일", multiReport.ReportDate.ToString("yyyy년 MM월 dd일 HH시 mm분"));
                    AddInfoRow(table, "총 파일 수", $"{multiReport.TotalFiles} 개");
                    AddInfoRow(table, "전체 컬럼 수", $"{multiReport.TotalColumns} 개");
                    AddInfoRow(table, "전체 성공률", multiReport.OverallSuccessRate);
                });
            });
        }

        private void FileDetailSection(IContainer container, ReportData reportData)
        {
            container.Column(column =>
            {
                column.Item().Text($"파일별 상세 결과: {reportData.FileName}").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(10);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); });
                    table.Header(header => { header.Cell().Element(HeaderCellStyle).Text("전체 필드"); header.Cell().Element(HeaderCellStyle).Text("정상"); header.Cell().Element(HeaderCellStyle).Text("오류"); header.Cell().Element(HeaderCellStyle).Text("정상률"); });
                    table.Cell().Element(DataCellStyle).Text(reportData.TotalCount.ToString());
                    table.Cell().Element(DataCellStyle).Text(reportData.NormalCount.ToString()).FontColor(Colors.Green.Medium);
                    table.Cell().Element(DataCellStyle).Text(reportData.ErrorCount.ToString()).FontColor(Colors.Red.Medium);
                    table.Cell().Element(DataCellStyle).Text(reportData.SuccessRate);
                });
                column.Item().PaddingTop(10);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns => { columns.RelativeColumn(0.8f); columns.RelativeColumn(1.2f); columns.RelativeColumn(1.5f); columns.RelativeColumn(1.0f); columns.RelativeColumn(0.8f); columns.RelativeColumn(1.2f); columns.RelativeColumn(1.0f); columns.RelativeColumn(0.8f); columns.RelativeColumn(1.5f); });
                    table.Header(header => { header.Cell().Element(HeaderCellStyle).Text("상태"); header.Cell().Element(HeaderCellStyle).Text("기준컬럼ID"); header.Cell().Element(HeaderCellStyle).Text("기준컬럼명"); header.Cell().Element(HeaderCellStyle).Text("기준타입"); header.Cell().Element(HeaderCellStyle).Text("기준길이"); header.Cell().Element(HeaderCellStyle).Text("찾은필드명"); header.Cell().Element(HeaderCellStyle).Text("파일타입"); header.Cell().Element(HeaderCellStyle).Text("파일길이"); header.Cell().Element(HeaderCellStyle).Text("비고"); });
                    foreach (var result in reportData.ValidationResults)
                    {
                        var statusColor = result.Status == "정상" ? Colors.Green.Medium : Colors.Red.Medium;
                        table.Cell().Element(DataCellStyle).Text(result.Status ?? "").FontColor(statusColor);
                        table.Cell().Element(DataCellStyle).Text(result.Std_ColumnId ?? "");
                        table.Cell().Element(DataCellStyle).Text(result.Std_ColumnName ?? "");
                        table.Cell().Element(DataCellStyle).Text(result.Std_Type ?? "");
                        table.Cell().Element(DataCellStyle).Text(result.Std_Length ?? "");
                        table.Cell().Element(DataCellStyle).Text(result.Found_FieldName ?? "");
                        table.Cell().Element(DataCellStyle).Text(result.Cur_Type ?? "");
                        table.Cell().Element(DataCellStyle).Text(result.Cur_Length ?? "");
                        table.Cell().Element(DataCellStyle).Text(ReportData.GetRemarks(result));
                    }
                });
            });
        }

        private void Footer(IContainer container)
        {
            container.AlignRight().Text($"보고서 생성: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | PureGIS GEO-QC v1.0 | {ExporterName}")
            .FontSize(10).FontColor(Colors.Grey.Medium);
        }

        private void AddInfoRow(TableDescriptor table, string label, string value)
        {
            table.Cell().Element(InfoLabelCellStyle).Text(label);
            table.Cell().Element(InfoValueCellStyle).Text(value);
        }

        private IContainer HeaderCellStyle(IContainer container) => container.DefaultTextStyle(x => x.FontSize(10).Bold().FontColor(Colors.White)).Background(Colors.Blue.Medium).Padding(6).AlignCenter();
        private IContainer DataCellStyle(IContainer container) => container.DefaultTextStyle(x => x.FontSize(9)).Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).AlignCenter();
        private IContainer InfoLabelCellStyle(IContainer container) => container.DefaultTextStyle(x => x.FontSize(11).Bold()).Background(Colors.Grey.Lighten3).Padding(8).BorderBottom(0.5f).BorderColor(Colors.Grey.Medium);
        private IContainer InfoValueCellStyle(IContainer container) => container.DefaultTextStyle(x => x.FontSize(11)).Padding(8).BorderBottom(0.5f).BorderColor(Colors.Grey.Medium);
    }
}