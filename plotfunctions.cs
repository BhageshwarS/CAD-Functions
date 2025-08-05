using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.PlottingServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace MYCOLLECTION
{
    class plotfunctions
    {
        public void PlotModelSpace(string rootFolder, Document finalDoc, Database acCurDb)

        {
            // CadFunctions cadFunctions = new CadFunctions();
            // cadFunctions.ZoomAll(finalDoc);
            List<Extents2d> plotExt = TitleBlockExtents(finalDoc);
            for (int i = 0; i < plotExt.Count; i++)
            {
                using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                {
                    BlockTableRecord btr = (BlockTableRecord)acTrans.GetObject(finalDoc.Database.CurrentSpaceId, OpenMode.ForRead);
                    Layout acLayout = (Layout)acTrans.GetObject(btr.LayoutId, OpenMode.ForRead);
                    using (PlotInfo acPlInfo = new PlotInfo())
                    {
                        acPlInfo.Layout = acLayout.ObjectId;
                        using (PlotSettings acPlSet = new PlotSettings(acLayout.ModelType))
                        {
                            acPlSet.CopyFrom(acLayout);
                            PlotSettingsValidator acPlSetVdr = PlotSettingsValidator.Current;
                            acPlSetVdr.SetUseStandardScale(acPlSet, true);
                            acPlSetVdr.SetStdScaleType(acPlSet, StdScaleType.ScaleToFit);
                            acPlSetVdr.SetPlotCentered(acPlSet, true);
                            foreach (string plotStyle in PlotSettingsValidator.Current.GetPlotStyleSheetList())
                            {
                                if (plotStyle == "monochrome.ctb")
                                {
                                    acPlSetVdr.SetCurrentStyleSheet(acPlSet, "monochrome.ctb");
                                    break;
                                }
                            }
                            double width = plotExt[i].MaxPoint.X - plotExt[i].MinPoint.X;
                            double height = plotExt[i].MaxPoint.Y - plotExt[i].MinPoint.Y;
                            if (width > height)
                            {
                                acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees000);
                                acPlSetVdr.SetPlotConfigurationName(acPlSet, "DWG To PDF.pc3", "ISO_full_bleed_A4_(297.00_x_210.00_MM)");
                            }
                            else
                            {
                                acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees180);
                                acPlSetVdr.SetPlotConfigurationName(acPlSet, "DWG To PDF.pc3", "ISO_full_bleed_A4_(210.00_x_297.00_MM)");
                            }
                            acPlSetVdr.SetPlotType(acPlSet, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                            acPlSetVdr.SetPlotWindowArea(acPlSet, new Extents2d(plotExt[i].MinPoint.X, plotExt[i].MinPoint.Y, plotExt[i].MaxPoint.X, plotExt[i].MaxPoint.Y));
                            acPlInfo.OverrideSettings = acPlSet;
                            using (PlotInfoValidator acPlInfoVdr = new PlotInfoValidator())
                            {
                                acPlInfoVdr.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                                acPlInfoVdr.Validate(acPlInfo);
                                Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("BACKGROUNDPLOT", 0);
                                if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                                {
                                    using (PlotEngine acPlEng = PlotFactory.CreatePublishEngine())
                                    {
                                        using (PlotProgressDialog acPlProgDlg = new PlotProgressDialog(false, 1, true))
                                        {
                                            using ((acPlProgDlg))
                                            {
                                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Plot Progress");
                                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
                                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
                                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");
                                                acPlProgDlg.LowerPlotProgressRange = 0;
                                                acPlProgDlg.UpperPlotProgressRange = 100;
                                                acPlProgDlg.PlotProgressPos = 0;
                                                acPlProgDlg.OnBeginPlot();
                                                acPlProgDlg.IsVisible = false;
                                                acPlEng.BeginPlot(acPlProgDlg, null);
                                                string pdfName = GetBlockAttributes(finalDoc, acTrans, plotExt[i]);
                                                string pdfFilePath = Path.Combine(rootFolder, pdfName + ".pdf");
                                                if (File.Exists(pdfFilePath))
                                                {
                                                    string fileName = Path.GetFileName(pdfFilePath); // Extract the file name
                                                    var response = MessageBox.Show($"The file \"{fileName}\" already exists. Would you like to replace it?",
                                                                                   "Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                                                    if (response == DialogResult.OK)
                                                    {
                                                        // Replace the existing file
                                                        File.Delete(pdfFilePath); // Ensure the old file is deleted before creating a new one
                                                    }
                                                    else
                                                    {
                                                        // Generate a unique file name with a numbered suffix (1), (2), etc.
                                                        int counter = 1;
                                                        string newFilePath;
                                                        do
                                                        {
                                                            newFilePath = Path.Combine(rootFolder, $"{pdfName}({counter}).pdf");
                                                            counter++;
                                                        }
                                                        while (File.Exists(newFilePath));
                                                        pdfFilePath = newFilePath; // Use the new unique file name
                                                    }
                                                }
                                                // Now, use pdfFilePath to generate and save the new PDF
                                                //string pdfFilePath = Path.Combine(rootFolder, pdfName + ".pdf");
                                                //if (File.Exists(pdfFilePath))
                                                //{
                                                //    var response = MessageBox.Show("Would you like to replace the file", "Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                                                //    if (response == DialogResult.OK)
                                                //    {
                                                //    }
                                                //}
                                                acPlEng.BeginDocument(acPlInfo, finalDoc.Name, null, 1, true, pdfFilePath);
                                                acPlProgDlg.set_PlotMsgString(PlotMessageIndex.Status, "Plotting: " + finalDoc.Name + " - " + acLayout.LayoutName);
                                                acPlProgDlg.OnBeginSheet();
                                                acPlProgDlg.LowerSheetProgressRange = 0;
                                                acPlProgDlg.UpperSheetProgressRange = 100;
                                                acPlProgDlg.SheetProgressPos = 0;
                                                using (PlotPageInfo acPlPageInfo = new PlotPageInfo())
                                                {
                                                    acPlEng.BeginPage(acPlPageInfo, acPlInfo, true, null);
                                                }
                                                acPlEng.BeginGenerateGraphics(null);
                                                acPlEng.EndGenerateGraphics(null);
                                                acPlEng.EndPage(null);
                                                acPlProgDlg.SheetProgressPos = 100;
                                                acPlProgDlg.OnEndSheet();
                                                acPlEng.EndDocument(null);
                                                acPlProgDlg.PlotProgressPos = 100;
                                                acPlProgDlg.OnEndPlot();
                                                acPlEng.EndPlot(null);
                                            }
                                        }
                                        acPlEng.Destroy();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public List<Extents2d> TitleBlockExtents(Document finalDoc)
        {
            List<Extents2d> titleExt = new List<Extents2d>();
            List<ObjectId> entID = ExtractAttributes(finalDoc, finalDoc.Database);
            using (Transaction tx = finalDoc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId entId in entID)
                {
                    Entity ent = tx.GetObject(entId, OpenMode.ForRead) as Entity;
                    if (ent != null)
                    {
                        BlockReference br = ent as BlockReference;
                        if (br != null)
                        {
                            titleExt.Add(new Extents2d(br.GeometricExtents.MinPoint.X, br.GeometricExtents.MinPoint.Y, br.GeometricExtents.MaxPoint.X, br.GeometricExtents.MaxPoint.Y));
                        }
                    }
                }
            }
            return titleExt;
        }
        public List<ObjectId> ExtractAttributes(Document doc, Database db)
        {
            List<ObjectId> sortedBlockIds = new List<ObjectId>();
            Editor editor = doc.Editor;
            TypedValue[] flt =
            {
         new TypedValue((int)DxfCode.Start, "INSERT"),
         new TypedValue((int)DxfCode.BlockName, "T-1027-A TITLE BLOCK,T-1010-A TITLE BLOCK,FORMING TITLE BLOCK,T-1027-A TITLE BLOCK NO TEXT,T-1010-A TITLE BLOCK NO TEXT,T-1027-A TITLE BLOCK W360,T-1010-A TITLE BLOCK W360,T-1027-A TITLE BLOCK FP,T-1027-A TITLE BLOCK SD,T-1027-A TITLE BLOCK SL,T-1027-A TITLE BLOCK BPI,T-1027-A TITLE BLOCK FPI")
     };
            PromptSelectionResult rs = doc.Editor.SelectAll(new SelectionFilter(flt));
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                Dictionary<ObjectId, string> blockSheetNumbers = new Dictionary<ObjectId, string>();
                foreach (SelectedObject selectedObject in rs.Value)
                {
                    if (selectedObject.ObjectId.ObjectClass.DxfName == "INSERT")
                    {
                        BlockReference blockRef = trans.GetObject(selectedObject.ObjectId, OpenMode.ForRead) as BlockReference;
                        if (blockRef != null)
                        {
                            foreach (ObjectId attributeId in blockRef.AttributeCollection)
                            {
                                DBObject attributeObj = trans.GetObject(attributeId, OpenMode.ForRead);
                                AttributeReference attribute = attributeObj as AttributeReference;
                                if (attribute != null)
                                {
                                    if (attribute.Tag == "ELEMENT_CODE")
                                    {
                                        string attributeValue = attribute.TextString;
                                        blockSheetNumbers.Add(selectedObject.ObjectId, attributeValue);
                                    }
                                }
                            }
                        }
                    }
                }
                var sortedSheetNumbers = blockSheetNumbers.GroupBy(pair => pair.Value).OrderBy(group => group.Key).Select(group => group.Last()).ToDictionary(pair => pair.Key, pair => pair.Value);
                foreach (var pair in sortedSheetNumbers)
                {
                    sortedBlockIds.Add(pair.Key);
                }
            }
            return sortedBlockIds;
        }
        public string GetBlockAttributes(Document doc, Transaction acTrans, Extents2d extents)
        {
            BlockTableRecord btr = (BlockTableRecord)acTrans.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForRead);
            foreach (ObjectId objId in btr)
            {
                if (acTrans.GetObject(objId, OpenMode.ForRead) is BlockReference blockRef)
                {
                    // Check if the block reference lies within the given extents
                    if (blockRef.Position.X >= extents.MinPoint.X && blockRef.Position.X <= extents.MaxPoint.X &&
                        blockRef.Position.Y >= extents.MinPoint.Y && blockRef.Position.Y <= extents.MaxPoint.Y)
                    {
                        string partList = string.Empty, elementCode = string.Empty;
                        foreach (ObjectId attId in blockRef.AttributeCollection)
                        {
                            AttributeReference attRef = (AttributeReference)acTrans.GetObject(attId, OpenMode.ForRead);
                            if (attRef.Tag == "PART_LIST")
                                partList = attRef.TextString;
                            if (attRef.Tag == "ELEMENT_CODE")
                                elementCode = attRef.TextString;
                        }
                        if (!string.IsNullOrEmpty(partList) && !string.IsNullOrEmpty(elementCode))
                        {
                            return $"{partList}{elementCode}".Replace('/', '-'); // Sanitize PDF name
                        }
                    }
                }
            }
            return "DefaultName";
        }

    }
}
