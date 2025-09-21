using Jetpack.InputWatchers;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using static Jetpack.InputWatchers.GazeBuffer;
using static ThunderRoad.ItemMagicAreaProjectile;

namespace Jetpack.DebugCode
{

    // TODO: show winning ray as well as its confidence

    public class GazeBufferVisualizer
    {
        #region Declaration Section

        private readonly GazeBuffer _gazeBuffer = new GazeBuffer();

        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private DebugRenderer3D _renderer = null;

        private DebugItem _dominant_direction = null;
        private Dictionary<string, (DebugItem item, Color color)> _spheres = new Dictionary<string, (DebugItem item, Color color)>();
        private int _hit_index = -1;
        private List<DebugItem> _hits = new List<DebugItem>();
        private DebugItem _status = null;

        #endregion

        public void Clear()
        {
            _gazeBuffer.Clear();
            ClearDebugVisuals();
        }

        public void Update()
        {
            Vector3 pos = Player.local.head.anchor.position;
            Vector3 look = Player.local.head.transform.forward;
            Vector3 velocity = Player.local.locomotion.physicBody.velocity;

            if (JetpackScript.ShowGazeBuffer_Target)
            {
                PrepForHitsUpdate();

                _gazeBuffer.AddSample_Target(pos, look, velocity.magnitude);

                float? confidence = null;
                //if (_gazeBuffer.TryGetDominantDirection_Target(out Vector3 dominant_direction, out float confidence2, pos))
                if (_gazeBuffer.TryGetDominantDirection_Target_Debug(out Vector3 dominant_direction, out float confidence2, out float sphere_radius, out Vector3 sphere_origin, pos))
                {
                    confidence = confidence2;
                    DrawDirection(dominant_direction, confidence2, pos, sphere_radius, sphere_origin);
                }
                else
                {
                    if (_dominant_direction != null)
                        _renderer.Remove(_dominant_direction);
                    _dominant_direction = null;
                }

                DrawSpheresAndHits();
                DrawStatus(confidence);

                FinishHitsUpdate();
            }

            //if(SHOWOFFSET)

        }

        #region Private Methods

        private void EnsureDebugActive()
        {
            if (_renderer == null)
                _renderer = DebugRenderer3D.GetOrAddDebugRenderer3D();
        }

        private void ClearDebugVisuals()
        {
            if (_renderer == null)
                return;

            // Dominant Direction
            if (_dominant_direction != null)
                _renderer.Remove(_dominant_direction);

            _dominant_direction = null;

            // Spheres
            foreach (var item in _spheres.Values)
                _renderer.Remove(item.item);

            _spheres.Clear();

            // Hits
            foreach (DebugItem item in _hits)
                _renderer.Remove(item);

            _hits.Clear();

            // Status
            if (_status != null)
                _renderer.Remove(_status);

            _status = null;

            _renderer = null;
        }

        private void PrepForHitsUpdate()
        {
            _hit_index = -1;
            RemoveDespawned(_hits);
        }
        private static void RemoveDespawned(List<DebugItem> items)
        {
            int index = 0;

            while (index < items.Count)
            {
                if (items[index].Object == null)
                {
                    //Debug.Log($"Removing despawned visual: {items[index].Token}");
                    items.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        private void FinishHitsUpdate()
        {
            for (int i = 0; i < _hits.Count; i++)
                _hits[i].Object.SetActive(i <= _hit_index);     // this should be cheaper than removing/adding
        }

        private void DrawDirection(Vector3 dominant_direction, float confidence, Vector3 pos, float sphere_radius, Vector3 sphere_origin)
        {
            EnsureDebugActive();

            // Get the color of the sphere
            string key = GetSphereKey(sphere_radius, sphere_origin);
            Color color = Color.magenta;
            if (_spheres.TryGetValue(key, out var sphere))
                color = sphere.color;

            // Create or update the line
            if (_dominant_direction == null)
            {
                _dominant_direction = _renderer.AddLine_Basic(pos, pos + dominant_direction, LINE_THICKNESS, color);
            }
            else
            {
                DebugRenderer3D.AdjustLinePositions(_dominant_direction, pos, pos + dominant_direction);
                DebugRenderer3D.AdjustColor(_dominant_direction, color);
            }
        }

        private void DrawSpheresAndHits()
        {
            EnsureDebugActive();

            // Need to remove all hits, since each debugitem will store N dots
            foreach (DebugItem item in _hits)
                _renderer.Remove(item);

            _hits.Clear();

            var sphere_keys = new List<string>();

            foreach (var by_radius in _gazeBuffer._target.Values)
            {
                foreach (var bucket in by_radius.Values)
                {
                    if (bucket.Count == 0)
                        continue;

                    // Sphere
                    string sphere_key = GetSphereKey(bucket[0].SphereRadius, bucket[0].SphereOrigin);     // each bucket is for a sphere, so every hit in this bucket will have the same sphere origin
                    sphere_keys.Add(sphere_key);

                    if (!_spheres.ContainsKey(sphere_key))
                    {
                        Color color = UtilityColor.RandomHSV(0, 1, 0.3f, 0.8f, 0.45f, 0.85f);
                        var item = (_renderer.AddWireframeSphere(bucket[0].SphereOrigin, bucket[0].SphereRadius, LINE_THICKNESS, color, isLowRes: false), color);
                        _spheres.Add(sphere_key, item);
                    }

                    // the churn of dots is killing the game.  need to reuse existing where possible

                    // Hits
                    //_hits.Add(_renderer.AddDots(bucket.Select(o => o.Hit), DOT_SIZE / 4, _spheres[sphere_key].color));

                    var color2 = _spheres[sphere_key].color;

                    foreach (var hit in bucket)
                    {
                        _hit_index++;

                        if (_hit_index < _hits.Count)
                        {
                            _hits[_hit_index].Object.transform.position = hit.Hit;
                            DebugRenderer3D.AdjustColor(_hits[_hit_index], color2);
                        }
                        else
                        {
                            _hits.Add(_renderer.AddDot(hit.Hit, DOT_SIZE / 4, color2));
                        }
                    }
                }
            }

            foreach (string dead_sphere in _spheres.Keys.Except(sphere_keys).ToArray())
            {
                bool was_removed = _renderer.Remove(_spheres[dead_sphere].item);
                _spheres.Remove(dead_sphere);
            }
        }
        private static string GetSphereKey(float radius, Vector3 origin)
        {
            return $"{GazeBuffer.GetRadiusKey(radius)} | {GazeBuffer.GetOriginKey(origin)}";
        }

        private void DrawStatus(float? confidence)
        {
            EnsureDebugActive();

            // select many buckets
            var buckets = new List<List<GazeSample_SphereTarget>>();

            foreach (var by_radius in _gazeBuffer._target.Values)
                buckets.AddRange(by_radius.Values);

            // text pos
            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                //Player.local.head.transform.right * -0.75f +
                Player.local.head.transform.up * -0.33f;

            // fill out report
            var text_list = new List<string>();

            text_list.Add($"num spheres: {buckets.Count}");
            text_list.Add($"total hits: {buckets.Sum(o => o.Count)}");
            text_list.Add($"avg hits per sphere: {buckets.Average(o => o.Count).ToStringSignificantDigits(1)}");
            text_list.Add($"max hits in sphere: {buckets.Max(o => o.Count)}");
            text_list.Add($"confidence: {confidence?.ToStringSignificantDigits(2) ?? "--"}");

            string text = string.Join(Environment.NewLine, text_list);

            // draw
            if (_status == null)
                _status = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, Color.cyan, Color.black, TEXT_HEIGHT * text_list.Count);

            _status.Object.transform.position = text_pos;
            _status.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_status, new_text: text);
        }

        #endregion
    }
}
