// 파일 경로: MainWindow/Tabs/MainWindow.Validation.cs

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using DotSpatial.Data;
using Microsoft.Win32;

using PureGIS_Geo_QC.Exports;
using PureGIS_Geo_QC.Exports.Models;
using PureGIS_Geo_QC.Models;
using PureGIS_Geo_QC.WPF;
// ...

namespace PureGIS_Geo_QC_Standalone
{
    public partial class MainWindow
    {
        // ======== 탭 2 & 3: 파일 검사 및 결과 관련 메서드들 ========
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Shapefiles (*.shp)|*.shp",
                Multiselect = true // 여러 파일 선택 허용
            };

            if (openFileDialog.ShowDialog() != true) return;

            foreach (string filePath in openFileDialog.FileNames)
            {
                try
                {
                    if (Shapefile.OpenFile(filePath) is Shapefile shapefile)
                    {
                        // 중복 추가 방지
                        if (!loadedShapefiles.Any(f => f.Filename.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                        {
                            loadedShapefiles.Add(shapefile);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, "파일 열기 오류", $"{System.IO.Path.GetFileName(filePath)} 파일을 여는 중 오류:\n{ex.Message}");
                }
            }
            UpdateFileListBox(); // ListBox UI 업데이트
        }
        /// <summary>
        /// 파일 목록 ListBox를 업데이트합니다.
        /// </summary>
        private void UpdateFileListBox()
        {
            FileListBox.ItemsSource = null;
            FileListBox.ItemsSource = loadedShapefiles.Select(f => System.IO.Path.GetFileName(f.Filename)).ToList();
        }
        /// <summary>
        /// ListBox에서 파일을 선택하면 해당 파일의 컬럼 정보를 DataGrid에 표시합니다.
        /// </summary>
        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileListBox.SelectedItem is string fileName)
            {
                var selectedShapefile = loadedShapefiles.FirstOrDefault(f => System.IO.Path.GetFileName(f.Filename) == fileName);
                if (selectedShapefile != null)
                {
                    var columnInfoList = new List<FileColumnInfo>();
                    foreach (DataColumn col in selectedShapefile.DataTable.Columns)
                    {
                        var (typeName, precision, scale) = GetDbfFieldInfo(selectedShapefile, col.ColumnName);
                        columnInfoList.Add(new FileColumnInfo
                        {
                            ColumnName = col.ColumnName,
                            DataType = new TypeInfo { Name = typeName },
                            MaxLength = scale > 0 ? $"{precision},{scale}" : precision.ToString()
                        });
                    }
                    LoadedFileGrid.ItemsSource = columnInfoList;
                }
            }
            else
            {
                LoadedFileGrid.ItemsSource = null;
            }
        }
        /// <summary>
        /// 다중 순차 검사 로직
        /// </summary>
        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            // ✨ 4. 체험판 제한 로직 추가
            if (IsTrialMode && loadedShapefiles.Count > 2)
            {
                CustomMessageBox.Show(this, "체험판 제한", "체험판에서는 최대 2개의 파일만 검사할 수 있습니다.\n\n정식 라이선스는 jindigo.kr에서 구매하실 수 있습니다.");
                return; // 검사 중단
            }

            if (loadedShapefiles.Count == 0)
            {
                CustomMessageBox.Show(this, "오류", "검사할 파일을 먼저 불러와주세요.");
                return;
            }
            if (CurrentProject == null)
            {
                CustomMessageBox.Show(this, "오류", "프로젝트를 먼저 생성하거나 불러오세요.");
                return;
            }

            multiFileReport = new MultiFileReport { ProjectName = CurrentProject.ProjectName };
            int validatedCount = 0;
            int skippedCount = 0;

            foreach (var shapefile in loadedShapefiles)
            {
                string fileId = System.IO.Path.GetFileNameWithoutExtension(shapefile.Filename);
                TableDefinition standardTable = null;

                foreach (var category in CurrentProject.Categories)
                {
                    standardTable = category.Tables.FirstOrDefault(t => t.TableId.Equals(fileId, StringComparison.OrdinalIgnoreCase));
                    if (standardTable != null) break;
                }

                if (standardTable == null)
                {
                    skippedCount++;
                    continue;
                }

                var validationResults = ValidateSingleFile(shapefile, standardTable);
                multiFileReport.FileResults.Add(new ReportData
                {
                    FileName = System.IO.Path.GetFileName(shapefile.Filename),
                    ProjectName = CurrentProject.ProjectName,
                    ValidationResults = validationResults
                });
                validatedCount++;
            }

            ResultTreeView.ItemsSource = multiFileReport.FileResults;
            MainTabControl.SelectedIndex = 2;

            string summary = $"총 {loadedShapefiles.Count}개 파일 중 {validatedCount}개 검사 완료.";
            if (skippedCount > 0)
            {
                summary += $"\n{skippedCount}개 파일은 일치하는 기준 테이블이 없어 건너뛰었습니다.";
            }
            CustomMessageBox.Show(this, "검사 완료", summary);
        }
        /// <summary>
        /// 결과 TreeView에서 파일 선택 시 해당 파일의 상세 결과를 DataGrid에 표시
        /// </summary>
        private void ResultTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (e.NewValue is ReportData selectedReportData)
                {
                    // 선택된 파일의 상세 결과를 DataGrid에 바인딩
                    ResultGrid.ItemsSource = selectedReportData.ValidationResults;

                    // 헤더 업데이트
                    if (SelectedFileHeader != null)
                    {
                        string headerText = $"📊 {selectedReportData.FileName} 상세 결과 " +
                                          $"(정상: {selectedReportData.NormalCount}/{selectedReportData.TotalCount} | " +
                                          $"성공률: {selectedReportData.SuccessRate})";
                        SelectedFileHeader.Text = headerText;
                    }
                }
                else if (e.NewValue is ColumnValidationResult)
                {
                    // 개별 컬럼 선택 시에는 아무 동작 안함 (TreeView에서 컬럼 클릭해도 DataGrid는 변경되지 않음)
                    return;
                }
                else
                {
                    // 아무것도 선택되지 않았을 때
                    ResultGrid.ItemsSource = null;
                    if (SelectedFileHeader != null)
                    {
                        SelectedFileHeader.Text = "파일을 선택하여 상세 결과를 확인하세요";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ResultTreeView_SelectedItemChanged 오류: {ex.Message}");
                if (SelectedFileHeader != null)
                {
                    SelectedFileHeader.Text = "결과 표시 중 오류가 발생했습니다";
                }
            }
        }
        /// <summary>
        /// PdfSharp로 내보내기
        /// </summary>
        private void ExportPdfSharpButton_Click(object sender, RoutedEventArgs e)
        {
            ExportReport(new PdfSharpExporter());
        }

        /// <summary>
        /// Word로 내보내기
        /// </summary>
        private void ExportToWordButton_Click(object sender, RoutedEventArgs e)
        {
            ExportReport(new WordExporter());
        }
        /// <summary>
        /// 통합 내보내기 메서드 (MultiFileReport 사용)
        /// </summary>
        private void ExportReport(IReportExporter exporter)
        {
            try
            {
                if (multiFileReport.FileResults.Count == 0)
                {
                    CustomMessageBox.Show(this, "알림", "내보낼 검사 결과가 없습니다.");
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = exporter.FileFilter,
                    DefaultExt = exporter.FileExtension,
                    FileName = $"GeoQC_Report_{DateTime.Now:yyyyMMdd_HHmmss}{exporter.FileExtension}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // multiFileReport 객체를 직접 넘겨줍니다.
                    bool success = exporter.Export(multiFileReport, saveFileDialog.FileName);

                    if (success)
                    {
                        CustomMessageBox.Show(this, "완료",
                            $"{exporter.ExporterName} 보고서를 생성했습니다.\n\n" +
                            $"파일: {saveFileDialog.FileName}");
                    }
                    else
                    {
                        CustomMessageBox.Show(this, "오류",
                            $"{exporter.ExporterName} 보고서 생성에 실패했습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(this, "오류",
                    $"보고서 내보내기 중 오류가 발생했습니다:\n\n" +
                    $"내보내기 방식: {exporter.ExporterName}\n" +
                    $"오류: {ex.Message}");
            }
        }


        // ======== 검증 및 헬퍼 메서드들 ========

        /// <summary>
        /// 단일 파일 검사 후 결과를 List로 반환하는 메서드
        /// </summary>
        private List<ColumnValidationResult> ValidateSingleFile(Shapefile shapefile, TableDefinition standardTable)
        {
            var results = new List<ColumnValidationResult>();
            try
            {
                foreach (var stdCol in standardTable.Columns)
                {
                    var resultRow = new ColumnValidationResult
                    {
                        Std_ColumnId = stdCol.ColumnId,
                        Std_ColumnName = stdCol.ColumnName,
                        Std_Type = stdCol.Type,
                        Std_Length = stdCol.Length,
                    };

                    if (!shapefile.DataTable.Columns.Contains(stdCol.ColumnId))
                    {
                        resultRow.Status = "오류";
                        resultRow.Found_FieldName = "없음";
                        resultRow.IsFieldFound = false;
                        resultRow.Cur_Type = "없음";
                        resultRow.Cur_Length = "없음";
                        resultRow.IsTypeCorrect = false;
                        resultRow.IsLengthCorrect = false;
                        results.Add(resultRow);
                        continue;
                    }

                    var (curTypeName, curPrecision, curScale) = GetDbfFieldInfo(shapefile, stdCol.ColumnId);
                    resultRow.Found_FieldName = stdCol.ColumnId;
                    resultRow.IsFieldFound = true;
                    resultRow.Cur_Type = curTypeName;
                    resultRow.Cur_Length = curScale > 0 ? $"{curPrecision},{curScale}" : curPrecision.ToString();

                    if (stdCol.Type.Equals("VARCHAR2", StringComparison.OrdinalIgnoreCase))
                    {
                        resultRow.IsTypeCorrect = curTypeName.Equals("Character", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (stdCol.Type.Equals("NUMBER", StringComparison.OrdinalIgnoreCase))
                    {
                        resultRow.IsTypeCorrect = curTypeName.Equals("Numeric", StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        resultRow.IsTypeCorrect = stdCol.Type.Equals(curTypeName, StringComparison.OrdinalIgnoreCase);
                    }

                    if (resultRow.IsTypeCorrect)
                    {
                        var (stdPrecision, stdScale) = ParseStandardLength(stdCol.Length);
                        if (stdCol.Type.Equals("VARCHAR2", StringComparison.OrdinalIgnoreCase))
                        {
                            resultRow.IsLengthCorrect = (stdPrecision == curPrecision);
                        }
                        else if (stdCol.Type.Equals("NUMBER", StringComparison.OrdinalIgnoreCase))
                        {
                            resultRow.IsLengthCorrect = (stdPrecision == curPrecision && stdScale == curScale);
                        }
                        else
                        {
                            resultRow.IsLengthCorrect = true;
                        }
                    }
                    else
                    {
                        resultRow.IsLengthCorrect = false;
                    }

                    resultRow.Status = (resultRow.IsTypeCorrect && resultRow.IsLengthCorrect) ? "정상" : "오류";
                    results.Add(resultRow);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(this, "검사 오류", $"파일 검사 중 오류가 발생했습니다: {ex.Message}");
            }
            return results;
        }
        /// <summary>
        /// *** 최종 수정된 헬퍼 메서드 (DotSpatial v2.0 호환) ***
        /// DataTable의 DataColumn을 DotSpatial.Data.Field로 캐스팅하여 상세 정보를 추출합니다.
        /// </summary>
        private (string TypeName, int Precision, int Scale) GetDbfFieldInfo(Shapefile shapefile, string fieldName)
        {
            try
            {
                var column = shapefile.DataTable.Columns[fieldName];

                if (column is DotSpatial.Data.Field field)
                {
                    string typeName = "Unknown";

                    // .NET 데이터 타입을 직접 비교하는 방식으로 변경
                    Type dotnetType = field.DataType;

                    if (dotnetType == typeof(string))
                    {
                        typeName = "Character";
                    }
                    else if (dotnetType == typeof(double) || dotnetType == typeof(float) || dotnetType == typeof(decimal) ||
                             dotnetType == typeof(int) || dotnetType == typeof(long) || dotnetType == typeof(short) || dotnetType == typeof(byte))
                    {
                        typeName = "Numeric";
                    }
                    else if (dotnetType == typeof(DateTime))
                    {
                        typeName = "Date";
                    }
                    else if (dotnetType == typeof(bool))
                    {
                        typeName = "Logical";
                    }

                    return (typeName, field.Length, field.DecimalCount);
                }

                return ("Not a DBF Field", 0, 0);
            }
            catch
            {
                return ("Error", 0, 0);
            }
        }

        /// <summary>
        /// *** 4. 새로운 헬퍼 메서드 2 ***
        /// 기준 정의의 길이 문자열(예: "50", "9,0", "7,2")을 분석하여 (전체 자릿수, 소수점 자릿수)로 변환합니다.
        /// </summary>
        private (int Precision, int Scale) ParseStandardLength(string lengthString)
        {
            if (string.IsNullOrWhiteSpace(lengthString)) return (0, 0);
            // 쉼표가 포함되어 있는지 확인
            if (lengthString.Contains(","))
            {
                var parts = lengthString.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int precision) && int.TryParse(parts[1], out int scale))
                {
                    // 쉼표 앞은 전체 자릿수, 뒤는 소수점 자릿수로 변환
                    return (precision, scale);
                }
            }
            else // 쉼표가 없으면
            {
                if (int.TryParse(lengthString, out int precision))
                {
                    // 전체를 자릿수로 취급하고 소수점은 0으로 처리
                    return (precision, 0);
                }
            }
            return (0, 0);
        }
        /// <summary>
        /// 배열에서 안전하게 값을 가져오는 헬퍼 함수
        /// </summary>
        private string GetSafeArrayValue(string[] array, int index, string defaultValue)
        {
            if (array != null && index >= 0 && index < array.Length)
            {
                return string.IsNullOrWhiteSpace(array[index]) ? defaultValue : array[index].Trim();
            }
            return defaultValue;
        }
        
    }
}