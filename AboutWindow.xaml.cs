using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics; // Process.Start를 위해 필요
using System.Windows.Navigation; // RequestNavigateEventArgs를 위해 필요

namespace PureGIS_Geo_QC.WPF
{
    /// <summary>
    /// AboutWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LoadVersionInfo();
        }

        /// <summary>
        /// 버전 정보를 동적으로 로드
        /// </summary>
        private void LoadVersionInfo()
        {
            try
            {
                // 어셈블리 정보 가져오기
                Assembly assembly = Assembly.GetExecutingAssembly();
                Version version = assembly.GetName().Version;

                // 버전 정보를 찾아서 업데이트 (XAML에서 TextBlock의 Name 속성이 필요한 경우)
                // 현재는 하드코딩되어 있지만 필요시 동적으로 변경 가능

                // 빌드 날짜 계산 (어셈블리 생성 시간 기반)
                DateTime buildDate = GetBuildDate(assembly);

                // UI 업데이트는 필요시 여기서 수행
                // 예: VersionTextBlock.Text = version.ToString();
                // 예: BuildDateTextBlock.Text = buildDate.ToString("yyyy.MM.dd");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"버전 정보 로드 오류: {ex.Message}");
            }
        }
        /// <summary>
        /// 라이선스 상태를 받아서 UI를 업데이트하는 메서드
        /// </summary>
        public void UpdateLicenseStatus(bool isTrial, bool isAuthenticated, string companyName, string expiryDate)
        {
            if (isTrial)
            {
                LicenseStatusText.Text = "라이선스: 체험판";
                LicenseStatusText.Foreground = new SolidColorBrush(Color.FromRgb(243, 156, 18)); // 주황색 계열
            }
            else if (isAuthenticated)
            {
                LicenseStatusText.Text = $"라이선스: 정식 ({companyName} / 만료일: {expiryDate})";
                LicenseStatusText.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113)); // 녹색 계열
            }
            // 미인증 상태는 XAML의 기본값으로 유지됩니다.
        }
        /// <summary>
        /// 어셈블리 빌드 날짜 계산
        /// </summary>
        private DateTime GetBuildDate(Assembly assembly)
        {
            try
            {
                // 어셈블리 파일의 생성 시간 반환
                return System.IO.File.GetCreationTime(assembly.Location);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        /// <summary>
        /// 타이틀바 드래그로 창 이동
        /// </summary>
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try
                {
                    this.DragMove();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"창 이동 오류: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        /// <summary>
        /// XAML의 Hyperlink 태그에서 호출하는 이벤트 핸들러입니다.
        /// </summary>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // 기본 브라우저로 링크를 엽니다.
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true; // 이벤트 처리를 완료했음을 알립니다.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"링크를 여는 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// ESC 키로 창 닫기
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            base.OnKeyDown(e);
        }
    }
}