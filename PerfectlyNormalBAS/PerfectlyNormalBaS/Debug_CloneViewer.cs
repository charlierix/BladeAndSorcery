using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalBaS
{
    /// <summary>
    /// This is a way to visualize a few game objects by creating cloned debug visuals of them in front of your camera (like a
    /// crude 3rd person view)
    /// </summary>
    /// <remarks>
    /// Add this script to your camera's gameobject
    /// 
    /// Add the descriptor script (Debug_Clone_TrackedItem) to game objects that you want to track
    /// 
    /// Those tracked gameobjects will have a debug visual that's always a set distance from the camera (along the camera's look direction)
    /// (they will be kept relative to the center of position of the set of tracked objects)
    /// </remarks>
    public class Debug_CloneViewer : MonoBehaviour
    {
        #region class: TrackedItem

        private class TrackedItem
        {
            public Debug_Clone_TrackedItem Item { get; set; }
            public (DebugItem debug, bool isLineToParent)[] DebugItems { get; set; }
        }

        #endregion

        public float Offset = 2;
        public float Scale = 0.3f;

        //TODO: bool for snap to horz plane vs plane below look dir

        /// <summary>
        /// Allows sets to be visible/invisible
        /// </summary>
        /// <remarks>
        /// If this array is empty, then it show any items that don't have group name populated.  This makes it so the group name
        /// can be left blank in tracked and here (making it optional)
        /// </remarks>
        public string[] GroupNames = new string[0];

        private DebugRenderer3D _renderer = null;


        // Putting everything in here so the scene hierarchy stays cleaner
        //TODO: This should be done in DebugRenderer3D
        //private GameObject _container = null;
        //if (_container == null)
        //    _container = new GameObject("Debug_CloneViewer_Container");


        private List<TrackedItem> _items = new List<TrackedItem>();

        private void Start()
        {
            //_renderer = new DebugRenderer3D();
            _renderer = gameObject.AddComponent<DebugRenderer3D>();

            Refresh();
        }

        public void Refresh()
        {
            const float DOT_SIZE = 0.02f;
            const float AXISLINE_LENGTH = 0.12f;
            const float AXISLINE_THICKNESS = 0.005f;
            const float PARENTLINE_THICKNESS = 0.008f;

            // Clear existing
            _renderer.Remove(_items.SelectMany(o => o.DebugItems).Select(o => o.debug));
            _items.Clear();

            // Recursively scan everything in the scene, looking for instances of Debug_Clone_TrackedItem
            var tracked = FindObjectsOfType<Debug_Clone_TrackedItem>(false);
            //tracked[0].gameObject     // this is how to get at the parent game object

            if (tracked.Length == 0)
                return;

            foreach (var tracked_item in tracked)
            {
                if (!IsInGroup(GroupNames, tracked_item.GroupName))
                    continue;

                var debug_items = new List<(DebugItem, bool)>();
                debug_items.Add((_renderer.AddDot(new Vector3(), DOT_SIZE * tracked_item.Size_Scale, UtilityColor.FromHex(tracked_item.Color)), false));

                if (tracked_item.ShowAxisLines)
                    debug_items.Add((_renderer.AddAxisLines(AXISLINE_LENGTH * tracked_item.Size_Scale, AXISLINE_THICKNESS * tracked_item.Size_Scale), false));

                if (tracked_item.ShowLineToParent && tracked_item.transform.parent?.gameObject != null)
                    debug_items.Add((_renderer.AddLine_Basic(new Vector3(), new Vector3(), PARENTLINE_THICKNESS * tracked_item.Size_Scale, UtilityColor.FromHex(tracked_item.Color)), true));

                _items.Add(new TrackedItem()
                {
                    DebugItems = debug_items.ToArray(),
                    Item = tracked_item,
                });
            }
        }

        private void Update()
        {
            if (_items.Count == 0)
                return;

            // Calculate the center of position
            Vector3 center_actual = Math3D.GetCenter(_items.Select(o => o.Item.gameObject.transform.position));
            Vector3 center_debug = GetPlacementCenter();

            foreach (var item in _items)
            {
                Vector3 diff = item.Item.transform.position - center_actual;
                Vector3 debug_pos = center_debug + diff * Scale;

                //TODO: Figure out how to move the debug objects
                foreach (var debug in item.DebugItems)
                {
                    if (debug.isLineToParent)
                    {
                        Vector3 diff_parent = item.Item.transform.parent.position - center_actual;
                        Vector3 debug_parent_pos = center_debug + diff_parent * Scale;

                        DebugRenderer3D.AdjustLinePositions(debug.debug, debug_parent_pos, debug_pos);
                    }
                    else
                    {
                        debug.debug.Object.transform.position = debug_pos;
                        debug.debug.Object.transform.rotation = item.Item.transform.rotation;
                    }
                }
            }
        }

        private Vector3 GetPlacementCenter()
        {
            return transform.position + transform.forward * Offset;
        }

        private static bool IsInGroup(string[] show_names, string name)
        {
            if (string.IsNullOrWhiteSpace(name))        // special case where if the tracked item doesn't have a name, then the allowed list can be empty
            {
                if (show_names == null || show_names.Length == 0)
                    return true;

                // not exiting here, because the list could still be populated, and one of the entries could be empty string
            }

            if (show_names == null || show_names.Length == 0)
                return false;       // when items are named, then clearing the allowed list is an easy way to disable the viewer

            string name_trimmed = (name ?? "").Trim();

            return show_names.Any(o => (o ?? "").Trim().Equals(name_trimmed, StringComparison.OrdinalIgnoreCase));
        }
    }
}
