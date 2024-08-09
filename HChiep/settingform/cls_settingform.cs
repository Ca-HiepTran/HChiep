using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Autodesk.Revit.Attributes;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;
using System.Windows.Controls;
using static System.Net.Mime.MediaTypeNames;

namespace HChiep
{
    [Transaction(TransactionMode.Manual)]
    internal class cls_settingform : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            wpf_setting wpf_Settingform = new wpf_setting();
            wpf_Settingform.ShowDialog();
            return Result.Succeeded;
        }
    }
}
