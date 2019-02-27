using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;

namespace rjc.GeneralNotesAutomation
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class CreateOutlines : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            FilteredElementCollector generalNotesViewports = new FilteredElementCollector(doc,doc.ActiveView.Id);
            generalNotesViewports.OfCategory(BuiltInCategory.OST_Viewports);

            FormatGeneralNote formatGeneralNote = new FormatGeneralNote();
            Transaction transaction = new Transaction(doc);

            //foreach (Viewport v in generalNotesViewports)
            //{
            //    Outline outline = v.GetBoxOutline();
            //    Line top = Line.CreateBound(new XYZ(outline.MinimumPoint.X, outline.MaximumPoint.Y, 0), new XYZ(outline.MaximumPoint.X, outline.MaximumPoint.Y, 0));
            //    Line bottom = Line.CreateBound(new XYZ(outline.MinimumPoint.X, outline.MinimumPoint.Y, 0), new XYZ(outline.MaximumPoint.X, outline.MinimumPoint.Y, 0));
            //    Line left = Line.CreateBound(new XYZ(outline.MinimumPoint.X, outline.MaximumPoint.Y, 0), new XYZ(outline.MinimumPoint.X, outline.MinimumPoint.Y, 0));
            //    Line right = Line.CreateBound(new XYZ(outline.MaximumPoint.X, outline.MaximumPoint.Y, 0), (new XYZ(outline.MaximumPoint.X, outline.MinimumPoint.Y, 0)));

            //    transaction.Start("Create Box");
            //    doc.Create.NewDetailCurve(doc.ActiveView, top);
            //    doc.Create.NewDetailCurve(doc.ActiveView, bottom);
            //    doc.Create.NewDetailCurve(doc.ActiveView, left);
            //    doc.Create.NewDetailCurve(doc.ActiveView, right);
            //    transaction.Commit();
            //}

            //------------------------------------
            Outline BK90Outlines = formatGeneralNote.generalNoteOutline(doc, doc.ActiveView);
            Line top2 = Line.CreateBound(new XYZ(BK90Outlines.MinimumPoint.X, BK90Outlines.MaximumPoint.Y, 0), new XYZ(BK90Outlines.MaximumPoint.X, BK90Outlines.MaximumPoint.Y, 0));
            Line bottom2 = Line.CreateBound(new XYZ(BK90Outlines.MinimumPoint.X, BK90Outlines.MinimumPoint.Y, 0), new XYZ(BK90Outlines.MaximumPoint.X, BK90Outlines.MinimumPoint.Y, 0));
            Line left2 = Line.CreateBound(new XYZ(BK90Outlines.MinimumPoint.X, BK90Outlines.MaximumPoint.Y, 0), new XYZ(BK90Outlines.MinimumPoint.X, BK90Outlines.MinimumPoint.Y, 0));
            Line right2 = Line.CreateBound(new XYZ(BK90Outlines.MaximumPoint.X, BK90Outlines.MaximumPoint.Y, 0), (new XYZ(BK90Outlines.MaximumPoint.X, BK90Outlines.MinimumPoint.Y, 0)));

            transaction.Start("Create Box");

            doc.Create.NewDetailCurve(doc.ActiveView, top2);
            doc.Create.NewDetailCurve(doc.ActiveView, bottom2);
            doc.Create.NewDetailCurve(doc.ActiveView, left2);
            doc.Create.NewDetailCurve(doc.ActiveView, right2);

            transaction.Commit();


            //------------------------------------
            BoundingBoxUV BoundBoxUV = (doc.ActiveView.Outline);
            
            

            Line top3 = Line.CreateBound(new XYZ(BoundBoxUV.Min.U, BoundBoxUV.Max.V, 0), new XYZ(BoundBoxUV.Max.U, BoundBoxUV.Max.V, 0));
            Line bottom3 = Line.CreateBound(new XYZ(BoundBoxUV.Min.U, BoundBoxUV.Min.V, 0), new XYZ(BoundBoxUV.Max.U, BoundBoxUV.Min.V, 0));
            Line left3 = Line.CreateBound(new XYZ(BoundBoxUV.Min.U, BoundBoxUV.Max.V, 0), new XYZ(BoundBoxUV.Min.U, BoundBoxUV.Min.V, 0));
            Line right3 = Line.CreateBound(new XYZ(BoundBoxUV.Max.U, BoundBoxUV.Max.V, 0), (new XYZ(BoundBoxUV.Max.U, BoundBoxUV.Min.V, 0)));
            

            transaction.Start("Create Bounding Box");

            doc.Create.NewDetailCurve(doc.ActiveView, top3);
            doc.Create.NewDetailCurve(doc.ActiveView, bottom3);
            doc.Create.NewDetailCurve(doc.ActiveView, left3);
            doc.Create.NewDetailCurve(doc.ActiveView, right3);

            transaction.Commit();


            return Result.Succeeded;

        }
    }
}
