using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MYCOLLECTION
{
    public class SelectionFilters
    {
        public List<ObjectId> SelectLines(Document doc)
        {
            List<ObjectId> lineIds = new List<ObjectId>();
            Database db = doc.Database;
            Editor edt = doc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                edt.WriteMessage("\nSelecting all the Line objects...");
                TypedValue[] tv = new TypedValue[1];
                tv[0] = new TypedValue((int)DxfCode.Start, "LINE");
                SelectionFilter filter = new SelectionFilter(tv);
                PromptSelectionResult psr = edt.SelectAll(filter);

                if (psr.Status == PromptStatus.OK)
                {
                    SelectionSet ss = psr.Value;

                    foreach (SelectedObject sObj in ss)
                    {
                        if (sObj != null)
                        {
                            Entity ent = trans.GetObject(sObj.ObjectId, OpenMode.ForRead) as Entity;
                            if (ent != null)
                            {
                                lineIds.Add(ent.ObjectId);

                            }
                        }
                    }

                    edt.WriteMessage($"\nThere are a total of {ss.Count} lines selected.");
                }
                trans.Commit();
            }
            return lineIds;
        }

        public List<ObjectId> SelectMTexts(Document doc)
        {
            Editor edt = doc.Editor;
            List<ObjectId> mtextIds = new List<ObjectId>();
            TypedValue[] tv = new TypedValue[1];
            tv[0] = new TypedValue((int)DxfCode.Start, "MTEXT");

            SelectionFilter filter = new SelectionFilter(tv);
            PromptSelectionResult psr = edt.SelectAll(filter);

            if (psr.Status == PromptStatus.OK)
            {
                SelectionSet ss = psr.Value;
                foreach (SelectedObject sObj in ss)
                {
                    if (sObj != null)
                    {
                        mtextIds.Add(sObj.ObjectId);
                    }
                }

                edt.WriteMessage($"\nThere are a total of {ss.Count} MText objects selected.");
            }
            else
            {
                edt.WriteMessage("\nNo MText objects selected.");
            }

            return mtextIds;
        }

        public List<ObjectId> SelectPlines(Document doc)
        {
            List<ObjectId> plineIds = new List<ObjectId>();
            Editor edt = doc.Editor;
            TypedValue[] tv = new TypedValue[1];
            tv[0] = new TypedValue((int)DxfCode.Start, "LWPOLYLINE");

            SelectionFilter filter = new SelectionFilter(tv);
            PromptSelectionResult psr = edt.SelectAll(filter);

            if (psr.Status == PromptStatus.OK)
            {
                SelectionSet ss = psr.Value;

                foreach (SelectedObject sObj in ss)
                {
                    if (sObj != null)
                    {
                        plineIds.Add(sObj.ObjectId);
                    }
                }

                edt.WriteMessage($"\nThere are a total of {ss.Count} LWPolylines selected.");
            }
            else
            {
                edt.WriteMessage("\nThere is no LWPolyline selected.");
            }

            return plineIds;
        }


        public List<ObjectId> SelectBlock(Document doc, string blockname)
        {
            Editor edt = doc.Editor;
            List<ObjectId> blockIds = new List<ObjectId>();

            edt.WriteMessage("\nSelecting all 'Door - French' blocks in the drawing...");

            // Create the filter
            TypedValue[] tv = new TypedValue[2];
            tv[0] = new TypedValue((int)DxfCode.Start, "INSERT");
            tv[1] = new TypedValue((int)DxfCode.BlockName, blockname);

            SelectionFilter filter = new SelectionFilter(tv);
            PromptSelectionResult ssPrompt = edt.SelectAll(filter);

            if (ssPrompt.Status == PromptStatus.OK)
            {
                SelectionSet ss = ssPrompt.Value;

                foreach (SelectedObject sObj in ss)
                {
                    if (sObj != null)
                        blockIds.Add(sObj.ObjectId);
                }

                edt.WriteMessage($"\nThe number of {blockname} blocks selected: {ss.Count}");
            }
            else
            {
                edt.WriteMessage("\nNo 'Door - French' blocks found.");
            }

            return blockIds;
        }
    }
}
