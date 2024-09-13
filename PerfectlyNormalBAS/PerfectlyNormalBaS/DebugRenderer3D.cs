using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PerfectlyNormalBaS
{
    /// <summary>
    /// This exposes simple functions that add visuals to the scene
    /// </summary>
    /// <remarks>
    /// This isn't meant to be used in your final product.  This is to help debug issues / quickly sketch out
    /// scenes
    /// 
    /// Note that these methods may not be optimal, the priority is having simple to use functions
    /// 
    /// To use this from one of your scripts:
    ///     DebugRenderer3D _debug;
    ///     _debug = gameObject.AddComponent{DebugRenderer3D}();
    ///     
    ///     Then just start calling add methods
    /// </remarks>
    public class DebugRenderer3D : MonoBehaviour
    {
        #region Declaration Section

        private const string PREFIX = "debug ";

        private const string AXISCOLOR_X = "FF6060";
        private const string AXISCOLOR_Y = "00C000";
        private const string AXISCOLOR_Z = "6060FF";

        private static long _token = 0;

        private GameObject _container = null;

        private readonly List<DebugItem> _stationary = new List<DebugItem>();
        private readonly List<DebugItem> _relativeTo = new List<DebugItem>();

        #endregion

        void Update()
        {
            //TODO: May want to also match orientation, also may want to apply the offset position relative to the chased object's orientation
            //Use item.RelativeToGameObject.TransformPoint()
            foreach (DebugItem item in _relativeTo)
            {
                if (item.RelativeToComponent != null)
                    item.Object.transform.position = item.RelativeToComponent.transform.position + item.Position;
                else if (item.RelativeToGameObject != null)
                    item.Object.transform.position = item.RelativeToGameObject.transform.position + item.Position;
            }
        }

        public DebugItem AddAxisLines(float length, float thickness, bool isBasic = true, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            EnsureContainerExists();

            GameObject parent = new GameObject();
            parent.name = PREFIX + "axis lines";
            parent.transform.SetParent(_container.transform, false);

            var children = new List<GameObject>();

            if (isBasic)
            {
                children.Add(GetNewBasicLine(new[] { new Vector3(0, 0, 0), new Vector3(length, 0, 0) }, thickness, UtilityUnity.ColorFromHex(AXISCOLOR_X), 0, 4, false, parent));
                children.Add(GetNewBasicLine(new[] { new Vector3(0, 0, 0), new Vector3(0, length, 0) }, thickness, UtilityUnity.ColorFromHex(AXISCOLOR_Y), 0, 4, false, parent));
                children.Add(GetNewBasicLine(new[] { new Vector3(0, 0, 0), new Vector3(0, 0, length) }, thickness, UtilityUnity.ColorFromHex(AXISCOLOR_Z), 0, 4, false, parent));
            }
            else
            {
                children.Add(GetNewPipeLine(new Vector3(0, 0, 0), new Vector3(length, 0, 0), thickness, UtilityUnity.ColorFromHex(AXISCOLOR_X), parent));
                children.Add(GetNewPipeLine(new Vector3(0, 0, 0), new Vector3(0, length, 0), thickness, UtilityUnity.ColorFromHex(AXISCOLOR_Y), parent));
                children.Add(GetNewPipeLine(new Vector3(0, 0, 0), new Vector3(0, 0, length), thickness, UtilityUnity.ColorFromHex(AXISCOLOR_Z), parent));
            }

            var retVal = new DebugItem(NextToken(), parent, children.ToArray(), new Vector3(), relativeToComponent, relativeToGameObject);

            AddItem(retVal);

            return retVal;
        }

        public DebugItem AddDot(Vector3 position, float radius, Color color, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            EnsureContainerExists();

            GameObject obj = GetNewDot(position, radius, color, _container);

            var retVal = new DebugItem(NextToken(), obj, null, position, relativeToComponent, relativeToGameObject);

            AddItem(retVal);

            return retVal;
        }
        public DebugItem AddDots(IEnumerable<Vector3> positions, float radius, Color color, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            EnsureContainerExists();

            GameObject parent = new GameObject();
            parent.name = PREFIX + "dots";
            parent.transform.SetParent(_container.transform, false);

            var children = new List<GameObject>();

            Vector3[] posArr = positions.ToArray();

            foreach (Vector3 pos in posArr)
            {
                children.Add(GetNewDot(pos, radius, color, parent));
            }

            var retVal = new DebugItem(NextToken(), parent, children.ToArray(), GetCenter(posArr), relativeToComponent, relativeToGameObject);

            AdjustColor(retVal, color);

            AddItem(retVal);

            return retVal;
        }

        /// <summary>
        /// This draws a line using the line renderer
        /// </summary>
        public DebugItem AddLine_Basic(Vector3 from, Vector3 to, float thickness, Color color, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            EnsureContainerExists();

            GameObject obj = GetNewBasicLine(new[] { from, to }, thickness, color, 0, 4, false, _container);

            var retVal = new DebugItem(NextToken(), obj, null, from, relativeToComponent, relativeToGameObject);

            AddItem(retVal);

            return retVal;
        }
        public DebugItem AddLine_Basic(Vector3[] points, bool isClosed, float thickness, Color color, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            EnsureContainerExists();

            GameObject obj = GetNewBasicLine(points, thickness, color, 4, 4, isClosed, _container);

            var retVal = new DebugItem(NextToken(), obj, null, GetCenter(points), relativeToComponent, relativeToGameObject);

            AddItem(retVal);

            return retVal;
        }

        /// <summary>
        /// This draws a line using a cylinder
        /// </summary>
        public DebugItem AddLine_Pipe(Vector3 from, Vector3 to, float thickness, Color color, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            EnsureContainerExists();

            GameObject obj = GetNewPipeLine(from, to, thickness, color, _container);

            var retVal = new DebugItem(NextToken(), obj, null, from, relativeToComponent, relativeToGameObject);

            AddItem(retVal);

            return retVal;
        }

        public DebugItem AddCube(Vector3 position, Vector3 size, Color color, Quaternion? rotation = null, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            EnsureContainerExists();

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = PREFIX + "cube";
            obj.transform.SetParent(_container.transform, false);

            RemoveCollider(obj);

            obj.transform.position = position;
            obj.transform.localScale = size;

            if (rotation != null)
                obj.transform.rotation = rotation.Value;

            AdjustColor(obj, color);

            var retVal = new DebugItem(NextToken(), obj, null, position, relativeToComponent, relativeToGameObject);

            AddItem(retVal);

            return retVal;
        }

        public DebugItem AddPlane(Plane plane, float size, Color color, int numCells = 12, Vector3? center = null, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            return AddPlane_PointNormal(plane.ClosestPointOnPlane(Vector3.zero), plane.normal, size, color, numCells, center, relativeToComponent, relativeToGameObject);
        }
        public DebugItem AddPlane_ThreePoints(Vector3 trianglePt1, Vector3 trianglePt2, Vector3 trianglePt3, float size, Color color, int numCells = 12, Vector3? center = null, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            //https://galasoft.ch/posts/2016/06/unity-adding-children-to-a-gameobject-in-code-and-retrieving-them

            EnsureContainerExists();

            GameObject parent = new GameObject();
            parent.name = PREFIX + "plane";
            parent.transform.SetParent(_container.transform, false);

            if (center != null)
                parent.transform.position = center.Value;
            else
                parent.transform.position = GetCenter(trianglePt1, trianglePt2, trianglePt3);

            parent.transform.rotation = Quaternion.FromToRotation(new Vector3(0, 1, 0), Vector3.Cross(trianglePt3 - trianglePt2, trianglePt1 - trianglePt2));

            var children = AddPlane_Children(parent, size, numCells, new Color(color.r, color.g, color.b, color.a * .25f));

            var retVal = new DebugItem(NextToken(), parent, children, new Vector3(), relativeToComponent, relativeToGameObject);

            //NOTE: If this is done here, it will also color the border line
            //AdjustColor(retVal, new Color(color.r, color.g, color.b, color.a * .25f));

            AddItem(retVal);

            return retVal;
        }
        public DebugItem AddPlane_PointNormal(Vector3 pointOnPlane, Vector3 normal, float size, Color color, int numCells = 12, Vector3? center = null, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            Vector3 dir1 = GetArbitraryOrhonganal(normal);
            Vector3 dir2 = Vector3.Cross(dir1, normal);

            return AddPlane_ThreePoints(pointOnPlane + dir1, pointOnPlane, pointOnPlane + dir2, size, color, numCells, center, relativeToComponent, relativeToGameObject);
        }
        public DebugItem AddPlane_PointVectors(Vector3 pointOnPlane, Vector3 direction1, Vector3 direction2, float size, Color color, int numCells = 12, Vector3? center = null, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            return AddPlane_ThreePoints(pointOnPlane + direction1, pointOnPlane, pointOnPlane + direction2, size, color, numCells, center, relativeToComponent, relativeToGameObject);
        }

        public DebugItem AddCircle(Vector3 position, Vector3 normal, float radius, float thickness, Color color, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            Quaternion quat = Quaternion.FromToRotation(new Vector3(0, 0, 1), normal);

            Vector2[] unit_circle = Math2D.GetCircle_Cached(36);

            Vector3[] points = new Vector3[unit_circle.Length];
            for (int i = 0; i < unit_circle.Length; i++)
            {
                points[i] = position + (quat * (new Vector3(unit_circle[i].x, unit_circle[i].y, 0) * radius));
            }

            return AddLine_Basic(points, true, thickness, color, relativeToComponent, relativeToGameObject);
        }

        public static void AdjustLinePositions(DebugItem item, Vector3 from, Vector3 to, float? thickness = null)
        {
            AdjustLinePositions(item.Object, from, to, thickness);
        }
        private static void AdjustLinePositions(GameObject obj, Vector3 from, Vector3 to, float? thickness = null)
        {
            LineRenderer line = obj.GetComponent<LineRenderer>();
            if (line != null)
            {
                if (line.positionCount > 2)
                    line.positionCount = 2;

                line.SetPosition(0, from);
                line.SetPosition(1, to);

                line.loop = false;
            }
            else
            {
                Vector3 directionHalf = (to - from) / 2f;

                obj.transform.position = from + directionHalf;

                obj.transform.localScale = thickness == null ?
                    new Vector3(obj.transform.localScale.x, directionHalf.magnitude, obj.transform.localScale.z) :
                    new Vector3(thickness.Value, directionHalf.magnitude, thickness.Value);

                obj.transform.rotation = Quaternion.FromToRotation(new Vector3(0, 1, 0), directionHalf);
            }
        }

        //TODO: public static void AdjustPlane(DebugItem item, Plane plane) -- and the other three

        public static void AdjustColor(DebugItem item, Color color)
        {
            AdjustColor(item.Object, color);

            if (item.ChildObjects != null)
            {
                foreach (GameObject child in item.ChildObjects)
                {
                    AdjustColor(child, color);
                }
            }
        }

        public static (float dot, float line) GetDrawSizes(float maxRadius)
        {
            return
            (
                maxRadius * .0075f,
                maxRadius * .005f
            );
        }

        public void Remove(DebugItem item)
        {
            var removeMatches = new Action<List<DebugItem>>(list =>
            {
                int index = 0;
                while (index < list.Count)
                {
                    if (list[index].Token == item.Token)
                    {
                        Destroy(list[index].Object);
                        list.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
            });

            removeMatches(_stationary);
            removeMatches(_relativeTo);
        }
        public void Remove(IEnumerable<DebugItem> items)
        {
            foreach (DebugItem item in items)
            {
                Remove(item);
            }
        }
        public void Clear()
        {
            foreach (DebugItem item in _stationary.Concat(_relativeTo))
            {
                Destroy(item.Object);
            }

            _stationary.Clear();
            _relativeTo.Clear();
        }

        #region Private Methods

        private void EnsureContainerExists()
        {
            if (_container == null)
                _container = new GameObject("DebugRenderer3D_Container");
        }

        private static long NextToken()
        {
            return System.Threading.Interlocked.Increment(ref _token);
        }

        private static void RemoveCollider(GameObject obj)
        {
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
        }

        private void AddItem(DebugItem item)
        {
            if (item.RelativeToComponent != null || item.RelativeToGameObject != null)
                _relativeTo.Add(item);
            else
                _stationary.Add(item);
        }

        private static void AdjustColor(GameObject obj, Color color)
        {
            MeshRenderer mesh = obj.GetComponent<MeshRenderer>();
            if (mesh != null)
            {
                AdjustColor(mesh, color);
                return;
            }

            LineRenderer line = obj.GetComponent<LineRenderer>();
            if (line != null)
            {
                line.startColor = color;
                line.endColor = color;

                if (line.material != null)
                    line.material.color = color;

                return;
            }

            // Just exit silently
        }
        private static void AdjustColor(MeshRenderer renderer, Color color)
        {
            if (renderer == null)
                return;

            renderer.material.color = color;

            if (color.a < 1f)       // default is an opaque mode (if the opacity goes back to 1, just leave it as transparent mode)
            {
                // This seems really hacky, but two websites suggest the same thing
                //https://answers.unity.com/questions/1004666/change-material-rendering-mode-in-runtime.html

                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.DisableKeyword("_ALPHABLEND_ON");
                renderer.material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = 3000;
            }
        }

        private static GameObject[] AddPlane_Children(GameObject parent, float size, int numCells, Color color)
        {
            // Tiles
            Quaternion cellRotation_up = Quaternion.Euler(90, 0, 0);
            Quaternion cellRotation_down = Quaternion.Euler(-90, 0, 0);

            var objects = new List<GameObject>();

            foreach (var pos in EnumeratePlaneTilePositions(size, numCells))
            {
                objects.Add(AddPlane_Children_Single(pos.position, pos.cellSize, parent, cellRotation_up));
                objects.Add(AddPlane_Children_Single(pos.position, pos.cellSize, parent, cellRotation_down));
            }

            foreach (GameObject obj in objects)
                AdjustColor(obj, color);

            // Lines
            float halfSize = size / 2f;
            Vector3[] points = new[]
            {
                new Vector3(-halfSize, 0, -halfSize),
                new Vector3(halfSize, 0, -halfSize),
                new Vector3(halfSize, 0, halfSize),
                new Vector3(-halfSize, 0, halfSize),
            };

            objects.Add(GetNewBasicLine(points, size / 666.6667f, new Color(.33f, .33f, .33f), 0, 0, true, parent));

            return objects.ToArray();
        }
        private static GameObject AddPlane_Children_Single(Vector2 position, float cellSize, GameObject parent, Quaternion cellRotation)
        {
            GameObject retVal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            retVal.name = "cell";

            RemoveCollider(retVal);

            retVal.transform.SetParent(parent.transform, false);

            retVal.transform.localRotation = cellRotation;
            retVal.transform.localPosition = new Vector3(position.x, 0, position.y);
            retVal.transform.localScale = new Vector3(cellSize, cellSize, 1);

            return retVal;
        }

        private static GameObject GetNewDot(Vector3 position, float radius, Color color, GameObject parent = null)
        {
            GameObject retVal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            retVal.name = PREFIX + "dot";

            if (parent != null)
                retVal.transform.SetParent(parent.transform, false);

            RemoveCollider(retVal);

            retVal.transform.position = position;
            retVal.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);

            AdjustColor(retVal, color);

            return retVal;
        }

        private static GameObject GetNewBasicLine(Vector3[] points, float thickness, Color color, int numCornerVertices, int numCapVertices, bool shouldLoop, GameObject parent = null)
        {
            GameObject retVal = new GameObject(PREFIX + "line (basic)");

            if (parent != null)
                retVal.transform.SetParent(parent.transform, false);

            LineRenderer line = retVal.AddComponent<LineRenderer>();

            line.startWidth = thickness;
            line.endWidth = thickness;

            line.startColor = color;
            line.endColor = color;

            line.useWorldSpace = false;     // this is false by default in the editor, but true by default here
            line.positionCount = points.Length;
            for (int cntr = 0; cntr < points.Length; cntr++)
            {
                line.SetPosition(cntr, points[cntr]);
            }

            line.loop = shouldLoop;

            //line.material = new Material(Shader.Find("Unlit/Texture"));       //NOTE: every example I see only uses this string, but it's color that's wanted, not texture
            line.material = new Material(Shader.Find("Unlit/Color"));
            line.material.color = color;

            line.numCornerVertices = numCornerVertices;
            line.numCapVertices = numCapVertices;

            return retVal;
        }
        private GameObject GetNewPipeLine(Vector3 from, Vector3 to, float thickness, Color color, GameObject parent = null)
        {
            GameObject retVal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            retVal.name = PREFIX + "line (pipe)";

            if (parent != null)
                retVal.transform.SetParent(parent.transform, false);

            RemoveCollider(retVal);
            AdjustLinePositions(retVal, from, to, thickness);
            AdjustColor(retVal, color);

            return retVal;
        }

        private static IEnumerable<(Vector2 position, float cellSize)> EnumeratePlaneTilePositions(float size, int numCells)        //NOTE: cellSize is the same for all cells, but there's no clean way to return this back to the caller
        {
            const float GAPRATIO = .8f;

            numCells = Math.Max(3, numCells);       // the positioning doesn't work with one cell (the plane graphic doesn't look very good with so few tiles anyway)

            float halfSize = size / 2f;

            int numGaps = numCells - 1;

            float totalGapSize = size * GAPRATIO;

            float gapSize = numGaps > 0 ?
                totalGapSize / (float)numGaps :
                0f;

            float cellSize = (size - totalGapSize) / numCells;
            float halfCellSize = cellSize / 2f;

            float nexty = -halfSize + halfCellSize;

            for (int y = 0; y < numCells; y++)
            {
                float nextX = -halfSize + halfCellSize;

                for (int x = 0; x < numCells; x++)
                {
                    yield return (new Vector2(nextX, nexty), cellSize);

                    nextX += gapSize + cellSize;
                }

                nexty += gapSize + cellSize;
            }
        }

        #endregion
        #region Private Methods - utils

        //TODO: Move these into dedicated classes

        private static Vector3 GetArbitraryOrhonganal(Vector3 vector)
        {
            if (IsInvalid(vector) || Mathf.Approximately(vector.sqrMagnitude, 0f))
            {
                return new Vector3(float.NaN, float.NaN, float.NaN);
            }

            Vector3 rand = UnityEngine.Random.onUnitSphere;

            for (int cntr = 0; cntr < 10; cntr++)
            {
                Vector3 retVal = Vector3.Cross(vector, rand);

                if (IsInvalid(retVal))
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

        private static bool IsInvalid(Vector3 testVect)
        {
            return IsInvalid(testVect.x) || IsInvalid(testVect.y) || IsInvalid(testVect.z);
        }
        private static bool IsInvalid(float testValue)
        {
            return float.IsNaN(testValue) || float.IsInfinity(testValue);
        }

        /// <summary>
        /// This returns the center of position of the points
        /// </summary>
        /// <remarks>
        /// NOTE: This was originally written to take in an IEnumerable.  Left the code alone, so it could be easily swapped back
        /// </remarks>
        private static Vector3 GetCenter(params Vector3[] points)
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

        #endregion
    }

    #region class: DebugItem

    public class DebugItem
    {
        public DebugItem(long token, GameObject obj, GameObject[] childObjects, Vector3 position, Component relativeToComponent, GameObject relativeToGameObject)
        {
            Token = token;
            Object = obj;
            ChildObjects = childObjects;
            Position = position;
            RelativeToComponent = relativeToComponent;
            RelativeToGameObject = relativeToGameObject;
        }

        public long Token { get; }
        public GameObject Object { get; }
        public GameObject[] ChildObjects { get; }

        public Vector3 Position { get; }
        public Component RelativeToComponent { get; }
        public GameObject RelativeToGameObject { get; }
    }

    #endregion
}
