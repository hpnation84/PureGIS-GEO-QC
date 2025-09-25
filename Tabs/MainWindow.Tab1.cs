// 파일 경로: MainWindow/Tabs/MainWindow.Tab1.cs

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DocumentFormat.OpenXml.Spreadsheet;
using PureGIS_Geo_QC.Models; // 네임스페이스는 실제 프로젝트에 맞게 조정하세요.
using PureGIS_Geo_QC.WPF;
using PureGIS_Geo_QC.Helpers;
using ColumnDefinition = PureGIS_Geo_QC.Models.ColumnDefinition;

namespace PureGIS_Geo_QC_Standalone
{
    public partial class MainWindow
    {
        // ======== 탭 1: 기준 정의 관련 메서드들 ========

        /// <summary>
        /// 프로젝트 변경 시 UI 업데이트
        /// </summary>
        private void UpdateProjectUI()
        {
            try
            {
                if (CurrentProject == null)
                {
                    // 프로젝트가 없을 때
                    ProjectTreeView.ItemsSource = null;
                    this.Title = "PureGIS Geo-QC";
                    return;
                }

                // 프로젝트가 로드되었을 때
                this.Title = $"PureGIS Geo-QC - {CurrentProject.ProjectName}";
                ProjectNameTextBox.Text = CurrentProject.ProjectName;

                // TreeView에 카테고리 구조로 바인딩
                ProjectTreeView.ItemsSource = CurrentProject.Categories;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateProjectUI 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// ✨ 1. 프로젝트 이름 저장 버튼 클릭 이벤트
        /// </summary>
        private void SaveProjectNameButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentProject == null)
            {
                CustomMessageBox.Show(this, "오류", "먼저 프로젝트를 생성하거나 불러오세요.");
                return;
            }
            if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
            {
                CustomMessageBox.Show(this, "오류", "프로젝트명을 입력하세요.");
                return;
            }

            CurrentProject.ProjectName = ProjectNameTextBox.Text.Trim();
            UpdateProjectUI(); // 창 제목 등 UI 업데이트
            CustomMessageBox.Show(this, "완료", "프로젝트명이 저장되었습니다.");
        }
        // 2. TreeView 선택 변경 이벤트 핸들러 추가 (XAML에서 참조하고 있지만 구현되지 않음)
        private void ProjectTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (e.NewValue is TableDefinition selectedTable)
                {
                    currentSelectedTable = selectedTable;
                    StandardGrid.ItemsSource = selectedTable.Columns;
                    ShowTableInfo(selectedTable);
                }
                else
                {
                    currentSelectedTable = null;
                    StandardGrid.ItemsSource = null;
                    HideTableInfo();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProjectTreeView_SelectedItemChanged 오류: {ex.Message}");
                CustomMessageBox.Show(this, "오류", $"테이블 선택 중 오류가 발생했습니다: {ex.Message}");
            }
        }
        /// <summary>
        /// 새 카테고리 추가 버튼 클릭
        /// </summary>
        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentProject == null)
            {
                CustomMessageBox.Show(this, "오류", "프로젝트를 먼저 생성하거나 불러오세요.");
                return;
            }

            var dialog = new InputDialog("새 분류 이름을 입력하세요.", "새 분류");
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                var newCategory = new InfrastructureCategory
                {
                    CategoryId = "CATE_" + DateTime.Now.ToString("HHmmss"),
                    CategoryName = dialog.InputText
                };
                CurrentProject.Categories.Add(newCategory);
                UpdateTreeView();
            }
        }
        /// <summary>
        /// 선택된 카테고리 이름 수정 버튼 클릭
        /// </summary>
        private void EditCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectTreeView.SelectedItem is InfrastructureCategory selectedCategory)
            {
                var dialog = new InputDialog("분류 이름을 수정하세요.", selectedCategory.CategoryName);
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    selectedCategory.CategoryName = dialog.InputText;
                    UpdateTreeView(); // 이름 변경을 TreeView에 즉시 반영
                }
            }
            else
            {
                CustomMessageBox.Show(this, "알림", "수정할 카테고리를 먼저 선택하세요.");
            }
        }
        /// <summary>
        /// 선택된 카테고리 삭제 버튼 클릭
        /// </summary>
        private void DeleteCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectTreeView.SelectedItem is InfrastructureCategory selectedCategory)
            {
                string message = $"'{selectedCategory.CategoryName}' 분류를 삭제하시겠습니까?";
                if (selectedCategory.Tables.Any())
                {
                    message += "\n\n⚠️ 경고: 이 분류에 포함된 모든 테이블도 함께 삭제됩니다!";
                }

                if (CustomMessageBox.Show(this, "분류 삭제", message, true) == true)
                {
                    CurrentProject.Categories.Remove(selectedCategory);
                    UpdateTreeView();
                }
            }
            else
            {
                CustomMessageBox.Show(this, "알림", "삭제할 분류를 먼저 선택하세요.");
            }
        }
        /// <summary>
        /// ✨ 2. 새 테이블 만들기 (선택된 카테고리에 추가)
        /// </summary>
        private void NewTableButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentProject == null)
            {
                CustomMessageBox.Show(this, "오류", "프로젝트를 먼저 생성하거나 불러오세요.");
                return;
            }

            InfrastructureCategory targetCategory = null;

            // TreeView에서 선택된 항목을 기준으로 부모 분류를 찾습니다.
            var selectedItem = ProjectTreeView.SelectedItem;
            if (selectedItem is InfrastructureCategory category)
            {
                // 분류를 직접 선택한 경우
                targetCategory = category;
            }
            else if (selectedItem is TableDefinition table)
            {
                // 테이블을 선택한 경우, 해당 테이블이 속한 부모 분류를 찾습니다.
                targetCategory = CurrentProject.Categories
                    .FirstOrDefault(c => c.Tables.Contains(table));
            }

            // 아무것도 선택하지 않았다면 첫 번째 분류에 추가합니다.
            if (targetCategory == null)
            {
                targetCategory = CurrentProject.Categories.FirstOrDefault();
                if (targetCategory == null)
                {
                    CustomMessageBox.Show(this, "오류", "테이블을 추가할 분류가 없습니다.");
                    return;
                }
            }

            // InputDialog를 사용하여 사용자에게 테이블 이름을 입력받습니다.
            var dialog = new InputDialog("새 테이블 이름을 입력하세요.", "새 테이블");
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                var newTable = new TableDefinition
                {
                    // 고유 ID는 자동으로 생성하고, TableName은 입력받은 값으로 설정합니다.
                    TableId = "TBL_" + DateTime.Now.ToString("HHmmss"),
                    TableName = dialog.InputText
                };

                targetCategory.Tables.Add(newTable);
                UpdateTreeView(); // TreeView UI 새로고침

                CustomMessageBox.Show(this, "완료", $"'{targetCategory.CategoryName}' 분류에 '{newTable.TableName}' 테이블이 추가되었습니다.");
            }
        }
        /// <summary>
        /// 선택 테이블 삭제
        /// </summary>
        private void DeleteTableButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedTable == null)
            {
                CustomMessageBox.Show(this, "알림", "삭제할 테이블을 먼저 선택하세요.");
                return;
            }

            var result = CustomMessageBox.Show(this, "테이블 삭제",
                $"'{currentSelectedTable.TableName}' 테이블을 삭제하시겠습니까?", true);

            if (result == true)
            {
                // 해당 테이블이 속한 카테고리에서 제거
                foreach (var category in CurrentProject.Categories)
                {
                    if (category.Tables.Contains(currentSelectedTable))
                    {
                        category.Tables.Remove(currentSelectedTable);
                        break;
                    }
                }

                currentSelectedTable = null;
                UpdateTableList();
                HideTableInfo();
                CustomMessageBox.Show(this, "완료", "테이블이 삭제되었습니다.");
            }
        }
        /// <summary>
        /// 기존 UpdateTreeView 호환성을 위한 래퍼
        /// </summary>
        private void UpdateTreeView()
        {
            UpdateTableList();
        }
        /// <summary>
        /// 테이블 정보 패널 표시 (추가 디버깅 버전)
        /// </summary>
        private void ShowTableInfo(TableDefinition table)
        {
            System.Diagnostics.Debug.WriteLine("=== ShowTableInfo 시작 ===");

            // table이 null인지 먼저 체크
            if (table == null)
            {
                System.Diagnostics.Debug.WriteLine("table이 null입니다.");
                HideTableInfo();
                return;
            }

            System.Diagnostics.Debug.WriteLine($"테이블 정보: ID={table.TableId}, Name={table.TableName}");
            System.Diagnostics.Debug.WriteLine($"컬럼 수: {table.Columns?.Count ?? 0}");

            try
            {
                // UI 컨트롤 null 체크
                System.Diagnostics.Debug.WriteLine($"TableInfoPanel null 체크: {TableInfoPanel == null}");
                System.Diagnostics.Debug.WriteLine($"TableIdTextBox null 체크: {TableIdTextBox == null}");
                System.Diagnostics.Debug.WriteLine($"TableNameTextBox null 체크: {TableNameTextBox == null}");
                System.Diagnostics.Debug.WriteLine($"SelectedTableHeader null 체크: {SelectedTableHeader == null}");

                // TableInfoPanel 먼저 표시 (내부 컨트롤들이 초기화되도록)
                if (TableInfoPanel != null)
                {
                    TableInfoPanel.Visibility = Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine("TableInfoPanel 표시 완료");
                }

                // 텍스트박스들 업데이트
                if (TableIdTextBox != null)
                {
                    TableIdTextBox.Text = table.TableId ?? "";
                    System.Diagnostics.Debug.WriteLine("TableIdTextBox 업데이트 완료");
                }

                if (TableNameTextBox != null)
                {
                    TableNameTextBox.Text = table.TableName ?? "";
                    System.Diagnostics.Debug.WriteLine("TableNameTextBox 업데이트 완료");
                }

                // SelectedTableHeader 업데이트 (패널이 표시된 후에)
                if (SelectedTableHeader != null)
                {
                    int columnCount = table.Columns?.Count ?? 0;
                    string headerText = $"📋 {table.TableName ?? "이름없음"} ({columnCount}개 컬럼)";
                    SelectedTableHeader.Text = headerText;
                    System.Diagnostics.Debug.WriteLine($"SelectedTableHeader 업데이트 완료: {headerText}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SelectedTableHeader가 null이므로 건너뜁니다.");
                }

                System.Diagnostics.Debug.WriteLine("=== ShowTableInfo 완료 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== ShowTableInfo 오류 ===");
                System.Diagnostics.Debug.WriteLine($"오류 메시지: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");

                // 오류 발생 시 패널만 표시
                if (TableInfoPanel != null)
                {
                    TableInfoPanel.Visibility = Visibility.Visible;
                }
            }
        }
        /// <summary>
        /// 테이블 정보 패널 숨김 (Null 안전 버전)
        /// </summary>
        private void HideTableInfo()
        {
            try
            {
                if (TableInfoPanel != null)
                {
                    TableInfoPanel.Visibility = Visibility.Collapsed;
                }

                if (SelectedTableHeader != null)
                {
                    SelectedTableHeader.Text = "테이블을 선택하세요";
                }

                // 텍스트박스 클리어
                if (TableIdTextBox != null)
                {
                    TableIdTextBox.Text = "";
                }

                if (TableNameTextBox != null)
                {
                    TableNameTextBox.Text = "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HideTableInfo 오류: {ex.Message}");
            }
        }
        /// <summary>
        /// 테이블 정보 저장 (Null 안전 버전)
        /// </summary>
        private void SaveTableInfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedTable == null)
            {
                CustomMessageBox.Show(this, "오류", "선택된 테이블이 없습니다.");
                return;
            }

            if (CurrentProject == null)
            {
                CustomMessageBox.Show(this, "오류", "프로젝트가 로드되지 않았습니다.");
                return;
            }

            try
            {
                string newTableId = TableIdTextBox?.Text?.Trim() ?? "";
                string newTableName = TableNameTextBox?.Text?.Trim() ?? "";

                if (string.IsNullOrEmpty(newTableId) || string.IsNullOrEmpty(newTableName))
                {
                    CustomMessageBox.Show(this, "오류", "테이블 ID와 테이블명을 모두 입력하세요.");
                    return;
                }

                // 🔥 수정: CurrentProject.Categories에서 중복 ID 체크
                bool isDuplicate = false;
                foreach (var category in CurrentProject.Categories)
                {
                    if (category.Tables.Any(t => t != currentSelectedTable && t.TableId == newTableId))
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (isDuplicate)
                {
                    CustomMessageBox.Show(this, "오류", "동일한 테이블 ID가 이미 존재합니다.");
                    return;
                }

                // 테이블 정보 업데이트
                currentSelectedTable.TableId = newTableId;
                currentSelectedTable.TableName = newTableName;

                UpdateTableList();
                ShowTableInfo(currentSelectedTable); // 헤더 업데이트
                CustomMessageBox.Show(this, "완료", "테이블 정보가 저장되었습니다.");
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(this, "오류", $"테이블 정보 저장 중 오류가 발생했습니다: {ex.Message}");
            }
        }
        /// <summary>
        /// 현재 선택된 테이블에 컬럼 붙여넣기
        /// </summary>
        private void PasteColumnsToCurrentTable()
        {
            try
            {
                if (currentSelectedTable == null)
                {
                    CustomMessageBox.Show(this, "알림", "컬럼을 추가할 테이블을 먼저 선택하세요.");
                    return;
                }

                string clipboardText = Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    CustomMessageBox.Show(this, "알림", "클립보드가 비어있습니다.");
                    return;
                }

                // ClipboardHelper를 사용하여 파싱
                var newColumns = ClipboardHelper.ParseColumnsFromClipboard(clipboardText);

                if (newColumns.Count > 0)
                {
                    // ===== 👇 [수정] AddRange 대신 하나씩 추가하도록 변경합니다. =====
                    if (currentSelectedTable.Columns == null)
                    {
                        currentSelectedTable.Columns = new BindingList<ColumnDefinition>();
                    }

                    foreach (var col in newColumns)
                    {
                        currentSelectedTable.Columns.Add(col);
                    }

                    // BindingList를 사용하면 UI가 자동으로 업데이트되므로 RefreshSelectedTableGrid() 호출은 필요 없습니다.
                    ShowTableInfo(currentSelectedTable); // 테이블 정보(컬럼 개수 등) 업데이트
                    CustomMessageBox.Show(this, "완료", $"{newColumns.Count}개의 컬럼이 '{currentSelectedTable.TableName}' 테이블에 추가되었습니다.");
                }
                else
                {
                    CustomMessageBox.Show(this, "오류", "올바른 컬럼 데이터를 찾을 수 없습니다.\n\n형식: 컬럼ID [Tab] 컬럼명 [Tab] 타입 [Tab] 길이 [Tab] NOTNULL(Y/N) [Tab] 코드명");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(this, "파싱 오류", $"컬럼 데이터 붙여넣기 중 오류가 발생했습니다:\n\n{ex.Message}");
            }
        }
        // =======================================================
        // ✨ 4. TreeView 드래그 앤 드롭 로직
        // =======================================================
        private Point startPoint;
        private bool isDragging = false;
        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }
        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !isDragging)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (ProjectTreeView.SelectedItem is TableDefinition)
                    {
                        isDragging = true;
                        DataObject data = new DataObject("myFormat", ProjectTreeView.SelectedItem);
                        DragDrop.DoDragDrop(ProjectTreeView, data, DragDropEffects.Move);
                        isDragging = false;
                    }
                }
            }
        }
        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("myFormat"))
            {
                TableDefinition droppedTable = e.Data.GetData("myFormat") as TableDefinition;
                var targetElement = e.OriginalSource as FrameworkElement;

                // 드롭된 위치의 데이터 컨텍스트를 찾습니다.
                object dataContext = targetElement?.DataContext;

                InfrastructureCategory targetCategory = null;

                if (dataContext is InfrastructureCategory category)
                {
                    targetCategory = category;
                }
                else if (dataContext is TableDefinition table)
                {
                    targetCategory = CurrentProject.Categories.FirstOrDefault(c => c.Tables.Contains(table));
                }

                if (droppedTable != null && targetCategory != null)
                {
                    // 기존 카테고리에서 테이블 제거
                    InfrastructureCategory sourceCategory = CurrentProject.Categories
                        .FirstOrDefault(c => c.Tables.Contains(droppedTable));

                    if (sourceCategory != null && sourceCategory != targetCategory)
                    {
                        sourceCategory.Tables.Remove(droppedTable);
                        targetCategory.Tables.Add(droppedTable);
                        UpdateTableList(); // UI 새로고침
                    }
                }
            }
        }
    }
}