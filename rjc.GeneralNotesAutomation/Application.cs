using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.ApplicationServices;
using System.Windows.Media;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace rjc.GeneralNotesAutomation
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]

    // This class is used to setup the RJC Column Hub add-in buttons in Revit
    // No code that carry out the actual Revit operations is include in this class
    // Each button is linked to a separate class in Main.cs, which contains the actual Revit functions
    public class Setup : IExternalApplication
    {
        private string codeDir;
        private string libPath;
        private const string DLL_NAME = "GeneralNotesAutomation.dll";
        public Setup()
        {
            // libPath establish the path of the compiled .dll file
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            codeDir = Path.GetDirectoryName(path);
            this.libPath = Path.Combine(codeDir, DLL_NAME);
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "RJC Tools 2";
            string panelName = "General Notes Formatting";

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException) { }

            RibbonPanel taggingPanel = null;
            foreach (RibbonPanel panel in application.GetRibbonPanels(tabName))
            {
                if (panel.Name == panelName)
                {
                    taggingPanel = panel;
                    break;
                }
            }
            if (taggingPanel is null)
                taggingPanel = application.CreateRibbonPanel(tabName, panelName);

            AddPushButton(taggingPanel, "StackGeneralNotes", "Stack General Notes", "rjc.GeneralNotesAutomation.StackGeneralNotes",
                "rjc.GeneralNotesAutomation.Graphics.stack.png", "Automatically stack and organize the General Notes views that are already existing on all sheets in the project containing the name \"General Notes\".");
            
           /* AddPushButton(taggingPanel, "PlaceUnassignedViews", "Place Unassigned Views", "rjc.GeneralNotesAutomation.PlaceGeneralNotes",
               "rjc.GeneralNotesAutomation.Graphics.import_views96px.png", "Place all views in the Project Browser that are categorized as \"03 UNASSIGNED VIEWS\" onto the General Notes sheets.");*/

            return Result.Succeeded;
        }

        private void AddPushButton(RibbonPanel panel, string buttonName, string buttonText, string buttonClass, string iconRes, string toolTip)
        {
            // Initialize buttons
            PushButtonData importBeamTaggingData = new PushButtonData(buttonName, buttonText, this.libPath, buttonClass);
            PushButton importBeamTagging = panel.AddItem(importBeamTaggingData) as PushButton;

            // Get image
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            Stream myStream = myAssembly.GetManifestResourceStream(iconRes);
            Bitmap bmp = new Bitmap(myStream);
            importBeamTagging.LargeImage = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            // Tool tip displayed when hovered over the button
            importBeamTagging.ToolTip = toolTip;
        }
    }
}