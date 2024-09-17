using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using Unity.Mathematics;
using UnityEngine;
using static ThunderRoad.ItemMagicAreaProjectile;

namespace Jetpack.Scanning
{
    /// <summary>
    /// This should be called on a regular basis.  If fires rays out and up (not down), then populates
    /// ConfinedPercent according to how open or confined the area is
    /// </summary>
    /// <remarks>
    /// This is meant to help with limiting accelerations in tight spaces
    /// 
    /// The property doesn't immediately change, it requires several updates to pull the value in line
    /// with what it sees (flying past a tree shouldn't have much effect)
    /// </remarks>
    public class ConfinedArea
    {
        private const bool SHOULD_DRAW = true;

        /// <summary>
        /// Calling Update on a regular basis will set this property
        /// 
        /// 0 = open space
        /// 1 = tight space
        /// </summary>
        public float ConfinedPercent { get; private set; } = 0.5f;

        /// <summary>
        /// These are randomly rotated icosahedrons with the rays pointing down removed
        /// </summary>
        private static Lazy<Ray[][]> _icos = new Lazy<Ray[][]>(() => GetIcosahedrons());

        private DateTime _prev_tick = DateTime.UtcNow;

        // Debug Visuals
        private DebugRenderer3D _renderer = null;
        private int _line_index = -1;
        private int _hit_index = -1;
        private List<DebugItem> _rayvisual_lines = null;
        private List<DebugItem> _rayvisual_hits = null;

        public void Update(float ray_length, float player_scale, float gain_factor)
        {
            // Adjust the ray length based on the player's scale
            var raylengths = GetRayMinMax(ray_length, player_scale);

            // Fire rays and get an average percent of how confined the area is
            float how_blocked = GetHowBlocked(raylengths.min, raylengths.max);

            // Pull the new value toward the current how_blocked value
            DateTime now = DateTime.UtcNow;

            ConfinedPercent = GetNewConfinedSpace(ConfinedPercent, how_blocked, gain_factor, (float)(now - _prev_tick).TotalSeconds);

            _prev_tick = now;


            // TODO: visual of ConfinedPercent
            // Ideally, a text label with color border
            // Otherwise, some dots that emulate a progress bar

        }

        public void Clear()
        {
            ConfinedPercent = 0.5f;
            _prev_tick = DateTime.UtcNow;
        }

        #region Private Methods - fire rays

        private float GetHowBlocked(float min_len, float max_len)
        {
            Ray[] rays = _icos.Value[0];        // TODO: pick a random one once this code is more proven

            float sum_score = 0;

            Vector3 pos = Player.local.transform.position;      // TODO: put this on the player

            if (SHOULD_DRAW)
                StartDrawingRays();

            for (int i = 0; i < rays.Length; i++)
                sum_score += FireRay(pos, rays[i], min_len, max_len);

            if (SHOULD_DRAW)
                FinishedDrawingRays();

            return sum_score / rays.Length;
        }

        private float FireRay(Vector3 pos, Ray ray, float min_len, float max_len)
        {
            // paste this into desmos
            // e^{-\left(mx\right)^{2}}\cdot\left(1-x^{o}\right)

            const float GAUSS_PINCH = 1.6f;
            const float CLAMP_POW = 2;

            if (Physics.Raycast(pos + ray.origin, ray.direction, out RaycastHit hit, max_len))
            {
                float dist_sqr = (hit.point - pos).sqrMagnitude;      // don't want to use hit.distance, since the ray is from pos + ray.origin

                if (dist_sqr <= min_len * min_len)
                    return 1;

                float dist = math.sqrt(dist_sqr);

                float mx = GAUSS_PINCH * dist;
                float gauss = math.exp(-(mx * mx));

                float clamp = 1 - math.pow(dist, CLAMP_POW);

                if (SHOULD_DRAW)
                    DrawRay(pos + ray.origin, ray.direction, max_len, hit, gauss * clamp);

                Debug.Log($"FireRay dist: {dist.ToStringSignificantDigits(2)}, retVal: {(gauss * clamp).ToStringSignificantDigits(3)}");

                return gauss * clamp;
            }
            else
            {
                if (SHOULD_DRAW)
                    DrawRay(pos + ray.origin, ray.direction, max_len, null, 0);

                return 0;
            }
        }

        private static (float min, float max) GetRayMinMax(float ray_length, float player_scale)
        {
            float min = 1.5f * player_scale;        // 1.5 meters should be a good min dist (distance where this returns most confined)
            float max = ray_length * player_scale;

            if (min >= max)     // should never happen
                min = 0;

            return (min, max);
        }

        #endregion
        #region Private Methods - adjust final

        private static float GetNewConfinedSpace(float prev_confined_percent, float how_blocked, float gain_factor, float delta_time)
        {
            if (delta_time > 0.5)       // avoid lag spikes wrecking the function
                delta_time = 0.5f;

            return (1 - gain_factor * delta_time) * prev_confined_percent + gain_factor * delta_time * how_blocked;
        }

        #endregion
        #region Private Methods - drawing

        private void EnsureDrawingSetup()
        {
            if (_renderer == null)
                _renderer = Player.local.gameObject.AddComponent<DebugRenderer3D>();

            if (_rayvisual_lines == null)
                _rayvisual_lines = new List<DebugItem>();

            if (_rayvisual_hits == null)
                _rayvisual_hits = new List<DebugItem>();
        }

        private void StartDrawingRays()
        {
            EnsureDrawingSetup();

            _line_index = -1;
            _hit_index = -1;

            if (_rayvisual_lines == null)
                _rayvisual_lines = new List<DebugItem>();

            if (_rayvisual_hits == null)
                _rayvisual_hits = new List<DebugItem>();
        }
        private void FinishedDrawingRays()
        {
            for (int i = 0; i < _rayvisual_lines.Count; i++)
                _rayvisual_lines[i].Object.SetActive(i <= _line_index);     // this should be cheaper than removing/adding

            for (int i = 0; i < _rayvisual_hits.Count; i++)
                _rayvisual_hits[i].Object.SetActive(i <= _hit_index);
        }

        private void DrawRay(Vector3 pos, Vector3 direction, float len, RaycastHit? hit, float percent)
        {
            Color color = hit == null ?
                UtilityColor.FromHex("A32B29") :
                UtilityColor.LERP_RGB(UtilityColor.FromHex("33E733"), UtilityColor.FromHex("385E38"), percent);

            Vector3 pos_to = pos + direction * len;

            // Line
            _line_index++;
            if (_line_index < _rayvisual_lines.Count)
            {
                DebugRenderer3D.AdjustLinePositions(_rayvisual_lines[_line_index], pos, pos_to);
                DebugRenderer3D.AdjustColor(_rayvisual_lines[_line_index], color);
            }
            else
            {
                _rayvisual_lines.Add(_renderer.AddLine_Basic(pos, pos_to, 0.02f, color));
            }

            // Hit Dot
            if (hit != null)
            {
                _hit_index++;

                if (_hit_index < _rayvisual_hits.Count)
                {
                    _rayvisual_hits[_hit_index].Object.transform.position = hit.Value.point;
                    //DebugRenderer3D.AdjustColor(_rayvisual_hits[_hit_index], UtilityColor.FromHex("34F035"));     // color never changes
                }
                else
                {
                    _rayvisual_hits.Add(_renderer.AddDot(hit.Value.point, 0.1f, UtilityColor.FromHex("34F035")));
                }
            }
        }

        #endregion
        #region Private Methods - icosahedron

        private static Ray[][] GetIcosahedrons()
        {
            // Create 24 random rotated copies of the base ico
            // For each of those, throw out rays that point down (-y)

            var ico = GetIcosahedron();

            return Enumerable.Range(0, 24).
                Select(o => RandomRotateRays(ico)).
                Select(o => RemoveDownward(o)).
                Where(o => o.Length > 0).
                ToArray();
        }

        private static Ray[] RandomRotateRays(Ray[] rays)
        {
            Quaternion quat = StaticRandom.RotationUniform();

            Ray[] retVal = new Ray[rays.Length];

            for (int i = 0; i < rays.Length; i++)
                retVal[i] = new Ray(quat * rays[i].origin, quat * rays[i].direction);

            return retVal;
        }

        private static Ray[] RemoveDownward(Ray[] rays)
        {
            var retVal = new List<Ray>();

            foreach (Ray ray in rays)
                if (ray.origin.y >= 0 && ray.direction.y >= 0)      // y is vertical axis (up/down)
                    retVal.Add(ray);

            return retVal.ToArray();
        }

        private static Ray[] GetIcosahedron()
        {
            return new[]
            {
                new Ray(new Vector3(-0.11469849f, 0.11469849f, 0.11469849f),        new Vector3( -0.57735026f, 0.57735026f, 0.57735026f)),
                new Ray(new Vector3(0, 0.18558607f, 0.070887566f),                  new Vector3(0, 0.93417233f, 0.3568221f)),
                new Ray(new Vector3(0, 0.18558607f, -0.070887566f),                 new Vector3(0, 0.93417233f, -0.3568221f)),
                new Ray(new Vector3(-0.11469849f, 0.11469849f, -0.11469849f),       new Vector3(-0.57735026f, 0.57735026f, -0.57735026f)),
                new Ray(new Vector3(-0.18558607f, 0.070887566f, 0),                 new Vector3(-0.93417233f, 0.3568221f, 0)),
                new Ray(new Vector3(0.11469849f, 0.11469849f, 0.11469849f),         new Vector3(0.57735026f, 0.57735026f, 0.57735026f)),
                new Ray(new Vector3(-0.070887566f, 0, 0.18558607f),                 new Vector3(-0.3568221f, 0, 0.93417233f)),
                new Ray(new Vector3(-0.18558607f, -0.070887566f, 0),                new Vector3(-0.93417233f, -0.3568221f, 0)),
                new Ray(new Vector3(-0.070887566f, 0, -0.18558607f),                new Vector3(-0.3568221f, -0, -0.93417233f)),
                new Ray(new Vector3(0.11469849f, 0.11469849f, -0.11469849f),        new Vector3(0.57735026f, 0.57735026f, -0.57735026f)),
                new Ray(new Vector3(0.11469849f, -0.11469849f, 0.11469849f),        new Vector3(0.57735026f, -0.57735026f, 0.57735026f)),
                new Ray(new Vector3(0, -0.18558607f, 0.070887566f),                 new Vector3(0, -0.93417233f, 0.3568221f)),
                new Ray(new Vector3(0, -0.18558607f, -0.070887566f),                new Vector3(0, -0.93417233f, -0.3568221f)),
                new Ray(new Vector3(0.11469849f, -0.11469849f, -0.11469849f),       new Vector3(0.57735026f, -0.57735026f, -0.57735026f)),
                new Ray(new Vector3(0.18558607f, -0.070887566f, 0),                 new Vector3(0.93417233f, -0.3568221f, 0)),
                new Ray(new Vector3(0.070887566f, 0, 0.18558607f),                  new Vector3(0.3568221f, 0, 0.93417233f)),
                new Ray(new Vector3(-0.11469849f, -0.11469849f, 0.11469849f),       new Vector3(-0.57735026f, -0.57735026f, 0.57735026f)),
                new Ray(new Vector3(-0.11469849f, -0.11469849f, -0.11469849f),      new Vector3(-0.57735026f, -0.57735026f, -0.57735026f)),
                new Ray(new Vector3(0.070887566f, 0, -0.18558607f),                 new Vector3(0.3568221f, 0, -0.93417233f)),
                new Ray(new Vector3(0.18558607f, 0.070887566f, 0),                  new Vector3(0.93417233f, 0.3568221f, -0)),
            };
        }

        #endregion
    }
}