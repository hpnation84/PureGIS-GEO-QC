// 파일 경로: MainWindow/Tabs/MainWindow.Tab1.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PureGIS_Geo_QC.Models; // 네임스페이스는 실제 프로젝트에 맞게 조정하세요.
using PureGIS_Geo_QC.WPF;
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
                System.Diagnostics.Debug.WriteLine("=== PasteColumnsToCurrentTable 시작 ===");

                // 1. 선택된 테이블 확인
                System.Diagnostics.Debug.WriteLine($"currentSelectedTable null 체크: {currentSelectedTable == null}");
                if (currentSelectedTable == null)
                {
                    CustomMessageBox.Show(this, "알림", "컬럼을 추가할 테이블을 먼저 선택하세요.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"선택된 테이블: {currentSelectedTable.TableName}");
                System.Diagnostics.Debug.WriteLine($"기존 컬럼 수: {currentSelectedTable.Columns?.Count ?? 0}");

                // 2. 클립보드 텍스트 가져오기
                string clipboardText = null;
                try
                {
                    System.Diagnostics.Debug.WriteLine("클립보드 텍스트 가져오는 중...");
                    clipboardText = Clipboard.GetText();
                    System.Diagnostics.Debug.WriteLine($"클립보드 텍스트 길이: {clipboardText?.Length ?? 0}");
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        System.Diagnostics.Debug.WriteLine($"클립보드 내용 일부: {clipboardText.Substring(0, Math.Min(100, clipboardText.Length))}");
                    }
                }
                catch (Exception clipEx)
                {
                    System.Diagnostics.Debug.WriteLine($"클립보드 오류: {clipEx.Message}");
                    CustomMessageBox.Show(this, "오류", $"클립보드에서 텍스트를 가져올 수 없습니다: {clipEx.Message}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    CustomMessageBox.Show(this, "알림", "클립보드가 비어있습니다.");
                    return;
                }

                // 3. 컬럼 데이터 파싱
                System.Diagnostics.Debug.WriteLine("컬럼 데이터 파싱 중...");
                var newColumns = ParseColumnsFromClipboard(clipboardText);
                if (newColumns == null)
                {
                    System.Diagnostics.Debug.WriteLine("파싱 결과가 null입니다.");
                    CustomMessageBox.Show(this, "오류", "컬럼 데이터 파싱에 실패했습니다.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"파싱된 컬럼 수: {newColumns.Count}");

                if (newColumns.Count > 0)
                {
                    // 4. 컬럼 추가
                    System.Diagnostics.Debug.WriteLine("컬럼 추가 중...");
                    if (currentSelectedTable.Columns == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Columns 리스트가 null이므로 새로 생성합니다.");
                        currentSelectedTable.Columns = new List<ColumnDefinition>();
                    }

                    currentSelectedTable.Columns.AddRange(newColumns);
                    System.Diagnostics.Debug.WriteLine($"컬럼 추가 완료. 총 컬럼 수: {currentSelectedTable.Columns.Count}");

                    // 5. UI 업데이트
                    System.Diagnostics.Debug.WriteLine("UI 업데이트 중...");
                    try
                    {
                        RefreshSelectedTableGrid();
                        System.Diagnostics.Debug.WriteLine("DataGrid 새로고침 완료");
                    }
                    catch (Exception gridEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"DataGrid 새로고침 오류: {gridEx.Message}");
                    }

                    try
                    {
                        UpdateTableList(); // ListBox 업데이트
                        System.Diagnostics.Debug.WriteLine("ListBox 업데이트 완료");
                    }
                    catch (Exception listEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"ListBox 업데이트 오류: {listEx.Message}");
                    }

                    // 6. 헤더 업데이트 (이 부분에서 오류 발생 가능성 높음)
                    System.Diagnostics.Debug.WriteLine("헤더 업데이트 중...");
                    try
                    {
                        ShowTableInfo(currentSelectedTable);
                        System.Diagnostics.Debug.WriteLine("헤더 업데이트 완료");
                    }
                    catch (Exception headerEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"헤더 업데이트 오류: {headerEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"헤더 업데이트 스택트레이스: {headerEx.StackTrace}");
                        // 헤더 업데이트 실패해도 계속 진행
                    }

                    // 7. 성공 메시지
                    System.Diagnostics.Debug.WriteLine("성공 메시지 표시 중...");
                    string tableName = currentSelectedTable?.TableName ?? "테이블";
                    CustomMessageBox.Show(this, "완료", $"{newColumns.Count}개의 컬럼이 '{tableName}' 에 추가되었습니다.");
                }
                else
                {
                    CustomMessageBox.Show(this, "오류", "올바른 컬럼 데이터를 찾을 수 없습니다.\n\n형식: 컬럼ID [Tab] 컬럼명 [Tab] 타입 [Tab] 길이");
                }

                System.Diagnostics.Debug.WriteLine("=== PasteColumnsToCurrentTable 완료 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== PasteColumnsToCurrentTable 전체 오류 ===");
                System.Diagnostics.Debug.WriteLine($"오류 메시지: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"오류 위치: {ex.TargetSite}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"=== 오류 정보 끝 ===");

                CustomMessageBox.Show(this, "파싱 오류", $"컬럼 데이터 붙여넣기 중 오류가 발생했습니다:\n\n{ex.Message}\n\n자세한 정보는 디버그 출력을 확인하세요.");
            }
        }
        /// <summary>
        /// 클립보드에서 컬럼 데이터만 파싱 (Null 안전 버전)
        /// </summary>
        private List<ColumnDefinition> ParseColumnsFromClipboard(string clipboardText)
        {
            var columns = new List<ColumnDefinition>();

            try
            {
                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    return columns;
                }

                var lines = clipboardText.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var cols = line.Split('\t');
                    if (cols.Length < 2) continue; // 최소 2개 컬럼은 있어야 함

                    columns.Add(new ColumnDefinition
                    {
                        ColumnId = GetSafeArrayValue(cols, 0, "COL_" + DateTime.Now.Ticks.ToString().Substring(10)),
                        ColumnName = GetSafeArrayValue(cols, 1, "컬럼_" + columns.Count),
                        Type = GetSafeArrayValue(cols, 2, "VARCHAR2"),
                        Length = GetSafeArrayValue(cols, 3, "50"),
                        IsNotNull = false, // 기본값
                        KeyType = "" // 기본값
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ParseColumnsFromClipboard 오류: {ex.Message}");
                // 빈 리스트 반환
            }

            return columns;
        }
        // <summary>
        /// 선택된 테이블의 DataGrid 새로고침 (Null 안전 버전)
        /// </summary>
        private void RefreshSelectedTableGrid()
        {
            try
            {
                if (currentSelectedTable != null && StandardGrid != null)
                {
                    StandardGrid.ItemsSource = null;
                    StandardGrid.ItemsSource = currentSelectedTable.Columns;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshSelectedTableGrid 오류: {ex.Message}");
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