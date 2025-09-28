using System.Windows;
using System.Windows.Input;

namespace PureGIS_Geo_QC.WPF
{
    public partial class InputDialog : Window
    {
        public string InputText { get; private set; }

        public InputDialog(string title, string defaultText = "")
        {
            InitializeComponent();
            TitleText.Text = title;
            InputTextBox.Text = defaultText;
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                MessageBox.Show("값을 입력하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            InputText = InputTextBox.Text;
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