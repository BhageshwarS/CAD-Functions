using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MYCOLLECTION
{
    /// <summary>
    /// Extension methods for performing fillet operations on AutoCAD Polylines at specified vertices.
    /// </summary>
    public static class PolylineFilletExtensions
    {
        /// <summary>
        /// Applies a fillet at the given vertex index of the polyline.
        /// Only applies fillet if both adjacent segments are lines or arcs.
        /// </summary>
        public static void FilletAtVertex(this Polyline pline, int vertexIndex, double radius)
        {
            // Determine the previous and next vertex indices, accounting for closed polylines
            int prevIndex = (vertexIndex == 0 && pline.Closed) ? pline.NumberOfVertices - 1 : vertexIndex - 1;
            int nextIndex = (vertexIndex == pline.NumberOfVertices - 1 && pline.Closed) ? 0 : vertexIndex + 1;

            // Only perform the fillet if both segments are lines or arcs
            if ((pline.GetSegmentType(prevIndex) == SegmentType.Line || pline.GetSegmentType(prevIndex) == SegmentType.Arc) &&
                (pline.GetSegmentType(vertexIndex) == SegmentType.Line || pline.GetSegmentType(vertexIndex) == SegmentType.Arc))
            {
                pline.FilletAt(vertexIndex, radius);
            }
            else
            {
                // Alert or handle cases where fillet cannot be applied
            }
        }

        /// <summary>
        /// Determines which type of fillet to apply based on the adjacent segment types.
        /// </summary>
        private static void FilletAt(this Polyline pline, int index, double radius)
        {
            int prev = index == 0 && pline.Closed ? pline.NumberOfVertices - 1 : index - 1;
            SegmentType prevType = pline.GetSegmentType(prev);
            SegmentType nextType = pline.GetSegmentType(index);

            // Call the appropriate fillet method based on segment types
            if (prevType == SegmentType.Line && nextType == SegmentType.Line)
            {
                PerformLineFillet(pline, prev, index, radius);
            }
            else if (prevType == SegmentType.Arc && nextType == SegmentType.Line)
            {
                PerformArcLineFillet(pline, prev, index, radius);
            }
            else if (prevType == SegmentType.Line && nextType == SegmentType.Arc)
            {
                PerformLineArcFillet(pline, prev, index, radius);
            }
            else if (prevType == SegmentType.Arc && nextType == SegmentType.Arc)
            {
                PerformArcFillet(pline, prev, index, radius);
            }
        }

        /// <summary>
        /// Performs a fillet between two line segments.
        /// </summary>
        private static void PerformLineFillet(Polyline pline, int prev, int index, double radius)
        {
            LineSegment2d seg1 = pline.GetLineSegment2dAt(prev);
            LineSegment2d seg2 = pline.GetLineSegment2dAt(index);
            Vector2d vec1 = seg1.StartPoint - seg1.EndPoint;
            Vector2d vec2 = seg2.EndPoint - seg2.StartPoint;

            // Calculate angle between segments and distance to cut back
            double angle = (Math.PI - vec1.GetAngleTo(vec2)) / 2.0;
            double dist = radius * Math.Tan(angle);

            // Ensure the distance does not exceed the segment lengths
            if (dist == 0.0 || dist > seg1.Length || dist > seg2.Length)
                return;

            // Calculate new tangent points
            Point2d pt1 = seg1.EndPoint + vec1.GetNormal() * dist;
            Point2d pt2 = seg2.StartPoint + vec2.GetNormal() * dist;

            // Calculate the bulge value for the arc
            double bulge = Math.Tan(angle / 2.0);
            if (Clockwise(seg1.StartPoint, seg1.EndPoint, seg2.EndPoint))
                bulge = -bulge;

            // Insert new vertex with bulge and update polyline
            pline.AddVertexAt(index, pt1, bulge, 0.0, 0.0);
            pline.SetPointAt(index + 1, pt2);
        }

        /// <summary>
        /// Performs a fillet between an arc and a line.
        /// </summary>
        private static void PerformArcLineFillet(Polyline pline, int arcIndex, int lineIndex, double radius)
        {
            CircularArc2d arc = pline.GetArcSegment2dAt(arcIndex);
            LineSegment2d line = pline.GetLineSegment2dAt(lineIndex);

            Point2d tangentArc = GetArcTangentPoint(arc, line.StartPoint, radius);
            Point2d tangentLine = GetLineTangentPoint(line, arc.EndPoint, radius);

            if (tangentArc == null || tangentLine == null)
                return;

            double bulge = CalculateArcBulge(arc, tangentArc, tangentLine);
            pline.SetPointAt(arcIndex, tangentArc);
            pline.SetPointAt(lineIndex, tangentLine);
            pline.AddVertexAt(lineIndex, tangentArc, bulge, 0.0, 0.0);
        }

        /// <summary>
        /// Performs a fillet between a line and an arc (uses ArcLine logic).
        /// </summary>
        private static void PerformLineArcFillet(Polyline pline, int lineIndex, int arcIndex, double radius)
        {
            PerformArcLineFillet(pline, arcIndex, lineIndex, radius);
        }

        /// <summary>
        /// Performs a fillet between two arcs.
        /// </summary>
        private static void PerformArcFillet(Polyline pline, int arc1Index, int arc2Index, double radius)
        {
            CircularArc2d arc1 = pline.GetArcSegment2dAt(arc1Index);
            CircularArc2d arc2 = pline.GetArcSegment2dAt(arc2Index);

            Point2d tangentArc1 = GetArcTangentPoint(arc1, arc2.StartPoint, radius);
            Point2d tangentArc2 = GetArcTangentPoint(arc2, arc1.EndPoint, radius);

            if (tangentArc1 == null || tangentArc2 == null)
                return;

            double bulge = CalculateArcBulge(arc1, tangentArc1, tangentArc2);
            pline.SetPointAt(arc1Index, tangentArc1);
            pline.SetPointAt(arc2Index, tangentArc2);
            pline.AddVertexAt(arc2Index, tangentArc1, bulge, 0.0, 0.0);
        }

        /// <summary>
        /// Gets the tangent point from an arc to a line direction.
        /// </summary>
        private static Point2d GetArcTangentPoint(CircularArc2d arc, Point2d linePoint, double radius)
        {
            Vector2d dir = arc.Center - linePoint;
            double dist = dir.Length - arc.Radius;

            if (dist > radius)
            {
                // No valid fillet possible
            }

            dir = dir.GetNormal() * radius;
            return linePoint + dir;
        }

        /// <summary>
        /// Gets the tangent point along a line towards an arc.
        /// </summary>
        private static Point2d GetLineTangentPoint(LineSegment2d line, Point2d arcPoint, double radius)
        {
            Vector2d dir = line.StartPoint - line.EndPoint;
            double dist = dir.Length;

            if (dist < radius)
            {
                // No valid fillet possible
            }

            dir = dir.GetNormal() * radius;
            return line.StartPoint + dir;
        }

        /// <summary>
        /// Calculates the bulge required to form an arc between two tangent points.
        /// </summary>
        private static double CalculateArcBulge(CircularArc2d arc, Point2d tangentArc, Point2d tangentLine)
        {
            Vector2d vec1 = arc.Center - tangentArc;
            Vector2d vec2 = arc.Center - tangentLine;
            double angle = vec1.GetAngleTo(vec2);
            return Math.Tan(angle / 4);
        }

        /// <summary>
        /// Determines if a set of three points are in clockwise order.
        /// </summary>
        private static bool Clockwise(Point2d p1, Point2d p2, Point2d p3) =>
            ((p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X)) < 1e-8;

        /// <summary>
        /// Computes the bulge of a given arc.
        /// </summary>
        public static double GetArcBulge(Arc arc)
        {
            double deltaAng = arc.EndAngle - arc.StartAngle;
            if (deltaAng < 0)
                deltaAng += 2 * Math.PI;
            return Math.Tan(deltaAng * 0.25);
        }

        /// <summary>
        /// Adds arc geometry to a polyline by subdividing into small linear segments.
        /// </summary>
        private static void AddArcToPolyline(Polyline polyline, Arc arc)
        {
            double startAngle = arc.StartAngle;
            double endAngle = arc.EndAngle;
            int segments = 4; // Number of segments to represent arc

            for (int i = 0; i <= segments; i++)
            {
                double angle = startAngle + (endAngle - startAngle) * i / segments;
                Point3d arcPoint = new Point3d(
                    arc.Center.X + arc.Radius * Math.Cos(angle),
                    arc.Center.Y + arc.Radius * Math.Sin(angle),
                    0);

                polyline.AddVertexAt(polyline.NumberOfVertices, new Point2d(arcPoint.X, arcPoint.Y), 0, 0, 0);
            }
        }

        /// <summary>
        /// Creates an arc from a start point, end point, and radius.
        /// </summary>
        public static Arc CreateArc(Point3d startPoint, Point3d endPoint, double radius)
        {
            Vector3d lineVector = endPoint - startPoint;
            double lineLength = lineVector.Length;
            Point3d midPoint = new Point3d((startPoint.X + endPoint.X) / 2, (startPoint.Y + endPoint.Y) / 2, startPoint.Z);
            Vector3d unitVector = lineVector.GetNormal();
            Vector3d perpendicular = new Vector3d(-unitVector.Y, unitVector.X, 0);

            // Calculate distance from midpoint to arc center
            double distanceFromMid = Math.Sqrt(radius * radius - (lineLength / 2) * (lineLength / 2));
            Point3d centerPoint = midPoint + perpendicular * distanceFromMid;

            // Compute start and end angles for the arc
            double startAngle = Math.Atan2(startPoint.Y - centerPoint.Y, startPoint.X - centerPoint.X);
            double endAngle = Math.Atan2(endPoint.Y - centerPoint.Y, endPoint.X - centerPoint.X);

            return new Arc(centerPoint, radius, startAngle, endAngle);
        }
    }
}
