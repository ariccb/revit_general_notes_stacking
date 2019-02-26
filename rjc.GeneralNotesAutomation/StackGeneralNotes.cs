using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

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

            //initialize utility classes
            UtilityClasses.Vectors vectorUtilities = new UtilityClasses.Vectors();
            UtilityClasses.Views viewUtilities = new UtilityClasses.Views();
            UtilityClasses.UnitConversion unitConversion = new UtilityClasses.UnitConversion();
            FormatGeneralNote formatGeneralNote = new FormatGeneralNote();

            #region  prompt user to select new working origin
            XYZ topRightWorkingArea = uiDoc.Selection.PickPoint("Select Top Right Of Working Area");
            XYZ bottomLeftWorkingArea = uiDoc.Selection.PickPoint("Select Bottom Left Of Working Area");
            //PickedBox pickedBox = uiDoc.Selection.PickBox(PickBoxStyle.Enclosing, "Select Working Sheet Area");
    
            #endregion
        
            #region get bounds of sheet view title block DEPRECATED
            /*
            FilteredElementCollector currentTitleBlockCollector = new FilteredElementCollector(doc);
            currentTitleBlockCollector
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .OwnedByView(currentView.Id);

            Element currentTitleBlock = currentTitleBlockCollector.FirstElement();
            BoundingBoxXYZ currentTitleBlockBounds = currentTitleBlock.get_BoundingBox(currentView);
            double workingOriginX = workingOrigin.X;
            double workingOriginY = workingOrigin.Y;
            double workingOriginZ = workingOrigin.Z;

            double titleBlockWidth = currentTitleBlockBounds.Max.X - currentTitleBlockBounds.Min.X;
            double titleBlockHeight = currentTitleBlockBounds.Max.Y - currentTitleBlockBounds.Min.Y;
            double rightBorderWidth = currentTitleBlockBounds.Max.X - workingOriginX;
            double topBorderWidth = currentTitleBlockBounds.Max.Y - workingOriginY;
            double bottomBorderWidth = topBorderWidth; //assumed for calc
            double leftBorderWidth = topBorderWidth; //assumed calc
            double sheetWorkingWidth = titleBlockWidth - rightBorderWidth - leftBorderWidth; //assumed calc
            double sheetWorkingHeight = (currentTitleBlockBounds.Max.Y - currentTitleBlockBounds.Min.Y) - topBorderWidth - bottomBorderWidth; //assumed calc

            double generalNoteWidth = unitConversion.mmToFt(160);
            int maxNumberOfColumns = Convert.ToInt32(Math.Floor(sheetWorkingWidth / generalNoteWidth))+1;
            */
            #endregion

            #region get drafting views on sheet
            //this collects the viewports which have been placed on sheets that are called or contain general notes

            FilteredElementCollector generalNotesViewports = CollectGeneralNotesViewports(doc);

            #endregion

            #region get sheets called general notes
            //this collects the sheets that are called "general notes"
            FilteredElementCollector generalNotesSheets = new FilteredElementCollector(doc);
            ParameterValueProvider parameterSheetNameProvider = new ParameterValueProvider(new ElementId((int)BuiltInParameter.SHEET_NAME));
            FilterStringRuleEvaluator sheetNameContainsEvaluator = new FilterStringContains();
            FilterStringRule filterSheetNameStringRule = new FilterStringRule(parameterSheetNameProvider, sheetNameContainsEvaluator, "General Notes", false);
            ElementParameterFilter sheetNameParameterFilter = new ElementParameterFilter(filterSheetNameStringRule);
            //collects sheets that are called "general notes"
            generalNotesSheets.OfCategory(BuiltInCategory.OST_Sheets).WherePasses(sheetNameParameterFilter);

            //create sheetdata instance and add to list of sheets
            List<SheetData> sheetData = new List<SheetData>();
            foreach(ViewSheet vs in generalNotesSheets)
            {
                sheetData.Add(new SheetData
                {
                    SheetId = vs.Id,
                    SheetName = vs.Name,
                    SheetNumber = vs.SheetNumber
                    
                });
                
            }

            sheetData = sheetData.OrderBy(x => x.SheetNumber).ToList();
            #endregion

            #region collect view data

            //create instance of view data list
            List<ViewData> viewData = new List<ViewData>();

            //iterate through viewports collects, and put view information in viewData
            foreach (Viewport v in generalNotesViewports)
            {
                ElementId viewElementId = v.ViewId;
                ElementId viewportElementId = v.Id;
                View associatedView = doc.GetElement(v.ViewId) as View;
                
                
                //get vertical length of note
                BoundingBoxUV outline = associatedView.Outline;

                UV topRightPoint = new UV(outline.Max.U, outline.Max.V);
                UV bottomLeftPoint = new UV(outline.Min.U, outline.Min.V);
                UV topLeftPoint = new UV(outline.Min.U, outline.Max.V);
                UV bottomRightPoint = new UV(outline.Max.U, outline.Min.V);

                //Outline sheetOutline = v.GetBoxOutline();

                BoundingBoxXYZ boundingBoxXYZ = formatGeneralNote.generalNoteBoundingBox(doc, associatedView);

                
                double scale = (double)1 / associatedView.Scale;
                double viewLength = (boundingBoxXYZ.Max.Y - boundingBoxXYZ.Min.Y) * scale;
                ViewSheet viewSheet = doc.GetElement(v.SheetId) as ViewSheet;

                //Add ViewData
                //ViewData item containing elementID, view name, and view length
                viewData.Add(new ViewData
                {
                    viewElementId = viewElementId,
                    viewName = associatedView.Name,
                    viewLength = viewLength,
                    //viewportElementId = viewportElementId,
                    sheetNumber = viewSheet.SheetNumber,
                    viewportOriginX = boundingBoxXYZ.Max.X * scale,
                    viewportOriginY = boundingBoxXYZ.Min.Y * scale,
                    canPlaceOnSheet = true
                });
            }

            viewData = viewData
                .OrderBy(x => x.sheetNumber)
                .ThenBy(x => x.viewportOriginY)
                .ThenBy(x => x.viewportOriginX)
                //.ThenBy(x => x.viewLength)
                .ToList();

            #endregion

            #region collect views called "no title"

            FilteredElementCollector viewTypeCollector = new FilteredElementCollector(doc);

            //get view type id for view type "No Title"
            ElementId typeId = viewTypeCollector
                .WhereElementIsElementType()
                .FirstOrDefault(n => n.Name == "No Title").Id;

            #endregion

            //OPTIMIZATION ALGORITHM
            //get list of all ids for views identified as a general notes
            //if note is on a sheet, remove it from the sheet. if not, do nothing
            //using list of view IDs, calculate and create list of note lengths
            //create new list of note length from smallest to largest
            //synchronously sort list of note IDs
            //Do this for all notes in project Identified as a General Note
            //insert first note on sheet
            //measure distance from bottom of note to bottom of sheet working area
            //search through list of notes to find best fittng note without going over sheet working area
            //if not view can be found, move over to new coloumn, repeat.
            //repeat process until column maximum is reached.
            //move to next sheet named "General Notes"
            //if no sheet exists, create new sheet named "General Notes"

            //for each drafting view get bounding box

            //viewData.Distinct() to remove duplicate items from list.

            //move general notes view to each view origin.
            //moves top right corner of note box to view origin (0,0,0)
            formatGeneralNote.MoveNoteToViewOrigin(doc);

            #region transaction to delete viewports and then recreate viewport

            double boundingBoxBorder = 0.01; //in feet
            double typicalNoteWidth = unitConversion.mmToFt(160); //in feet

            XYZ protoOrigin = new XYZ(topRightWorkingArea.X - (typicalNoteWidth / 2), topRightWorkingArea.Y, 0);
            XYZ workingOrigin = protoOrigin;
            XYZ nextOrigin = protoOrigin;

            Transaction transaction = new Transaction(doc);
            TransactionGroup transactionGroup = new TransactionGroup(doc);

            transactionGroup.Start("Stack Drafting Views");
            //remove viewports from sheets so they can be placed fresh
            foreach (ViewSheet vs in generalNotesSheets)
            {
                List<ElementId> viewPortIds = new List<ElementId>(vs.GetAllViewports());
                foreach (ElementId vpId in viewPortIds)
                {

                    Viewport vp = doc.GetElement(vpId) as Viewport;
                    transaction.Start("Delete Viewport");
                    vs.DeleteViewport(vp);
                    transaction.Commit();
                }

            }

            int currentSheetIndex = 0;
            int currentColumn = 1;
            int currentViewIndex = 0;
            int checkIndex = 0;
            int nextViewIndex = currentViewIndex + 1;
            bool newColumnStarted = false;

            //calculate max number of columns
            double workingAreaWidth = topRightWorkingArea.X - bottomLeftWorkingArea.X;
            int maxNumberOfColumns = Convert.ToInt32(Math.Floor(workingAreaWidth / typicalNoteWidth)) + 1;

            ElementId viewElementIdToPlace = null;
            ElementId currentSheetElementId = sheetData[currentSheetIndex].SheetId;
            double currentViewLength = 0;
            
            //try for loop
            for(currentViewIndex=0; currentViewIndex<viewData.Count; currentViewIndex++)
            {
                checkIndex = currentViewIndex;

                if (nextOrigin.Y - viewData[currentViewIndex].viewLength > bottomLeftWorkingArea.Y)
                {
                    viewElementIdToPlace = viewData[currentViewIndex].viewElementId;
                    currentViewLength = viewData[currentViewIndex].viewLength;

                    if (currentViewIndex == 0)
                    {
                        newColumnStarted = true;
                    }

                    else { newColumnStarted = false; }
                }

                else
                {
                    while (nextOrigin.Y - viewData[checkIndex].viewLength < bottomLeftWorkingArea.Y)
                    {
                        if (checkIndex == (viewData.Count-1))
                        {
                            break;
                        }

                        checkIndex++;
                        

                    }

                    nextOrigin = new XYZ(nextOrigin.X - (typicalNoteWidth), protoOrigin.Y, 0);
                    newColumnStarted = true;

                    
                    viewElementIdToPlace = viewData[checkIndex].viewElementId;
                    currentViewLength = viewData[checkIndex].viewLength;
                    viewData.RemoveAt(checkIndex);
                }

                View view = doc.GetElement(viewElementIdToPlace) as View;
                BoundingBoxUV actualBoundingBoxUV = view.Outline;
                BoundingBoxXYZ desiredBoundingBox = formatGeneralNote.generalNoteBoundingBox(doc, view);
                double dTop = actualBoundingBoxUV.Max.V - desiredBoundingBox.Max.Y*((double)1/view.Scale);
                double dBottom = actualBoundingBoxUV.Min.V - desiredBoundingBox.Min.Y*((double)1/view.Scale);
                double dModifier = 0;

                if(newColumnStarted)
                {
                    dModifier = ((dTop - dBottom) / 2);
                }

                XYZ placementPoint = new XYZ(nextOrigin.X, (nextOrigin.Y) - (currentViewLength / 2) + dModifier, 0);
                transaction.Start("Place Viewport");
                Viewport.Create(doc, currentSheetElementId, viewElementIdToPlace, placementPoint);
                transaction.Commit();

                transaction.Start("Change Type");

                List<ElementId> elementIds = new List<ElementId>(CollectGeneralNotesViewports(doc).ToElementIds());
                Element.ChangeTypeId(doc,elementIds, typeId);

                transaction.Commit();

                nextOrigin = new XYZ(placementPoint.X, (placementPoint.Y) - (currentViewLength / 2), 0);

            }
            
            transactionGroup.Commit();

            #endregion

            /*
            transaction.Start("Move Drafting View");

            List<ElementId> draftingViewElementIds = new List<ElementId>();
            foreach (var vd in viewData)
            {
                draftingViewElementIds.Add(vd.viewElementId);
                View view = doc.GetElement(vd.viewElementId) as View;
            }
            Element.ChangeTypeId(doc, draftingViewElementIds, typeId);

            int currentColumn = 0;


            foreach (ElementId e in draftingViewElementIds)
            {
                Viewport vp = doc.GetElement(e) as Viewport;

                ElementId associateViewId = vp.ViewId;
                View associatedView = doc.GetElement(associateViewId) as View;

                string viewTitle = associatedView.Name.ToString();

                
                BoundingBoxUV associatedViewBoundUV = associatedView.Outline;
                GeometryElement geometryElement = vp.get_Geometry(new Options());

                Outline outline = vp.GetBoxOutline();

                //Working points of bounding box
                XYZ topRightPoint = new XYZ(outline.MaximumPoint.X - BoundingBoxBorder, outline.MaximumPoint.Y - BoundingBoxBorder, 0);
                XYZ bottomLeftPoint = new XYZ(outline.MinimumPoint.X + BoundingBoxBorder, outline.MinimumPoint.Y + BoundingBoxBorder, 0);
                XYZ topLeftPoint = new XYZ(outline.MinimumPoint.X + BoundingBoxBorder, outline.MaximumPoint.Y - BoundingBoxBorder, 0);
                XYZ bottomRightPoint = new XYZ(outline.MaximumPoint.X - BoundingBoxBorder, outline.MinimumPoint.Y + BoundingBoxBorder, 0);

                //calculate distance  and vector from top right corner of bouning box to clicked corner, this is first translation vector,
                ElementTransformUtils.MoveElement(doc, e, vectorUtilities.TwoPointVector(topRightPoint, nextOrigin));

                //get bounding box again to determine location of next origin point
                outline = vp.GetBoxOutline();
                nextOrigin = new XYZ(workingOriginX-(currentColumn*generalNoteWidth), outline.MinimumPoint.Y+BoundingBoxBorder, 0);
            }
            

            transaction.Commit();
            */


            //iterate to next index. using bottom right corned of index-1 bounding box to find new top right of current index.
            //calculate translation vector as before

            return Result.Succeeded;
        }

        public FilteredElementCollector CollectGeneralNotesViewports(Document doc)
        {
            //this collects the viewports which have been placed on sheets that are called or contain general notes

            FilteredElementCollector generalNotesViewports = new FilteredElementCollector(doc);
            ParameterValueProvider parameterViewportSheetNameProvider = new ParameterValueProvider(new ElementId((int)BuiltInParameter.VIEWPORT_SHEET_NAME));
            FilterStringRuleEvaluator sheetNameContainsEvaluator = new FilterStringContains();
            FilterStringRule filterViewportSheetNameStringRule = new FilterStringRule(parameterViewportSheetNameProvider, sheetNameContainsEvaluator, "General Notes", false);
            ElementParameterFilter viewportSheetNameParameterFilter = new ElementParameterFilter(filterViewportSheetNameStringRule);
            //collects viewports that are on sheets called "general notes"
            generalNotesViewports.OfCategory(BuiltInCategory.OST_Viewports).WherePasses(viewportSheetNameParameterFilter);

            return generalNotesViewports;
        }

        
    }
}
