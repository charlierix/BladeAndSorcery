using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalUnity
{
    //NOTE: It can be tempting to use Mesh as a container of triangles for the math classes.
    //But it smoothes the normals out and looks pretty expensive
    //
    //So these triangle definitions are for when only a few triangles are needed and get handed
    //to math functions

    #region class: Triangle

    //TODO:  Add methods like Clone, GetTransformed(transform), etc

    /// <summary>
    /// This stores 3 points explicitly - as opposed to TriangleIndexed, which stores ints that point to a list of points
    /// </summary>
    public class Triangle : ITriangle
    {
        #region Declaration Section

        // Caching these so that Enum.GetValues doesn't have to be called all over the place
        public static TriangleEdge[] Edges = (TriangleEdge[])Enum.GetValues(typeof(TriangleEdge));
        public static TriangleCorner[] Corners = (TriangleCorner[])Enum.GetValues(typeof(TriangleCorner));

        #endregion

        #region Constructor

        public Triangle()
        {
        }

        public Triangle(Vector3 point0, Vector3 point1, Vector3 point2)
        {
            _point0 = point0;
            _point1 = point1;
            _point2 = point2;
            OnPointChanged();
        }

        #endregion

        #region ITriangle Members

        private Vector3? _point0 = null;
        public Vector3 Point0
        {
            get
            {
                return _point0.Value;       // skipping the null check to be as fast as possible (.net will throw an execption anyway)
            }
            set
            {
                _point0 = value;
                OnPointChanged();
            }
        }

        private Vector3? _point1 = null;
        public Vector3 Point1
        {
            get
            {
                return _point1.Value;       // skipping the null check to be as fast as possible (.net will throw an execption anyway)
            }
            set
            {
                _point1 = value;
                OnPointChanged();
            }
        }

        private Vector3? _point2 = null;
        public Vector3 Point2
        {
            get
            {
                return _point2.Value;       // skipping the null check to be as fast as possible (.net will throw an execption anyway)
            }
            set
            {
                _point2 = value;
                OnPointChanged();
            }
        }

        public Vector3[] PointArray => new[] { _point0.Value, _point1.Value, _point2.Value };

        public Vector3 this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return this.Point0;

                    case 1:
                        return this.Point1;

                    case 2:
                        return this.Point2;

                    default:
                        throw new ArgumentOutOfRangeException("index", $"index can only be 0, 1, 2: {index}");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        this.Point0 = value;
                        break;

                    case 1:
                        this.Point1 = value;
                        break;

                    case 2:
                        this.Point2 = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("index", $"index can only be 0, 1, 2: {index}");
                }
            }
        }

        private Vector3? _normal = null;
        /// <summary>
        /// This returns the triangle's normal.  Its length is the area of the triangle
        /// </summary>
        public Vector3 Normal
        {
            get
            {
                if (_normal == null)
                {
                    CalculateNormal(out Vector3 normal, out float length, out Vector3 normalUnit, this.Point0, this.Point1, this.Point2);

                    _normal = normal;
                    _normalLength = length;
                    _normalUnit = normalUnit;
                }

                return _normal.Value;
            }
        }
        private Vector3? _normalUnit = null;
        /// <summary>
        /// This returns the triangle's normal.  Its length is one
        /// </summary>
        public Vector3 NormalUnit
        {
            get
            {
                if (_normalUnit == null)
                {
                    CalculateNormal(out Vector3 normal, out float length, out Vector3 normalUnit, this.Point0, this.Point1, this.Point2);

                    _normal = normal;
                    _normalLength = length;
                    _normalUnit = normalUnit;
                }

                return _normalUnit.Value;
            }
        }
        private float? _normalLength = null;
        /// <summary>
        /// This returns the length of the normal (the area of the triangle)
        /// NOTE:  Call this if you just want to know the length of the normal, it's cheaper than calling this.Normal.Length, since it's already been calculated
        /// </summary>
        public float NormalLength
        {
            get
            {
                if (_normalLength == null)
                {
                    CalculateNormal(out Vector3 normal, out float length, out Vector3 normalUnit, this.Point0, this.Point1, this.Point2);

                    _normal = normal;
                    _normalLength = length;
                    _normalUnit = normalUnit;
                }

                return _normalLength.Value;
            }
        }

        private float? _planeDistance = null;
        public float PlaneDistance
        {
            get
            {
                if (_planeDistance == null)
                {
                    _planeDistance = Math3D.GetPlaneOriginDistance(this.NormalUnit, this.Point0);
                }

                return _planeDistance.Value;
            }
        }

        private long? _token = null;
        public long Token
        {
            get
            {
                if (_token == null)
                {
                    _token = TokenGenerator.NextToken();
                }

                return _token.Value;
            }
        }

        public Vector3 GetCenterPoint()
        {
            return GetCenterPoint(this.Point0, this.Point1, this.Point2);
        }
        public Vector3 GetPoint(TriangleEdge edge, bool isFrom)
        {
            return GetPoint(this, edge, isFrom);
        }
        public Vector3 GetCommonPoint(TriangleEdge edge0, TriangleEdge edge1)
        {
            return GetCommonPoint(this, edge0, edge1);
        }
        public Vector3 GetUncommonPoint(TriangleEdge edge0, TriangleEdge edge1)
        {
            return GetUncommonPoint(this, edge0, edge1);
        }
        public Vector3 GetOppositePoint(TriangleEdge edge)
        {
            return GetOppositePoint(this, edge);
        }
        public Vector3 GetEdgeMidpoint(TriangleEdge edge)
        {
            return GetEdgeMidpoint(this, edge);
        }
        public float GetEdgeLength(TriangleEdge edge)
        {
            return GetEdgeLength(this, edge);
        }

        #endregion
        #region IComparable<ITriangle> Members

        /// <summary>
        /// This is so triangles can be used as keys in a sorted list
        /// </summary>
        public int CompareTo(ITriangle other)
        {
            if (other == null)
            {
                // this is greater than null
                return 1;
            }

            if (this.Token < other.Token)
            {
                return -1;
            }
            else if (this.Token > other.Token)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        #endregion

        #region Public Methods

        public static Vector3[] GetUniquePoints(IEnumerable<ITriangle> triangles)
        {
            //NOTE: Distinct overload uses a default implementation of DelegateComparer that sets GetHashCode to 1, which is ineficient for large lists
            //(because everything gets the same hash code)
            //
            //It's tempting to make a custom DelegateComparer<Vector3>.  But the equality check uses IsNearValue between two points
            //
            //So even if it aligned Vector3 to some kind of gridpoint and returned gethashcode of that, you could alway have two points sitting on either
            //side of the boundry.  So those points would be considered equal to each other if just using the Equals call, but have different hash codes
            //because they round down to different grid points

            return triangles.
                SelectMany(o => o.PointArray).
                Distinct((p1, p2) => p1.IsNearValue(p2)).
                ToArray();
        }

        /// <summary>
        /// This helps a lot when looking at lists of triangles in the quick watch
        /// </summary>
        public override string ToString()
        {
            return string.Format("({0}) ({1}) ({2})",
                _point0 == null ? "null" : _point0.Value.ToStringSignificantDigits(2),
                _point1 == null ? "null" : _point1.Value.ToStringSignificantDigits(2),
                _point2 == null ? "null" : _point2.Value.ToStringSignificantDigits(2));
        }

        #endregion
        #region Internal Methods

        internal static void CalculateNormal(out Vector3 normal, out float normalLength, out Vector3 normalUnit, Vector3 point0, Vector3 point1, Vector3 point2)
        {
            Vector3 dir1 = point0 - point1;
            Vector3 dir2 = point2 - point1;

            Vector3 triangleNormal = Vector3.Cross(dir2, dir1);

            normal = triangleNormal;
            normalLength = triangleNormal.magnitude;
            normalUnit = triangleNormal / normalLength;
        }

        internal static Vector3 GetCenterPoint(Vector3 point0, Vector3 point1, Vector3 point2)
        {
            //return ((triangle.Point0.ToVector() + triangle.Point1.ToVector() + triangle.Point2.ToVector()) / 3d).ToPoint();

            // Doing the math with floats to avoid casting to vector
            float x = (point0.x + point1.x + point2.x) / 3f;
            float y = (point0.y + point1.y + point2.y) / 3f;
            float z = (point0.z + point1.z + point2.z) / 3f;

            return new Vector3(x, y, z);
        }

        internal static Vector3 GetPoint(ITriangle triangle, TriangleEdge edge, bool isFrom)
        {
            switch (edge)
            {
                case TriangleEdge.Edge_01:
                    if (isFrom)
                    {
                        return triangle.Point0;
                    }
                    else
                    {
                        return triangle.Point1;
                    }

                case TriangleEdge.Edge_12:
                    if (isFrom)
                    {
                        return triangle.Point1;
                    }
                    else
                    {
                        return triangle.Point2;
                    }

                case TriangleEdge.Edge_20:
                    if (isFrom)
                    {
                        return triangle.Point2;
                    }
                    else
                    {
                        return triangle.Point0;
                    }

                default:
                    throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
            }
        }

        internal static Vector3 GetCommonPoint(ITriangle triangle, TriangleEdge edge0, TriangleEdge edge1)
        {
            Vector3[] points0 = new Vector3[] { triangle.GetPoint(edge0, true), triangle.GetPoint(edge0, false) };
            Vector3[] points1 = new Vector3[] { triangle.GetPoint(edge1, true), triangle.GetPoint(edge1, false) };

            // Find exact
            for (int cntr0 = 0; cntr0 < points0.Length; cntr0++)
            {
                for (int cntr1 = 0; cntr1 < points1.Length; cntr1++)
                {
                    if (points0[cntr0] == points1[cntr1])
                    {
                        return points0[cntr0];
                    }
                }
            }

            // Find close - execution should never get here, just being safe
            for (int cntr0 = 0; cntr0 < points0.Length; cntr0++)
            {
                for (int cntr1 = 0; cntr1 < points1.Length; cntr1++)
                {
                    if (points0[cntr0].IsNearValue(points1[cntr1]))
                    {
                        return points0[cntr0];
                    }
                }
            }

            throw new ApplicationException("Didn't find a common point");
        }

        internal static Vector3 GetUncommonPoint(ITriangle triangle, TriangleEdge edge0, TriangleEdge edge1)
        {
            Vector3[] points0 = new Vector3[] { triangle.GetPoint(edge0, true), triangle.GetPoint(edge0, false) };
            Vector3[] points1 = new Vector3[] { triangle.GetPoint(edge1, true), triangle.GetPoint(edge1, false) };

            // Find exact
            for (int cntr0 = 0; cntr0 < points0.Length; cntr0++)
            {
                for (int cntr1 = 0; cntr1 < points1.Length; cntr1++)
                {
                    if (points0[cntr0] == points1[cntr1])
                    {
                        return points0[cntr0 == 0 ? 1 : 0];     // return the one that isn't common
                    }
                }
            }

            // Find close - execution should never get here, just being safe
            for (int cntr0 = 0; cntr0 < points0.Length; cntr0++)
            {
                for (int cntr1 = 0; cntr1 < points1.Length; cntr1++)
                {
                    if (points0[cntr0].IsNearValue(points1[cntr1]))
                    {
                        return points0[cntr0 == 0 ? 1 : 0];     // return the one that isn't common
                    }
                }
            }

            throw new ApplicationException("Didn't find a common point");
        }

        internal static Vector3 GetOppositePoint(ITriangle triangle, TriangleEdge edge)
        {
            switch (edge)
            {
                case TriangleEdge.Edge_01:
                    return triangle.Point2;

                case TriangleEdge.Edge_12:
                    return triangle.Point0;

                case TriangleEdge.Edge_20:
                    return triangle.Point1;

                default:
                    throw new ApplicationException("Unknown TriangleEdge: " + edge.ToString());
            }
        }

        internal static Vector3 GetEdgeMidpoint(ITriangle triangle, TriangleEdge edge)
        {
            Vector3 point0 = triangle.GetPoint(edge, true);
            Vector3 point1 = triangle.GetPoint(edge, false);

            Vector3 halfLength = (point1 - point0) * .5f;

            return point0 + halfLength;
        }

        internal static float GetEdgeLength(ITriangle triangle, TriangleEdge edge)
        {
            Vector3 point0 = triangle.GetPoint(edge, true);
            Vector3 point1 = triangle.GetPoint(edge, false);

            return (point1 - point0).magnitude;
        }

        #endregion
        #region Protected Methods

        protected virtual void OnPointChanged()
        {
            _normal = null;
            _normalUnit = null;
            _normalLength = null;
            _planeDistance = null;
        }

        #endregion
    }

    #endregion
    #region class: TriangleThreadsafe

    /// <summary>
    /// This is a copy of Triangle, but is readonly
    /// NOTE: Only use this class if it's needed.  Extra stuff needs to be cached during the constructor, even if it will never be used, so this class is a bit more expensive
    /// </summary>
    public class TriangleThreadsafe : ITriangle
    {
        #region Constructor

        public TriangleThreadsafe(Vector3 point0, Vector3 point1, Vector3 point2, bool calculateNormalUpFront)
        {
            _point0 = point0;
            _point1 = point1;
            _point2 = point2;

            if (calculateNormalUpFront)
            {
                Triangle.CalculateNormal(out Vector3 normal, out float length, out Vector3 normalUnit, point0, point1, point2);

                _normal = normal;
                _normalLength = length;
                _normalUnit = normalUnit;

                _planeDistance = Math3D.GetPlaneOriginDistance(normalUnit, point0);
            }
            else
            {
                _normal = null;
                _normalLength = null;
                _normalUnit = null;
                _planeDistance = null;
            }

            _token = TokenGenerator.NextToken();
        }

        #endregion

        #region ITriangle Members

        private readonly Vector3 _point0;
        public Vector3 Point0 => _point0;

        private readonly Vector3 _point1;
        public Vector3 Point1 => _point1;

        private readonly Vector3 _point2;
        public Vector3 Point2 => _point2;

        public Vector3[] PointArray => new[] { _point0, _point1, _point2 };

        public Vector3 this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return this.Point0;

                    case 1:
                        return this.Point1;

                    case 2:
                        return this.Point2;

                    default:
                        throw new ArgumentOutOfRangeException("index", $"index can only be 0, 1, 2: {index}");
                }
            }
        }

        private readonly Vector3? _normal;
        /// <summary>
        /// This returns the triangle's normal.  Its length is the area of the triangle
        /// </summary>
        public Vector3 Normal
        {
            get
            {
                if (_normal == null)
                {
                    return Vector3.Cross(_point2 - _point1, _point0 - _point1);
                }
                else
                {
                    return _normal.Value;
                }
            }
        }
        private readonly Vector3? _normalUnit;
        /// <summary>
        /// This returns the triangle's normal.  Its length is one
        /// </summary>
        public Vector3 NormalUnit
        {
            get
            {
                if (_normalUnit == null)
                {
                    Triangle.CalculateNormal(out Vector3 normal, out float length, out Vector3 normalUnit, _point0, _point1, _point2);

                    return normalUnit;
                }
                else
                {
                    return _normalUnit.Value;
                }
            }
        }
        private readonly float? _normalLength;
        /// <summary>
        /// This returns the length of the normal (the area of the triangle)
        /// NOTE:  Call this if you just want to know the length of the normal, it's cheaper than calling this.Normal.Length, since it's already been calculated
        /// </summary>
        public float NormalLength
        {
            get
            {
                if (_normalLength == null)
                {
                    Triangle.CalculateNormal(out Vector3 normal, out float length, out Vector3 normalUnit, _point0, _point1, _point2);

                    return length;
                }
                else
                {
                    return _normalLength.Value;
                }
            }
        }

        private readonly float? _planeDistance;
        public float PlaneDistance
        {
            get
            {
                if (_planeDistance == null)
                {
                    return Math3D.GetPlaneOriginDistance(this.NormalUnit, _point0);
                }
                else
                {
                    return _planeDistance.Value;
                }
            }
        }

        private readonly long _token;
        public long Token => _token;

        public Vector3 GetCenterPoint()
        {
            return Triangle.GetCenterPoint(_point0, _point1, _point2);
        }
        public Vector3 GetPoint(TriangleEdge edge, bool isFrom)
        {
            return Triangle.GetPoint(this, edge, isFrom);
        }
        public Vector3 GetCommonPoint(TriangleEdge edge0, TriangleEdge edge1)
        {
            return Triangle.GetCommonPoint(this, edge0, edge1);
        }
        public Vector3 GetUncommonPoint(TriangleEdge edge0, TriangleEdge edge1)
        {
            return Triangle.GetUncommonPoint(this, edge0, edge1);
        }
        public Vector3 GetOppositePoint(TriangleEdge edge)
        {
            return Triangle.GetOppositePoint(this, edge);
        }
        public Vector3 GetEdgeMidpoint(TriangleEdge edge)
        {
            return Triangle.GetEdgeMidpoint(this, edge);
        }
        public float GetEdgeLength(TriangleEdge edge)
        {
            return Triangle.GetEdgeLength(this, edge);
        }

        #endregion
        #region IComparable<ITriangle> Members

        /// <summary>
        /// I wanted to be able to use triangles as keys in a sorted list
        /// </summary>
        public int CompareTo(ITriangle other)
        {
            if (other == null)
            {
                // I'm greater than null
                return 1;
            }

            if (this.Token < other.Token)
            {
                return -1;
            }
            else if (this.Token > other.Token)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This helps a lot when looking at lists of triangles in the quick watch
        /// </summary>
        public override string ToString()
        {
            return string.Format("({0}) ({1}) ({2})",
                _point0.ToStringSignificantDigits(2),
                _point1.ToStringSignificantDigits(2),
                _point2.ToStringSignificantDigits(2));
        }

        #endregion
    }

    #endregion

    #region interface: ITriangle

    //TODO:  May want more readonly statistics methods, like IsIntersecting, is Acute/Right/Obtuse
    public interface ITriangle : IComparable<ITriangle>
    {
        Vector3 Point0 { get; }
        Vector3 Point1 { get; }
        Vector3 Point2 { get; }

        Vector3[] PointArray { get; }

        Vector3 this[int index] { get; }

        Vector3 Normal { get; }
        /// <summary>
        /// This returns the triangle's normal.  Its length is one
        /// </summary>
        Vector3 NormalUnit { get; }
        /// <summary>
        /// This returns the length of the normal (the area of the triangle)
        /// NOTE:  Call this if you just want to know the length of the normal, it's cheaper than calling this.Normal.Length, since it's already been calculated
        /// </summary>
        float NormalLength { get; }

        /// <summary>
        /// This is useful for functions that use this triangle as the definition of a plane
        /// (normal * planeDist = 0)
        /// </summary>
        float PlaneDistance { get; }

        Vector3 GetCenterPoint();
        Vector3 GetPoint(TriangleEdge edge, bool isFrom);
        Vector3 GetCommonPoint(TriangleEdge edge0, TriangleEdge edge1);
        /// <summary>
        /// Returns the point in edge0 that isn't in edge1
        /// </summary>
        Vector3 GetUncommonPoint(TriangleEdge edge0, TriangleEdge edge1);
        Vector3 GetOppositePoint(TriangleEdge edge);
        Vector3 GetEdgeMidpoint(TriangleEdge edge);
        float GetEdgeLength(TriangleEdge edge);

        long Token { get; }
    }

    #endregion

    #region enum: TriangleEdge

    public enum TriangleEdge
    {
        Edge_01,
        Edge_12,
        Edge_20
    }

    #endregion
    #region enum: TriangleCorner

    public enum TriangleCorner
    {
        Corner_0,
        Corner_1,
        Corner_2
    }

    #endregion
}
