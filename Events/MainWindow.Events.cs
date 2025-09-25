// 파일 경로: MainWindow/Events/MainWindow.Events.cs

using System;
using System.Windows;
using System.Windows.Input;
using PureGIS_Geo_QC.Managers;
using PureGIS_Geo_QC.WPF;
using PureGIS_Geo_QC.Licensing;

namespace PureGIS_Geo_QC_Standalone
{
    public partial class MainWindow
    {
        #region 타이틀바 및 메뉴 이벤트 핸들러
        /// <summary>
        /// 타이틀바 드래그로 창 이동
        /// </summary>
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        /// <summary>
        /// 최소화 버튼 클릭
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 최대화/복원 버튼 클릭
        /// </summary>
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }
        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        // 메뉴 이벤트 핸들러 구현
        private void NewProjectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var result = CustomMessageBox.Show(this, "새 프로젝트", "새 프로젝트를 생성하시겠습니까?", true);
            if (result == true)
            {
                // TODO: 프로젝트명 입력 다이얼로그 표시
                var projectName = "이름 없는 프로젝트";
                CurrentProject = ProjectManager.CreateNewProject(projectName);
                CustomMessageBox.Show(this, "새 프로젝트", "새 프로젝트가 생성되었습니다. 상단의 입력란에 프로젝트명을 입력하고 저장하세요.");
            }
        }
        private void SaveProjectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentProject == null)
            {
                CustomMessageBox.Show(this, "오류", "저장할 프로젝트가 없습니다.");
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PureGIS 프로젝트 파일 (*.pgs)|*.pgs",
                DefaultExt = ".pgs"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ProjectManager.SaveProject(CurrentProject, saveFileDialog.FileName);
                    CustomMessageBox.Show(this, "완료", "프로젝트가 저장되었습니다.");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, "오류", ex.Message);
                }
            }
        }
        private void OpenProjectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "PureGIS 프로젝트 파일 (*.pgs)|*.pgs",
                DefaultExt = ".pgs"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    CurrentProject = ProjectManager.LoadProject(openFileDialog.FileName);
                    CustomMessageBox.Show(this, "완료", "프로젝트를 불러왔습니다.");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, "오류", ex.Message);
                }
            }
        }
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 종료 확인 메시지
            var result = CustomMessageBox.Show(this, "종료 확인", "프로그램을 종료하시겠습니까?", true);
            if (result == true)
            {
                this.Close();
            }
        }

        /// <summary>
        /// 프로그램 정보 메뉴 클릭
        /// </summary>
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var aboutWindow = new AboutWindow
                {
                    Owner = this // 부모 창 설정
                };

                // LicenseManager 인스턴스에서 현재 라이선스 정보 가져오기
                var licenseManager = LicenseManager.Instance;

                // AboutWindow의 상태 업데이트 메서드 호출
                aboutWindow.UpdateLicenseStatus(
                    this.IsTrialMode,
                    licenseManager.IsLicenseValid,
                    licenseManager.CompanyName, // LicenseManager에 CompanyName, ExpiryDate 속성 추가 필요
                    licenseManager.ExpiryDate
                );

                aboutWindow.ShowDialog(); // 모달 창으로 표시
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(this, "오류", $"정보 창을 여는 중 오류가 발생했습니다: {ex.Message}");
            }
        }
        /// <summary>
        /// Ctrl+V로 데이터 붙여넣기
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // ===== 👇 [수정] 탭 인덱스에 따라 다른 메서드를 호출하도록 변경 =====
                switch (MainTabControl.SelectedIndex)
                {
                    case 0: // 기준 정의 탭
                        PasteColumnsToCurrentTable();
                        break;
                    case 1: // 코드 관리 탭
                        PasteCodesToCurrentCodeSet();
                        break;
                }
                e.Handled = true;
            }
        }
        #endregion
    }
}