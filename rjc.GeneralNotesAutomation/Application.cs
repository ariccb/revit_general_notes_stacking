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
            RibbonPanel BeamScheduleToolsPanel = application.CreateRibbonPanel("RJC General Notes Tools");
            AddPushButton(BeamScheduleToolsPanel);

            return Result.Succeeded;
            //throw new NotImplementedException();
        }

        private void AddPushButton(RibbonPanel panel)
        {
            PushButtonData insertGeneralNotesData = new PushButtonData("InsertGeneralNotes", "Insert General\nNotes", Path.Combine(AssemblyDirectory, "GeneralNotesAutomation.dll"),"rjc.GeneralNotesAutomation.InsertGeneralNotes");
            PushButtonData stackGeneralNotesData = new PushButtonData("StackGeneralNotes", "Stack General\nNotes", Path.Combine(AssemblyDirectory, "GeneralNotesAutomation.dll"), "rjc.GeneralNotesAutomation.StackGeneralNotes");
            PushButtonData formatGeneralNotesData = new PushButtonData("FormatGeneralNote", "Format", Path.Combine(AssemblyDirectory, "GeneralNotesAutomation.dll"), "rjc.GeneralNotesAutomation.FormatGeneralNote");

            //PushButtonData createBeamScheduleData = new PushButtonData("InsertFamilyInDraftingView", "Create Beam\nSchedule", @"C:\Program Files\RJC Beam Schedule Tools\Revit 2019\CreateBeamSchedule.dll", "CreateBeamSchedule.InsertFamilyInDraftingView");
            PushButton insertGeneralNotesButton = panel.AddItem(insertGeneralNotesData) as PushButton;
            PushButton stackGeneralNotesButton = panel.AddItem(stackGeneralNotesData) as PushButton;
            PushButton formatGeneralNotesButton = panel.AddItem(formatGeneralNotesData) as PushButton;
            /*
            // Set ToolTip and contextual help
            pushButton.ToolTip = "Say Hello World";
            ContextualHelp contextHelp = new ContextualHelp(ContextualHelpType.Url,
                "http://www.autodesk.com");
            pushButton.SetContextualHelp(contextHelp);
            // Set the large image shown on button
            */
            //Executing Assembly Directory
            insertGeneralNotesButton.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(Path.Combine(AssemblyDirectory, @"Graphics\revitInsertGeneralNotesButton.png")));
            stackGeneralNotesButton.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(Path.Combine(AssemblyDirectory, @"Graphics\revitInsertGeneralNotesButton.png")));
            formatGeneralNotesButton.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(Path.Combine(AssemblyDirectory, @"Graphics\revitInsertGeneralNotesButton.png")));

            //Debug Version
            //createBeamSchedule.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"C:\Users\cfebbraro\Documents\rjcDev\rjcRevitSchedules\CreateBeamSchedule\bin\Debug 2017\revitCreateBeamScheduleButton.png"));
            //createBeamSchedule.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"C:\Users\cfebbraro\Documents\rjcDev\rjcRevitSchedules\CreateBeamSchedule\bin\Debug 2018\revitCreateBeamScheduleButton.png"));
            //createBeamSchedule.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"C:\Users\cfebbraro\Documents\rjcDev\rjcRevitSchedules\CreateBeamSchedule\bin\Debug 2019\revitCreateBeamScheduleButton.png"));

            //Release Version
            //createBeamSchedule.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"C:\Program Files\RJC Beam Schedule Tools\Revit 2017\revitCreateBeamScheduleButton.png"));
            //createBeamSchedule.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"C:\Program Files\RJC Beam Schedule Tools\Revit 2018\revitCreateBeamScheduleButton.png"));
            //createBeamSchedule.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"C:\Program Files\RJC Beam Schedule Tools\Revit 2019\revitCreateBeamScheduleButton.png"));

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
