using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.Creation;

namespace rjc.GeneralNotesAutomation
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class FormatGeneralNote
    {
        //adding this line to test branches
        public void MoveNoteToViewOrigin(Autodesk.Revit.DB.Document doc)
        {
            //UIApplication uiApp = commandData.Application;
            //Autodesk.Revit.DB.Document doc = uiApp.ActiveUIDocument.Document;

            UtilityClasses.Vectors vectorUtilities = new UtilityClasses.Vectors();
            UtilityClasses.Views viewUtilities = new UtilityClasses.Views();
            UtilityClasses.UnitConversion unitConversion = new UtilityClasses.UnitConversion();


            //this collects the viewports which have been placed on sheets that are called or contain general notes
            FilteredElementCollector draftingViews = new FilteredElementCollector(doc);
            FilteredElementCollector generalNotesViewports = new FilteredElementCollector(doc);
            ParameterValueProvider parameterViewportSheetNameProvider = new ParameterValueProvider(new ElementId((int)BuiltInParameter.VIEWPORT_SHEET_NAME));
            FilterStringRuleEvaluator sheetNameContainsEvaluator = new FilterStringContains();
            FilterStringRule filterViewportSheetNameStringRule = new FilterStringRule(parameterViewportSheetNameProvider, sheetNameContainsEvaluator, "General Notes", false);
            ElementParameterFilter viewportSheetNameParameterFilter = new ElementParameterFilter(filterViewportSheetNameStringRule);
            //collects viewports that are on sheets called "general notes"
            generalNotesViewports.OfCategory(BuiltInCategory.OST_Viewports).WherePasses(viewportSheetNameParameterFilter);

            List<View> generalNotesViews = new List<View>();
            Transaction transaction = new Transaction(doc);
            TransactionGroup transactionGroup = new TransactionGroup(doc);

            transactionGroup.Start("Format Notes");

            foreach (Viewport v in generalNotesViewports)
            {
                try
                {
                    View view = doc.GetElement(v.ViewId) as View;
                    generalNotesViews.Add(view);

                    //create bounding box
                    BoundingBoxXYZ boundingBoxXYZ = generalNoteBoundingBox(doc, view);

                    //collect all the elements in the view
                    //to move them
                    FilteredElementCollector allElementsInView = new FilteredElementCollector(doc, v.ViewId);
                    allElementsInView.WhereElementIsNotElementType();
                    Group group = null;

                    XYZ moveVector = vectorUtilities.TwoPointVector(boundingBoxXYZ.Max, new XYZ());


                    transaction.Start("Format Note");

                    group = doc.Create.NewGroup(allElementsInView.ToElementIds());
                    ElementTransformUtils.MoveElement(doc, group.Id, moveVector);

                    //use the following lines to confirm the view origin point. comment out for release
                    //Arc arc1 = Arc.Create(new XYZ(), .05, 0, 2 * Math.PI, new XYZ(1, 0, 0), new XYZ(0, 1, 0));
                    //Arc arc2 = Arc.Create(new XYZ(maxX, maxY, 0), .05, 0, 2 * Math.PI, new XYZ(1, 0, 0), new XYZ(0, 1, 0));
                    //doc.Create.NewDetailCurve(view, arc1);
                    //doc.Create.NewDetailCurve(view, arc2);

                    group.UngroupMembers();

                    transaction.Commit();
                }

                catch { }
            }

            transactionGroup.Assimilate();
        }

        public BoundingBoxXYZ generalNoteBoundingBox(Autodesk.Revit.DB.Document doc, View view)
        {
            FilteredElementCollector BK90ElementsCollector = new FilteredElementCollector(doc, view.Id);

            ParameterValueProvider GraphicStyleProvider = new ParameterValueProvider(new ElementId((int)BuiltInParameter.BUILDING_CURVE_GSTYLE));
            FilterStringRuleEvaluator BK90Evaluator = new FilterStringContains();
            FilterStringRule BK90FilterRule = new FilterStringRule(GraphicStyleProvider, BK90Evaluator, "S-ANNO-BK90", false);
            ElementParameterFilter BK90ElementParameterFilter = new ElementParameterFilter(BK90FilterRule);
            BK90ElementsCollector.OfCategory(BuiltInCategory.OST_Lines).WherePasses(BK90ElementParameterFilter);

            List<XYZ> BK90PointList = new List<XYZ>();

            foreach (DetailLine d in BK90ElementsCollector)
            {
                DetailCurve detailCurve = d as DetailCurve;
                XYZ startPoint = detailCurve.GeometryCurve.GetEndPoint(0);
                XYZ endPoint = detailCurve.GeometryCurve.GetEndPoint(1);
                BK90PointList.Add(startPoint);
                BK90PointList.Add(endPoint);
            }

            //BK90PointList now contains all the points in the view
            //need to find point with maximum x and y values, and point with min x and y values
            double maxX = BK90PointList.Max(point => point.X);
            double maxY = BK90PointList.Max(point => point.Y);
            double minX = BK90PointList.Min(point => point.X);
            double minY = BK90PointList.Min(point => point.Y);


            //create bounding box
            BoundingBoxXYZ boundingBoxXYZ = new BoundingBoxXYZ();
            boundingBoxXYZ.Max = new XYZ(maxX, maxY, 0);
            boundingBoxXYZ.Min = new XYZ(minX, minY, 0);

            return boundingBoxXYZ;

        }

        public Outline generalNoteOutline(Autodesk.Revit.DB.Document doc, View view)
        {
            FilteredElementCollector BK90ElementsCollector = new FilteredElementCollector(doc, view.Id);

            ParameterValueProvider GraphicStyleProvider = new ParameterValueProvider(new ElementId((int)BuiltInParameter.BUILDING_CURVE_GSTYLE));
            FilterStringRuleEvaluator BK90Evaluator = new FilterStringContains();
            FilterStringRule BK90FilterRule = new FilterStringRule(GraphicStyleProvider, BK90Evaluator, "S-ANNO-BK90", false);
            ElementParameterFilter BK90ElementParameterFilter = new ElementParameterFilter(BK90FilterRule);
            BK90ElementsCollector.OfCategory(BuiltInCategory.OST_Lines).WherePasses(BK90ElementParameterFilter);

            List<XYZ> BK90PointList = new List<XYZ>();

            foreach (DetailLine d in BK90ElementsCollector)
            {
                DetailCurve detailCurve = d as DetailCurve;
                XYZ startPoint = detailCurve.GeometryCurve.GetEndPoint(0);
                XYZ endPoint = detailCurve.GeometryCurve.GetEndPoint(1);
                BK90PointList.Add(startPoint);
                BK90PointList.Add(endPoint);
            }

            //BK90PointList now contains all the points in the view
            //need to find point with maximum x and y values, and point with min x and y values
            double maxX = BK90PointList.Max(point => point.X);
            double maxY = BK90PointList.Max(point => point.Y);
            double minX = BK90PointList.Min(point => point.X);
            double minY = BK90PointList.Min(point => point.Y);


            //create bounding box
            XYZ maximumPoint = new XYZ(maxX, maxY, 0);
            XYZ minimumPoint = new XYZ(minX, minY, 0);
            Outline outline = new Outline(minimumPoint,maximumPoint);

            return outline;

        }

        public double generalNoteLength(BoundingBoxXYZ boundingBoxXYZ)
        {
            return boundingBoxXYZ.Max.Y - boundingBoxXYZ.Min.Y;
        }

        public double ScaledGeneralNoteLength(BoundingBoxXYZ boundingBoxXYZ, View view)
        {
            int viewScale = view.Scale;
            return (boundingBoxXYZ.Max.Y - boundingBoxXYZ.Min.Y)*((double)1/view.Scale);
        }
    }
}
