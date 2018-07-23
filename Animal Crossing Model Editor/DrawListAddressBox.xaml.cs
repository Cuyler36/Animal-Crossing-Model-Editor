using System.Windows;
using System.Windows.Controls;

namespace Animal_Crossing_Model_Editor
{
    /// <summary>
    /// Interaction logic for DrawListAddressBox.xaml
    /// </summary>
    public partial class DrawListAddressBox : Window
    {
        public uint Address;

        public DrawListAddressBox()
        {
            InitializeComponent();
        }

        private string LastText = "";

        private bool CheckHexString(string Text)
            => System.Text.RegularExpressions.Regex.IsMatch(Text, @"\A\b[0-9a-fA-F]+\b\Z");

        private void DrawListAddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(LastText) && !CheckHexString(DrawListAddressTextBox.Text))
            {
                e.Handled = true;
                DrawListAddressTextBox.Text = LastText;
            }
            else
            {
                LastText = DrawListAddressTextBox.Text;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (uint.TryParse(DrawListAddressTextBox.Text, System.Globalization.NumberStyles.HexNumber, null, out Address))
            {
                if (Address >= 0x80000000 && Address < 0x81800000)
                {
                    DialogResult = true;
                    Close();
                }
            }
        }
    }
}
