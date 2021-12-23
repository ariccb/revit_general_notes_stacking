﻿using System;
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
using System.Diagnostics;
using rjc.UtilityClasses;
using System.Text.RegularExpressions;


namespace rjc.GeneralNotesAutomation
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class StackGeneralNotes : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region initialization
            //get active view as view
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;
            View currentView = uiDoc.ActiveView;

            //instance transaction class
            Transaction transaction = new Transaction(doc);
            TransactionGroup transactionGroup = new TransactionGroup(doc);

            //initialize utility classes
            Vectors vectorUtilities = new Vectors();
            Views viewUtilities = new Views();
            UnitConversion unitConversion = new UnitConversion();
            FormatGeneralNote formatGeneralNote = new FormatGeneralNote();

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
            foreach (ViewSheet vs in generalNotesSheets)
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

            #region get titleblocks on sheets called general notes

            //this collects the sheets that are called "general notes"
            FilteredElementCollector gnTitleblocks = new FilteredElementCollector(doc);
            ParameterValueProvider parameterGnTitleblockProvider = new ParameterValueProvider(new ElementId((int)BuiltInParameter.SHEET_NAME));
            FilterStringRuleEvaluator gnTitleblockContainsEvaluator = new FilterStringContains();
            FilterStringRule filterGnTitleblockStringRule = new FilterStringRule(parameterSheetNameProvider, sheetNameContainsEvaluator, "General Notes", false);
            ElementParameterFilter gnTitleblockParameterFilter = new ElementParameterFilter(filterSheetNameStringRule);
            //collects sheets that are called "general notes"
            gnTitleblocks.OfCategory(BuiltInCategory.OST_TitleBlocks).WherePasses(sheetNameParameterFilter);

            #endregion

            #region collect view data

            //create instance of view data list
            List<ViewData> viewData = new List<ViewData>();

            //create another list to collect placed views-to maintain access to the viewdata 
            List<ViewData> placedViewData = new List<ViewData>();

            //iterate through viewports collects, and put view information in viewData
            foreach (Viewport v in generalNotesViewports)
            {
                ElementId viewElementId = v.ViewId;
                ElementId viewportElementId = v.Id;
                View associatedView = doc.GetElement(v.ViewId) as View;
                Parameter RJCStandardViewID = v.LookupParameter("RJC Standard View ID");
                Parameter RJCOfficeID = v.LookupParameter("RJC Office ID");

                //get vertical length of note
                BoundingBoxUV outline = associatedView.Outline;

                UV topRightPoint = new UV(outline.Max.U, outline.Max.V);
                UV bottomLeftPoint = new UV(outline.Min.U, outline.Min.V);
                UV topLeftPoint = new UV(outline.Min.U, outline.Max.V);
                UV bottomRightPoint = new UV(outline.Max.U, outline.Min.V);

                Outline viewportOutline = v.GetBoxOutline();


                BoundingBoxXYZ boundingBoxXYZ = formatGeneralNote.generalNoteBoundingBox(doc, associatedView);


                double scale = (double)1 / associatedView.Scale;
                double viewLength = (boundingBoxXYZ.Max.Y - boundingBoxXYZ.Min.Y) * scale;
                ViewSheet viewSheet = doc.GetElement(v.SheetId) as ViewSheet;

                //Add ViewData
                //ViewData object containing elementID, view name, and view length
                viewData.Add(new ViewData
                {
                    viewElementId = viewElementId,
                    viewName = associatedView.Name,
                    viewLength = viewLength,
                    //viewportElementId = viewportElementId,
                    sheetNumber = viewSheet.SheetNumber,
                    viewportOriginX = boundingBoxXYZ.Max.X * scale,
                    viewportOriginY = boundingBoxXYZ.Min.Y * scale,
                    canPlaceOnSheet = true,
                    viewportOutline = viewportOutline,
                    viewRJCOfficeId = RJCOfficeID.AsString(),
                    viewRJCStandardViewId = RJCStandardViewID.AsString()
                });
            }

            viewData = viewData
                .OrderBy(x => x.viewRJCStandardViewId)
                //.ThenByDescending(x => x.viewportOriginX)
                //.ThenByDescending(x => x.viewportOriginY)
                //.ThenBy(x => x.viewLength)
                .ToList();

            #endregion

            #region get view type id "no title"

            FilteredElementCollector viewTypeCollector = new FilteredElementCollector(doc);

            //get view type id for view type "No Title"
            ElementId typeId = viewTypeCollector
                .WhereElementIsElementType()
                .FirstOrDefault(n => n.Name == "No Title").Id;

            #endregion

            #region format general note views

            formatGeneralNote.MoveNoteToViewOrigin(doc);

            #endregion

            #region transaction to reposition titleblocks and delete viewports

            //double boundingBoxBorder = 0.01; //in feet
            double typicalNoteWidth = unitConversion.mmToFt(160); //in 
            //remove viewports from sheets so they can be placed fresh
            transactionGroup.Start("Delete Viewports");

            //reposition general notes title blocks
            foreach (Element e in gnTitleblocks)
            {
                ViewSheet viewSheet = doc.GetElement(e.OwnerViewId) as ViewSheet;
                BoundingBoxXYZ boundingBoxXYZ = e.get_BoundingBox(viewSheet);

                transaction.Start("Reposition Titleblock");
                ElementTransformUtils.MoveElement(doc, e.Id, vectorUtilities.TwoPointVector(boundingBoxXYZ.Min, viewSheet.Origin));
                transaction.Commit();
            }

            //collect viewport ids and delete viewports
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

            transactionGroup.Assimilate();

            #endregion

            #region user point selection
            //prompt user to select new working origin
            uiDoc.ActiveView = doc.GetElement(sheetData[0].SheetId) as ViewSheet;
            XYZ topRightWorkingArea = uiDoc.Selection.PickPoint("Select Top Right Of Working Area");
            XYZ bottomLeftWorkingArea = uiDoc.Selection.PickPoint("Select Bottom Left Of Working Area");

            BoundingBoxXYZ workingAreaBoundingBox = new BoundingBoxXYZ();
            workingAreaBoundingBox.Max = topRightWorkingArea;
            workingAreaBoundingBox.Min = bottomLeftWorkingArea;

            //PickedBox pickedBox = uiDoc.Selection.PickBox(PickBoxStyle.Enclosing, "Select Working Sheet Area");

            XYZ protoOrigin = new XYZ(topRightWorkingArea.X - (typicalNoteWidth / 2), topRightWorkingArea.Y, 0);
            XYZ workingOrigin = protoOrigin;
            XYZ origin = protoOrigin;
            #endregion

            #region stack general notes
            transactionGroup.Start("Stack Drafting Views");

            int currentSheetIndex = 0;
            int currentColumn = 0;
            int currentViewIndex = 0;
            int i = 0;
            //int indexToPlace = 0;
            int nextViewIndex = currentViewIndex + 1;
            int viewIndex = 0;

            //calculate max number of columns
            double workingAreaWidth = topRightWorkingArea.X - bottomLeftWorkingArea.X;

            int maxNumberOfColumns = Convert.ToInt32(Math.Floor(workingAreaWidth / typicalNoteWidth)) - 1;

            ElementId viewElementIdToPlace = null;
            ElementId currentSheetElementId = sheetData[currentSheetIndex].SheetId;
            double currentViewLength = 0;
            int numberOfViewsToPlace = viewData.Count;




            /*for file in os.listdir(file_path):
            if fnmatch.fnmatch(file, 'STR-STD-00?-*' + units + ' Notes - Revit 20??.rvt'):

            }*/


            //try for loop
            //this loop will try to select the index of the view to place next.
            for (i = 0; i < numberOfViewsToPlace; i++)
            {
                viewElementIdToPlace = null;
                viewIndex = 0;



                if ((origin.Y - viewData[viewIndex].viewLength) > workingAreaBoundingBox.Min.Y)
                {
                    viewElementIdToPlace = viewData[viewIndex].viewElementId; // saves the elementId of the view to place next
                    currentViewLength = viewData[viewIndex].viewLength;
                    placedViewData.Add(viewData[viewIndex]);
                    viewData.RemoveAt(viewIndex);    // need to move this to a NEW list instead of deleting it, because we still want to be able to
                                                     // use the data from the last View.
                }

                else
                {               //this while loop is breaking when the RJC standard view ID goes from N1*** to N2***
                    while ((origin.Y - viewData[viewIndex].viewLength) < workingAreaBoundingBox.Min.Y && placedViewData.Count != 0 && CompareRjcSVI(viewData, viewIndex, placedViewData, placedViewData.Count - 1))
                    {
                        viewIndex++;

                        //what to do if the conditions are never met
                        if (viewIndex == viewData.Count)
                        {
                            currentColumn++;
                            if (currentColumn > maxNumberOfColumns)
                            {
                                currentSheetIndex++;
                                currentSheetElementId = sheetData[currentSheetIndex].SheetId; // need to add code to duplicate sheet if currentSheetIndex
                                currentColumn = 0;                                            // is smaller than indexes in sheetData
                            }
                            viewIndex = 0;
                            origin = protoOrigin;
                            break;
                        }
                    }

                    viewElementIdToPlace = viewData[viewIndex].viewElementId;
                    currentViewLength = viewData[viewIndex].viewLength;
                    viewData.RemoveAt(viewIndex);
                }

                View view = doc.GetElement(viewElementIdToPlace) as View;
                BoundingBoxUV actualBoundingBoxUV = view.Outline;
                BoundingBoxXYZ desiredBoundingBox = formatGeneralNote.generalNoteBoundingBox(doc, view);
                double dTop = actualBoundingBoxUV.Max.V - desiredBoundingBox.Max.Y * ((double)1 / view.Scale);
                double dBottom = desiredBoundingBox.Min.Y * ((double)1 / view.Scale) - actualBoundingBoxUV.Min.V;
                double dModifierY = ((dTop - dBottom) / 2);

                double dRight = actualBoundingBoxUV.Max.U - desiredBoundingBox.Max.X * ((double)1 / view.Scale);
                double dLeft = desiredBoundingBox.Min.X * ((double)1 / view.Scale) - actualBoundingBoxUV.Min.U;
                double dModifierX = ((dRight - dLeft) / 2);

                XYZ placementPoint = new XYZ(protoOrigin.X - (currentColumn * typicalNoteWidth), (origin.Y) - (currentViewLength / 2), 0);
                XYZ modifiedPlacementPoint = new XYZ(protoOrigin.X - (currentColumn * typicalNoteWidth) + dModifierX, (origin.Y) - (currentViewLength / 2) + dModifierY, 0);

                transaction.Start("Place Viewport");
                Viewport.Create(doc, currentSheetElementId, viewElementIdToPlace, modifiedPlacementPoint);
                transaction.Commit();

                origin = new XYZ(placementPoint.X, (placementPoint.Y) - (currentViewLength / 2), 0);

            }

            transaction.Start("Change Type");

            List<ElementId> elementIds = new List<ElementId>(CollectGeneralNotesViewports(doc).ToElementIds());
            Element.ChangeTypeId(doc, elementIds, typeId);

            transaction.Commit();

            transactionGroup.Assimilate();

            #endregion

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

        public bool CompareRjcSVI(List<ViewData> list1, int index1, List<ViewData> list2, int index2)
        {

            if (list1[index1].viewRJCStandardViewId.Substring(0, 2).DoesStringMatch(list2[index2].viewRJCStandardViewId.Substring(0, 2)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
