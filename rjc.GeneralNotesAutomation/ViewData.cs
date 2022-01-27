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
        public ElementId ViewElementId { get; set; }

        //public ElementId viewportElementId { get; set; }

        public double ViewLength { get; set; }

        public string ViewName { get; set; }

        public double ViewportOriginX { get; set; }

        public double ViewportOriginY { get; set; }

        public string SheetNumber { get; set; }

        public bool CanPlaceOnSheet { get; set; }

        public Outline ViewportOutline { get; set; }

        public bool IsHollowCoreNote { get; set; }

        public Parameter ViewRJCOfficeId { get; set; }      

        public Parameter ViewRJCStandardViewID { get; set; }
    }
}
