using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static UnityEngine.RectTransform;

namespace PerfectlyNormalBaS
{
    public static class Math3D
    {
        #region misc

        public static (Vector3 min, Vector3 max) GetAABB(IEnumerable<Vector3> points)
        {
            bool foundOne = false;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            foreach (Vector3 point in points)
            {
                foundOne = true;        // it's too expensive to look at points.Count()

                if (point.x < minX)
                {
                    minX = point.x;
                }

                if (point.y < minY)
                {
                    minY = point.y;
                }

                if (point.z < minZ)
                {
                    minZ = point.z;
                }

                if (point.x > maxX)
                {
                    maxX = point.x;
                }

                if (point.y > maxY)
                {
                    maxY = point.y;
                }

                if (point.z > maxZ)
                {
                    maxZ = point.z;
                }
            }

            if (!foundOne)
            {
                // There were no points passed in
                //TODO: May want an exception
                return (new Vector3(0, 0, 0), new Vector3(0, 0, 0));
            }

            return (new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
        }

        /// <summary>
        /// This returns the location of the point relative to the triangle
        /// </summary>
        /// <remarks>
        /// The term Barycentric for a triangle seems to be 3 positions, so I'm not sure if this method is named right
        /// 
        /// This is useful if you want to store a point's location relative to a triangle when that triangle will move all
        /// around.  You don't need to know the transform used to move that triangle, just the triangle's final position
        /// 
        /// This is also useful to see if the point is inside the triangle.  If x or y is negative, or they add up to > 1, then it
        /// is outside the triangle:
        ///		if x is zero, it's on the 0_1 edge
        ///		if y is zero, it's on the 0_2 edge
        ///		if x+y is one, it's on the 1_2 edge
        /// 
        /// Got this here (good flash visualization too) (the website's still there, even though flash is long gone):
        /// http://www.blackpawn.com/texts/pointinpoly/default.html
        /// </remarks>
        /// <returns>
        /// X = % along the line triangle.P0 to triangle.P1
        /// Y = % along the line triangle.P0 to triangle.P2
        /// </returns>
        public static Vector2 ToBarycentric(ITriangle triangle, Vector3 point)
        {
            return ToBarycentric(triangle.Point0, triangle.Point1, triangle.Point2, point);
        }
        public static Vector2 ToBarycentric(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 testPoint)
        {
            // Compute vectors        
            Vector3 v0 = p2 - p0;
            Vector3 v1 = p1 - p0;
            Vector3 v2 = testPoint - p0;

            // Compute dot products
            float dot00 = Vector3.Dot(v0, v0);
            float dot01 = Vector3.Dot(v0, v1);
            float dot02 = Vector3.Dot(v0, v2);
            float dot11 = Vector3.Dot(v1, v1);
            float dot12 = Vector3.Dot(v1, v2);

            // Compute barycentric coordinates
            float invDenom = 1f / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            return new Vector2(u, v);
        }
        /// <summary>
        /// This projects the barycentric back into cartesian coords
        /// </summary>
        /// <param name="bary">Save me Bary!!! (Misfits)</param>
        public static Vector3 FromBarycentric(ITriangle triangle, Vector2 bary)
        {
            return FromBarycentric(triangle.Point0, triangle.Point1, triangle.Point2, bary);
        }
        public static Vector3 FromBarycentric(Vector3 p0, Vector3 p1, Vector3 p2, Vector2 bary)
        {
            //Vector3 line01 = p1 - p0;
            //Vector3 line02 = p2 - p0;
            Vector3 line01 = p2 - p0;		// ToBarycentric has x as p2
            Vector3 line02 = p1 - p0;

            return p0 + (line01 * bary.x) + (line02 * bary.y);
        }

        /// <summary>
        /// This returns the index into the list or -1
        /// NOTE: This uses IsNearValue
        /// </summary>
        public static int IndexOf(IEnumerable<Vector3> points, Vector3 findPoint)
        {
            int index = 0;
            foreach (Vector3 candidate in points)
            {
                if (candidate.IsNearValue(findPoint))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        /// <summary>
        /// This is more exact than the plane overload
        /// </summary>
        /// <remarks>
        /// from1 and 2 define one plane, to1 and 2 define another plane
        /// 
        /// It is expected that to is a rotated version of from
        /// (at least from1 and to1.  from2 and to2 are only used to get the plane level rotation.)
        /// In other words, once the plane rotation is calculated, there is a secondary rotation that maps from1 to to1
        /// 
        /// from2 doesn't need to be orthogonal to from1, just not parallel
        /// </remarks>
        public static Quaternion GetRotation(DoubleVector from, DoubleVector to)
        {
            const float ANGLEMIN = .05f;
            const float ANGLEMAX = .1f;

            // Calculate normals
            Vector3 fromNormalUnit = Vector3.Cross(from.Standard, from.Orth).normalized;
            Vector3 toNormalUnit = Vector3.Cross(to.Standard, to.Orth).normalized;

            if (fromNormalUnit.IsInvalid() || toNormalUnit.IsInvalid())     // if the normals are invalid, then the two directions are colinear (or one is zero length, or invalid)
            {
                //TODO: May want to throw an exception instead
                return Quaternion.identity;
            }

            // Detect Parallel
            if (Math.Abs(Vector3.Dot(fromNormalUnit, toNormalUnit)).IsNearValue(1f))
            {
                return Quaternion.FromToRotation(from.Standard, to.Standard);
            }

            // Figure out how to rotate the planes onto each other
            Quaternion planeRotation = Quaternion.FromToRotation(fromNormalUnit, toNormalUnit);

            planeRotation.ToAngleAxis(out float planeRotationAngle, out _);

            if (planeRotationAngle.IsNearValue(90) || planeRotationAngle.IsNearValue(180))
            {
                // Quaternion.Multiply fails with 90s and 180s.  Randomize things slightly and try again
                //NOTE: It's not just Quaternion.Multiply.  A group transform with two rotate transforms fails the same way
                //Here is a test case: from1=(-1, 0, 0), from2=(0, 0, 1), to1=(1, 0, 0), to2=(0, 1, 0)
                return GetRotation
                (
                    new DoubleVector
                    (
                        GetRandomVector_Cone(from.Standard, ANGLEMIN, ANGLEMAX, 1, 1),
                        GetRandomVector_Cone(from.Orth, ANGLEMIN, ANGLEMAX, 1, 1)
                    ),
                    new DoubleVector
                    (
                        GetRandomVector_Cone(to.Standard, ANGLEMIN, ANGLEMAX, 1, 1),
                        GetRandomVector_Cone(to.Orth, ANGLEMIN, ANGLEMAX, 1, 1)
                    )
                );
            }

            // Rotate from onto to
            Vector3 rotated1 = planeRotation * from.Standard;

            // Now that they are in the same plane, rotate the vectors onto each other
            Quaternion secondRotation = Quaternion.FromToRotation(rotated1, to.Standard);

            secondRotation.ToAngleAxis(out float secondRotationAngle, out _);

            if (secondRotationAngle.IsNearValue(90) || secondRotationAngle.IsNearValue(180))
            {
                // Quaternion.Multiply fails with 90s and 180s.  Randomize things slightly and try again
                return GetRotation
                (
                    new DoubleVector
                    (
                        GetRandomVector_Cone(from.Standard, ANGLEMIN, ANGLEMAX, 1, 1),
                        GetRandomVector_Cone(from.Orth, ANGLEMIN, ANGLEMAX, 1, 1)
                    ),
                    new DoubleVector
                    (
                        GetRandomVector_Cone(to.Standard, ANGLEMIN, ANGLEMAX, 1, 1),
                        GetRandomVector_Cone(to.Orth, ANGLEMIN, ANGLEMAX, 1, 1)
                    )
                );
            }

            // Just to be safe
            planeRotation.Normalize();
            secondRotation.Normalize();

            // Combine the two rotations
            return secondRotation * planeRotation;		// note that order is important (stand, orth is wrong)
        }
        /// <summary>
        /// When you take from times this return, you get to
        /// </summary>
        public static Quaternion GetRotation(Quaternion from, Quaternion to)
        {
            return Quaternion.Inverse(from) * to;
        }

        public static Quaternion GetMirroredRotation(Quaternion quat, AxisDim normal)
        {
            //https://stackoverflow.com/questions/32438252/efficient-way-to-apply-mirror-effect-on-quaternion-rotation

            if (quat.IsNearValue(Quaternion.identity))
                return quat;

            quat.ToAngleAxis(out float angle, out Vector3 axis);

            switch (normal)
            {
                case AxisDim.X:
                    //return Quaternion.AngleAxis(angle, new Vector3(axis.x, -axis.y, -axis.z));        // this is equivalent to the statement with negative angle (they both produce the same quaternion)
                    return Quaternion.AngleAxis(-angle, new Vector3(-axis.x, axis.y, axis.z));

                case AxisDim.Y:
                    //return Quaternion.AngleAxis(angle, new Vector3(-axis.x, axis.y, -axis.z));
                    return Quaternion.AngleAxis(-angle, new Vector3(axis.x, -axis.y, axis.z));

                case AxisDim.Z:
                    //return Quaternion.AngleAxis(angle, new Vector3(-axis.x, -axis.y, axis.z));
                    return Quaternion.AngleAxis(-angle, new Vector3(axis.x, axis.y, -axis.z));

                default:
                    throw new ApplicationException($"Unknown AxisDim: {normal}");
            }
        }

        /// <summary>
        /// This returns the center of position of the points
        /// NOTE: This is identical to GetAverage, just have two names depending on how the vectors are thought of (points vs vectors)
        /// </summary>
        public static Vector3 GetCenter(IEnumerable<Vector3> points)
        {
            if (points == null)
            {
                return new Vector3(0, 0, 0);
            }

            float x = 0f;
            float y = 0f;
            float z = 0f;

            int length = 0;

            foreach (Vector3 point in points)
            {
                x += point.x;
                y += point.y;
                z += point.z;

                length++;
            }

            if (length == 0)
            {
                return new Vector3(0, 0, 0);
            }

            float oneOverLen = 1f / (float)length;

            return new Vector3(x * oneOverLen, y * oneOverLen, z * oneOverLen);
        }
        public static Vector3 GetCenter(params Vector3[] points)
        {
            return GetCenter((IEnumerable<Vector3>)points);
        }
        /// <summary>
        /// This returns the center of mass of the points
        /// </summary>
        public static Vector3 GetCenter(params (Vector3 position, float weight)[] pointsMasses)
        {
            if (pointsMasses == null || pointsMasses.Length == 0)
                return new Vector3(0, 0, 0);

            float totalMass = pointsMasses.Sum(o => o.weight);
            if (totalMass.IsNearZero())
                return GetCenter(pointsMasses.Select(o => o.position).ToArray());

            float x = 0;
            float y = 0;
            float z = 0;

            foreach (var pointMass in pointsMasses)
            {
                x += pointMass.position.x * pointMass.weight;
                y += pointMass.position.y * pointMass.weight;
                z += pointMass.position.z * pointMass.weight;
            }

            float totalMassInverse = 1f / totalMass;

            return new Vector3(x * totalMassInverse, y * totalMassInverse, z * totalMassInverse);
        }
        /// <summary>
        /// NOTE: This is identical to GetCenter, just have two names depending on how the vectors are thought of (points vs vectors)
        /// </summary>
        public static Vector3 GetAverage(IEnumerable<Vector3> vectors)
        {
            return GetCenter(vectors);
        }
        public static Vector3 GetAverage(params Vector3[] vectors)
        {
            return GetCenter((IEnumerable<Vector3>)vectors);
        }
        public static Vector3 GetAverage(params (Vector3 position, float weight)[] pointsMasses)
        {
            return GetCenter(pointsMasses);
        }

        public static Vector3 LERP(Vector3 a, Vector3 b, float percent)
        {
            return new Vector3(
                a.x + (b.x - a.x) * percent,
                a.y + (b.y - a.y) * percent,
                a.z + (b.z - a.z) * percent);
        }

        #endregion

        #region intersections

        /// <summary>
        /// This returns true if any part of the sphere intersects any part of the AABB
        /// (also returns true if one is inside the other)
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://stackoverflow.com/questions/4578967/cube-sphere-intersection-test
        /// 
        /// Which referenced:
        /// http://www.ics.uci.edu/~arvo/code/BoxSphereIntersect.c
        /// </remarks>
        public static bool IsIntersecting_AABB_Sphere(Vector3 min, Vector3 max, Vector3 center, float radius)
        {
            float r2 = radius * radius;
            float dmin = 0f;

            if (center.x < min.x)
            {
                dmin += (center.x - min.x) * (center.x - min.x);
            }
            else if (center.x > max.x)
            {
                dmin += (center.x - max.x) * (center.x - max.x);
            }

            if (center.y < min.y)
            {
                dmin += (center.y - min.y) * (center.y - min.y);
            }
            else if (center.y > max.y)
            {
                dmin += (center.y - max.y) * (center.y - max.y);
            }

            if (center.z < min.z)
            {
                dmin += (center.z - min.z) * (center.z - min.z);
            }
            else if (center.z > max.z)
            {
                dmin += (center.z - max.z) * (center.z - max.z);
            }

            return dmin <= r2;
        }
        /// <summary>
        /// This returns true if any part of AABB1 intersects any part of the AABB2
        /// (also returns true if one is inside the other)
        /// </summary>
        public static bool IsIntersecting_AABB_AABB(Vector3 min1, Vector3 max1, Vector3 min2, Vector3 max2)
        {
            if (min1.x > max2.x || min2.x > max1.x)
            {
                return false;
            }
            else if (min1.y > max2.y || min2.y > max1.y)
            {
                return false;
            }
            else if (min1.z > max2.z || min2.z > max1.z)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// This returns whether the point is inside all the planes (the triangles don't define finite triangles, but whole planes)
        /// NOTE: Make sure the normals point outward, or there will be odd results
        /// </summary>
        /// <remarks>
        /// This is a reworked copy of QuickHull3D.GetOutsideSet, which was inspired by:
        /// http://www.gamedev.net/topic/106765-determining-if-a-point-is-in-front-of-or-behind-a-plane/
        /// </remarks>
        public static bool IsInside_Planes(IEnumerable<Plane> planes, Vector3 testPoint)
        {
            foreach (Plane plane in planes)
            {
                if (plane.GetSide(testPoint))        // this needs the normals to point out
                {
                    return false;
                }
            }

            return true;
        }
        public static bool IsInside_AABB(Vector3 min, Vector3 max, Vector3 testPoint)
        {
            if (testPoint.x < min.x)
            {
                return false;
            }
            else if (testPoint.x > max.x)
            {
                return false;
            }
            else if (testPoint.y < min.y)
            {
                return false;
            }
            else if (testPoint.y > max.y)
            {
                return false;
            }
            else if (testPoint.z < min.z)
            {
                return false;
            }
            else if (testPoint.z > max.z)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// This returns a point along the line that is the shortest distance to the test point
        /// NOTE:  The line passed in is assumed to be infinite, not a line segment
        /// </summary>
        /// <param name="line">Any arbitrary point along the line and direction of the line</param>
        /// <param name="testPoint">The point that is not on the line</param>
        public static Vector3 GetClosestPoint_Line_Point(Ray line, Vector3 testPoint)
        {
            Vector3 dirToPoint = testPoint - line.origin;

            float dot1 = Vector3.Dot(dirToPoint, line.direction);
            float dot2 = Vector3.Dot(line.direction, line.direction);
            float ratio = dot1 / dot2;

            Vector3 retVal = line.origin + (ratio * line.direction);

            return retVal;
        }
        /// <summary>
        /// This is a wrapper to GetNearestPointAlongLine that just returns the distance
        /// </summary>
        public static float GetClosestDistance_Line_Point(Ray line, Vector3 testPoint)
        {
            return (testPoint - GetClosestPoint_Line_Point(line, testPoint)).magnitude;
        }

        public static Vector3 GetClosestPoint_Plane_Point(Plane plane, Vector3 testPoint)
        {
            Vector3? retVal = GetIntersection_Plane_Line(plane, new Ray(testPoint, plane.normal));
            if (retVal == null)
            {
                throw new ApplicationException("Intersection between a plane and its normal should never be null");
            }

            return retVal.Value;
        }

        public static Vector3 GetClosestPoint_Triangle_Point(ITriangle triangle, Vector3 testPoint)
        {
            Vector3 pointOnPlane = GetClosestPoint_Plane_Point(new Plane(triangle.Point0, triangle.Point1, triangle.Point2), testPoint);

            Vector2 bary = ToBarycentric(triangle, pointOnPlane);

            if (bary.x >= 0 && bary.y >= 0 && bary.x + bary.y <= 1)
            {
                // It's inside the triangle
                return pointOnPlane;
            }

            // Cap to one of the edges
            if (bary.x < 0)
            {
                return GetClosestPoint_LineSegment_Point(triangle.Point0, triangle.Point1, testPoint);      // see the comments in ToBarycentric for how to know which points to use
            }
            else if (bary.y < 0)
            {
                return GetClosestPoint_LineSegment_Point(triangle.Point0, triangle.Point2, testPoint);
            }
            else
            {
                return GetClosestPoint_LineSegment_Point(triangle.Point1, triangle.Point2, testPoint);
            }
        }
        public static float GetClosestDistance_Triangle_Point(ITriangle triangle, Vector3 testPoint)
        {
            return (testPoint - GetClosestPoint_Triangle_Point(triangle, testPoint)).magnitude;
        }

        /// <summary>
        /// This returns the distance beween two skew lines at their closest point
        /// </summary>
        /// <remarks>
        /// http://2000clicks.com/mathhelp/GeometryPointsAndLines3D.aspx
        /// </remarks>
        public static float GetClosestDistance_Line_Line(Ray line1, Ray line2)
        {
            //g = (a-c) · (b×d) / |b×d|
            //dist = (a - c) dot (b cross d).ToUnit
            //dist = (point1 - point2) dot (dir1 cross dir2).ToUnit

            //TODO: Detect if they are parallel and return the distance

            Vector3 cross1_2 = Vector3.Cross(line1.direction, line2.direction).normalized;
            Vector3 sub1_2 = line1.origin - line2.origin;

            float retVal = Vector3.Dot(sub1_2, cross1_2);

            return retVal;
        }

        /// <summary>
        /// Calculates the intersection line segment between 2 lines (not segments).
        /// Returns false if no solution can be found.
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://local.wasp.uwa.edu.au/~pbourke/geometry/lineline3d/calclineline.cs
        /// 
        /// Which was ported from the C algorithm of Paul Bourke:
        /// http://local.wasp.uwa.edu.au/~pbourke/geometry/lineline3d/
        /// </remarks>
        public static bool GetClosestPoints_Line_Line(out Vector3? resultPoint1, out Vector3? resultPoint2, Ray line1, Ray line2)
        {
            return GetClosestPoints_Line_Line(out resultPoint1, out resultPoint2, line1.origin, line1.origin + line1.direction, line2.origin, line2.origin + line2.direction);
        }
        public static bool GetClosestPoints_Line_Line(out Vector3? resultPoint1, out Vector3? resultPoint2, Vector3 line1Point1, Vector3 line1Point2, Vector3 line2Point1, Vector3 line2Point2)
        {
            resultPoint1 = null;
            resultPoint2 = null;

            Vector3 p1 = line1Point1;
            Vector3 p2 = line1Point2;
            Vector3 p3 = line2Point1;
            Vector3 p4 = line2Point2;
            Vector3 p13 = p1 - p3;
            Vector3 p43 = p4 - p3;

            //if (IsNearZero(p43.LengthSquared))
            //{
            //    return false;
            //}

            Vector3 p21 = p2 - p1;
            //if (IsNearZero(p21.LengthSquared))
            //{
            //    return false;
            //}

            float d1343 = (p13.x * p43.x) + (p13.y * p43.y) + (p13.z * p43.z);
            float d4321 = (p43.x * p21.x) + (p43.y * p21.y) + (p43.z * p21.z);
            float d1321 = (p13.x * p21.x) + (p13.y * p21.y) + (p13.z * p21.z);
            float d4343 = (p43.x * p43.x) + (p43.y * p43.y) + (p43.z * p43.z);
            float d2121 = (p21.x * p21.x) + (p21.y * p21.y) + (p21.z * p21.z);

            float denom = (d2121 * d4343) - (d4321 * d4321);
            //if (IsNearZero(denom))
            //{
            //    return false;
            //}
            float numer = (d1343 * d4321) - (d1321 * d4343);

            float mua = numer / denom;
            if (float.IsNaN(mua))
            {
                return false;
            }

            float mub = (d1343 + d4321 * (mua)) / d4343;

            resultPoint1 = new Vector3(p1.x + mua * p21.x, p1.y + mua * p21.y, p1.z + mua * p21.z);
            resultPoint2 = new Vector3(p3.x + mub * p43.x, p3.y + mub * p43.y, p3.z + mub * p43.z);

            if (float.IsNaN(resultPoint1.Value.x) || float.IsNaN(resultPoint1.Value.y) || float.IsNaN(resultPoint1.Value.z) ||
                float.IsNaN(resultPoint2.Value.x) || float.IsNaN(resultPoint2.Value.y) || float.IsNaN(resultPoint2.Value.z))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public static bool GetClosestPoints_Line_LineSegment(out Vector3[] resultPointsLine, out Vector3[] resultPointsLineSegment, Ray line, Vector3 lineSegmentStart, Vector3 lineSegmentStop)
        {
            if (!GetClosestPoints_Line_Line(out Vector3? result1, out Vector3? result2, line.origin, line.origin + line.direction, lineSegmentStart, lineSegmentStop))
            {
                // If line/line fails, it's because they are parallel.  If the lines coincide, then return the segment start and stop


                //TODO: Finish this



                resultPointsLine = new Vector3[0];
                resultPointsLineSegment = new Vector3[0];
                return false;
            }

            // Make sure the line segment result isn't beyond the line segment
            Vector3 segmentDirLen = lineSegmentStop - lineSegmentStart;
            Vector3 resultDirLen = result2.Value - lineSegmentStart;

            if (!Vector3.Dot(segmentDirLen.normalized, resultDirLen.normalized).IsNearValue(1f))
            {
                // It's the other direction (beyond segment start)
                resultPointsLine = new Vector3[0];
                resultPointsLineSegment = new Vector3[0];
                return false;
            }

            if (resultDirLen.sqrMagnitude > segmentDirLen.sqrMagnitude)
            {
                // It's beyond segment stop
                resultPointsLine = new Vector3[0];
                resultPointsLineSegment = new Vector3[0];
                return false;
            }

            // Return single points (this is the standard flow)
            resultPointsLine = new Vector3[] { result1.Value };
            resultPointsLineSegment = new Vector3[] { result2.Value };
            return true;
        }

        public static Vector3 GetClosestPoint_LineSegment_Point(Vector3 segmentStart, Vector3 segmentStop, Vector3 testPoint)
        {
            return GetClosestPoint_LineSegment_Point_verbose(segmentStart, segmentStop, testPoint).point;
        }
        public static (Vector3 point, LocationOnLineSegment where) GetClosestPoint_LineSegment_Point_verbose(Vector3 segmentStart, Vector3 segmentStop, Vector3 testPoint)
        {
            Vector3 lineDir = segmentStop - segmentStart;

            Vector3 retVal = GetClosestPoint_Line_Point(new Ray(segmentStart, lineDir), testPoint);

            Vector3 returnDir = retVal - segmentStart;

            if (Vector3.Dot(lineDir, returnDir) < 0)
            {
                // It's going in the wrong direction, so start point is the closest
                return (segmentStart, LocationOnLineSegment.Start);
            }
            else if (returnDir.sqrMagnitude > lineDir.sqrMagnitude)
            {
                // It's past the segment stop
                return (segmentStop, LocationOnLineSegment.Stop);
            }
            else
            {
                // The return point is sitting somewhere on the line segment
                return (retVal, LocationOnLineSegment.Middle);
            }
        }

        public static Vector3? GetClosestPoint_Circle_Point(Plane circlePlane, Vector3 circleCenter, float circleRadius, Vector3 testPoint)
        {
            // Project the test point onto the circle's plane
            Vector3 planePoint = GetClosestPoint_Plane_Point(circlePlane, testPoint);

            if (planePoint.IsNearValue(circleCenter))
            {
                // The test point is directly over the center of the circle (or is the center of the circle)
                return null;
            }

            // Get the line from the circle's center to that point
            Vector3 line = planePoint - circleCenter;

            // Project out to the length of the circle
            return circleCenter + (line.normalized * circleRadius);
        }

        public static Vector3? GetClosestPoint_Cylinder_Point(Ray axis, float radius, Vector3 testPoint)
        {
            // Get the shortest point between the cylinder's axis and the test point
            Vector3 nearestAxisPoint = GetClosestPoint_Line_Point(axis, testPoint);

            // Get the line from that point to the test point
            Vector3 line = testPoint - nearestAxisPoint;

            if (line.IsNearZero())
            {
                // The test point is sitting on the axis
                return null;
            }

            // Project out to the radius of the cylinder
            return nearestAxisPoint + (line.normalized * radius);
        }

        public static Vector3? GetClosestPoint_Sphere_Point(Vector3 centerPoint, float radius, Vector3 testPoint)
        {
            if (centerPoint.IsNearValue(testPoint))
            {
                // The test point is the center of the sphere
                return null;
            }

            // Get the line from the center to the test point
            Vector3 line = testPoint - centerPoint;

            // Project out to the radius of the sphere
            return centerPoint + (line.normalized * radius);
        }

        /// <summary>
        /// This will figure out the nearest points between a circle and line
        /// </summary>
        /// <remarks>
        /// circlePoints will be the nearest point on the circle to the line, and linePoints will hold the closest point on the line to
        /// the corresponding element of circlePoints
        /// 
        /// The only time false is returned is if the line is perpendicular to the circle and goes through the center of the circle (in
        /// that case, linePoints will hold the circle's center point
        /// 
        /// Most of the time, only one output point will be returned, but there are some cases where two are returned
        /// 
        /// If onlyReturnSinglePoint is true, then the arrays will never be larger than one (the point in linePoints that is closest to
        /// pointOnLine is chosen)
        /// </remarks>
        public static bool GetClosestPoints_Circle_Line(out Vector3[] circlePoints, out Vector3[] linePoints, Plane circlePlane, Vector3 circleCenter, float circleRadius, Ray line, RayCastReturn returnWhich)
        {
            // There are too many loose variables, so package them up
            CircleLineArgs args = new CircleLineArgs()
            {
                CirclePlane = circlePlane,
                CircleCenter = circleCenter,
                CircleRadius = circleRadius,
                Line = line,
            };

            // Call the overload
            bool retVal = GetClosestPointsBetweenLineCircle(out circlePoints, out linePoints, args);
            if (returnWhich == RayCastReturn.AllPoints || !retVal || circlePoints.Length == 1)
            {
                return retVal;
            }

            switch (returnWhich)
            {
                case RayCastReturn.ClosestToRay:
                    GetClosestPointsBetweenLineCircle_Closest_CircleLine(ref circlePoints, ref linePoints, line.origin);
                    break;

                case RayCastReturn.ClosestToRayOrigin:
                    GetClosestPointsBetweenLineCircle_Closest_RayOrigin(ref circlePoints, ref linePoints, line.origin);
                    break;

                default:
                    throw new ApplicationException("Unexpected RayCastReturn: " + returnWhich.ToString());
            }

            return true;
        }

        public static bool GetClosestPoints_Cylinder_Line(out Vector3[] cylinderPoints, out Vector3[] linePoints, Ray axis, float radius, Ray line, RayCastReturn returnWhich)
        {
            // Get the shortest point between the cylinder's axis and the line
            if (!GetClosestPoints_Line_Line(out Vector3? nearestAxisPoint, out Vector3? nearestLinePoint, axis, line))
            {
                // The axis and line are parallel
                cylinderPoints = null;
                linePoints = null;
                return false;
            }

            Vector3 nearestLine = nearestLinePoint.Value - nearestAxisPoint.Value;
            float nearestDistance = nearestLine.magnitude;

            if (nearestDistance >= radius)
            {
                // Sitting outside the cylinder, so just project the line to the cylinder wall
                cylinderPoints = new Vector3[] { nearestAxisPoint.Value + (nearestLine.normalized * radius) };
                linePoints = new Vector3[] { nearestLinePoint.Value };
                return true;
            }

            // The rest of this function is for a line intersect inside the cylinder (there's always two intersect points)

            // Make a plane that the circle sits in (this is used by code shared with the circle/line intersect)
            //NOTE: The plane is using nearestAxisPoint, and not the arbitrary point that was passed in (this makes later logic easier)
            Vector3 circlePlaneLine1 = nearestDistance.IsNearZero() ?
                GetArbitraryOrthonganal(axis.direction) :
                nearestLine;
            Vector3 circlePlaneLine2 = Vector3.Cross(axis.direction, circlePlaneLine1);
            Plane circlePlane = new Plane(nearestAxisPoint.Value, nearestAxisPoint.Value + circlePlaneLine1, nearestAxisPoint.Value + circlePlaneLine2);

            CircleLineArgs args = new CircleLineArgs()
            {
                CircleCenter = nearestAxisPoint.Value,
                CirclePlane = circlePlane,
                CircleRadius = radius,
                Line = line,
            };

            CirclePlaneIntersectProps intersectArgs = GetClosestPointsBetweenLineCylinder_PlaneIntersect(args, nearestLinePoint.Value, nearestLine, nearestDistance);

            GetClosestPointsBetweenLineCylinder_Finish(out cylinderPoints, out linePoints, args, intersectArgs);

            switch (returnWhich)
            {
                case RayCastReturn.AllPoints:
                    // Nothing more to do
                    break;

                case RayCastReturn.ClosestToRay:
                    GetClosestPointsBetweenLineCircle_Closest_CircleLine(ref cylinderPoints, ref linePoints, line.origin);
                    break;

                case RayCastReturn.ClosestToRayOrigin:
                    GetClosestPointsBetweenLineCircle_Closest_RayOrigin(ref cylinderPoints, ref linePoints, line.origin);
                    break;

                default:
                    throw new ApplicationException("Unknown RayCastReturn: " + returnWhich.ToString());
            }

            return true;
        }

        public static void GetClosestPoints_Sphere_Line(out Vector3[] spherePoints, out Vector3[] linePoints, Vector3 centerPoint, float radius, Ray line, RayCastReturn returnWhich)
        {
            // Get the shortest point between the sphere's center and the line
            Vector3 nearestLinePoint = GetClosestPoint_Line_Point(line, centerPoint);

            Vector3 nearestLine = nearestLinePoint - centerPoint;
            float nearestDistance = nearestLine.magnitude;

            if (nearestDistance >= radius)
            {
                // Sitting outside the sphere, so just project the line to the sphere wall
                spherePoints = new Vector3[] { centerPoint + (nearestLine.normalized * radius) };
                linePoints = new Vector3[] { nearestLinePoint };
                return;
            }

            // The rest of this function is for a line intersect inside the sphere (there's always two intersect points)

            // Make a plane that the circle sits in (this is used by code shared with the circle/line intersect)
            //NOTE: The plane is oriented along the shortest path line
            Vector3 circlePlaneLine1 = nearestDistance.IsNearZero() ? new Vector3(1, 0, 0) : nearestLine;
            Vector3 circlePlaneLine2 = line.direction;
            Plane circlePlane = new Plane(centerPoint, centerPoint + circlePlaneLine1, centerPoint + circlePlaneLine2);

            CircleLineArgs args = new CircleLineArgs()
            {
                CircleCenter = centerPoint,
                CirclePlane = circlePlane,
                CircleRadius = radius,
                Line = line,
            };

            CirclePlaneIntersectProps intersectArgs = new CirclePlaneIntersectProps()
            {
                Line = line,
                NearestToCenter = nearestLinePoint,
                CenterToNearest = nearestLine,
                CenterToNearestLength = nearestDistance,
                IsInsideCircle = true
            };

            // Get the circle intersects (since the line is on the circle's plane, this is the final answer)
            GetClosestPointsBetweenLineCircle_InsidePerps(out spherePoints, out linePoints, args, intersectArgs);

            switch (returnWhich)
            {
                case RayCastReturn.AllPoints:
                    // Nothing more to do
                    break;

                case RayCastReturn.ClosestToRay:
                    GetClosestPointsBetweenLineCircle_Closest_CircleLine(ref spherePoints, ref linePoints, line.origin);
                    break;

                case RayCastReturn.ClosestToRayOrigin:
                    GetClosestPointsBetweenLineCircle_Closest_RayOrigin(ref spherePoints, ref linePoints, line.origin);
                    break;

                case RayCastReturn.AlongRayDirection:
                    GetClosestPointsBetweenLineCircle_Closest_RayDirection(ref spherePoints, ref linePoints, line);
                    break;

                default:
                    throw new ApplicationException("Unknown RayCastReturn: " + returnWhich.ToString());
            }
        }

        /// <summary>
        /// This gets the line of intersection between two planes (returns false if they are parallel)
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://forums.create.msdn.com/forums/t/40119.aspx
        /// </remarks>
        public static bool GetIntersection_Plane_Plane(out Ray line, Plane plane1, Plane plane2)
        {
            Vector3 normal1 = plane1.normal.normalized;
            Vector3 normal2 = plane2.normal.normalized;

            // Find a point that satisfies both plane equations
            float distance1 = -plane1.distance;       // Math3D.PlaneDistance uses "normal + d = 0", but the equation below uses "normal = d", so the distances need to be negated
            float distance2 = -plane2.distance;

            float offDiagonal = Vector3.Dot(normal1, normal2);
            if (Math.Abs(offDiagonal).IsNearValue(1f))
            {
                // The planes are parallel
                line = new Ray();
                return false;
            }

            float det = 1f - offDiagonal * offDiagonal;

            float a = (distance1 - distance2 * offDiagonal) / det;
            float b = (distance2 - distance1 * offDiagonal) / det;
            Vector3 point = (a * normal1) + (b * normal2);

            // The line is perpendicular to both normals
            Vector3 direction = Vector3.Cross(normal1, normal2);

            line = new Ray(point, direction);

            return true;
        }

        public static Vector3[] GetIntersection_Plane_Triangle(Plane plane, ITriangle triangle)
        {
            // Get the line of intersection of the two planes
            if (!GetIntersection_Plane_Plane(out Ray line, plane, triangle.ToPlane()))
            {
                return null;
            }

            // Cap to the triangle
            return GetIntersection_Line_Triangle_sameplane(line, triangle);
        }
        public static Vector3[] GetIntersection_Triangle_Triangle(ITriangle triangle1, ITriangle triangle2)
        {
            // Get the line of intersection of the two planes
            if (!GetIntersection_Plane_Plane(out Ray line, triangle1.ToPlane(), triangle2.ToPlane()))
            {
                return null;
            }

            // Cap to the triangles
            Vector3[] segment1 = GetIntersection_Line_Triangle_sameplane(line, triangle1);
            if (segment1 == null)
            {
                return null;
            }

            Vector3[] segment2 = GetIntersection_Line_Triangle_sameplane(line, triangle2);
            if (segment2 == null)
            {
                return null;
            }

            // Cap the line segments
            Vector3? point1_A = GetIntersection_LineSegment_Point_colinear(segment1[0], segment1[segment1.Length - 1], segment2[0]);        //TODO: change to segment1[^1] when c# 8 is supported
            Vector3? point1_B = GetIntersection_LineSegment_Point_colinear(segment1[0], segment1[segment1.Length - 1], segment2[segment2.Length - 1]);

            Vector3? point2_A = GetIntersection_LineSegment_Point_colinear(segment2[0], segment2[segment2.Length - 1], segment1[0]);
            Vector3? point2_B = GetIntersection_LineSegment_Point_colinear(segment2[0], segment2[segment2.Length - 1], segment1[segment1.Length - 1]);

            List<Vector3> retVal = new List<Vector3>();

            AddIfUnique(retVal, point1_A);
            AddIfUnique(retVal, point1_B);
            AddIfUnique(retVal, point2_A);
            AddIfUnique(retVal, point2_B);

            if (retVal.Count == 0)
            {
                // The triangles aren't touching
                return null;
            }
            else if (retVal.Count == 1)
            {
                // The triangles are touching at a point, just consider that a non touch
                return null;
            }
            else if (retVal.Count == 2)
            {
                return retVal.ToArray();
            }
            else
            {
                throw new ApplicationException($"Didn't expect more than 2 unique points.  Got {retVal.Count} unique points");
            }
        }
        public static Vector3[] GetIntersection_Face_Triangle(Face3D face, ITriangle triangle, float rayLength = 1000)
        {
            Mesh facePoly = face.GetPolygonTriangles(rayLength);

            var intersections = facePoly.IterateTriangles().
                Select(o => GetIntersection_Triangle_Triangle(o, triangle)).
                Where(o => o != null).
                Select(o => (o[0], o[o.Length - 1])).
                ToArray();

            if (intersections.Length == 0)
            {
                return null;
            }

            // Turn them into a single segment
            //NOTE: This will fail if face.GetPolygonTriangles returns a non continuous chain of triangles
            Vector3 end1 = intersections[0].Item1;
            Vector3 end2 = intersections[0].Item2;

            for (int cntr = 1; cntr < intersections.Length; cntr++)
            {
                if (end1.IsNearValue(intersections[cntr].Item1))
                {
                    end1 = intersections[cntr].Item2;
                }
                else if (end2.IsNearValue(intersections[cntr].Item1))
                {
                    end2 = intersections[cntr].Item2;
                }
                else if (end1.IsNearValue(intersections[cntr].Item2))
                {
                    end1 = intersections[cntr].Item1;
                }
                else if (end2.IsNearValue(intersections[cntr].Item2))
                {
                    end2 = intersections[cntr].Item1;
                }
            }

            return new[] { end1, end2 };
        }

        /// <summary>
        /// This returns the distance between a plane and the origin
        /// WARNING: Make sure you actually want this instead of this.DistanceFromPlane
        /// NOTE: Normal must be a unit vector
        /// </summary>
        public static float GetPlaneOriginDistance(Vector3 normalUnit, Vector3 pointOnPlane)
        {
            // Use the plane equation to find the distance (Ax + By + Cz + D = 0)  We want to find D.
            // So, we come up with D = -(Ax + By + Cz)
            // Basically, the negated dot product of the normal of the plane and the point. (More about the dot product in another tutorial)
            return -((normalUnit.x * pointOnPlane.x) + (normalUnit.y * pointOnPlane.y) + (normalUnit.z * pointOnPlane.z));
        }

        //WARNING: This returns negative values if below the plane, so if you want distance, take the absolute value
        public static float DistanceFromPlane(Vector3[] polygon, Vector3 point)
        {
            Vector3 normal = GetTriangleNormalUnit(polygon);

            // Let's find the distance our plane is from the origin.  We can find this value
            // from the normal to the plane (polygon) and any point that lies on that plane (Any vertex)
            float originDistance = GetPlaneOriginDistance(normal, polygon[0]);

            // Get the distance from point1 from the plane uMath.Sing: Ax + By + Cz + D = (The distance from the plane)
            return DistanceFromPlane(normal, originDistance, point);
        }
        public static float DistanceFromPlane(Vector3 normalUnit, float originDistance, Vector3 point)
        {
            return ((normalUnit.x * point.x) +                  // Ax +
                    (normalUnit.y * point.y) +                  // Bx +
                    (normalUnit.z * point.z)) + originDistance; // Cz + D
        }

        /// <summary>
        /// This returns the intersection point of the line that intersects the plane
        /// </summary>
        public static Vector3? GetIntersection_Plane_Line(Plane plane, Ray ray)
        {
            Vector3? retVal = GetIntersection_Plane_Line(plane.normal.normalized, new Vector3[] { ray.origin, ray.origin + ray.direction }, plane.distance, EdgeType.Line);
            if (retVal == null)
            {
                return null;
            }
            else
            {
                return retVal.Value;
            }
        }
        public static Vector3? GetIntersection_Plane_Ray(Plane plane, Ray ray)
        {
            Vector3? retVal = GetIntersection_Plane_Line(plane.normal.normalized, new Vector3[] { ray.origin, ray.origin + ray.direction }, plane.distance, EdgeType.Ray);
            if (retVal == null)
            {
                return null;
            }
            else
            {
                return retVal.Value;
            }
        }
        public static Vector3? GetIntersection_Plane_LineSegment(Plane plane, Vector3 lineStart, Vector3 lineStop)
        {
            Vector3? retVal = GetIntersection_Plane_Line(plane.normal.normalized, new Vector3[] { lineStart, lineStop }, plane.distance, EdgeType.Segment);
            if (retVal == null)
            {
                return null;
            }
            else
            {
                return retVal.Value;
            }
        }

        public static Vector3[] GetIntersection_Face_Sphere(Face3D face, Vector3 sphereCenter, float sphereRadius)
        {
            // Intersect the face's plane with the sphere
            Plane plane = face.GetPlane();

            var circleIntersect = Math3D.GetIntersection_Plane_Sphere(plane, sphereCenter, sphereRadius);
            if (!circleIntersect.intersects)
            {
                return new Vector3[0];
            }

            // There is an intersection, edge intersections need to be done in 2D
            var transform2D = Math2D.GetTransformTo2D(plane);

            Vector2 circleCenter = transform2D.From3D_To2D.MultiplyPoint3x4(circleIntersect.center);
            float radiusSquared = circleIntersect.radius * circleIntersect.radius;

            var retVal = new List<Vector3>();

            #region visualize

            //const double LINETHICK = .01;
            //const double DOTRAD = .02;

            //Color color;

            //Debug3DWindow window = new Debug3DWindow()
            //{
            //    Title = "GetIntersectedPolygon",
            //    Background = new SolidColorBrush(UtilityWPF.ColorFromHex("BCC")),
            //};

            //window.AddAxisLines(sphereRadius * 2, LINETHICK * .75);

            ////window.AddPlane(plane, sphereRadius, UtilityWPF.ColorFromHex("888"));
            ////window.AddLine(plane.GetCenterPoint(), plane.GetCenterPoint() + plane.Normal, LINETHICK, UtilityWPF.ColorFromHex("888"));

            //window.AddCircle(circleIntersect.Item1, circleIntersect.Item2, LINETHICK, Colors.Black, plane);
            //window.AddDot(circleIntersect.Item1, DOTRAD, Colors.Black);

            #endregion

            foreach (Edge3D edge in face.Edges)
            {
                //Point3D point0_2D = transform2D.Item1.Transform(edge.Point0);
                //Point3D point1_2D = transform2D.Item1.Transform(edge.Point1Ext);
                //Point3D circleCender_2D = transform2D.Item1.Transform(circleIntersect.Item1);

                float distSqr0 = (edge.Point0 - circleIntersect.center).sqrMagnitude;
                float distSqr1 = (edge.Point1Ext - circleIntersect.center).sqrMagnitude;

                if (distSqr0 > radiusSquared && distSqr1 > radiusSquared)
                {
                    // Outside the circle
                    #region visualize

                    //color = UtilityWPF.ColorFromHex("999");

                    //if (edge.Point0.ToVector().Length <= sphereRadius || edge.Point1Ext.ToVector().Length <= sphereRadius)
                    //{
                    //    color = UtilityWPF.AlphaBlend(Colors.Red, color, .25);
                    //    window.Background = Brushes.Red;
                    //}

                    //window.AddLine(edge.Point0, edge.Point1Ext, LINETHICK, color);

                    #endregion
                }
                else if (distSqr0 < radiusSquared && distSqr1 < radiusSquared)
                {
                    // Inside the circle
                    retVal.Add(edge.Point0);
                    retVal.Add(edge.Point1Ext);
                    #region visualize

                    //color = UtilityWPF.ColorFromHex("575");

                    //if (edge.Point0.ToVector().Length > sphereRadius || edge.Point1Ext.ToVector().Length > sphereRadius)
                    //{
                    //    color = UtilityWPF.AlphaBlend(Colors.Red, color, .25);
                    //    window.Background = Brushes.Red;
                    //}

                    //window.AddLine(edge.Point0, edge.Point1Ext, LINETHICK, color);

                    #endregion
                }
                else if (distSqr0 < radiusSquared && distSqr1 > radiusSquared)
                {
                    retVal.AddRange(GetIntersectionFaceSphere_edge(edge.Point0, edge.Point1Ext, transform2D, circleCenter, circleIntersect.radius, sphereRadius));
                    #region visualize

                    //color = UtilityWPF.ColorFromHex("EEE");

                    //if (edge.Point0.ToVector().Length >= sphereRadius || edge.Point1Ext.ToVector().Length <= sphereRadius)
                    //{
                    //    color = UtilityWPF.AlphaBlend(Colors.Red, color, .25);
                    //    window.Background = Brushes.Red;
                    //}

                    //window.AddLine(edge.Point0, edge.Point1Ext, LINETHICK, color);

                    #endregion
                }
                else
                {
                    retVal.AddRange(GetIntersectionFaceSphere_edge(edge.Point1Ext, edge.Point0, transform2D, circleCenter, circleIntersect.radius, sphereRadius));
                    #region visualize

                    //color = UtilityWPF.ColorFromHex("DDD");

                    //if (edge.Point0.ToVector().Length < sphereRadius || edge.Point1Ext.ToVector().Length > sphereRadius)
                    //{
                    //    color = UtilityWPF.AlphaBlend(Colors.Red, color, .25);
                    //    window.Background = Brushes.Red;
                    //}

                    //window.AddLine(edge.Point0, edge.Point1Ext, LINETHICK, color);

                    #endregion
                }
            }

            #region visualize

            //window.AddDots(retVal, DOTRAD, Colors.White);
            //window.AddDot(sphereCenter, sphereRadius, UtilityWPF.ColorFromHex("20FFFFFF"), isHiRes: true);
            //window.Show();

            #endregion

            return retVal.ToArray();
        }

        /// <summary>
        /// This returns the circle that is the intersection of a sphere and plane
        /// NOTE: Returns null if there is no intersection
        /// </summary>
        /// <returns>
        /// intersects=True: sphere intersects plane.  False: sphere is too far away from plane
        /// center=Center of the circle (it is a point on the plane)
        /// radius=Radius of the circle
        /// </returns>
        public static (bool intersects, Vector3 center, float radius) GetIntersection_Plane_Sphere(Plane plane, Vector3 sphereCenter, float sphereRadius)
        {
            float distFromPlane = plane.GetDistanceToPoint(sphereCenter);

            if (Math.Abs(distFromPlane) > sphereRadius)     // returned distance could be negative (distFromPlane is negative if the normal is pointing the wrong direction)
            {
                return (false, new Vector3(), 0);
            }

            Vector3 circleCenter = sphereCenter + (plane.normal.normalized * distFromPlane);

            // Can't blindly trust the normal to know what direction to go
            float distFromPlane2 = plane.GetDistanceToPoint(circleCenter);
            if (!distFromPlane2.IsNearZero())
            {
                circleCenter = sphereCenter - (plane.normal.normalized * distFromPlane);
            }

            // Figure out the circle radius.  Look at the diagram here:
            //http://math.stackexchange.com/questions/943383/determine-circle-of-intersection-of-plane-and-sphere

            // sphereRadius^2 = distFromPlane^2 + circleRadius^2
            // circleRadius^2 = sphereRadius^2 - distFromPlane^2
            float circleRadius = (float)Math.Sqrt((sphereRadius * sphereRadius) - (distFromPlane * distFromPlane));

            return (true, circleCenter, circleRadius);
        }

        public static Vector3? GetIntersection_Triangle_Line(ITriangle triangle, Ray line)
        {
            // Plane
            Vector3? retVal = GetIntersection_Plane_Line(triangle.ToPlane(), line);
            if (retVal == null)
            {
                return null;
            }

            // Constrain to triangle
            Vector2 bary = ToBarycentric(triangle, retVal.Value);
            if (bary.x < 0 || bary.y < 0 || bary.x + bary.y > 1)
            {
                return null;
            }

            // The return point is inside the triangle
            return retVal.Value;
        }
        public static Vector3? GetIntersection_Triangle_Ray(ITriangle triangle, Ray ray)
        {
            // Plane
            Vector3? retVal = GetIntersection_Plane_Ray(triangle.ToPlane(), ray);
            if (retVal == null)
            {
                return null;
            }

            // Constrain to triangle
            Vector2 bary = ToBarycentric(triangle, retVal.Value);
            if (bary.x < 0 || bary.y < 0 || bary.x + bary.y > 1)
            {
                return null;
            }

            // The return point is inside the triangle
            return retVal.Value;
        }
        public static Vector3? GetIntersection_Triangle_LineSegment(ITriangle triangle, Vector3 lineStart, Vector3 lineStop)
        {
            // Plane
            Vector3? retVal = GetIntersection_Plane_LineSegment(triangle.ToPlane(), lineStart, lineStop);
            if (retVal == null)
            {
                return null;
            }

            // Constrain to triangle
            Vector2 bary = ToBarycentric(triangle, retVal.Value);
            if (bary.x < 0 || bary.y < 0 || bary.x + bary.y > 1)
            {
                return null;
            }

            // The return point is inside the triangle
            return retVal.Value;
        }

        //public static Vector3[] GetIntersection_Hull_Plane(Mesh convexHull, Plane plane)
        //{
        //    return HullTriangleIntersect.GetIntersection_Hull_Plane(convexHull, plane);
        //}
        //public static Vector3[] GetIntersection_Hull_Triangle(Mesh[] convexHull, ITriangle triangle)
        //{
        //    return HullTriangleIntersect.GetIntersection_Hull_Triangle(convexHull, triangle);
        //}

        /// <summary>
        /// This returns hits ordered by distance from the rayStart
        /// </summary>
        /// <returns>
        /// point=Point of intersection between the ray and a triangle
        /// triangleIndex=The triangle
        /// distToRayOrigin=Distance from rayStart
        /// </returns>
        public static (Vector3 point, int triangleIndex, float distToRayOrigin)[] GetIntersection_Hull_Ray(Mesh hull, Ray ray)
        {
            // Make a delagate to intersect the ray with a triangle
            var getHit = new Func<int, ITriangle, Ray, (bool hadHit, Vector3 point, int index, float distance)>((i, t, r) =>
            {
                Vector3? hit = GetIntersection_Triangle_Ray(t, r);

                if (hit == null)
                {
                    return (false, new Vector3(), i, 0f);
                }

                float distance = (hit.Value - r.origin).magnitude;

                return (true, hit.Value, i, distance);
            });

            // Test all triangles
            if (hull.TriangleCount() > 100)
            {
                return hull.IterateTriangles().
                    Select((o, i) => new
                    {
                        index = i,
                        triangle = o,
                    }).
                    AsParallel().
                    Select(o => getHit(o.index, o.triangle, ray)).
                    Where(o => o.hadHit).
                    OrderBy(o => o.distance).
                    Select(o => (o.point, o.index, o.distance)).
                    ToArray();
            }
            else
            {
                return hull.IterateTriangles().
                    Select((o, i) => new
                    {
                        index = i,
                        triangle = o,
                    }).
                    Select(o => getHit(o.index, o.triangle, ray)).
                    Where(o => o.hadHit).
                    OrderBy(o => o.distance).
                    Select(o => (o.point, o.index, o.distance)).
                    ToArray();
            }
        }

        //public static ContourPolygon[] GetIntersection_Mesh_Plane(Mesh[] mesh, Plane plane)
        //{
        //    return HullTriangleIntersect.GetIntersection_Mesh_Plane(mesh, plane);
        //}

        ///// <summary>
        ///// This intersects a convex hull with a voronoi.  Returns convex hulls
        ///// NOTE: This works in most cases, but may return hulls that are shaved off more than they should be
        ///// </summary>
        //public static (int controlPointIndex, Mesh convexHull)[] GetIntersection_Hull_Voronoi_full(Mesh convexHull, VoronoiResult3D voronoi)
        //{
        //    return HullVoronoiIntersect.GetIntersection_Hull_Voronoi_full(convexHull, voronoi);
        //}
        ///// <summary>
        ///// This intersects a convex hull with a voronoi.  Only returns the patches of the hull.  Doesn't return any depth along
        ///// the voronoi's faces
        ///// </summary>
        ///// <remarks>
        ///// In other words, the surface of the hull is divided up by the voronoi
        ///// </remarks>
        //public static VoronoiHullIntersect_PatchFragment[] GetIntersection_Triangles_Voronoi_surface(Mesh convexHull, VoronoiResult3D_wpf voronoi)
        //{
        //    return HullVoronoiIntersect.GetIntersection_Hull_Voronoi_surface(convexHull, voronoi);
        //}


        /// <summary>
        /// This checks to see if a point is inside the ranges of a polygon
        /// TODO: Figure out why this is giving false positives - I think it's meant for a 2D polygon within 3D.  Not a 3D hull
        /// </summary>
        public static bool IsInside_Polygon2D(Vector3 intersectionPoint, Vector3[] polygon2D, long verticeCount)
        {
            //const float MATCH_FACTOR = 0.9999;		// Used to cover up the error in floating point
            const float MATCH_FACTOR = 0.999999f;       // Used to cover up the error in floating point
            float Angle = 0;                     // Initialize the angle

            // Just because we intersected the plane, doesn't mean we were anywhere near the polygon.
            // This functions checks our intersection point to make sure it is inside of the polygon.
            // This is another tough function to grasp at first, but let me try and explain.
            // It's a brilliant method really, what it does is create triangles within the polygon
            // from the intersection point.  It then adds up the inner angle of each of those triangles.
            // If the angles together add up to 360 degrees (or 2 * PI in radians) then we are inside!
            // If the angle is under that value, we must be outside of polygon.  To further
            // understand why this works, take a pencil and draw a perfect triangle.  Draw a dot in
            // the middle of the triangle.  Now, from that dot, draw a line to each of the vertices.
            // Now, we have 3 triangles within that triangle right?  Now, we know that if we add up
            // all of the angles in a triangle we get 360 right?  Well, that is kinda what we are doing,
            // but the inverse of that.  Say your triangle is an isosceles triangle, so add up the angles
            // and you will get 360 degree angles.  90 + 90 + 90 is 360.

            for (int i = 0; i < verticeCount; i++)      // Go in a circle to each vertex and get the angle between
            {
                Vector3 vA = polygon2D[i] - intersectionPoint; // Subtract the intersection point from the current vertex
                                                               // Subtract the point from the next vertex
                Vector3 vB = polygon2D[(i + 1) % verticeCount] - intersectionPoint;

                Angle += Vector3.Angle(vA, vB);     // Find the angle between the 2 vectors and add them all up as we go along
            }

            // Now that we have the total angles added up, we need to check if they add up to 360 degrees.
            // Math.Since we are uMath.Sing the dot product, we are working in radians, so we check if the angles
            // equals 2*PI.  We defined PI in 3DMath.h.  You will notice that we use a MATCH_FACTOR
            // in conjunction with our desired degree.  This is because of the inaccuracy when working
            // with floating point numbers.  It usually won't always be perfectly 2 * PI, so we need
            // to use a little twiddling.  I use .9999, but you can change this to fit your own desired accuracy.

            if (Angle >= (MATCH_FACTOR * 360))  // If the angle is greater than 360 degrees
                return true;                            // The point is inside of the polygon

            return false;                               // If you get here, it obviously wasn't inside the polygon, so Return FALSE
        }

        /// <summary>
        /// This checks if a line is intersecting a polygon
        /// </summary>
        public static bool IsIntersecting_Polygon2D_Line(Vector3[] polygon2D, Vector3[] line, int verticeCount, out Vector3 normal, out float originDistance, out Vector3? intersectionPoint)
        {
            intersectionPoint = null;

            // First we check to see if our line intersected the plane.  If this isn't true
            // there is no need to go on, so return false immediately.
            // We pass in address of vNormal and originDistance so we only calculate it once

            // Reference
            if (!IsIntersecting_Plane_Line(polygon2D, line, out normal, out originDistance))
                return false;

            // Now that we have our normal and distance passed back from IntersectedPlane(), 
            // we can use it to calculate the intersection point.  The intersection point
            // is the point that actually is ON the plane.  It is between the line.  We need
            // this point test next, if we are inside the polygon.  To get the I-Point, we
            // give our function the normal of the plan, the points of the line, and the originDistance.

            intersectionPoint = GetIntersection_Plane_Line(normal, line, originDistance, EdgeType.Line);
            if (intersectionPoint == null)
                return false;

            // Now that we have the intersection point, we need to test if it's inside the polygon.
            // To do this, we pass in :
            // (our intersection point, the polygon, and the number of vertices our polygon has)

            if (IsInside_Polygon2D(intersectionPoint.Value, polygon2D, verticeCount))
                return true;                            // We collided!	  Return success


            // If we get here, we must have NOT collided

            return false;                               // There was no collision, so return false
        }
        /// <summary>
        /// This checks if a line is intersecting a polygon
        /// </summary>
        public static bool IsIntersecting_Polygon2D_Line(Vector3[] polygon2D, Vector3[] line)
        {
            return IsIntersecting_Polygon2D_Line(polygon2D, line, polygon2D.Length, out _, out _, out _);
        }

        /// <summary>
        /// NOTE: This only works if all of the triangle's normals point outward
        /// </summary>
        public static bool IsInside_ConvexHull(Mesh hull, Vector3 point)
        {
            foreach (var (point0, point1, point2) in hull.IterateTrianglePoints())
            {
                Vector3 normal = Vector3.Cross(point0 - point1, point2 - point1);

                if (Vector3.Dot(normal, point - point0) > 0d)
                {
                    // This point is outside
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        /// This will work for concave or convex hulls.  The triangle normals don't matter
        /// NOTE: It's up to the caller to do sphere or AABB check before taking the expense of calling this method
        /// </summary>
        /// <param name="asParallel">WARNING: Test both to see which is faster before automatically choosing parallel - it can be slower</param>
        /// <remarks>
        /// Got this here:
        /// http://www.yaldex.com/game-programming/0131020099_ch22lev1sec1.html
        /// </remarks>
        public static bool IsInside_ConcaveHull(Mesh hull, Vector3 point, bool asParallel = false)
        {
            //Vector3D rayDirection = new Vector3D(1, 0, 0);        // can't use this, because I was getting flaky results when the ray was shooting through perfectly aligned hulls (ray was intersecting the edges)
            Vector3 rayDirection = UnityEngine.Random.onUnitSphere;

            Ray ray = new Ray(point, rayDirection);

            int numIntersections = 0;

            if (asParallel)
            {
                numIntersections = hull.
                    IterateTriangles().
                    AsParallel().
                    Sum(o => (GetIntersection_Triangle_Ray(o, ray) != null) ? 1 : 0);
            }
            else
            {
                numIntersections = hull.
                    IterateTriangles().
                    Sum(o => (GetIntersection_Triangle_Ray(o, ray) != null) ? 1 : 0);
            }

            // If the number of intersections is odd, then the point started inside the hull.
            // If zero, it missed
            // If even, it punched all the way through
            return numIntersections % 2 == 1;
        }

        /// <summary>
        /// This splits the vector into a vector along the plane, and orthogonal to the plane
        /// </summary>
        /// <remarks>
        /// The two returned vectors will add up to the original vector passed in
        /// </remarks>
        public static DoubleVector SplitVector(Vector3 vector, Plane plane)
        {
            // Get portion along normal: this is up/down
            Vector3 orth = vector.GetProjectedVector(plane.normal);

            // Subtract that off: this is left/right
            Vector3 along = vector - orth;

            return new DoubleVector(along, orth);
        }

        #endregion

        #region random

        /// <summary>
        /// Gets a random vector with radius between maxRadius*-1 and maxRadius (bounds are spherical,
        /// rather than cube)
        /// </summary>
        public static Vector3 GetRandomVector_Spherical(float maxRadius)
        {
            return GetRandomVector_Spherical(0, maxRadius);
        }
        /// <summary>
        /// Gets a random vector with radius between maxRadius*-1 and maxRadius (bounds are spherical,
        /// rather than cube).  The radius will never be inside minRadius
        /// </summary>
        /// <remarks>
        /// The sqrt idea came from here:
        /// http://dzindzinovic.blogspot.com/2010/05/xna-random-point-in-circle.html
        /// </remarks>
        public static Vector3 GetRandomVector_Spherical(float minRadius, float maxRadius)
        {
            // A sqrt, sin and cos  :(           can it be made cheaper?
            float radius = minRadius + ((maxRadius - minRadius) * Mathf.Sqrt(StaticRandom.NextFloat()));		// without the square root, there is more chance at the center than the edges

            return GetRandomVector_Spherical_Shell(radius);
        }
        /// <summary>
        /// Gets a random vector with the radius passed in (bounds are spherical, rather than cube)
        /// </summary>
        public static Vector3 GetRandomVector_Spherical_Shell(float radius)
        {
            float theta = StaticRandom.NextFloat() * Mathf.PI * 2f;

            float phi = GetPhiForRandom(StaticRandom.NextFloat(-1, 1));

            float sinPhi = Mathf.Sin(phi);

            float x = radius * Mathf.Cos(theta) * sinPhi;
            float y = radius * Mathf.Sin(theta) * sinPhi;
            float z = radius * Mathf.Cos(phi);

            return new Vector3(x, y, z);
        }

        public static Vector3 GetRandomVector_Cone(Vector3 axis, float minAngle, float maxAngle, float minRadius, float maxRadius)
        {
            return GetRandomVectors_Cone(1, axis, minAngle, maxAngle, minRadius, maxRadius)[0];
        }
        public static Vector3[] GetRandomVectors_Cone(int count, Vector3 axis, float minAngle, float maxAngle, float minRadius, float maxRadius)
        {
            var rand = StaticRandom.GetRandomForThread();

            float minRand = GetRandomForPhi(minAngle * Mathf.Deg2Rad);
            float maxRand = GetRandomForPhi(maxAngle * Mathf.Deg2Rad);
            UtilityMath.MinMax(ref minRand, ref maxRand);

            Quaternion transform = Quaternion.FromToRotation(new Vector3(0, 0, 1), axis);       // make sure this works the same as the rotate transform

            Vector3[] retVal = new Vector3[count];

            for (int cntr = 0; cntr < count; cntr++)
            {
                float theta = rand.NextFloat(2 * (float)Math.PI);
                float phi = GetPhiForRandom(rand.NextFloat(minRand, maxRand));
                float radius = minRadius + ((maxRadius - minRadius) * (float)Math.Sqrt(rand.NextFloat()));       // without the square root, there is more chance at the center than the edges

                float sinPhi = (float)Math.Sin(phi);

                Vector3 vector = new Vector3
                (
                    radius * (float)Math.Cos(theta) * sinPhi,
                    radius * (float)Math.Sin(theta) * sinPhi,
                    radius * (float)Math.Cos(phi)
                );

                retVal[cntr] = transform * vector;
            }

            return retVal;
        }

        public static Vector3 GetArbitraryOrthonganal(Vector3 vector)
        {
            if (vector.IsInvalid() || vector.IsNearZero())
            {
                return new Vector3(float.NaN, float.NaN, float.NaN);
            }

            Vector3 rand = UnityEngine.Random.onUnitSphere;

            for (int cntr = 0; cntr < 10; cntr++)
            {
                Vector3 retVal = Vector3.Cross(vector, rand);

                if (retVal.IsInvalid())
                {
                    rand = UnityEngine.Random.onUnitSphere;
                }
                else
                {
                    return retVal;
                }
            }

            throw new ApplicationException("Infinite loop detected");
        }

        #endregion

        #region Private Methods

        // These were written a long time ago by someone else.  If they need to be exposed publicly, make a simplified version
        // that just takes a Plane and Ray, returns the intersect point: Vector3? (normal and originDistance are properties of the Plane type)
        private static bool IsIntersecting_Plane_Line(Vector3[] polygon, Vector3[] line, out Vector3 normal, out float originDistance)
        {
            if (line.Length != 2) throw new ArgumentException("A line vector can only be 2 verticies.", "vLine");

            //float distance1 = 0, distance2 = 0;                        // The distances from the 2 points of the line from the plane

            normal = GetTriangleNormalUnit(polygon);                            // We need to get the normal of our plane to go any further

            // Let's find the distance our plane is from the origin.  We can find this value
            // from the normal to the plane (polygon) and any point that lies on that plane (Any vertice)
            originDistance = GetPlaneOriginDistance(normal, polygon[0]);

            // Get the distance from point1 from the plane uMath.Sing: Ax + By + Cz + D = (The distance from the plane)
            float distance1 = ((normal.x * line[0].x) +                   // Ax +
                                (normal.y * line[0].y) +                   // Bx +
                                (normal.z * line[0].z)) + originDistance;  // Cz + D

            // Get the distance from point2 from the plane uMath.Sing Ax + By + Cz + D = (The distance from the plane)
            float distance2 = ((normal.x * line[1].x) +                   // Ax +
                                (normal.y * line[1].y) +                   // Bx +
                                (normal.z * line[1].z)) + originDistance;  // Cz + D

            // Now that we have 2 distances from the plane, if we times them together we either
            // get a positive or negative number.  If it's a negative number, that means we collided!
            // This is because the 2 points must be on either side of the plane (IE. -1 * 1 = -1).

            if (distance1 * distance2 >= 0)         // Check to see if both point's distances are both negative or both positive
                return false;                       // Return false if each point has the same sign.  -1 and 1 would mean each point is on either side of the plane.  -1 -2 or 3 4 wouldn't...
            else
                return true;                        // The line intersected the plane, Return TRUE
        }
        private static Vector3? GetIntersection_Plane_Line(Vector3 normal, Vector3[] line, float originDistance, EdgeType edgeType)
        {
            // Here comes the confuMath.Sing part.  We need to find the 3D point that is actually
            // on the plane.  Here are some steps to do that:

            // 1)  First we need to get the vector of our line, Then normalize it so it's a length of 1
            Vector3 lineDir = line[1] - line[0];       // Get the Vector of the line

            //NO!!!!  I don't know why this was done, but it messes up the segment length constraint
            //lineDir.Normalize();                // Normalize the lines vector

            // 2) Use the plane equation (distance = Ax + By + Cz + D) to find the distance from one of our points to the plane.
            //    Here I just chose a arbitrary point as the point to find that distance.  You notice we negate that
            //    distance.  We negate the distance because we want to eventually go BACKWARDS from our point to the plane.
            //    By doing this is will basically bring us back to the plane to find our intersection point.
            float numerator = -(normal.x * line[0].x +     // Use the plane equation with the normal and the line
                                               normal.y * line[0].y +
                                               normal.z * line[0].z + originDistance);

            // 3) If we take the dot product between our line vector and the normal of the polygon,
            //    this will give us the Math.CoMath.Sine of the angle between the 2 (Math.Since they are both normalized - length 1).
            //    We will then divide our Numerator by this value to find the offset towards the plane from our arbitrary point.
            float denominator = Vector3.Dot(normal, lineDir);      // Get the dot product of the line's vector and the normal of the plane

            // Math.Since we are uMath.Sing division, we need to make sure we don't get a divide by zero error
            // If we do get a 0, that means that there are INFINATE points because the the line is
            // on the plane (the normal is perpendicular to the line - (Normal.Vector = 0)).  
            // In this case, we should just return any point on the line.

            if (denominator == 0f)                     // Check so we don't divide by zero
                return null;        // line is parallel to plane

            // We divide the (distance from the point to the plane) by (the dot product)
            // to get the distance (dist) that we need to move from our arbitrary point.  We need
            // to then times this distance (dist) by our line's vector (direction).  When you times
            // a scalar (Math.Single number) by a vector you move along that vector.  That is what we are
            // doing.  We are moving from our arbitrary point we chose from the line BACK to the plane
            // along the lines vector.  It seems logical to just get the numerator, which is the distance
            // from the point to the line, and then just move back that much along the line's vector.
            // Well, the distance from the plane means the SHORTEST distance.  What about in the case that
            // the line is almost parallel with the polygon, but doesn't actually intersect it until half
            // way down the line's length.  The distance from the plane is short, but the distance from
            // the actual intersection point is pretty long.  If we divide the distance by the dot product
            // of our line vector and the normal of the plane, we get the correct length.  Cool huh?

            float dist = numerator / denominator;              // Divide to get the multiplying (percentage) factor

            switch (edgeType)
            {
                case EdgeType.Ray:
                    if (dist < 0f)
                    {
                        // This is outside of the ray
                        return null;
                    }
                    break;

                case EdgeType.Segment:
                    if (dist < 0f || dist > 1f)
                    {
                        // This is outside of the line segment
                        return null;
                    }
                    break;
            }

            // Now, like we said above, we times the dist by the vector, then add our arbitrary point.
            // This essentially moves the point along the vector to a certain distance.  This now gives
            // us the intersection point.  Yay!

            // Return the intersection point
            return new Vector3
            (
                line[0].x + (lineDir.x * dist),
                line[0].y + (lineDir.y * dist),
                line[0].z + (lineDir.z * dist)
            );
        }

        /// <summary>
        /// This returns the normal of a polygon (The direction the polygon is facing)
        /// </summary>
        private static Vector3 GetTriangleNormalUnit(Vector3[] triangle)
        {
            // This is the original, but was returning a left handed normal
            //Vector3D vVector1 = triangle[2] - triangle[0];
            //Vector3D vVector2 = triangle[1] - triangle[0];

            //Vector3D vNormal = Vector3D.CrossProduct(vVector1, vVector2);		// Take the cross product of our 2 vectors to get a perpendicular vector

            Vector3 dir1 = triangle[0] - triangle[1];
            Vector3 dir2 = triangle[2] - triangle[1];

            Vector3 normal = Vector3.Cross(dir2, dir1);

            return normal.normalized;
        }

        private static Vector3[] GetIntersection_Line_Triangle_sameplane(Ray line, ITriangle triangle)
        {
            var retval = new List<Vector3>();

            // Cap the line to the triangle
            foreach (TriangleEdge edge in Triangle.Edges)
            {
                if (!GetClosestPoints_Line_LineSegment(out Vector3[] resultsLine, out Vector3[] resultsLineSegment, line, triangle.GetPoint(edge, true), triangle.GetPoint(edge, false)))
                {
                    continue;
                }

                if (resultsLine.Length != resultsLineSegment.Length)
                {
                    throw new ApplicationException("The line vs line segments have a different number of matches");
                }

                // This method is dealing with lines that are in the same plane, so if the result point for the plane/plane line is different
                // than the triangle edge, then throw out this match
                bool allMatched = true;
                for (int cntr = 0; cntr < resultsLine.Length; cntr++)
                {
                    if (!resultsLine[cntr].IsNearValue(resultsLineSegment[cntr]))
                    {
                        allMatched = false;
                        break;
                    }
                }

                if (!allMatched)
                {
                    continue;
                }

                retval.AddRange(resultsLineSegment);

                if (retval.Count >= 2)
                {
                    // No need to keep looking, there will only be up to two points of intersection
                    break;
                }
            }

            // Exit Function
            if (retval.Count == 0)
            {
                return null;
            }
            else if (retval.Count.In(1, 2))
            {
                // 1 match is just touching a vertex
                // 2 matches is a standard result
                return retval.ToArray();
            }
            else
            {
                throw new ApplicationException("Found more than two intersection points");
            }
        }

        private static Vector3? GetIntersection_LineSegment_Point_colinear(Vector3 segmentStart, Vector3 segmentStop, Vector3 point)
        {
            if (segmentStart.IsNearValue(point) || segmentStop.IsNearValue(point))
            {
                // It's touching one of the endpoints
                return point;
            }

            // Make sure the point isn't beyond the line segment
            Vector3 segmentDir = segmentStop - segmentStart;
            Vector3 testDir = point - segmentStart;

            if (!Vector3.Dot(segmentDir.normalized, testDir.normalized).IsNearValue(1f))
            {
                // It's the other direction (beyond segment start)
                return null;
            }

            if (testDir.sqrMagnitude > segmentDir.sqrMagnitude)
            {
                // It's beyond segment stop
                return null;
            }

            // It's somewhere inside the segment
            return point;
        }

        private static void AddIfUnique(List<Vector3> list, Vector3? test)
        {
            if (test == null)
            {
                return;
            }

            if (list.Any(o => o.IsNearValue(test.Value)))
            {
                return;
            }

            list.Add(test.Value);
        }

        private static IEnumerable<Vector3> GetIntersectionFaceSphere_edge(Vector3 inside, Vector3 outside, TransformsToFrom2D transform2D, Vector2 circleCenter, float circleRadius, float sphereRadius_TEMP)
        {
            Vector2 inside2D = transform2D.From3D_To2D.MultiplyPoint3x4(inside);
            Vector2 outside2D = transform2D.From3D_To2D.MultiplyPoint3x4(outside);

            float? intersectPercent = Math2D.GetIntersection_LineSegment_Circle_percent(inside2D, outside2D, circleCenter, circleRadius);
            if (intersectPercent == null)
            {
                #region visualize

                //const double LINETHICK = .005;
                //const double DOTRAD = .03;

                //var window = new Debug3DWindow()
                //{
                //    Title = "GetIntersectedPolygon_IntersectedEdge: missed",
                //    Background = new SolidColorBrush(UtilityWPF.ColorFromHex("EED")),
                //};

                //window.AddAxisLines(circleRadius * 2, LINETHICK);

                //// 2D
                //window.AddDot(inside2D.ToPoint3D(), DOTRAD, UtilityWPF.ColorFromHex("000"));
                //window.AddDot(outside2D.ToPoint3D(), DOTRAD, UtilityWPF.ColorFromHex("000"));
                //window.AddLine(inside2D.ToPoint3D(), outside2D.ToPoint3D(), LINETHICK, UtilityWPF.ColorFromHex("000"));

                //window.AddCircle(circleCenter.ToPoint3D(), circleRadius, LINETHICK, UtilityWPF.ColorFromHex("000"));

                //// 3D
                //window.AddDot(inside, DOTRAD, UtilityWPF.ColorFromHex("808080"));
                //window.AddDot(outside, DOTRAD, UtilityWPF.ColorFromHex("808080"));
                //window.AddLine(inside, outside, LINETHICK, UtilityWPF.ColorFromHex("808080"));

                //window.AddDot(new Point3D(0, 0, 0), sphereRadius_TEMP, UtilityWPF.ColorFromHex("30808080"), isHiRes: true);

                //window.Show();

                #endregion

                return new Vector3[0];
            }

            #region visualize

            //const double LINETHICK = .005;
            //const double DOTRAD = .03;
            //const double CIRCLETHICK = DOTRAD * .66;

            //Point3D inside2D_3D = transform2D.Item1.Transform(inside);
            //Point3D outside2D_3D = transform2D.Item1.Transform(outside);

            //var window = new Debug3DWindow()
            //{
            //    Title = "GetIntersectedPolygon_IntersectedEdge",
            //    Background = new SolidColorBrush(UtilityWPF.ColorFromHex("AAA")),
            //};

            //window.AddAxisLines(circleRadius * 2, LINETHICK);

            //// 2D
            //window.AddDot(inside2D_3D, DOTRAD, UtilityWPF.ColorFromHex("888"));
            //window.AddDot(outside2D_3D, DOTRAD, UtilityWPF.ColorFromHex("888"));
            //window.AddDot(inside2D_3D + ((outside2D_3D - inside2D_3D) * intersectPercent.Value), DOTRAD, UtilityWPF.ColorFromHex("CCC"));

            //window.AddDot(circleCenter.ToPoint3D(), DOTRAD, UtilityWPF.ColorFromHex("577F57"));
            //window.AddCircle(circleCenter.ToPoint3D(), circleRadius, CIRCLETHICK, UtilityWPF.ColorFromHex("577F57"));

            //// 3D
            //window.AddDot(inside, DOTRAD, UtilityWPF.ColorFromHex("000"));
            //window.AddDot(outside, DOTRAD, UtilityWPF.ColorFromHex("000"));
            //window.AddDot(inside + ((outside - inside) * intersectPercent.Value), DOTRAD, UtilityWPF.ColorFromHex("FFF"));

            //Point3D circleCenter3D = transform2D.Item2.Transform(circleCenter.ToPoint3D());
            //window.AddDot(circleCenter3D, DOTRAD, UtilityWPF.ColorFromHex("79B279"));

            //ITriangle_wpf plane = new Triangle(transform2D.Item2.Transform(new Point3D(0, 0, 0)), transform2D.Item2.Transform(new Point3D(1, 0, 0)), transform2D.Item2.Transform(new Point3D(0, 1, 0)));

            //window.AddCircle(circleCenter3D, circleRadius, CIRCLETHICK, UtilityWPF.ColorFromHex("79B279"), plane);

            //window.Show();

            #endregion

            return new[]
            {
                inside,
                inside + ((outside - inside) * intersectPercent.Value),
            };
        }

        /// <summary>
        /// This returns a phi from 0 to pi based on an input from -1 to 1
        /// </summary>
        /// <remarks>
        /// NOTE: The input is linear (even chance of any value from -1 to 1), but the output is scaled to give an even chance of a Z
        /// on a sphere:
        /// 
        /// z is cos of phi, which isn't linear.  So the probability is higher that more will be at the poles.  Which means if I want
        /// a linear probability of z, I need to feed the cosine something that will flatten it into a line.  The curve that will do that
        /// is arccos (which basically rotates the cosine wave 90 degrees).  This means that it is undefined for any x outside the range
        /// of -1 to 1.  So I have to shift the random statement to go between -1 to 1, run it through the curve, then shift the result
        /// to go between 0 and pi
        /// </remarks>
        internal static float GetPhiForRandom(float num_negone_posone)
        {
            //double phi = rand.NextDouble(-1, 1);		// value from -1 to 1
            //phi = -Math.Asin(phi) / (Math.PI * .5d);		// another value from -1 to 1
            //phi = (1d + phi) * Math.PI * .5d;		// from 0 to pi

            double retVal = Math.PI / 2d - Math.Asin(num_negone_posone);
            return (float)retVal;
        }
        /// <summary>
        /// This is a complimentary function to GetPhiForRandom.  It's used to figure out the range for random to get a desired phi
        /// </summary>
        internal static float GetRandomForPhi(float expectedRadians)
        {
            double retVal = -Math.Sin(expectedRadians - (Math.PI / 2));
            return (float)retVal;
        }

        #region Circle/Line Intersect Helpers

        private struct CircleLineArgs
        {
            public Plane CirclePlane;
            public Vector3 CircleCenter;
            public float CircleRadius;
            public Ray Line;
        }

        private struct CirclePlaneIntersectProps
        {
            // This is the line that the planes intersect along
            public Ray Line;

            // This is a line from the circle's center to the intersect line
            public Vector3 NearestToCenter;
            public Vector3 CenterToNearest;
            public float CenterToNearestLength;

            // This is whether NearestToCenter is within the circle or outside of it
            public bool IsInsideCircle;
        }

        private static bool GetClosestPointsBetweenLineCircle(out Vector3[] circlePoints, out Vector3[] linePoints, CircleLineArgs args)
        {
            #region Scenarios

            // Line intersects plane inside circle:
            //		Calculate intersect point to circle rim
            //		Calculate two perps to circle rim
            //
            // Take the closest of those three points

            // Line intersects plane outside circle, but passes over circle:
            //		Calculate intersect point to circle rim
            //		Calculate two perps to circle rim
            //
            // Take the closest of those three points

            // Line is parallel to the plane, passes over circle
            //		Calculate two perps to circle rim

            // Line is parallel to the plane, does not pass over circle
            //		Get closest point between center and line, project onto plane, find point along the circle

            // Line does not pass over the circle
            //		Calculate intersect point to circle rim
            //		Get closest point between plane intersect line and circle center
            //
            // Take the closest of those two points

            // Line is perpendicular to the plane
            //		Calculate intersect point to circle rim

            #endregion

            // Detect perpendicular
            float dot = Vector3.Dot(args.CirclePlane.normal.normalized, args.Line.direction.normalized);
            if (Math.Abs(dot).IsNearValue(1f))
            {
                return GetClosestPointsBetweenLineCircle_Perpendicular(out circlePoints, out linePoints, args);
            }

            // Project the line onto the circle's plane
            CirclePlaneIntersectProps planeIntersect = GetClosestPointsBetweenLineCircle_PlaneIntersect(args);

            // There's less to do if the line is parallel
            if (dot.IsNearZero())
            {
                GetClosestPointsBetweenLineCircle_Parallel(out circlePoints, out linePoints, args, planeIntersect);
            }
            else
            {
                GetClosestPointsBetweenLineCircle_Other(out circlePoints, out linePoints, args, planeIntersect);
            }

            return true;
        }
        private static bool GetClosestPointsBetweenLineCircle_Perpendicular(out Vector3[] circlePoints, out Vector3[] linePoints, CircleLineArgs args)
        {
            Vector3 planeIntersect = GetClosestPoint_Line_Point(args.Line, args.CircleCenter);

            if (planeIntersect.IsNearValue(args.CircleCenter))
            {
                // This is a perpendicular ray shot straight through the center.  All circle points are closest to the line
                circlePoints = null;
                linePoints = new Vector3[] { args.CircleCenter };
                return false;
            }

            GetClosestPointsBetweenLineCircle_CenterToPlaneIntersect(out circlePoints, out linePoints, args, planeIntersect);
            return true;
        }
        private static void GetClosestPointsBetweenLineCircle_Parallel(out Vector3[] circlePoints, out Vector3[] linePoints, CircleLineArgs args, CirclePlaneIntersectProps planeIntersect)
        {
            if (planeIntersect.IsInsideCircle)
            {
                GetClosestPointsBetweenLineCircle_InsidePerps(out circlePoints, out linePoints, args, planeIntersect);
            }
            else
            {
                circlePoints = new Vector3[] { args.CircleCenter + (planeIntersect.CenterToNearest * (args.CircleRadius / planeIntersect.CenterToNearestLength)) };
                linePoints = new Vector3[] { GetClosestPoint_Line_Point(args.Line, circlePoints[0]) };
            }
        }
        private static void GetClosestPointsBetweenLineCircle_Other(out Vector3[] circlePoints, out Vector3[] linePoints, CircleLineArgs args, CirclePlaneIntersectProps planeIntersect)
        {
            // See where the line intersects the circle's plane
            Vector3? lineIntersect = GetIntersection_Plane_Line(args.CirclePlane, args.Line);
            if (lineIntersect == null)      // this should never happen, since an IsParallel check was already done (but one might be stricter than the other)
            {
                GetClosestPointsBetweenLineCircle_Parallel(out circlePoints, out linePoints, args, planeIntersect);
                return;
            }

            if (planeIntersect.IsInsideCircle)
            {
                #region Line is over circle

                // Line intersects plane inside circle:
                //		Calculate intersect point to circle rim
                //		Calculate two perps to circle rim
                //
                // Take the closest of those three points

                GetClosestPointsBetweenLineCircle_CenterToPlaneIntersect(out Vector3[] circlePoints1, out Vector3[] linePoints1, args, lineIntersect.Value);

                GetClosestPointsBetweenLineCircle_InsidePerps(out Vector3[] circlePoints2, out Vector3[] linePoints2, args, planeIntersect);

                GetClosestPointsBetweenLineCircle_Other_Min(out circlePoints, out linePoints, circlePoints1, linePoints1, circlePoints2, linePoints2);

                #endregion
            }
            else
            {
                #region Line is outside circle

                // Line does not pass over the circle
                //		Calculate intersect point to circle rim
                //		Get closest point between plane intersect line and circle center
                //
                // Take the closest of those two points

                GetClosestPointsBetweenLineCircle_CenterToPlaneIntersect(out Vector3[] circlePoints3, out Vector3[] linePoints3, args, lineIntersect.Value);

                GetClosestPointsBetweenLineCircle_CenterToPlaneIntersect(out Vector3[] circlePoints4, out Vector3[] linePoints4, args, planeIntersect.NearestToCenter);

                GetClosestPointsBetweenLineCircle_Other_Min(out circlePoints, out linePoints, circlePoints3, linePoints3, circlePoints4, linePoints4);

                #endregion
            }
        }
        private static void GetClosestPointsBetweenLineCircle_Other_Min(out Vector3[] circlePoints, out Vector3[] linePoints, Vector3[] circlePoints1, Vector3[] linePoints1, Vector3[] circlePoints2, Vector3[] linePoints2)
        {
            List<Vector3> circlePointList = new List<Vector3>();
            List<Vector3> linePointList = new List<Vector3>();
            float distance = float.MaxValue;

            // Find the shortest distance across the pairs
            if (circlePoints1 != null)
            {
                for (int cntr = 0; cntr < circlePoints1.Length; cntr++)
                {
                    float localDistance = (linePoints1[cntr] - circlePoints1[cntr]).magnitude;

                    if (localDistance.IsNearValue(distance))
                    {
                        circlePointList.Add(circlePoints1[cntr]);
                        linePointList.Add(linePoints1[cntr]);
                    }
                    else if (localDistance < distance)
                    {
                        circlePointList.Clear();
                        linePointList.Clear();
                        circlePointList.Add(circlePoints1[cntr]);
                        linePointList.Add(linePoints1[cntr]);
                        distance = localDistance;
                    }
                }
            }

            if (circlePoints2 != null)
            {
                for (int cntr = 0; cntr < circlePoints2.Length; cntr++)
                {
                    float localDistance = (linePoints2[cntr] - circlePoints2[cntr]).magnitude;

                    if (localDistance.IsNearValue(distance))
                    {
                        circlePointList.Add(circlePoints2[cntr]);
                        linePointList.Add(linePoints2[cntr]);
                    }
                    else if (localDistance < distance)
                    {
                        circlePointList.Clear();
                        linePointList.Clear();
                        circlePointList.Add(circlePoints2[cntr]);
                        linePointList.Add(linePoints2[cntr]);
                        distance = localDistance;
                    }
                }
            }

            if (circlePointList.Count == 0)
            {
                throw new ApplicationException("Couldn't find a return point");
            }

            // Return the result
            circlePoints = circlePointList.ToArray();
            linePoints = linePointList.ToArray();
        }
        private static CirclePlaneIntersectProps GetClosestPointsBetweenLineCircle_PlaneIntersect(CircleLineArgs args)
        {
            CirclePlaneIntersectProps retVal;

            // The slice plane runs perpendicular to the circle's plane
            Plane slicePlane = new Plane(args.Line.origin, args.Line.origin + args.Line.direction, args.Line.origin + args.CirclePlane.normal);

            // Use that slice plane to project the line onto the circle's plane
            if (!GetIntersection_Plane_Plane(out retVal.Line, args.CirclePlane, slicePlane))
            {
                throw new ApplicationException("The slice plane should never be parallel to the circle's plane");       // it was defined as perpendicular
            }

            // Find the closest point between the circle's center to this intersection line
            retVal.NearestToCenter = GetClosestPoint_Line_Point(retVal.Line, args.CircleCenter);
            retVal.CenterToNearest = retVal.NearestToCenter - args.CircleCenter;
            retVal.CenterToNearestLength = retVal.CenterToNearest.magnitude;

            retVal.IsInsideCircle = retVal.CenterToNearestLength <= args.CircleRadius;

            return retVal;
        }
        private static void GetClosestPointsBetweenLineCircle_CenterToPlaneIntersect(out Vector3[] circlePoints, out Vector3[] linePoints, CircleLineArgs args, Vector3 planeIntersect)
        {
            Vector3 centerToIntersect = planeIntersect - args.CircleCenter;
            float centerToIntersectLength = centerToIntersect.magnitude;

            if (centerToIntersectLength.IsNearZero())
            {
                circlePoints = null;
                linePoints = null;
            }
            else
            {
                circlePoints = new Vector3[] { args.CircleCenter + (centerToIntersect * (args.CircleRadius / centerToIntersectLength)) };
                linePoints = new Vector3[] { GetClosestPoint_Line_Point(args.Line, circlePoints[0]) };
            }
        }
        private static void GetClosestPointsBetweenLineCircle_InsidePerps(out Vector3[] circlePoints, out Vector3[] linePoints, CircleLineArgs args, CirclePlaneIntersectProps planeIntersect)
        {
            // See if the line passes through the center
            if (planeIntersect.CenterToNearestLength.IsNearZero())
            {
                Vector3 lineDirUnit = planeIntersect.Line.direction.normalized;

                // The line passes over the circle's center, so the nearest points will shoot straight from the center in the direction of the line
                circlePoints = new Vector3[]
                {
                    args.CircleCenter + (lineDirUnit * args.CircleRadius),
                    args.CircleCenter - (lineDirUnit * args.CircleRadius),
                };
            }
            else
            {
                // The two points are perpendicular to this line.  Use A^2 + B^2 = C^2 to get the length of the perpendiculars
                float perpLength = (float)Math.Sqrt((args.CircleRadius * args.CircleRadius) - (planeIntersect.CenterToNearestLength * planeIntersect.CenterToNearestLength));
                Vector3 perpDirection = Vector3.Cross(planeIntersect.CenterToNearest, args.CirclePlane.normal).normalized;

                circlePoints = new Vector3[]
                {
                    planeIntersect.NearestToCenter + (perpDirection * perpLength),
                    planeIntersect.NearestToCenter - (perpDirection * perpLength),
                };
            }

            // Get corresponding points along the line
            linePoints = new Vector3[]
            {
                GetClosestPoint_Line_Point(args.Line, circlePoints[0]),
                GetClosestPoint_Line_Point(args.Line, circlePoints[1]),
            };
        }

        /// <summary>
        /// This returns the one that is closest to pointOnLine
        /// </summary>
        private static void GetClosestPointsBetweenLineCircle_Closest_RayOrigin(ref Vector3[] circlePoints, ref Vector3[] linePoints, Vector3 rayOrigin)
        {
            #region Find closest point

            // There is more than one point, and they want a single point
            float minDistance = float.MaxValue;
            int minIndex = -1;

            for (int cntr = 0; cntr < circlePoints.Length; cntr++)
            {
                float distance = (linePoints[cntr] - rayOrigin).sqrMagnitude;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    minIndex = cntr;
                }
            }

            if (minIndex < 0)
            {
                throw new ApplicationException("Should always find a closest point");
            }

            #endregion

            // Return only the closest point
            circlePoints = new Vector3[] { circlePoints[minIndex] };
            linePoints = new Vector3[] { linePoints[minIndex] };
        }
        /// <summary>
        /// This returns the one that is closest between the two hits
        /// </summary>
        private static void GetClosestPointsBetweenLineCircle_Closest_CircleLine(ref Vector3[] circlePoints, ref Vector3[] linePoints, Vector3 rayOrigin)
        {
            #region Find closest point

            // There is more than one point, and they want a single point
            float minDistance = float.MaxValue;
            float minOriginDistance = float.MaxValue;     // use this as a secondary sort (really important if the collision shape is a cylinder or sphere.  The line will have two exact matches, so return the one closest to the ray cast origin)
            int minIndex = -1;

            for (int cntr = 0; cntr < circlePoints.Length; cntr++)
            {
                float distance = (linePoints[cntr] - circlePoints[cntr]).sqrMagnitude;
                float originDistance = (linePoints[cntr] - rayOrigin).sqrMagnitude;

                bool isEqualDistance = distance.IsNearValue(minDistance);

                //NOTE: I can't just say distance < minDistance, because for a sphere, it kept jittering between the near
                // side and far side, so it has to be closer by a decisive amount
                if ((!isEqualDistance && distance < minDistance) || (isEqualDistance && originDistance < minOriginDistance))
                {
                    minDistance = distance;
                    minOriginDistance = originDistance;
                    minIndex = cntr;
                }
            }

            if (minIndex < 0)
            {
                throw new ApplicationException("Should always find a closest point");
            }

            #endregion

            // Return only the closest point
            circlePoints = new Vector3[] { circlePoints[minIndex] };
            linePoints = new Vector3[] { linePoints[minIndex] };
        }
        /// <summary>
        /// This returns the points that are along the direction of the ray
        /// </summary>
        private static void GetClosestPointsBetweenLineCircle_Closest_RayDirection(ref Vector3[] circlePoints, ref Vector3[] linePoints, Ray ray)
        {
            List<Vector3> circleReturn = new List<Vector3>();
            List<Vector3> lineReturn = new List<Vector3>();

            for (int cntr = 0; cntr < circlePoints.Length; cntr++)
            {
                Vector3 testDirection = circlePoints[cntr] - ray.origin;
                if (Vector3.Dot(testDirection, ray.direction) > 0)
                {
                    circleReturn.Add(circlePoints[cntr]);
                    lineReturn.Add(linePoints[cntr]);
                }
            }

            circlePoints = circleReturn.ToArray();
            linePoints = lineReturn.ToArray();
        }

        #endregion
        #region Cylinder/Line Intersect Helpers

        private static CirclePlaneIntersectProps GetClosestPointsBetweenLineCylinder_PlaneIntersect(CircleLineArgs args, Vector3 nearestLinePoint, Vector3 nearestLine, float nearestLineDistance)
        {
            //NOTE: This is nearly identical to GetClosestPointsBetweenLineCircle_PlaneIntersect, but since some stuff was already done,
            // it's more just filling out the struct

            CirclePlaneIntersectProps retVal;

            // The slice plane runs perpendicular to the circle's plane
            Plane slicePlane = new Plane(args.Line.origin, args.Line.origin + args.Line.direction, args.Line.origin + args.CirclePlane.normal);

            // Use that slice plane to project the line onto the circle's plane
            if (!GetIntersection_Plane_Plane(out retVal.Line, args.CirclePlane, slicePlane))
            {
                throw new ApplicationException("The slice plane should never be parallel to the circle's plane");       // it was defined as perpendicular
            }

            // Store what was passed in (the circle/line intersect waits till now to do this, but for cylinder, this was done previously)
            retVal.NearestToCenter = nearestLinePoint;
            retVal.CenterToNearest = nearestLine;
            retVal.CenterToNearestLength = nearestLineDistance;

            retVal.IsInsideCircle = true;       // this method is only called when true

            return retVal;
        }

        private static void GetClosestPointsBetweenLineCylinder_Finish(out Vector3[] cylinderPoints, out Vector3[] linePoints, CircleLineArgs args, CirclePlaneIntersectProps intersectArgs)
        {
            // Get the circle intersects
            GetClosestPointsBetweenLineCircle_InsidePerps(out Vector3[] circlePoints2D, out Vector3[] linePoints2D, args, intersectArgs);

            // Project the circle hits onto the original line
            GetClosestPoints_Line_Line(out Vector3? p1, out _, args.Line, new Ray(circlePoints2D[0], args.CirclePlane.normal));
            GetClosestPoints_Line_Line(out Vector3? p3, out _, args.Line, new Ray(circlePoints2D[1], args.CirclePlane.normal));

            // p1 and p2 are the same, p3 and p4 are the same (p2 and p4 were later changed to _)
            if (p1 == null || p3 == null)
            {
                cylinderPoints = new Vector3[] { circlePoints2D[0], circlePoints2D[1] };
            }
            else
            {
                cylinderPoints = new Vector3[] { p1.Value, p3.Value };
            }
            linePoints = cylinderPoints;
        }

        #endregion

        #endregion
    }

    #region enum: RayCastReturn

    public enum RayCastReturn
    {
        AllPoints,
        ClosestToRayOrigin,
        ClosestToRay,
        AlongRayDirection,
    }

    #endregion
    #region enum: EdgeType

    public enum EdgeType
    {
        Segment,
        Ray,
        Line
    }

    #endregion
    #region enum: LocationOnLineSegment

    public enum LocationOnLineSegment
    {
        Start,
        Middle,
        Stop,
    }

    #endregion

    #region class: Face3D

    /// <summary>
    /// This is a polygon, but in 3D.  Even though it has multiple edges, it's expected to be coplanar.  Also, the outer
    /// two edges could be rays
    /// </summary>
    public class Face3D
    {
        #region Declaration Section

        private readonly object _lock = new object();

        #endregion

        #region Constructor

        public Face3D(int[] edges, Edge3D[] allEdges)
        {
            //TODO: May want to validate that the points are coplanar

            this.AllEdges = allEdges;

            this.EdgeIndices = edges;
            this.Edges = edges.
                Select(o => allEdges[o]).
                ToArray();

            this.IsClosed = this.Edges.All(o => o.EdgeType == EdgeType.Segment);
        }

        #endregion

        public readonly int[] EdgeIndices;

        public readonly Edge3D[] Edges;

        public readonly Edge3D[] AllEdges;

        /// <summary>
        /// True: All edges are segments
        /// False:  This face contains rays
        /// </summary>
        public readonly bool IsClosed;

        private long? _token = null;
        public long Token
        {
            get
            {
                lock (_lock)
                {
                    if (_token == null)
                    {
                        _token = TokenGenerator.NextToken();
                    }

                    return _token.Value;
                }
            }
        }

        #region Public Methods

        /// <returns>
        /// Item1=Indices into all points
        /// Item2=All points -- NOT just the polygon's points, ALL POINTS (use indices into item2 to get poly points)
        /// </returns>
        public (int[] indices, Vector3[] allPoints) GetPolygon(float rayLength = 1000f)
        {
            if (this.IsClosed)
            {
                return GetPolygon_Closed(this.Edges);
            }
            else
            {
                return GetPolygon_Open(this.Edges, rayLength);
            }
        }

        /// <summary>
        /// This overload returns just the points instead of a tuple
        /// </summary>
        public Vector3[] GetPolygonPoints(float rayLength = 1000)
        {
            var polyPoints = GetPolygon(rayLength);

            return polyPoints.Item1.
                Select(o => polyPoints.allPoints[o]).
                ToArray();
        }

        /// <summary>
        /// This converts into polygons
        /// NOTE: The triangle's index has nothing to do with this.EdgeIndicies.  The values are local to the set of triangles returned
        /// </summary>
        public Mesh GetPolygonTriangles(float rayLength = 1000)
        {
            throw new ApplicationException("finish this");      // port it from party people when it's needed

            //var polyPoints = GetPolygon(rayLength);
            //return Math2D.GetTrianglesFromConvexPoly(polyPoints.indices, polyPoints.allPoints);
        }

        public Plane GetPlane()
        {
            if (this.Edges.Length < 2)
            {
                throw new ApplicationException("There needs to be at least two edges");
            }

            int common = Edge3D.GetCommonIndex(this.Edges[0], this.Edges[1]);
            if (common < 0)
            {
                throw new ApplicationException(string.Format("Non touching edges: {0} - {1}", this.Edges[0].ToString(), this.Edges[1].ToString()));
            }

            Vector3 point0 = Edge3D.GetOtherPointExt(this.Edges[0], this.Edges[1]);
            Vector3 point1 = this.Edges[0].GetPoint(common);
            Vector3 point2 = Edge3D.GetOtherPointExt(this.Edges[1], this.Edges[0]);

            return new Plane(point0, point1, point2);
        }

        #endregion

        #region Private Methods

        private static (int[] indices, Vector3[] allPoints) GetPolygon_Closed(Edge3D[] edges)
        {
            #region asserts
#if DEBUG

            if (edges.Length < 3)
            {
                throw new ArgumentException("Must have at least three edges: " + edges.Length.ToString());
            }

            if (!edges.All(o => o.EdgeType == EdgeType.Segment))
            {
                throw new ArgumentException("All edges must be segments when calling the closed method");
            }

#endif
            #endregion

            List<int> indices = new List<int>();
            Vector3[] points = edges[0].AllEdgePoints;

            // Set up a sequence to avoid duplicating logic when looping back from the last edge to first
            var edgePairs = Enumerable.Range(0, edges.Length - 1).
                Select(o => Tuple.Create(edges[o], edges[o + 1])).
                Concat(new[] { Tuple.Create(edges[edges.Length - 1], edges[0]) });

            foreach (Tuple<Edge3D, Edge3D> pair in edgePairs)
            {
                // Add the point from edge1 that is shared with edge2
                int commonIndex = Edge3D.GetCommonIndex(pair.Item1, pair.Item2);
                if (commonIndex < 0)
                {
                    // While in this main loop, there can't be any breaks
                    throw new ApplicationException("Didn't find common point between edges");
                }
                else
                {
                    indices.Add(commonIndex);
                }
            }

            return (indices.ToArray(), points);
        }
        private (int[] indices, Vector3[] allPoints) GetPolygon_Open(Edge3D[] edges, float rayLength)
        {
            #region asserts
#if DEBUG

            if (edges.Length < 2)
            {
                throw new ArgumentException("Must have at least two edges: " + edges.Length.ToString());
            }

            if (edges[0].EdgeType != EdgeType.Ray || edges[edges.Length - 1].EdgeType != EdgeType.Ray)
            {
                throw new ArgumentException("First and last edges must be rays when calling the open method");
            }

            if (Enumerable.Range(1, edges.Length - 2).Any(o => edges[o].EdgeType != EdgeType.Segment))
            {
                throw new ArgumentException("Middle edges must be segments when calling the open method");
            }

#endif
            #endregion

            List<int> indices = new List<int>();

            var points = new List<Vector3>(edges[0].AllEdgePoints);       // two more will be added to the list for the rays 
            points.Add(edges[0].GetPoint1Ext(rayLength));
            points.Add(edges[edges.Length - 1].GetPoint1Ext(rayLength));

            // The end of the first ray
            indices.Add(points.Count - 2);

            foreach (var pair in Enumerable.Range(0, edges.Length - 1).Select(o => (edges[o], edges[o + 1])))
            {
                // Add the point from edge1 that is shared with edge2
                int commonIndex = Edge3D.GetCommonIndex(pair.Item1, pair.Item2);
                if (commonIndex < 0)
                {
                    // While in this main loop, there can't be any breaks
                    throw new ApplicationException("Didn't find common point between edges");
                }
                else
                {
                    indices.Add(commonIndex);
                }
            }

            // The end of the last ray
            indices.Add(points.Count - 1);

            return (indices.ToArray(), points.ToArray());
        }

        #endregion
    }

    #endregion
    #region class: Edge3D

    /// <summary>
    /// This represents a line in 3D (either a line segment, ray, or infinite line)
    /// </summary>
    /// <remarks>
    /// I decided to take an array of points, and store indexes into that array.  That makes it easier to compare points across
    /// multiple edges to see which ones are using the same points (int comparisons are exact, doubles are iffy)
    /// </remarks>
    public class Edge3D
    {
        #region Declaration Section

        private readonly object _lock = new object();

        #endregion

        #region Constructor

        public Edge3D(Vector3 point0, Vector3 point1)
        {
            this.EdgeType = EdgeType.Segment;
            this.Index0 = 0;
            this.Index1 = 1;
            this.Direction = null;
            this.AllEdgePoints = new Vector3[] { point0, point1 };
        }
        public Edge3D(int index0, int index1, Vector3[] allEdgePoints)
        {
            this.EdgeType = EdgeType.Segment;
            this.Index0 = index0;
            this.Index1 = index1;
            this.Direction = null;
            this.AllEdgePoints = allEdgePoints;
        }
        public Edge3D(EdgeType edgeType, int index0, Vector3 direction, Vector3[] allEdgePoints)
        {
            if (edgeType == EdgeType.Segment)
            {
                throw new ArgumentException("This overload requires edge type to be Ray or Line, not Segment");
            }

            this.EdgeType = edgeType;
            this.Index0 = index0;
            this.Index1 = null;
            this.Direction = direction;
            this.AllEdgePoints = allEdgePoints;
        }

        #endregion

        /// <summary>
        /// This tells what type of line this edge represents
        /// </summary>
        /// <remarks>
        /// Segment:
        ///     Index0, Index1 will be populated
        /// 
        /// Ray:
        ///     Index0, Direction will be populated
        ///     
        /// Line:
        ///     Index0, Direction will be populated, but to get the full line, use the opposite of direction as well
        /// </remarks>
        public readonly EdgeType EdgeType;

        public readonly int Index0;
        public readonly int? Index1;

        public readonly Vector3? Direction;
        /// <summary>
        /// This either returns Direction (if the edge is a ray or line), or it returns Point1 - Point0
        /// (this is helpful if you just want to treat the edge like a ray)
        /// </summary>
        public Vector3 DirectionExt
        {
            get
            {
                if (this.Direction != null)
                {
                    return this.Direction.Value;
                }
                else
                {
                    return this.Point1.Value - this.Point0;
                }
            }
        }

        public Vector3 Point0
        {
            get
            {
                return this.AllEdgePoints[this.Index0];
            }
        }
        public Vector3? Point1
        {
            get
            {
                if (this.Index1 == null)
                {
                    return null;
                }

                return this.AllEdgePoints[this.Index1.Value];
            }
        }
        /// <summary>
        /// This either returns Point1 (if the edge is a segment), or it returns Point0 + Direction
        /// (this is helpful if you just want to always treat the edge like a segment)
        /// </summary>
        public Vector3 Point1Ext
        {
            get
            {
                if (this.Point1 != null)
                {
                    return this.Point1.Value;
                }
                else
                {
                    return this.Point0 + this.Direction.Value;
                }
            }
        }

        public readonly Vector3[] AllEdgePoints;

        private long? _token = null;
        public long Token
        {
            get
            {
                lock (_lock)
                {
                    if (_token == null)
                    {
                        _token = TokenGenerator.NextToken();
                    }

                    return _token.Value;
                }
            }
        }

        #region Public Methods

        public Vector3 GetPoint(int index)
        {
            if (Index0 == index)
            {
                return Point0;
            }
            else if (Index1 != null && Index1.Value == index)
            {
                return Point1.Value;
            }
            else
            {
                throw new ArgumentOutOfRangeException("index", index, "Index not found");
            }
        }

        /// <summary>
        /// This takes a bunch of edges that were built independently of each other, and make them share the
        /// same set of allpoints
        /// </summary>
        public static Edge3D[] Clone_DedupePoints(Edge3D[] edges)
        {
            #region build allpoints

            var allPoints = new List<Vector3>();
            int[] index0 = new int[edges.Length];
            int?[] index1 = new int?[edges.Length];

            for (int cntr = 0; cntr < edges.Length; cntr++)
            {
                // Point 0
                int index = Math3D.IndexOf(allPoints, edges[cntr].Point0);
                if (index < 0)
                {
                    allPoints.Add(edges[cntr].Point0);
                    index = allPoints.Count - 1;
                }

                index0[cntr] = index;

                // Point 1
                if (edges[cntr].Point1 != null)
                {
                    index = Math3D.IndexOf(allPoints, edges[cntr].Point1.Value);
                    if (index < 0)
                    {
                        allPoints.Add(edges[cntr].Point1.Value);
                        index = allPoints.Count - 1;
                    }

                    index1[cntr] = index;
                }
            }

            Vector3[] allPointArr = allPoints.ToArray();

            #endregion

            #region build return

            Edge3D[] retVal = new Edge3D[edges.Length];

            for (int cntr = 0; cntr < edges.Length; cntr++)
            {
                if (index1[cntr] != null)
                {
                    if (edges[cntr].EdgeType != EdgeType.Segment)
                    {
                        throw new ApplicationException(string.Format("Invalid segment type: {0}.  Expected {1}", edges[cntr].EdgeType, EdgeType.Segment));
                    }

                    retVal[cntr] = new Edge3D(index0[cntr], index1[cntr].Value, allPointArr);
                }
                else
                {
                    if (edges[cntr].EdgeType == EdgeType.Segment)
                    {
                        throw new ApplicationException(string.Format("Invalid segment type: {0}", edges[cntr].EdgeType, EdgeType.Segment));
                    }

                    if (edges[cntr].Direction == null)
                    {
                        throw new ApplicationException(string.Format("Direction shouldn't be null for {0}", edges[cntr].EdgeType));
                    }

                    retVal[cntr] = new Edge3D(edges[cntr].EdgeType, index0[cntr], edges[cntr].Direction.Value, allPointArr);
                }
            }

            #endregion

            return retVal;
        }

        /// <summary>
        /// This finds all unique lines, and converts them into line segments
        /// NOTE: This returns more points than edges[0].AllEdgePoints, because it creates points for rays
        /// </summary>
        public static ((int, int)[] lines, Vector3[] allPoints) GetUniqueLines(Edge3D[] edges, float? rayLength = null)
        {
            if (edges.Length == 0)
            {
                return (new (int, int)[0], new Vector3[0]);
            }

            // Dedupe the edges
            Edge3D[] uniqueEdges = GetUniqueLines(edges);

            var segments = new List<(int, int)>();
            var points = new List<Vector3>();

            // Segments
            foreach (Edge3D segment in uniqueEdges.Where(o => o.EdgeType == EdgeType.Segment))
            {
                if (points.Count == 0)
                {
                    points.AddRange(segment.AllEdgePoints);
                }

                segments.Add((segment.Index0, segment.Index1.Value));
            }

            // Rays
            foreach (Edge3D ray in uniqueEdges.Where(o => o.EdgeType == EdgeType.Ray))
            {
                points.Add(ray.GetPoint1Ext(rayLength));

                segments.Add((ray.Index0, points.Count - 1));
            }

            return (segments.ToArray(), points.ToArray());
        }

        public static Edge3D[] GetUniqueLines(Edge3D[] edges)
        {
            List<Edge3D> retVal = new List<Edge3D>();

            // Interior edges
            retVal.AddRange(edges.
                Where(o => o.EdgeType == EdgeType.Segment).
                Select(o => new
                {
                    Key = Tuple.Create(Math.Min(o.Index0, o.Index1.Value), Math.Max(o.Index0, o.Index1.Value)),
                    Edge = o,
                }).
                Distinct(o => o.Key).
                Select(o => o.Edge));

            // Rays
            var rays = edges.
                Where(o => o.EdgeType == EdgeType.Ray).
                Select(o => new
                {
                    Ray = o,
                    DirectionUnit = o.Direction.Value.normalized
                }).
                ToLookup(o => o.Ray.Index0);

            foreach (var raySet in rays)
            {
                // These are the rays off of a point.  Dedupe
                retVal.AddRange(raySet.
                    Distinct((o, p) => o.DirectionUnit.IsNearValue(p.DirectionUnit)).
                    Select(o => o.Ray));
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This is the same as Point1Ext get, but lets the user pass in how long the extension should be (if null is passed in
        /// just uses direction's length - which is what Point1Ext does)
        /// </summary>
        public Vector3 GetPoint1Ext(float? rayLength = null)
        {
            if (this.Point1 != null)
            {
                return this.Point1.Value;
            }
            else
            {
                if (rayLength == null)
                {
                    return this.Point0 + this.Direction.Value;
                }
                else
                {
                    return this.Point0 + (this.Direction.Value.normalized * rayLength.Value);
                }
            }
        }

        public Vector3 GetMidpointExt(float? rayLength = null)
        {
            return this.Point0 + ((GetPoint1Ext(rayLength) - this.Point0) * .5f);
        }

        /// <summary>
        /// This returns the point from edge that isn't in common with otherEdge (makes up a point if edge is a ray)
        /// </summary>
        public static Vector3 GetOtherPointExt(Edge3D edge, Edge3D otherEdge, float? rayLength = null)
        {
            int common = GetCommonIndex(edge, otherEdge);

            if (edge.Index0 == common)
            {
                return edge.GetPoint1Ext(rayLength);
            }
            else
            {
                return edge.Point0;
            }
        }

        /// <summary>
        /// This tells which edges are touching each of the other edges
        /// NOTE: This only looks at the ints.  It doesn't project lines
        /// </summary>
        public static int[][] GetTouchingEdges(Edge3D[] edges)
        {
            var retVal = new int[edges.Length][];

            for (int outer = 0; outer < edges.Length; outer++)
            {
                var touching = new List<int>();

                for (int inner = 0; inner < edges.Length; inner++)
                {
                    if (outer == inner)
                    {
                        continue;
                    }

                    if (IsTouching(edges[outer], edges[inner]))
                    {
                        touching.Add(inner);
                    }
                }

                retVal[outer] = touching.ToArray();
            }

            return retVal;
        }

        /// <summary>
        /// This returns whether the two edges touch
        /// NOTE: It only compares ints.  It doesn't check if lines cross each other
        /// </summary>
        public static bool IsTouching(Edge3D edge0, Edge3D edge1)
        {
            return GetCommonIndex(edge0, edge1) >= 0;
        }
        public static int GetCommonIndex(Edge3D edge0, Edge3D edge1)
        {
            //  All edge types have an index 0, so get that comparison out of the way
            if (edge0.Index0 == edge1.Index0)
            {
                return edge0.Index0;
            }

            if (edge0.EdgeType == EdgeType.Segment)
            {
                //  Extra check, since edge0 is a segment
                if (edge0.Index1.Value == edge1.Index0)
                {
                    return edge0.Index1.Value;
                }

                //  If edge1 is also a segment, then compare its endpoint to edge0's points
                if (edge1.EdgeType == EdgeType.Segment)
                {
                    if (edge1.Index1.Value == edge0.Index0)
                    {
                        return edge1.Index1.Value;
                    }
                    else if (edge1.Index1.Value == edge0.Index1.Value)
                    {
                        return edge1.Index1.Value;
                    }
                }
            }
            else if (edge1.EdgeType == EdgeType.Segment)
            {
                //  Edge1 is a segment, but edge0 isn't, so just need the single compare
                if (edge1.Index1.Value == edge0.Index0)
                {
                    return edge1.Index1.Value;
                }
            }

            //  No more compares needed (this method doesn't bother with projecting rays/lines to see if they intersect, that's left up to the caller if they need it)
            return -1;
        }

        /// <summary>
        /// This returns the point in common between the two edges, and vectors that represent rays coming out of
        /// that point
        /// </summary>
        /// <remarks>
        /// This will throw an exception if the edges don't share a common point
        /// 
        /// It doesn't matter if the edges are segments or rays (will bomb if either is a line)
        /// </remarks>
        public static (Vector3 point, Vector3 direction1, Vector3 direction2) GetRays(Edge3D edge0, Edge3D edge1)
        {
            if (edge0.EdgeType == EdgeType.Line || edge1.EdgeType == EdgeType.Line)
            {
                throw new ArgumentException("This method doesn't allow lines, only segments and rays");
            }

            int common = GetCommonIndex(edge0, edge1);
            if (common < 0)
            {
                throw new ArgumentException("The edges passed in don't share a common point");
            }

            return
            (
                edge0.AllEdgePoints[common],
                GetDirectionFromPoint(edge0, common),
                GetDirectionFromPoint(edge1, common)
            );
        }

        /// <summary>
        /// This returns the direction from the point at index passed in to the other point (rays only go one direction, but
        /// segments can go either, lines throw an exception)
        /// </summary>
        public static Vector3 GetDirectionFromPoint(Edge3D edge, int index)
        {
            switch (edge.EdgeType)
            {
                case EdgeType.Line:
                    throw new ArgumentException("This method doesn't make sense for lines");        //  because lines can go two directions

                case EdgeType.Ray:
                    #region Ray

                    if (edge.Index0 != index)
                    {
                        throw new ArgumentException("The index passed in doesn't belong to this edge");
                    }

                    return edge.Direction.Value;

                #endregion

                case EdgeType.Segment:
                    #region Segment

                    if (edge.Index0 == index)
                    {
                        return edge.Point1.Value - edge.Point0;
                    }
                    else if (edge.Index1.Value == index)
                    {
                        return edge.Point0 - edge.Point1.Value;
                    }
                    else
                    {
                        throw new ArgumentException("The index passed in doesn't belong to this edge");
                    }

                #endregion

                default:
                    throw new ApplicationException($"Unknown EdgeType: {edge.EdgeType}");
            }
        }

        public bool ContainsPoint(int index)
        {
            return this.Index0 == index || (this.Index1 != null && this.Index1.Value == index);
        }

        /// <summary>
        /// This is useful when looking at lists of edges in the quick watch
        /// </summary>
        public override string ToString()
        {
            const string DELIM = "       |       ";

            StringBuilder retVal = new StringBuilder(100);

            retVal.Append(this.EdgeType.ToString());
            retVal.Append(DELIM);

            switch (this.EdgeType)
            {
                case EdgeType.Segment:
                    retVal.Append(string.Format("{0} - {1}{2}({3}) ({4})",
                        this.Index0,
                        this.Index1,
                        DELIM,
                        this.Point0.ToStringSignificantDigits(2),
                        this.Point1.Value.ToStringSignificantDigits(2)));
                    break;

                case EdgeType.Ray:
                    retVal.Append(string.Format("{0}{1}({2}) --> ({3})",
                        this.Index0,
                        DELIM,
                        this.Point0.ToStringSignificantDigits(2),
                        this.Direction.Value.ToStringSignificantDigits(2)));
                    break;

                case EdgeType.Line:
                    retVal.Append(string.Format("{0}{1}({2}) <---> ({3})",
                        this.Index0,
                        DELIM,
                        this.Point0.ToStringSignificantDigits(2),
                        this.Direction.Value.ToStringSignificantDigits(2)));
                    break;

                default:
                    retVal.Append("Unknown EdgeType");
                    break;
            }

            return retVal.ToString();
        }

        #endregion
    }

    #endregion
}
