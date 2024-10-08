﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static UnityEngine.RectTransform;

namespace PerfectlyNormalBaS
{
    public static class BezierUtil
    {
        #region struct: BezierMeshSamples_Distance

        private struct BezierMeshSamples_Distance
        {
            public Vector3 From { get; set; }
            public Vector3 To { get; set; }

            public VectorInt2 FromIndex { get; set; }
            public VectorInt2 ToIndex { get; set; }

            public float DistSegment { get; set; }
            public float DistSum { get; set; }

            public bool IsHorizontal { get; set; }
        }

        #endregion

        // Get points along the curve
        public static Vector3[] GetPoints(int count, Vector3 from, Vector3 control, Vector3 to)
        {
            return GetPoints(count, new[] { from, control, to });
        }
        public static Vector3[] GetPoints(int count, Vector3 from, Vector3 fromControl, Vector3 toControl, Vector3 to)
        {
            return GetPoints(count, new[] { from, fromControl, toControl, to });
        }
        public static Vector3[] GetPoints(int count, Vector3 from, Vector3[] controls, Vector3 to)
        {
            return GetPoints(count, UtilityCore.Iterate<Vector3>(from, controls, to).ToArray());
        }
        public static Vector3[] GetPoints(int count, Vector3[] controlPoints)
        {
            #region asserts
#if DEBUG
            if (controlPoints.Length < 2)
            {
                throw new ArgumentException("There must be at least two points passed in: " + controlPoints.Length.ToString());
            }
#endif
            #endregion

            float countD = count - 1;

            Vector3[] retVal = new Vector3[count];

            retVal[0] = controlPoints[0];
            retVal[count - 1] = controlPoints[controlPoints.Length - 1];

            for (int cntr = 1; cntr < count - 1; cntr++)
            {
                retVal[cntr] = GetPoint(cntr / countD, controlPoints);
            }

            return retVal;
        }
        public static Vector3[] GetPoints(int count, BezierSegment3D segment)
        {
            return GetPoints(count, UtilityCore.Iterate<Vector3>(segment.EndPoint0, segment.ControlPoints, segment.EndPoint1).ToArray());
        }
        public static Vector3[] GetPoints(int count, BezierSegment3D[] bezier)
        {
            float divisor = count - 1;

            return Enumerable.Range(0, count).
                Select(o => GetPoint(o / divisor, bezier)).
                ToArray();
        }

        /// <summary>
        /// Get a single point along the curve
        /// </summary>
        public static Vector3 GetPoint(float percent, BezierSegment3D segment)
        {
            return GetPoint(percent, UtilityCore.Iterate<Vector3>(segment.EndPoint0, segment.ControlPoints, segment.EndPoint1).ToArray());
        }
        /// <summary>
        /// Get a single point along the curve
        /// </summary>
        /// <returns>
        /// Got this here:
        /// http://www.cubic.org/docs/bezier.htm
        /// </returns>
        public static Vector3 GetPoint(float percent, Vector3[] controlPoints)
        {
            #region asserts
#if DEBUG
            if (controlPoints.Length < 2)
            {
                throw new ArgumentException("There must be at least two points passed in: " + controlPoints.Length.ToString());
            }
#endif
            #endregion

            Vector3[] prev = controlPoints;
            Vector3[] current = null;

            for (int outer = controlPoints.Length - 1; outer > 0; outer--)
            {
                current = new Vector3[outer];

                for (int inner = 0; inner < outer; inner++)
                {
                    current[inner] = Math3D.LERP(prev[inner], prev[inner + 1], percent);
                }

                prev = current;
            }

            return current[0];      // by the time execution gets here, the array only has one element
        }
        public static Vector3 GetPoint(float percent, BezierSegment3D[] bezier)
        {
            //TODO: If the bezier is closed, make it circular
            if (percent < 0)
                return bezier[0].EndPoint0;

            float totalLength = bezier.Sum(o => o.Length_quick);

            float fromPercent = 0;
            for (int cntr = 0; cntr < bezier.Length; cntr++)
            {
                float toPercent = fromPercent + (bezier[cntr].Length_quick / totalLength);

                if (percent >= fromPercent && percent <= toPercent)
                {
                    float localPercent = ((percent - fromPercent) * totalLength) / bezier[cntr].Length_quick;

                    return GetPoint(localPercent, bezier[cntr]);
                }

                fromPercent = toPercent;
            }

            return bezier[bezier.Length - 1].EndPoint1;
        }

        /// <summary>
        /// This returns points across several segment definitions.  count is the total number of sample points to return
        /// </summary>
        /// <remarks>
        /// This assumes that the segments are linked together into a single path
        /// 
        /// If the first and last point of segments are the same, then this will only return that shared point once (but the point count
        /// will still be how many were requested
        /// </remarks>
        public static Vector3[] GetPath(int count, BezierSegment3D[] segments)
        {
            // Get the total length of the curve
            float totalLength = 0;
            float[] cumulativeLengths = new float[segments.Length + 1];
            for (int cntr = 1; cntr < segments.Length + 1; cntr++)
            {
                totalLength += segments[cntr - 1].Length_quick;
                cumulativeLengths[cntr] = cumulativeLengths[cntr - 1] + segments[cntr - 1].Length_quick;
            }

            float countD = count - 1;

            Vector3[] retVal = new Vector3[count];

            retVal[0] = segments[0].EndPoint0;
            retVal[count - 1] = segments[segments.Length - 1].EndPoint1;        //NOTE: If the segment is a closed curve, this is the same point as retVal[0].  May want a boolean that tells whether the last point should be replicated

            int index = 0;

            for (int cntr = 1; cntr < count - 1; cntr++)
            {
                // Get the location along the entire path
                float totalPercent = cntr / countD;
                float portionTotalLength = totalLength * totalPercent;

                // Advance to the appropriate segment
                while (cumulativeLengths[index + 1] < portionTotalLength)
                {
                    index++;
                }

                // Get the percent of the current segment
                float localLength = portionTotalLength - cumulativeLengths[index];
                float localPercent = localLength / segments[index].Length_quick;

                // Calculate the bezier point
                retVal[cntr] = GetPoint(localPercent, segments[index].Combined);
            }

            return retVal;
        }
        /// <summary>
        /// This returns points across sets of segment definition.  Each set is run through the other path overload.  So the endpoints
        /// of each set are guaranteed to be included in the return points (deduped)
        /// </summary>
        /// <param name="countPerPath">This is how many points per set (the total number of points will be countPerPath * segmentSets.Length)</param>
        public static Vector3[] GetPath(int countPerPath, BezierSegment3D[][] segmentSets)
        {
            //TODO: Make an overload that takes in total count instead of per path

            // Get the points for each set of beziers
            List<Vector3[]> perPathPoints = new List<Vector3[]>();

            foreach (BezierSegment3D[] segments in segmentSets)
            {
                if (segments.Length == 1)
                {
                    perPathPoints.Add(GetPoints(countPerPath, segments[0]));
                }
                else
                {
                    perPathPoints.Add(GetPath(countPerPath, segments));
                }
            }

            // Dedupe them
            List<Vector3> retVal = new List<Vector3>();

            retVal.AddRange(perPathPoints[0]);

            for (int cntr = 1; cntr < perPathPoints.Count; cntr++)
            {
                if (retVal[retVal.Count - 1].IsNearValue(perPathPoints[cntr][0]))
                {
                    // First point dupes with the last
                    retVal.AddRange(perPathPoints[cntr].Skip(1));
                }
                else
                {
                    retVal.AddRange(perPathPoints[cntr]);
                }
            }

            if (retVal[0].IsNearValue(retVal[retVal.Count - 1]))
            {
                retVal.RemoveAt(retVal.Count - 1);
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This is a helper method that creates a bezier definition that runs through a set of points
        /// </summary>
        /// <param name="ends">These are the end points that the beziers run through</param>
        /// <param name="along">This is how far out the control points should be pulled from the end points (it is a percent of that line segment's length)</param>
        /// <param name="isClosed">
        /// True: The assumption is that ends[0] and ends[len-1] aren't the same point.  This will add an extra segment to create a closed curve.
        /// False: This method compares ends[0] and ends[len-1].  If they are the same point, it makes a closed curve.  If they are different, it makes an open curve.
        /// </param>
        public static BezierSegment3D[] GetBezierSegments(Vector3[] ends, float along = .25f, bool isClosed = false)
        {
            if (isClosed)
            {
                return GetBezierSegments_Closed(ends, along);
            }

            if (ends.Length > 2 && ends[0].IsNearValue(ends[ends.Length - 1]))
            {
                Vector3[] endsClosed = new Vector3[ends.Length - 1];
                Array.Copy(ends, endsClosed, ends.Length - 1);
                return GetBezierSegments_Closed(endsClosed, along);       // remove the last point, which is redundant
            }
            else
            {
                return GetBezierSegments_Open(ends, along);
            }
        }

        public static Tuple<Vector2, Vector2> GetControlPoints_Middle(Vector2 end1, Vector2 end2, Vector2 end3, float percentAlong12 = .25f, float percentAlong23 = .25f)
        {
            // Just use the 3D overload
            var retVal = GetControlPoints_Middle(end1.ToVector3(), end2.ToVector3(), end3.ToVector3(), percentAlong12, percentAlong23);

            // Convert the response back to 2D
            return Tuple.Create(retVal.Item1.ToVector2(), retVal.Item2.ToVector2());
        }
        /// <summary>
        /// This is a helper method to generate control points
        /// </summary>
        /// <remarks>
        /// A bezier curve will always go through the end points.  It will use the control points to pull it off the direct
        /// line segment.
        /// 
        /// When two bezier segments are linked, the curve will be smooth if the two control points for the shared
        /// end point are in a line.
        /// 
        /// This method takes the three end points, and returns the two control points for the middle end point (end2)
        /// 
        /// The returned control points will be colinear with end2
        /// </remarks>
        /// <param name="percentAlong12">This is the percent of the 1-2 segment's length</param>
        /// <param name="percentAlong23">This is the percent of the 2-3 segment's length</param>
        /// <returns>
        /// Item1=control point for end2 for the 1-2 bezier segment (this is the last point in this.ControlPoints)
        /// Item2=control point for end2 for the 2-3 bezier segment (this is the first point in this.ControlPoints)
        /// </returns>
        public static Tuple<Vector3, Vector3> GetControlPoints_Middle(Vector3 end1, Vector3 end2, Vector3 end3, float percentAlong12 = .25f, float percentAlong23 = .25f)
        {
            Vector3 dir21 = end1 - end2;
            Vector3 dir23 = end3 - end2;

            Vector3? controlLine = GetControlPoints_Middle_ControlLine(dir21, dir23);
            if (controlLine == null)
            {
                // The directions are either on top of each other, or pointing directly away from each other, or
                // some of the end points are the same.
                //
                // Just return control points that are the same as the middle point.  This could be improved in the
                // future if certain cases look bad
                return Tuple.Create(end2, end2);
            }

            Vector3 controlLineUnit;
            if (Vector3.Dot(dir21, controlLine.Value) > 0)
            {
                // Control line is toward end 1
                controlLineUnit = controlLine.Value.normalized;
            }
            else
            {
                // Control line is toward end 3
                controlLineUnit = (-controlLine.Value).normalized;
            }

            Vector3 control21 = end2 + (controlLineUnit * (dir21.magnitude * percentAlong12));
            Vector3 control23 = end2 - (controlLineUnit * (dir23.magnitude * percentAlong23));

            return Tuple.Create(control21, control23);
        }

        /// <summary>
        /// This creates a control point for end1.  It is along a line that is some angle from dir12.  The distance along
        /// that rotated direction is (end2-end1)*percentAlong12
        /// </summary>
        /// <param name="otherPoint">This is a third point that is coplanar to end1 and end2.  It is just used to figure out the rotation axis (axis will be orthogonal to the plane defined by the three points)</param>
        /// <param name="isAwayFromOther">Whether the control line should rotate away from otherPoint or toward it</param>
        /// <param name="angle">Angle in degrees</param>
        /// <param name="percentAlong12">This is the percent of the 1-2 segment's length</param>
        public static Vector3 GetControlPoint_End(Vector3 end1, Vector3 end2, Vector3 otherPoint, bool isAwayFromOther = true, float angle = 30, float percentAlong12 = .25f)
        {
            // Figure out the axis
            Vector3 axis = isAwayFromOther ?
                Vector3.Cross(otherPoint - end1, end2 - end1) :
                Vector3.Cross(end2 - end1, otherPoint - end1);

            // Call the other overload
            return GetControlPoint_End(end1, end2, axis, angle, percentAlong12);
        }
        public static Vector3 GetControlPoint_End(Vector3 end1, Vector3 end2, Vector3 axis, float angle = 20, float along12 = .25f)
        {
            Vector3 dir12 = end2 - end1;

            Vector3 controlLine = Quaternion.AngleAxis(angle, axis) * dir12.normalized;
            controlLine = controlLine * (dir12.magnitude * along12);

            return end1 + controlLine;
        }

        /// <summary>
        /// This takes in some horizontal beziers, then can generate a grid of points from that definition (using the beziers to infer
        /// points at regular intervals)
        /// </summary>
        /// <param name="horizontals">
        /// A set of beziers that define horizontals
        /// 
        /// Use GetBezierSegments() to create a horizontal line
        /// 
        /// The horizontals don't need to form a rectangle, they could form any kind of patch (they don't even need to be horizontal
        /// along X).  But they shouldn't cross over each other
        /// </param>
        /// <param name="horzCount">How many horizontal points to return</param>
        /// <param name="vertCount">How many vertical points to return</param>
        /// <param name="controlPointPercent">
        /// Where along the line segment to place the control point
        /// 
        /// .5 would be halfway.  Anything less than half will cause the curves to pinch.  Anything greater will exaggerate the curves
        /// </param>
        /// <returns>
        /// A grid of points
        /// 
        /// NOTE: This 1D arrays is vertical, which is backward from normal images (bitmaps are horizontal scan lines concatenated
        /// together.  These GetBezierMesh functions use vertical scan lines concatenated together)
        /// </returns>
        public static Vector3[] GetBezierMesh_Points(BezierSegment3D[][] horizontals, int horzCount, int vertCount, float controlPointPercent = .5f)
        {
            if (horzCount < 2 || vertCount < 2)
            {
                throw new ArgumentException($"horzCount and vertCount need to be at least 2.  horzCount={horzCount}, vertCount={vertCount}");
            }

            return GetVerticalSamples(horizontals, horzCount, vertCount, controlPointPercent).
                SelectMany(o => o).
                ToArray();
        }
        public static Vector3[][] GetBezierMesh_Horizontals(BezierSegment3D[][] horizontals, int horzCount, int vertCount, float controlPointPercent = .5f)
        {
            if (horzCount < 2 || vertCount < 2)
            {
                throw new ArgumentException($"horzCount and vertCount need to be at least 2.  horzCount={horzCount}, vertCount={vertCount}");
            }

            Vector3[][] verticals = GetVerticalSamples(horizontals, horzCount, vertCount, controlPointPercent);

            // Convert the verticals into horizontals
            return Enumerable.Range(0, vertCount).
                Select(v => Enumerable.Range(0, horzCount).
                    Select(h => verticals[h][v]).
                    ToArray()).
                ToArray();
        }

        ///// <summary>
        ///// This creates a continous mesh of triangles
        ///// </summary>
        ///// <remarks>
        ///// NOTE: When there were only two triangles per square, the lighting reflection looked bad.  So an extra middle point per
        ///// square is generated and each square has four triangles.  This means that there are more points returned than requested
        ///// </remarks>
        //public static ITriangleIndexed_wpf[] GetBezierMesh_Triangles(BezierSegment3D[][] horizontals, int horzCount, int vertCount, float controlPointPercent = .5)
        //{
        //    if (horzCount < 2 || vertCount < 2)
        //    {
        //        throw new ArgumentException($"horzCount and vertCount need to be at least 2.  horzCount={horzCount}, vertCount={vertCount}");
        //    }

        //    int horzCount_centers = (horzCount * 2) - 1;
        //    int vertCount_centers = (vertCount * 2) - 1;

        //    var verticals = GetVerticalSamples(horizontals, horzCount_centers, vertCount_centers, controlPointPercent);

        //    //NOTE: There are more points here than the triangles will use.  But it keeps the math simpler to keep a square grid
        //    Vector3[] allPoints = verticals.
        //        SelectMany(o => o).
        //        ToArray();

        //    List<ITriangleIndexed_wpf> triangles = new List<ITriangleIndexed_wpf>();

        //    // Build triangles
        //    foreach (var vertex in IterateTrianglePoints(horzCount_centers, vertCount_centers))
        //    {
        //        triangles.Add(new TriangleIndexed_wpf(vertex.index0, vertex.index1, vertex.index2, allPoints));
        //    }

        //    return triangles.ToArray();
        //}
        ///// <summary>
        ///// This creates a geometry that has the TextureCoordinates collection filled out
        ///// </summary>
        ///// <remarks>
        ///// For best results, the dimensions of the mesh should be roughly the same as the image (and roughly square).  If not, large amounts
        ///// of the edges of the mesh will stay transparent
        ///// 
        ///// NOTE: You must set "ImageBrush.ViewportUnits = BrushMappingMode.Absolute" to honor these texture mappings.  If you
        ///// leave it with the default of BrushMappingMode.RelativeToBoundingBox, wpf does it's own thing and the edges of the image
        ///// will likely fall off the geometry
        ///// 
        ///// TODO: Take in a zoom property
        ///// </remarks>
        ///// <param name="textureAspectRatio">
        ///// Measure the image's width/height before calling this function
        ///// 
        ///// This way the image will map to the curve and preserve the aspect ratio.  This function also makes sure the entire image fits on
        ///// the mesh.  Which means some of the geometry outside the image will be transparent
        ///// </param>
        //public static MeshGeometry3D GetBezierMesh_MeshGeometry3D(BezierSegment3D[][] horizontals, int horzCount, int vertCount, float controlPointPercent = .5, float textureAspectRatio = 1, bool invertY = true, float zoom = 1)
        //{
        //    if (horzCount < 3 || vertCount < 3)
        //    {
        //        throw new ArgumentException($"horzCount and vertCount need to be at least 3.  horzCount={horzCount}, vertCount={vertCount}");
        //    }
        //    else if (horzCount % 2 != 1 || vertCount % 2 != 1)
        //    {
        //        throw new ArgumentException($"horzCount and vertCount both need to be odd numbers (because the mesh's center needs to fall on an actual point).  horzCount={horzCount}, vertCount={vertCount}");
        //    }

        //    int horzCount_centers = (horzCount * 2) - 1;
        //    int vertCount_centers = (vertCount * 2) - 1;

        //    var verticals = GetVerticalSamples(horizontals, horzCount_centers, vertCount_centers, controlPointPercent);

        //    //NOTE: There are more points here than the triangles will use.  But it keeps the math simpler to keep a square grid
        //    Vector3[] allPoints = verticals.
        //        SelectMany(o => o).
        //        ToArray();

        //    VectorInt2 center = new VectorInt2(horzCount_centers / 2, vertCount_centers / 2);

        //    var distances = GetDistancesFromPoint(center, allPoints, horzCount_centers, vertCount_centers);

        //    #region build transform

        //    float maxX = distances.lines.
        //        Where(o => o.IsHorizontal).
        //        Where(o => o.ToIndex.X == 0 || o.ToIndex.X == horzCount_centers - 1).
        //        Min(o => Math.Abs(o.DistSum));

        //    float maxY = distances.lines.
        //        Where(o => !o.IsHorizontal).
        //        Where(o => o.ToIndex.Y == 0 || o.ToIndex.Y == vertCount_centers - 1).
        //        Min(o => Math.Abs(o.DistSum));

        //    maxX *= 2;      // aspect ratio deals with width, and maxX and Y are only have width, half height.
        //    maxY *= 2;

        //    float aspect = maxX / maxY;

        //    float scaleX = 1d;
        //    float scaleY = 1d;
        //    if (aspect.IsNearValue(textureAspectRatio))
        //    {
        //        scaleX = 1 / maxX;
        //        scaleY = 1 / maxY;
        //    }
        //    else if (aspect > textureAspectRatio)
        //    {
        //        scaleX = 1 / (maxX / (aspect / textureAspectRatio));
        //        scaleY = 1 / maxY;
        //    }
        //    else
        //    {
        //        scaleX = 1 / maxX;
        //        scaleY = 1 / maxY / (aspect / textureAspectRatio);
        //    }

        //    scaleX /= zoom;
        //    scaleY /= zoom;

        //    TransformGroup textureTransform = new TransformGroup();
        //    textureTransform.Children.Add(new ScaleTransform(scaleX, scaleY));      // turn -maxX to maxX into -.5 to .5
        //    textureTransform.Children.Add(new TranslateTransform(.5, .5));      // distances.lengths is centered at (.5,.5).  TextureCoordinates needs 0 to 1

        //    #endregion

        //    MeshGeometry3D retVal = new MeshGeometry3D();

        //    for (int cntr = 0; cntr < allPoints.Length; cntr++)
        //    {
        //        retVal.Positions.Add(allPoints[cntr]);

        //        Point point = textureTransform.Transform(distances.lengths[cntr].ToPoint());
        //        if (invertY)
        //        {
        //            point = new Point(point.X, 1 - point.Y);
        //        }

        //        retVal.TextureCoordinates.Add(point);
        //    }

        //    foreach (var vertex in IterateTrianglePoints(horzCount_centers, vertCount_centers))
        //    {
        //        retVal.TriangleIndices.Add(vertex.index0);
        //        retVal.TriangleIndices.Add(vertex.index1);
        //        retVal.TriangleIndices.Add(vertex.index2);
        //    }

        //    #region DEBUG DRAW

        //    //float LINE = .01;

        //    //Debug3DWindow window = new Debug3DWindow();

        //    ////TODO: Come up with more distinct colors
        //    //var getColor = new Func<float, Color>(d =>
        //    //{
        //    //    if (d >= 0 && d <= 1)
        //    //    {
        //    //        return UtilityWPF.AlphaBlend(Colors.White, Colors.Black, d);
        //    //    }
        //    //    else
        //    //    {
        //    //        d = d < 0 ? -d : d - 1;
        //    //        return UtilityWPF.AlphaBlend(Colors.Red, Colors.Gray, UtilityCore.GetScaledValue_Capped(0, 1, 0, 2, d));
        //    //    }
        //    //});

        //    //foreach (var line in distances.lines)
        //    //{
        //    //    Point textureCoord = new Point(line.DistSum, line.DistSum);     // the lines are either horizontal or vertical (so there should be twice as many distances.lines as distances.lengths)
        //    //    textureCoord = textureTransform.Transform(textureCoord);
        //    //    Color color = getColor(line.IsHorizontal ? textureCoord.X : textureCoord.Y);

        //    //    window.AddLine(line.From, line.To, LINE, color);
        //    //}

        //    //window.AddText($"max X: {maxX.ToStringSignificantDigits(3)}");
        //    //window.AddText($"max Y: {maxY.ToStringSignificantDigits(3)}");
        //    //window.AddText("");
        //    //window.AddText($"scale X: {scaleX.ToStringSignificantDigits(3)}");
        //    //window.AddText($"scale Y: {scaleY.ToStringSignificantDigits(3)}");

        //    //window.Show();

        //    #endregion

        //    return retVal;
        //}

        #region Private Methods

        private static BezierSegment3D[] GetBezierSegments_Closed(Vector3[] ends, float along = .25f)
        {
            //NOTE: The difference between closed and open is closed has one more segment that loops back to zero (and a control point for point zero)

            // Precalculate the control points
            Tuple<Vector3, Vector3>[] controls = new Tuple<Vector3, Vector3>[ends.Length - 1];

            for (int cntr = 1; cntr < ends.Length; cntr++)
            {
                int lastIndex = cntr == ends.Length - 1 ? 0 : cntr + 1;

                Tuple<float, float> adjustedAlong = GetAdjustedRatios(ends[cntr - 1], ends[cntr], ends[lastIndex], along);

                controls[cntr - 1] = GetControlPoints_Middle(ends[cntr - 1], ends[cntr], ends[lastIndex], adjustedAlong.Item1, adjustedAlong.Item2);
            }

            Tuple<float, float> adjustedAlong2 = GetAdjustedRatios(ends[ends.Length - 1], ends[0], ends[1], along);
            var extraControl = GetControlPoints_Middle(ends[ends.Length - 1], ends[0], ends[1], adjustedAlong2.Item1, adjustedAlong2.Item2);      // loop back

            // Build the return segments
            BezierSegment3D[] retVal = new BezierSegment3D[ends.Length];

            for (int cntr = 0; cntr < ends.Length; cntr++)
            {
                Vector3? ctrl0 = cntr == 0 ? extraControl.Item2 : controls[cntr - 1].Item2;
                Vector3? ctrl1 = cntr == ends.Length - 1 ? extraControl.Item1 : controls[cntr].Item1;

                int lastIndex = cntr == ends.Length - 1 ? 0 : cntr + 1;

                retVal[cntr] = new BezierSegment3D(cntr, lastIndex, UtilityCore.Iterate<Vector3>(ctrl0, ctrl1).ToArray(), ends);
            }

            return retVal;
        }
        private static BezierSegment3D[] GetBezierSegments_Open(Vector3[] ends, float along = .25f)
        {
            // Precalculate the control points
            Tuple<Vector3, Vector3>[] controls = new Tuple<Vector3, Vector3>[ends.Length - 2];

            for (int cntr = 1; cntr < ends.Length - 1; cntr++)
            {
                Tuple<float, float> adjustedAlong = GetAdjustedRatios(ends[cntr - 1], ends[cntr], ends[cntr + 1], along);

                controls[cntr - 1] = GetControlPoints_Middle(ends[cntr - 1], ends[cntr], ends[cntr + 1], adjustedAlong.Item1, adjustedAlong.Item2);
            }

            // Build the return segments
            BezierSegment3D[] retVal = new BezierSegment3D[ends.Length - 1];

            for (int cntr = 0; cntr < ends.Length - 1; cntr++)
            {
                Vector3? ctrl0 = cntr == 0 ? (Vector3?)null : controls[cntr - 1].Item2;
                Vector3? ctrl1 = cntr == ends.Length - 2 ? (Vector3?)null : controls[cntr].Item1;

                retVal[cntr] = new BezierSegment3D(cntr, cntr + 1, UtilityCore.Iterate<Vector3>(ctrl0, ctrl1).ToArray(), ends);
            }

            return retVal;
        }

        private static Vector3? GetControlPoints_Middle_ControlLine(Vector3 dir21, Vector3 dir23)
        {
            // Get the angle between the two directions
            float angle = Vector3.Angle(dir21, dir23);
            if (float.IsNaN(angle))
                return null;

            Vector3 axis = Vector3.Cross(dir21, dir23);
            if (axis.IsNearZero())
            {
                if (angle.IsNearValue(180))
                {
                    // The two lines are colinear.  Can't return null because the calling function will return arbitrary points which is wrong.  Come
                    // up with a random orth to one of the vectors so that the below portion of this function will choose accurate control points
                    axis = Math3D.GetArbitraryOrthonganal(dir21);
                }
                else
                {
                    return null;
                }
            }

            // Get the vector directly between the two directions
            Vector3 between = Quaternion.AngleAxis(angle / 2, axis) * dir21;

            // Now get the vector that is orthogonal to that between vector.  This is the line that
            // the control points will be along
            return Vector3.Cross(between, axis);        // length doesn't really matter for this.  It could also point in the exact opposite direction, and that wouldn't matter
        }

        private static Tuple<float, float> GetAdjustedRatios(Vector3 p1, Vector3 p2, Vector3 p3, float along)
        {
            float length12 = (p2 - p1).magnitude;
            float length23 = (p3 - p2).magnitude;

            // The shorter segment gets the full amount, and the longer segment gets an adjusted amount

            if (length12.IsNearValue(length23))
            {
                return Tuple.Create(along, along);
            }
            else if (length12 < length23)
            {
                return Tuple.Create(along, along * (length12 / length23));
            }
            else
            {
                return Tuple.Create(along * (length23 / length12), along);
            }
        }

        private static Vector3[][] GetVerticalSamples(BezierSegment3D[][] horizontals, int horzCount, int vertCount, float controlPointPercent)
        {
            // Get samples of the horizontals
            Vector3[][] horizontalPoints = horizontals.
                Select(o => GetPoints(horzCount, o)).
                ToArray();

            // Get samples of the verticals (these are the final points)
            return GetVerticalSamples(horizontalPoints, horzCount, vertCount, controlPointPercent);
        }
        private static Vector3[][] GetVerticalSamples(Vector3[][] horizontals, int horzCount, int vertCount, float controlPointPercent)
        {
            List<Vector3[]> retVal = new List<Vector3[]>();

            for (int h = 0; h < horzCount; h++)
            {
                // Get the points from each of the horizontal lines at this index
                Vector3[] samples = horizontals.
                    Select(o => o[h]).
                    ToArray();

                if (samples.Length == vertCount)
                {
                    // It would be rare that the number of vertical points requested is the same as the number of horizontal
                    // stripes passed in as control points.  It's more probable that the horizontal stripes are just a rough set
                    // of control points, and they are asking for a higher resolution of sample points within the mesh
                    retVal.Add(samples);
                }
                else
                {
                    // Turn those sample points into a vertical bezier
                    BezierSegment3D[] vertSegments = GetBezierSegments(samples, controlPointPercent);

                    Vector3[] vertLine = GetPoints(vertCount, vertSegments);

                    retVal.Add(vertLine);
                }
            }

            return retVal.ToArray();
        }

        private static (Vector2[] lengths, BezierMeshSamples_Distance[] lines) GetDistancesFromPoint(VectorInt2 center, Vector3[] points, int horzCount, int vertCount)
        {
            var lengths = new Vector2[points.Length];
            var lines = new List<BezierMeshSamples_Distance>();
            
            // Horizontal passes
            for (int y = 0; y < vertCount; y++)
            {
                lines.AddRange(DoPass_X_atY(new AxisFor(AxisDim.X, center.X, horzCount - 2), y, lengths, points, horzCount, vertCount));
                lines.AddRange(DoPass_X_atY(new AxisFor(AxisDim.X, center.X, 1), y, lengths, points, horzCount, vertCount));
            }

            // Vertical passes
            for (int x = 0; x < horzCount; x++)
            {
                lines.AddRange(DoPass_Y_atX(new AxisFor(AxisDim.Y, center.Y, vertCount - 2), x, lengths, points, horzCount, vertCount));
                lines.AddRange(DoPass_Y_atX(new AxisFor(AxisDim.Y, center.Y, 1), x, lengths, points, horzCount, vertCount));
            }

            return (lengths, lines.ToArray());
        }
        private static BezierMeshSamples_Distance[] DoPass_X_atY(AxisFor axisX, int y, Vector2[] lengths, Vector3[] points, int horzCount, int vertCount)
        {
            var retVal = new List<BezierMeshSamples_Distance>();

            foreach (int x in axisX.Iterate())
            {
                int index0 = (x * vertCount) + y;
                int index1 = ((x + axisX.Increment) * vertCount) + y;

                Vector3 line = points[index1] - points[index0];

                float lineLen = line.magnitude * (axisX.IsPos ? 1 : -1);      // lines going left should have a negative length
                lengths[index1].x = lengths[index0].x + lineLen;

                retVal.Add(new BezierMeshSamples_Distance()
                {
                    From = points[index0],
                    To = points[index1],
                    FromIndex = new VectorInt2(x, y),
                    ToIndex = new VectorInt2(x + axisX.Increment, y),
                    DistSegment = lineLen,
                    DistSum = lengths[index1].x,
                    IsHorizontal = true,
                });
            }

            return retVal.ToArray();
        }
        private static BezierMeshSamples_Distance[] DoPass_Y_atX(AxisFor axisY, int x, Vector2[] lengths, Vector3[] points, int horzCount, int vertCount)
        {
            var retVal = new List<BezierMeshSamples_Distance>();

            foreach (int y in axisY.Iterate())
            {
                int index0 = (x * vertCount) + y;
                int index1 = (x * vertCount) + y + axisY.Increment;

                Vector3 line = points[index1] - points[index0];

                float lineLen = line.magnitude * (axisY.IsPos ? 1 : -1);      // lines going toward 0 should have negative length
                lengths[index1].y = lengths[index0].y + lineLen;

                retVal.Add(new BezierMeshSamples_Distance()
                {
                    From = points[index0],
                    To = points[index1],
                    FromIndex = new VectorInt2(x, y),
                    ToIndex = new VectorInt2(x, y + axisY.Increment),
                    DistSegment = lineLen,
                    DistSum = lengths[index1].y,
                    IsHorizontal = false,
                });
            }

            return retVal.ToArray();
        }

        #endregion
    }

    //TODO: Make 1D and ND versions
    #region class: BezierSegment3D

    public class BezierSegment3D
    {
        #region Declaration Section

        private readonly object _lock = new object();

        #endregion

        #region Constructor

        public BezierSegment3D(Vector3 end0, Vector3 end1, Vector3[] controlPoints)
        : this(0, 1, controlPoints, new[] { end0, end1 }) { }

        public BezierSegment3D(int endIndex0, int endIndex1, Vector3[] controlPoints, Vector3[] allEndPoints)
        {
            EndIndex0 = endIndex0;
            EndIndex1 = endIndex1;
            ControlPoints = controlPoints;
            AllEndPoints = allEndPoints;

            Combined = UtilityCore.Iterate<Vector3>(EndPoint0, ControlPoints, EndPoint1).ToArray();
        }

        #endregion

        public readonly Vector3[] AllEndPoints;

        public readonly int EndIndex0;
        public readonly int EndIndex1;

        public Vector3 EndPoint0 => AllEndPoints[EndIndex0];
        public Vector3 EndPoint1 => AllEndPoints[EndIndex1];

        public readonly Vector3[] ControlPoints;

        /// <summary>
        /// This is { EndPoint0, ControlPoints, EndPoint1 }
        /// </summary>
        public readonly Vector3[] Combined;

        private float? _length_quick = null;
        /// <summary>
        /// This is a rough approximation of the length of the bezier.  It will likely be shorter than the actual length
        /// </summary>
        /// <remarks>
        /// Some suggestions on how to do it right:
        /// http://math.stackexchange.com/questions/12186/arc-length-of-b%C3%A9zier-curves
        /// http://www.carlosicaza.com/2012/08/12/an-more-efficient-way-of-calculating-the-length-of-a-bezier-curve-part-ii/
        /// </remarks>
        public float Length_quick
        {
            get
            {
                lock (_lock)
                {
                    if (_length_quick == null)
                    {
                        if (ControlPoints == null || ControlPoints.Length == 0)
                        {
                            _length_quick = (EndPoint1 - EndPoint0).magnitude;
                        }
                        else
                        {
                            float length = 0;

                            length += (ControlPoints[0] - EndPoint0).sqrMagnitude;
                            length += (ControlPoints[ControlPoints.Length - 1] - EndPoint1).sqrMagnitude;

                            for (int cntr = 0; cntr < ControlPoints.Length - 1; cntr++)
                            {
                                length += (ControlPoints[cntr] - ControlPoints[cntr + 1]).sqrMagnitude;
                            }

                            _length_quick = Mathf.Sqrt(length);
                        }
                    }

                    return _length_quick.Value;
                }
            }
        }

        #region Public Methods

        public BezierSegment3D ToReverse()
        {
            Vector3[] controlPoints = null;
            if (this.ControlPoints != null)
            {
                controlPoints = this.ControlPoints.Reverse().ToArray();
            }

            return new BezierSegment3D(this.EndIndex1, this.EndIndex0, controlPoints, this.AllEndPoints);      // no need to reverse AllEndPoints, just the indexes
        }

        #endregion
    }

    #endregion

    #region class: BezierMesh

    //TODO: This needs to use BezierSegment1D.  The x and y in the bezier objects are ignored, and is just wasted calculations

    /// <summary>
    /// This takes in a grid of points, and will then apply beziers to estimate locations between those control points
    /// </summary>
    public class BezierMesh
    {
        #region Constructor

        public BezierMesh(float[] axisX, float[] axisY, float[] valuesZ)
        {
            var result = ConvertToBezier(axisX, axisY, valuesZ);

            AxisX = result.Item1;
            Horizontal = result.Item2;

            AxisY = result.Item3;
            Vertical = result.Item4;
        }

        #endregion

        #region Public Properties

        public readonly float[] AxisX;
        public readonly float[] AxisY;

        public readonly BezierSegment3D[][] Horizontal;
        public readonly BezierSegment3D[][] Vertical;

        #endregion

        #region Public Methods

        public float EstimateValue(float x, float y)
        {
            var segX = FindSegment(x, AxisX);
            var segY = FindSegment(y, AxisY);

            if (new[] { segX.Item1, segX.Item2, segY.Item1, segY.Item2 }.Any(o => o < 0))
            {
                // Probably want to just assume linear.  Use the slope and length of the nearest segments
                throw new ApplicationException("TODO: Handle out of bounds requests");
            }

            Vector3 lerp = BicubicInterpolation(
                Horizontal[segY.Item1][segX.Item1],
                Horizontal[segY.Item2][segX.Item1],
                Vertical[segX.Item1][segY.Item1],
                Vertical[segX.Item2][segY.Item1],
                segX.Item3,
                segY.Item3);

            return lerp.z;
        }

        #endregion

        #region Private Methods

        private static Tuple<float[], BezierSegment3D[][], float[], BezierSegment3D[][]> ConvertToBezier(float[] axisX, float[] axisY, float[] valuesZ)
        {
            #region validate

            // X length
            if (axisX == null || axisX.Length < 2)
                throw new ArgumentException(string.Format("axisX must have at least 2 items: len={0}", axisX == null ? "null" : axisX.Length.ToString()));

            // Y length
            if (axisY == null || axisY.Length < 2)
                throw new ArgumentException(string.Format("axisY must have at least 2 items: len={0}", axisY == null ? "null" : axisY.Length.ToString()));

            // Z area
            if (valuesZ == null || valuesZ.Length != axisX.Length * axisY.Length)
                throw new ArgumentException(string.Format("valuesZ is invalid length: values={0}, axis1={1}, axis2={2}", valuesZ == null ? "null" : valuesZ.Length.ToString(), axisX.Length, axisY.Length));

            // X equality
            if (Enumerable.Range(0, axisX.Length - 1).Any(o => axisX[o].IsNearValue(axisX[o + 1])))
                throw new ArgumentException("Values can't be the same in x axis");

            // Y equality
            if (Enumerable.Range(0, axisY.Length - 1).Any(o => axisY[o].IsNearValue(axisY[o + 1])))
                throw new ArgumentException("Values can't be the same in y axis");

            #endregion

            bool isAccendingX = axisX[1] > axisX[0];
            bool isAccendingY = axisY[1] > axisY[0];

            #region validate

            // X ascending
            if (Enumerable.Range(0, axisX.Length - 1).Any(o => isAccendingX ? axisX[o + 1] < axisX[o] : axisX[o + 1] > axisX[o]))
                throw new ArgumentException("The values in axisX must all ascend or descend");

            // Y ascending
            if (Enumerable.Range(0, axisY.Length - 1).Any(o => isAccendingY ? axisY[o + 1] < axisY[o] : axisY[o + 1] > axisY[o]))
                throw new ArgumentException("The values in axisX must all ascend or descend");

            #endregion

            #region ensure ascending X

            if (!isAccendingX)
            {
                axisX = axisX.Reverse().ToArray();

                float[] newZ = new float[valuesZ.Length];

                for (int oldX = 0; oldX < axisX.Length; oldX++)
                {
                    int newX = axisX.Length - 1 - oldX;

                    for (int y = 0; y < axisY.Length; y++)
                    {
                        int yIndex = y * axisX.Length;
                        newZ[yIndex + newX] = valuesZ[yIndex + oldX];
                    }
                }

                valuesZ = newZ;
            }

            #endregion
            #region ensure ascending Y

            if (!isAccendingY)
            {
                axisY = axisY.Reverse().ToArray();

                float[] newZ = new float[valuesZ.Length];

                for (int oldY = 0; oldY < axisY.Length; oldY++)
                {
                    int newY = axisY.Length - 1 - oldY;

                    int yIndexOld = oldY * axisX.Length;
                    int yIndexNew = newY * axisX.Length;

                    for (int x = 0; x < axisX.Length; x++)
                    {
                        newZ[yIndexNew + x] = valuesZ[yIndexOld + x];
                    }
                }

                valuesZ = newZ;
            }

            #endregion

            BezierSegment3D[][] horizontal = new BezierSegment3D[axisY.Length][];     // there is a horizontal set for each y
            BezierSegment3D[][] vertical = new BezierSegment3D[axisX.Length][];

            //TODO: Make an option of this that figures out the percent to use:
            //if you pass in .25, that is the max.
            //  if both segments are equal length then it will be .25 for each
            //  if they are different lengths, the smaller will use .25.  the larger will use the ratio of lengths*.25
            //BezierSegmentDef.GetBezierSegments();

            #region horizontal

            for (int y = 0; y < axisY.Length; y++)
            {
                int yIndex = y * axisX.Length;

                Vector3[] ends = Enumerable.Range(0, axisX.Length).
                    Select(x => new Vector3(axisX[x], axisY[y], valuesZ[yIndex + x])).
                    ToArray();

                horizontal[y] = BezierUtil.GetBezierSegments(ends);
            }

            #endregion
            #region vertical

            for (int x = 0; x < axisX.Length; x++)
            {
                Vector3[] ends = Enumerable.Range(0, axisY.Length).
                    Select(y => new Vector3(axisX[x], axisY[y], valuesZ[(y * axisX.Length) + x])).
                    ToArray();

                vertical[x] = BezierUtil.GetBezierSegments(ends);
            }

            #endregion

            return Tuple.Create(axisX, horizontal, axisY, vertical);
        }

        private static Tuple<int, int, float> FindSegment(float value, float[] axis)
        {
            // Find the two points that straddle the value
            for (int cntr = 0; cntr < axis.Length - 1; cntr++)
            {
                if (value >= axis[cntr] && value <= axis[cntr + 1])
                {
                    float percent = (value - axis[cntr]) / (axis[cntr + 1] - axis[cntr]);
                    return Tuple.Create(cntr, cntr + 1, percent);
                }
            }

            // Out of bounds
            if (value < axis[0])
            {
                return Tuple.Create(-1, 0, 0f);
            }
            else //if (value > axis[axis.Length - 1])
            {
                return Tuple.Create(axis.Length - 1, -1, 0f);
            }
        }

        #endregion

        //TODO: Put this in Math3D
        public static Vector3 BicubicInterpolation(BezierSegment3D top, BezierSegment3D bottom, BezierSegment3D left, BezierSegment3D right, float percentX, float percentY)
        {
            Vector3 valueTop = BezierUtil.GetPoint(percentX, top);
            Vector3 valueBottom = BezierUtil.GetPoint(percentX, bottom);

            Vector3 valueLeft = BezierUtil.GetPoint(percentY, left);
            Vector3 valueRight = BezierUtil.GetPoint(percentY, right);

            var points = new[]
                {
                    (valueTop, 1 - percentY),
                    (valueBottom, percentY),
                    (valueLeft, 1 - percentX),
                    (valueRight, percentX),
                };

            return Math3D.GetCenter(points);
        }
    }

    #endregion
}
