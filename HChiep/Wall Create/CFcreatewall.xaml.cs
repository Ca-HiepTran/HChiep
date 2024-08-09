using System.Windows;

namespace HChiep.Wall_Create
{
    public partial class CFcreatewall : Window
    {
        public bool Overlap { get; private set; }
        public bool ApplyToAll { get; private set; }

        public CFcreatewall()
        {
            InitializeComponent();
        }

        private void btn_wcoverlap_Click(object sender, RoutedEventArgs e)
        {
            Overlap = true;
            ApplyToAll = true;  // Set ApplyToAll to true for the overwrite action
            DialogResult = true;
        }

        private void btn_wccancel_Click(object sender, RoutedEventArgs e)
        {
            Overlap = false;
            ApplyToAll = false; // Set ApplyToAll to false for the cancel action
            DialogResult = false;
        }
    }
}
