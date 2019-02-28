using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using System.IO;
using System.Reflection;

namespace rjc.GeneralNotesAutomation
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    
    public class Application : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            //throw new NotImplementedException();
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            application.CreateRibbonTab("RJC Toronto");
            RibbonPanel BeamScheduleToolsPanel = application.CreateRibbonPanel("RJC Toronto","General Notes Tools");
            AddPushButton(BeamScheduleToolsPanel);

            return Result.Succeeded;
        }

        private void AddPushButton(RibbonPanel panel)
        {
            PushButtonData insertGeneralNotesData = new PushButtonData("InsertGeneralNotes", "Insert General\nNotes", Path.Combine(AssemblyDirectory, "GeneralNotesAutomation.dll"),"rjc.GeneralNotesAutomation.InsertGeneralNotes");
            PushButtonData stackGeneralNotesData = new PushButtonData("StackGeneralNotes", "Stack General\nNotes", Path.Combine(AssemblyDirectory, "GeneralNotesAutomation.dll"), "rjc.GeneralNotesAutomation.StackGeneralNotes");
            //PushButtonData formatGeneralNotesData = new PushButtonData("FormatGeneralNote", "Format", Path.Combine(AssemblyDirectory, "GeneralNotesAutomation.dll"), "rjc.GeneralNotesAutomation.FormatGeneralNote");
            //PushButtonData createOutlinesData = new PushButtonData("CreateOutlines", "Create Outlines", Path.Combine(AssemblyDirectory, "GeneralNotesAutomation.dll"), "rjc.GeneralNotesAutomation.CreateOutlines");

            PushButton insertGeneralNotesButton = panel.AddItem(insertGeneralNotesData) as PushButton;
            PushButton stackGeneralNotesButton = panel.AddItem(stackGeneralNotesData) as PushButton;
            //PushButton formatGeneralNotesButton = panel.AddItem(formatGeneralNotesData) as PushButton;
            //PushButton createOutlinesButton = panel.AddItem(createOutlinesData) as PushButton;

            insertGeneralNotesButton.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(Path.Combine(AssemblyDirectory, @"Graphics\revitInsertGeneralNotesButton.png")));
            stackGeneralNotesButton.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(Path.Combine(AssemblyDirectory, @"Graphics\revitStackGeneralNotesButton.png")));
            //formatGeneralNotesButton.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(Path.Combine(AssemblyDirectory, @"Graphics\revitInsertGeneralNotesButton.png")));
            //createOutlinesButton.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(Path.Combine(AssemblyDirectory, @"Graphics\revitInsertGeneralNotesButton.png")));

        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
