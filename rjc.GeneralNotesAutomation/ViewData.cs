using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;

namespace rjc.GeneralNotesAutomation
{
    public class ViewData
    {
        public ElementId viewElementId { get; set; }

        //public ElementId viewportElementId { get; set; }

        public double viewLength { get; set; }

        public string viewName { get; set; }

        public double viewportOriginX { get; set; }

        public double viewportOriginY { get; set; }

        public string sheetNumber { get; set; }

        public bool canPlaceOnSheet { get; set; }

        public Outline viewportOutline { get; set; }

        public bool isHollowCoreNote { get; set; }
    }
}
