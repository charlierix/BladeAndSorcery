using Jetpack2.InputWatchers;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.FlightProcessing
{
    // TODO: this class got huge, and will get much larger.  a lot of those private regions will need to be their own classes

    // TODO: need settings for free rotate vs align to any N degrees (15, 30, 45)
    // TODO: need optional settings for min/max pitch, min/max roll

    // TODO: need dead zones pre rotate and during rotate, also speed based.  that way, once rotating for some time, dead zones shrink, maybe capacitors stay longer

    // TODO: have the opposite of dead zone so if they look past that dot product, it speeds up capacitor
    // TODO: if they snap their head in a direction, that should also speed up the capcitor

    // TODO: this class has enough to test and visualize.  but once a lot of values are json config, focus on adding pitch and roll limits in RotateToLook2

    /// <summary>
    /// PullYawToLook only does 2D.  RotateToLook does 3D (also roll based on head roll)
    /// </summary>
    public class RotateToLook
    {
        #region class: Directions

        private class Directions
        {
            // world
            public Vector3 pos { get; set; }
            public Vector3 velocity { get; set; }
            public Vector3 center_player { get; set; }

            public Vector3 body_forward { get; set; }
            public Vector3 body_up { get; set; }
            public Vector3 head_forward { get; set; }       // look
            public Vector3 head_up { get; set; }

            // local
            //public Vector3 localroll_body_forward { get; set; }
            public Vector3 localroll_body_up { get; set; }
            //public Vector3 localroll_head_forward { get; set; }     // this should be the same as localroll_body_forward
            public Vector3 localroll_head_up { get; set; }

            public Quaternion quat_tolocalroll { get; set; }
            public Quaternion quat_fromlocalroll { get; set; }

            // --- these are only used for drawing ---
            public Vector3 localroll_forward { get; set; }
        }

        #endregion
        #region class: GazeResults

        private class GazeResults
        {
            // all directions are in world coords

            public float? yawpitch_confidence { get; set; }
            public Vector3 yawpitch_direction { get; set; }
            public Vector3 yaw_direction { get; set; }
            public Vector3 pitch_direction { get; set; }

            public float? roll_confidence { get; set; }
            public Vector3 roll_direction { get; set; }

            // --- these are only used for drawing ---
            public float confidence_roll_debug { get; set; }

            public float? confidence_yawpitch_offset { get; set; }
            public float confidence_yawpitch_offset_debug { get; set; }
            public Vector3 direction_yawpitch_offset { get; set; }

            public float? confidence_yawpitch_target { get; set; }
            public float confidence_yawpitch_target_debug { get; set; }
            public Vector3 direction_yawpitch_target { get; set; }

            public GazeBuffer.DominantDirectionResponse yawpitch_offset_result { get; set; }
            public GazeBuffer.DominantDirectionResponse roll_offset_result { get; set; }
        }

        #endregion
        #region struct: DeadzonePercents

        private struct DeadzonePercents
        {
            public float yaw { get; set; }
            public float pitch { get; set; }
            public float roll { get; set; }
        }

        #endregion

        #region Declaration Section

        private readonly PlayerRotator _rotator;

        private readonly GazeBuffer _gazebuffer = new GazeBuffer();
        private readonly GazeBuffer _gazebuffer_roll = new GazeBuffer();

        private readonly PlayerRagdollUtil _ragdollUtil = new PlayerRagdollUtil();

        private readonly System.Random _rand;

        private float _capacitor_yaw = 0f;
        private float _capacitor_pitch = 0f;
        private float _capacitor_roll = 0f;

        #region debug drawing vars

        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private const string FINALLINE_COLOR_MIN = "857754";
        private const string FINALLINE_COLOR_MAX = "F5BA28";

        private DebugRenderer3D _renderer = null;

        private DebugItem _body_forward = null;
        private DebugItem _lookline = null;
        private DebugItem _lookorth_yaw = null;
        private DebugItem _lookorth_pitch = null;

        private float _deadzone_inner_dot_yaw = float.MinValue;
        private float _deadzone_inner_dot_pitch = float.MinValue;
        private DebugItem _deadzone_inner = null;

        private float _deadzone_outer_dot_yaw = float.MinValue;
        private float _deadzone_outer_dot_pitch = float.MinValue;
        private DebugItem _deadzone_outer = null;

        private int _line_index = -1;
        private List<DebugItem> _lines = new List<DebugItem>();       // these are lines (showing contents of buffer)     // NOTE: this also holds derived lines from target

        private int _dot_index = -1;
        private List<DebugItem> _dots = new List<DebugItem>();       // these are dots (showing contents of buffer)

        private DebugItem _body_up = null;
        private DebugItem _head_up = null;
        private DebugItem _deadzone_inner_left = null;
        private DebugItem _deadzone_inner_right = null;
        private DebugItem _deadzone_outer_left = null;
        private DebugItem _deadzone_outer_right = null;

        private DebugItem _capa_yaw_vert = null;
        private DebugItem _capa_yaw_tick = null;
        private DebugItem _capa_pitch_vert = null;
        private DebugItem _capa_pitch_tick = null;
        private DebugItem _capa_roll_vert = null;
        private DebugItem _capa_roll_tick = null;
        private DebugItem _capa_text = null;

        private DebugItem _gaze_extra_text = null;

        private DebugItem _axis_yaw = null;
        private DebugItem _axis_pitch = null;
        private DebugItem _axis_roll = null;

        private DebugItem _turnrate = null;

        #endregion

        #endregion

        public RotateToLook(PlayerRotator rotator)
        {
            _rotator = rotator;
            _rand = StaticRandom.GetRandomForThread();
        }

        public void Clear()
        {
            _capacitor_yaw = 0f;
            _capacitor_pitch = 0f;
            _capacitor_roll = 0f;
            _gazebuffer.Clear();
            _gazebuffer_roll.Clear();

            if (UIModOptions.ShowRotateToLook)
                ClearDebugVisuals();
        }

        public void Update(float elapsed_seconds)
        {
            if (!(UIModOptions.ShouldRotateToLook_Yaw || UIModOptions.ShouldRotateToLook_Pitch || UIModOptions.ShouldRotateToLook_Roll))
                return;

            // Get body and head directions (also pos, velocity)
            var dirs = GetDirections();

            // Update gaze buffers, get their averaged directions
            var gaze = UpdateGazeBuffers(dirs);

            // Get percents inside dead zones
            var deadzones = GetDeadzonePercents(dirs, gaze);

            // Update the capacitors
            Vector3 look_yaw = dirs.head_forward.GetProjectedVector_plane(dirs.body_up).normalized;
            Vector3 look_pitch = dirs.head_forward.GetProjectedVector_plane(Vector3.Cross(dirs.body_forward, dirs.body_up)).normalized;        // normal is to the right

            _capacitor_yaw = UpdateCapacitor(_capacitor_yaw, gaze.yaw_direction, look_yaw, gaze.yawpitch_confidence, deadzones.yaw, elapsed_seconds);
            _capacitor_pitch = UpdateCapacitor(_capacitor_pitch, gaze.pitch_direction, look_pitch, gaze.yawpitch_confidence, deadzones.pitch, elapsed_seconds);
            _capacitor_roll = UpdateCapacitor(_capacitor_roll, gaze.roll_direction, dirs.head_up, gaze.roll_confidence, deadzones.roll, elapsed_seconds);

            // Get turn rates
            var turnrate_yaw = UIModOptions.ShouldRotateToLook_Yaw ?
                GetTurnRate(dirs.body_forward, gaze.yaw_direction, gaze.yawpitch_confidence, deadzones.yaw, _capacitor_yaw, UIModOptions.RotateToLook_TurnRate_Yaw) :
                null;

            var turnrate_pitch = UIModOptions.ShouldRotateToLook_Pitch ?
                GetTurnRate(dirs.body_forward, gaze.pitch_direction, gaze.yawpitch_confidence, deadzones.pitch, _capacitor_pitch, UIModOptions.RotateToLook_TurnRate_Pitch) :
                null;

            var turnrate_roll = UIModOptions.ShouldRotateToLook_Roll ?
                GetTurnRate(dirs.body_up, gaze.roll_direction, gaze.roll_confidence, deadzones.roll, _capacitor_roll, UIModOptions.RotateToLook_TurnRate_Roll) :
                null;

            // Turn Player
            TurnPlayer(turnrate_yaw, turnrate_pitch, turnrate_roll, dirs.center_player, elapsed_seconds);

            if (UIModOptions.ShowRotateToLook)
            {
                PrepareForDraw();

                DrawYawPitch(
                    dirs.body_forward, dirs.body_up, dirs.head_forward,
                    deadzones.yaw, deadzones.pitch,
                    gaze.direction_yawpitch_offset, gaze.confidence_yawpitch_offset,
                    gaze.direction_yawpitch_target, gaze.confidence_yawpitch_target,
                    gaze.yawpitch_direction, gaze.yaw_direction, gaze.pitch_direction, gaze.yawpitch_confidence);

                DrawRoll(dirs.localroll_forward, dirs.localroll_body_up, dirs.localroll_head_up, deadzones.roll, gaze.roll_direction, gaze.roll_confidence, dirs.quat_fromlocalroll);

                DrawCapacitors(deadzones.yaw, deadzones.pitch, deadzones.roll, gaze.yawpitch_confidence, gaze.roll_confidence, gaze.confidence_yawpitch_offset_debug, gaze.confidence_yawpitch_target_debug, gaze.confidence_roll_debug);

                DrawDebugGaze_ExtraText(gaze);

                DrawTurnRates(turnrate_yaw, turnrate_pitch, turnrate_roll);

                FinishedDraw();
            }
        }

        #region Private Methods - drawing

        private void EnsureDebugActive()
        {
            if (_renderer == null)
                _renderer = DebugRenderer3D.GetOrAddDebugRenderer3D();
        }

        private void ClearDebugVisuals()
        {
            if (_renderer == null)
                return;

            if (_body_forward != null)
            {
                _renderer.Remove(_body_forward);
                _body_forward = null;
            }

            if (_lookline != null)
            {
                _renderer.Remove(_lookline);
                _lookline = null;
            }

            if (_lookorth_yaw != null)
            {
                _renderer.Remove(_lookorth_yaw);
                _lookorth_yaw = null;
            }

            if (_lookorth_pitch != null)
            {
                _renderer.Remove(_lookorth_pitch);
                _lookorth_pitch = null;
            }

            if (_deadzone_inner != null)
            {
                _renderer.Remove(_deadzone_inner);
                _deadzone_inner = null;
            }

            if (_deadzone_outer != null)
            {
                _renderer.Remove(_deadzone_outer);
                _deadzone_outer = null;
            }

            if (_body_up != null)
            {
                _renderer.Remove(_body_up);
                _body_up = null;
            }

            if (_head_up != null)
            {
                _renderer.Remove(_head_up);
                _head_up = null;
            }

            if (_deadzone_inner_left != null)
            {
                _renderer.Remove(_deadzone_inner_left);
                _deadzone_inner_left = null;
            }

            if (_deadzone_inner_right != null)
            {
                _renderer.Remove(_deadzone_inner_right);
                _deadzone_inner_right = null;
            }

            if (_deadzone_outer_left != null)
            {
                _renderer.Remove(_deadzone_outer_left);
                _deadzone_outer_left = null;
            }

            if (_deadzone_outer_right != null)
            {
                _renderer.Remove(_deadzone_outer_right);
                _deadzone_outer_right = null;
            }

            if (_capa_yaw_vert != null)
            {
                _renderer.Remove(_capa_yaw_vert);
                _capa_yaw_vert = null;
            }

            if (_capa_yaw_tick != null)
            {
                _renderer.Remove(_capa_yaw_tick);
                _capa_yaw_tick = null;
            }

            if (_capa_pitch_vert != null)
            {
                _renderer.Remove(_capa_pitch_vert);
                _capa_pitch_vert = null;
            }

            if (_capa_pitch_tick != null)
            {
                _renderer.Remove(_capa_pitch_tick);
                _capa_pitch_tick = null;
            }

            if (_capa_roll_vert != null)
            {
                _renderer.Remove(_capa_roll_vert);
                _capa_roll_vert = null;
            }

            if (_capa_roll_tick != null)
            {
                _renderer.Remove(_capa_roll_tick);
                _capa_roll_tick = null;
            }

            if (_capa_text != null)
            {
                _renderer.Remove(_capa_text);
                _capa_text = null;
            }

            if (_gaze_extra_text != null)
            {
                _renderer.Remove(_gaze_extra_text);
                _gaze_extra_text = null;
            }

            if (_axis_yaw != null)
            {
                _renderer.Remove(_axis_yaw);
                _axis_yaw = null;
            }

            if (_axis_pitch != null)
            {
                _renderer.Remove(_axis_pitch);
                _axis_pitch = null;
            }

            if (_axis_roll != null)
            {
                _renderer.Remove(_axis_roll);
                _axis_roll = null;
            }

            if (_turnrate != null)
            {
                _renderer.Remove(_turnrate);
                _turnrate = null;
            }

            foreach (DebugItem item in _lines.Concat(_dots))
                _renderer.Remove(item);
            _lines.Clear();
            _dots.Clear();

            _renderer = null;
        }

        // These manage visuals that persist across frames.  Prepare removes despawned, finsih sets visibility based on how many are used this frame
        private void PrepareForDraw()
        {
            RemoveDespawned(_lines);
            RemoveDespawned(_dots);

            _line_index = -1;
            _dot_index = -1;
        }
        private void FinishedDraw()
        {
            SetActive(_lines, _line_index + 1);
            SetActive(_dots, _dot_index + 1);
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

        private static void SetActive(List<DebugItem> items, int count)
        {
            for (int i = 0; i < items.Count; i++)
                items[i].Object.SetActive(i < count);     // this should be cheaper than removing/adding
        }

        private void DrawYawPitch(Vector3 body_forward, Vector3 body_up, Vector3 look, float deadzone_percent_yaw, float deadzone_percent_pitch, Vector3 direction_offset, float? confidence_offset, Vector3 direction_target, float? confidence_target, Vector3 direction_final, Vector3 direction_final_yaw, Vector3 direction_final_pitch, float? confidence_final)
        {
            // NOTE: trying to avoid lines coming out of head position, so the lines go from INNER_DIST to PLANE_DIST
            const float PLANE_DIST = 1.5f;      // NOTE: calling it a plane, which it is for the deadzone circles, but all other graphics will go to the surface of the sphere at this radius
            const float INNER_DIST = 0.5f;

            EnsureDebugActive();

            Vector3 head_pos = Player.local.head.anchor.position;
            Vector3 plane_point = head_pos + body_forward * PLANE_DIST;

            // Forward
            if (_body_forward == null)
                _body_forward = _renderer.AddLine_Basic(head_pos + body_forward * INNER_DIST, plane_point, LINE_THICKNESS, Color.cyan);
            else
                DebugRenderer3D.AdjustLinePositions(_body_forward, head_pos + body_forward * INNER_DIST, plane_point);

            // Look
            if (_lookline == null)
                _lookline = _renderer.AddLine_Basic(head_pos + look * INNER_DIST, head_pos + look * PLANE_DIST, LINE_THICKNESS, Color.white);
            else
                DebugRenderer3D.AdjustLinePositions(_lookline, head_pos + look * INNER_DIST, head_pos + look * PLANE_DIST);

            // Look Orth Yaw (color based on dead zone percent)
            //Color color = Color.Lerp(new Color(1, 1, 1, 0), Color.white, deadzone_percent);     // TODO: this semitransparency doesn't seem to work
            Color color = Color.Lerp(Color.white, Color.black, deadzone_percent_yaw);       // 0% is white (no deadzone)
            Vector3 look_yaw = look.GetProjectedVector_plane(body_up);
            if (_lookorth_yaw == null)
                _lookorth_yaw = _renderer.AddLine_Basic(plane_point, head_pos + look_yaw * PLANE_DIST, LINE_THICKNESS, color);
            else
            {
                DebugRenderer3D.AdjustLinePositions(_lookorth_yaw, plane_point, head_pos + look_yaw * PLANE_DIST);
                DebugRenderer3D.AdjustColor(_lookorth_yaw, color);
            }

            // Look Orth Pitch (color based on dead zone percent)
            color = Color.Lerp(Color.white, Color.black, deadzone_percent_pitch);       // 0% is white (no deadzone)
            Vector3 look_pitch = look.GetProjectedVector_plane(Vector3.Cross(body_forward, body_up));
            if (_lookorth_pitch == null)
                _lookorth_pitch = _renderer.AddLine_Basic(plane_point, head_pos + look_pitch * PLANE_DIST, LINE_THICKNESS, color);
            else
            {
                DebugRenderer3D.AdjustLinePositions(_lookorth_pitch, plane_point, head_pos + look_pitch * PLANE_DIST);
                DebugRenderer3D.AdjustColor(_lookorth_pitch, color);
            }

            // Draw dead zones as ellipses
            DrawYawPitch_DeadzoneEllipse(ref _deadzone_inner, ref _deadzone_inner_dot_yaw, ref _deadzone_inner_dot_pitch, UIModOptions.RotToLook_DeadZone_Yaw_Full, UIModOptions.RotToLook_DeadZone_Pitch_Full, plane_point, body_forward, body_up, PLANE_DIST, _renderer);
            DrawYawPitch_DeadzoneEllipse(ref _deadzone_outer, ref _deadzone_outer_dot_yaw, ref _deadzone_outer_dot_pitch, UIModOptions.RotToLook_DeadZone_Yaw_Start, UIModOptions.RotToLook_DeadZone_Pitch_Start, plane_point, body_forward, body_up, PLANE_DIST, _renderer);

            // Gaze buffer results
            DrawGazeOffsets(PLANE_DIST, INNER_DIST, head_pos, body_forward, direction_offset, confidence_offset);
            DrawGazeTargets(PLANE_DIST, INNER_DIST, head_pos, body_forward, direction_target, confidence_target);

            // Final
            // NOTE: using offsets list
            if (confidence_final != null)
            {
                color = Color.Lerp(UtilityColor.FromHex(FINALLINE_COLOR_MAX), UtilityColor.FromHex(FINALLINE_COLOR_MIN), confidence_final.Value);
                DrawGazeOffsets_AddLine(body_forward, head_pos, PLANE_DIST, INNER_DIST, color, direction: direction_final);
                //DrawGazeOffsets_AddLine(body_forward, head_pos, PLANE_DIST, INNER_DIST, color, direction: direction_final_yaw);       // these are working like they are supposed to
                //DrawGazeOffsets_AddLine(body_forward, head_pos, PLANE_DIST, INNER_DIST, color, direction: direction_final_pitch);
            }
        }
        private static void DrawYawPitch_DeadzoneEllipse(ref DebugItem debug_item, ref float basedon_dot_yaw, ref float basedon_dot_pitch, float dead_zone_yaw, float dead_zone_pitch, Vector3 origin, Vector3 normal, Vector3 up, float plane_dist, DebugRenderer3D renderer)
        {
            if (dead_zone_yaw.IsNearValue(1) && dead_zone_pitch.IsNearValue(1))
            {
                if (debug_item != null)
                    renderer.Remove(debug_item);        // this would only happen when dragging the slider to one.  so just remove it
                debug_item = null;
                basedon_dot_yaw = 1;
                basedon_dot_pitch = 1;
                return;
            }

            if (debug_item != null && (!basedon_dot_yaw.IsNearValue(dead_zone_yaw) || !basedon_dot_pitch.IsNearValue(dead_zone_pitch)))
            {
                renderer.Remove(debug_item);        // the value changed, need to recalculate radius
                debug_item = null;
            }

            if (debug_item == null)
            {
                float radius_yaw = plane_dist * Mathf.Tan(Math1D.Dot_to_Radians(dead_zone_yaw));
                float radius_pitch = plane_dist * Mathf.Tan(Math1D.Dot_to_Radians(dead_zone_pitch));
                basedon_dot_yaw = dead_zone_yaw;
                basedon_dot_pitch = dead_zone_pitch;
                debug_item = renderer.AddEllipse(origin, normal, up, radius_yaw, radius_pitch, LINE_THICKNESS, Color.gray);
            }
            else
            {
                DebugRenderer3D.AdjustEllipsePosition(debug_item, origin, normal, up);
            }
        }

        private void DrawGazeOffsets(float plane_dist, float inner_dist, Vector3 head_pos, Vector3 body_forward, Vector3 direction, float? confidence)
        {
            if (_gazebuffer._offset.Count < UIModOptions.GazeBuffer_MaxCount * 0.8 && confidence == null)
                return;

            Color color_sample = UtilityColor.FromHex("3F4F68");     // blue
            Color color_derived = UtilityColor.FromHex("2176F4");

            // NOTE: only drawing the first and last to avoid clutter and better performance
            DrawGazeOffsets_AddLine(body_forward, head_pos, plane_dist, inner_dist, color_sample, sample: _gazebuffer._offset[0]);
            DrawGazeOffsets_AddLine(body_forward, head_pos, plane_dist, inner_dist, color_sample, sample: _gazebuffer._offset[_gazebuffer._offset.Count - 1]);

            if (confidence != null)
                DrawGazeOffsets_AddLine(body_forward, head_pos, plane_dist, inner_dist, color_derived, direction: direction);
        }
        private void DrawGazeOffsets_AddLine(Vector3 body_forward, Vector3 head_pos, float plane_dist, float inner_dist, Color color, GazeBuffer.GazeSample_Offset? sample = null, Vector3? direction = null)
        {
            Vector3 dir = sample != null ? sample.Value.Quaternion * body_forward :
                direction != null ? direction.Value :
                throw new ArgumentException("either sample or direction needs to be passed in");

            _line_index++;

            Vector3 from_point = head_pos + dir * inner_dist;
            Vector3 to_point = head_pos + dir * plane_dist;

            if (_line_index < _lines.Count)
            {
                DebugRenderer3D.AdjustLinePositions(_lines[_line_index], from_point, to_point);
            }
            else
            {
                _lines.Add(_renderer.AddLine_Basic(from_point, to_point, LINE_THICKNESS * 0.5f, color));
                DebugRenderer3D.AdjustColor(_lines[_line_index], color);
            }
        }

        private void DrawGazeTargets(float plane_dist, float inner_dist, Vector3 head_pos, Vector3 body_forward, Vector3 direction, float? confidence)
        {
            var sphere_keys = new List<string>();

            Color color_sample = UtilityColor.FromHex("58486B");        // purple
            Color color_derived = UtilityColor.FromHex("9126F5");

            foreach (var by_radius in _gazebuffer._target.Values)
            {
                foreach (var bucket in by_radius.Values)
                {
                    if (bucket.Count < UIModOptions.GazeBuffer_MaxCount * 0.8 && confidence == null)
                        continue;

                    // NOTE: only drawing the first and last to avoid clutter and better performance
                    DrawGazeTargets_AddDot(bucket[0].Hit, color_sample);
                    DrawGazeTargets_AddDot(bucket[bucket.Count - 1].Hit, color_sample);
                }
            }

            // NOTE: this is getting stored in the offsets list, since there's no need for a separate list (may want to change its name)
            if (confidence != null)
                DrawGazeOffsets_AddLine(body_forward, head_pos, plane_dist, inner_dist, color_derived, direction: direction);
        }
        private void DrawGazeTargets_AddDot(Vector3 pos, Color color)
        {
            _dot_index++;

            if (_dot_index < _dots.Count)
            {
                _dots[_dot_index].Object.transform.position = pos;
                DebugRenderer3D.AdjustColor(_dots[_dot_index], color);
            }
            else
            {
                _dots.Add(_renderer.AddDot(pos, DOT_SIZE / 4, color));
            }
        }

        private void DrawDebugGaze_ExtraText(GazeResults gaze)
        {
            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.7f +
                Player.local.head.transform.right * 0.4f +
                Player.local.head.transform.up * 0.4f;

            var lines = new[]
            {
                "** yaw/pitch **",
                gaze.yawpitch_offset_result.Reason,
                $"min: {gaze.yawpitch_offset_result.MinConfidence.ToStringSignificantDigits(2)}",
                $"max: {gaze.yawpitch_offset_result.MaxConfidence.ToStringSignificantDigits(2)}",
                $"count: {_gazebuffer._offset.Count}",
                $"skip %: {_gazebuffer._frameskip_adjust_percent.ToStringSignificantDigits(4)}",
                "",
                "** roll **",
                gaze.roll_offset_result.Reason,
                $"min: {gaze.roll_offset_result.MinConfidence.ToStringSignificantDigits(2)}",
                $"max: {gaze.roll_offset_result.MaxConfidence.ToStringSignificantDigits(2)}",
                $"count: {_gazebuffer_roll._offset.Count}",
                $"skip %: {_gazebuffer_roll._frameskip_adjust_percent.ToStringSignificantDigits(4)}",
            };

            string text = string.Join(Environment.NewLine, lines);

            if (_gaze_extra_text == null)
                _gaze_extra_text = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, Color.green, Color.black, TEXT_HEIGHT * lines.Length);

            _gaze_extra_text.Object.transform.position = text_pos;
            _gaze_extra_text.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_gaze_extra_text, new_text: text);
        }

        private void DrawRoll(Vector3 body_forward, Vector3 body_up, Vector3 head_up, float deadzone_percent, Vector3 direction_roll, float? confidence_roll, Quaternion from_local)
        {
            const float LINE_LEN = 0.35f;

            EnsureDebugActive();

            // start point
            Vector3 origin = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.25f +
                Player.local.head.transform.right * -0.15f +
                Player.local.head.transform.up * 0.15f;

            // body_up
            if (_body_up == null)
                _body_up = _renderer.AddLine_Basic(origin, origin + from_local * body_up * LINE_LEN, LINE_THICKNESS, Color.cyan);
            else
                DebugRenderer3D.AdjustLinePositions(_body_up, origin, origin + from_local * body_up * LINE_LEN);

            // head_up
            if (_head_up == null)
                _head_up = _renderer.AddLine_Basic(origin, origin + from_local * head_up * LINE_LEN, LINE_THICKNESS, Color.white);
            else
                DebugRenderer3D.AdjustLinePositions(_head_up, origin, origin + from_local * head_up * LINE_LEN);

            // dead zones
            DrawRoll_DeadzoneLines(ref _deadzone_inner_left, ref _deadzone_inner_right, UIModOptions.RotToLook_DeadZone_Roll_Full, origin, from_local * body_up, from_local * body_forward, LINE_LEN, _renderer);
            DrawRoll_DeadzoneLines(ref _deadzone_outer_left, ref _deadzone_outer_right, UIModOptions.RotToLook_DeadZone_Roll_Start, origin, from_local * body_up, from_local * body_forward, LINE_LEN, _renderer);

            // buffer samples
            // sample[0] has quat that needs to be multiplied by body_up.  then multiply by this function's quat to rotate onto the radar display's plane
            DrawGazeRoll(origin, direction_roll, confidence_roll, body_up, LINE_LEN, from_local);
        }
        private static void DrawRoll_DeadzoneLines(ref DebugItem debug_item_left, ref DebugItem debug_item_right, float dead_zone, Vector3 origin, Vector3 up, Vector3 normal, float line_len, DebugRenderer3D renderer)
        {
            if (dead_zone.IsNearValue(1))
            {
                if (debug_item_left != null)
                    renderer.Remove(debug_item_left);        // this would only happen when dragging the slider to one.  so just remove it
                debug_item_left = null;

                if (debug_item_right != null)
                    renderer.Remove(debug_item_right);        // this would only happen when dragging the slider to one.  so just remove it
                debug_item_right = null;

                return;
            }

            float angle = Math1D.Dot_to_Degrees(dead_zone);

            Vector3 line_left = Quaternion.AngleAxis(-angle, normal) * up * line_len;
            Vector3 line_right = Quaternion.AngleAxis(angle, normal) * up * line_len;

            if (debug_item_left == null)
                debug_item_left = renderer.AddLine_Basic(origin, origin + line_left, LINE_THICKNESS, Color.gray);
            else
                DebugRenderer3D.AdjustLinePositions(debug_item_left, origin, origin + line_left);

            if (debug_item_right == null)
                debug_item_right = renderer.AddLine_Basic(origin, origin + line_right, LINE_THICKNESS, Color.gray);
            else
                DebugRenderer3D.AdjustLinePositions(debug_item_right, origin, origin + line_right);
        }
        private void DrawRoll_AddLine(Vector3 from_point, Vector3 to_point, Color color)
        {
            _line_index++;

            if (_line_index < _lines.Count)
            {
                DebugRenderer3D.AdjustLinePositions(_lines[_line_index], from_point, to_point);
            }
            else
            {
                _lines.Add(_renderer.AddLine_Basic(from_point, to_point, LINE_THICKNESS * 0.5f, color));
                DebugRenderer3D.AdjustColor(_lines[_line_index], color);
            }
        }
        private void DrawGazeRoll(Vector3 origin, Vector3 direction, float? confidence, Vector3 body_up, float line_len, Quaternion from_local)
        {
            if (_gazebuffer_roll._offset.Count < UIModOptions.GazeBuffer_MaxCount * 0.8 && confidence == null)
                return;

            Color color_sample = UtilityColor.FromHex("426B5E");        // green, almost cyan
            //Color color_derived = UtilityColor.FromHex("2AF5B4");

            // first rotation is relative to body_up
            Vector3 line_0 = _gazebuffer_roll._offset[0].Quaternion * body_up * line_len;
            Vector3 line_N = _gazebuffer_roll._offset[_gazebuffer_roll._offset.Count - 1].Quaternion * body_up * line_len;

            // now rotate to world
            line_0 = from_local * line_0;
            line_N = from_local * line_N;

            DrawRoll_AddLine(origin, origin + line_0, color_sample);
            DrawRoll_AddLine(origin, origin + line_N, color_sample);

            // Final
            if (confidence != null)
            {
                Color color = Color.Lerp(UtilityColor.FromHex(FINALLINE_COLOR_MAX), UtilityColor.FromHex(FINALLINE_COLOR_MIN), confidence.Value);
                // NOTE: direction is already world coords
                DrawRoll_AddLine(origin, origin + direction * line_len, color);
            }
        }

        private void DrawCapacitors(float deadzone_yaw_percent, float deadzone_pitch_percent, float deadzone_roll_percent, float? confidence_yawpitch, float? confidence_roll, float confidence_yawpitch_offset_debug, float confidence_yawpitch_target_debug, float confidence_roll_debug)
        {
            const float HEIGHT = 0.32f;
            const float TICK_HALF_WIDTH = 0.04f;

            EnsureDebugActive();

            Vector3 up = Player.local.head.transform.up;
            Vector3 right = Player.local.head.transform.right;

            Vector3 bottom = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                right * -0.4f +
                up * -0.47f;

            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.7f +
                right * -0.4f +
                up * (-0.47f + HEIGHT / 2f);

            Vector3 leftright_offset = right * (TICK_HALF_WIDTH * 2);

            DrawCapacitors_Single(ref _capa_yaw_vert, ref _capa_yaw_tick, _capacitor_yaw, _renderer, bottom - leftright_offset, up, right, HEIGHT, TICK_HALF_WIDTH, UtilityColor.FromHex("4E2BCC"));
            DrawCapacitors_Single(ref _capa_pitch_vert, ref _capa_pitch_tick, _capacitor_pitch, _renderer, bottom, up, right, HEIGHT, TICK_HALF_WIDTH, UtilityColor.FromHex("1478BA"));
            DrawCapacitors_Single(ref _capa_roll_vert, ref _capa_roll_tick, _capacitor_roll, _renderer, bottom + leftright_offset, up, right, HEIGHT, TICK_HALF_WIDTH, UtilityColor.FromHex("1DDB9F"));

            //string text = $"capacitor: {_capacitor.ToStringSignificantDigits(2)}";

            var lines = new[]
            {
                "** capacitors **",
                $"yaw: {_capacitor_yaw.ToStringSignificantDigits(2)}",
                $"pitch: {_capacitor_pitch.ToStringSignificantDigits(2)}",
                $"roll: {_capacitor_roll.ToStringSignificantDigits(2)}",
                "",
                "** confidence final **",
                $"yaw/pitch: {confidence_yawpitch?.ToStringSignificantDigits(2) ?? "--"}",
                $"roll: {confidence_roll?.ToStringSignificantDigits(2) ?? "--"}",
                "",
                "** confidence debug **",
                $"yaw/pitch offset: {confidence_yawpitch_offset_debug.ToStringSignificantDigits(2)}",
                $"yaw/pitch target: {confidence_yawpitch_target_debug.ToStringSignificantDigits(2)}",
                $"roll: {confidence_roll_debug.ToStringSignificantDigits(2)}",
                "",
                "** deadzone % **",
                $"yaw: {deadzone_yaw_percent.ToStringSignificantDigits(2)}",
                $"pitch: {deadzone_pitch_percent.ToStringSignificantDigits(2)}",
                $"roll: {deadzone_roll_percent.ToStringSignificantDigits(2)}",
            };

            string text = string.Join(Environment.NewLine, lines);

            if (_capa_text == null)
                _capa_text = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, Color.black, Color.white, TEXT_HEIGHT * lines.Length);

            _capa_text.Object.transform.position = text_pos;
            _capa_text.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_capa_text, new_text: text);
        }
        private static void DrawCapacitors_Single(ref DebugItem vert, ref DebugItem tick, float capacitor, DebugRenderer3D renderer, Vector3 bottom, Vector3 up, Vector3 right, float height, float tick_half_width, Color vert_color)
        {
            // vertical line
            if (vert == null)
                vert = renderer.AddLine_Basic(bottom, bottom + up * height, LINE_THICKNESS, vert_color);
            else
                DebugRenderer3D.AdjustLinePositions(vert, bottom, bottom + up * height);

            // horizontal tick line that is capacitor value
            Vector3 tick_mid = bottom + up * (capacitor * height);

            if (tick == null)
                tick = renderer.AddLine_Basic(tick_mid - right * tick_half_width, tick_mid + right * tick_half_width, LINE_THICKNESS, Color.yellow);
            else
                DebugRenderer3D.AdjustLinePositions(tick, tick_mid - right * tick_half_width, tick_mid + right * tick_half_width);
        }

        private void DrawTurnRates((Vector3 axis, float degrees_per_sec)? turnrate_yaw, (Vector3 axis, float degrees_per_sec)? turnrate_pitch, (Vector3 axis, float degrees_per_sec)? turnrate_roll)
        {
            const float LINE_LEN = 0.3f;
            const float LINE_LEN_HIDDEN = 0.001f;       // don't bother with visibility, just make it tiny

            EnsureDebugActive();

            // Axis Lines
            Vector3 axis_center = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.25f +
                Player.local.head.transform.right * +0.15f +
                Player.local.head.transform.up * 0.15f;

            Vector3 axis_yaw = turnrate_yaw?.axis * LINE_LEN ?? new Vector3(LINE_LEN_HIDDEN, 0, 0);
            Vector3 axis_pitch = turnrate_pitch?.axis * LINE_LEN ?? new Vector3(LINE_LEN_HIDDEN, 0, 0);
            Vector3 axis_roll = turnrate_roll?.axis * LINE_LEN ?? new Vector3(LINE_LEN_HIDDEN, 0, 0);

            if (_axis_yaw == null)
                _axis_yaw = _renderer.AddLine_Basic(axis_center, axis_center + axis_yaw, LINE_THICKNESS, Color.green);
            else
                DebugRenderer3D.AdjustLinePositions(_axis_yaw, axis_center, axis_center + axis_yaw);

            if (_axis_pitch == null)
                _axis_pitch = _renderer.AddLine_Basic(axis_center, axis_center + axis_pitch, LINE_THICKNESS, Color.red);
            else
                DebugRenderer3D.AdjustLinePositions(_axis_pitch, axis_center, axis_center + axis_pitch);

            if (_axis_roll == null)
                _axis_roll = _renderer.AddLine_Basic(axis_center, axis_center + axis_roll, LINE_THICKNESS, Color.blue);
            else
                DebugRenderer3D.AdjustLinePositions(_axis_roll, axis_center, axis_center + axis_roll);

            // Text
            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                Player.local.head.transform.right * 0.35f +
                Player.local.head.transform.up * -0.25f;

            string degtext_yaw = turnrate_yaw != null ?
                Mathf.Round(turnrate_yaw.Value.degrees_per_sec).ToString() :
                "--";

            string degtext_pitch = turnrate_pitch != null ?
                Mathf.Round(turnrate_pitch.Value.degrees_per_sec).ToString() :
                "--";

            string degtext_roll = turnrate_roll != null ?
                Mathf.Round(turnrate_roll.Value.degrees_per_sec).ToString() :
                "--";

            string text = $"turn rates{Environment.NewLine}yaw: {degtext_yaw}{Environment.NewLine}pitch: {degtext_pitch}{Environment.NewLine}roll: {degtext_roll}";

            if (_turnrate == null)
                _turnrate = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, Color.black, Color.cyan, TEXT_HEIGHT * 4);

            _turnrate.Object.transform.position = text_pos;
            _turnrate.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_turnrate, new_text: text);
        }

        #endregion
        #region Private Methods - directions

        private Directions GetDirections()
        {
            // initial values (world coords)
            var (body_forward, body_up) = _ragdollUtil.GetRagdollForwardUp();
            Vector3 head_forward = Player.local.head.transform.forward;
            Vector3 head_up = Player.local.head.transform.up;

            // ---- FOR ROLL ----
            var (head_up2, head_forward2) = GetProjecteHeadUp(head_up, head_forward, body_forward);      // pull head into the plane of body up and right

            // rotated so that forward is Z, up is Y
            var (body_up3, head_up3, quat3) = RotateUps(body_forward, body_up, head_up2);
            Vector3 body_forward3 = quat3 * body_forward;
            //Vector3 head_forward3 = quat3 * head_forward2;

            // Positions
            Vector3 head_pos = Player.local.head.anchor.position;
            Vector3 foot_pos = Math3D.GetAverage(Player.local.footLeft.ragdollFoot.root.position, Player.local.footRight.ragdollFoot.root.position);        // Player.local.transform.position is the room level origin

            return new Directions
            {
                pos = head_pos,
                velocity = Player.local.locomotion.physicBody.velocity,
                center_player = foot_pos + (head_pos - foot_pos) * 0.5f,

                body_forward = body_forward,
                body_up = body_up,
                head_forward = head_forward,
                head_up = head_up,

                localroll_body_up = body_up3,
                localroll_head_up = head_up3,

                //localroll_body_forward = body_forward3,
                //localroll_head_forward = head_forward3,
                localroll_forward = body_forward3,      // body_forward3 and head_forward3 should be identical after rotation

                quat_tolocalroll = quat3,
                quat_fromlocalroll = Quaternion.Inverse(quat3),
            };
        }

        /// <summary>
        /// Projects head_up into the plane of body_up and body_right
        /// </summary>
        private static (Vector3 up, Vector3 forward) GetProjecteHeadUp(Vector3 head_up, Vector3 head_forward, Vector3 body_forward)
        {
            var quat = Quaternion.FromToRotation(head_forward, body_forward);
            return (quat * head_up, quat * head_forward);
        }

        private static (Vector3 body_up, Vector3 head_up, Quaternion quat) RotateUps(Vector3 body_forward, Vector3 body_up, Vector3 head_up)
        {
            DoubleVector from = new DoubleVector(body_forward, body_up);
            DoubleVector to = new DoubleVector(new Vector3(0, 0, 1), new Vector3(0, 1, 0));

            Quaternion quat = Math3D.GetRotation(from, to);

            return (quat * body_up, quat * head_up, quat);
        }

        #endregion
        #region Private Methods - gaze buffers

        private GazeResults UpdateGazeBuffers(Directions dirs)
        {
            _gazebuffer.PrepareForNewFrame();
            _gazebuffer_roll.PrepareForNewFrame();

            // Populate gaze buffers
            _gazebuffer.AddSample_Offset(dirs.head_forward, dirs.body_forward);
            _gazebuffer.AddSample_Target(dirs.pos, dirs.head_forward, dirs.velocity.magnitude);
            _gazebuffer_roll.AddSample_Offset(dirs.localroll_head_up, dirs.localroll_body_up);

            // Get gazed vectors
            float? confidence_yawpitch_offset = null;
            //if (_gazebuffer.TryGetDominantDirection_Offset(out var direction_yawpitch_offset, out float confidence_yawpitch_offset_debug, dirs.body_forward))
            if (_gazebuffer.TryGetDominantDirection_Offset(out var result_yawpitch_offset, dirs.body_forward))
                confidence_yawpitch_offset = result_yawpitch_offset.Confidence;

            float? confidence_yawpitch_target = null;
            if (_gazebuffer.TryGetDominantDirection_Target_Debug(out Vector3 direction_yawpitch_target, out float confidence_yawpitch_target_debug, out float sphere_radius, out Vector3 sphere_origin, dirs.pos))
                confidence_yawpitch_target = confidence_yawpitch_target_debug;

            var yawpitch = GetFinalConfidence(result_yawpitch_offset.DominantDirection, confidence_yawpitch_offset, direction_yawpitch_target, confidence_yawpitch_target);

            Vector3 dir_yaw = Vector3.right;
            Vector3 dir_pitch = Vector3.down;
            if (yawpitch.confidence != null)
            {
                dir_yaw = yawpitch.direction.GetProjectedVector_plane(dirs.body_up).normalized;
                dir_pitch = yawpitch.direction.GetProjectedVector_plane(Vector3.Cross(dirs.body_forward, dirs.body_up)).normalized;        // normal is to the right
            }

            float? confidence_roll = null;
            //if (_gazebuffer_roll.TryGetDominantDirection_Offset(out Vector3 direction_roll_local, out float confidence_roll_debug, dirs.localroll_body_up))
            if (_gazebuffer_roll.TryGetDominantDirection_Offset(out var result_roll_local, dirs.localroll_body_up))
                confidence_roll = result_roll_local.Confidence;

            Vector3 direction_roll = dirs.quat_fromlocalroll * result_roll_local.DominantDirection;

            return new GazeResults
            {
                yawpitch_confidence = yawpitch.confidence,
                yawpitch_direction = yawpitch.direction,
                yaw_direction = dir_yaw,
                pitch_direction = dir_pitch,

                roll_confidence = confidence_roll,
                roll_direction = direction_roll,

                confidence_yawpitch_offset = confidence_yawpitch_offset,
                direction_yawpitch_offset = result_yawpitch_offset.DominantDirection,
                confidence_yawpitch_target = confidence_yawpitch_target,
                direction_yawpitch_target = direction_yawpitch_target,

                confidence_yawpitch_offset_debug = result_yawpitch_offset.Confidence,
                confidence_yawpitch_target_debug = confidence_yawpitch_target_debug,
                confidence_roll_debug = result_roll_local.Confidence,

                yawpitch_offset_result = result_yawpitch_offset,
                roll_offset_result = result_roll_local,
            };
        }

        private static (Vector3 direction, float? confidence) GetFinalConfidence(Vector3 direction_offset, float? confidence_offset, Vector3 direction_target, float? confidence_target)
        {
            const float RETURN_CONFIDENCE_THRESHOLD = 0.7f;
            float THRESHOLD_OFFSET = UIModOptions.GazeBuffer_GazeConfidence_Offset;
            float THRESHOLD_TARGET = UIModOptions.GazeBuffer_GazeConfidence_Target;

            if (confidence_offset == null && confidence_target == null)
                return (Vector3.zero, null);

            if (confidence_offset != null && confidence_target != null)
            {
                float scaledcon_offset = GetScaledConfidence(confidence_offset.Value, THRESHOLD_OFFSET, RETURN_CONFIDENCE_THRESHOLD);
                float scaledcon_target = GetScaledConfidence(confidence_target.Value, THRESHOLD_TARGET, RETURN_CONFIDENCE_THRESHOLD);

                var combined = GetFinalConfidence_Combined(direction_offset, scaledcon_offset, direction_target, scaledcon_target);

                return (combined.direction, combined.confidence);
            }

            if (confidence_offset != null)
                return (direction_offset, GetScaledConfidence(confidence_offset.Value, THRESHOLD_OFFSET, RETURN_CONFIDENCE_THRESHOLD));

            if (confidence_target != null)
                return (direction_target, GetScaledConfidence(confidence_target.Value, THRESHOLD_TARGET, RETURN_CONFIDENCE_THRESHOLD));

            throw new ApplicationException($"Execution shouldn't get here: {confidence_offset}, {confidence_target}");
        }

        /// <summary>
        /// Does an average of the two directions, weighted by confidence percents.  So if one is high confidence and one
        /// is low, the average will point mostly along the high confidence direction, even if they are in opposite directions
        /// 
        /// The returned confidence favors the input with higher confidence.  If both are similar confidence, then normalized
        /// dot product has a strong influence over final (two strong opinions in opposing directions make a weak final opinion
        /// in the avg direction)
        /// </summary>
        private static (Vector3 direction, float confidence) GetFinalConfidence_Combined(Vector3 dir1, float con1, Vector3 dir2, float con2)
        {
            // Clamping shouldn't be needed, but it ensures no odd bugs pop up
            con1 = Mathf.Clamp01(con1);
            con2 = Mathf.Clamp01(con2);

            // Compute normalized dot product (alignment 0 to 1)
            float dot = Vector3.Dot(dir1, dir2);
            float norm_dot = (dot + 1) / 2;

            float c_min = Mathf.Min(con1, con2);
            float c_max = Mathf.Max(con1, con2);

            float c_avg = (c_min + c_max) / 2;
            float weight = c_min / c_max;

            float c_final = weight * norm_dot * c_avg + (1 - weight) * c_max;

            // Get weighted average of directions
            Vector3 dir_combined = (dir1 * con1 + dir2 * con2) / (con1 + con2);
            dir_combined = dir_combined.normalized;

            return (dir_combined, c_final);
        }

        private static float GetScaledConfidence(float value, float from_threshold, float to_threshold)
        {
            if (value >= from_threshold)
            {
                // .95 of .9 is 50% from .9 to 1
                float percent = (value - from_threshold) / (1 - from_threshold);
                return to_threshold + (1 - to_threshold) * percent;
            }
            else
            {
                float percent = value / from_threshold;
                return to_threshold * percent;
            }
        }

        #endregion
        #region Private Methods - dead zones

        private static DeadzonePercents GetDeadzonePercents(Directions dirs, GazeResults gaze)
        {
            return new DeadzonePercents
            {
                yaw = gaze.yawpitch_confidence != null ?
                    GetDeadZonePercent(dirs.body_forward, gaze.yaw_direction, UIModOptions.RotToLook_DeadZone_Yaw_Full, UIModOptions.RotToLook_DeadZone_Yaw_Start) :
                    1,

                pitch = gaze.yawpitch_confidence != null ?
                    GetDeadZonePercent(dirs.body_forward, gaze.pitch_direction, UIModOptions.RotToLook_DeadZone_Pitch_Full, UIModOptions.RotToLook_DeadZone_Pitch_Start) :
                    1,

                roll = gaze.roll_confidence != null ?
                    GetDeadZonePercent(dirs.body_up, gaze.roll_direction, UIModOptions.RotToLook_DeadZone_Roll_Full, UIModOptions.RotToLook_DeadZone_Roll_Start) :
                    1,
            };
        }

        private static float GetDeadZonePercent(Vector3 forward, Vector3 direction, float dot_full, float dot_start)
        {
            float dot = Vector3.Dot(forward, direction);

            if (dot >= dot_full)      // using > because 1 is directly looking along forward, down to -1 which is directly away
                return 1;       // desired direction is in the dead zone (too close to forward)

            // if between start and full deadzones, run it through 1-cos so that there are no sharp speed changes
            if (dot >= dot_start)
            {
                float gap = dot_full - dot_start;
                float percent_gap = (dot - dot_start) / gap;
                return (1 - Mathf.Cos(Mathf.PI * percent_gap)) / 2;
            }

            return 0;
        }

        #endregion
        #region Private Methods - capicitors

        private static float UpdateCapacitor(float capacitor, Vector3 target, Vector3 look, float? confidence, float deadzone_percent, float elapsed_seconds)
        {
            float upper_dot = UIModOptions.RotToLook_Capacitor_UpperDot;
            float lower_dot = UIModOptions.RotToLook_Capacitor_LowerDot;
            float bottom_dot = UIModOptions.RotToLook_Capacitor_BottomDot;

            float discharge_speed = UIModOptions.RotToLook_Capacitor_DischargeSpeed;

            // Calculate alignment between target and look directions
            float dot = Vector3.Dot(target, look);

            var retVal = capacitor;

            // Determine which region we're in
            if (confidence == null || deadzone_percent.IsNearValue(1))
            {
                // DISCHARGE REGION: special cases, discharge at max rate
                retVal -= discharge_speed * (float)elapsed_seconds;
            }
            else if (dot > upper_dot)
            {
                // CHARGE REGION: dot > upper threshold
                // Normalize to 0-1 range based on available threshold window
                float chargeFactor = (dot - upper_dot) / (1f - upper_dot);
                float chargeRate = UIModOptions.RotToLook_Capacitor_ChargeSpeed * Mathf.Pow(chargeFactor, UIModOptions.RotToLook_Capacitor_ChargePower);
                retVal += chargeRate * confidence.Value * (1 - deadzone_percent) * (float)elapsed_seconds;
            }
            else if (dot < bottom_dot)
            {
                // DISCHARGE REGION: max amount
                retVal -= discharge_speed * (float)elapsed_seconds;
            }
            else if (dot < lower_dot)
            {
                // DISCHARGE REGION: dot < lower threshold
                // Normalize to 0-1 range based on threshold position
                float decayFactor = UtilityMath.GetScaledValue_Capped(0, 1, bottom_dot, lower_dot, dot);
                float decayRate = discharge_speed * Mathf.Pow(decayFactor, UIModOptions.RotToLook_Capacitor_DischargePower);

                float deadzone_discharge = deadzone_percent > 0 ?
                    discharge_speed * deadzone_percent :
                    0;

                float final_rate = Mathf.Clamp(decayRate + deadzone_discharge, 0, discharge_speed);

                // NOTE: not sure if this should consider confidence percent.  I can't think of why it would matter when discharging
                retVal -= final_rate * (float)elapsed_seconds;
            }
            else if (deadzone_percent > 0)
            {
                // DISCHARGE REGION: they are in neutral zone, but also in a partial deadzone with forward.  discharge by that deadzone percent
                retVal -= discharge_speed * deadzone_percent * (float)elapsed_seconds;
            }
            // else: NEUTRAL WINDOW - capacitor remains unchanged

            // Enforce capacitor bounds
            retVal = Mathf.Clamp(retVal, 0f, 1f);

            return retVal;
        }

        #endregion
        #region Private Methods - turn rate

        // Version 1 doesn't bother with angular momentum

        // This calculates degrees per second.  It's up to the caller to multiply be elapsed time
        private static (Vector3 axis, float degrees_per_sec)? GetTurnRate(Vector3 forward, Vector3 direction, float? confidence, float deadzone_percent, float capacitor, float max_speed)
        {
            if (confidence == null)
                return null;       // not holding a gaze at anything

            if (capacitor.IsNearZero())
                return null;       // capacitor says no

            if (deadzone_percent.IsNearValue(1))
                return null;

            // figure out which way to rotate
            Quaternion quat = Quaternion.FromToRotation(forward, direction);
            quat.ToAngleAxis(out float rot_angle, out Vector3 rot_axis);

            // figure out angular speed
            // NOTE: not reducing by confidence percent.  that has already influenced capacitor charge rate
            float speed = max_speed * capacitor * (1 - deadzone_percent);
            if (rot_angle < 0)
                speed = -speed;     // shouldn't happen

            return (rot_axis, speed);
        }

        #endregion
        #region Private Methods - turn player

        private void TurnPlayer((Vector3 axis, float degrees_per_sec)? turnrate_yaw, (Vector3 axis, float degrees_per_sec)? turnrate_pitch, (Vector3 axis, float degrees_per_sec)? turnrate_roll, Vector3 center_player, float elapsed_seconds)
        {
            switch (_rand.Next(6))
            {
                case 0:     // y p r
                    TurnPlayer(turnrate_yaw, center_player, elapsed_seconds);
                    TurnPlayer(turnrate_pitch, center_player, elapsed_seconds);
                    TurnPlayer(turnrate_roll, center_player, elapsed_seconds);
                    break;

                case 1:     // y r p
                    TurnPlayer(turnrate_yaw, center_player, elapsed_seconds);
                    TurnPlayer(turnrate_roll, center_player, elapsed_seconds);
                    TurnPlayer(turnrate_pitch, center_player, elapsed_seconds);
                    break;

                case 2:     // p r y
                    TurnPlayer(turnrate_pitch, center_player, elapsed_seconds);
                    TurnPlayer(turnrate_roll, center_player, elapsed_seconds);
                    TurnPlayer(turnrate_yaw, center_player, elapsed_seconds);
                    break;

                case 3:     // p y r
                    TurnPlayer(turnrate_pitch, center_player, elapsed_seconds);
                    TurnPlayer(turnrate_yaw, center_player, elapsed_seconds);
                    TurnPlayer(turnrate_roll, center_player, elapsed_seconds);
                    break;

                case 4:     // r y p
                    TurnPlayer(turnrate_roll, center_player, elapsed_seconds);
                    TurnPlayer(turnrate_yaw, center_player, elapsed_seconds);
                    TurnPlayer(turnrate_pitch, center_player, elapsed_seconds);
                    break;

                default:    // r p y
                    TurnPlayer(turnrate_roll, center_player, elapsed_seconds);
                    TurnPlayer(turnrate_pitch, center_player, elapsed_seconds);
                    TurnPlayer(turnrate_yaw, center_player, elapsed_seconds);
                    break;
            }
        }
        private void TurnPlayer((Vector3 axis, float degrees_per_sec)? turnrate, Vector3 center_player, float elapsed_seconds)
        {
            if (turnrate == null)
                return;

            _rotator.RotateAround(center_player, turnrate.Value.axis, turnrate.Value.degrees_per_sec, elapsed_seconds);
        }

        #endregion
    }
}
