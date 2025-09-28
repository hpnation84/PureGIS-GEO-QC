using PureGIS_Geo_QC.Models;
using PureGIS_Geo_QC.WPF;
using System.Linq;
using System.Windows;

namespace PureGIS_Geo_QC_Standalone
{
    public partial class MainWindow
    {
        /// <summary>
        /// 새 컬럼(행)을 추가합니다.
        /// </summary>
        private void AddColumnButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedTable == null)
            {
                CustomMessageBox.Show(this, "알림", "컬럼을 추가할 테이블을 먼저 선택하세요.");
                return;
            }

            currentSelectedTable.Columns.Add(new ColumnDefinition
            {
                ColumnId = "NEW_COLUMN",
                ColumnName = "새 컬럼",
                Type = "VARCHAR2",
                Length = "100"
            });

            // 마지막 행으로 스크롤
            if (StandardGrid.Items.Count > 0)
            {
                StandardGrid.ScrollIntoView(StandardGrid.Items[StandardGrid.Items.Count - 1]);
            }
        }

        /// <summary>
        /// 선택한 컬럼(행)을 삭제합니다.
        /// </summary>
        private void DeleteColumnButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedTable == null || StandardGrid.SelectedItems.Count == 0)
            {
                CustomMessageBox.Show(this, "알림", "삭제할 컬럼을 먼저 선택하세요.");
                return;
            }

            if (CustomMessageBox.Show(this, "삭제 확인", $"{StandardGrid.SelectedItems.Count}개의 컬럼을 삭제하시겠습니까?", true) == true)
            {
                // 여러 항목을 안전하게 삭제하기 위해 리스트로 복사 후 역순으로 제거
                var itemsToRemove = StandardGrid.SelectedItems.Cast<ColumnDefinition>().ToList();
                foreach (var item in itemsToRemove)
                {
                    currentSelectedTable.Columns.Remove(item);
                }
            }
        }

        /// <summary>
        /// 선택한 컬럼(행)을 위로 이동합니다.
        /// </summary>
        private void MoveColumnUp_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedTable == null || StandardGrid.SelectedItem == null) return;

            var selectedColumn = StandardGrid.SelectedItem as ColumnDefinition;
            int currentIndex = currentSelectedTable.Columns.IndexOf(selectedColumn);

            if (currentIndex > 0)
            {
                currentSelectedTable.Columns.RemoveAt(currentIndex);
                currentSelectedTable.Columns.Insert(currentIndex - 1, selectedColumn);
                StandardGrid.SelectedItem = selectedColumn; // 이동 후에도 선택 유지
            }
        }

        /// <summary>
        /// 선택한 컬럼(행)을 아래로 이동합니다.
        /// </summary>
        private void MoveColumnDown_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedTable == null || StandardGrid.SelectedItem == null) return;

            var selectedColumn = StandardGrid.SelectedItem as ColumnDefinition;
            int currentIndex = currentSelectedTable.Columns.IndexOf(selectedColumn);

            if (currentIndex < currentSelectedTable.Columns.Count - 1)
            {
                currentSelectedTable.Columns.RemoveAt(currentIndex);
                currentSelectedTable.Columns.Insert(currentIndex + 1, selectedColumn);
                StandardGrid.SelectedItem = selectedColumn; // 이동 후에도 선택 유지
            }
        }
    }
}