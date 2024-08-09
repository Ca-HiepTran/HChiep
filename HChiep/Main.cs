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
    /*[Transaction(TransactionMode.Manual)]*/
    public class Main : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            //createTAB
            string nametab = "Traintool1";
            application.CreateRibbonTab(nametab); //tao ribbon

            //create Panel
            RibbonPanel panel1 = application.CreateRibbonPanel(nametab, "Setting");
            RibbonPanel panel2 = application.CreateRibbonPanel(nametab, "Wall Utils");
            RibbonPanel panel3 = application.CreateRibbonPanel(nametab, "Door Utils");
            RibbonPanel panel4 = application.CreateRibbonPanel(nametab, "Room Untils");


            //create button
            string path = Assembly.GetExecutingAssembly().Location;

            PushButtonData settingbtn = new PushButtonData("btnSetting", "Setting", path, "HChiep.cls_settingform");

            PushButtonData createwallbtn = new PushButtonData("btnCreatewall", "Create wall", path, "HChiep.cls_createwall1");

            PushButtonData walltagbtn = new PushButtonData("btnWalltag", "Placed Beam", path, "HChiep.CommandPlaced");

            PushButtonData autotagbtn = new PushButtonData("btnAutotag", "Auto tag", path, "HChiep.Autotag");
            PulldownButtonData checkalltagbtn = new PulldownButtonData("btnCheckalltag", "Check all tag");
            PushButtonData verifytagbtn = new PushButtonData("btnVerifytag", "Verify tag", path, "HChiep.Verifytag");
            PushButtonData autorenamebtn = new PushButtonData("btnAutorename", "Auto rename", path, "HChiep.Autorename");
            PushButtonData definepositionbtn = new PushButtonData("btnDefineposition", "Define position", path, "HChiep.Defineposition");
            PushButtonData updateroombtn = new PushButtonData("btnUpdateroom", "Update room", path, "HChiep.Updateroom");

            //add button to panel
            PushButton settingPbtn = panel1.AddItem(settingbtn) as PushButton;
            PushButton createwalPbtn = panel2.AddItem(createwallbtn) as PushButton;
            PushButton walltagPbtn = panel2.AddItem(walltagbtn) as PushButton;
            PushButton autotagPbtn = panel2.AddItem(autotagbtn) as PushButton;
            PulldownButton checkalltagPbtn = panel2.AddItem(checkalltagbtn) as PulldownButton;
            PushButton verifytagPbtn = panel2.AddItem(verifytagbtn) as PushButton;
            PushButton autorenamePbtn = panel3.AddItem(autorenamebtn) as PushButton;
            PushButton definepositionPbtn = panel3.AddItem(definepositionbtn) as PushButton;
            PushButton updateroomPbtn = panel4.AddItem(updateroombtn) as PushButton;

            //add icon to button   C:\Users\MSI LAPTOP\source\repos\HChiep\HChiep\image
            Uri urisource1 = new Uri(@"C:\Users\MSI LAPTOP\source\repos\HChiep\HChiep\image\command1.png");
            Uri urisource2 = new Uri(@"C:\Users\MSI LAPTOP\source\repos\HChiep\HChiep\image\command2.png");
            Uri urisource3 = new Uri(@"C:\Users\MSI LAPTOP\source\repos\HChiep\HChiep\image\command3.png");
            Uri urisource4 = new Uri(@"C:\Users\MSI LAPTOP\source\repos\HChiep\HChiep\image\command4.png");
            Uri urisource5 = new Uri(@"C:\Users\MSI LAPTOP\source\repos\HChiep\HChiep\image\command5.png");
            Uri urisource6 = new Uri(@"C:\Users\MSI LAPTOP\source\repos\HChiep\HChiep\image\command6.png");
            Uri urisource7 = new Uri(@"C:\Users\MSI LAPTOP\source\repos\HChiep\HChiep\image\command7.png");
            Uri urisource8 = new Uri(@"C:\Users\MSI LAPTOP\source\repos\HChiep\HChiep\image\command8.png");
            Uri urisource9 = new Uri(@"C:\Users\MSI LAPTOP\source\repos\HChiep\HChiep\image\command9.png");
             //add image to button
            BitmapImage image1 = new BitmapImage(urisource1);
            BitmapImage image2 = new BitmapImage(urisource2);
            BitmapImage image3 = new BitmapImage(urisource3);
            BitmapImage image4 = new BitmapImage(urisource4);
            BitmapImage image5 = new BitmapImage(urisource5);
            BitmapImage image6 = new BitmapImage(urisource6);
            BitmapImage image7 = new BitmapImage(urisource7);
            BitmapImage image8 = new BitmapImage(urisource8);
            BitmapImage image9 = new BitmapImage(urisource9);

            settingPbtn.LargeImage = image1;
            createwalPbtn.LargeImage = image2; 
            walltagPbtn.LargeImage = image3;
            autotagPbtn.LargeImage = image4;  
            checkalltagPbtn.LargeImage = image5;
            verifytagPbtn.LargeImage = image6;
            autorenamePbtn.LargeImage = image7;
            definepositionPbtn.LargeImage = image8;
            updateroomPbtn.LargeImage = image9;
           
            settingPbtn.ToolTip = "Setting system family, color for Jasty addin";
            settingPbtn.LongDescription = "Use this function to set up external, internal wall tag, error color and initialize all other buttons";
            createwalPbtn.ToolTip = "Auto create wall type with specific name";
            createwalPbtn.LongDescription = "Use excel input file and automatically create wall type, rename as specific rule.";
            walltagPbtn.ToolTip = "Tag wall with specific position and tag type";
            walltagPbtn.LongDescription = "Automatically detect external, internal wall and tag into specific position.";
            autotagPbtn.ToolTip = "Automatically tag all wall in current view.";
            autotagPbtn.LongDescription = "Automatically tag all wall in internal and external position in current view.";
            checkalltagPbtn.ToolTip = "Check the wrong tag in current view.";
            checkalltagPbtn.LongDescription = "Check all the tag that has wrong type, position and null value in current view";
            verifytagPbtn.ToolTip = "Automatically correct the wrong tag.";
            verifytagPbtn.LongDescription = "Correct the wrong position, type tag by changing correct tag type.";
            autorenamePbtn.ToolTip = "Auto rename door type with specific parameters.";
            autorenamePbtn.LongDescription = "Select parameter from UI to create specific rule for type name and automatically rename all door in current document.";
            definepositionPbtn.ToolTip = "Define the room that door is belong to.";
            definepositionPbtn.LongDescription = "Find all the room that contain this door and insert room name in the specific parameter.";
            updateroomPbtn.ToolTip = "Auto update area from area plan to room plane";
            updateroomPbtn.LongDescription = "Find all the areas inside the room and summary area into specific room parameter.";

            /*//split button
            PushButtonData checkalltagSbtn = new PushButtonData("btnCheckalltag2", "Check all tag", path, "HChiep.Checkalltag2");
            PushButtonData checkoffSbtn = new PushButtonData("btnCheckoff", "Check off", path, "HChiep.Checkoff");

            SplitButtonData splitButtonData = new SplitButtonData("splitbutton", "Split Button");
            SplitButton splitbtn = panel2.AddItem(splitButtonData) as SplitButton;
            splitbtn.AddPushButton(checkalltagSbtn);
            splitbtn.AddPushButton(checkoffSbtn);*/


            // Tạo PulldownButton
            //PulldownButtonData pulldownButtonData = new PulldownButtonData("pulldownButton", "Dropdown Button");
            //PulldownButton pulldownButton = panel2.AddItem(checkalltagbtn) as PulldownButton;

            // Tạo PushButtonData cho các nút bên trong PulldownButton
            PushButtonData checkalltagSbtn = new PushButtonData("btnCheckalltag2", "Check all tag", path, "HChiep.Checkalltag2");
            PushButtonData checkoffSbtn = new PushButtonData("btnCheckoff", "Check off", path, "HChiep.Checkoff");

            // Thêm các PushButton vào PulldownButton mà không thiết lập ToolTip và LargeImage
            checkalltagPbtn.AddPushButton(checkalltagSbtn);
            checkalltagPbtn.AddPushButton(checkoffSbtn);


            //Press F1 for more help
            ContextualHelp contextHelp = new ContextualHelp(ContextualHelpType.Url,
        "http://www.autodesk.com");
            settingPbtn.SetContextualHelp(contextHelp);
            createwalPbtn.SetContextualHelp(contextHelp);
            walltagPbtn.SetContextualHelp(contextHelp);
            autotagPbtn.SetContextualHelp(contextHelp);
            checkalltagPbtn.SetContextualHelp(contextHelp);
            verifytagPbtn.SetContextualHelp(contextHelp);
            autorenamePbtn.SetContextualHelp(contextHelp);
            definepositionPbtn.SetContextualHelp(contextHelp);
            updateroomPbtn.SetContextualHelp(contextHelp);

            return Result.Succeeded;
        }
    }
}
