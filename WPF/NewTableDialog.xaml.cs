using System.Windows;

namespace PureGIS_Geo_QC.WPF
{
    public partial class NewTableDialog : Window
    {
        public string TableId { get; private set; }
        public string TableName { get; private set; }

        public NewTableDialog()
        {
            InitializeComponent();
            TableIdTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TableIdTextBox.Text) || string.IsNullOrWhiteSpace(TableNameTextBox.Text))
            {
                MessageBox.Show("테이블 ID와 테이블명을 모두 입력하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            TableId = TableIdTextBox.Text;
            TableName = TableNameTextBox.Text;
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}