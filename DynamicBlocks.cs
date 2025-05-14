using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;

namespace MYCOLLECTION
{
    public static class DynamicBlocks
    {
        public static ObjectId ImportBlock(Database destdb, string Filepath, string Blockname)
        {
            try
            {
                if (System.IO.File.Exists(Filepath))
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
        public static  void InsertBlock(Database destdb, Point3d inspt, string blockname, string Filepath)
        {
            ObjectId blockid = ImportBlock(destdb, Filepath, blockname);

            using (Transaction tr = destdb.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(destdb.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                if (blockid == ObjectId.Null)
                {
                    if (bt.Has(blockname))
                    {
                        blockid = bt[blockname];
                    }
                }
                using (BlockReference br = new BlockReference(inspt, blockid))
                {
                    BlockTableRecord btr = tr.GetObject(br.BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    // br.ScaleFactors = new Scale3d(10);
                    ms.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);
                    UpdateAttributes(br, btr, tr);
                    EditProperties(br);
                }

                tr.Commit();
            }
        }
        public static Point3d Inspt()
        {
            PromptPointOptions ppo = new PromptPointOptions("Select a insertion point");
            PromptPointResult psr = Application.DocumentManager.MdiActiveDocument.Editor.GetPoint(ppo);
            if (psr.Status == PromptStatus.OK)
            {
                return psr.Value;
            }
            return Point3d.Origin;
        }
        public static void UpdateAttributes(BlockReference br, BlockTableRecord btr, Transaction tr)
        {
            try
            {

                if (br != null && btr.HasAttributeDefinitions)
                {
                    foreach (ObjectId id in btr)
                    {
                        DBObject obj = id.GetObject(OpenMode.ForRead);
                        AttributeDefinition attDef = obj as AttributeDefinition;
                        if (attDef != null && !attDef.Constant)
                        {

                            AttributeReference attRef = new AttributeReference();
                            attRef.SetAttributeFromBlock(attDef, br.BlockTransform);
                            if (attRef.Tag == "RADIUS")
                            {
                                attRef.TextString = "0";
                                br.AttributeCollection.AppendAttribute(attRef);
                                tr.AddNewlyCreatedDBObject(attRef, true);
                            }

                        }
                    }
                }


            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Application.ShowAlertDialog("Exception :" + ex.Message + ex.StackTrace);
            }
        }
        public static void EditProperties(BlockReference br)
        {
            foreach (DynamicBlockReferenceProperty dbrp in br.DynamicBlockReferencePropertyCollection)
            {
                if (dbrp.PropertyName == "Angle1")
                {
                    dbrp.Value = Math.PI / 4;

                }
                else if (dbrp.PropertyName == "Flip")
                {
                    dbrp.Value = 1;
                    Application.ShowAlertDialog(" " + dbrp.Value.GetType());
                }
                else if (dbrp.PropertyName == "Distance1")
                {
                    dbrp.Value = 100.0;
                }
            }

        }
    }
}

