using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.IO;
namespace MYCOLLECTION 
{
    public class InsertMultipleblock
    {
        
        public void MainMethod()
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                string Filepath = @"C:\Users\GuestUser\Documents\crcl1.dwg";
                string[] blocknames = new string[] { "crcl", "blk1", "ONOFF_BLOCK" };
                int i = 0;
                foreach (string blockname in blocknames)
                {
                    Point3d inspt = Inspt(i);
                    InsertBlock(db, inspt, blockname, Filepath);
                    i++;
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Application.ShowAlertDialog("Exception :" + ex.Message + ex.StackTrace);
            }


        }
        public ObjectId ImportBlock(Database destdb, string Filepath, string Blockname)
        {
            try
            {
                if (File.Exists(Filepath))
                {
                    ObjectId objid = ObjectId.Null;
                    using (Database sourcedb = new Database(false, true))
                    {
                        sourcedb.ReadDwgFile(Filepath, FileOpenMode.OpenForReadAndReadShare, true, null);
                        using (Transaction sourcetr = new OpenCloseTransaction())
                        {
                            BlockTable sourcebt = sourcetr.GetObject(sourcedb.BlockTableId, OpenMode.ForRead) as BlockTable;
                            if (sourcebt.Has(Blockname))
                            {
                                objid = sourcebt[Blockname];
                            }
                            ObjectIdCollection objids = new ObjectIdCollection();
                            objids.Add(objid);
                            IdMapping map = new IdMapping();
                            sourcedb.WblockCloneObjects(objids, destdb.BlockTableId, map, DuplicateRecordCloning.Replace, false);
                            if (map[objid].IsCloned)
                            {
                                return map[objid].Value;
                            }

                        }
                    }
                }

            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Application.ShowAlertDialog("Exception :" + ex.Message + ex.StackTrace);
            }
            return ObjectId.Null;

        }
        public void InsertBlock(Database destdb, Point3d inspt, string blockname, string Filepath)
        {
            ObjectId blockid = ImportBlock(destdb, Filepath, blockname);

            using (Transaction tr = destdb.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(destdb.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                if (blockid == ObjectId.Null)
                {
                    if (bt.Has(blockname))
                    {
                        blockid = bt[blockname];
                    }
                }
                using (BlockReference br = new BlockReference(inspt, blockid))
                {
                    // br.ScaleFactors = new Scale3d(10);
                    btr.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);
                }
                tr.Commit();
            }
        }
        public Point3d Inspt(int i)
        {
            PromptPointOptions ppo = new PromptPointOptions("Select a insertion point" + i);
            PromptPointResult psr = Application.DocumentManager.MdiActiveDocument.Editor.GetPoint(ppo);
            if (psr.Status == PromptStatus.OK)
            {
                return psr.Value;
            }
            return Point3d.Origin;
        }
    }
}

