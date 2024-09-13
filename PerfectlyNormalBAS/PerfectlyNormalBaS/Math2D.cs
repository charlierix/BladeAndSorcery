using GeneticSharp.Domain;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Infrastructure.Framework.Threading;
using PerfectlyNormalBaS.Clipper;
using PerfectlyNormalBaS.GeneticSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalBaS
{
    //NOTE: This was originally written for wpf, so Z is up.  But in unity, Y is up.  This class still rotates 3D points onto the XY plane, where unity probably thinks in the XZ plane
    //TODO: May want to change that in the future
    public static class Math2D
    {
        #region class: SutherlandHodgman

        /// <remarks>
        /// Put this here:
        /// http://rosettacode.org/wiki/Sutherland-Hodgman_polygon_clipping#C.23
        /// </remarks>
        private static class SutherlandHodgman
        {
            /// <summary>
            /// This clips the subject polygon against the clip polygon (gets the intersection of the two polygons)
            /// </summary>
            /// <remarks>
            /// Based on the psuedocode from:
            /// http://en.wikipedia.org/wiki/Sutherland%E2%80%93Hodgman
            /// </remarks>
            /// <param name="subjectPoly">Can be concave or convex</param>
            /// <param name="clipPoly">Must be convex</param>
            /// <returns>The intersection of the two polygons (or null)</returns>
            public static Vector2[] GetIntersectedPolygon(Vector2[] subjectPoly, Vector2[] clipPoly)
            {
                if (subjectPoly.Length < 3 || clipPoly.Length < 3)
                {
                    throw new ArgumentException(string.Format("The polygons passed in must have at least 3 points: subject={0}, clip={1}", subjectPoly.Length.ToString(), clipPoly.Length.ToString()));
                }

                var outputList = subjectPoly.ToList();

                // Make sure it's clockwise
                if (!Math2D.IsClockwise(subjectPoly))
                {
                    outputList.Reverse();
                }

                // Walk around the clip polygon clockwise
                foreach (Edge2D clipEdge in Math2D.IterateEdges(clipPoly, true))
                {
                    var inputList = outputList.ToList();		// clone it
                    outputList.Clear();

                    if (inputList.Count == 0)
                    {
                        // Sometimes when the polygons don't intersect, this list goes to zero.  Jump out to avoid an index out of range exception
                        break;
                    }

                    Vector2 S = inputList[inputList.Count - 1];

                    foreach (Vector2 E in inputList)
                    {
                        if (IsInside(clipEdge, E))
                        {
                            if (!IsInside(clipEdge, S))
                            {
                                Vector2? point = Math2D.GetIntersection_Line_Line(S, E, clipEdge.Point0, clipEdge.Point1.Value);
                                if (point == null)
                                {
                                    //throw new ApplicationException("Line segments don't intersect");		// may be colinear, or may be a bug
                                    return null;
                                }
                                else
                                {
                                    outputList.Add(point.Value);
                                }
                            }

                            outputList.Add(E);
                        }
                        else if (IsInside(clipEdge, S))
                        {
                            Vector2? point = Math2D.GetIntersection_Line_Line(S, E, clipEdge.Point0, clipEdge.Point1.Value);
                            if (point == null)
                            {
                                //throw new ApplicationException("Line segments don't intersect");		// may be colinear, or may be a bug
                                return null;        // this is hitting on a case where the subject and clip share an edge, but don't really intersect (just touch).  Plus, subject had 3 points that were colinear (plus a fourth that made it a proper polygon)
                            }
                            else
                            {
                                outputList.Add(point.Value);
                            }
                        }

                        S = E;
                    }
                }

                // Dedupe
                outputList = Math2D.DedupePoints(outputList);

                if (outputList.Count < 3)       // only one or two points is just the two polygons touching, not an intersection that creates a new polygon
                {
                    return null;
                }
                else
                {
                    return outputList.ToArray();
                }
            }

            #region Private Methods

            private static bool IsInside(Edge2D edge, Vector2 test)
            {
                bool? isLeft = Math2D.IsLeftOf(edge, test);
                if (isLeft == null)
                {
                    // Colinear points should be considered inside
                    return true;
                }

                return !isLeft.Value;
            }

            #endregion
        }

        #endregion
        #region class: QuickHull2D

        /// <remarks>
        /// Got this here (ported it from java):
        /// http://www.ahristov.com/tutorial/geometry-games/convex-hull.html
        /// </remarks>
        internal static class QuickHull2D
        {
            public static QuickHull2DResult GetConvexHull(Vector3[] points)
            {
                Matrix4x4 transformTo2D = Matrix4x4.identity;
                Matrix4x4 transformTo3D = Matrix4x4.identity;

                // If there are less than three points, just return everything
                if (points.Length == 0)
                {
                    return new QuickHull2DResult(new Vector2[0], new int[0], Matrix4x4.identity, Matrix4x4.identity);
                }
                else if (points.Length == 1)
                {
                    return new QuickHull2DResult(
                        new Vector2[] { new Vector2(points[0].x, points[0].y) },
                        new int[] { 0 },
                        Matrix4x4.TRS(new Vector3(0, 0, -points[0].z), Quaternion.identity, Vector3.one),
                        Matrix4x4.TRS(new Vector3(0, 0, points[0].z), Quaternion.identity, Vector3.one));
                }
                else if (points.Length == 2)
                {
                    Vector2[] transformedPoints = GetRotatedPoints(out transformTo2D, out transformTo3D, points[0], points[1]);
                    return new QuickHull2DResult(transformedPoints, new int[] { 0, 1 }, transformTo2D, transformTo3D);
                }

                Vector2[] points2D = null;
                if (points.All(o => o.z.IsNearValue(points[0].z)))
                {
                    // They are already in the xy plane
                    points2D = points.
                        Select(o => new Vector2(o.x, o.y)).
                        ToArray();

                    if (!points[0].z.IsNearZero())
                    {
                        transformTo2D = Matrix4x4.TRS(new Vector3(0, 0, -points[0].z), Quaternion.identity, Vector3.one);
                        transformTo3D = Matrix4x4.TRS(new Vector3(0, 0, points[0].z), Quaternion.identity, Vector3.one);
                    }
                }
                else
                {
                    // Rotate the points so that Z drops out (and make sure they are all coplanar)
                    points2D = GetRotatedPoints(out transformTo2D, out transformTo3D, points, true);
                }

                if (points2D == null)
                {
                    return null;
                }

                // Call quickhull
                QuickHull2DResult retVal = GetConvexHull(points2D);
                return new QuickHull2DResult(retVal.Points, retVal.PerimiterLines, transformTo2D, transformTo3D);
            }
            public static QuickHull2DResult GetConvexHull(Vector2[] points)
            {
                if (points.Length < 3)
                {
                    return new QuickHull2DResult(points, Enumerable.Range(0, points.Length).ToArray(), Matrix4x4.identity, Matrix4x4.identity);		// return all the points
                }

                var retVal = new List<int>();
                var remainingPoints = Enumerable.Range(0, points.Length).ToList();

                #region find two most extreme points

                int minIndex = -1;
                int maxIndex = -1;

                double minX = double.MaxValue;
                double maxX = double.MinValue;

                for (int cntr = 0; cntr < points.Length; cntr++)
                {
                    if (points[cntr].x < minX)
                    {
                        minX = points[cntr].x;
                        minIndex = cntr;
                    }

                    if (points[cntr].x > maxX)
                    {
                        maxX = points[cntr].x;
                        maxIndex = cntr;
                    }
                }

                #endregion

                #region move points to return list

                retVal.Add(minIndex);
                retVal.Add(maxIndex);

                if (maxIndex > minIndex)
                {
                    remainingPoints.RemoveAt(maxIndex);		// need to remove the later index first so it doesn't shift
                    remainingPoints.RemoveAt(minIndex);
                }
                else
                {
                    remainingPoints.RemoveAt(minIndex);
                    remainingPoints.RemoveAt(maxIndex);
                }

                #endregion

                #region divide the list left and right of the line

                var leftSet = new List<int>();
                var rightSet = new List<int>();

                for (int cntr = 0; cntr < remainingPoints.Count; cntr++)
                {
                    if (IsRightOfLine(minIndex, maxIndex, remainingPoints[cntr], points))
                    {
                        rightSet.Add(remainingPoints[cntr]);
                    }
                    else
                    {
                        leftSet.Add(remainingPoints[cntr]);
                    }
                }

                #endregion

                // Process these sets recursively, adding to retVal
                HullSet(minIndex, maxIndex, rightSet, retVal, points);
                HullSet(maxIndex, minIndex, leftSet, retVal, points);

                return new QuickHull2DResult(points, retVal.ToArray(), Matrix4x4.identity, Matrix4x4.identity);
            }

            #region Private Methods

            private static void HullSet(int lineStart, int lineStop, List<int> set, List<int> hull, Vector2[] allPoints)
            {
                int insertPosition = hull.IndexOf(lineStop);

                if (set.Count == 0)
                {
                    return;
                }
                else if (set.Count == 1)
                {
                    hull.Insert(insertPosition, set[0]);
                    set.RemoveAt(0);
                    return;
                }

                #region find most distant point

                double maxDistance = double.MinValue;
                int farIndexIndex = -1;
                for (int cntr = 0; cntr < set.Count; cntr++)
                {
                    int point = set[cntr];
                    double distance = GetDistanceFromLineSquared(allPoints[lineStart], allPoints[lineStop], allPoints[point]);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        farIndexIndex = cntr;
                    }
                }

                // Move the point to the hull
                int farIndex = set[farIndexIndex];
                set.RemoveAt(farIndexIndex);
                hull.Insert(insertPosition, farIndex);

                #endregion

                #region find everything left of (start, far)

                var leftSet_Start_Far = new List<int>();

                for (int cntr = 0; cntr < set.Count; cntr++)
                {
                    int pointIndex = set[cntr];

                    if (IsRightOfLine(lineStart, farIndex, pointIndex, allPoints))
                    {
                        leftSet_Start_Far.Add(pointIndex);
                    }
                }

                #endregion

                #region find everything right of (far, stop)

                var leftSet_Far_Stop = new List<int>();

                for (int cntr = 0; cntr < set.Count; cntr++)
                {
                    int pointIndex = set[cntr];

                    if (IsRightOfLine(farIndex, lineStop, pointIndex, allPoints))
                    {
                        leftSet_Far_Stop.Add(pointIndex);
                    }
                }

                #endregion

                // Recurse
                //NOTE: The set passed in was split into these two sets
                HullSet(lineStart, farIndex, leftSet_Start_Far, hull, allPoints);
                HullSet(farIndex, lineStop, leftSet_Far_Stop, hull, allPoints);
            }

            internal static bool IsRightOfLine(int lineStart, int lineStop, int testPoint, Vector2[] allPoints)
            {
                double cp1 = ((allPoints[lineStop].x - allPoints[lineStart].x) * (allPoints[testPoint].y - allPoints[lineStart].y)) -
                            ((allPoints[lineStop].y - allPoints[lineStart].y) * (allPoints[testPoint].x - allPoints[lineStart].x));

                return cp1 > 0;
                //return (cp1 > 0) ? 1 : -1;
            }
            internal static bool IsRightOfLine(int lineStart, int lineStop, Vector2 testPoint, Vector2[] allPoints)
            {
                double cp1 = ((allPoints[lineStop].x - allPoints[lineStart].x) * (testPoint.y - allPoints[lineStart].y)) -
                            ((allPoints[lineStop].y - allPoints[lineStart].y) * (testPoint.x - allPoints[lineStart].x));

                return cp1 > 0;
                //return (cp1 > 0) ? 1 : -1;
            }
            internal static bool IsRightOfLine(Vector2 lineStart, Vector2 lineStop, Vector2 testPoint)
            {
                double cp1 = ((lineStop.x - lineStart.x) * (testPoint.y - lineStart.y)) -
                            ((lineStop.y - lineStart.y) * (testPoint.x - lineStart.x));

                return cp1 > 0;
                //return (cp1 > 0) ? 1 : -1;
            }

            private static float GetDistanceFromLineSquared(Vector2 lineStart, Vector2 lineStop, Vector2 testPoint)
            {
                Vector3 pointOnLine = new Vector3(lineStart.x, lineStart.y, 0f);
                Vector3 lineDirection = new Vector3(lineStop.x, lineStop.y, 0f) - pointOnLine;
                Vector3 point = new Vector3(testPoint.x, testPoint.y, 0f);

                Vector3 nearestPoint = Math3D.GetClosestPoint_Line_Point(new Ray(pointOnLine, lineDirection), point);

                return (point - nearestPoint).sqrMagnitude;
            }

            #endregion
        }

        #endregion
        #region class: AveragePlane

        private static class AveragePlane
        {
            /// <remarks>
            /// I spent a couple days reading articles, trying to get a better solution.  Pages of math, or lots of cryptic code
            /// 
            /// There is a way to get an exact answer.  It becomes a system of linear equations (or something).
            /// 
            /// The problem is, a lot of the solutions assume that z is a function of x and y (so no asymptotes along z).  Then they take
            /// shortcuts with distance from the line to be distance along the z axis, instead of the distance perp to the plane.
            /// 
            /// This method takes the easy way out, and chooses a bunch of random triangles, calculates the normal for those, then averages
            /// those normals together.  This is accurate enough if the points aren't too far from coplanar.
            /// 
            /// Here are some pages that I looked at:
            /// 
            /// Ransac is a method that throws away outlier samples, and averages the rest together
            /// http://www.timzaman.com/?p=190
            ///
            /// This is c++, but he was casting a float array to a char array, then just had if(charArray).  And of course no comments
            /// http://codesuppository.blogspot.com/2006/03/best-fit-plane.html
            /// http://codesuppository.blogspot.com/2009/06/holy-crap-my-veyr-own-charles.html
            /// https://code.google.com/p/codesuppository/source/browse/trunk/app/CodeSuppository/
            /// http://www.geometrictools.com/
            /// </remarks>
            public static Plane? GetAveragePlane(Vector3[] points, bool matchPolyNormalDirection = false)
            {
                if (points.Length < 3)
                {
                    return null;
                }

                // The plane will go through this center point
                Vector3 center = Math3D.GetCenter(points);

                // Take a bunch of triangles and get the average normal
                var retVal = Get_RandomTriangles(points, center, matchPolyNormalDirection);

                // The above method works fine when all the points are separated from each other and pretty close to the plane.
                // But if you have a lot of points clumped together, then tiny triangles can be chosen that don't line up with
                // the plane, or other inaccuracies can crop up.  This uses a genetic algorithm and scores based on the sum of
                // the point's distance from the plane.  It's slow (about a second to run) but is really accurate
                try
                {
                    return Improve_Genetic(retVal, points, center);
                }
                catch (DllNotFoundException)
                {
                    // Since GeneticSharp is a separate dll, it's possible that they didn't copy it, just return the crude average
                    return retVal;
                }
            }

            #region Private Methods - random triangles

            //TODO: When there are enough points, pull the random vertices from far away locations instead of pure random
            private static Plane? Get_RandomTriangles(Vector3[] points, Vector3 center, bool matchPolyNormalDirection = false)
            {
                // Get a bunch of sample up vectors
                Vector3[] upVectors = GetAveragePlane_UpVectors(points, matchPolyNormalDirection);
                if (upVectors.Length == 0)
                {
                    return null;
                }

                // Average them together to get a single normal
                Vector3 avgNormal = Math3D.GetAverage(upVectors);

                return new Plane(avgNormal, center);
            }

            /// <summary>
            /// This chooses a bunch of random triangles out of the points passed in, and returns their normals (all pointing the
            /// same direction)
            /// </summary>
            /// <remarks>
            /// This limits the number of returned vectors to 100.  Here is a small table of the number of triangles based
            /// on the number of points (it's roughly count^3)
            /// 
            /// 3 - 1
            /// 4 - 4
            /// 5 - 10
            /// 6 - 20
            /// 7 - 35
            /// 8 - 56
            /// 9 - 84
            /// 10 - 120
            /// 11 - 165
            /// 12 - 220
            /// 13 - 286
            /// 14 - 364
            /// 15 - 455
            /// 16 - 560
            /// 17 - 680
            /// 18 - 816
            /// 19 - 969
            /// 20 - 1140
            /// 21 - 1330
            /// 22 - 1540
            /// </remarks>
            /// <param name="matchPolyNormalDirection">
            /// True = They will point in the direction of the polygon's normal (only makes sense if the points represent a polygon, and that polygon is convex)
            /// False = The direction of the vectors returned is arbitrary (they will all point in the same direction, but it's random which direction that will be)
            /// </param>
            private static Vector3[] GetAveragePlane_UpVectors(Vector3[] points, bool matchPolyNormalDirection = false)
            {
                if (points.Length < 3)
                {
                    return new Vector3[0];
                }

                Vector3[] retVal;
                if (points.Length < 15)      //see the table in the remarks.  Even though 13 makes 364 triangles, it would be inneficient to randomly choose triangles, and throw out already attempted ones (I was looking for at least a 1:4 ratio - didn't do performance testing, just feels right)
                {
                    // Do them all
                    retVal = GetAveragePlane_UpVectors_All(points);
                }
                else
                {
                    // Randomly choose 100 points
                    retVal = GetAveragePlane_UpVectors_Sample(points, 100);

                    if (retVal.Length == 0)
                    {
                        #region lots of colinear

                        // The only way to get here is if the infinite loop detectors hit, which means there are a lot of colinear points (or identical
                        // points).  Before completely giving up, use the normal of any triangle formed by the points
                        try
                        {
                            Vector3[] nonDupedInitial = GetNonDupedInitialPoints(points);       // GetPolygonNormal throws an exception if the first two points are the same

                            Vector3 normal = Math2D.GetPolygonNormal(nonDupedInitial, PolygonNormalLength.DoesntMatter);

                            retVal = new[] { normal };
                        }
                        catch (Exception) { }       // the method throws exceptions if it can't get an answer

                        #endregion
                    }
                }

                if (retVal.Length == 0)
                {
                    return retVal;
                }

                // Make sure they are all pointing in the same direction
                GetAveragePlane_SameDirection(retVal, points, matchPolyNormalDirection);

                return retVal.ToArray();
            }

            private static Vector3[] GetAveragePlane_UpVectors_All(Vector3[] points)
            {
                var retVal = new List<Vector3>();

                for (int a = 0; a < points.Length - 2; a++)
                {
                    for (int b = a + 1; b < points.Length - 1; b++)
                    {
                        for (int c = b + 1; c < points.Length; c++)
                        {
                            GetAveragePlane_UpVectors_Vector(retVal, a, b, c, points);
                        }
                    }
                }

                return retVal.ToArray();
            }
            private static Vector3[] GetAveragePlane_UpVectors_Sample(Vector3[] points, int count)
            {
                var retVal = new List<Vector3>();
                var used = new SortedList<Tuple<int, int, int>, byte>();     // the value doesn't mean anything, I just wanted to keep the keys sorted

                var rand = StaticRandom.GetRandomForThread();
                int pointsLen = points.Length;      // not sure if there is a cost to hitting the length property, but this method would hit it a lot

                int infiniteLoopCntr1 = 0;
                int[] indices = new int[3];

                while (retVal.Count < count && infiniteLoopCntr1 < 40)
                {
                    int infiniteLoopCntr2 = 0;
                    Tuple<int, int, int> triangle = null;
                    while (infiniteLoopCntr2 < 1000)
                    {
                        infiniteLoopCntr2++;

                        // Choose 3 random points
                        indices[0] = rand.Next(pointsLen);
                        indices[1] = rand.Next(pointsLen);
                        indices[2] = rand.Next(pointsLen);

                        // Make sure they are unique
                        if (indices[0] == indices[1] || indices[0] == indices[2])
                        {
                            continue;
                        }

                        // Generate the key (the inidividual indices need to be sorted so that { 1,2,3 | 1,3,2 | 2,1,3 | 2,3,1 | 3,1,2 | 3,2,1 } would all be considered the same key)
                        Array.Sort(indices);
                        triangle = Tuple.Create(indices[0], indices[1], indices[2]);

                        // Make sure this hasn't been tried before
                        if (used.ContainsKey(triangle))
                        {
                            continue;
                        }

                        used.Add(triangle, 0);

                        // triangle is valid and not attempted yet, break out of this inner loop
                        break;
                    }

                    // Get the normal of triangle
                    if (GetAveragePlane_UpVectors_Vector(retVal, triangle.Item1, triangle.Item2, triangle.Item3, points))
                    {
                        infiniteLoopCntr1 = 0;
                    }
                    else
                    {
                        // The points are colinear.  If there are too many in a row, just fail
                        infiniteLoopCntr1++;
                    }
                }

                return retVal.ToArray();
            }

            private static bool GetAveragePlane_UpVectors_Vector(List<Vector3> returnVectors, int index1, int index2, int index3, Vector3[] points)
            {
                Vector3 cross = Vector3.Cross(points[index2] - points[index1], points[index3] - points[index1]);
                if (!cross.IsInvalid() && !cross.IsNearZero())        // there may be colinear points
                {
                    returnVectors.Add(cross);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            private static void GetAveragePlane_SameDirection(Vector3[] vectors, Vector3[] points, bool matchPolyNormalDirection)
            {
                // Get the vector to compare with
                int start;
                Vector3 match;
                if (matchPolyNormalDirection)
                {
                    start = 0;
                    match = Math2D.GetPolygonNormal(points, PolygonNormalLength.DoesntMatter);
                }
                else
                {
                    start = 1;
                    match = vectors[0];
                }

                // Make sure all the vectors match that direction
                for (int cntr = start; cntr < vectors.Length; cntr++)
                {
                    if (Vector3.Dot(vectors[cntr], match) < 0)
                    {
                        vectors[cntr] = -vectors[cntr];
                    }
                }
            }

            /// <summary>
            /// This is an ugly, hardcoded method that makes sure the first two points aren't identical
            /// </summary>
            private static Vector3[] GetNonDupedInitialPoints(Vector3[] points)
            {
                if (points.Length == 0)
                {
                    return new Vector3[0];
                }

                int startIndex = 1;
                while (startIndex < points.Length)
                {
                    if (!points[0].IsNearValue(points[startIndex]))
                    {
                        // Found a non identical point at startIndex
                        break;
                    }

                    startIndex++;
                }

                if (startIndex >= points.Length)
                {
                    // All the points in the array are the same, just return the first point
                    return new Vector3[] { points[0] };
                }
                else if (startIndex == 1)
                {
                    // There is nothing to cut out, just return what was passed in
                    return points;
                }

                // Cut out the points that are the same as the first
                var retVal = new List<Vector3>();
                retVal.Add(points[0]);
                retVal.AddRange(points.Skip(startIndex));

                return retVal.ToArray();
            }

            #endregion
            #region Private Methods - genetic

            private static Plane Improve_Genetic(Plane? current, Vector3[] points, Vector3 center)
            {
                Vector3 initial_norm = current == null ?
                    Vector3.one :
                    current.Value.normal.normalized;

                Vector3 initial_pos = current == null ?
                    Vector3.zero :
                    Math3D.GetClosestPoint_Plane_Point(current.Value, center);

                var aabb = Math3D.GetAABB(points);

                var chromosome = FloatingPointChromosome2.Create(
                    new double[] { -3, -3, -3, aabb.min.x, aabb.min.y, aabb.min.z },
                    new double[] { 3, 3, 3, aabb.max.x, aabb.max.y, aabb.max.z },
                    new int[]
                    {
                        5,
                        5,
                        5,
                        GeneticSharpUtil.GetNumDecimalPlaces(3, aabb.min.x, aabb.max.x),
                        GeneticSharpUtil.GetNumDecimalPlaces(3, aabb.min.y, aabb.max.y),
                        GeneticSharpUtil.GetNumDecimalPlaces(3, aabb.min.z, aabb.max.z),
                    },
                    new double[] { initial_norm.x, initial_norm.y, initial_norm.z, initial_pos.x, initial_pos.y, initial_pos.z });

                var population = new Population(72, 144, chromosome);

                double maxError = ((aabb.max - aabb.min).magnitude * points.Length) * 1.5;

                var fitness = new FuncFitness(c =>
                {
                    var fc = c as FloatingPointChromosome2;
                    var vals = fc.ToFloatingPoints();

                    var plain = new Plane(new Vector3((float)vals[0], (float)vals[1], (float)vals[2]), new Vector3((float)vals[3], (float)vals[4], (float)vals[5]));
                    double error = points.Sum(o => Math.Abs(plain.GetDistanceToPoint(o)));

                    if (error > maxError)
                        throw new ApplicationException("we're going to need a bigger boat");

                    return maxError - error;       // need to return in a format where largest value wins
                });

                var selection = new EliteSelection();       // the larger the score, the better

                var crossover = new UniformCrossover(0.5f);     // .5 will pull half from each parent

                var mutation = new FlipBitMutation();       // FloatingPointChromosome inherits from BinaryChromosomeBase, which is a series of bits.  This mutator will flip random bits

                var termination = new FitnessStagnationTermination(144);        // keeps going until it generates the same winner this many generations in a row

                var taskExecutor = new ParallelTaskExecutor();

                var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
                {
                    TaskExecutor = taskExecutor,
                    Termination = termination,
                };

                ga.Start();

                var bestChromosome = ga.BestChromosome as FloatingPointChromosome2;
                double[] values = bestChromosome.ToFloatingPoints();

                var plane = new Plane(new Vector3((float)values[0], (float)values[1], (float)values[2]), new Vector3((float)values[3], (float)values[4], (float)values[5]));

                Vector3 final_norm = plane.normal.normalized;
                Vector3 final_pos = Math3D.GetClosestPoint_Plane_Point(plane, center);      // the plane's center probably drifted, force it to be near the center of the sample points
                if (Vector3.Dot(initial_norm, final_norm) < 0)
                    final_norm = -final_norm;       // the normal flipped, flip it back

                return new Plane(final_norm, final_pos);
            }

            #endregion
        }

        #endregion
        #region class: PointsSingleton

        private class PointsSingleton
        {
            #region Declaration Section

            private static readonly object _lockStatic = new object();
            private readonly object _lockInstance;

            /// <summary>
            /// The static constructor makes sure that this instance is created only once.  The outside users of this class
            /// call the static property Instance to get this one instance copy.  (then they can use the rest of the instance
            /// methods)
            /// </summary>
            private static PointsSingleton _instance;

            private SortedList<int, Vector2[]> _points;

            #endregion

            #region Constructor / Instance Property

            /// <summary>
            /// Static constructor.  Called only once before the first time you use my static properties/methods.
            /// </summary>
            static PointsSingleton()
            {
                lock (_lockStatic)
                {
                    // If the instance version of this class hasn't been instantiated yet, then do so
                    if (_instance == null)
                    {
                        _instance = new PointsSingleton();
                    }
                }
            }
            /// <summary>
            /// Instance constructor.  This is called only once by one of the calls from my static constructor.
            /// </summary>
            private PointsSingleton()
            {
                _lockInstance = new object();

                _points = new SortedList<int, Vector2[]>();
            }

            /// <summary>
            /// This is how you get at my instance.  The act of calling this property guarantees that the static constructor gets called
            /// exactly once (per process?)
            /// </summary>
            public static PointsSingleton Instance
            {
                get
                {
                    // There is no need to check the static lock, because _instance is only set one time, and that is guaranteed to be
                    // finished before this function gets called
                    return _instance;
                }
            }

            #endregion

            #region Public Methods

            public Vector2[] GetPoints(int numSides)
            {
                lock (_lockInstance)
                {
                    if (!_points.ContainsKey(numSides))
                    {
                        float deltaTheta = 2f * Mathf.PI / numSides;
                        float theta = 0f;

                        Vector2[] points = new Vector2[numSides];		// these define a unit circle

                        for (int cntr = 0; cntr < numSides; cntr++)
                        {
                            points[cntr] = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
                            theta += deltaTheta;
                        }

                        _points.Add(numSides, points);
                    }

                    return _points[numSides];
                }
            }

            #endregion
        }

        #endregion

        #region misc

        /// <summary>
        /// This returns the center of position of the points
        /// </summary>
        public static Vector2 GetCenter(IEnumerable<Vector2> points)
        {
            if (points == null)
            {
                return Vector2.zero;
            }

            float x = 0f;
            float y = 0f;

            int length = 0;

            foreach (Vector2 point in points)
            {
                x += point.x;
                y += point.y;

                length++;
            }

            if (length == 0)
            {
                return Vector2.zero;
            }

            float oneOverLen = 1f / Convert.ToSingle(length);

            return new Vector2(x * oneOverLen, y * oneOverLen);
        }
        /// <summary>
        /// This returns the center of mass of the points
        /// </summary>
        public static Vector2 GetCenter((Vector2 pos, float mass)[] pointsMasses)
        {
            if (pointsMasses == null || pointsMasses.Length == 0)
            {
                return new Vector2(0, 0);
            }

            float totalMass = pointsMasses.Sum(o => o.Item2);
            if (totalMass.IsNearZero())
            {
                return GetCenter(pointsMasses.Select(o => o.pos).ToArray());
            }

            float x = 0f;
            float y = 0f;

            foreach (var pointMass in pointsMasses)
            {
                x += pointMass.pos.x * pointMass.Item2;
                y += pointMass.pos.y * pointMass.Item2;
            }

            float totalMassInverse = 1f / totalMass;

            return new Vector2(x * totalMassInverse, y * totalMassInverse);
        }

        /// <summary>
        /// This is identical to GetCenter.  (with points, that is thought of as the center.  With vectors, that's thought of as the
        /// average - even though it's the same logic)
        /// </summary>
        public static Vector2 GetAverage(IEnumerable<Vector2> vectors)
        {
            return GetCenter(vectors);
        }

        public static Vector2 GetSum(IEnumerable<Vector2> vectors)
        {
            if (vectors == null)
            {
                return Vector2.zero;
            }

            float x = 0f;
            float y = 0f;

            foreach (Vector2 vector in vectors)
            {
                x += vector.x;
                y += vector.y;
            }

            return new Vector2(x, y);
        }

        public static Vector2[] GetUnique(IEnumerable<Vector2> vectors)
        {
            List<Vector2> retVal = new List<Vector2>();

            foreach (Vector2 vector in vectors)
            {
                if (!retVal.Any(o => o.IsNearValue(vector)))
                {
                    retVal.Add(vector);
                }
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This gets the area of any polygon as long as edges don't cross over (like a 4 point creating a bow tie)
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://www.wikihow.com/Calculate-the-Area-of-a-Polygon
        /// http://www.mathopenref.com/coordpolygonarea.html
        /// 
        /// What a crazy algorithm.  It even works with negative coordinates
        /// </remarks>
        public static float GetAreaPolygon(Vector2[] polygon)
        {
            float sum = 0f;

            for (int cntr = 0; cntr < polygon.Length - 1; cntr++)
            {
                sum += (polygon[cntr].x * polygon[cntr + 1].y) - (polygon[cntr].y * polygon[cntr + 1].x);
            }

            int last = polygon.Length - 1;
            sum += (polygon[last].x * polygon[0].y) - (polygon[last].y * polygon[0].x);

            return Math.Abs(sum) / 2f;
        }
        public static float GetAreaPolygon(Vector3[] polygon)
        {
            Vector3 normal = GetPolygonNormal(polygon, PolygonNormalLength.DoesntMatter);
            return GetAreaPolygon(polygon, normal);
        }
        public static float GetAreaPolygon(Vector3[] polygon, Vector3 normal)
        {
            // Rotate into 2D
            Quaternion rotateTo2D = Quaternion.FromToRotation(normal, new Vector3(0, 0, 1));

            var poly2D = new Vector2[polygon.Length];

            for (int cntr = 0; cntr < polygon.Length; cntr++)
            {
                Vector3 rotated = rotateTo2D * polygon[cntr];
                poly2D[cntr] = new Vector2(rotated.x, rotated.y);
            }

            // Get area using transformed 2D points
            return Math2D.GetAreaPolygon(poly2D);
        }

        public static Vector3 GetPolygonNormal(Vector2[] polygon, PolygonNormalLength returnLength)
        {
            //NOTE: Even though this is a copy of the 3D overload's code, it would be inneficient to convert all points to 3D just to call
            //that overload (especially if they request PolygonNormalLength.PolygonArea)

            if (polygon.Length < 3)
            {
                throw new ArgumentException("The polygon passed in must have at least 3 points");
            }

            Vector3 direction1 = polygon[1] - polygon[0];
            if (direction1.IsNearZero())
            {
                throw new ApplicationException("First two points are identical");
            }

            Vector3? retVal = null;

            // Can't just blindly use the first three points, they could be colinear
            for (int cntr = 1; cntr < polygon.Length - 1; cntr++)
            {
                Vector3 direction2 = polygon[cntr + 1] - polygon[cntr];

                retVal = Vector3.Cross(direction1, direction2);

                if (!retVal.Value.IsInvalid() && !retVal.Value.IsNearZero())        // it will be invalid or zero if the vectors are colinear
                {
                    break;
                }
            }

            if (retVal == null)
            {
                throw new ArgumentException("The points in the polygon are colinear");
            }

            switch (returnLength)
            {
                case PolygonNormalLength.DoesntMatter:
                    break;

                case PolygonNormalLength.Unit:
                    retVal = retVal.Value.normalized;
                    break;

                case PolygonNormalLength.PolygonArea:
                    retVal = retVal.Value.normalized * GetAreaPolygon(polygon);
                    break;

                default:
                    throw new ApplicationException("Unknown PolygonNormalLength: " + returnLength.ToString());
            }

            return retVal.Value;
        }
        public static Vector3 GetPolygonNormal(Vector3[] polygon, PolygonNormalLength returnLength)
        {
            if (polygon.Length < 3)
            {
                throw new ArgumentException("The polygon passed in must have at least 3 points");
            }

            Vector3 direction1 = polygon[1] - polygon[0];
            if (direction1.IsNearZero())
            {
                throw new ApplicationException("First two points are identical");
            }

            Vector3? retVal = null;

            // Can't just blindly use the first three points, they could be colinear
            for (int cntr = 1; cntr < polygon.Length - 1; cntr++)
            {
                Vector3 direction2 = polygon[cntr + 1] - polygon[cntr];

                retVal = Vector3.Cross(direction1, direction2);

                if (!retVal.Value.IsInvalid() && !retVal.Value.IsNearZero())        // it will be invalid or zero if the vectors are colinear
                {
                    break;
                }
            }

            if (retVal == null)
            {
                throw new ArgumentException("The points in the polygon are colinear");
            }

            switch (returnLength)
            {
                case PolygonNormalLength.DoesntMatter:
                    break;

                case PolygonNormalLength.Unit:
                    retVal = retVal.Value.normalized;
                    break;

                case PolygonNormalLength.PolygonArea:
                    retVal = retVal.Value.normalized;
                    retVal = retVal.Value * GetAreaPolygon(polygon, retVal.Value);      // this line is the main reason why this is a copy of the vec2 overload (this is using 3D overload of GetAreaPolygon)
                    break;

                default:
                    throw new ApplicationException("Unknown PolygonNormalLength: " + returnLength.ToString());
            }

            return retVal.Value;
        }

        /// <summary>
        /// Got this here:
        /// http://social.msdn.microsoft.com/Forums/windows/en-US/95055cdc-60f8-4c22-8270-ab5f9870270a/determine-if-the-point-is-in-the-polygon-c
        /// 
        /// Explanation here:
        /// http://conceptual-misfire.awardspace.com/point_in_polygon.htm
        /// </summary>
        /// <param name="includeEdgeHits">
        /// null=Edge hits aren't handled specially (some edge hits may return true, some may return false)
        /// true=Edge hits return true
        /// false=Edge hits return false
        /// </param>
        public static bool IsInsidePolygon(Vector2[] polygon, Vector2 testPoint, bool? includeEdgeHits)
        {
            if (includeEdgeHits != null && IsOnEdgeOfPolygon(polygon, testPoint))
            {
                // The point is sitting on the edge.  Return what they asked for
                return includeEdgeHits.Value;
            }

            if (polygon.Length < 3)
            {
                return false;
            }

            Vector2 p1, p2;
            bool inside = false;

            Vector2 oldPoint = new Vector2(polygon[polygon.Length - 1].x, polygon[polygon.Length - 1].y);

            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 newPoint = new Vector2(polygon[i].x, polygon[i].y);

                if (newPoint.x > oldPoint.x)
                {
                    p1 = oldPoint;
                    p2 = newPoint;
                }
                else
                {
                    p1 = newPoint;
                    p2 = oldPoint;
                }

                if ((newPoint.x < testPoint.x) == (testPoint.x <= oldPoint.x) &&
                    (testPoint.y - p1.y) * (p2.x - p1.x) <
                    (p2.y - p1.y) * (testPoint.x - p1.x))
                {
                    inside = !inside;
                }

                oldPoint = newPoint;
            }

            return inside;
        }
        public static bool IsOnEdgeOfPolygon(Vector2[] polygon, Vector2 testPoint)
        {
            foreach (Edge2D edge in Math2D.IterateEdges(polygon, null))
            {
                float distance = (Math2D.GetNearestPoint_Edge_Point(edge, testPoint) - testPoint).sqrMagnitude;

                if (distance.IsNearZero())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// This is a copy of Math3D's version
        /// </summary>
        public static Vector2 ToBarycentric(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 testPoint)
        {
            // Compute vectors        
            Vector2 v0 = p2 - p0;
            Vector2 v1 = p1 - p0;
            Vector2 v2 = testPoint - p0;

            // Compute dot products
            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            // Compute barycentric coordinates
            float invDenom = 1f / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            return new Vector2(u, v);
        }

        public static float GetAspectRatio(Vector2 size)
        {
            return size.x / size.y;
        }

        public static TransformsToFrom2D GetTransformTo2D(Plane plane)
        {
            Vector3 zUp = new Vector3(0, 0, 1);

            if (Math.Abs(Vector3.Dot(plane.normal.normalized, zUp)).IsNearValue(1))
            {
                // It's already 2D
                float distZ = plane.GetDistanceToPoint(Vector3.zero);      // this might be the same as plane.distance, but it's not clear whether plane.distance is signed
                return new TransformsToFrom2D()
                {
                    From3D_To2D = Matrix4x4.TRS(new Vector3(0, 0, -distZ), Quaternion.identity, Vector3.one),
                    From2D_BackTo3D = Matrix4x4.TRS(new Vector3(0, 0, distZ), Quaternion.identity, Vector3.one)
                };
            }

            // Don't bother with a double vector, just rotate the normal
            Quaternion rotation = Quaternion.FromToRotation(plane.normal, zUp);
            Vector3 rotatedXYPlane = rotation * plane.ClosestPointOnPlane(Vector3.zero);       // just need any point on the plane, then rotate so all points on the plane share the same z value

            Matrix4x4 transformTo2D = Matrix4x4.TRS(new Vector3(0, 0, -rotatedXYPlane.z), rotation, Vector3.one);
            Matrix4x4 transformTo3D = Matrix4x4.TRS(new Vector3(0, 0, rotatedXYPlane.z), Quaternion.Inverse(rotation), Vector3.one);

            return new TransformsToFrom2D()
            {
                From3D_To2D = transformTo2D,
                From2D_BackTo3D = transformTo3D,
            };
        }

        public static List<Vector2> DedupePoints(IEnumerable<Vector2> points)
        {
            var retVal = new List<Vector2>();

            var exists = new Func<Vector2, bool>(o =>
            {
                for (int cntr = 0; cntr < retVal.Count; cntr++)
                    if (retVal[cntr].IsNearValue(o))
                        return true;

                return false;
            });

            foreach (Vector2 point in points)
            {
                if (!exists(point))
                    retVal.Add(point);
            }

            return retVal;
        }

        /// <summary>
        /// This iterates through the edges of the polygon.  The Edge2D.EdgeType will always be Segment
        /// </summary>
        /// <param name="isClockwise">
        /// null: doesn't matter
        /// true: walk clockwise
        /// false: walk counter clockwise
        /// </param>
        public static IEnumerable<Edge2D> IterateEdges(Vector2[] polygon, bool? isClockwise)
        {
            bool traverseAsIs = true;
            if (isClockwise != null)
            {
                traverseAsIs = IsClockwise(polygon) == isClockwise.Value;
            }

            if (traverseAsIs)
            {
                #region already proper direction

                for (int cntr = 0; cntr < polygon.Length - 1; cntr++)
                {
                    yield return new Edge2D(cntr, cntr + 1, polygon);
                }

                yield return new Edge2D(polygon.Length - 1, 0, polygon);

                #endregion
            }
            else
            {
                #region reverse

                for (int cntr = polygon.Length - 1; cntr > 0; cntr--)
                {
                    yield return new Edge2D(cntr, cntr - 1, polygon);
                }

                yield return new Edge2D(0, polygon.Length - 1, polygon);

                #endregion
            }
        }

        public static (Vector2 min, Vector2 max) GetAABB(IEnumerable<Vector2> points)
        {
            bool foundOne = false;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (Vector2 point in points)
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

                if (point.x > maxX)
                {
                    maxX = point.x;
                }

                if (point.y > maxY)
                {
                    maxY = point.y;
                }
            }

            if (!foundOne)
            {
                // There were no points passed in
                //TODO: May want an exception
                return (Vector2.zero, Vector2.zero);
            }

            return (new Vector2(minX, minY), new Vector2(maxX, maxY));
        }
        public static Rect GetAABB(IEnumerable<Rect> rectangles)
        {
            //NOTE: Copied for speed

            bool foundOne = false;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (Rect rect in rectangles)
            {
                foundOne = true;        // it's too expensive to look at points.Count()

                if (rect.x < minX)
                {
                    minX = rect.x;
                }

                if (rect.y < minY)
                {
                    minY = rect.y;
                }

                if (rect.xMax > maxX)
                {
                    maxX = rect.xMax;
                }

                if (rect.yMax > maxY)
                {
                    maxY = rect.yMax;
                }
            }

            if (!foundOne)
            {
                // There were no rectangles passed in
                //TODO: May want an exception
                return new Rect(0, 0, 0, 0);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// This is the same as calling the other overload, and not ensuring coplanar.  Just removing the hassle of ignoring
        /// the out transforms.
        /// </summary>
        public static Vector2[] GetRotatedPoints(Vector3[] points)
        {
            return GetRotatedPoints(out _, out _, points, false);
        }
        /// <summary>
        /// This overload assumes that there are at least 3 points, or it will just return null
        /// </summary>
        /// <remarks>
        /// If the input points aren't perfectly coplanar, then the out transforms aren't terribly useful (assuming ensureCoplanarInput is false)
        /// 
        /// Also, don't go too nuts with the input points variance from the plane, or the returned 2D points may not form the same shaped
        /// polygon that the input 3D points did (assuming the points passed in are for a polygon, it could just be a point cloud)
        /// </remarks>
        /// <param name="ensureCoplanarInput">
        /// True = Once the plane is calculated, any points no lying on that plane will cause this method to return null.
        /// False = The points are assumed to be not perfectly coplanar, and a slightly more expensive (but far more forgiving) method is called.
        /// </param>
        public static Vector2[] GetRotatedPoints(out Matrix4x4 transformTo2D, out Matrix4x4 transformTo3D, Vector3[] points, bool ensureCoplanarInput = false)
        {
            Plane? plane;
            if (ensureCoplanarInput)
            {
                plane = GetPlane_Strict(points);
            }
            else
            {
                plane = GetPlane_Average(points, true);
            }

            if (plane == null)
            {
                transformTo2D = Matrix4x4.identity;
                transformTo3D = Matrix4x4.identity;
                return null;
            }

            // Figure out a transform that will make Z drop out
            var transform = GetTransformTo2D(plane.Value);
            transformTo2D = transform.From3D_To2D;
            transformTo3D = transform.From2D_BackTo3D;

            // Transform them
            Vector2[] retVal = new Vector2[points.Length];
            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                Vector3 transformed = transformTo2D.MultiplyPoint3x4(points[cntr]);
                retVal[cntr] = new Vector2(transformed.x, transformed.y);
            }

            return retVal;
        }
        /// <summary>
        /// This overload handles exactly two points
        /// </summary>
        public static Vector2[] GetRotatedPoints(out Matrix4x4 transformTo2D, out Matrix4x4 transformTo3D, Vector3 point1, Vector3 point2)
        {
            // Get rotation
            Quaternion rotation = Quaternion.identity;
            if (!point1.IsNearValue(point2))
            {
                Vector3 line1 = point2 - point1;		// this line is not along the z plane
                Vector3 line2 = new Vector3(point2.x, point2.y, point1.z) - point1;		// this line uses point1's z so is in the z plane

                rotation = Quaternion.FromToRotation(line1, line2);
            }

            // To 2D
            transformTo2D = Matrix4x4.TRS(new Vector3(0, 0, -point1.z), rotation, Vector3.one);

            // To 3D
            transformTo3D = Matrix4x4.TRS(new Vector3(0, 0, point1.z), Quaternion.Inverse(rotation), Vector3.one);

            // Transform the points
            Vector2[] retVal = new Vector2[2];
            Vector3 transformedPoint = transformTo2D.MultiplyPoint3x4(point1);
            retVal[0] = new Vector2(transformedPoint.x, transformedPoint.y);

            transformedPoint = transformTo2D.MultiplyPoint3x4(point2);
            retVal[1] = new Vector2(transformedPoint.x, transformedPoint.y);

            return retVal;
        }

        /// <summary>
        /// This takes a set of points that are coplanar, or pretty close to coplanar, and returns the plane that they sit on
        /// </summary>
        public static Plane? GetPlane_Average(Vector3[] points, bool matchPolyNormalDirection = false)
        {
            return AveragePlane.GetAveragePlane(points, matchPolyNormalDirection);
        }
        /// <summary>
        /// This returns the plane that these points sit on.  It also makes sure that all the points lie in the same plane as the returned triangle
        /// (some of the points may still be colinear or the same, but at least they are on the same plane)
        /// </summary>
        /// <remarks>
        /// NOTE: GetPolygonNormal has a lot of similarity with this method, but is simpler, cheaper
        /// </remarks>
        public static Plane? GetPlane_Strict(Vector3[] points)
        {
            Vector3? line1 = null;
            Vector3? line1Unit = null;

            Plane? retVal = null;

            for (int cntr = 1; cntr < points.Length; cntr++)
            {
                if (points[0].IsNearValue(points[cntr]))
                {
                    // These points are sitting on top of each other
                    continue;
                }

                Vector3 line = points[cntr] - points[0];

                if (line1 == null)
                {
                    // Found the first line
                    line1 = line;
                    line1Unit = line.normalized;
                    continue;
                }

                if (retVal == null)
                {
                    if (!Math.Abs(Vector3.Dot(line1Unit.Value, line.normalized)).IsNearValue(1))
                    {
                        // These two lines aren't colinear.  Found the second line
                        retVal = new Plane(points[0], points[0] + line1.Value, points[cntr]);
                    }

                    continue;
                }

                //if (!Math1D.IsNearZero(Vector3D.DotProduct(retVal.NormalUnit, line.ToUnit())))
                if (Math.Abs(Vector3.Dot(retVal.Value.normal.normalized, line.normalized)) > (Mathf.Epsilon * 1000))        // this was being a bit too strict.  Loosening it a little
                {
                    // This point isn't coplanar with the triangle
                    return null;
                }
            }

            return retVal;
        }

        public static (int index1, int index2, float distance)[] GetDistancesBetween(Vector2[] positions)
        {
            var retVal = new List<(int, int, float)>();

            for (int outer = 0; outer < positions.Length - 1; outer++)
            {
                for (int inner = outer + 1; inner < positions.Length; inner++)
                {
                    float distance = (positions[outer] - positions[inner]).magnitude;
                    retVal.Add((outer, inner, distance));
                }
            }

            return retVal.ToArray();
        }

        //public static Point[] ApplyBallOfSprings(Point[] positions, Tuple<int, int, double>[] desiredDistances, int numIterations)

        //GetCells

        //public static VectorInt2 GetCellColumnsRows(int count)

        #endregion

        #region intersections

        /// <summary>
        /// Returns the intersection of the two lines (line segments are passed in, but they are treated like infinite lines)
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://stackoverflow.com/questions/14480124/how-do-i-detect-triangle-and-rectangle-intersection
        /// </remarks>
        public static Vector2? GetIntersection_Line_Line(Vector2 line1From, Vector2 line1To, Vector2 line2From, Vector2 line2To)
        {
            Vector2 direction1 = line1To - line1From;
            Vector2 direction2 = line2To - line2From;
            float dotPerp = (direction1.x * direction2.y) - (direction1.y * direction2.x);

            // If it's 0, it means the lines are parallel so have infinite intersection points
            if (dotPerp.IsNearZero())
            {
                return null;
            }

            Vector2 c = line2From - line1From;
            float t = (c.x * direction2.y - c.y * direction2.x) / dotPerp;
            //if (t < 0 || t > 1)
            //{
            //    return null;		// lies outside the line segment
            //}

            //double u = (c.X * direction1.Y - c.Y * direction1.X) / dotPerp;
            //if (u < 0 || u > 1)
            //{
            //    return null;		// lies outside the line segment
            //}

            // Return the intersection point
            return line1From + (t * direction1);
        }
        /// <summary>
        /// Returns the intersection of the two line segments
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://stackoverflow.com/questions/14480124/how-do-i-detect-triangle-and-rectangle-intersection
        /// </remarks>
        public static Vector2? GetIntersection_LineSegment_LineSegment(Vector2 line1From, Vector2 line1To, Vector2 line2From, Vector2 line2To)
        {
            Vector2 direction1 = line1To - line1From;
            Vector2 direction2 = line2To - line2From;
            float dotPerp = (direction1.x * direction2.y) - (direction1.y * direction2.x);

            // If it's 0, it means the lines are parallel so have infinite intersection points
            if (dotPerp.IsNearZero())
            {
                return null;
            }

            Vector2 c = line2From - line1From;
            float t = (c.x * direction2.y - c.y * direction2.x) / dotPerp;
            if (t < 0 || t > 1)
            {
                return null;		// lies outside the line segment
            }

            float u = (c.x * direction1.y - c.y * direction1.x) / dotPerp;
            if (u < 0 || u > 1)
            {
                return null;		// lies outside the line segment
            }

            // Return the intersection point
            return line1From + (t * direction1);
        }

        /// <summary>
        /// This clips the subject polygon against the clip polygon (gets the intersection of the two polygons)
        /// </summary>
        /// <remarks>
        /// Based on the psuedocode from:
        /// http://en.wikipedia.org/wiki/Sutherland%E2%80%93Hodgman
        /// </remarks>
        /// <param name="subjectPoly">Can be concave or convex</param>
        /// <param name="clipPoly">Must be convex</param>
        /// <returns>The intersection of the two polygons (or null)</returns>
        public static Vector2[] GetIntersection_Polygon_Polygon(Vector2[] subjectPoly, Vector2[] clipPoly)
        {
            return SutherlandHodgman.GetIntersectedPolygon(subjectPoly, clipPoly);
        }
        /// <summary>
        /// This overload takes a 2D polygon and triangle (but in 3D coords)
        /// NOTE: polygon and triangle are expected to be coplanar
        /// </summary>
        /// <param name="polygon">Can be concave or convex</param>
        public static Vector3[] GetIntersection_Polygon_Triangle(Vector3[] polygon, ITriangle triangle)
        {
            // triangle and polygon should be in the same plane.  Find a transform that will cause the Z to drop out
            Quaternion rotateTo2D = Quaternion.FromToRotation(triangle.Normal, new Vector3(0, 0, 1));

            // Transform the points to the 2D plane (leaving the triangle as Point3D to get the z offset)
            Vector3[] triangleRotated = new Vector3[]
            {
                rotateTo2D * triangle.Point0,
                rotateTo2D * triangle.Point1,
                rotateTo2D * triangle.Point2
            };

            Vector2[] polygonRotated = polygon.
                Select(o => rotateTo2D * o).
                Select(o => new Vector2(o.x, o.y)).
                ToArray();

            Vector2[] retVal = SutherlandHodgman.GetIntersectedPolygon(polygonRotated, triangleRotated.Select(o => new Vector2(o.x, o.y)).ToArray());
            if (retVal == null || retVal.Length == 0)
            {
                return null;
            }

            // Transform clipped back into 3D
            Quaternion rotateTo3D = Quaternion.Inverse(rotateTo2D);

            return retVal.
                Select(o => rotateTo3D * new Vector3(o.x, o.y, triangleRotated[0].z)).
                ToArray();
        }
        /// <summary>
        /// This overload takes two 2D polygons (but in 3D coords)
        /// NOTE: Both polygons are expected to be coplanar
        /// </summary>
        /// <param name="subjectPoly">Can be concave or convex</param>
        /// <param name="clipPoly">Must be convex</param>
        public static Vector3[] GetIntersection_Polygon_Polygon(Vector3[] subjectPoly, Vector3[] clipPoly)
        {
            // triangle and polygon should be in the same plane.  Find a transform that will cause the Z to drop out
            Quaternion rotateTo2D = Quaternion.FromToRotation(GetPolygonNormal(clipPoly, PolygonNormalLength.DoesntMatter), new Vector3(0, 0, 1));

            // Transform the points to the 2D plane
            Vector3[] clipRotated = clipPoly.       // leaving the clip as Vector3 to get the z offset
                Select(o => rotateTo2D * o).
                ToArray();

            Vector2[] subjectRotated = subjectPoly.
                Select(o => rotateTo2D * o).
                Select(o => new Vector2(o.x, o.y)).
                ToArray();

            Vector2[] retVal = SutherlandHodgman.GetIntersectedPolygon(subjectRotated, clipRotated.Select(o => new Vector2(o.x, o.y)).ToArray());
            if (retVal == null || retVal.Length == 0)
            {
                return null;
            }

            // Transform clipped back into 3D
            Quaternion rotateTo3D = Quaternion.Inverse(rotateTo2D);

            return retVal.
                Select(o => rotateTo3D * new Vector3(o.x, o.y, clipRotated[0].z)).
                ToArray();
        }

        public static Vector2? GetIntersection_LineSegment_Circle(Vector2 lineStart, Vector2 lineEnd, Vector2 circleCenter, float radius)
        {
            float? percent = GetIntersection_LineSegment_Circle_percent(lineStart, lineEnd, circleCenter, radius);
            if (percent == null)
            {
                return null;
            }
            else
            {
                return lineStart + ((lineEnd - lineStart) * percent.Value);
            }
        }
        /// <summary>
        /// Got this here:
        /// http://stackoverflow.com/questions/1073336/circle-line-collision-detection
        /// </summary>
        public static float? GetIntersection_LineSegment_Circle_percent(Vector2 lineStart, Vector2 lineEnd, Vector2 circleCenter, float radius)
        {
            Vector2 lineDir = lineEnd - lineStart;

            Vector2 C = circleCenter;
            float r = radius;
            Vector2 E = lineStart;
            Vector2 d = lineDir;
            Vector2 f = E - C;

            Vector3 d3D = new Vector3(d.x, d.y, 0);
            Vector3 f3D = new Vector3(f.x, f.y, 0);

            float a = Vector3.Dot(d3D, d3D);
            float b = 2f * Vector3.Dot(f3D, d3D);
            float c = Vector3.Dot(f3D, f3D) - (r * r);

            float discriminant = (b * b) - (4 * a * c);
            if (discriminant < 0f)
            {
                // no intersection
                return null;
            }
            else
            {
                // ray didn't totally miss circle, so there is a solution to the equation.
                discriminant = (float)Math.Sqrt(discriminant);

                // either solution may be on or off the ray so need to test both
                float t1 = (-b + discriminant) / (2f * a);
                float t2 = (-b - discriminant) / (2f * a);

                if (t1 >= 0f && t1 <= 1f)
                {
                    // t1 solution on is ON THE RAY.
                    return t1;
                }
                else if (t1.IsNearZero())
                {
                    return 0f;
                }
                else if (t1.IsNearValue(1f))
                {
                    return 1f;
                }
                else
                {
                    // t1 solution "out of range" of ray
                    //return null;
                }

                if (t2 >= 0f && t2 <= 1f)
                {
                    // t2 solution on is ON THE RAY.
                    return t2;
                }
                else if (t2.IsNearZero())
                {
                    return 0f;
                }
                else if (t2.IsNearValue(1f))
                {
                    return 1f;
                }
                else
                {
                    // t2 solution "out of range" of ray
                }
            }

            return null;
        }

        /// <summary>
        /// This takes in an arbitrary number of polygons (it's ok for them to be concave), and returns polygons
        /// that are the union
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://sourceforge.net/projects/polyclipping/
        /// </remarks>
        public static Polygon2D[] GetUnion_Polygons(Vector2[][] polygons)
        {
            if (polygons == null || polygons.Length == 0)
            {
                return new Polygon2D[0];
            }

            float scale = GetUnion_Polygons_GetScale(polygons);
            var convertedPolys = GetUnion_Polygons_ConvertInput(polygons, scale);

            var clipper = new Clipper.Clipper();
            clipper.AddPolygons(convertedPolys, PolyType.ptSubject);        // when doing union, I don't think it matters what is subject and what is union
            //clipper.ForceSimple = true;       // not sure if this is helpful or not

            // Here is a page describing PolyFillType (nonzero is what you intuitively think of for a union)
            // http://www.angusj.com/delphi/clipper/documentation/Docs/Units/ClipperLib/Types/PolyFillType.htm

            PolyTree solution = new PolyTree();
            if (!clipper.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero))
            {
                return new Polygon2D[0];
            }

            return GetUnion_Polygons_ConvertOutput(solution, 1f / scale);
        }

        /// <summary>
        /// This takes a bunch of polygons, and figures out which ones are inside of others
        /// WARNING: The polygons passed in cannot intersect each other, if you don't know, call GetUnion_Polygons instead.  It's more general purpose (but slower)
        /// </summary>
        /// <remarks>
        /// If there are polygons inside of holes, they are treated as holes of the outermost polygon
        /// </remarks>
        public static (int polyIndex, int[] pointIndices)[] GetPolygonIslands(Vector2[][] polygons)
        {
            // Find the polygons that are inside of others
            SortedList<int, List<int>> containers = GetPolygonIslands_FindHoles(polygons);

            // The initial pass finds parent-child, but doesn't detect grandchild and deeper.  Merge grandchildren so that containers.Keys is
            // only root polygons
            GetPolygonIslands_Roots(containers);

            // Build the return
            var retVal = new List<(int, int[])>();

            foreach (int key in containers.Keys)
            {
                retVal.Add((key, containers[key].Distinct().ToArray()));
            }

            int[] mentioned = containers.Keys.
                Concat(containers.Values.SelectMany(o => o)).
                ToArray();

            int[] notMentioned = Enumerable.Range(0, polygons.Length).
                Where(o => !mentioned.Contains(o)).
                ToArray();

            retVal.AddRange(notMentioned.Select(o => (o, new int[0])));     // any polygons that aren't in containers are standalone

            return retVal.ToArray();
        }

        public static Vector2 GetNearestPoint_Edge_Point(Edge2D edge, Vector2 point)
        {
            switch (edge.EdgeType)
            {
                case EdgeType.Line:
                    return GetNearestPoint_Line_Point(new Ray2D(edge.Point0, edge.Direction.Value), point);

                case EdgeType.Ray:
                    return GetNearestPoint_Ray_Point(new Ray2D(edge.Point0, edge.Direction.Value), point);

                case EdgeType.Segment:
                    return GetNearestPoint_LineSegment_Point(edge.Point0, edge.Point1.Value, point);

                default:
                    throw new ApplicationException($"Unknown EdgeType: {edge.EdgeType}");
            }
        }
        public static float GetClosestDistance_Edge_Point(Edge2D edge, Vector2 point)
        {
            switch (edge.EdgeType)
            {
                case EdgeType.Line:
                    return GetClosestDistance_Line_Point(new Ray2D(edge.Point0, edge.Direction.Value), point);

                case EdgeType.Ray:
                    return GetClosestDistance_Ray_Point(new Ray2D(edge.Point0, edge.Direction.Value), point);

                case EdgeType.Segment:
                    return GetClosestDistance_LineSegment_Point(edge.Point0, edge.Point1.Value, point);

                default:
                    throw new ApplicationException($"Unknown EdgeType: {edge.EdgeType}");
            }
        }

        /// <summary>
        /// Returns the nearest point between a point and line segment
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://stackoverflow.com/questions/849211/shortest-distance-between-a-point-and-a-line-segment
        /// </remarks>
        public static Vector2 GetNearestPoint_LineSegment_Point(Vector2 start, Vector2 stop, Vector2 test)
        {
            float segmentLenSqr = (stop - start).sqrMagnitude;
            if (segmentLenSqr.IsNearZero())
            {
                return start;       //  line segment is just a point, so return that point
            }

            // Consider the line extending the segment, parameterized as start + t (stop - start).
            // We find projection of test point onto the line. 
            // It falls where t = [(test-start) . (stop-start)] / |stop-start|^2
            float t = Vector2.Dot(test - start, stop - start) / segmentLenSqr;
            if (t < 0d)
            {
                return start;       // Beyond the start of the segment
            }
            else if (t > 1d)
            {
                return stop;  // Beyond the stop of the segment
            }

            return start + t * (stop - start);  // Projection falls on the segment
        }
        public static float GetClosestDistance_LineSegment_Point(Vector2 start, Vector2 stop, Vector2 test)
        {
            Vector2 nearest = GetNearestPoint_LineSegment_Point(start, stop, test);
            return (test - nearest).magnitude;
        }

        public static Vector2 GetNearestPoint_Ray_Point(Ray2D ray, Vector2 test)
        {
            if (ray.direction.sqrMagnitude.IsNearZero())
            {
                return ray.origin;       //  the dirction is zero, so just return the start of the ray
            }

            // Consider the line extending the ray, parameterized as start + t (direction).
            // We find projection of test point onto the line. 
            // It falls where t = [(test-start) . direction]
            float t = Vector2.Dot(test - ray.origin, ray.direction);
            if (t < 0d)
            {
                return ray.origin;       // Beyond the start of the ray
            }

            return ray.origin + t * (ray.direction);  // Projection falls on the ray
        }
        public static float GetClosestDistance_Ray_Point(Ray2D ray, Vector2 test)
        {
            Vector2 nearest = GetNearestPoint_Ray_Point(ray, test);
            return (test - nearest).magnitude;
        }

        public static Vector2 GetNearestPoint_Line_Point(Ray2D ray, Vector2 test)
        {
            if (ray.direction.sqrMagnitude.IsNearZero())
            {
                return ray.origin;       //  the dirction is zero, so just return the point (this isn't tecnically correct, but the user should never pass in a zero length direction)
            }

            // Find projection of test point onto the line. 
            // It falls where t = [(test-start) . direction]
            float t = Vector2.Dot(test - ray.origin, ray.direction);

            return ray.origin + t * (ray.direction);
        }
        public static float GetClosestDistance_Line_Point(Ray2D ray, Vector2 test)
        {
            Vector2 nearest = GetNearestPoint_Line_Point(ray, test);
            return (test - nearest).magnitude;
        }

        #endregion

        #region hulls/graphs/triangulation

        /// <summary>
        /// This returns points around a unit circle.  The result is cached in a singleton, so any future request for the same number
        /// of sides is fast
        /// </summary>
        public static Vector2[] GetCircle_Cached(int numSides)
        {
            return PointsSingleton.Instance.GetPoints(numSides);
        }

        #endregion

        #region Private Methods

        private static bool IsClockwise(Vector2[] polygon)
        {
            for (int cntr = 2; cntr < polygon.Length; cntr++)
            {
                bool? isLeft = IsLeftOf(new Edge2D(polygon[0], polygon[1]), polygon[cntr]);
                if (isLeft != null)		// some of the points may be colinear.  That's ok as long as the overall is a polygon
                {
                    return !isLeft.Value;
                }
            }

            throw new ArgumentException("All the points in the polygon are colinear");
        }

        /// <summary>
        /// Tells if the test point lies on the left side of the edge line
        /// </summary>
        private static bool? IsLeftOf(Edge2D edge, Vector2 test)
        {
            Vector2 tmp1 = edge.Point1Ext - edge.Point0;
            Vector2 tmp2 = test - edge.Point1Ext;

            double x = (tmp1.x * tmp2.y) - (tmp1.y * tmp2.x);		// dot product of perpendicular?

            if (x < 0)
            {
                return false;
            }
            else if (x > 0)
            {
                return true;
            }
            else
            {
                // Colinear points;
                return null;
            }
        }

        private static float GetUnion_Polygons_GetScale(Vector2[][] polygons)
        {
            // I was going to go with massively large scale to use most of the range of int64, but there was a comment that says he
            // caps at +- 1.5 billion

            //var aabb = Math2D.GetAABB(polygons.SelectMany(o => o));
            //double max = Math3D.Max(aabb.Item1.X, aabb.Item1.Y, aabb.Item2.X, aabb.Item2.Y);


            //TODO: Don't scale if aabb is larger than some value
            //TODO: Scale a lot if there are a lot of points less than .001


            return 10000f;
        }
        private static List<List<IntPoint>> GetUnion_Polygons_ConvertInput(Vector2[][] polygons, float scale)
        {
            var retVal = new List<List<IntPoint>>();

            // The union method flakes out if the polygons have different windings (clockwise vs counter clockwise)
            Vector3 normal = Math2D.GetPolygonNormal(polygons[0], PolygonNormalLength.DoesntMatter);

            for (int cntr = 0; cntr < polygons.Length; cntr++)
            {
                Vector2[] points = polygons[cntr];

                if (cntr > 0)       // no need to compare the first poly with itself
                {
                    Vector3 normal2 = Math2D.GetPolygonNormal(points, PolygonNormalLength.DoesntMatter);

                    if (Vector3.Dot(normal, normal2) < 0)
                    {
                        // This is wound the wrong direction.  Reverse it
                        points = points.Reverse().ToArray();
                    }
                }

                // Convert into the custom poly format
                retVal.Add(points.Select(o => new IntPoint(Convert.ToInt64(o.x * scale), Convert.ToInt64(o.y * scale))).ToList());
            }

            return retVal;
        }
        /// <summary>
        /// Convert into an array of polygons
        /// </summary>
        /// <remarks>
        /// The polytree will nest deeply if solids are inside of holes.  But Polygon2D would treat the solids inside of holes as their own
        /// independent isntances
        /// 
        /// http://www.angusj.com/delphi/clipper/documentation/Docs/Units/ClipperLib/Classes/PolyTree/_Body.htm
        /// </remarks>
        private static Polygon2D[] GetUnion_Polygons_ConvertOutput(PolyTree solution, float scaleInverse)
        {
            var retVal = new List<Polygon2D>();

            var descendants = Descendants((PolyNode)solution, o => o.Childs).
                Where(o => !o.IsHole && o.Contour.Count > 0);

            // Walk the tree, and get all parents (need to look at contour count so that the root gets skipped)
            foreach (PolyNode parent in descendants)
            {
                // Convert the parent polygon
                Vector2[] points = parent.Contour.Select(o => new Vector2(o.X * scaleInverse, o.Y * scaleInverse)).ToArray();

                if (parent.Childs.Count == 0)
                {
                    // No holes
                    retVal.Add(new Polygon2D(points));
                }
                else
                {
                    var holes = new List<Vector2[]>();

                    foreach (PolyNode child in parent.Childs)
                    {
                        if (!child.IsHole)
                        {
                            throw new ApplicationException("Expected the child of a non hole to be a hole");
                        }

                        // Convert the hole polygon
                        holes.Add(child.Contour.Select(o => new Vector2(o.X * scaleInverse, o.Y * scaleInverse)).ToArray());
                    }

                    // Store with holes
                    retVal.Add(new Polygon2D(points, holes.ToArray()));
                }
            }

            return retVal.ToArray();
        }

        private static SortedList<int, List<int>> GetPolygonIslands_FindHoles(Vector2[][] polygons)
        {
            var retVal = new SortedList<int, List<int>>();

            for (int outer = 0; outer < polygons.Length; outer++)
            {
                for (int inner = 0; inner < polygons.Length; inner++)
                {
                    if (outer == inner)
                    {
                        continue;
                    }

                    if (retVal.ContainsKey(outer) && retVal[outer].Contains(inner))
                    {
                        // This outer ate a hole that contained other holes.  inner is one of those grandchild holes
                        continue;
                    }

                    // There shouldn't be any intersections of polygons, so just need to see if one of the points is inside
                    //if (polygons2D[inner].All(o => Math2D.IsInsidePolygon(polygons2D[outer], o, null)))
                    if (Math2D.IsInsidePolygon(polygons[outer], polygons[inner][0], null))
                    {
                        if (!retVal.ContainsKey(outer))
                        {
                            retVal.Add(outer, new List<int>());
                        }

                        retVal[outer].Add(inner);

                        if (retVal.ContainsKey(inner))
                        {
                            retVal[outer].AddRange(retVal[inner]);      // don't worry about dupes.  The children will be deduped later
                            retVal.Remove(inner);
                        }
                    }
                }
            }

            return retVal;
        }
        private static void GetPolygonIslands_Roots(SortedList<int, List<int>> containers)
        {
            bool hadMerge = false;

            do
            {
                hadMerge = false;

                // Do a pass through all the keys, looking for a merge
                foreach (int key in containers.Keys.ToArray())      // may not need toarray here, but it feels cleaner (because a merge will call remove)
                {
                    foreach (int child in containers[key].ToArray())        // convert to array so that the merging won't affect the while loop
                    {
                        if (containers.ContainsKey(child))
                        {
                            containers[key].AddRange(containers[child]);        // don't worry about dupes, that will be handled later
                            containers.Remove(child);

                            hadMerge = true;
                        }
                    }

                    // If a key has done a merge, then need to go back to the outer loop and scan again
                    if (hadMerge)
                    {
                        break;
                    }
                }
            } while (hadMerge);
        }

        /// <summary>
        /// Lets you walk a tree as a 1D list (hard coded to depth first)
        /// (commented this because it shows up for all T.  uncomment if needed)
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://www.claassen.net/geek/blog/2009/06/searching-tree-of-objects-with-linq.html
        /// 
        /// Ex (assuming Node has a property IEnumerable<Node> Children):
        ///     Node[] all = root.Descendants(o => o.Children).ToArray();
        ///     
        /// The original code has two: depth first, breadth first.  But for simplicity, I'm just using depth first.  Uncomment the
        /// more explicit methods if neeeded
        /// 
        /// NOTE: In AsteroidMiner and PartyPeople, this was an extension method.  But it wasn't really used and was annoying being
        /// tied to any type
        /// </remarks>
        private static IEnumerable<T> Descendants<T>(T head, Func<T, IEnumerable<T>> childrenFunc)
        {
            yield return head;

            var children = childrenFunc(head);
            if (children != null)
            {
                foreach (var node in childrenFunc(head))
                {
                    foreach (var child in Descendants(node, childrenFunc))
                    {
                        yield return child;
                    }
                }
            }
        }

        #endregion
    }

    #region enum: PolygonNormalLength

    public enum PolygonNormalLength
    {
        DoesntMatter,
        Unit,
        PolygonArea
    }

    #endregion

    #region class: QuickHull2DResult

    public class QuickHull2DResult
    {
        public QuickHull2DResult(Vector2[] points, int[] perimiterLines, Matrix4x4 transformTo2D, Matrix4x4 transformTo3D)
        {
            Points = points;
            PerimiterLines = perimiterLines;
            TransformTo2D = transformTo2D;
            TransformTo3D = transformTo3D;
        }

        public readonly Vector2[] Points;
        public readonly int[] PerimiterLines;
        private readonly Matrix4x4 TransformTo2D;
        private readonly Matrix4x4 TransformTo3D;

        public bool IsInside(Vector2 point)
        {
            for (int cntr = 0; cntr < this.Points.Length - 1; cntr++)
            {
                if (!Math2D.QuickHull2D.IsRightOfLine(cntr, cntr + 1, point, Points))
                {
                    return false;
                }
            }

            if (!Math2D.QuickHull2D.IsRightOfLine(this.Points.Length - 1, 0, point, Points))
            {
                return false;
            }

            return true;
        }
        public Vector2? GetTransformedPoint(Vector3 point)
        {
            // Use the transform to rotate/translate the point to the z plane
            Vector3 transformed = point;
            if (TransformTo2D != null)
            {
                transformed = TransformTo2D.MultiplyPoint3x4(point);
            }

            // Only return a value if it's now in the z plane, which will only work if the point passed in is in the same plane as this.Points
            if (transformed.z.IsNearZero())
            {
                return new Vector2(transformed.x, transformed.y);
            }
            else
            {
                return null;
            }
        }
        public Vector3 GetTransformedPoint(Vector2 point)
        {
            Vector3 retVal = point;
            if (TransformTo3D != null)
            {
                retVal = TransformTo3D.MultiplyPoint3x4(retVal);
            }

            return retVal;
        }
    }

    #endregion

    #region class: Edge2D

    /// <summary>
    /// This represents a line in 2D (either a line segment, ray, or infinite line)
    /// </summary>
    /// <remarks>
    /// I decided to take an array of points, and store indexes into that array.  That makes it easier to compare points across
    /// multiple edges to see which ones are using the same points (int comparisons are exact, doubles are iffy)
    /// </remarks>
    public class Edge2D
    {
        #region Constructor

        public Edge2D(Vector2 point0, Vector2 point1)
        {
            EdgeType = EdgeType.Segment;
            Index0 = 0;
            Index1 = 1;
            Direction = null;
            AllEdgePoints = new[] { point0, point1 };
        }
        public Edge2D(int index0, int index1, Vector2[] allEdgePoints)
        {
            EdgeType = EdgeType.Segment;
            Index0 = index0;
            Index1 = index1;
            Direction = null;
            AllEdgePoints = allEdgePoints;
        }
        public Edge2D(EdgeType edgeType, int index0, Vector2 direction, Vector2[] allEdgePoints)
        {
            if (edgeType == EdgeType.Segment)
            {
                throw new ArgumentException("This overload requires edge type to be Ray or Line, not Segment");
            }

            EdgeType = edgeType;
            Index0 = index0;
            Index1 = null;
            Direction = direction;
            AllEdgePoints = allEdgePoints;
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

        public readonly Vector2? Direction;
        /// <summary>
        /// This either returns Direction (if the edge is a ray or line), or it returns Point1 - Point0
        /// (this is helpful if you just want to treat the edge like a segment)
        /// </summary>
        public Vector2 DirectionExt => Direction != null ? Direction.Value : Point1.Value - Point0;

        public Vector2 Point0 => AllEdgePoints[Index0];
        public Vector2? Point1 => Index1 == null ? (Vector2?)null : AllEdgePoints[Index1.Value];
        /// <summary>
        /// This either returns Point1 (if the edge is a segment), or it returns Point0 + Direction
        /// (this is helpful if you just want to always treat the edge like a segment)
        /// </summary>
        public Vector2 Point1Ext => Point1 != null ? Point1.Value : Point0 + Direction.Value;

        public readonly Vector2[] AllEdgePoints;

        #region Public Methods

        /// <summary>
        /// This is the same as Point1Ext get;, but lets the user pass in how long the extension should be
        /// </summary>
        public Vector2 GetPoint1Ext(float extensionLength)
        {
            if (Point1 != null)
            {
                return Point1.Value;
            }
            else
            {
                return Point0 + (Direction.Value.normalized * extensionLength);
            }
        }

        /// <summary>
        /// This tells which edges are touching each of the other edges
        /// NOTE: This only looks at the ints.  It doesn't project lines
        /// </summary>
        public static int[][] GetTouchingEdges(Edge2D[] edges)
        {
            int[][] retVal = new int[edges.Length][];

            for (int outer = 0; outer < edges.Length; outer++)
            {
                List<int> touching = new List<int>();

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
        public static bool IsTouching(Edge2D edge0, Edge2D edge1)
        {
            return GetCommonIndex(edge0, edge1) >= 0;
        }
        public static int GetCommonIndex(Edge2D edge0, Edge2D edge1)
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
        public static (Vector2 commonPoint, Vector2 ray0, Vector2 ray1) GetRays(Edge2D edge0, Edge2D edge1)
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
        public static Vector2 GetDirectionFromPoint(Edge2D edge, int index)
        {
            switch (edge.EdgeType)
            {
                case EdgeType.Line:
                    throw new ArgumentException("This method doesn't make sense for lines");        //  because lines can go two directions

                case EdgeType.Ray:
                    #region ray

                    if (edge.Index0 != index)
                    {
                        throw new ArgumentException("The index passed in doesn't belong to this edge");
                    }

                    return edge.Direction.Value;

                #endregion

                case EdgeType.Segment:
                    #region segment

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

        /// <summary>
        /// This takes a set of edges and returns points in order
        /// NOTE: edges[0] must share a point with edges[1], etc (these should have come from Voronoi2DResult.EdgesByControlPoint, which puts rays on the outside)
        /// WARNING: This method is a bit fragile.  If the edges come from someplace other than Voronoi2DResult.EdgesByControlPoint, test it well
        /// </summary>
        /// <param name="rayLength">If an edge is a ray, it will be projected this far to be a line segment</param>
        public static Vector2[] GetPolygon(Edge2D[] edges, float rayLength)
        {
            if (edges == null || edges.Length < 2)
            {
                throw new ArgumentException("Need at least two edges to make a polygon");
            }
#if DEBUG
            else if (edges.Any(o => o.EdgeType == EdgeType.Line))
            {
                throw new ArgumentException("This method doesn't support lines");
            }
#endif

            var retVal = new List<Vector2>();
            int commonIndex;

            for (int cntr = 0; cntr < edges.Length - 1; cntr++)
            {
                #region cntr, cntr+1

                // Add the point from cntr that isn't shared with cntr + 1
                commonIndex = GetCommonIndex(edges[cntr], edges[cntr + 1]);

                if (commonIndex < 0)
                {
                    // While in this main loop, there can't be any breaks
                    throw new ApplicationException("Didn't find common point between edges");
                }
                else
                {
                    retVal.Add(GetPolygon_Point(edges[cntr], rayLength, edges[cntr].Index0 != commonIndex));
                }

                #endregion
            }

            #region last edge

            commonIndex = GetCommonIndex(edges[0], edges[edges.Length - 1]);

            if (commonIndex < 0 || edges.Length == 2)       // When the length is 2, it's a V.  Come in here to force the middle point to be written
            {
                // These edges define an open polygon - looks like a U.  They could be segments or rays, doesn't matter.  Since it's the last
                // edge that's being written, it shares a common point with the second to last edge (or an exception would have been thrown
                // above).  So just use that as the one not to write
                commonIndex = GetCommonIndex(edges[edges.Length - 2], edges[edges.Length - 1]);

                // But write the common one first, because it would get skipped
                retVal.Add(GetPolygon_Point(edges[edges.Length - 1], rayLength, edges[edges.Length - 1].Index0 == commonIndex));
            }

            retVal.Add(GetPolygon_Point(edges[edges.Length - 1], rayLength, edges[edges.Length - 1].Index0 != commonIndex));

            #endregion

            return retVal.ToArray();
        }
        private static Vector2 GetPolygon_Point(Edge2D edge, float rayLength, bool useZero)
        {
            if (useZero)
            {
                return edge.Point0;
            }

            switch (edge.EdgeType)
            {
                case EdgeType.Segment:
                    return edge.Point1.Value;

                case EdgeType.Ray:
                    return edge.Point0 + (edge.Direction.Value.normalized * rayLength);

                default:
                    throw new ApplicationException($"Unexpected EdgeType: {edge.EdgeType}");     // lines aren't supported
            }
        }

        /// <summary>
        /// This takes a set of edges that either form a polygon, or are a set of rays
        /// NOTE: edges[0] must share a point with edges[1], etc (these should have come from Voronoi2DResult.EdgesByControlPoint, which puts rays on the outside)
        /// WARNING: This method is a bit fragile.  If the edges come from someplace other than Voronoi2DResult.EdgesByControlPoint, test it well
        /// </summary>
        /// <remarks>
        /// This is sort of a relaxed version of GetPolygon
        /// 
        /// NOTE: This method takes advantage of the fact that the edges always form a convex polygon
        /// </remarks>
        public static bool IsInside(Edge2D[] edges, Vector2 point)
        {
            if (edges == null || edges.Length < 2)
            {
                throw new ArgumentException("This method requires at least two edges");
            }
#if DEBUG
            else if (edges.Any(o => o.EdgeType == EdgeType.Line))
            {
                throw new ArgumentException("This method doesn't support lines");
            }
#endif

            // Figure out the winding
            bool includeRight = IsInside_Winding(edges);

            int common;
            Vector2 from, to;

            for (int cntr = 0; cntr < edges.Length - 1; cntr++)
            {
                #region cntr, cntr+1

                // Can't just blindly use p0 then p1, need to get the common point, then always go from other to common
                common = GetCommonIndex(edges[cntr], edges[cntr + 1]);

                from = IsInside_OtherPoint(edges[cntr], common);
                to = edges[cntr].AllEdgePoints[common];

                if (Math2D.QuickHull2D.IsRightOfLine(from, to, point) != includeRight)
                {
                    return false;
                }

                #endregion
            }

            #region last edge

            // Compare the previous two, rather than looping back to edges[0] (since this could be an open poly, the prev two edges will always touch)
            common = GetCommonIndex(edges[edges.Length - 1], edges[edges.Length - 2]);

            // Within the loop, from is other.  But in this section to is other
            from = edges[edges.Length - 1].AllEdgePoints[common];
            to = IsInside_OtherPoint(edges[edges.Length - 1], common);

            if (Math2D.QuickHull2D.IsRightOfLine(from, to, point) != includeRight)
            {
                return false;
            }

            #endregion

            return true;
        }
        private static bool IsInside_Winding(Edge2D[] edges)
        {
            int common = GetCommonIndex(edges[0], edges[1]);

            Vector2 left = IsInside_OtherPoint(edges[0], common);
            Vector2 middle = edges[0].AllEdgePoints[common];
            Vector2 right = IsInside_OtherPoint(edges[1], common);

            return Math2D.QuickHull2D.IsRightOfLine(left, middle, right);
        }
        /// <summary>
        /// Returns the point that isn't commmon
        /// </summary>
        private static Vector2 IsInside_OtherPoint(Edge2D edge, int common)
        {
            if (edge.Index0 == common)
            {
                return edge.Point1Ext;
            }
            else
            {
                return edge.Point0;
            }
        }

        /// <summary>
        /// This is useful when looking at lists of edges in the quick watch
        /// </summary>
        public override string ToString()
        {
            const string DELIM = "       |       ";

            StringBuilder retVal = new StringBuilder(100);

            retVal.Append(EdgeType.ToString());
            retVal.Append(DELIM);

            switch (EdgeType)
            {
                case EdgeType.Segment:
                    retVal.Append(string.Format("{0} - {1}{2}({3}) ({4})",
                        Index0,
                        Index1,
                        DELIM,
                        Point0.ToString("N2"),
                        Point1.Value.ToString("N2")));
                    break;

                case EdgeType.Ray:
                    retVal.Append(string.Format("{0}{1}({2}) --> ({3})",
                        Index0,
                        DELIM,
                        Point0.ToString("N2"),
                        Direction.Value.ToString("N2")));
                    break;

                case EdgeType.Line:
                    retVal.Append(string.Format("{0}{1}({2}) <---> ({3})",
                        Index0,
                        DELIM,
                        Point0.ToString("N2"),
                        Direction.Value.ToString("N2")));
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

    #region class: Polygon2D

    public class Polygon2D
    {
        public Polygon2D(Vector2[] polygon)
            : this(polygon, new Vector2[0][]) { }
        public Polygon2D(Vector2[] polygon, Vector2[][] holes)
        {
            Polygon = polygon;
            Holes = holes;
        }

        /// <summary>
        /// This is the outer shell that represents a polygon
        /// </summary>
        public readonly Vector2[] Polygon;
        /// <summary>
        /// This is an array of polygons that are inside the outer polygon
        /// </summary>
        public readonly Vector2[][] Holes;
    }

    #endregion

    #region class: TransformsToFrom2D

    /// <summary>
    /// This holds a pair of transforms to take coplanar 3D points into 2D, then back from that 2D plane to the 3D's plane
    /// </summary>
    public class TransformsToFrom2D
    {
        /// <summary>
        /// Transform from 3D to 2D
        /// </summary>
        public Matrix4x4 From3D_To2D { get; set; }
        /// <summary>
        /// How to get a 2D back to 3D
        /// </summary>
        public Matrix4x4 From2D_BackTo3D { get; set; }
    }

    #endregion
}
