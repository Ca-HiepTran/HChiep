using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Autodesk.Revit.UI;

namespace HChiep.Place
{
    /// <summary>
    /// Interaction logic for wpf_place.xaml
    /// </summary>
    public partial class wpf_place : Window
    {
        private readonly ViewModel _viewModel;

        public wpf_place(Document document, UIDocument uiDocument)
        {
            InitializeComponent();
            _viewModel = new ViewModel(document, uiDocument, this);
            DataContext = _viewModel;
        }

    }
}
