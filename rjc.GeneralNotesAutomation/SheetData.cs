using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace rjc.GeneralNotesAutomation
{
    public class SheetData
    {
        public ElementId SheetId { get; set; }

        public string SheetNumber { get; set; }

        public string SheetName { get; set; }

        public ElementId TitleBlockId { get; set; }

        public ViewSheet SheetView { get; set; }

    }
}
