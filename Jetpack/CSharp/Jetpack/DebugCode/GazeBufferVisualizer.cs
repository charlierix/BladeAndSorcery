using Jetpack.InputWatchers;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.DebugCode
{
    public class GazeBufferVisualizer
    {
        #region Declaration Section

        private const bool SHOWTARGET = true;
        private const bool SHOWOFFSET = false;

        private readonly GazeBuffer _gazeBuffer = new GazeBuffer();

        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private DebugRenderer3D _renderer = null;

        private Dictionary<string, (DebugItem item, Color color)> _spheres = new Dictionary<string, (DebugItem item, Color color)>();
        private List<DebugItem> _hits = new List<DebugItem>();

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

            if (SHOWTARGET)
            {
                _gazeBuffer.AddSample_Target(pos, look, velocity.magnitude);

                if (_gazeBuffer.TryGetDominantDirection_Target(out Vector3 dominant_direction, out float confidence, pos))
                {
                    //DrawDirection();
                }
                else
                {

                }

                DrawSpheresAndHits();
                //DrawReport();
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

            // Spheres
            foreach (var item in _spheres.Values)
                _renderer.Remove(item.item);

            _spheres.Clear();

            // Hits
            foreach (DebugItem item in _hits)
                _renderer.Remove(item);

            _hits.Clear();

            _renderer = null;
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
                    string sphere_key = $"{GazeBuffer.GetRadiusKey(bucket[0].SphereRadius)} | {GazeBuffer.GetOriginKey(bucket[0].SphereOrigin)}";        // each bucket is for a sphere, so every hit in this bucket will have the same sphere origin
                    sphere_keys.Add(sphere_key);

                    if (!_spheres.ContainsKey(sphere_key))
                    {
                        Color color = UtilityColor.RandomHSV(0, 1, 0.3f, 0.8f, 0.45f, 0.85f);
                        var item = (_renderer.AddWireframeSphere(bucket[0].SphereOrigin, bucket[0].SphereRadius, LINE_THICKNESS, color), color);
                        _spheres.Add(sphere_key, item);
                    }

                    // the churn of dots is killing the game.  need to reuse existing where possible

                    // Hits
                    //_hits.Add(_renderer.AddDots(bucket.Select(o => o.Hit), DOT_SIZE / 4, _spheres[sphere_key].color));
                }
            }

            foreach (string dead_sphere in _spheres.Keys.Except(sphere_keys).ToArray())
            {
                bool was_removed = _renderer.Remove(_spheres[dead_sphere].item);
                _spheres.Remove(dead_sphere);
            }
        }

        #endregion
    }
}
