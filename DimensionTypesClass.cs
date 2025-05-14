using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Exception = Autodesk.AutoCAD.Runtime.Exception;
using System;
using System.Diagnostics;



namespace MYCOLLECTION 
{
    public static class DimensionTypesClass
    {
        public static void DimStyleMethod(Database  db, string name = "MyDimStyle", double textHeight = 2.5, double arrowSize = 2.0, double extensionLineOffset = 0.5, double extensionLineExtension = 0.5, int decimalPlaces = 2, double gap = 0.5, bool zeroSuppression = true, short dimensionLineColor = 1)
        {
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DimStyleTable dimStyleTable = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForWrite);

                    DimStyleTableRecord dimStyleRecord = new DimStyleTableRecord
                    {
                        Name = name,
                        Dimtxt = textHeight,
                        Dimasz = arrowSize,
                        Dimgap = gap,
                        Dimexo = extensionLineOffset,
                        Dimdle = extensionLineExtension,
                        Dimdec = decimalPlaces,
                        Dimzin = zeroSuppression ? 8 : 0,
                        Dimclrd = Color.FromColorIndex(ColorMethod.ByAci, dimensionLineColor)
                    };

                    dimStyleTable.Add(dimStyleRecord);
                    tr.AddNewlyCreatedDBObject(dimStyleRecord, true);

                    tr.Commit();
                }
            }
            catch (System.Exception e)
            {
                Application.ShowAlertDialog(e.Message + e.StackTrace);
            }
            
        }
        public static void AlignedDimesionMethod(Database db, Point3d startpt, Point3d endpt, double offset, bool Flipoffset)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                    BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                    Point3d offsetpt = Flipoffset ? new Point3d(startpt.X - offset, startpt.Y - offset, 0) : new Point3d(startpt.X + offset, startpt.Y + offset, 0);
                    AlignedDimension AD = new AlignedDimension(startpt, endpt, offsetpt, "", db.Dimstyle);
                    btr.AppendEntity(AD);
                    trans.AddNewlyCreatedDBObject(AD, true);

                }
                catch (System.Exception e)
                {
                    Application.ShowAlertDialog(e.Message + e.StackTrace);
                }
                trans.Commit();
            }
        }
        public static void AngularDimensionMethod(Database  db, Point3d cenpt, Point3d startpt, Point3d endpt, Point3d Arcpoint)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                    BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                    Point3AngularDimension pt = new Point3AngularDimension(cenpt, startpt, endpt, Arcpoint, "", db.Dimstyle);

                    btr.AppendEntity(pt);
                    trans.AddNewlyCreatedDBObject(pt, true);

                }
                catch (Exception e)
                {
                    Application.ShowAlertDialog(e.Message + e.StackTrace);
                }
                trans.Commit();
            }
        }
        public static void RadialDimentionsMethod(Database  db, Point3d cenpt, Point3d Chordpt, double length)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                    BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    RadialDimension rd = new RadialDimension(cenpt, Chordpt, length, "", db.Dimstyle);

                    btr.AppendEntity(rd);
                    trans.AddNewlyCreatedDBObject(rd, true);


                }
                catch (Exception e)
                {
                    Application.ShowAlertDialog(e.Message + e.StackTrace);
                }
                trans.Commit();
            }
        }
        public static void DiametricDimentionsMethod(Database db, Point3d Chordpt1, Point3d Chordpt2, double length)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                    BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    DiametricDimension dd = new DiametricDimension(Chordpt1, Chordpt2, length, "", db.Dimstyle);

                    btr.AppendEntity(dd);
                    trans.AddNewlyCreatedDBObject(dd, true);


                }
                catch (Exception e)
                {
                    Application.ShowAlertDialog(e.Message + e.StackTrace);
                }
                trans.Commit();
            }
        }
    }
}
