using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;

namespace rjc.GeneralNotesAutomation
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class StackGeneralNotes : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //get active view as view
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            View currentView = uiDoc.ActiveView;

            //get drafting views in view

            FilteredElementCollector draftingViews = new FilteredElementCollector(doc);
            List<ElementId> elementIds = draftingViews.OfCategory(BuiltInCategory.OST_Viewports).ToElementIds().ToList();

            FilteredElementCollector viewTypeCollector = new FilteredElementCollector(doc);
            //viewTypeCollector.OfCategory(BuiltInCategory.OST_ViewportLabel).FirstOrDefault(n => n.Name == "No Title");
            //ElementIsElementTypeFilter filter = new ElementIsElementTypeFilter();

            ElementId typeId = viewTypeCollector.WhereElementIsElementType().FirstOrDefault(n => n.Name == "No Title").Id;

            //click top left corned of sheet to align
            //Use origin for now
            double sheetOriginX = 3.561639;
            double sheetOriginY = 2.932821;
            double sheetOriginZ = 0;

            //max sizes for 36x48
            double maxSheetHeight = 2.868767;
            double maxSheetWidth = 3.429364;

            XYZ origin = new XYZ(sheetOriginX, sheetOriginY, sheetOriginZ);
            //XYZ origin = new XYZ();
            XYZ nextOrigin = origin;
            double BoundingBoxBorder = 0.01; //in feet


            //for each drafting view get bounding box

            Transaction transaction = new Transaction(doc);
            transaction.Start("Move Drafting View");

            Element.ChangeTypeId(doc, elementIds, typeId);

            Frame frame = new Frame();
            Plane.Create(frame);
            Arc.Create(Plane.Create(frame), .1, 0, 6.28319);

            doc.Create.NewDetailCurve(currentView, Arc.Create(Plane.Create(frame), .1, 0, 6.28319));


            foreach (ElementId e in elementIds)
            {
                Viewport vp = doc.GetElement(e) as Viewport;

                ElementId associateViewId = vp.ViewId;
                View associatedView = doc.GetElement(associateViewId) as View;

                string viewTitle = associatedView.Name.ToString();

                
                BoundingBoxUV associatedViewBoundUV = associatedView.Outline;
                GeometryElement geometryElement = vp.get_Geometry(new Options());


                //reconcile sheet coordinate systems and view coordinate system by matching the origins of both.
                //The origin view must be moved to the origin of the sheet. Then any location on the view will match the sheet.

                //Arc arc1 = Arc.Create(new XYZ(associatedViewBoundUV.Max.U, associatedViewBoundUV.Max.V, 0), .05, 0, 2 * Math.PI, new XYZ(1, 0, 0), new XYZ(0, 1, 0));
                //Arc arc2 = Arc.Create(new XYZ(associatedViewBoundUV.Min.U, associatedViewBoundUV.Min.V, 0), .05, 0, 2 * Math.PI, new XYZ(1, 0, 0), new XYZ(0, 1, 0));
                //doc.Create.NewDetailCurve(currentView, arc1);
                //doc.Create.NewDetailCurve(currentView, arc2);

                //doc.Create.NewDetailCurve(currentView, Arc.Create(Plane.Create(new Frame(new XYZ(associatedViewBoundUV.Max.U, associatedViewBoundUV.Max.V, 0), new XYZ(1, 0, 0), new XYZ(0, 1, 0), new XYZ(0, 0, 1))), .1, 0, 6.28319));
                //doc.Create.NewDetailCurve(currentView, Arc.Create(Plane.Create(new Frame(new XYZ(associatedViewBoundUV.Min.U,associatedViewBoundUV.Min.V,0),new XYZ(1,0,0),new XYZ(0,1,0),new XYZ(0,0,1))), .1, 0, 6.28319));

                Outline outline = vp.GetBoxOutline();

                //Working points of bounding box
                XYZ topRightPoint = new XYZ(outline.MaximumPoint.X - BoundingBoxBorder, outline.MaximumPoint.Y - BoundingBoxBorder, 0);
                XYZ bottomLeftPoint = new XYZ(outline.MinimumPoint.X + BoundingBoxBorder, outline.MinimumPoint.Y + BoundingBoxBorder, 0);
                XYZ topLeftPoint = new XYZ(outline.MinimumPoint.X + BoundingBoxBorder, outline.MaximumPoint.Y - BoundingBoxBorder, 0);
                XYZ bottomRightPoint = new XYZ(outline.MaximumPoint.X - BoundingBoxBorder, outline.MinimumPoint.Y + BoundingBoxBorder, 0);
                //XYZ topRightPoint = new XYZ(outline.MaximumPoint.X, outline.MaximumPoint.Y, 0);
                //XYZ bottomLeftPoint = new XYZ(outline.MinimumPoint.X, outline.MinimumPoint.Y, 0);
                //XYZ topLeftPoint = new XYZ(outline.MinimumPoint.X, outline.MaximumPoint.Y, 0);
                //XYZ bottomRightPoint = new XYZ(outline.MaximumPoint.X, outline.MinimumPoint.Y, 0);

                doc.Create.NewDetailCurve(currentView, Line.CreateBound(topLeftPoint, topRightPoint));
                doc.Create.NewDetailCurve(currentView, Line.CreateBound(topRightPoint, bottomRightPoint));
                doc.Create.NewDetailCurve(currentView, Line.CreateBound(bottomRightPoint, bottomLeftPoint));
                doc.Create.NewDetailCurve(currentView, Line.CreateBound(bottomLeftPoint, topLeftPoint));



                rjc.UtilityClasses.Vectors createVectors = new UtilityClasses.Vectors();


                //calculate distance  and vector from top right corner of bouning box to clicked corner, this is first translation vector,
                ElementTransformUtils.MoveElement(doc, e, createVectors.TwoPointVector(topRightPoint, nextOrigin));

                //get bounding box again to determine location of next origin point
                outline = vp.GetBoxOutline();
                nextOrigin = new XYZ(outline.MaximumPoint.X-BoundingBoxBorder, outline.MinimumPoint.Y+BoundingBoxBorder, 0);
            }

            transaction.Commit();





            //iterate to next index. using bottom right corned of index-1 bounding box to find new top right of current index.
            //calculate translation vector as before

            return Result.Succeeded;
        }
    }
}
