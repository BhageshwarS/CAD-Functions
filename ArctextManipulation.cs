using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MYCOLLECTION
{
    class ArctextManipulation
    {
        public void ExplodeAllArcAlignedText(Document doc)
        {
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<Entity> explodedEntities = new List<Entity>();
            List<ObjectId> arcTextIds = new List<ObjectId>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                TypedValue[] filterList = new TypedValue[]
                {
                new TypedValue((int)DxfCode.Start, "ARCALIGNEDTEXT")
                };
                SelectionFilter filter = new SelectionFilter(filterList);
                PromptSelectionResult selRes = ed.SelectAll(filter);

                if (selRes.Status != PromptStatus.OK)
                    return;

                arcTextIds.AddRange(selRes.Value.GetObjectIds());
                ed.SetImpliedSelection(arcTextIds.ToArray());
                tr.Commit();
            }

            doc.SendStringToExecute("_.EXPLODE\n", true, false, false);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in ms)
                {
                    if (!id.IsValid || id.IsErased) continue;

                    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    explodedEntities.Add(ent);
                }
                tr.Commit();


            }

            ed.WriteMessage($"\nExploded entities: {explodedEntities.Count}");
        }


        public (double startAngle, double endAngle, string textString)? ProcessArcTextLetters(Document doc, Point3d arcCenter, double radius, bool isCCW = true)
        {
            Editor ed = doc.Editor;
            Database db = doc.Database;

            List<(DBText Text, double Angle)> letters = new List<(DBText, double)>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                // You can iterate through all BlockTableRecords (ModelSpace, PaperSpace, etc.)
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                    foreach (ObjectId objId in btr)
                    {
                        if (objId.ObjectClass == RXObject.GetClass(typeof(DBText)))
                        {
                            DBText textEnt = (DBText)tr.GetObject(objId, OpenMode.ForRead);

                            if (textEnt.TextString.Length == 1)
                            {
                                double angle = Math.Atan2(textEnt.Position.Y - arcCenter.Y, textEnt.Position.X - arcCenter.X);
                                if (angle < 0) angle += 2 * Math.PI;

                                letters.Add((textEnt, angle));
                            }
                        }
                    }
                }

                tr.Commit();
            }

            if (letters.Count == 0)
            {
                ed.WriteMessage("\nNo single-character DBText entities found.");
                return null;
            }

            // Sort the letters based on their angle
            letters.Sort((a, b) => isCCW ? a.Angle.CompareTo(b.Angle) : b.Angle.CompareTo(a.Angle));

            // Estimate average character width
            double avgCharWidth = 0;
            foreach (var (text, _) in letters)
            {
                avgCharWidth += text.WidthFactor * text.Height;
            }
            avgCharWidth /= letters.Count;

            double spacingThreshold = avgCharWidth * 1.5;

            // Build the text string based on spacing between characters
            string resultText = letters[0].Text.TextString;
            for (int i = 1; i < letters.Count; i++)
            {
                double diff = letters[i].Angle - letters[i - 1].Angle;
                if (diff < 0) diff += 2 * Math.PI;
                double arcDistance = diff * radius;

                if (arcDistance > spacingThreshold)
                    resultText += " ";

                resultText += letters[i].Text.TextString;
            }

            return (letters[0].Angle, letters[letters.Count - 1].Angle, resultText);
        }
        public void ArcText(Database db, double startAngleDeg, double endAngleDeg, Point3d center, double rad, string text, double textHeight)
        {
            if (text.Length > 9)
            {
                startAngleDeg = startAngleDeg - ((text.Length - 9) * 2);
                endAngleDeg = endAngleDeg + ((text.Length - 9) * 2);

            }
            double startAngleRad = startAngleDeg * (Math.PI / 180.0);
            double endAngleRad = endAngleDeg * (Math.PI / 180.0);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                double totalAngle = (endAngleRad > startAngleRad) ? endAngleRad - startAngleRad : (2 * Math.PI - startAngleRad) + endAngleRad;

                double angleStep = totalAngle / text.Length;

                for (int i = 0; i < text.Length; i++)
                {
                    int charIndex = text.Length - 1 - i;
                    double angle = startAngleRad + (i + 0.5) * angleStep;
                    double x = center.X + rad * Math.Cos(angle);
                    double y = center.Y + rad * Math.Sin(angle);
                    Point3d pos = new Point3d(x, y, 0);

                    DBText dbText = new DBText
                    {
                        TextString = text[charIndex].ToString(),
                        Height = textHeight,
                        Position = pos,
                        Rotation = angle - Math.PI / 2,
                        HorizontalMode = TextHorizontalMode.TextCenter,
                        VerticalMode = TextVerticalMode.TextBase,
                        AlignmentPoint = pos,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, 3)
                    };

                    dbText.AdjustAlignment(db);
                    btr.AppendEntity(dbText);
                    tr.AddNewlyCreatedDBObject(dbText, true);
                }

                tr.Commit();
            }
        }
    }
}
