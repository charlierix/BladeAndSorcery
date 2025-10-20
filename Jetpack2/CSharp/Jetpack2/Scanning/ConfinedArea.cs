using Jetpack2.Models;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using Unity.Mathematics;
using UnityEngine;

namespace Jetpack2.Scanning
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
        #region Declaration Section

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

        private readonly RayCastStorage _raycast_storage;

        // These are set each time _raycast_storage is updated with ray cast results (values used between
        // Update_CastRays and Update_Finish
        private float _ray_min_len;
        private float _ray_max_len;
        private Vector3 _pos;

        #region debug drawing vars

        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private DebugRenderer3D _renderer = null;

        private int _line_index = -1;
        private List<DebugItem> _rayvisual_lines = null;

        private int _hit_index = -1;
        private List<DebugItem> _rayvisual_hits = null;

        private Dictionary<int, Color> _rayHitColors = null;

        private DebugItem _text = null;

        #endregion

        #endregion

        public ConfinedArea(RayCastStorage raycast_storage)
        {
            _raycast_storage = raycast_storage;
        }

        public void Update_CastRays()
        {
            if (!JetpackScript.ShouldDetectConfinedArea)
                return;

            float player_scale = 1;     // not sure if this should matter - maybe if the player is really small

            Vector3 head_pos = Player.local.head.anchor.position;
            Vector3 foot_pos = Math3D.GetAverage(Player.local.footLeft.ragdollFoot.root.position, Player.local.footRight.ragdollFoot.root.position);        // Player.local.transform.position is the room level origin
            _pos = foot_pos + ((head_pos - foot_pos) * 0.5f);

            // Adjust the ray length based on the player's scale
            var raylengths = GetRayMinMax(ConfinedAreaData.rayLength, player_scale);

            _ray_min_len = raylengths.min;
            _ray_max_len = raylengths.max;

            // Fire rays, store results
            FireRays(_pos, raylengths.min, raylengths.max);
        }
        public void Update_Finish(float elapsed_seconds)
        {
            if (!JetpackScript.ShouldDetectConfinedArea)
            {
                ConfinedPercent = 0f;
                return;
            }

            // Fire rays and get an average percent of how confined the area is
            float how_blocked = GetHowBlocked(_pos, _ray_min_len, _ray_max_len);

            // Pull the new value toward the current how_blocked value
            ConfinedPercent = GetNewConfinedSpace(ConfinedPercent, how_blocked, ConfinedAreaData.gainFactor, elapsed_seconds);

            if (JetpackScript.ShowConfinedArea)
                DrawConfinedPercent();
        }

        public void Clear()
        {
            ConfinedPercent = 0.5f;

            ClearDebugVisuals();
        }

        #region Private Methods - fire rays

        private void FireRays(Vector3 pos, float min_len, float max_len)
        {
            //Ray[] rays = _icos.Value[0];
            Ray[] rays = GetRandomRayBall();

            var results = new RayCastStorage.RayInfo[rays.Length];

            for (int i = 0; i < rays.Length; i++)
            {
                results[i] = new RayCastStorage.RayInfo()
                {
                    Origin = pos + rays[i].origin,
                    Direction = rays[i].direction,
                    MaxLen = max_len,
                };

                if (Physics.Raycast(pos + rays[i].origin, rays[i].direction, out RaycastHit hit, max_len, ScanningUtil.SolidObject_LayerMask.Value, QueryTriggerInteraction.Ignore))
                    results[i].Hit = hit;
            }

            _raycast_storage.AddRayCasts(
                RayCastStorage.RayCategory.ConfinedArea_Ico,
                new RayCastStorage.RayCastBundle
                {
                    Rays = results,
                });
        }

        private float GetHowBlocked(Vector3 pos, float min_len, float max_len)
        {
            var rays = _raycast_storage.GetRayCasts(RayCastStorage.RayCategory.ConfinedArea_Ico);

            if (rays == null || rays.Length == 0)
            {
                Debug.Log("ConfinedArea: no rays were cast");       // shouldn't happen
                return 0;
            }

            float sum_score = 0;

            if (JetpackScript.ShowConfinedArea)
                StartDrawingRays();

            for (int i = 0; i < rays[0].Rays.Length; i++)
                sum_score += ExamineRay(pos, rays[0].Rays[i], min_len, max_len);

            if (JetpackScript.ShowConfinedArea)
                FinishedDrawingRays();

            return sum_score / rays[0].Rays.Length;
        }

        private float ExamineRay(Vector3 pos, RayCastStorage.RayInfo ray, float min_len, float max_len)
        {
            // paste this into desmos
            // e^{-\left(mx\right)^{2}}\cdot\left(1-x^{o}\right)

            const float GAUSS_PINCH = 1.6f;     // no need for a slider with this one

            if (ray.Hit != null)
            {
                float dist_sqr = (ray.Hit.Value.point - pos).sqrMagnitude;      // don't want to use hit.distance, since the ray is from pos + ray.origin

                if (dist_sqr <= min_len * min_len)
                    return 1;

                float dist = math.sqrt(dist_sqr) / max_len;     // need to make it between 0 and 1

                float mx = ConfinedAreaData.falloffPower * dist;
                float gauss = math.exp(-(mx * mx));

                float clamp = 1 - math.pow(dist, GAUSS_PINCH);      // this part sets curve=0 at dist=1 (otherwise, it's a guass curve that approaches but never hits 0)

                if (JetpackScript.ShowConfinedArea)
                    //DrawRay(pos, ray.direction, max_len, ray.Hit.Value, gauss * clamp);
                    DrawRay2(pos, ray.Direction, max_len, ray.Hit.Value, gauss * clamp);

                //Debug.Log($"FireRay dist: {dist.ToStringSignificantDigits(2)}, retVal: {(gauss * clamp).ToStringSignificantDigits(3)}");

                return gauss * clamp;
            }
            else
            {
                if (JetpackScript.ShowConfinedArea)
                    DrawRay(pos, ray.Direction, max_len, null, 0);

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

        private static Ray[] GetRandomRayBall()
        {
            var balls = _icos.Value;
            return balls[StaticRandom.Next(balls.Length)];
        }

        #endregion
        #region Private Methods - adjust final

        private static float GetNewConfinedSpace(float prev_confined_percent, float how_blocked, float gain_factor, float elapsed_seconds)
        {
            if (elapsed_seconds > 0.5)       // avoid lag spikes wrecking the function
                elapsed_seconds = 0.5f;

            return (1 - gain_factor * elapsed_seconds) * prev_confined_percent + gain_factor * elapsed_seconds * how_blocked;
        }

        #endregion
        #region Private Methods - drawing

        private void EnsureDrawingSetup()
        {
            if (_renderer == null)
                _renderer = DebugRenderer3D.GetOrAddDebugRenderer3D();

            if (_rayvisual_lines == null)
                _rayvisual_lines = new List<DebugItem>();

            if (_rayvisual_hits == null)
                _rayvisual_hits = new List<DebugItem>();

            if (_rayHitColors == null)
                _rayHitColors = new Dictionary<int, Color>();
        }

        private void ClearDebugVisuals()
        {
            if (_renderer == null)
                return;

            if (_rayvisual_lines != null)
            {
                foreach (var line in _rayvisual_lines)
                    _renderer.Remove(line);

                _rayvisual_lines.Clear();
            }

            if (_rayvisual_hits != null)
            {
                foreach (var hit in _rayvisual_hits)
                    _renderer.Remove(hit);

                _rayvisual_hits.Clear();
            }

            if (_text != null)
                _renderer.Remove(_text);
            _text = null;

            _renderer = null;
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

            RemoveDespawned(_rayvisual_lines);
            RemoveDespawned(_rayvisual_hits);
        }
        private void FinishedDrawingRays()
        {
            for (int i = 0; i < _rayvisual_lines.Count; i++)
                _rayvisual_lines[i].Object.SetActive(i <= _line_index);     // this should be cheaper than removing/adding

            for (int i = 0; i < _rayvisual_hits.Count; i++)
                _rayvisual_hits[i].Object.SetActive(i <= _hit_index);
        }

        // TODO: Make a DrawRay2 that does its own ray that returns all matches.  Color the hits by layer (keep a dictionary)
        // As layers get added to the dictionary, log them
        private void DrawRay(Vector3 pos, Vector3 direction, float len, RaycastHit? hit, float percent)
        {
            Color color = hit == null ?
                UtilityColor.FromHex("A32B29") :
                UtilityColor.LERP_RGB(UtilityColor.FromHex("33E733"), UtilityColor.FromHex("2F4A2F"), percent);

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
                _rayvisual_lines.Add(_renderer.AddLine_Basic(pos, pos_to, LINE_THICKNESS, color));
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
                    _rayvisual_hits.Add(_renderer.AddDot(hit.Value.point, DOT_SIZE, UtilityColor.FromHex("34F035")));
                }
            }
        }
        private void DrawRay2(Vector3 pos, Vector3 direction, float len, RaycastHit? hit, float percent)
        {
            Color color = hit == null ?
                UtilityColor.FromHex("A32B29") :
                UtilityColor.LERP_RGB(UtilityColor.FromHex("33E733"), UtilityColor.FromHex("2F4A2F"), percent);

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
                _rayvisual_lines.Add(_renderer.AddLine_Basic(pos, pos_to, LINE_THICKNESS, color));
            }

            // Hit Dots
            if (hit != null)
            {
                var hits = Physics.RaycastAll(pos, direction, len, ScanningUtil.SolidObject_LayerMask.Value, QueryTriggerInteraction.Ignore);

                foreach (var hit2 in hits)
                {
                    int layer = hit2.transform.gameObject.layer;

                    if (!_rayHitColors.TryGetValue(layer, out Color hit_color))
                    {
                        hit_color = UtilityColor.RandomHSV(0, 1, 0.4f, 0.8f, 0.5f, 0.9f);
                        _rayHitColors.Add(layer, hit_color);
                        //Debug.Log($"Layer Hit: {layer}, '{LayerMask.LayerToName(layer)}', {UtilityColor.ToHex(hit_color, false, false)}");
                    }

                    _hit_index++;
                    if (_hit_index < _rayvisual_hits.Count)
                    {
                        _rayvisual_hits[_hit_index].Object.transform.position = hit2.point;
                        DebugRenderer3D.AdjustColor(_rayvisual_hits[_hit_index], hit_color);
                    }
                    else
                    {
                        _rayvisual_hits.Add(_renderer.AddDot(hit2.point, DOT_SIZE, hit_color));
                    }
                }
            }
        }

        private void DrawConfinedPercent()
        {
            EnsureDrawingSetup();

            // text pos
            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                Player.local.head.transform.right * 0.33f +
                Player.local.head.transform.up * -0.15f;

            string text = $"confined%: {Mathf.RoundToInt(ConfinedPercent * 100f)}";

            if (_text == null)
                _text = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, Color.blue, Color.white, TEXT_HEIGHT);

            _text.Object.transform.position = text_pos;
            _text.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_text, new_text: text);
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

        #endregion
        #region Private Methods - init

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
            // came from PartyPeople.UnitTests

            return new[]
            {
                new Ray(new Vector3(-0.11469849f, 0.11469849f, 0.11469849f),    new Vector3( -0.57735026f, 0.57735026f, 0.57735026f)),
                new Ray(new Vector3(0, 0.18558607f, 0.070887566f),              new Vector3(0, 0.93417233f, 0.3568221f)),
                new Ray(new Vector3(0, 0.18558607f, -0.070887566f),             new Vector3(0, 0.93417233f, -0.3568221f)),
                new Ray(new Vector3(-0.11469849f, 0.11469849f, -0.11469849f),   new Vector3(-0.57735026f, 0.57735026f, -0.57735026f)),
                new Ray(new Vector3(-0.18558607f, 0.070887566f, 0),             new Vector3(-0.93417233f, 0.3568221f, 0)),
                new Ray(new Vector3(0.11469849f, 0.11469849f, 0.11469849f),     new Vector3(0.57735026f, 0.57735026f, 0.57735026f)),
                new Ray(new Vector3(-0.070887566f, 0, 0.18558607f),             new Vector3(-0.3568221f, 0, 0.93417233f)),
                new Ray(new Vector3(-0.18558607f, -0.070887566f, 0),            new Vector3(-0.93417233f, -0.3568221f, 0)),
                new Ray(new Vector3(-0.070887566f, 0, -0.18558607f),            new Vector3(-0.3568221f, -0, -0.93417233f)),
                new Ray(new Vector3(0.11469849f, 0.11469849f, -0.11469849f),    new Vector3(0.57735026f, 0.57735026f, -0.57735026f)),
                new Ray(new Vector3(0.11469849f, -0.11469849f, 0.11469849f),    new Vector3(0.57735026f, -0.57735026f, 0.57735026f)),
                new Ray(new Vector3(0, -0.18558607f, 0.070887566f),             new Vector3(0, -0.93417233f, 0.3568221f)),
                new Ray(new Vector3(0, -0.18558607f, -0.070887566f),            new Vector3(0, -0.93417233f, -0.3568221f)),
                new Ray(new Vector3(0.11469849f, -0.11469849f, -0.11469849f),   new Vector3(0.57735026f, -0.57735026f, -0.57735026f)),
                new Ray(new Vector3(0.18558607f, -0.070887566f, 0),             new Vector3(0.93417233f, -0.3568221f, 0)),
                new Ray(new Vector3(0.070887566f, 0, 0.18558607f),              new Vector3(0.3568221f, 0, 0.93417233f)),
                new Ray(new Vector3(-0.11469849f, -0.11469849f, 0.11469849f),   new Vector3(-0.57735026f, -0.57735026f, 0.57735026f)),
                new Ray(new Vector3(-0.11469849f, -0.11469849f, -0.11469849f),  new Vector3(-0.57735026f, -0.57735026f, -0.57735026f)),
                new Ray(new Vector3(0.070887566f, 0, -0.18558607f),             new Vector3(0.3568221f, 0, -0.93417233f)),
                new Ray(new Vector3(0.18558607f, 0.070887566f, 0),              new Vector3(0.93417233f, 0.3568221f, -0)),
            };
        }

        #endregion
    }
}