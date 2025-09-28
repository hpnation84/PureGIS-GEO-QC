using DocumentFormat.OpenXml.Spreadsheet;
using DotSpatial.Data;
using Microsoft.Win32;
using PdfSharpCore.Fonts;
using PureGIS_Geo_QC.Helpers;
using PureGIS_Geo_QC.Licensing;
using PureGIS_Geo_QC.Managers;
using PureGIS_Geo_QC.Models;
using PureGIS_Geo_QC.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using LicenseManager = PureGIS_Geo_QC.Licensing.LicenseManager;
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
        
        // 다중 파일 관리를 위한 변수들
        private List<Shapefile> loadedShapefiles = new List<Shapefile>();        
        private MultiFileReport multiFileReport = new MultiFileReport();
        private ProjectDefinition currentProject = null;
        private TableDefinition currentSelectedTable = null;
        // 1. 매개변수가 없는 기본 생성자는 이 생성자를 호출하도록 수정합니다.
        public MainWindow() : this(false) // 기본적으로는 체험판이 아닌 상태로 시작
        {
        }

        public MainWindow(bool isTrial)
        {
            // =======================================================
            // ✨ PdfSharpCore 폰트 리졸버를 전역으로 설정
            // =======================================================
            GlobalFontSettings.FontResolver = new FontResolver();
            InitializeComponent();
            this.DataContext = this;

            // 전달받은 값으로 체험판 모드 설정
            this.IsTrialMode = isTrial;

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
        /// <summary>
        /// DataGrid에 엑셀 스타일의 붙여넣기를 처리하는 범용 메서드 (안정성 최종 강화 버전)
        /// </summary>
        /// <param name="grid">붙여넣기를 적용할 DataGrid</param>
        private void HandleExcelPaste(DataGrid grid)
        {
            // 붙여넣기할 데이터 소스가 있는지 먼저 확인합니다.
            if (!(grid.ItemsSource is IBindingList bindingList)) return;

            try
            {
                var clipboardData = ClipboardHelper.ParseClipboardGridData(Clipboard.GetText());
                if (clipboardData.Count == 0) return;

                // 1. 그리드가 비어있거나 셀 선택이 없을 때를 모두 고려하여 시작 위치를 안전하게 계산합니다.
                int startRowIndex = grid.CurrentCell != null ? grid.Items.IndexOf(grid.CurrentCell.Item) : 0;
                if (startRowIndex < 0) startRowIndex = grid.Items.Count > 0 ? grid.Items.Count - 1 : 0;

                int startColumnIndex = grid.CurrentCell != null ? grid.CurrentCell.Column.DisplayIndex : 0;

                for (int r = 0; r < clipboardData.Count; r++)
                {
                    int targetRowIndex = startRowIndex + r;
                    object dataItem;

                    // 2. 행이 부족하면 새로 추가하고, 그 결과(newItem)를 직접 사용합니다.
                    if (targetRowIndex >= grid.Items.Count)
                    {
                        // IBindingList.AddNew()는 새로 추가된 객체를 반환해 줍니다.
                        dataItem = bindingList.AddNew();
                        if (dataItem == null) break; // 객체 생성 실패 시 중단
                    }
                    else
                    {
                        dataItem = grid.Items[targetRowIndex];
                    }

                    // "NewItemPlaceholder" 같은 임시 객체는 건너뜁니다.
                    if (dataItem == CollectionView.NewItemPlaceholder) continue;

                    var clipboardRow = clipboardData[r];

                    for (int c = 0; c < clipboardRow.Length; c++)
                    {
                        int targetColumnIndex = startColumnIndex + c;
                        if (targetColumnIndex >= grid.Columns.Count) continue;

                        var column = grid.Columns[targetColumnIndex];
                        var cellValue = clipboardRow[c];

                        if (column is DataGridBoundColumn boundColumn)
                        {
                            var bindingPath = (boundColumn.Binding as Binding)?.Path.Path;
                            if (string.IsNullOrEmpty(bindingPath)) continue;

                            var property = dataItem.GetType().GetProperty(bindingPath);
                            if (property != null && property.CanWrite)
                            {
                                try
                                {
                                    var convertedValue = Convert.ChangeType(cellValue, property.PropertyType);
                                    property.SetValue(dataItem, convertedValue, null);
                                }
                                catch (FormatException) { continue; } // 타입 변환 오류 시 해당 셀은 무시
                            }
                        }
                    }
                }
                grid.Items.Refresh(); // UI 강제 새로고침
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(this, "붙여넣기 오류", $"데이터를 붙여넣는 중 오류가 발생했습니다:\n{ex.Message}");
            }
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // LicenseManager 인스턴스를 통해 비동기적으로 로그아웃을 호출합니다.
            await LicenseManager.Instance.LogoutAsync();
        }        
    }
}
