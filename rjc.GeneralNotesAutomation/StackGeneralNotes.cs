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

            //create another list to collect next views to place after a column change
            List<ViewData> nextToPlaceViewData = new List<ViewData>();

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


                BoundingBoxXYZ boundingBoxXYZ = formatGeneralNote.GeneralNoteBoundingBox(doc, associatedView);


                double scale = (double)1 / associatedView.Scale;
                double viewLength = (boundingBoxXYZ.Max.Y - boundingBoxXYZ.Min.Y) * scale;
                ViewSheet viewSheet = doc.GetElement(v.SheetId) as ViewSheet;

                //Add ViewData
                //ViewData object containing elementID, view name, and view length
                viewData.Add(new ViewData
                {
                    ViewElementId = viewElementId,
                    ViewName = associatedView.Name,
                    ViewLength = viewLength,
                    //viewportElementId = viewportElementId,
                    SheetNumber = viewSheet.SheetNumber,
                    ViewportOriginX = boundingBoxXYZ.Max.X * scale,
                    ViewportOriginY = boundingBoxXYZ.Min.Y * scale,
                    CanPlaceOnSheet = true,
                    ViewportOutline = viewportOutline,
                    ViewRJCOfficeId = RJCOfficeID.AsString(),
                    ViewRJCStandardViewId = RJCStandardViewID.AsString()
                });
            }

            viewData = viewData
                .OrderBy(x => x.ViewRJCStandardViewId)
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

            #region user point selection
            //double boundingBoxBorder = 0.01; //in feet
            double typicalNoteWidth = unitConversion.mmToFt(160); //in 

            //prompt user to select new working origin
            uiDoc.ActiveView = doc.GetElement(sheetData[0].SheetId) as ViewSheet;
            XYZ topRightWorkingArea = uiDoc.Selection.PickPoint("Select Top Right Of Working Area");
            XYZ bottomLeftWorkingArea = uiDoc.Selection.PickPoint("Select Bottom Left Of Working Area");

            BoundingBoxXYZ workingAreaBoundingBox = new BoundingBoxXYZ
            {
                Max = topRightWorkingArea,
                Min = bottomLeftWorkingArea
            };
            //create a list to collect next views to resize
            List<ViewData> needToResize = new List<ViewData>();
            // check all general notes to see if they fit within the selected bounding box
            for (int i = 0; i < viewData.Count; i++)
            {
                if(viewData[i].ViewLength > (workingAreaBoundingBox.Max.Y - workingAreaBoundingBox.Min.Y))
                {
                    needToResize.Add(viewData[i]);
                }
            }
            if (needToResize.Count != 0)
            {                
                List<string> list = new List<string>();               
                for (int i = 0; i < needToResize.Count; i++)
                {   
                    //add all ViewNames to list to be added to a string
                    list.Add(needToResize[i].ViewName);                    
                }
                String oversizeViews = String.Join("\n", list.ToArray());
                TaskDialog oversizeViewDialog = new TaskDialog("NEED TO RESIZE VIEWS");
                oversizeViewDialog.CommonButtons = TaskDialogCommonButtons.Ok;


                oversizeViewDialog.MainInstruction = "The bounding box selected by the user is too small to fit some views.";
                oversizeViewDialog.MainContent = "If the bounding box needs to be this small" +
                                " to accommodate a small sheet size for example, you will need to split these views into multiple smaller views: \n\n" + oversizeViews;
                oversizeViewDialog.ExpandedContent = "An easy way to do this is: \n1: Right click the views in the Project Browser and select \"Duplicate View > Duplicate With Detailing\". \n\n2: Find both original and duplicated views and " +
                                    "add \"A,B,C,D\" etc. to the end of the view name. \n\n3: Delete content from original \"A\" note to make fit on desired sheet size,  then delete the content from \"B\" note " +
                                    "that is already on the \"A\" note you just edited. \n\n4: IN ORDER TO MAINTAIN THE SPLIT VIEWS ABILITY TO AUTO-STACK PROPERLY, PLEASE ADD \"A,B,C,D\" ETC. TO THE END OF THE \"RJC Standard View ID\" PARAMETER FOR EACH VIEW." +
                                    "\n\n5: Then place the new views onto any General Notes sheet and re-run the \"Stack General Notes\" tool.";
                TaskDialogResult tResult = oversizeViewDialog.Show();                              
            }

            //PickedBox pickedBox = uiDoc.Selection.PickBox(PickBoxStyle.Enclosing, "Select Working Sheet Area");

            XYZ protoOrigin = new XYZ(topRightWorkingArea.X - (typicalNoteWidth / 2), topRightWorkingArea.Y, 0);
            XYZ workingOrigin = protoOrigin;
            XYZ origin = protoOrigin;
            #endregion


            #region transaction to reposition titleblocks and delete viewports                   

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

            #region stack general notes
            transactionGroup.Start("Stack Drafting Views");

            int currentSheetIndex = 0;
            int currentColumn = 0;
            bool newCategoryGroupStarted = false;
            //int i = 0;
            int viewIndex = 0;
            int nextToPlaceViewIndex = 0;

            //calculate max number of columns
            double workingAreaWidth = topRightWorkingArea.X - bottomLeftWorkingArea.X;

            int maxNumberOfColumns = Convert.ToInt32(Math.Floor(workingAreaWidth / typicalNoteWidth)) - 1;

            ElementId viewElementIdToPlace = null;
            ElementId currentSheetElementId = sheetData[currentSheetIndex].SheetId;
            double currentViewLength = 0;
            int numberOfViewsToPlace = viewData.Count;

            int maxTries = 50;
            int tries = 0;

            //this loop will iterate until all the views have been placed
            while (numberOfViewsToPlace > 0 || tries > maxTries)
            {
                viewElementIdToPlace = null;
                viewIndex = 0;                
                newCategoryGroupStarted = false;

                try
                {
                    if (placedViewData.Count == 0)
                    {
                        PlaceView(viewData, viewIndex);
                        numberOfViewsToPlace--;
                    }
                    // follows this while loop until the new Category Group is changed.
                    while (newCategoryGroupStarted == false || nextToPlaceViewIndex < nextToPlaceViewData.Count)
                    {
                        //While current viewIndex hasn't reached the end of viewData 
                        while (viewIndex < viewData.Count)
                        {                            
                            // while the attempted view matches the Standard View ID of the last placed view, AND the view fits
                            while (CompareRjcSVI(viewData, viewIndex, placedViewData, placedViewData.Count - 1) && ((origin.Y - viewData[viewIndex].ViewLength) > workingAreaBoundingBox.Min.Y))
                            {
                                PlaceView(viewData, viewIndex);
                                numberOfViewsToPlace--;
                                tries = 0;
                            }
                            // while the attempted view matches the Standard View ID of the last placed view, AND the view DOESN'T fit
                            if (CompareRjcSVI(viewData, viewIndex, placedViewData, placedViewData.Count - 1) && ((origin.Y - viewData[viewIndex].ViewLength) < workingAreaBoundingBox.Min.Y))
                            {
                                nextToPlaceViewData.Add(viewData[viewIndex]);
                                viewData.RemoveAt(viewIndex);
                                numberOfViewsToPlace--;
                                tries = 0;
                            }                            
                            // if the attempted view DOESN'T match the Standard View ID of the last placed view
                            else
                            {
                                viewIndex++;
                            }
                            if (tries == maxTries)
                            {
                                break;
                            }
                            tries++;
                        }
                        // after trying to find all views in viewData set that match the category group, place saved nextToPlaceViewData views next, and 
                        // if that set is empty, place the next view in the viewData set regardless of match or not. 
                        if (viewIndex == viewData.Count)
                        {                            
                            // if there are any views that match the category group, but didn't fit on the same column, start new column and place rest of matching category group views
                            if (nextToPlaceViewData.Count > 0)
                            {
                                currentColumn++;
                                if (currentColumn > maxNumberOfColumns)
                                {
                                    currentSheetIndex++;
                                    currentSheetElementId = sheetData[currentSheetIndex].SheetId; // this is the like causing the out of range error. 
                                    currentColumn = 0;
                                }
                                nextToPlaceViewIndex = 0;
                                origin = protoOrigin;

                                // while there are 'saved views left in the category group'
                                while (nextToPlaceViewData.Count > 0)
                                {                                    
                                    if (nextToPlaceViewIndex == nextToPlaceViewData.Count && nextToPlaceViewData.Count != 0)
                                    {
                                        currentColumn++;
                                        if (currentColumn > maxNumberOfColumns)
                                        {
                                            currentSheetIndex++;
                                            currentSheetElementId = sheetData[currentSheetIndex].SheetId; // this is the like causing the out of range error. 
                                            currentColumn = 0;
                                        }
                                        nextToPlaceViewIndex = 0;
                                        origin = protoOrigin;
                                        PlaceView(nextToPlaceViewData, nextToPlaceViewIndex);
                                        tries = 0;
                                        continue;
                                    }
                                    // if the view fits
                                    else if ((origin.Y - nextToPlaceViewData[nextToPlaceViewIndex].ViewLength) > workingAreaBoundingBox.Min.Y)
                                    {
                                        PlaceView(nextToPlaceViewData, nextToPlaceViewIndex);
                                        tries = 0;
                                    }
                                    // if we've iterated through all of the nextToPlaceViewData list and they all don't fit in current column, only while there is still views in the list
                                    else
                                    {
                                        nextToPlaceViewIndex++;
                                    }
                                    //what to do if the conditions are never met
                                    if (nextToPlaceViewData.Count == 0)
                                    {
                                        newCategoryGroupStarted = true;
                                        nextToPlaceViewIndex = 0;
                                    }
                                    if (tries == maxTries)
                                    {
                                        break;
                                    }
                                    tries++;
                                }
                            }
                            else
                            {
                                viewIndex = 0;
                                newCategoryGroupStarted = true;
                                if (viewData.Count != 0)
                                {
                                    if ((origin.Y - viewData[viewIndex].ViewLength) > workingAreaBoundingBox.Min.Y)
                                    {
                                        PlaceView(viewData, viewIndex);
                                        numberOfViewsToPlace--;
                                        newCategoryGroupStarted = true;
                                    }
                                    else
                                    {
                                        currentColumn++;
                                        if (currentColumn > maxNumberOfColumns)
                                        {
                                            currentSheetIndex++;
                                            currentSheetElementId = sheetData[currentSheetIndex].SheetId; // this is the like causing the out of range error. 
                                            currentColumn = 0;
                                        }                                        
                                        origin = protoOrigin;
                                        PlaceView(viewData, viewIndex); 
                                        numberOfViewsToPlace--;
                                        newCategoryGroupStarted = true;
                                        tries = 0;                              
                                    }
                                }
                            }
                            if (tries == maxTries)
                            {
                                break;
                            }
                            tries++;
                        }                                           
                    }
                }              
                catch (Exception e)
                {
                    throw new Exception("There was an error during script execution, so please review your drawing, and you might want to undo the changes.\n" + e);                    
                }

                void PlaceView(List<ViewData> sourceList, int index)
                {
                    viewElementIdToPlace = sourceList[index].ViewElementId; // saves the elementId of the view to place next
                    currentViewLength = sourceList[index].ViewLength;
                    placedViewData.Add(sourceList[index]);
                    sourceList.RemoveAt(index);

                    View view = doc.GetElement(viewElementIdToPlace) as View;
                    BoundingBoxUV actualBoundingBoxUV = view.Outline;
                    BoundingBoxXYZ desiredBoundingBox = formatGeneralNote.GeneralNoteBoundingBox(doc, view);
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
            if (list1.Count == 0 || list2.Count == 0) {
                return false;
            }
            else if (list1[index1].ViewRJCStandardViewId.Substring(0, 3).DoesStringMatch(list2[index2].ViewRJCStandardViewId.Substring(0, 3)))
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
