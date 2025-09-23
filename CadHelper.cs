using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MYCOLLECTION
{
    class CadHelper
    {
        /// <summary> Draws a line in the specified database using the provided line information.
        public void DrawLine(Database db, LineInfo lineInfo)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (lineInfo == null) throw new ArgumentNullException(nameof(lineInfo));

            Line line = new Line(lineInfo.StartPoint, lineInfo.EndPoint);
            ApplyEntityProperties(db, line, lineInfo);

            AppendEntity(db, line);
        }
        /// <summary> Draws a circle in the specified database using the provided circle information. 
        public void DrawCircle(Database db, CircleInfo circleInfo)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (circleInfo == null) throw new ArgumentNullException(nameof(circleInfo));

            Circle circle = new Circle
            {
                Center = circleInfo.Center,
                Radius = circleInfo.Radius,
                Normal = circleInfo.Normal
            };

            ApplyEntityProperties(db, circle, circleInfo);

            AppendEntity(db, circle);
        }
        /// <summary> Draws an LWPolyline in the specified database using the provided polyline information.
        public void DrawPolyline(Database db, PolylineInfo polylineInfo)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (polylineInfo == null) throw new ArgumentNullException(nameof(polylineInfo));

            // Create a new 2D polyline
            Polyline polyline = new Polyline();

            for (int i = 0; i < polylineInfo.Vertices.Count; i++)
            {
                var v = polylineInfo.Vertices[i];
                polyline.AddVertexAt(i, v.Point, v.Bulge, v.StartWidth, v.EndWidth);
            }

            polyline.Closed = polylineInfo.Closed;
            polyline.Elevation = polylineInfo.Elevation;
            polyline.Normal = polylineInfo.Normal;

            ApplyEntityProperties(db, polyline, polylineInfo);

            AppendEntity(db, polyline);
        }
        /// <summary> Draws an arc in the specified database using the provided arc information.
        public void DrawArc(Database db, ArcInfo arcInfo)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (arcInfo == null) throw new ArgumentNullException(nameof(arcInfo));

            Arc arc = new Arc
            {
                Center = arcInfo.Center,
                Radius = arcInfo.Radius,
                StartAngle = arcInfo.StartAngle,
                EndAngle = arcInfo.EndAngle,
                Normal = arcInfo.Normal
            };

            ApplyEntityProperties(db, arc, arcInfo);

            AppendEntity(db, arc);
        }
        /// <summary> Draws an ellipse in the specified database using the provided ellipse information.
        public void DrawEllipse(Database db, EllipseInfo ellipseInfo)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (ellipseInfo == null) throw new ArgumentNullException(nameof(ellipseInfo));

            Ellipse ellipse = new Ellipse(
                ellipseInfo.Center,
                ellipseInfo.Normal,
                ellipseInfo.MajorAxis,
                ellipseInfo.RadiusRatio,
                ellipseInfo.StartAngle,
                ellipseInfo.EndAngle);

            ApplyEntityProperties(db, ellipse, ellipseInfo);

            AppendEntity(db, ellipse);
        }
        /// <summary> Draws a 3D polyline in the specified database using the provided polyline3d information.
        public void DrawSpline(Database db, SplineInfo splineInfo)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (splineInfo == null) throw new ArgumentNullException(nameof(splineInfo));
            if (splineInfo.ControlPoints.Count < 2)
                throw new ArgumentException("At least two control points are required to create a spline.");

            // Convert control points to fit point collection
            Point3dCollection fitPoints = new Point3dCollection(splineInfo.ControlPoints.ToArray());

            // Use zero vector for tangents unless you add custom support
            Vector3d startTangent = new Vector3d(0, 0, 0);
            Vector3d endTangent = new Vector3d(0, 0, 0);

            // Create the spline using fit points and tangents
            Spline spline = new Spline(
                fitPoints,
                startTangent,
                endTangent,
                splineInfo.Degree,
                splineInfo.FitTolerance
            );

            // Apply a rotation if needed to match target normal
            Vector3d currentNormal = Vector3d.ZAxis;
            Vector3d targetNormal = splineInfo.Normal;

            if (!targetNormal.IsEqualTo(currentNormal, new Tolerance(1e-9, 1e-9)))
            {
                Vector3d axis = currentNormal.CrossProduct(targetNormal);
                double angle = currentNormal.GetAngleTo(targetNormal);

                if (!axis.IsZeroLength())
                {
                    Matrix3d rotation = Matrix3d.Rotation(angle, axis, Point3d.Origin);
                    spline.TransformBy(rotation);
                }
            }

            // Apply common properties
            ApplyEntityProperties(db, spline, splineInfo);

            // Add to database
            AppendEntity(db, spline);
        }
        /// <summary> Draws a 3D polyline in the specified database using the provided polyline3d information.
        public void DrawXline(Database db, XLineInfo xlineInfo)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (xlineInfo == null) throw new ArgumentNullException(nameof(xlineInfo));

            if (xlineInfo.Direction.IsZeroLength())
                throw new ArgumentException("Direction vector must not be zero.");

            Xline xline = new Xline
            {
                BasePoint = xlineInfo.BasePoint,
                UnitDir = xlineInfo.Direction.GetNormal()
            };

            ApplyEntityProperties(db, xline, xlineInfo);
            AppendEntity(db, xline);
        }
        /// <summary> Draws a ray in the specified database using the provided ray information.
        public void DrawRay(Database db, RayInfo rayInfo)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (rayInfo == null) throw new ArgumentNullException(nameof(rayInfo));

            if (rayInfo.Direction.IsZeroLength())
                throw new ArgumentException("Direction vector must not be zero.");

            Ray ray = new Ray
            {
                BasePoint = rayInfo.BasePoint,
                UnitDir = rayInfo.Direction.GetNormal()
            };

            ApplyEntityProperties(db, ray, rayInfo);
            AppendEntity(db, ray);
        }
        /// <summary> Draws a point in the specified database using the provided point information.
        public void DrawPoint(Database db, PointInfo pointInfo)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (pointInfo == null) throw new ArgumentNullException(nameof(pointInfo));

            DBPoint point = new DBPoint(pointInfo.Position);

            ApplyEntityProperties(db, point, pointInfo);
            AppendEntity(db, point);
        }
        /// <summary> Draws a text in the specified database using the provided text information.
        public void DrawText(Database db, DBTextInfo textInfo)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (textInfo == null) throw new ArgumentNullException(nameof(textInfo));

            DBText dbText = new DBText
            {
                Position = textInfo.Position,
                TextString = textInfo.TextString,
                Height = textInfo.Height,
                Rotation = textInfo.Rotation,
                WidthFactor = textInfo.WidthFactor,
                TextStyleId = textInfo.TextStyleId
            };

            ApplyEntityProperties(db, dbText, textInfo);
            AppendEntity(db, dbText);
        }
        /// <summary> Draws a MText in the specified database using the provided MText information.
        public void DrawMText(Database db, MTextInfo mTextInfo)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (mTextInfo == null) throw new ArgumentNullException(nameof(mTextInfo));

            MText mText = new MText
            {
                Location = mTextInfo.Location,
                Contents = mTextInfo.Contents,
                TextHeight = mTextInfo.TextHeight,
                Width = mTextInfo.Width,
                Rotation = mTextInfo.Rotation,
                TextStyleId = mTextInfo.TextStyleId
            };

            ApplyEntityProperties(db, mText, mTextInfo);
            AppendEntity(db, mText);
        }
        /// <summary> Draws a rotated dimension in the specified database using the provided rotated dimension information.
        public void DrawRotatedDimension(Database db,RotatedDimensionInfo info)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (info == null) throw new ArgumentNullException(nameof(info));

            var dim = new RotatedDimension(
                info.Rotation,
                info.XLine1Point,
                info.XLine2Point,
                info.DimLinePoint,
                info.DimensionText ?? "<>",
                ObjectId.Null
            );

            ApplyEntityProperties(db, dim, info);
            SetDimensionStyle(db, dim, info);
            AppendEntity(db, dim);
        }
        /// <summary> Draws an aligned dimension in the specified database using the provided aligned dimension information.
        public void DrawAlignedDimension(Database db, AlignedDimensionInfo info)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (info == null) throw new ArgumentNullException(nameof(info));

            var dim = new AlignedDimension(
                info.XLine1Point,
                info.XLine2Point,
                info.DimLinePoint,
                info.DimensionText ?? "<>",
                ObjectId.Null
            );

            ApplyEntityProperties(db, dim, info);
            SetDimensionStyle(db, dim, info);
            AppendEntity(db, dim);
        }
        /// <summary> Draws a radial dimension in the specified database using the provided radial dimension information.
        public void DrawRadialDimension(Database db, RadialDimensionInfo info)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (info == null) throw new ArgumentNullException(nameof(info));

            var dim = new RadialDimension(
                info.Center,
                info.ChordPoint,
                info.LeaderLength,
                info.DimensionText ?? "<>",
                ObjectId.Null
            );

            ApplyEntityProperties(db, dim, info);
            SetDimensionStyle(db, dim, info);
            AppendEntity(db, dim);
        }
        /// <summary> Draws a diametric dimension in the specified database using the provided diametric dimension information.
        public void DrawDiametricDimension(Database db, DiametricDimensionInfo info)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (info == null) throw new ArgumentNullException(nameof(info));

            var dim = new DiametricDimension(
                info.ChordPoint,
                info.FarChordPoint,
                info.LeaderLength,
                info.DimensionText ?? "<>",
                ObjectId.Null
            );

            ApplyEntityProperties(db, dim, info);
            SetDimensionStyle(db, dim, info);
            AppendEntity(db, dim);
        }
        /// <summary> Draws a line angular dimension in the specified database using the provided line angular dimension information.
        public void DrawLineAngularDimension(Database db, LineAngularDimensionInfo info)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (info == null) throw new ArgumentNullException(nameof(info));

            var dim = new LineAngularDimension2(
                info.Line1Start,
                info.Line1End,
                info.Line2Start,
                info.Line2End,
                info.DimensionArcPoint,
                info.DimensionText ?? "<>",
                ObjectId.Null
            );

            ApplyEntityProperties(db, dim, info);
            SetDimensionStyle(db, dim, info);
            AppendEntity(db, dim);
        }
        /// <summary> Draws an arc dimension in the specified database using the provided arc dimension information.
        public void DrawArcDimension(Database db, ArcDimensionInfo info)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (info == null) throw new ArgumentNullException(nameof(info));

            var dim = new ArcDimension(
                info.Center,
                info.StartPoint,
                info.EndPoint,
                info.DimensionArcPoint,
                info.DimensionText ?? "<>",
                ObjectId.Null
            );

            ApplyEntityProperties(db, dim, info);
            SetDimensionStyle(db, dim, info);
            AppendEntity(db, dim);
        }
        /// <summary> Draws a point3 angular dimension in the specified database using the provided point3 angular dimension information.
        public void DrawPoint3AngularDimension(Database db, Point3AngularDimensionInfo info)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (info == null) throw new ArgumentNullException(nameof(info));

            var dim = new Point3AngularDimension(
                info.Vertex,
                info.StartPoint,
                info.EndPoint,
                info.DimensionArcPoint,
                info.DimensionText ?? "<>",
                ObjectId.Null
            );

            ApplyEntityProperties(db, dim, info);
            SetDimensionStyle(db, dim, info);
            AppendEntity(db, dim);
        }
        /// <summary> Draws a leader in the specified database using the provided leader information.
        public void DrawLeader(Database db, LeaderInfo info)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (info.Vertices == null || info.Vertices.Count < 2)
                throw new ArgumentException("Leader requires at least two vertices.", nameof(info));

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Leader leader = new Leader();

                // Append points
                foreach (var pt in info.Vertices)
                {
                    leader.AppendVertex(pt);
                }

                // Arrow head
                leader.HasArrowHead = info.HasArrowHead;

                // Annotation (if provided)
                if (info.Annotation != null)
                {
                    leader.Annotation = info.Annotation;
                }

                ApplyEntityProperties(db, leader, info);  // using the version that checks linetype, layer etc as needed

                btr.AppendEntity(leader);
                tr.AddNewlyCreatedDBObject(leader, true);
                tr.Commit();
            }
        }
        /// <summary> Draws a multi-leader in the specified database using the provided MLeader information.
        public void DrawMLeader(Database db, MLeaderInfo info)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (info.LeaderLines == null || info.LeaderLines.Count < 1)
                throw new ArgumentException("MLeader must have at least one leader line point", nameof(info));

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                MLeader mld = new MLeader();


                mld.ContentType = ContentType.MTextContent;

                // Set optional arrow symbol
                if (!info.ArrowSymbolId.IsNull)
                {
                    mld.ArrowSymbolId = info.ArrowSymbolId;
                }

                mld.LeaderLineType = info.LeaderLineType;

                // Create leader line
                int leaderIndex = mld.AddLeader();
                int lineIndex = mld.AddLeaderLine(leaderIndex);

                for (int i = 0; i < info.LeaderLines.Count; i++)
                {
                    if (i == 0)
                        mld.AddFirstVertex(lineIndex, info.LeaderLines[i]);
                    else
                        mld.AddLastVertex(lineIndex, info.LeaderLines[i]);
                }

                // Add text
                MText mtext = new MText
                {
                    Location = info.AttachmentPoint,
                    Contents = info.Text,
                    TextHeight = info.TextHeight
                };
                mld.MText = mtext;

                ApplyEntityProperties(db, mld, info);

                btr.AppendEntity(mld);
                tr.AddNewlyCreatedDBObject(mld, true);

                tr.Commit();
            }
        }
        /// <summary> Draws a table in the specified database using the provided table information.
        public void DrawTable(Database db, TableInfo info)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (info == null) throw new ArgumentNullException(nameof(info));

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Table table = new Table();
                table.SetSize(info.Rows, info.Columns);
                table.SetRowHeight(info.RowHeight);
                table.SetColumnWidth(info.ColumnWidth);
                table.Position = info.InsertPoint;

                // Fill cells if contents provided
                if (info.CellContents != null)
                {
                    int rows = Math.Min(info.Rows, info.CellContents.GetLength(0));
                    int cols = Math.Min(info.Columns, info.CellContents.GetLength(1));
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            table.Cells[r, c].TextString = info.CellContents[r, c];
                        }
                    }
                }

                ApplyEntityProperties(db, table, info);

                btr.AppendEntity(table);
                tr.AddNewlyCreatedDBObject(table, true);
                tr.Commit();
            }
        }
        /// <summary> Draws a hatch in the specified database using the provided hatch information.
        public void DrawHatch(Database db, HatchInfo info)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (info.BoundaryEntities == null || info.BoundaryEntities.Count == 0)
                throw new ArgumentException("Hatch must have at least one boundary entity", nameof(info));

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Hatch hatch = new Hatch();
                hatch.SetHatchPattern(info.PatternType, info.PatternName);
                hatch.PatternAngle = info.PatternAngle;
                hatch.PatternScale = info.PatternScale;

                // Append boundary paths from provided entities
                foreach (Entity ent in info.BoundaryEntities)
                {
                    // Each boundary path must be a closed curve or loop
                    hatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection { ent.ObjectId });
                }

                hatch.EvaluateHatch(true);  // Calculates hatch fill

                ApplyEntityProperties(db, hatch, info);

                btr.AppendEntity(hatch);
                tr.AddNewlyCreatedDBObject(hatch, true);
                tr.Commit();
            }
        }
        /// <summary> Draws a wipeout in the specified database using the provided wipeout information.
        public void DrawWipeout(Database db, WipeoutInfo info)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (info.Vertices == null || info.Vertices.Count < 3)
                throw new ArgumentException("Wipeout requires at least 3 vertices to form a closed boundary", nameof(info));

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Prepare point collection
                Point2dCollection pts = new Point2dCollection();
                foreach (var v in info.Vertices)
                    pts.Add(v);

                // Create the wipeout
                Wipeout wipeout = new Wipeout();
                wipeout.SetDatabaseDefaults();
                wipeout.SetFrom(pts, info.Normal);

                ApplyEntityProperties(db, wipeout, info);

                btr.AppendEntity(wipeout);
                tr.AddNewlyCreatedDBObject(wipeout, true);
                tr.Commit();
            }
        }
        /// <summary> Draws a block reference in the specified database with attributes using the provided block reference information.
        public void AddBlockReference(Database db, BlockReferenceInfo info)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (string.IsNullOrWhiteSpace(info.BlockName)) throw new ArgumentException("Block name is required.", nameof(info));

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                if (!bt.Has(info.BlockName))
                    return;

                var modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var blockDef = (BlockTableRecord)tr.GetObject(bt[info.BlockName], OpenMode.ForRead);

                var br = new BlockReference(info.Position, blockDef.ObjectId)
                {
                    Rotation = info.Rotation,
                    ScaleFactors = info.ScaleFactors
                };

                ApplyEntityProperties(db, br, info);
                modelSpace.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);

                // Add Attributes
                if (blockDef.HasAttributeDefinitions)
                {
                    foreach (ObjectId id in blockDef)
                    {
                        if (tr.GetObject(id, OpenMode.ForRead) is AttributeDefinition attDef && !attDef.Constant)
                        {
                            var attrInfo = info.Attributes.FirstOrDefault(a => a.Tag.Equals(attDef.Tag, StringComparison.OrdinalIgnoreCase));
                            if (attrInfo == null) continue;

                            var attrRef = new AttributeReference
                            {
                                Position = attDef.Position.TransformBy(br.BlockTransform),
                                TextString = attrInfo.TextString,
                                Tag = attDef.Tag,
                                Height = attDef.Height,
                                Rotation = attDef.Rotation,
                                Justify = attDef.Justify,
                                AlignmentPoint = attDef.AlignmentPoint.TransformBy(br.BlockTransform),
                                HorizontalMode = attDef.HorizontalMode,
                                VerticalMode = attDef.VerticalMode,
                                TextStyleId = attDef.TextStyleId
                            };

                            attrRef.SetDatabaseDefaults();
                            br.AttributeCollection.AppendAttribute(attrRef);
                            tr.AddNewlyCreatedDBObject(attrRef, true);
                        }
                    }
                }

                tr.Commit();
            }
        }
     
        /// <summary> Sets the dimension style for a given Dimension based on DimensionInfo.
        private void SetDimensionStyle(Database db, Dimension dim, DimensionInfo info)
        {
            if (!string.IsNullOrWhiteSpace(info.DimensionStyle))
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
                    if (dst.Has(info.DimensionStyle))
                    {
                        dim.DimensionStyle = dst[info.DimensionStyle];
                    }
                    tr.Commit();
                }
            }
            else if (!info.DimensionStyleId.IsNull)
            {
                dim.DimensionStyle = info.DimensionStyleId;
            }
        }

        /// <summary> Applies common properties from EntityInfo to the given Entity.
        private void ApplyEntityProperties(Database db, Entity entity, EntityInfo info)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Check and set Layer if it exists
                if (!string.IsNullOrEmpty(info.Layer))
                {
                    var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    if (layerTable.Has(info.Layer))
                    {
                        entity.Layer = info.Layer;
                    }
                }

                // Check and set Linetype if it exists
                if (!string.IsNullOrEmpty(info.Linetype))
                {
                    var linetypeTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                    if (linetypeTable.Has(info.Linetype))
                    {
                        entity.Linetype = info.Linetype;
                    }
                }

                tr.Commit();
            }

            // Assign other properties outside transaction
            entity.LineWeight = info.LineWeight;
            entity.LinetypeScale = info.LinetypeScale;

            entity.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(
                Autodesk.AutoCAD.Colors.ColorMethod.ByAci,
                info.ColorIndex
            );
        }

        /// <summary>  Appends a given entity to the model space of the specified database.
        private void AppendEntity(Database db, Entity entity)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                btr.AppendEntity(entity);
                tr.AddNewlyCreatedDBObject(entity, true);

                tr.Commit();
            }
        }

    }

        // 🔹 Base entity info
        public class EntityInfo
        {
            public string Layer { get; set; }
            public short ColorIndex { get; set; }
            public string Linetype { get; set; }
            public LineWeight LineWeight { get; set; }
            public double LinetypeScale { get; set; }
        }

        // 🔹 Line
        public class LineInfo : EntityInfo
        {
            public Point3d StartPoint { get; set; }
            public Point3d EndPoint { get; set; }
        }

        // 🔹 Circle
        public class CircleInfo : EntityInfo
        {
            public Point3d Center { get; set; }
            public double Radius { get; set; }
            public Vector3d Normal { get; set; }
        }

        // 🔹 Arc
        public class ArcInfo : EntityInfo
        {
            public Point3d Center { get; set; }
            public double Radius { get; set; }
            public double StartAngle { get; set; }
            public double EndAngle { get; set; }
            public Vector3d Normal { get; set; }
        }

        // 🔹 Polyline (2D)
        public class PolylineInfo : EntityInfo
        {
            public List<PolylineVertex> Vertices { get; set; } = new List<PolylineVertex>();
            public bool Closed { get; set; }
            public double Elevation { get; set; }
            public Vector3d Normal { get; set; }
        }

        public class PolylineVertex
        {
            public Point2d Point { get; set; }
            public double Bulge { get; set; }
            public double StartWidth { get; set; }
            public double EndWidth { get; set; }
        }

        // 🔹 Polyline3D
        public class Polyline3dInfo : EntityInfo
        {
            public List<Point3d> Vertices { get; set; } = new List<Point3d>();
            public Poly3dType PolyType { get; set; }
            public bool Closed { get; set; }
        }

        // 🔹 Ellipse
        public class EllipseInfo : EntityInfo
        {
            public Point3d Center { get; set; }
            public Vector3d Normal { get; set; }
            public Vector3d MajorAxis { get; set; }
            public double RadiusRatio { get; set; }
            public double StartAngle { get; set; }
            public double EndAngle { get; set; }
        }

        // 🔹 Text (DBText)
        public class DBTextInfo : EntityInfo
        {
            public Point3d Position { get; set; }
            public string TextString { get; set; }
            public double Height { get; set; }
            public double Rotation { get; set; }
            public double WidthFactor { get; set; }
            public ObjectId TextStyleId { get; set; }
        }

        // 🔹 MText
        public class MTextInfo : EntityInfo
        {
            public Point3d Location { get; set; }
            public string Contents { get; set; }
            public double TextHeight { get; set; }
            public double Width { get; set; }
            public double Rotation { get; set; }
            public ObjectId TextStyleId { get; set; }
        }

        // 🔹 BlockReference
        public class BlockReferenceInfo : EntityInfo
        {
            public string BlockName { get; set; }
            public Point3d Position { get; set; }
            public Scale3d ScaleFactors { get; set; }
            public double Rotation { get; set; }
            public List<AttributeReferenceInfo> Attributes { get; set; } = new List<AttributeReferenceInfo>();
        }

        public class AttributeReferenceInfo
        {
            public string Tag { get; set; }
            public string TextString { get; set; }
        }

        // 🔹 Spline
        public class SplineInfo : EntityInfo
        {
            public List<Point3d> ControlPoints { get; set; } = new List<Point3d>();
            public List<double> Knots { get; set; } = new List<double>();
            public int Degree { get; set; }
            public bool Closed { get; set; }
            public bool Periodic { get; set; }
            public double FitTolerance { get; set; }
            public Vector3d Normal { get; set; }
        }

        // 🔹 Hatch
        public class HatchInfo : EntityInfo
        {
            public HatchPatternType PatternType { get; set; } = HatchPatternType.PreDefined;
            public string PatternName { get; set; } = "ANSI31";  // common default pattern
            public double PatternAngle { get; set; } = 0.0;
            public double PatternScale { get; set; } = 1.0;

            public List<DBObject> BoundaryEntities { get; set; } = new List<DBObject>();
            // These are curve entities (like Polyline, Circle, etc.) defining hatch boundary
        }


        // ✅ Base dimension class
        public abstract class DimensionInfo : EntityInfo
        {
            public string DimensionText { get; set; } = "<>"; // Default AutoCAD text
            public string DimensionStyle { get; set; } = "Standard"; // Optional name
            public ObjectId DimensionStyleId { get; set; } = ObjectId.Null; // Optional resolved ID
        }

        // ✅ Rotated (horizontal/vertical linear) dimension
        public class RotatedDimensionInfo : DimensionInfo
        {
            public Point3d XLine1Point { get; set; }
            public Point3d XLine2Point { get; set; }
            public Point3d DimLinePoint { get; set; }
            public double Rotation { get; set; } = 0.0; // Angle in radians
        }

        // ✅ Aligned dimension (aligned between two points)
        public class AlignedDimensionInfo : DimensionInfo
        {
            public Point3d XLine1Point { get; set; }
            public Point3d XLine2Point { get; set; }
            public Point3d DimLinePoint { get; set; }
        }

        // ✅ Radial dimension (for radius)
        public class RadialDimensionInfo : DimensionInfo
        {
            public Point3d Center { get; set; }
            public Point3d ChordPoint { get; set; }
            public double LeaderLength { get; set; } = 0.0;
        }

        // ✅ Diametric dimension (for diameter)
        public class DiametricDimensionInfo : DimensionInfo
        {
            public Point3d ChordPoint { get; set; }
            public Point3d FarChordPoint { get; set; }
            public double LeaderLength { get; set; }

        }

        // ✅ Line angular dimension (angle between two lines)
        public class LineAngularDimensionInfo : DimensionInfo
        {
            public Point3d Line1Start { get; set; }
            public Point3d Line1End { get; set; }
            public Point3d Line2Start { get; set; }
            public Point3d Line2End { get; set; }
            public Point3d DimensionArcPoint { get; set; }
        }

        // ✅ Arc dimension (angle along arc between start and end)
        public class ArcDimensionInfo : DimensionInfo
        {
            public Point3d Center { get; set; }
            public Point3d StartPoint { get; set; }
            public Point3d EndPoint { get; set; }
            public Point3d DimensionArcPoint { get; set; }
        }

        // ✅ Point3 Angular dimension (Three-point angular)
        public class Point3AngularDimensionInfo : DimensionInfo
        {
            public Point3d Vertex { get; set; }
            public Point3d StartPoint { get; set; }
            public Point3d EndPoint { get; set; }
            public Point3d DimensionArcPoint { get; set; }
        }
        // Leader model
        public class LeaderInfo : EntityInfo
        {
            public List<Point3d> Vertices { get; set; } = new List<Point3d>();
            public ObjectId Annotation { get; set; }  // optional
            public bool HasArrowHead { get; set; } = true;

        }

        // MLeader model
        public class MLeaderInfo : EntityInfo
        {
            public List<Point3d> LeaderLines { get; set; } = new List<Point3d>();
            public Point3d AttachmentPoint { get; set; }
            public string Text { get; set; }
            public double TextHeight { get; set; } = 1.0;
            public ObjectId ArrowSymbolId { get; set; } = ObjectId.Null;
            public LeaderType LeaderLineType { get; set; } = LeaderType.StraightLeader;
            public bool IsAnnotative { get; set; } = false;
        }

        // 🔹 Raster Image
        public class RasterImageInfo : EntityInfo
        {
            public string FilePath { get; set; }
            public Point3d InsertionPoint { get; set; }
            public double Rotation { get; set; }
            public Scale3d ScaleFactors { get; set; }
        }
        // 🔹 XLine (Construction Line)
        public class XLineInfo : EntityInfo
        {
            public Point3d BasePoint { get; set; }
            public Vector3d Direction { get; set; }
        }

        // 🔹 Ray (Semi-infinite Line)
        public class RayInfo : EntityInfo
        {
            public Point3d BasePoint { get; set; }
            public Vector3d Direction { get; set; }
        }

        // 🔹 Point (DBPoint)
        public class PointInfo : EntityInfo
        {
            public Point3d Position { get; set; }
        }

        public class WipeoutInfo :EntityInfo
        {
            public List<Point2d> Vertices { get; set; } = new List<Point2d>();
            public double Elevation { get; set; } = 0.0;
            public Vector3d Normal { get; set; } = Vector3d.ZAxis;
            public bool ShowFrame { get; set; } = true;
        }

        // 🔹 Table
        public class TableInfo : EntityInfo
        {
            public int Rows { get; set; } = 2;
            public int Columns { get; set; } = 2;
            public double RowHeight { get; set; } = 3.0;
            public double ColumnWidth { get; set; } = 15.0;
            public Point3d InsertPoint { get; set; } = Point3d.Origin;

            // Optional: you can add a 2D array or List<List<string>> for cell contents
            public string[,] CellContents { get; set; } = null;
        }


        // 🔹 Layer (from your earlier request)
        public class LayerInfo
        {
            public string Name { get; set; }
            public short ColorIndex { get; set; }
            public LineWeight LineWeight { get; set; }
            public bool IsOff { get; set; }
            public bool IsFrozen { get; set; }
            public bool IsLocked { get; set; }
        }
    

}
