using System;
using System.Linq;
using System.Threading.Tasks;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PureGIS_Geo_QC.Exports.Models;
using PureGIS_Geo_QC.Models;

namespace PureGIS_Geo_QC.Exports
{
    public class PdfSharpExporter : IReportExporter
    {
        public string FileExtension => ".pdf";
        public string FileFilter => "PDF 파일 (*.pdf)|*.pdf";
        public string ExporterName => "PdfSharpCore";

        public async Task<bool> ExportAsync(MultiFileReport multiReport, string filePath)
        {
            return await Task.Run(() => Export(multiReport, filePath));
        }

        public bool Export(MultiFileReport multiReport, string filePath)
        {
            try
            {
                var document = new PdfDocument();
                var page = document.AddPage();
                page.Orientation = PdfSharpCore.PageOrientation.Landscape;
                var graphics = XGraphics.FromPdfPage(page);

                var titleFont = new XFont("Malgun Gothic", 18, XFontStyle.Bold);
                var headerFont = new XFont("Malgun Gothic", 12, XFontStyle.Bold);
                var normalFont = new XFont("Malgun Gothic", 10);
                var smallFont = new XFont("Malgun Gothic", 8);

                double yPos = 40;
                double leftMargin = 40;
                double pageWidth = page.Width - 80;

                DrawCenteredText(graphics, $"[{multiReport.ProjectName}] SHP데이터 형식 결과 보고서", titleFont, XBrushes.DarkBlue, leftMargin, pageWidth, yPos);
                yPos += 35;
                graphics.DrawLine(new XPen(XColors.Gray, 1), leftMargin, yPos, leftMargin + pageWidth, yPos);
                yPos += 20;

                yPos = DrawOverallSummary(graphics, multiReport, headerFont, normalFont, leftMargin, yPos);
                yPos += 20;

                foreach (var reportData in multiReport.FileResults)
                {
                    if (yPos > page.Height - 200) // 페이지 여유 공간 확인
                    {
                        DrawFooter(graphics, smallFont, leftMargin, pageWidth, page.Height);
                        page = document.AddPage();
                        page.Orientation = PdfSharpCore.PageOrientation.Landscape;
                        graphics = XGraphics.FromPdfPage(page);
                        yPos = 40;
                    }
                    yPos = DrawFileDetailSection(graphics, reportData, headerFont, normalFont, smallFont, leftMargin, pageWidth, yPos, page.Height);
                    yPos += 25;
                }

                DrawFooter(graphics, smallFont, leftMargin, pageWidth, page.Height);

                graphics.Dispose();
                document.Save(filePath);
                document.Close();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PdfSharp Export Error: {ex.Message}");
                return false;
            }
        }

        private double DrawOverallSummary(XGraphics graphics, MultiFileReport multiReport, XFont headerFont, XFont normalFont, double leftMargin, double yPos)
        {
            graphics.DrawString("전체 검사 요약", headerFont, XBrushes.DarkBlue, leftMargin, yPos);
            yPos += 25;
            var infoItems = new[]
            {
                ("검사 실행일:", multiReport.ReportDate.ToString("yyyy년 MM월 dd일 HH시 mm분")),
                ("총 파일 수:", $"{multiReport.TotalFiles} 개"),
                ("전체 컬럼 수:", $"{multiReport.TotalColumns} 개"),
                ("전체 성공률:", multiReport.OverallSuccessRate)
            };
            foreach (var (label, value) in infoItems)
            {
                graphics.DrawString(label, normalFont, XBrushes.Black, leftMargin, yPos);
                graphics.DrawString(value, normalFont, XBrushes.Black, leftMargin + 120, yPos);
                yPos += 18;
            }
            return yPos;
        }

        private double DrawFileDetailSection(XGraphics graphics, ReportData reportData, XFont headerFont, XFont normalFont, XFont smallFont, double leftMargin, double pageWidth, double yPos, double pageHeight)
        {
            graphics.DrawString($"파일별 상세 결과: {reportData.FileName}", headerFont, XBrushes.DarkBlue, leftMargin, yPos);
            yPos += 25;

            // 요약 테이블
            var headers = new[] { "전체 필드", "정상", "오류", "정상률" };
            var data = new[] { reportData.TotalCount.ToString(), reportData.NormalCount.ToString(), reportData.ErrorCount.ToString(), reportData.SuccessRate };
            double colWidth = pageWidth / 4;
            var headerRect = new XRect(leftMargin, yPos, pageWidth, 20);
            graphics.DrawRectangle(XBrushes.LightGray, headerRect);
            for (int i = 0; i < headers.Length; i++)
            {
                var rect = new XRect(leftMargin + i * colWidth, yPos, colWidth, 20);
                DrawCenteredText(graphics, headers[i], normalFont, XBrushes.Black, rect);
            }
            yPos += 20;
            var dataRect = new XRect(leftMargin, yPos, pageWidth, 20);
            graphics.DrawRectangle(XPens.Black, dataRect);
            for (int i = 0; i < data.Length; i++)
            {
                var rect = new XRect(leftMargin + i * colWidth, yPos, colWidth, 20);
                var brush = i == 1 ? XBrushes.Green : i == 2 ? XBrushes.Red : XBrushes.Black;
                DrawCenteredText(graphics, data[i], normalFont, brush, rect);
            }
            yPos += 25;

            // 상세 결과 테이블
            var detailHeaders = new[] { "상태", "기준컬럼ID", "기준컬럼명", "기준타입", "기준길이", "찾은필드명", "파일타입", "파일길이", "비고" };
            var colWidths = new double[] { 40, 70, 90, 60, 50, 70, 60, 50, 90 };
            double xPos = leftMargin;
            var headerBgRect = new XRect(leftMargin, yPos, pageWidth, 18);
            graphics.DrawRectangle(XBrushes.LightGray, headerBgRect);
            for (int i = 0; i < detailHeaders.Length; i++)
            {
                var rect = new XRect(xPos, yPos, colWidths[i], 18);
                graphics.DrawRectangle(XPens.Black, rect);
                DrawCenteredText(graphics, detailHeaders[i], smallFont, XBrushes.Black, rect);
                xPos += colWidths[i];
            }
            yPos += 18;

            foreach (var result in reportData.ValidationResults)
            {
                if (yPos > pageHeight - 60) break; // 페이지 넘어가면 중단
                xPos = leftMargin;
                var rowData = new[] { result.Status ?? "", result.Std_ColumnId ?? "", result.Std_ColumnName ?? "", result.Std_Type ?? "", result.Std_Length ?? "", result.Found_FieldName ?? "", result.Cur_Type ?? "", result.Cur_Length ?? "", ReportData.GetRemarks(result) };
                for (int i = 0; i < rowData.Length; i++)
                {
                    var rect = new XRect(xPos, yPos, colWidths[i], 15);
                    graphics.DrawRectangle(XPens.Black, rect);
                    var brush = (i == 0 && result.Status == "오류") ? XBrushes.Red : XBrushes.Black;
                    DrawCenteredText(graphics, rowData[i], smallFont, brush, rect);
                    xPos += colWidths[i];
                }
                yPos += 15;
            }
            return yPos;
        }

        private void DrawFooter(XGraphics graphics, XFont smallFont, double leftMargin, double pageWidth, double pageHeight)
        {
            var footerText = $"보고서 생성: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | PureGIS GEO-QC v1.0 | {ExporterName}";
            var footerY = pageHeight - 30;
            graphics.DrawString(footerText, smallFont, XBrushes.Gray, leftMargin + pageWidth - 300, footerY);
        }

        private void DrawCenteredText(XGraphics graphics, string text, XFont font, XBrush brush, double left, double width, double y)
        {
            var rect = new XRect(left, y, width, font.Height);
            graphics.DrawString(text, font, brush, rect, XStringFormats.Center);
        }

        private void DrawCenteredText(XGraphics graphics, string text, XFont font, XBrush brush, XRect rect)
        {
            graphics.DrawString(text, font, brush, rect, XStringFormats.Center);
        }
    }
}