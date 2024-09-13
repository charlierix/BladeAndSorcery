using PerfectlyNormalBaS.DebugLogger_Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalBaS
{
    /// <summary>
    /// This class is used to log visuals and text, save to a file, then view from a c# app
    /// </summary>
    /// <remarks>
    /// All colors are hex format.  ARGB, alpha is optional
    ///     "444" becomes "FF444444" (very dark gray)
    ///     "806A" becomes "880066AA" (dusty blue)
    ///     "FF8040" becomes "FFFF8040" (sort of a coral)
    ///     "80FFFFFF" (50% transparent white)
    ///     
    /// Copied from:
    /// https://github.com/charlierix/CyberpunkMods/blob/main/grappling_hook/cet/core/debug_render_logger.lua
    /// https://github.com/charlierix/CyberpunkMods/blob/main/wall_hang/core/debug_render_logger.lua
    /// https://github.com/charlierix/CyberpunkMods/blob/main/debug_render_log/cet/code/debug_render_logger.lua
    ///
    /// Viewed with:
    /// https://github.com/charlierix/PartyPeople/tree/master/Math_WPF/WPF/DebugLogViewer
    /// https://github.com/charlierix/CyberpunkMods/tree/main/debug_render_log/DebugRenderViewer
    /// </remarks>
    public class DebugLogger
    {
        #region Declaration Section

        private readonly bool _enable_logging;
        public bool Logging_Enabled => _enable_logging;

        private readonly string _folder;

        private readonly List<Category> _categories = new List<Category>();
        private readonly List<LogFrame> _frames = new List<LogFrame>();
        private readonly List<Text> _global_text = new List<Text>();

        #endregion

        #region Constructor

        public DebugLogger(string folder, bool enable_logging)
        {
            _folder = folder;
            _enable_logging = enable_logging;

            NewFrame();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This can make it easier to group similar items into the same category.  All items will be shown with
        /// the color and size specified here
        /// </summary>
        public void DefineCategory(string name, Color? color = null, float? size_mult = null)
        {
            _categories.Add(new Category()
            {
                name = name,

                color = color == null ?
                    null :
                    UtilityUnity.ColorToHex(color.Value, true, false),

                size_mult = size_mult,
            });
        }

        /// <summary>
        /// Allows for multiple sets of points to be added.  The viwer will show one frame at a time
        /// </summary>
        /// <remarks>
        /// This is optional.When used, all add actions get tied to the current frame.  This allows views to
        /// be logged over time, then the viewer will have a scrollbar to flip between frames
        /// 
        /// There is no need to call this after instantiation, a first frame is implied (but there's no harm in
        /// explicitly calling it for the first frame)
        /// </remarks>
        public void NewFrame(string name = null, Color? back_color = null)
        {
            if (!_enable_logging)
                return;

            _frames.Add(new LogFrame()
            {
                name = name,

                back_color = back_color == null ?
                    null :
                    UtilityUnity.ColorToHex(back_color.Value, true, false),

                items = new ItemBase[0],
                text = new Text[0],
            });
        }

        // Category and after are all optional.If a category is passed in, then the item will use that
        // category's size and color, unless overridden in the add method call
        public void Add_Dot(Vector3 position, string category = null, Color? color = null, float? size_mult = null, string tooltip = null)
        {
            if (!_enable_logging)
                return;

            _frames[_frames.Count - 1].items = UtilityCore.ArrayAdd(_frames[_frames.Count - 1].items, new ItemDot()
            {
                category = category,

                color = color == null ?
                    null :
                    UtilityUnity.ColorToHex(color.Value, true, false),

                size_mult = size_mult,
                tooltip = tooltip,

                position = Vector_to_String(position),
            });
        }
        public void Add_Line(Vector3 point1, Vector3 point2, string category = null, Color? color = null, float? size_mult = null, string tooltip = null)
        {
            if (!_enable_logging)
                return;

            _frames[_frames.Count - 1].items = UtilityCore.ArrayAdd(_frames[_frames.Count - 1].items, new ItemLine()
            {
                category = category,

                color = color == null ?
                    null :
                    UtilityUnity.ColorToHex(color.Value, true, false),

                size_mult = size_mult,
                tooltip = tooltip,

                point1 = Vector_to_String(point1),
                point2 = Vector_to_String(point2),

            });
        }
        public void Add_Circle(Vector3 center, Vector3 normal, float radius, string category = null, Color? color = null, float? size_mult = null, string tooltip = null)
        {
            if (!_enable_logging)
                return;

            _frames[_frames.Count - 1].items = UtilityCore.ArrayAdd(_frames[_frames.Count - 1].items, new ItemCircle_Edge()
            {
                category = category,

                color = color == null ?
                    null :
                    UtilityUnity.ColorToHex(color.Value, true, false),

                size_mult = size_mult,
                tooltip = tooltip,

                center = Vector_to_String(center),
                normal = Vector_to_String(normal),
                radius = radius,
            });
        }
        public void Add_Square(Vector3 center, Vector3 normal, float size_x, float size_y, string category = null, Color? color = null, float? size_mult = null, string tooltip = null)
        {
            if (!_enable_logging)
                return;

            _frames[_frames.Count - 1].items = UtilityCore.ArrayAdd(_frames[_frames.Count - 1].items, new ItemSquare_Filled()
            {
                category = category,

                color = color == null ?
                    null :
                    UtilityUnity.ColorToHex(color.Value, true, false),

                size_mult = size_mult,
                tooltip = tooltip,

                center = Vector_to_String(center),
                normal = Vector_to_String(normal),
                size_x = size_x,
                size_y = size_y,
            });
        }
        public void Add_AxisLines(Vector3 position, Quaternion rotation, float size, string category = null, float? size_mult = null, string tooltip = null)
        {
            if (!_enable_logging)
                return;

            _frames[_frames.Count - 1].items = UtilityCore.ArrayAdd(_frames[_frames.Count - 1].items, new ItemAxisLines()
            {
                category = category,
                color = null,
                size_mult = size_mult,
                tooltip = tooltip,

                position = Vector_to_String(position),
                axis_x = Vector_to_String(rotation * new Vector3(1, 0, 0)),
                axis_y = Vector_to_String(rotation * new Vector3(0, 1, 0)),
                axis_z = Vector_to_String(rotation * new Vector3(0, 0, 1)),
                size = size,
            });
        }

        /// <summary>
        /// This is any extra text attached to the frame, useful for including log dumps next to the picture
        /// </summary>
        public void WriteLine_Frame(string text, Color? color = null, float? fontsize_mult = null)
        {
            if (!_enable_logging)
                return;

            _frames[_frames.Count - 1].text = UtilityCore.ArrayAdd(_frames[_frames.Count - 1].text, new Text()
            {
                text = text,

                color = color == null ?
                    null :
                    UtilityUnity.ColorToHex(color.Value, true, false),

                fontsize_mult = fontsize_mult,
            });
        }
        /// <summary>
        /// This is text that is shown regardless of frames
        /// </summary>
        public void WriteLine_Global(string text, Color? color = null, float? fontsize_mult = null)
        {
            if (!_enable_logging)
                return;

            _global_text.Add(new Text()
            {
                text = text,

                color = color == null ?
                    null :
                    UtilityUnity.ColorToHex(color.Value, true, false),

                fontsize_mult = fontsize_mult,
            });
        }

        /// <summary>
        /// This returns true if there is something to save (this ignores categories, since that is considered
        /// prep, and not real logged items)
        /// </summary>
        public bool IsPopulated()
        {
            if (!_enable_logging)
                return false;

            if (_global_text.Count > 0)
                return true;

            return _frames.Any(o => o.items.Length > 0 || o.text.Length > 0);
        }

        /// <summary>
        /// Saves everything into a file, clears all frames, so more can be added.  Keeps categories
        /// </summary>
        /// <param name="name"></param>
        public void Save(string name = null)
        {
            if (!_enable_logging)
                return;

            PossiblyRemoveFirstFrame();

            var scene = new LogScene()
            {
                categories = _categories.ToArray(),
                frames = _frames.ToArray(),
                text = _global_text.ToArray(),
            };

            string filename = GetFilename(name);

            if (!Directory.Exists(_folder))
                Directory.CreateDirectory(_folder);

            //NOTE: Unity's json serializer isn't as good as the standard one.  The types need [Serializable], and only fields (not properties)
            //https://stackoverflow.com/questions/60651878/unity-c-sharp-jsonutility-functions-arent-working

            //File.WriteAllText(filename, JsonUtility.ToJson(scene, true));
            File.WriteAllText(filename, ToJSON(scene));

            _frames.Clear();
            _global_text.Clear();

            NewFrame();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// The constructor calls NewFrame with no params.  But the user might immediately call NewFrame with params.  That
        /// would leave an empty first frame.This function detects and removes that first frame
        /// </summary>
        /// <param name=""></param>
        private void PossiblyRemoveFirstFrame()
        {
            if (_frames.Count < 2)
                return;

            if (string.IsNullOrWhiteSpace(_frames[0].name) && _frames[0].back_color == null && _frames[0].items.Length == 0 && _frames[0].text.Length == 0)
                _frames.RemoveAt(0);
        }

        private string GetFilename(string name)
        {
            string retVal = Path.Combine(_folder, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));

            if (!string.IsNullOrWhiteSpace(name))
                retVal += " - " + name;

            retVal += ".json";

            return retVal;
        }

        private static string Vector_to_String(Vector3 vector)
        {
            return $"{vector.x}, {vector.y}, {vector.z}";       // standard tostring wraps in parenthesis
        }
        private static string Quat_to_String(Quaternion quat)
        {
            // This was for debugging
            //quat.ToAngleAxis(out float angle, out Vector3 axis);
            //return $"{quat.x}, {quat.y}, {quat.z}, {quat.w}|{axis.x}, {axis.y}, {axis.z}|{angle}";

            return $"{quat.x}, {quat.y}, {quat.z}, {quat.w}";
        }

        #endregion
        #region Private Methods - serialize json

        //NOTE: This is really ugly code, but unity's json serializer is limited, doesn't understand null.  It might have been better to write something
        //more generic using reflection, but whatever, it's written

        private const string INDENT = "    ";

        private static string ToJSON(LogScene scene)
        {
            string[] lines = new[]
            {
                ToJSON(nameof(scene.categories), scene.categories, ""),
                ToJSON(nameof(scene.frames), scene.frames, ""),
                ToJSON(nameof(scene.text), scene.text, ""),
            }.
            Where(o => !string.IsNullOrWhiteSpace(o)).
            ToArray();

            var retVal = new StringBuilder();

            retVal.AppendLine("{");
            retVal.AppendLine(CombineJSONs(lines, INDENT));
            retVal.Append("}");

            return retVal.ToString();
        }

        private static string ToJSON(string name, Category[] categories, string indent)
        {
            if (categories == null)
                categories = new Category[0];      // arrays are required

            string[] lines = categories.
                Select(o => ToJSON(o, indent)).
                ToArray();

            return GetJsonArray(name, lines, indent);
        }
        private static string ToJSON(Category category, string indent)
        {
            var lines = new List<string>();

            AddJsonStringProp(lines, nameof(category.name), category.name);
            AddJsonStringProp(lines, nameof(category.color), category.color);
            AddJsonFloatProp(lines, nameof(category.size_mult), category.size_mult);

            return GetJsonObject(lines.ToArray(), indent);
        }

        private static string ToJSON(string name, LogFrame[] frames, string indent)
        {
            if (frames == null)
                frames = new LogFrame[0];       // arrays are required

            string[] lines = frames.
                Select(o => ToJSON(o, indent)).     // no need to add to indent here, that will be done in CombineJSONs
                ToArray();

            return GetJsonArray(name, lines, indent);
        }
        private static string ToJSON(LogFrame frame, string indent)
        {
            var lines = new List<string>();

            AddJsonStringProp(lines, nameof(frame.name), frame.name);
            AddJsonStringProp(lines, nameof(frame.back_color), frame.back_color);

            //if (frame.items != null && frame.items.Length > 0)        // arrays are required
            lines.Add(ToJSON(frame.items, ""));

            //if (frame.text != null && frame.text.Length > 0)
            lines.Add(ToJSON(nameof(frame.text), frame.text, ""));

            return GetJsonObject(lines.ToArray(), indent);
        }

        private static string ToJSON(ItemBase[] items, string indent)
        {
            if (items == null)
                items = new ItemBase[0];        // arrays are required

            string[] lines = items.
                Select(o => ToJSON(o, indent)).
                ToArray();

            return GetJsonArray("items", lines, indent);
        }
        private static string ToJSON(ItemBase item, string indent)
        {
            var lines = new List<string>();

            AddJsonStringProp(lines, nameof(item.category), item.category);
            AddJsonStringProp(lines, nameof(item.color), item.color);
            AddJsonFloatProp(lines, nameof(item.size_mult), item.size_mult);
            AddJsonStringProp(lines, nameof(item.tooltip), item.tooltip);

            if (item is ItemDot dot)
            {
                AddJsonStringProp(lines, nameof(dot.position), dot.position);
            }
            else if (item is ItemLine line)
            {
                AddJsonStringProp(lines, nameof(line.point1), line.point1);
                AddJsonStringProp(lines, nameof(line.point2), line.point2);
            }
            else if (item is ItemCircle_Edge circle)
            {
                AddJsonStringProp(lines, nameof(circle.center), circle.center);
                AddJsonStringProp(lines, nameof(circle.normal), circle.normal);
                AddJsonFloatProp(lines, nameof(circle.radius), circle.radius);
            }
            else if (item is ItemSquare_Filled square)
            {
                AddJsonStringProp(lines, nameof(square.center), square.center);
                AddJsonStringProp(lines, nameof(square.normal), square.normal);
                AddJsonFloatProp(lines, nameof(square.size_x), square.size_x);
                AddJsonFloatProp(lines, nameof(square.size_y), square.size_y);
            }
            else if (item is ItemAxisLines axislines)
            {
                AddJsonStringProp(lines, nameof(axislines.position), axislines.position);
                AddJsonStringProp(lines, nameof(axislines.axis_x), axislines.axis_x);
                AddJsonStringProp(lines, nameof(axislines.axis_y), axislines.axis_y);
                AddJsonStringProp(lines, nameof(axislines.axis_z), axislines.axis_z);
                AddJsonFloatProp(lines, nameof(axislines.size), axislines.size);
            }
            else
            {
                return JsonUtility.ToJson(item, true);      // should never happen.  this would be ugly json, but would still work (default includes all types regardless if they are null or not)
            }

            return GetJsonObject(lines.ToArray(), indent);
        }

        private static string ToJSON(string name, Text[] texts, string indent)
        {
            if (texts == null)
                texts = new Text[0];        // arrays are required

            string[] lines = texts.
                Select(o => ToJSON(o, indent)).
                ToArray();

            return GetJsonArray(name, lines, indent);
        }
        private static string ToJSON(Text text, string indent)
        {
            var lines = new List<string>();

            AddJsonStringProp(lines, nameof(text.text), text.text);
            AddJsonStringProp(lines, nameof(text.color), text.color);
            AddJsonFloatProp(lines, nameof(text.fontsize_mult), text.fontsize_mult);

            return GetJsonObject(lines.ToArray(), indent);
        }

        private static string GetJsonArray(string name, string[] lines, string indent)
        {
            var retVal = new StringBuilder();

            retVal.Append(indent);
            retVal.Append($"\"{name}\": [");

            if (lines != null && lines.Length > 0)
            {
                retVal.AppendLine();        // waiting until now so that empty arrays can be []
                retVal.AppendLine(CombineJSONs(lines, indent + INDENT));
                retVal.Append(indent);
            }

            retVal.Append("]");

            return retVal.ToString();
        }
        private static string GetJsonObject(string[] lines, string indent)
        {
            var retVal = new StringBuilder();

            retVal.Append(indent);
            retVal.AppendLine("{");

            retVal.AppendLine(CombineJSONs(lines.ToArray(), indent + INDENT));

            retVal.Append(indent);
            retVal.Append("}");

            return retVal.ToString();
        }

        private static void AddJsonStringProp(List<string> list, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
                list.Add($"\"{name}\": \"{value}\"");
        }
        private static void AddJsonFloatProp(List<string> list, string name, float? value)
        {
            if (value != null)
                list.Add($"\"{name}\": {value}");
        }

        private static string CombineJSONs(string[] jsons, string indent)
        {
            var retVal = new StringBuilder();

            for (int i = 0; i < jsons.Length; i++)
            {
                //retVal.Append(jsons[i]);      // can't do this directly, because the indent needs to be injected at the front of each line

                string[] lines = jsons[i].Replace("\r\n", "\n").Split('\n');
                for (int j = 0; j < lines.Length; j++)
                {
                    retVal.Append(indent);
                    retVal.Append(lines[j]);

                    if (j < lines.Length - 1)
                        retVal.AppendLine();
                }

                if (i < jsons.Length - 1)
                    retVal.AppendLine(",");
            }

            return retVal.ToString();
        }

        #endregion
    }
}
