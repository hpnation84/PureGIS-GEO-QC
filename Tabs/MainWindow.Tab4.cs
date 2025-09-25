// 파일 경로: Tabs/MainWindow.Tab4.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PureGIS_Geo_QC.Models;
using PureGIS_Geo_QC.WPF;

namespace PureGIS_Geo_QC_Standalone
{
    public partial class MainWindow
    {
        // ======== 탭 4: 코드 관리 관련 메서드들 ========

        /// <summary>
        /// CodeSet 리스트뷰를 새로고침합니다.
        /// </summary>
        private void RefreshCodeSetList()
        {
            if (CurrentProject != null)
            {
                // 현재 선택된 항목 기억
                var selectedItem = CodeSetListView.SelectedItem;

                CodeSetListView.ItemsSource = null;
                CodeSetListView.ItemsSource = CurrentProject.CodeSets.OrderBy(c => c.CodeName);

                // 이전 선택 항목이 여전히 존재하면 다시 선택
                if (selectedItem != null && CurrentProject.CodeSets.Contains(selectedItem))
                {
                    CodeSetListView.SelectedItem = selectedItem;
                }
            }
        }

        /// <summary>
        /// 새 코드 그룹 추가 버튼 클릭
        /// </summary>
        private void AddCodeSetButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentProject == null)
            {
                CustomMessageBox.Show(this, "알림", "프로젝트를 먼저 생성하거나 불러오세요.");
                return;
            }

            var dialog = new InputDialog("새 코드 그룹 이름 입력", "예: FTR_CDE");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                string newCodeName = dialog.InputText.Trim();
                if (string.IsNullOrEmpty(newCodeName)) return;

                if (CurrentProject.CodeSets.Any(c => c.CodeName.Equals(newCodeName, StringComparison.OrdinalIgnoreCase)))
                {
                    CustomMessageBox.Show(this, "오류", "이미 존재하는 코드 그룹 이름입니다.");
                    return;
                }

                CurrentProject.CodeSets.Add(new CodeSet { CodeName = newCodeName });
                RefreshCodeSetList();
            }
        }

        /// <summary>
        /// 선택된 코드 그룹 이름 수정
        /// </summary>
        private void EditCodeSetButton_Click(object sender, RoutedEventArgs e)
        {
            if (CodeSetListView.SelectedItem is CodeSet selectedCodeSet)
            {
                var dialog = new InputDialog("코드 그룹 이름 수정", selectedCodeSet.CodeName);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true)
                {
                    string newCodeName = dialog.InputText.Trim();
                    if (string.IsNullOrEmpty(newCodeName)) return;

                    if (CurrentProject.CodeSets.Any(c => c.CodeName.Equals(newCodeName, StringComparison.OrdinalIgnoreCase) && c != selectedCodeSet))
                    {
                        CustomMessageBox.Show(this, "오류", "이미 존재하는 코드 그룹 이름입니다.");
                        return;
                    }
                    selectedCodeSet.CodeName = newCodeName;
                    RefreshCodeSetList();
                }
            }
            else
            {
                CustomMessageBox.Show(this, "알림", "수정할 코드 그룹을 선택하세요.");
            }
        }
        /// <summary>
        /// 선택된 코드 그룹 삭제
        /// </summary>
        private void DeleteCodeSetButton_Click(object sender, RoutedEventArgs e)
        {
            if (CodeSetListView.SelectedItem is CodeSet selectedCodeSet)
            {
                if (CustomMessageBox.Show(this, "삭제 확인", $"'{selectedCodeSet.CodeName}' 코드 그룹을 삭제하시겠습니까?", true) == true)
                {
                    CurrentProject.CodeSets.Remove(selectedCodeSet);
                    RefreshCodeSetList();
                    CodeDataGrid.ItemsSource = null;
                    SelectedCodeSetHeader.Text = "코드 그룹을 선택하세요";
                }
            }
            else
            {
                CustomMessageBox.Show(this, "알림", "삭제할 코드 그룹을 선택하세요.");
            }
        }
        /// <summary>
        /// 코드 그룹 선택 시, 해당 그룹의 코드 목록을 DataGrid에 표시
        /// </summary>
        private void CodeSetListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CodeSetListView.SelectedItem is CodeSet selectedCodeSet)
            {
                SelectedCodeSetHeader.Text = $"'{selectedCodeSet.CodeName}' 코드 목록";
                // ===== 👇 [수정] OrderBy를 제거하여 BindingList가 직접 연결되도록 합니다. =====
                CodeDataGrid.ItemsSource = selectedCodeSet.Codes;
            }
            else
            {
                SelectedCodeSetHeader.Text = "코드 그룹을 선택하세요";
                CodeDataGrid.ItemsSource = null;
            }
        }
        /// <summary>
        /// 현재 선택된 코드 그룹에 클립보드 데이터 붙여넣기
        /// </summary>
        private void PasteCodesToCurrentCodeSet()
        {
            if (CodeSetListView.SelectedItem is CodeSet selectedCodeSet)
            {
                try
                {
                    string clipboardText = Clipboard.GetText();
                    if (string.IsNullOrWhiteSpace(clipboardText)) return;

                    var newCodes = ParseCodesFromClipboard(clipboardText);

                    if (newCodes.Count > 0)
                    {
                        foreach (var newCode in newCodes)
                        {
                            // 중복 코드 확인
                            if (!selectedCodeSet.Codes.Any(c => c.Code.Equals(newCode.Code, StringComparison.OrdinalIgnoreCase)))
                            {
                                selectedCodeSet.Codes.Add(newCode);
                            }
                        }
                        // BindingList는 자동으로 UI를 업데이트하므로 별도의 새로고침 코드가 필요 없습니다.
                        CustomMessageBox.Show(this, "완료", $"{newCodes.Count}개의 코드가 붙여넣어졌습니다.");
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, "붙여넣기 오류", $"데이터 붙여넣기 중 오류 발생: {ex.Message}");
                }
            }
            else
            {
                CustomMessageBox.Show(this, "알림", "코드값을 추가할 코드 그룹을 먼저 선택하세요.");
            }
        }

        /// <summary>
        /// 클립보드 텍스트를 파싱하여 CodeValue 리스트로 변환
        /// </summary>
        private List<CodeValue> ParseCodesFromClipboard(string clipboardText)
        {
            var codeValues = new List<CodeValue>();
            var lines = clipboardText.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = line.Split('\t'); // 탭으로 열 구분
                if (cols.Length >= 1)
                {
                    codeValues.Add(new CodeValue
                    {
                        Code = cols[0].Trim(),
                        Description = cols.Length > 1 ? cols[1].Trim() : ""
                    });
                }
            }
            return codeValues;
        }

        /// <summary>
        /// 현재 선택된 코드 그룹에 새 코드값 추가
        /// </summary>
        private void AddCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (CodeSetListView.SelectedItem is CodeSet selectedCodeSet)
            {
                var codeDialog = new InputDialog("새 코드 입력 (예: ADD001)", "");
                codeDialog.Owner = this;
                if (codeDialog.ShowDialog() != true) return;
                string newCode = codeDialog.InputText.Trim();

                var descDialog = new InputDialog("코드의 한글명 입력 (예: 맨홀)", "");
                descDialog.Owner = this;
                if (descDialog.ShowDialog() != true) return;
                string newDesc = descDialog.InputText.Trim();

                if (string.IsNullOrEmpty(newCode)) return;

                if (selectedCodeSet.Codes.Any(c => c.Code.Equals(newCode, StringComparison.OrdinalIgnoreCase)))
                {
                    CustomMessageBox.Show(this, "오류", "이미 존재하는 코드입니다.");
                    return;
                }

                selectedCodeSet.Codes.Add(new CodeValue { Code = newCode, Description = newDesc });
                CodeDataGrid.ItemsSource = null;
                CodeDataGrid.ItemsSource = selectedCodeSet.Codes.OrderBy(c => c.Code);
            }
            else
            {
                CustomMessageBox.Show(this, "알림", "먼저 코드 그룹을 선택하세요.");
            }
        }
        /// <summary>
        /// 선택된 코드값 수정
        /// </summary>
        private void EditCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (CodeSetListView.SelectedItem is CodeSet selectedCodeSet && CodeDataGrid.SelectedItem is CodeValue selectedCodeValue)
            {
                var codeDialog = new InputDialog("코드 수정", selectedCodeValue.Code);
                codeDialog.Owner = this;
                if (codeDialog.ShowDialog() != true) return;
                string newCode = codeDialog.InputText.Trim();

                var descDialog = new InputDialog("코드명(한글) 수정", selectedCodeValue.Description);
                descDialog.Owner = this;
                if (descDialog.ShowDialog() != true) return;
                string newDesc = descDialog.InputText.Trim();

                if (string.IsNullOrEmpty(newCode)) return;

                if (selectedCodeSet.Codes.Any(c => c.Code.Equals(newCode, StringComparison.OrdinalIgnoreCase) && c != selectedCodeValue))
                {
                    CustomMessageBox.Show(this, "오류", "수정하려는 코드가 이미 존재합니다.");
                    return;
                }

                selectedCodeValue.Code = newCode;
                selectedCodeValue.Description = newDesc;

                CodeDataGrid.ItemsSource = null;
                CodeDataGrid.ItemsSource = selectedCodeSet.Codes.OrderBy(c => c.Code);
            }
            else
            {
                CustomMessageBox.Show(this, "알림", "수정할 코드를 선택하세요.");
            }
        }

        /// <summary>
        /// 선택된 코드값 삭제
        /// </summary>
        private void DeleteCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (CodeSetListView.SelectedItem is CodeSet selectedCodeSet && CodeDataGrid.SelectedItem is CodeValue selectedCodeValue)
            {
                if (CustomMessageBox.Show(this, "삭제 확인", $"'{selectedCodeValue.Code}' 코드를 삭제하시겠습니까?", true) == true)
                {
                    selectedCodeSet.Codes.Remove(selectedCodeValue);
                    CodeDataGrid.ItemsSource = null;
                    CodeDataGrid.ItemsSource = selectedCodeSet.Codes.OrderBy(c => c.Code);
                }
            }
            else
            {
                CustomMessageBox.Show(this, "알림", "삭제할 코드를 선택하세요.");
            }
        }        
    }
}