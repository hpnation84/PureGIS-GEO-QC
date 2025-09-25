using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DotSpatial.Data;
using Microsoft.Win32;
using PdfSharpCore.Fonts;

using PureGIS_Geo_QC.Licensing;
using PureGIS_Geo_QC.Managers;
using PureGIS_Geo_QC.Models;
using PureGIS_Geo_QC.WPF;

// 이름 충돌을 피하기 위한 using 별칭(alias) 사용
using TableDefinition = PureGIS_Geo_QC.Models.TableDefinition;

namespace PureGIS_Geo_QC_Standalone
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        // ✨ 1. 체험판 모드인지 저장할 변수 추가
        private bool IsTrialMode = false;
        private List<TableDefinition> standardTables = new List<TableDefinition>();
        // 다중 파일 관리를 위한 변수들
        private List<Shapefile> loadedShapefiles = new List<Shapefile>();
        private Shapefile currentSelectedFile = null;
        private MultiFileReport multiFileReport = new MultiFileReport();
        private ProjectDefinition currentProject = null;
        private TableDefinition currentSelectedTable = null;
        public List<string> ColumnTypes { get; } = new List<string> { "VARCHAR2", "NUMBER", "DATE" };
        public MainWindow()
        {
            // =======================================================
            // ✨ PdfSharpCore 폰트 리졸버를 전역으로 설정
            // =======================================================
            GlobalFontSettings.FontResolver = new FontResolver();
            InitializeComponent();
            this.DataContext = this;

            // 전달받은 값으로 체험판 모드 설정
        //    this.IsTrialMode = isTrial;

            // 창 제목에 체험판 표시
            if (this.IsTrialMode)
            {
                this.Title += " (체험판)";
            }
        }
        // MainWindow가 로드될 때 실행될 이벤트 핸들러를 추가합니다.
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CheckLicense();
            await CheckForUpdatesAsync(); // 업데이트 확인 함수 호출
        }

        /// <summary>
        /// 프로그램 업데이트를 확인하고 사용자에게 알리는 비동기 메서드
        /// </summary>
        private async Task CheckForUpdatesAsync()
        {
            try
            {
                VersionInfo latestVersionInfo = await UpdateManager.CheckForUpdatesAsync();
                if (latestVersionInfo == null) return; // 서버에서 정보를 못가져오면 조용히 종료

                Version currentVersion = UpdateManager.GetCurrentVersion();
                Version latestVersion = new Version(latestVersionInfo.LatestVersion);

                // 현재 버전과 최신 버전을 비교합니다.
                if (latestVersion > currentVersion)
                {
                    // 새 버전이 있으면 사용자에게 알림창을 띄웁니다.
                    string message = $"새로운 버전({latestVersionInfo.LatestVersion})이 있습니다. 지금 업데이트하시겠습니까?\n\n";
                    message += "릴리즈 노트:\n" + latestVersionInfo.ReleaseNotes;

                    if (CustomMessageBox.Show(this, "업데이트 알림", message, true) == true)
                    {
                        // '확인'을 누르면 다운로드 URL을 기본 웹 브라우저로 엽니다.
                        Process.Start(new ProcessStartInfo(latestVersionInfo.DownloadUrl) { UseShellExecute = true });

                        // 프로그램을 종료하여 사용자가 설치를 진행하도록 유도
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                // 예외가 발생하더라도 프로그램 실행에 영향을 주지 않도록 처리
                System.Diagnostics.Debug.WriteLine($"업데이트 프로세스 오류: {ex.Message}");
            }
        }
        // 라이선스를 확인하고 로그인 창을 띄우는 메서드를 추가합니다.
        private void CheckLicense()
        {
            // 향후 라이선스 정보를 로컬에 저장하고 유효성을 검사하는 로직을 추가할 수 있습니다.
            // 지금은 매번 로그인 창을 띄웁니다.
            var loginWindow = new LicenseLoginWindow
            {
                Owner = this // 로그인 창이 MainWindow 중앙에 오도록 설정
            };

            bool? isAuthenticated = loginWindow.ShowDialog();

            if (isAuthenticated == true)
            {
                // 인증에 성공했거나 체험판을 선택한 경우
                this.IsTrialMode = loginWindow.IsTrialMode;

                // 체험판 모드일 경우 창 제목 변경
                if (this.IsTrialMode)
                {
                    this.Title += " (체험판)";
                }
            }
            else
            {
                // 사용자가 인증 없이 창을 닫은 경우
                CustomMessageBox.Show(this, "알림", "라이선스 인증이 필요합니다. 프로그램을 종료합니다.");
                this.Close(); // MainWindow를 닫습니다.
            }
        }
        // =======================================================
        // ✨ PdfSharpCore 폰트 리졸버 구현을 위한 내부 클래스 추가
        // =======================================================
        public class FontResolver : IFontResolver
        {
            // =======================================================
            // ✨ 이 속성을 추가하여 오류를 해결합니다.
            // IFontResolver 인터페이스는 기본 폰트 이름을 지정하는 속성을 요구합니다.
            // =======================================================
            public string DefaultFontName => "Malgun Gothic";

            public byte[] GetFont(string faceName)
            {
                // 'faceName'에 따라 다른 폰트 파일을 반환할 수 있습니다.
                // 여기서는 'Malgun Gothic' 폰트 파일 경로를 사용합니다.
                // 대부분의 Windows 시스템에 해당 경로에 폰트가 있습니다.

                // 폰트 파일 경로는 대소문자를 구분하지 않도록 수정합니다.
                string fontPath = "C:/Windows/Fonts/malgun.ttf";
                if (faceName.Contains("Bold")) // 굵은 글꼴 요청 시
                {
                    fontPath = "C:/Windows/Fonts/malgunbd.ttf";
                }

                return File.ReadAllBytes(fontPath);
            }

            public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
            {
                // 폰트 패밀리 이름으로 폰트 파일을 매핑합니다.
                if (familyName.Equals("Malgun Gothic", StringComparison.OrdinalIgnoreCase))
                {
                    // PdfSharpCore에게 이 폰트 패밀리의 이름을 알려줍니다.
                    // GetFont 메서드에서 이 이름을 사용할 수 있습니다.
                    if (isBold)
                    {
                        // 굵은 글꼴일 경우 "Malgun Gothic Bold"로 구분
                        return new FontResolverInfo("Malgun Gothic Bold");
                    }

                    return new FontResolverInfo("Malgun Gothic");
                }

                // 지정된 폰트가 없으면 기본값 반환
                return null;
            }
        }

        public ProjectDefinition CurrentProject
        {
            get => currentProject;
            private set
            {
                currentProject = value;
                UpdateProjectUI();
            }
        }
        /// <summary>
        /// 테이블 목록 업데이트 (Null 안전 버전)
        /// </summary>
        private void UpdateTableList()
        {
            try
            {
                if (ProjectTreeView != null && CurrentProject != null)
                {
                    ProjectTreeView.ItemsSource = null;
                    ProjectTreeView.ItemsSource = CurrentProject.Categories;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateTableList 오류: {ex.Message}");
            }
        }
        // =================== 탭 3 로직 (검사 실행) ===================        
        #region Validation Methods
        private void ValidateFile(Shapefile shapefile, TableDefinition standardTable)
        {
            var results = new List<ColumnValidationResult>();
            try
            {
                foreach (var stdCol in standardTable.Columns)
                {
                    var resultRow = new ColumnValidationResult
                    {
                        // ✅ 추가: 컬럼ID 저장
                        Std_ColumnId = stdCol.ColumnId,

                        Std_ColumnName = stdCol.ColumnName,
                        Std_Type = stdCol.Type,
                        Std_Length = stdCol.Length,
                    };

                    // ❌ 기존: stdCol.ColumnName으로 찾았음
                    // ✅ 수정: stdCol.ColumnId로 변경
                    if (!shapefile.DataTable.Columns.Contains(stdCol.ColumnId))
                    {
                        resultRow.Status = "오류";
                        // ✅ 추가: 찾은 필드명과 존재 여부 설정
                        resultRow.Found_FieldName = "없음";
                        resultRow.IsFieldFound = false;

                        resultRow.Cur_Type = "없음";
                        resultRow.Cur_Length = "없음";
                        resultRow.IsTypeCorrect = false;
                        resultRow.IsLengthCorrect = false;
                        results.Add(resultRow);
                        continue;
                    }
                    // ❌ 기존: stdCol.ColumnName으로 필드 정보 가져왔음
                    // ✅ 수정: stdCol.ColumnId로 변경
                    var (curTypeName, curPrecision, curScale) = GetDbfFieldInfo(shapefile, stdCol.ColumnId);

                    // ✅ 추가: 찾은 필드명과 존재 여부 설정
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

        }             
        #endregion

        /// <summary>
        /// DBF 파일에서 필드의 타입을 추출
        /// </summary>
        private string GetDbfFieldType(Shapefile shapefile, string fieldName)
        {
            try
            {
                // DotSpatial에서 DBF 파일의 필드 정보에 접근하는 방법
                // DataTable의 컬럼 타입을 확인
                var column = shapefile.DataTable.Columns[fieldName];
                if (column != null)
                {
                    Type colType = column.DataType;
                    if (colType == typeof(double) || colType == typeof(float) || colType == typeof(decimal))
                    {
                        return "NUMBER";
                    }
                    else if (colType == typeof(int) || colType == typeof(long))
                    {
                        return "NUMBER";
                    }
                    else if (colType == typeof(string))
                    {
                        return "VARCHAR2";
                    }
                    else if (colType == typeof(DateTime))
                    {
                        return "DATE";
                    }
                }
                return "VARCHAR2"; // 기본값
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        /// <summary>
        /// DBF 파일에서 필드의 길이 정보를 추출
        /// </summary>
        private string GetDbfFieldLength(Shapefile shapefile, string fieldName)
        {
            try
            {
                // DataTable의 컬럼을 DotSpatial.Data.Field로 캐스팅
                var field = shapefile.DataTable.Columns[fieldName] as DotSpatial.Data.Field;
                if (field != null)
                {
                    if (field.DecimalCount > 0)
                        return $"{field.Length},{field.DecimalCount}"; // 예: "10,2"
                    else
                        return field.Length.ToString(); // 예: "50"
                }

                // Fallback: 일반 DataColumn 처리
                var column = shapefile.DataTable.Columns[fieldName];
                if (column != null && column.DataType == typeof(string))
                {
                    return column.MaxLength > 0 ? column.MaxLength.ToString() : "255";
                }

                return "UNKNOWN";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetDbfFieldLength 오류: {ex.Message}");
                return "ERROR";
            }
        }         
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // LicenseManager 인스턴스를 통해 비동기적으로 로그아웃을 호출합니다.
            await LicenseManager.Instance.LogoutAsync();
        }
    }
}
