using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThunderRoad;
using UnityEngine;
using UnityEngine.UI;       // NOTE: had to dig to find this, it's not with the other unity dlls that this project references - also difficulty with netstandard2.0 vs 2.1

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
    ///     or use DebugRenderer3D.GetOrAddDebugRenderer3D(Player.local.gameObject);        // use this if multiple mods will possibly try to draw at the same time
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

        private const int FONTSIZE = 12;

        private static long _token = 0;

        private GameObject _container = null;

        private readonly List<DebugItem> _stationary = new List<DebugItem>();
        private readonly List<DebugItem> _relativeTo = new List<DebugItem>();

        private static Lazy<(Vector3, Vector3)[]> _icolines_lowres = new Lazy<(Vector3, Vector3)[]>(() => GetIcosahedronLines(true));
        private static Lazy<(Vector3, Vector3)[]> _icolines_highres = new Lazy<(Vector3, Vector3)[]>(() => GetIcosahedronLines(false));

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

        public static DebugRenderer3D GetOrAddDebugRenderer3D(GameObject gameobject = null)
        {
            if (gameobject == null)
                gameobject = Player.local.gameObject;

            DebugRenderer3D retVal = gameobject.GetComponent<DebugRenderer3D>();
            if (retVal == null)
                retVal = gameobject.AddComponent<DebugRenderer3D>();

            return retVal;
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
                children.Add(GetNewBasicLine(new[] { new Vector3(0, 0, 0), new Vector3(length, 0, 0) }, thickness, UtilityColor.FromHex(AXISCOLOR_X), 0, 4, false, parent));
                children.Add(GetNewBasicLine(new[] { new Vector3(0, 0, 0), new Vector3(0, length, 0) }, thickness, UtilityColor.FromHex(AXISCOLOR_Y), 0, 4, false, parent));
                children.Add(GetNewBasicLine(new[] { new Vector3(0, 0, 0), new Vector3(0, 0, length) }, thickness, UtilityColor.FromHex(AXISCOLOR_Z), 0, 4, false, parent));
            }
            else
            {
                children.Add(GetNewPipeLine(new Vector3(0, 0, 0), new Vector3(length, 0, 0), thickness, UtilityColor.FromHex(AXISCOLOR_X), parent));
                children.Add(GetNewPipeLine(new Vector3(0, 0, 0), new Vector3(0, length, 0), thickness, UtilityColor.FromHex(AXISCOLOR_Y), parent));
                children.Add(GetNewPipeLine(new Vector3(0, 0, 0), new Vector3(0, 0, length), thickness, UtilityColor.FromHex(AXISCOLOR_Z), parent));
            }

            var retVal = new DebugItem(NextToken(), parent, children.ToArray(), new Vector3(), relativeToComponent, relativeToGameObject, true);

            AddItem(retVal);

            return retVal;
        }
        public DebugItem AddAxisLines(Transform transform, float length, float thickness, bool isBasic = true)
        {
            EnsureContainerExists();

            GameObject parent = new GameObject();
            parent.name = PREFIX + "axis lines";
            parent.transform.SetParent(_container.transform, false);

            var children = new List<GameObject>();

            if (isBasic)
            {
                children.Add(GetNewBasicLine(new[] { transform.position, transform.position + transform.rotation * new Vector3(length, 0, 0) }, thickness, UtilityColor.FromHex(AXISCOLOR_X), 0, 4, false, parent));
                children.Add(GetNewBasicLine(new[] { transform.position, transform.position + transform.rotation * new Vector3(0, length, 0) }, thickness, UtilityColor.FromHex(AXISCOLOR_Y), 0, 4, false, parent));
                children.Add(GetNewBasicLine(new[] { transform.position, transform.position + transform.rotation * new Vector3(0, 0, length) }, thickness, UtilityColor.FromHex(AXISCOLOR_Z), 0, 4, false, parent));
            }
            else
            {
                children.Add(GetNewPipeLine(transform.position, transform.position + transform.rotation * new Vector3(length, 0, 0), thickness, UtilityColor.FromHex(AXISCOLOR_X), parent));
                children.Add(GetNewPipeLine(transform.position, transform.position + transform.rotation * new Vector3(0, length, 0), thickness, UtilityColor.FromHex(AXISCOLOR_Y), parent));
                children.Add(GetNewPipeLine(transform.position, transform.position + transform.rotation * new Vector3(0, 0, length), thickness, UtilityColor.FromHex(AXISCOLOR_Z), parent));
            }

            var retVal = new DebugItem(NextToken(), parent, children.ToArray(), new Vector3(), null, null, true);

            AddItem(retVal);

            return retVal;
        }

        public DebugItem AddDot(Vector3 position, float radius, Color color, bool isLit = false, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            EnsureContainerExists();

            GameObject obj = GetNewDot(position, radius, color, isLit, _container);

            var retVal = new DebugItem(NextToken(), obj, null, position, relativeToComponent, relativeToGameObject, isLit);

            AddItem(retVal);

            return retVal;
        }
        public DebugItem AddDots(IEnumerable<Vector3> positions, float radius, Color color, bool isLit = false, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            EnsureContainerExists();

            GameObject parent = new GameObject();
            parent.name = PREFIX + "dots";
            parent.transform.SetParent(_container.transform, false);

            var children = new List<GameObject>();

            Vector3[] posArr = positions.ToArray();

            foreach (Vector3 pos in posArr)
                children.Add(GetNewDot(pos, radius, color, isLit, parent));

            var retVal = new DebugItem(NextToken(), parent, children.ToArray(), GetCenter(posArr), relativeToComponent, relativeToGameObject, isLit);

            //AdjustColor(retVal, color);       // GetNewDot already set the color

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

            var retVal = new DebugItem(NextToken(), obj, null, from, relativeToComponent, relativeToGameObject, true);

            AddItem(retVal);

            return retVal;
        }


        private DebugItem AddLine_Basic_FAIL(Vector3[] points, bool isClosed, float thickness, Color color, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            EnsureContainerExists();

            GameObject obj = GetNewBasicLine(points, thickness, color, 4, 4, isClosed, _container);

            var retVal = new DebugItem(NextToken(), obj, null, GetCenter(points), relativeToComponent, relativeToGameObject, true);

            AddItem(retVal);

            return retVal;
        }
        public DebugItem AddLine_Basic(Vector3[] points, bool isClosed, float thickness, Color color, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            var segments = new List<(Vector3, Vector3)>();

            for (int i = 0; i < points.Length - 1; i++)
                segments.Add((points[i], points[i + 1]));

            if (isClosed)
                segments.Add((points[points.Length - 1], points[0]));

            return AddLine_Basic(segments.ToArray(), thickness, color, relativeToComponent, relativeToGameObject);
        }


        public DebugItem AddLine_Basic((Vector3, Vector3)[] segments, float thickness, Color color, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            EnsureContainerExists();

            GameObject parent = new GameObject();
            parent.name = PREFIX + "basic line segments";
            parent.transform.SetParent(_container.transform, false);

            var children = new List<GameObject>();

            for (int i = 0; i < segments.Length; i++)
                children.Add(GetNewBasicLine(new[] { segments[i].Item1, segments[i].Item2 }, thickness, color, 0, 4, false, parent));

            var retVal = new DebugItem(NextToken(), parent, children.ToArray(), new Vector3(), relativeToComponent, relativeToGameObject, true);

            AddItem(retVal);

            return retVal;
        }

        /// <summary>
        /// This draws a line using a cylinder
        /// </summary>
        public DebugItem AddLine_Pipe(Vector3 from, Vector3 to, float thickness, Color color, bool isLit = false, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            EnsureContainerExists();

            GameObject obj = GetNewPipeLine(from, to, thickness, color, isLit, _container);

            var retVal = new DebugItem(NextToken(), obj, null, from, relativeToComponent, relativeToGameObject, isLit);

            AddItem(retVal);

            return retVal;
        }
        public DebugItem AddLine_Pipe((Vector3, Vector3)[] segments, float thickness, Color color, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            EnsureContainerExists();

            GameObject parent = new GameObject();
            parent.name = PREFIX + "pipe line segments";
            parent.transform.SetParent(_container.transform, false);

            var children = new List<GameObject>();

            for (int i = 0; i < segments.Length; i++)
                children.Add(GetNewPipeLine(segments[i].Item1, segments[i].Item2, thickness, color, parent));

            var retVal = new DebugItem(NextToken(), parent, children.ToArray(), new Vector3(), relativeToComponent, relativeToGameObject, true);

            AddItem(retVal);

            return retVal;
        }

        public DebugItem AddCube(Vector3 position, Vector3 size, Color color, bool isLit = false, Quaternion? rotation = null, Component relativeToComponent = null, GameObject relativeToGameObject = null)
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

            AdjustColor(obj, color, isLit, true);

            var retVal = new DebugItem(NextToken(), obj, null, position, relativeToComponent, relativeToGameObject, isLit);

            AddItem(retVal);

            return retVal;
        }

        public DebugItem AddPlane(Plane plane, float size, Color color, bool isLit = false, int numCells = 12, Vector3? center = null, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            return AddPlane_PointNormal(plane.ClosestPointOnPlane(Vector3.zero), plane.normal, size, color, isLit, numCells, center, relativeToComponent, relativeToGameObject);
        }
        public DebugItem AddPlane_ThreePoints(Vector3 trianglePt1, Vector3 trianglePt2, Vector3 trianglePt3, float size, Color color, bool isLit = false, int numCells = 12, Vector3? center = null, Component relativeToComponent = null, GameObject relativeToGameObject = null)
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

            var children = AddPlane_Children(parent, size, numCells, new Color(color.r, color.g, color.b, color.a * .25f), isLit);

            var retVal = new DebugItem(NextToken(), parent, children, new Vector3(), relativeToComponent, relativeToGameObject, isLit);

            //NOTE: If this is done here, it will also color the border line
            //AdjustColor(retVal, new Color(color.r, color.g, color.b, color.a * .25f));

            AddItem(retVal);

            return retVal;
        }
        public DebugItem AddPlane_PointNormal(Vector3 pointOnPlane, Vector3 normal, float size, Color color, bool isLit = false, int numCells = 12, Vector3? center = null, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            Vector3 dir1 = GetArbitraryOrthonganal(normal);
            Vector3 dir2 = Vector3.Cross(dir1, normal);

            return AddPlane_ThreePoints(pointOnPlane + dir1, pointOnPlane, pointOnPlane + dir2, size, color, isLit, numCells, center, relativeToComponent, relativeToGameObject);
        }
        public DebugItem AddPlane_PointVectors(Vector3 pointOnPlane, Vector3 direction1, Vector3 direction2, float size, Color color, bool isLit = false, int numCells = 12, Vector3? center = null, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            return AddPlane_ThreePoints(pointOnPlane + direction1, pointOnPlane, pointOnPlane + direction2, size, color, isLit, numCells, center, relativeToComponent, relativeToGameObject);
        }

        public DebugItem AddCircle(Vector3 position, Vector3 normal, float radius, float thickness, Color color, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            Quaternion quat = Quaternion.FromToRotation(new Vector3(0, 0, 1), normal);

            Vector2[] unit_circle = Math2D.GetCircle_Cached(36);

            Vector3[] points = new Vector3[unit_circle.Length];
            for (int i = 0; i < unit_circle.Length; i++)
                points[i] = position + (quat * (new Vector3(unit_circle[i].x, unit_circle[i].y, 0) * radius));

            Debug.Log($"creating circle: {string.Join(" | ", points.Select(o => o.ToStringSignificantDigits(3)))}");

            return AddLine_Basic(points, true, thickness, color, relativeToComponent, relativeToGameObject);
        }

        public DebugItem AddWireframeSphere(Vector3 position, float radius, float line_thickness, Color color, bool isBasic = true, bool isLowRes = true, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            // Get the precalculated lines
            var lines_shared = isLowRes ?
                _icolines_lowres.Value :
                _icolines_highres.Value;

            // Random rotation
            Quaternion quat = StaticRandom.RotationUniform();

            var lines = new (Vector3, Vector3)[lines_shared.Length];        // need to store the tranformed vectors in a new array

            for (int i = 0; i < lines.Length; i++)
                lines[i] = (position + quat * lines_shared[i].Item1 * radius, position + quat * lines_shared[i].Item2 * radius);

            // Call the line segments overload
            return isBasic ?
                AddLine_Basic(lines, line_thickness, color, relativeToComponent, relativeToGameObject) :
                AddLine_Pipe(lines, line_thickness, color, relativeToComponent, relativeToGameObject);
        }

        public DebugItem AddText(string text, Vector3 pos, Vector3 normal, Color back_color, Color fore_color, float world_height, Component relativeToComponent = null, GameObject relativeToGameObject = null)
        {
            var (canvasObj, canvas) = AddText_CreateCanvas(pos, normal);
            GameObject image = AddText_CreateImage(canvas.transform, back_color);
            var (textObj, textComp) = AddText_CreateText(canvas.transform, text, fore_color);

            // Apply results to text and image
            textComp.fontSize = FONTSIZE;
            textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf") ??
                                   Font.CreateDynamicFontFromOSFont("Arial", FONTSIZE);

            // Calculate scale and dimensions
            var dimensions = AddText_CalculateImageDimensions(textComp, world_height, canvas.GetComponent<CanvasScaler>(), FONTSIZE);

            // Apply scale to text
            RectTransform textRect = textComp.rectTransform;
            textRect.localScale = Vector3.one * dimensions.textScale;

            // Apply image dimensions
            image.GetComponent<RectTransform>().sizeDelta = new Vector2(dimensions.imageWidth, world_height);

            // NOTE: if the array of child objects changes, be sure to also change the indices in AdjustText()
            var retVal = new DebugItem(NextToken(), canvasObj, new[] { image, textObj }, pos, relativeToComponent, relativeToGameObject, false);

            AddItem(retVal);

            return retVal;
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

        // TODO: public static void AdjustPlane(DebugItem item, Plane plane) -- and the other three

        public static void AdjustText(DebugItem item, string new_text = null, Color? new_backcolor = null, Color? new_forecolor = null, float? new_worldheight = null)
        {
            const int INDEX_IMAGE = 0;
            const int INDEX_TEXT = 1;

            bool recalc_size = false;

            Text textComp = null;
            Image imageComp = null;

            // Set text string
            if (new_text != null)
            {
                recalc_size = true;

                if (textComp == null)
                    textComp = item.ChildObjects[INDEX_TEXT].GetComponent<Text>();

                textComp.text = new_text;
            }

            // Set back color
            if (new_backcolor != null)
            {
                if (imageComp == null)
                    imageComp = item.ChildObjects[INDEX_IMAGE].GetComponent<Image>();

                imageComp.color = new_backcolor.Value;
            }

            // Set fore color
            if (new_forecolor != null)
            {
                if (textComp == null)
                    textComp = item.ChildObjects[INDEX_TEXT].GetComponent<Text>();

                textComp.color = new_forecolor.Value;
            }

            // See if world height changed
            if (new_worldheight != null)
                recalc_size = true;

            // Change size of image and text
            if (recalc_size)
            {
                if (textComp == null)
                    textComp = item.ChildObjects[INDEX_TEXT].GetComponent<Text>();

                if (imageComp == null)
                    imageComp = item.ChildObjects[INDEX_IMAGE].GetComponent<Image>();

                RectTransform image_rect = item.ChildObjects[INDEX_IMAGE].GetComponent<RectTransform>();

                float world_height = new_worldheight ?? image_rect.sizeDelta.y;

                var dimensions = AddText_CalculateImageDimensions(textComp, world_height, item.Object.GetComponent<CanvasScaler>(), FONTSIZE);      // item.Object is the gameobject of the canvas

                textComp.rectTransform.localScale = Vector3.one * dimensions.textScale;
                image_rect.sizeDelta = new Vector2(dimensions.imageWidth, world_height);
            }
        }

        public static void AdjustColor(DebugItem item, Color color)
        {
            AdjustColor(item.Object, color, item.IsLit, false);

            if (item.ChildObjects != null)
                foreach (GameObject child in item.ChildObjects)
                    AdjustColor(child, color, item.IsLit, false);
        }

        public static (float dot, float line) GetDrawSizes(float maxRadius)
        {
            return
            (
                maxRadius * .0075f,
                maxRadius * .005f
            );
        }

        public bool Remove(DebugItem item)
        {
            var removeMatches = new Func<List<DebugItem>, bool>(list =>
            {
                bool was_removed = false;
                int index = 0;
                while (index < list.Count)
                {
                    if (list[index].Token == item.Token)
                    {
                        Destroy(list[index].Object);
                        list.RemoveAt(index);
                        was_removed = true;
                    }
                    else
                    {
                        index++;
                    }
                }

                return was_removed;
            });

            bool retVal = false;

            retVal |= removeMatches(_stationary);
            retVal |= removeMatches(_relativeTo);

            return retVal;
        }
        public bool Remove(IEnumerable<DebugItem> items)
        {
            bool retVal = false;

            foreach (DebugItem item in items)
                retVal |= Remove(item);

            return retVal;
        }
        public void Clear()
        {
            foreach (DebugItem item in _stationary.Concat(_relativeTo))
                Destroy(item.Object);

            _stationary.Clear();
            _relativeTo.Clear();
        }

        public string ReportCurrentTokens()
        {
            string stationary = string.Join(", ", _stationary.Select(o => o.Token.ToString()));
            string relative = string.Join(", ", _relativeTo.Select(o => o.Token.ToString()));

            return $"stationary: {stationary}{Environment.NewLine}relative: {relative}";
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

        private static void AdjustColor(GameObject obj, Color color, bool isLit, bool isNewItem)
        {
            MeshRenderer mesh = obj.GetComponent<MeshRenderer>();
            if (mesh != null)
            {
                AdjustColor(mesh, color, isLit, isNewItem);
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
        private static void AdjustColor(MeshRenderer renderer, Color color, bool isLit, bool isNewItem)
        {
            if (renderer == null)
                return;

            if (isNewItem)
            {

                // TODO: figure out Lit mode (only unlit is working)

                if (isLit)
                {
                    string[] keys = new[]
                    {
                        "LitMoss",
                        "ThunderRoad/LitMoss",
                        "ASshader/LitMoss",
                        "ThunderRoad/ASshader/LitMoss",
                    };

                    var results = keys.
                        Select(o => new
                        {
                            key = o,
                            shader = Shader.Find(o),
                        }).
                        Where(o => o.shader != null).
                        ToArray();

                    if (results.Length == 0)
                    {
                        Debug.Log("Couldn't find LitMoss");     // this is the message that fires
                    }
                    else
                    {
                        Debug.Log($"Found LitMoss: {results.Select(o => $"'{o.key}'").ToJoin(", ")}");
                        renderer.material.shader = results[0].shader;
                    }
                }
                else
                {
                    renderer.material.shader = Shader.Find("Sprites/Default");      // BREAD — the default shader that's applied isn't in the game, need to change it
                }
            }

            //if (isLit)
            //renderer.material.SetColor("_BaseColor", color);        // also try _Color
            //else
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

        private static GameObject[] AddPlane_Children(GameObject parent, float size, int numCells, Color color, bool isLit)
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
                AdjustColor(obj, color, isLit, true);

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

        private static GameObject GetNewDot(Vector3 position, float radius, Color color, bool isLit, GameObject parent = null)
        {
            GameObject retVal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            retVal.name = PREFIX + "dot";

            if (parent != null)
                retVal.transform.SetParent(parent.transform, false);

            RemoveCollider(retVal);

            retVal.transform.position = position;
            retVal.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);

            AdjustColor(retVal, color, isLit, true);

            return retVal;
        }

        private static GameObject GetNewBasicLine(Vector3[] points, float thickness, Color color, int numCornerVertices, int numCapVertices, bool shouldLoop, GameObject parent = null)
        {
            if (shouldLoop)
                points = UtilityCore.ArrayAdd(points, points[0]);       // telling it to loop made it invisible, add the extra point manually

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
            for (int i = 0; i < points.Length; i++)
                line.SetPosition(i, points[i]);

            //line.loop = shouldLoop;       // nothing was showing when loop is true
            line.loop = false;

            //line.material = new Material(Shader.Find("Unlit/Texture"));       //NOTE: every example I see only uses this string, but it's color that's wanted, not texture
            //line.material = new Material(Shader.Find("Unlit/Color"));
            line.material = new Material(Shader.Find("Sprites/Default"));       // blade and scorcery's unlit shader
            line.material.color = color;

            line.numCornerVertices = numCornerVertices;
            line.numCapVertices = numCapVertices;

            return retVal;
        }
        private GameObject GetNewPipeLine(Vector3 from, Vector3 to, float thickness, Color color, bool isLit, GameObject parent = null)
        {
            GameObject retVal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            retVal.name = PREFIX + "line (pipe)";

            if (parent != null)
                retVal.transform.SetParent(parent.transform, false);

            RemoveCollider(retVal);
            AdjustLinePositions(retVal, from, to, thickness);
            AdjustColor(retVal, color, isLit, true);

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

        private (GameObject obj, Canvas canvas) AddText_CreateCanvas(Vector3 position, Vector3 normal)
        {
            // Creates a world-space Canvas

            GameObject canvas = new GameObject("DynamicCanvas");

            canvas.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            canvas.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 12;     //100;

            canvas.AddComponent<GraphicRaycaster>();
            canvas.transform.position = position;
            canvas.transform.rotation = Quaternion.LookRotation(normal, Player.local.head.transform.up);


            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 1);     // sets width and height of the canvas to 1 (instead of default of 100).  it doesn't matter much since it's not visible and children overflow, but 100 seems excessive

            return (canvas, canvas.GetComponent<Canvas>());
        }
        private GameObject AddText_CreateImage(Transform parent, Color color)
        {
            // Creates a textured background Image

            GameObject image = new GameObject("Image", typeof(Image));

            Image imageComp = image.GetComponent<Image>();
            imageComp.color = color;

            RectTransform rect = image.GetComponent<RectTransform>();

            // These statements set anchor to center
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            image.transform.SetParent(parent, false);

            return image;
        }
        private (GameObject obj, Text comp) AddText_CreateText(Transform parent, string text, Color color)
        {
            // Creates a Text component

            GameObject textObject = new GameObject("Text", typeof(Text));

            Text textComp = textObject.GetComponent<Text>();
            textComp.text = text;
            textComp.color = color;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            textComp.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = textComp.rectTransform;

            // These statements set anchor to center
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            rect.localScale = Vector3.one;

            textObject.transform.SetParent(parent, false);

            return (textObject, textComp);
        }
        private static (float textScale, float imageWidth) AddText_CalculateImageDimensions(Text textComponent, float desiredWorldHeight, CanvasScaler canvasScaler, float baseFontSize)
        {
            // Force layout update to get accurate preferred dimensions
            LayoutRebuilder.ForceRebuildLayoutImmediate(textComponent.rectTransform);

            // Calculate required scale factor
            float scale = desiredWorldHeight / textComponent.preferredHeight;

            // Now get the measured string width at the new scale to see how wide the image should be
            float imageWidth = textComponent.preferredWidth * scale;

            // Make text's scale slightly smaller so there are margins
            scale *= 0.9f;

            return (scale, imageWidth);
        }

        private static (Vector3, Vector3)[] GetIcosahedronLines(bool isLowRes)
        {
            // came from PartyPeople.UnitTests.IcoNormals_Click

            if (isLowRes)
                return new[]
                {
                    (new Vector3(-0.5257311f, 0.8506508f, 0f),  new Vector3(-0.8506508f, 0f, 0.5257311f)),
                    (new Vector3(0f, 0.5257311f, 0.8506508f),   new Vector3(-0.8506508f, 0f, 0.5257311f)),
                    (new Vector3(-0.5257311f, 0.8506508f, 0f),  new Vector3(0f, 0.5257311f, 0.8506508f)),
                    (new Vector3(0.5257311f, 0.8506508f, 0f),   new Vector3(0f, 0.5257311f, 0.8506508f)),
                    (new Vector3(-0.5257311f, 0.8506508f, 0f),  new Vector3(0.5257311f, 0.8506508f, 0f)),
                    (new Vector3(0.5257311f, 0.8506508f, 0f),   new Vector3(0f, 0.5257311f, -0.8506508f)),
                    (new Vector3(-0.5257311f, 0.8506508f, 0f),  new Vector3(0f, 0.5257311f, -0.8506508f)),
                    (new Vector3(0f, 0.5257311f, -0.8506508f),  new Vector3(-0.8506508f, 0f, -0.5257311f)),
                    (new Vector3(-0.5257311f, 0.8506508f, 0f),  new Vector3(-0.8506508f, 0f, -0.5257311f)),
                    (new Vector3(-0.8506508f, 0f, -0.5257311f), new Vector3(-0.8506508f, 0f, 0.5257311f)),
                    (new Vector3(0f, 0.5257311f, 0.8506508f),   new Vector3(0.8506508f, 0f, 0.5257311f)),
                    (new Vector3(0.5257311f, 0.8506508f, 0f),   new Vector3(0.8506508f, 0f, 0.5257311f)),
                    (new Vector3(0f, -0.5257311f, 0.8506508f),  new Vector3(-0.8506508f, 0f, 0.5257311f)),
                    (new Vector3(0f, -0.5257311f, 0.8506508f),  new Vector3(0f, 0.5257311f, 0.8506508f)),
                    (new Vector3(-0.5257311f, -0.8506508f, 0f), new Vector3(-0.8506508f, 0f, -0.5257311f)),
                    (new Vector3(-0.5257311f, -0.8506508f, 0f), new Vector3(-0.8506508f, 0f, 0.5257311f)),
                    (new Vector3(0f, -0.5257311f, -0.8506508f), new Vector3(0f, 0.5257311f, -0.8506508f)),
                    (new Vector3(0f, -0.5257311f, -0.8506508f), new Vector3(-0.8506508f, 0f, -0.5257311f)),
                    (new Vector3(0.5257311f, 0.8506508f, 0f),   new Vector3(0.8506508f, 0f, -0.5257311f)),
                    (new Vector3(0f, 0.5257311f, -0.8506508f),  new Vector3(0.8506508f, 0f, -0.5257311f)),
                    (new Vector3(0.5257311f, -0.8506508f, 0f),  new Vector3(0.8506508f, 0f, 0.5257311f)),
                    (new Vector3(0f, -0.5257311f, 0.8506508f),  new Vector3(0.8506508f, 0f, 0.5257311f)),
                    (new Vector3(0.5257311f, -0.8506508f, 0f),  new Vector3(0f, -0.5257311f, 0.8506508f)),
                    (new Vector3(-0.5257311f, -0.8506508f, 0f), new Vector3(0f, -0.5257311f, 0.8506508f)),
                    (new Vector3(-0.5257311f, -0.8506508f, 0f), new Vector3(0.5257311f, -0.8506508f, 0f)),
                    (new Vector3(-0.5257311f, -0.8506508f, 0f), new Vector3(0f, -0.5257311f, -0.8506508f)),
                    (new Vector3(0.5257311f, -0.8506508f, 0f),  new Vector3(0f, -0.5257311f, -0.8506508f)),
                    (new Vector3(0f, -0.5257311f, -0.8506508f), new Vector3(0.8506508f, 0f, -0.5257311f)),
                    (new Vector3(0.5257311f, -0.8506508f, 0f),  new Vector3(0.8506508f, 0f, -0.5257311f)),
                    (new Vector3(0.8506508f, 0f, -0.5257311f),  new Vector3(0.8506508f, 0f, 0.5257311f)),
                };

            else
                return new[]
                {
                    (new Vector3(-0.5257311f, 0.8506508f, 0f),  new Vector3(-0.809017f, 0.5f, 0.309017f)),
                    (new Vector3(-0.809017f, 0.5f, 0.309017f),  new Vector3(-0.309017f, 0.809017f, 0.5f)),
                    (new Vector3(-0.5257311f, 0.8506508f, 0f),  new Vector3(-0.309017f, 0.809017f, 0.5f)),
                    (new Vector3(-0.8506508f, 0f, 0.5257311f),  new Vector3(-0.5f, 0.309017f, 0.809017f)),
                    (new Vector3(-0.809017f, 0.5f, 0.309017f),  new Vector3(-0.5f, 0.309017f, 0.809017f)),
                    (new Vector3(-0.8506508f, 0f, 0.5257311f),  new Vector3(-0.809017f, 0.5f, 0.309017f)),
                    (new Vector3(0f, 0.5257311f, 0.8506508f),   new Vector3(-0.309017f, 0.809017f, 0.5f)),
                    (new Vector3(-0.5f, 0.309017f, 0.809017f),  new Vector3(-0.309017f, 0.809017f, 0.5f)),
                    (new Vector3(0f, 0.5257311f, 0.8506508f),   new Vector3(-0.5f, 0.309017f, 0.809017f)),
                    (new Vector3(-0.309017f, 0.809017f, 0.5f),  new Vector3(0f, 1f, 0f)),
                    (new Vector3(-0.5257311f, 0.8506508f, 0f),  new Vector3(0f, 1f, 0f)),
                    (new Vector3(0f, 0.5257311f, 0.8506508f),   new Vector3(0.309017f, 0.809017f, 0.5f)),
                    (new Vector3(-0.309017f, 0.809017f, 0.5f),  new Vector3(0.309017f, 0.809017f, 0.5f)),
                    (new Vector3(0.5257311f, 0.8506508f, 0f),   new Vector3(0f, 1f, 0f)),
                    (new Vector3(0.309017f, 0.809017f, 0.5f),   new Vector3(0f, 1f, 0f)),
                    (new Vector3(0.5257311f, 0.8506508f, 0f),   new Vector3(0.309017f, 0.809017f, 0.5f)),
                    (new Vector3(0f, 1f, 0f),                   new Vector3(-0.309017f, 0.809017f, -0.5f)),
                    (new Vector3(-0.5257311f, 0.8506508f, 0f),  new Vector3(-0.309017f, 0.809017f, -0.5f)),
                    (new Vector3(0.5257311f, 0.8506508f, 0f),   new Vector3(0.309017f, 0.809017f, -0.5f)),
                    (new Vector3(0f, 1f, 0f),                   new Vector3(0.309017f, 0.809017f, -0.5f)),
                    (new Vector3(0f, 0.5257311f, -0.8506508f),  new Vector3(-0.309017f, 0.809017f, -0.5f)),
                    (new Vector3(0.309017f, 0.809017f, -0.5f),  new Vector3(-0.309017f, 0.809017f, -0.5f)),
                    (new Vector3(0f, 0.5257311f, -0.8506508f),  new Vector3(0.309017f, 0.809017f, -0.5f)),
                    (new Vector3(-0.309017f, 0.809017f, -0.5f), new Vector3(-0.809017f, 0.5f, -0.309017f)),
                    (new Vector3(-0.5257311f, 0.8506508f, 0f),  new Vector3(-0.809017f, 0.5f, -0.309017f)),
                    (new Vector3(0f, 0.5257311f, -0.8506508f),  new Vector3(-0.5f, 0.309017f, -0.809017f)),
                    (new Vector3(-0.309017f, 0.809017f, -0.5f), new Vector3(-0.5f, 0.309017f, -0.809017f)),
                    (new Vector3(-0.8506508f, 0f, -0.5257311f), new Vector3(-0.809017f, 0.5f, -0.309017f)),
                    (new Vector3(-0.5f, 0.309017f, -0.809017f), new Vector3(-0.809017f, 0.5f, -0.309017f)),
                    (new Vector3(-0.8506508f, 0f, -0.5257311f), new Vector3(-0.5f, 0.309017f, -0.809017f)),
                    (new Vector3(-0.809017f, 0.5f, 0.309017f),  new Vector3(-0.809017f, 0.5f, -0.309017f)),
                    (new Vector3(-0.8506508f, 0f, -0.5257311f), new Vector3(-1f, 0f, 0f)),
                    (new Vector3(-0.809017f, 0.5f, -0.309017f), new Vector3(-1f, 0f, 0f)),
                    (new Vector3(-0.809017f, 0.5f, 0.309017f),  new Vector3(-1f, 0f, 0f)),
                    (new Vector3(-0.8506508f, 0f, 0.5257311f),  new Vector3(-1f, 0f, 0f)),
                    (new Vector3(0.309017f, 0.809017f, 0.5f),   new Vector3(0.809017f, 0.5f, 0.309017f)),
                    (new Vector3(0.5257311f, 0.8506508f, 0f),   new Vector3(0.809017f, 0.5f, 0.309017f)),
                    (new Vector3(0f, 0.5257311f, 0.8506508f),   new Vector3(0.5f, 0.309017f, 0.809017f)),
                    (new Vector3(0.309017f, 0.809017f, 0.5f),   new Vector3(0.5f, 0.309017f, 0.809017f)),
                    (new Vector3(0.8506508f, 0f, 0.5257311f),   new Vector3(0.809017f, 0.5f, 0.309017f)),
                    (new Vector3(0.5f, 0.309017f, 0.809017f),   new Vector3(0.809017f, 0.5f, 0.309017f)),
                    (new Vector3(0.8506508f, 0f, 0.5257311f),   new Vector3(0.5f, 0.309017f, 0.809017f)),
                    (new Vector3(-0.5f, 0.309017f, 0.809017f),  new Vector3(0f, 0f, 1f)),
                    (new Vector3(0f, 0.5257311f, 0.8506508f),   new Vector3(0f, 0f, 1f)),
                    (new Vector3(-0.8506508f, 0f, 0.5257311f),  new Vector3(-0.5f, -0.309017f, 0.809017f)),
                    (new Vector3(-0.5f, 0.309017f, 0.809017f),  new Vector3(-0.5f, -0.309017f, 0.809017f)),
                    (new Vector3(0f, -0.5257311f, 0.8506508f),  new Vector3(0f, 0f, 1f)),
                    (new Vector3(-0.5f, -0.309017f, 0.809017f), new Vector3(0f, 0f, 1f)),
                    (new Vector3(0f, -0.5257311f, 0.8506508f),  new Vector3(-0.5f, -0.309017f, 0.809017f)),
                    (new Vector3(-1f, 0f, 0f),                  new Vector3(-0.809017f, -0.5f, 0.309017f)),
                    (new Vector3(-0.8506508f, 0f, 0.5257311f),  new Vector3(-0.809017f, -0.5f, 0.309017f)),
                    (new Vector3(-0.8506508f, 0f, -0.5257311f), new Vector3(-0.809017f, -0.5f, -0.309017f)),
                    (new Vector3(-1f, 0f, 0f),                  new Vector3(-0.809017f, -0.5f, -0.309017f)),
                    (new Vector3(-0.5257311f, -0.8506508f, 0f), new Vector3(-0.809017f, -0.5f, 0.309017f)),
                    (new Vector3(-0.809017f, -0.5f, -0.309017f),new Vector3(-0.809017f, -0.5f, 0.309017f)),
                    (new Vector3(-0.5257311f, -0.8506508f, 0f), new Vector3(-0.809017f, -0.5f, -0.309017f)),
                    (new Vector3(-0.5f, 0.309017f, -0.809017f), new Vector3(-0.5f, -0.309017f, -0.809017f)),
                    (new Vector3(-0.8506508f, 0f, -0.5257311f), new Vector3(-0.5f, -0.309017f, -0.809017f)),
                    (new Vector3(0f, 0.5257311f, -0.8506508f),  new Vector3(0f, 0f, -1f)),
                    (new Vector3(-0.5f, 0.309017f, -0.809017f), new Vector3(0f, 0f, -1f)),
                    (new Vector3(0f, -0.5257311f, -0.8506508f), new Vector3(-0.5f, -0.309017f, -0.809017f)),
                    (new Vector3(0f, 0f, -1f),                  new Vector3(-0.5f, -0.309017f, -0.809017f)),
                    (new Vector3(0f, -0.5257311f, -0.8506508f), new Vector3(0f, 0f, -1f)),
                    (new Vector3(0.309017f, 0.809017f, -0.5f),  new Vector3(0.5f, 0.309017f, -0.809017f)),
                    (new Vector3(0f, 0.5257311f, -0.8506508f),  new Vector3(0.5f, 0.309017f, -0.809017f)),
                    (new Vector3(0.5257311f, 0.8506508f, 0f),   new Vector3(0.809017f, 0.5f, -0.309017f)),
                    (new Vector3(0.309017f, 0.809017f, -0.5f),  new Vector3(0.809017f, 0.5f, -0.309017f)),
                    (new Vector3(0.8506508f, 0f, -0.5257311f),  new Vector3(0.5f, 0.309017f, -0.809017f)),
                    (new Vector3(0.809017f, 0.5f, -0.309017f),  new Vector3(0.5f, 0.309017f, -0.809017f)),
                    (new Vector3(0.8506508f, 0f, -0.5257311f),  new Vector3(0.809017f, 0.5f, -0.309017f)),
                    (new Vector3(0.5257311f, -0.8506508f, 0f),  new Vector3(0.809017f, -0.5f, 0.309017f)),
                    (new Vector3(0.809017f, -0.5f, 0.309017f),  new Vector3(0.309017f, -0.809017f, 0.5f)),
                    (new Vector3(0.5257311f, -0.8506508f, 0f),  new Vector3(0.309017f, -0.809017f, 0.5f)),
                    (new Vector3(0.8506508f, 0f, 0.5257311f),   new Vector3(0.5f, -0.309017f, 0.809017f)),
                    (new Vector3(0.809017f, -0.5f, 0.309017f),  new Vector3(0.5f, -0.309017f, 0.809017f)),
                    (new Vector3(0.8506508f, 0f, 0.5257311f),   new Vector3(0.809017f, -0.5f, 0.309017f)),
                    (new Vector3(0f, -0.5257311f, 0.8506508f),  new Vector3(0.309017f, -0.809017f, 0.5f)),
                    (new Vector3(0.5f, -0.309017f, 0.809017f),  new Vector3(0.309017f, -0.809017f, 0.5f)),
                    (new Vector3(0f, -0.5257311f, 0.8506508f),  new Vector3(0.5f, -0.309017f, 0.809017f)),
                    (new Vector3(0.309017f, -0.809017f, 0.5f),  new Vector3(0f, -1f, 0f)),
                    (new Vector3(0.5257311f, -0.8506508f, 0f),  new Vector3(0f, -1f, 0f)),
                    (new Vector3(0f, -0.5257311f, 0.8506508f),  new Vector3(-0.309017f, -0.809017f, 0.5f)),
                    (new Vector3(0.309017f, -0.809017f, 0.5f),  new Vector3(-0.309017f, -0.809017f, 0.5f)),
                    (new Vector3(-0.5257311f, -0.8506508f, 0f), new Vector3(0f, -1f, 0f)),
                    (new Vector3(-0.309017f, -0.809017f, 0.5f), new Vector3(0f, -1f, 0f)),
                    (new Vector3(-0.5257311f, -0.8506508f, 0f), new Vector3(-0.309017f, -0.809017f, 0.5f)),
                    (new Vector3(0f, -1f, 0f),                  new Vector3(0.309017f, -0.809017f, -0.5f)),
                    (new Vector3(0.5257311f, -0.8506508f, 0f),  new Vector3(0.309017f, -0.809017f, -0.5f)),
                    (new Vector3(-0.5257311f, -0.8506508f, 0f), new Vector3(-0.309017f, -0.809017f, -0.5f)),
                    (new Vector3(0f, -1f, 0f),                  new Vector3(-0.309017f, -0.809017f, -0.5f)),
                    (new Vector3(0f, -0.5257311f, -0.8506508f), new Vector3(0.309017f, -0.809017f, -0.5f)),
                    (new Vector3(-0.309017f, -0.809017f, -0.5f),new Vector3(0.309017f, -0.809017f, -0.5f)),
                    (new Vector3(0f, -0.5257311f, -0.8506508f), new Vector3(-0.309017f, -0.809017f, -0.5f)),
                    (new Vector3(0.309017f, -0.809017f, -0.5f), new Vector3(0.809017f, -0.5f, -0.309017f)),
                    (new Vector3(0.5257311f, -0.8506508f, 0f),  new Vector3(0.809017f, -0.5f, -0.309017f)),
                    (new Vector3(0f, -0.5257311f, -0.8506508f), new Vector3(0.5f, -0.309017f, -0.809017f)),
                    (new Vector3(0.309017f, -0.809017f, -0.5f), new Vector3(0.5f, -0.309017f, -0.809017f)),
                    (new Vector3(0.8506508f, 0f, -0.5257311f),  new Vector3(0.809017f, -0.5f, -0.309017f)),
                    (new Vector3(0.5f, -0.309017f, -0.809017f), new Vector3(0.809017f, -0.5f, -0.309017f)),
                    (new Vector3(0.8506508f, 0f, -0.5257311f),  new Vector3(0.5f, -0.309017f, -0.809017f)),
                    (new Vector3(0.809017f, -0.5f, 0.309017f),  new Vector3(0.809017f, -0.5f, -0.309017f)),
                    (new Vector3(0.8506508f, 0f, -0.5257311f),  new Vector3(1f, 0f, 0f)),
                    (new Vector3(0.809017f, -0.5f, -0.309017f), new Vector3(1f, 0f, 0f)),
                    (new Vector3(0.809017f, -0.5f, 0.309017f),  new Vector3(1f, 0f, 0f)),
                    (new Vector3(0.8506508f, 0f, 0.5257311f),   new Vector3(1f, 0f, 0f)),
                    (new Vector3(0f, 0f, 1f),                   new Vector3(0.5f, -0.309017f, 0.809017f)),
                    (new Vector3(0.5f, 0.309017f, 0.809017f),   new Vector3(0.5f, -0.309017f, 0.809017f)),
                    (new Vector3(0.5f, 0.309017f, 0.809017f),   new Vector3(0f, 0f, 1f)),
                    (new Vector3(-0.809017f, -0.5f, 0.309017f), new Vector3(-0.309017f, -0.809017f, 0.5f)),
                    (new Vector3(-0.5f, -0.309017f, 0.809017f), new Vector3(-0.309017f, -0.809017f, 0.5f)),
                    (new Vector3(-0.5f, -0.309017f, 0.809017f), new Vector3(-0.809017f, -0.5f, 0.309017f)),
                    (new Vector3(-0.5f, -0.309017f, -0.809017f),new Vector3(-0.309017f, -0.809017f, -0.5f)),
                    (new Vector3(-0.809017f, -0.5f, -0.309017f),new Vector3(-0.309017f, -0.809017f, -0.5f)),
                    (new Vector3(-0.809017f, -0.5f, -0.309017f),new Vector3(-0.5f, -0.309017f, -0.809017f)),
                    (new Vector3(0.5f, 0.309017f, -0.809017f),  new Vector3(0.5f, -0.309017f, -0.809017f)),
                    (new Vector3(0f, 0f, -1f),                  new Vector3(0.5f, -0.309017f, -0.809017f)),
                    (new Vector3(0f, 0f, -1f),                  new Vector3(0.5f, 0.309017f, -0.809017f)),
                    (new Vector3(0.809017f, 0.5f, 0.309017f),   new Vector3(1f, 0f, 0f)),
                    (new Vector3(0.809017f, 0.5f, -0.309017f),  new Vector3(1f, 0f, 0f)),
                    (new Vector3(0.809017f, 0.5f, 0.309017f),   new Vector3(0.809017f, 0.5f, -0.309017f)),
                };
        }

        #endregion
        #region Private Methods - utils

        //TODO: Move these into dedicated classes

        private static Vector3 GetArbitraryOrthonganal(Vector3 vector)
        {
            if (IsInvalid(vector) || Mathf.Approximately(vector.sqrMagnitude, 0f))
                return new Vector3(float.NaN, float.NaN, float.NaN);

            Vector3 rand = UnityEngine.Random.onUnitSphere;

            for (int i = 0; i < 10; i++)
            {
                Vector3 retVal = Vector3.Cross(vector, rand);

                if (IsInvalid(retVal))
                    rand = UnityEngine.Random.onUnitSphere;
                else
                    return retVal;
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
                return new Vector3(0, 0, 0);

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
                return new Vector3(0, 0, 0);

            float oneOverLen = 1f / (float)length;

            return new Vector3(x * oneOverLen, y * oneOverLen, z * oneOverLen);
        }

        #endregion
    }

    #region class: DebugItem

    public class DebugItem
    {
        public DebugItem(long token, GameObject obj, GameObject[] childObjects, Vector3 position, Component relativeToComponent, GameObject relativeToGameObject, bool isLit)
        {
            Token = token;
            Object = obj;
            ChildObjects = childObjects;
            Position = position;
            RelativeToComponent = relativeToComponent;
            RelativeToGameObject = relativeToGameObject;
            IsLit = isLit;
        }

        public long Token { get; }
        public GameObject Object { get; }
        public GameObject[] ChildObjects { get; }

        public Vector3 Position { get; }
        public Component RelativeToComponent { get; }
        public GameObject RelativeToGameObject { get; }

        public bool IsLit { get; }
    }

    #endregion
}
