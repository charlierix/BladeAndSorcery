using Jetpack.InputWatchers;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.FlightProcessing
{
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
        }

        #endregion
        #region class: GazeResults

        private class GazeResults
        {
            public float? yawpitch_confidence { get; set; }
            public Vector3 yawpitch_direction { get; set; }

            public float? roll_confidence { get; set; }
            public Vector3 roll_direction { get; set; }

            // --- these are only used for drawing ---
            public float? confidence_yawpitch_offset { get; set; }
            public Vector3 direction_yawpitch_offset { get; set; }
            public float? confidence_yawpitch_target { get; set; }
            public Vector3 direction_yawpitch_target { get; set; }
        }

        #endregion
        #region struct: DeadzonePercents

        private struct DeadzonePercents
        {
            public float yawpitch {  get; set; }
            public float roll { get; set; }
        }

        #endregion

        #region Declaration Section

        private readonly PlayerRotator _rotator;

        private readonly GazeBuffer _gazebuffer = new GazeBuffer();
        private readonly GazeBuffer _gazebuffer_roll = new GazeBuffer();

        private readonly PlayerRagdollUtil _ragdollUtil = new PlayerRagdollUtil();

        private float _capacitor_yawpitch = 0f;
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
        private DebugItem _lookorth = null;

        private float _deadzone_inner_dot = float.MinValue;
        private DebugItem _deadzone_inner = null;

        private float _deadzone_outer_dot = float.MinValue;
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

        private DebugItem _capa_yawpitch_vert = null;
        private DebugItem _capa_yawpitch_tick = null;
        private DebugItem _capa_roll_vert = null;
        private DebugItem _capa_roll_tick = null;
        private DebugItem _capa_text = null;

        #endregion

        #endregion

        public RotateToLook(PlayerRotator rotator)
        {
            _rotator = rotator;
        }

        public void Clear()
        {
            _capacitor_yawpitch = 0f;
            _capacitor_roll = 0f;
            _gazebuffer.Clear();
            _gazebuffer_roll.Clear();

            if (JetpackScript.ShowRotateToLook)
                ClearDebugVisuals();
        }

        public void Update(float elapsed_seconds)
        {
            if (!JetpackScript.ShouldRotateToLook)
                return;

            // Get body and head directions (also pos, velocity)
            var dirs = GetDirections();

            // Update gaze buffers, get their averaged directions
            var gaze = UpdateGazeBuffers(dirs);

            // Get percents inside dead zones
            var deadzones = GetDeadzonePercents(dirs, gaze);

            // Update the capacitors
            _capacitor_yawpitch = PullYawToLook2.UpdateCapacitor(_capacitor_yawpitch, gaze.yawpitch_direction, dirs.head_forward, gaze.yawpitch_confidence, deadzones.yawpitch, elapsed_seconds);
            _capacitor_roll = PullYawToLook2.UpdateCapacitor(_capacitor_roll, gaze.roll_direction, dirs.head_up, gaze.roll_confidence, deadzones.roll, elapsed_seconds);


            // once drawing confirms it's good, add in the actual turning


            if (JetpackScript.ShowRotateToLook)
            {
                PrepareForDraw();

                DrawYawPitch(dirs.body_forward, dirs.head_forward, deadzones.yawpitch, gaze.direction_yawpitch_offset, gaze.confidence_yawpitch_offset, gaze.direction_yawpitch_target, gaze.confidence_yawpitch_target, gaze.yawpitch_direction, gaze.yawpitch_confidence);
                DrawRoll(dirs.body_forward, dirs.body_up, dirs.head_up, deadzones.roll, gaze.roll_direction, gaze.roll_confidence);
                DrawCapacitors(deadzones.yawpitch, deadzones.roll, gaze.yawpitch_confidence, gaze.roll_confidence);

                //DrawCircles(pos, look);

                // text for final turn rates (yawpitch, roll)
                //DrawTurnRates();

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

            if (_lookorth != null)
            {
                _renderer.Remove(_lookorth);
                _lookorth = null;
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

            if (_capa_yawpitch_vert != null)
            {
                _renderer.Remove(_capa_yawpitch_vert);
                _capa_yawpitch_vert = null;
            }

            if (_capa_yawpitch_tick != null)
            {
                _renderer.Remove(_capa_yawpitch_tick);
                _capa_yawpitch_tick = null;
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

        private void DrawYawPitch(Vector3 body_forward, Vector3 look, float deadzone_percent, Vector3 direction_offset, float? confidence_offset, Vector3 direction_target, float? confidence_target, Vector3 direction_final, float? confidence_final)
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

            // Look Orth (color based on dead zone percent)
            //Color color = Color.Lerp(new Color(1, 1, 1, 0), Color.white, deadzone_percent);     // TODO: this semitransparency doesn't seem to work
            Color color = Color.Lerp(Color.white, Color.black, deadzone_percent);       // 0% is white (no deadzone)
            if (_lookorth == null)
                _lookorth = _renderer.AddLine_Basic(plane_point, head_pos + look * PLANE_DIST, LINE_THICKNESS, color);
            else
            {
                DebugRenderer3D.AdjustLinePositions(_lookorth, plane_point, head_pos + look * PLANE_DIST);
                DebugRenderer3D.AdjustColor(_lookorth, color);
            }

            // Draw dead zones as circles
            DrawYawPitch_DeadzoneCircle(ref _deadzone_inner, ref _deadzone_inner_dot, JetpackScript.YawToLook2_DeadZone_Full, plane_point, body_forward, PLANE_DIST, _renderer);
            DrawYawPitch_DeadzoneCircle(ref _deadzone_outer, ref _deadzone_outer_dot, JetpackScript.YawToLook2_DeadZone_Start, plane_point, body_forward, PLANE_DIST, _renderer);

            // Gaze buffer results
            DrawGazeOffsets(PLANE_DIST, INNER_DIST, head_pos, body_forward, direction_offset, confidence_offset);
            DrawGazeTargets(PLANE_DIST, INNER_DIST, head_pos, body_forward, direction_target, confidence_target);

            // Final
            // NOTE: using offsets list
            if (confidence_final != null)
            {
                color = Color.Lerp(UtilityColor.FromHex(FINALLINE_COLOR_MAX), UtilityColor.FromHex(FINALLINE_COLOR_MIN), confidence_final.Value);
                DrawGazeOffsets_AddLine(body_forward, head_pos, PLANE_DIST, INNER_DIST, color, direction: direction_final);
            }
        }
        private static void DrawYawPitch_DeadzoneCircle(ref DebugItem debug_item, ref float basedon_dot, float dead_zone, Vector3 origin, Vector3 normal, float plane_dist, DebugRenderer3D renderer)
        {
            if (dead_zone.IsNearValue(1))
            {
                if (debug_item != null)
                    renderer.Remove(debug_item);        // this would only happen when dragging the slider to one.  so just remove it
                debug_item = null;
                basedon_dot = 1;
                return;
            }

            if (debug_item != null && !basedon_dot.IsNearValue(dead_zone))
            {
                renderer.Remove(debug_item);        // the value changed, need to recalculate radius
                debug_item = null;
            }

            if (debug_item == null)
            {
                float radius = plane_dist * Mathf.Tan(Math1D.Dot_to_Radians(dead_zone));
                basedon_dot = dead_zone;
                debug_item = renderer.AddCircle(origin, normal, radius, LINE_THICKNESS, Color.gray);
            }
            else
            {
                DebugRenderer3D.AdjustCirclePosition(debug_item, origin, normal);
            }
        }

        private void DrawGazeOffsets(float plane_dist, float inner_dist, Vector3 head_pos, Vector3 body_forward, Vector3 direction, float? confidence)
        {
            if (_gazebuffer._offset.Count < JetpackScript.GazeBuffer_MaxCount * 0.8 && confidence == null)
                return;

            Color color_sample = UtilityColor.FromHex("3F4F6");     // blue
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
                    if (bucket.Count < JetpackScript.GazeBuffer_MaxCount * 0.8 && confidence == null)
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

        private void DrawRoll(Vector3 body_forward, Vector3 body_up, Vector3 head_up, float deadzone_percent, Vector3 direction_roll, float? confidence_roll)
        {
            const float LINE_LEN = 0.35f;

            EnsureDebugActive();

            Vector3 up = Player.local.head.transform.up;
            Vector3 right = Player.local.head.transform.right;
            Vector3 forward = Player.local.head.transform.forward;

            // start point
            Vector3 origin = Player.local.head.anchor.position +
                forward * 1.25f +
                right * -0.15f +
                up * 0.15f;

            // NOTE: head_up has been pulled into the plane where body_forward is the normal (see GetProjecteHeadUp)

            // the other vectors should be in the plane where body_forward is the normal.  so get a rotation that will put them
            // in a plane where head forward is the normal
            //Quaternion quat = Quaternion.FromToRotation(body_forward, forward);
            //Quaternion quat = Quaternion.identity;      // seeing what it looks like without

            //body_up = quat * body_up;
            //head_up = quat * head_up;
            //direction_roll = quat * direction_roll;

            // body_up
            if (_body_up == null)
                _body_up = _renderer.AddLine_Basic(origin, origin + body_up * LINE_LEN, LINE_THICKNESS, Color.cyan);
            else
                DebugRenderer3D.AdjustLinePositions(_body_up, origin, origin + body_up * LINE_LEN);

            // head_up
            if (_head_up == null)
                _head_up = _renderer.AddLine_Basic(origin, origin + head_up * LINE_LEN, LINE_THICKNESS, Color.white);
            else
                DebugRenderer3D.AdjustLinePositions(_head_up, origin, origin + head_up * LINE_LEN);

            // dead zones
            DrawRoll_DeadzoneLines(ref _deadzone_inner_left, ref _deadzone_inner_right, JetpackScript.RotToLook_DeadZone_Roll_Full, origin, body_up, forward, LINE_LEN, _renderer);
            DrawRoll_DeadzoneLines(ref _deadzone_outer_left, ref _deadzone_outer_right, JetpackScript.RotToLook_DeadZone_Roll_Start, origin, body_up, forward, LINE_LEN, _renderer);

            // buffer samples

            // sample[0] has quat that needs to be multiplied by body_up.  then multiply by this function's quat to rotate onto the radar display's plane
            DrawGazeRoll(origin, direction_roll, confidence_roll, body_up, LINE_LEN);
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
        private void DrawGazeRoll(Vector3 origin, Vector3 direction, float? confidence, Vector3 body_up, float line_len)
        {
            if (_gazebuffer_roll._offset.Count < JetpackScript.GazeBuffer_MaxCount * 0.8 && confidence == null)
                return;

            Color color_sample = UtilityColor.FromHex("426B5E");        // green, almost cyan
            //Color color_derived = UtilityColor.FromHex("2AF5B4");

            // first rotation is relative to body_up
            Vector3 line_0 = _gazebuffer_roll._offset[0].Quaternion * body_up * line_len;
            Vector3 line_N = _gazebuffer_roll._offset[_gazebuffer_roll._offset.Count - 1].Quaternion * body_up * line_len;

            // then rotate using quat, which makes it perpendicular to normal
            //line_0 = quat * line_0;
            //line_N = quat * line_N;

            DrawRoll_AddLine(origin, origin + line_0, color_sample);
            DrawRoll_AddLine(origin, origin + line_N, color_sample);

            // Final
            if (confidence != null)
            {
                Color color = Color.Lerp(UtilityColor.FromHex(FINALLINE_COLOR_MAX), UtilityColor.FromHex(FINALLINE_COLOR_MIN), confidence.Value);
                DrawRoll_AddLine(origin, origin + direction * line_len, color);
            }
        }

        private void DrawCapacitors(float deadzone_yawpitch_percent, float deadzone_roll_percent, float? confidence_yawpitch, float? confidence_roll)
        {
            const float HEIGHT = 0.32f;
            const float TICK_HALF_WIDTH = 0.04f;

            EnsureDebugActive();

            Vector3 up = Player.local.head.transform.up;
            Vector3 right = Player.local.head.transform.right;

            Vector3 bottom = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                right * -0.4f +
                up * -0.4f;

            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.7f +
                right * -0.4f +
                up * (-0.4f + HEIGHT / 2f);

            Vector3 leftright_offset = right * (TICK_HALF_WIDTH * 1.5f);

            DrawCapacitors_Single(ref _capa_yawpitch_vert, ref _capa_yawpitch_tick, _capacitor_yawpitch, _renderer, bottom - leftright_offset, up, right, HEIGHT, TICK_HALF_WIDTH, UtilityColor.FromHex("4E2BCC"));
            DrawCapacitors_Single(ref _capa_roll_vert, ref _capa_roll_tick, _capacitor_roll, _renderer, bottom + leftright_offset, up, right, HEIGHT, TICK_HALF_WIDTH, UtilityColor.FromHex("1DDB9F"));

            //string text = $"capacitor: {_capacitor.ToStringSignificantDigits(2)}";

            var lines = new[]
            {
                "** capacitors **",
                $"yaw/pitch: {_capacitor_yawpitch.ToStringSignificantDigits(2)}",
                $"roll: {_capacitor_roll.ToStringSignificantDigits(2)}",
                "** confidence **",
                $"yaw/pitch: {confidence_yawpitch?.ToStringSignificantDigits(2) ?? "--"}",
                $"roll: {confidence_roll?.ToStringSignificantDigits(2) ?? "--"}",
                "** deadzone % **",
                $"yaw/pitch: {deadzone_yawpitch_percent.ToStringSignificantDigits(2)}",
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

        private void DrawTurnRates()
        {
            EnsureDebugActive();



        }

        #endregion
        #region Private Methods

        private Directions GetDirections()
        {
            // initial values (world coords)
            var (body_forward, body_up) = _ragdollUtil.GetRagdollForwardUp();
            Vector3 head_forward = Player.local.head.transform.forward;
            Vector3 head_up = Player.local.head.transform.up;

            // trimmed (world coords)
            TrimForwardUp(ref body_forward, ref body_up);

            // ---- FOR ROLL ----
            var (head_up2, head_forward2) = GetProjecteHeadUp(head_up, head_forward, body_forward);      // pull head into the plane of body up and right

            // rotated so that forward is Z, up is Y
            var (body_up3, head_up3, quat3) = RotateUps(body_forward, body_up, head_up2);
            //Vector3 body_forward3 = quat3 * body_forward;
            //Vector3 head_forward3 = quat3 * head_forward2;

            return new Directions
            {
                pos = Player.local.head.anchor.position,
                velocity = Player.local.locomotion.physicBody.velocity,

                body_forward = body_forward,
                body_up = body_up,
                head_forward = head_forward,
                head_up = head_up,

                //localroll_body_forward = body_forward3,
                localroll_body_up = body_up3,
                //localroll_head_forward = head_forward3,
                localroll_head_up = head_up3,

                quat_tolocalroll = quat3,
                quat_fromlocalroll = Quaternion.Inverse(quat3),
            };
        }

        private GazeResults UpdateGazeBuffers(Directions dirs)
        {
            // Populate gaze buffers
            _gazebuffer.AddSample_Offset(dirs.head_forward, dirs.body_forward);
            _gazebuffer.AddSample_Target(dirs.pos, dirs.head_forward, dirs.velocity.magnitude);
            _gazebuffer_roll.AddSample_Offset(dirs.localroll_head_up, dirs.localroll_body_up);

            // Get gazed vectors
            float? confidence_yawpitch_offset = null;
            if (_gazebuffer.TryGetDominantDirection_Offset(out Vector3 direction_yawpitch_offset, out float confidence, dirs.body_forward))
                confidence_yawpitch_offset = confidence;

            float? confidence_yawpitch_target = null;
            if (_gazebuffer.TryGetDominantDirection_Target_Debug(out Vector3 direction_yawpitch_target, out confidence, out float sphere_radius, out Vector3 sphere_origin, dirs.pos))
                confidence_yawpitch_target = confidence;

            var yawpitch = PullYawToLook2.GetFinalConfidence(direction_yawpitch_offset, confidence_yawpitch_offset, direction_yawpitch_target, confidence_yawpitch_target);

            float? confidence_roll = null;
            if (_gazebuffer_roll.TryGetDominantDirection_Offset(out Vector3 direction_roll_local, out confidence, dirs.localroll_body_up))
                confidence_roll = confidence;

            Vector3 direction_roll = dirs.quat_fromlocalroll * direction_roll_local;

            return new GazeResults
            {
                yawpitch_confidence = yawpitch.confidence,
                yawpitch_direction = yawpitch.direction,
                roll_confidence = confidence_roll,
                roll_direction = direction_roll,

                confidence_yawpitch_offset = confidence_yawpitch_offset,
                direction_yawpitch_offset = direction_yawpitch_offset,
                confidence_yawpitch_target = confidence_yawpitch_target,
                direction_yawpitch_target = direction_yawpitch_target,
            };
        }

        private static DeadzonePercents GetDeadzonePercents(Directions dirs, GazeResults gaze)
        {
            return new DeadzonePercents
            {
                yawpitch = gaze.yawpitch_confidence != null ?
                    PullYawToLook2.GetDeadZonePercent(dirs.body_forward, gaze.yawpitch_direction, JetpackScript.YawToLook2_DeadZone_Full, JetpackScript.YawToLook2_DeadZone_Start) :
                    0,

                roll = gaze.roll_confidence != null ?
                    PullYawToLook2.GetDeadZonePercent(dirs.body_up, gaze.roll_direction, JetpackScript.RotToLook_DeadZone_Roll_Full, JetpackScript.RotToLook_DeadZone_Roll_Start) :
                    0,
            };
        }

        private void TrimForwardUp(ref Vector3 forward, ref Vector3 up)
        {
            // Yaw Trim
            if (!JetpackScript.YawToLook2_ForwardTrimDegrees_Yaw.IsNearZero())
            {
                Quaternion yaw = Quaternion.AngleAxis(JetpackScript.YawToLook2_ForwardTrimDegrees_Yaw, up);

                // Apply Yaw Rotation to both forward and up
                forward = yaw * forward;
                up = yaw * up;
            }

            // Pitch Trim
            if (!JetpackScript.RotToLook_ForwardTrimDegrees_Pitch.IsNearZero())
            {
                Vector3 right = Vector3.Cross(forward, up);
                Quaternion pitch = Quaternion.AngleAxis(JetpackScript.RotToLook_ForwardTrimDegrees_Pitch, right);

                // Apply Pitch Rotation to both forward and up
                forward = pitch * forward;
                up = pitch * up;
            }
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
    }
}
