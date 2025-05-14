using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using Exception = Autodesk.AutoCAD.Runtime.Exception;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
namespace MYCOLLECTION
{
    public class HelperMethod
    {

        public Line DrawLine(Database d1, Point3d startpoint, Point3d endpoint)
        {
            Line line;
            try
            {
                using (Transaction TS = d1.TransactionManager.StartTransaction())
                {
                    BlockTable btt = TS.GetObject(d1.BlockTableId, OpenMode.ForWrite) as BlockTable;
                    BlockTableRecord btrr = TS.GetObject(btt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                    line = new Line(startpoint, endpoint);
                    btrr.AppendEntity(line);
                    TS.AddNewlyCreatedDBObject(line, true);
                    TS.Commit();

                }
                return line;
            }
            catch (System.Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("Exception :" + ex.Message + ex.StackTrace);
                return null;
            }
        }

    
        public void DrawLine(Document doc, string lineName, Point3d startPoint, Point3d endPoint)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (string.IsNullOrWhiteSpace(lineName)) throw new ArgumentException("Line name is required.", nameof(lineName));

            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;


                    Line line = new Line(startPoint, endPoint);
                    btr.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);



                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    Application.ShowAlertDialog($"Error creating line \"{lineName}\": {ex.Message}");
                }
            }
        }

        public void DrawPolyline(Document doc, Point2dCollection vertices, bool isClosed = false)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (vertices == null || vertices.Count < 2)
                throw new ArgumentException("At least 2 vertices required");

            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                    Polyline pline = new Polyline();
                    // Add vertices
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        pline.AddVertexAt(i, vertices[i], 0, 0, 0);
                    }
                    pline.Closed = isClosed;
                    btr.AppendEntity(pline);
                    tr.AddNewlyCreatedDBObject(pline, true);





                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    tr.Abort();
                    Application.ShowAlertDialog($"Error creating polyline: {ex.Message}");
                }
            }
        }
        public static void DrawCircle(Document doc, Point3d center, double radius)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (radius <= 0) throw new ArgumentException("Radius must be positive");

            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite) as BlockTableRecord;

                    Circle circle = new Circle(center, Vector3d.ZAxis, radius);

                    btr.AppendEntity(circle);
                    tr.AddNewlyCreatedDBObject(circle, true);
                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    tr.Abort();
                    Application.ShowAlertDialog($"Error creating circle: {ex.Message}");
                }
            }
        }
        public static void AddMText(Document doc, string contents, Point3d location)
        {
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                MText mt = new MText();
                mt.Contents = contents;
                mt.Location = location;
                mt.TextStyleId = db.Textstyle;

                ObjectId mtId = ms.AppendEntity(mt);
                tr.AddNewlyCreatedDBObject(mt, true);

                tr.Commit();
            }
        }
        public static void DrawArc(Document doc, Point3d center, double radius, double startAngle, double endAngle)
        {
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Arc arc = new Arc(center, radius, startAngle * Math.PI / 180, endAngle * Math.PI / 180);

                ObjectId arcId = ms.AppendEntity(arc);
                tr.AddNewlyCreatedDBObject(arc, true);

                tr.Commit();
            }
        }
        public ObjectId DrawLine(Document doc, Point3d startPoint, Point3d endPoint)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite) as BlockTableRecord;
                    Line line = new Line(startPoint, endPoint);
                    btr.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);
                    tr.Commit();
                    return line.ObjectId;
                }
                catch (System.Exception ex)
                {
                    tr.Abort();
                    Application.ShowAlertDialog($"Error creating line: {ex.Message}");

                }
                return ObjectId.Null;


            }
        }

       
    }
}








//Hatch hatch1 = new Hatch();
//hatch1.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
//hatch1.PatternSpace = 50;
//hatch1.PatternAngle = Math.PI;
//hatch1.AppendLoop(HatchLoopTypes.SelfIntersecting, new ObjectIdCollection { polyline1.ObjectId });
//btr.AppendEntity(hatch1);
//tr.AddNewlyCreatedDBObject(hatch1, true);
//change in polyline name