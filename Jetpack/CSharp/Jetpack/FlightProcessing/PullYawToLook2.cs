using Jetpack.InputWatchers;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.FlightProcessing
{
    /// <summary>
    /// While flying, this will slowly rotate the player's body toward look direction
    /// </summary>
    public class PullYawToLook2
    {
        #region Declaration Section

        private readonly ITriangle _xzplane = new Triangle(new Vector3(1, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 1));

        private readonly PlayerRotator _rotator;

        // NOTE: both offset and target are used in the same instance of gazebuffer, simply because it
        // supports different lists at the same time.  if roll is wanted, use another instance and pass
        // head up to its offset buffer
        private readonly GazeBuffer _gazeBuffer = new GazeBuffer();

        private readonly PlayerRagdollUtil _ragdollUtil = new PlayerRagdollUtil();

        private Quaternion _trim = Quaternion.identity;
        private float _trim_basedon = 0;

        private float _capacitor = 0f;

        #region debug drawing vars

        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private DebugRenderer3D _renderer = null;

        private DebugItem _forwardline = null;
        private DebugItem _lookline = null;

        private DebugItem _offset_back = null;
        private DebugItem _offset_fore = null;

        private DebugItem _target_back = null;
        private DebugItem _target_fore = null;

        private DebugItem _final_back = null;
        private DebugItem _final_fore = null;

        private DebugItem _hud_text = null;

        private DebugItem _deadzone_inner_l = null;
        private DebugItem _deadzone_inner_r = null;
        private DebugItem _deadzone_outer_l = null;
        private DebugItem _deadzone_outer_r = null;

        private DebugItem _capa_vert = null;
        private DebugItem _capa_tick = null;
        private DebugItem _capa_text = null;

        private DebugItem _turnrate = null;

        #endregion

        #endregion

        public PullYawToLook2(PlayerRotator rotator)
        {
            _rotator = rotator;
        }

        // This should be called when entering/leaving flight
        public void Clear()
        {
            _capacitor = 0f;
            _gazeBuffer.Clear();

            if (JetpackScript.ShowPullYawToLook2)
                ClearDebugVisuals();
        }

        // This should only be called while in flight
        public void Update(float elapsed_seconds)
        {
            if (!JetpackScript.ShouldYawToLook2)
                return;

            // Get some values needed by the rest of the function
            Vector3 pos = Player.local.head.anchor.position;
            Vector3 look = Player.local.head.transform.forward.GetProjectedVector(_xzplane).normalized;
            Vector3 forward = _ragdollUtil.GetRagdollForwardUp().forward.GetProjectedVector(_xzplane).normalized;
            Vector3 velocity = Player.local.locomotion.physicBody.velocity;

            forward = TrimForward(forward);

            // Populate gaze buffer
            _gazeBuffer.AddSample_Offset(look, forward);
            _gazeBuffer.AddSample_Target(pos, look, velocity.magnitude);

            // Get gazed vectors
            float? confidence_offset = null;
            if (_gazeBuffer.TryGetDominantDirection_Offset(out Vector3 direction_offset, out float confidence, forward))
                confidence_offset = confidence;

            float? confidence_target = null;
            if (_gazeBuffer.TryGetDominantDirection_Target_Debug(out Vector3 direction_target, out confidence, out float sphere_radius, out Vector3 sphere_origin, pos))
                confidence_target = confidence;

            // Get a combined vector
            var final = GetFinalConfidence(direction_offset, confidence_offset, direction_target, confidence_target);

            // Get percent inside dead zone
            float deadzone_percent = final.confidence != null ?
                GetDeadZonePercent(forward, final.direction, JetpackScript.YawToLook2_DeadZone_Full, JetpackScript.YawToLook2_DeadZone_Start) :
                0;

            // Update the capacitor
            _capacitor = UpdateCapacitor(_capacitor, final.direction, look, final.confidence, deadzone_percent, elapsed_seconds);

            // Turn Player
            var turn_rate = GetTurnRate(look, forward, final.direction, final.confidence, deadzone_percent, _capacitor);
            if (turn_rate != null)
                TurnPlayer(turn_rate.Value.axis, turn_rate.Value.degrees_per_sec, elapsed_seconds);

            if (JetpackScript.ShowPullYawToLook2)
            {
                DrawForwardLook(look, forward, direction_offset, confidence_offset, direction_target, confidence_target, final.direction, final.confidence);
                DrawDeadZones(forward);
                DrawCapacitor();
                DrawTurnRate(turn_rate);
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

            // hud
            if (_forwardline != null)
            {
                _renderer.Remove(_forwardline);
                _forwardline = null;
            }

            if (_lookline != null)
            {
                _renderer.Remove(_lookline);
                _lookline = null;
            }

            if (_offset_back != null)
            {
                _renderer.Remove(_offset_back);
                _offset_back = null;
            }

            if (_offset_fore != null)
            {
                _renderer.Remove(_offset_fore);
                _offset_fore = null;
            }

            if (_target_back != null)
            {
                _renderer.Remove(_target_back);
                _target_back = null;
            }

            if (_target_fore != null)
            {
                _renderer.Remove(_target_fore);
                _target_fore = null;
            }

            if (_final_back != null)
            {
                _renderer.Remove(_final_back);
                _final_back = null;
            }

            if (_final_fore != null)
            {
                _renderer.Remove(_final_fore);
                _final_fore = null;
            }

            if (_hud_text != null)
            {
                _renderer.Remove(_hud_text);
                _hud_text = null;
            }

            // dead zone
            if (_deadzone_inner_l != null)
            {
                _renderer.Remove(_deadzone_inner_l);
                _deadzone_inner_l = null;
            }

            if (_deadzone_inner_r != null)
            {
                _renderer.Remove(_deadzone_inner_r);
                _deadzone_inner_r = null;
            }

            if (_deadzone_outer_l != null)
            {
                _renderer.Remove(_deadzone_outer_l);
                _deadzone_outer_l = null;
            }

            if (_deadzone_outer_r != null)
            {
                _renderer.Remove(_deadzone_outer_r);
                _deadzone_outer_r = null;
            }

            // capacitor
            if (_capa_vert != null)
            {
                _renderer.Remove(_capa_vert);
                _capa_vert = null;
            }

            if (_capa_tick != null)
            {
                _renderer.Remove(_capa_tick);
                _capa_tick = null;
            }

            if (_capa_text != null)
            {
                _renderer.Remove(_capa_text);
                _capa_text = null;
            }

            // turn rate
            if (_turnrate != null)
            {
                _renderer.Remove(_turnrate);
                _turnrate = null;
            }

            _renderer = null;
        }

        private void DrawForwardLook(Vector3 look, Vector3 forward, Vector3 direction_offset, float? confidence_offset, Vector3 direction_target, float? confidence_target, Vector3 direction_final, float? confidence_final)
        {
            const float LINE_LEN = 2;

            EnsureDebugActive();

            // go partially up the body
            Vector3 head_pos = Player.local.head.anchor.position;
            Vector3 foot_pos = Math3D.GetAverage(Player.local.footLeft.ragdollFoot.root.position, Player.local.footRight.ragdollFoot.root.position);        // Player.local.transform.position is the room level origin
            Vector3 origin = foot_pos + (head_pos - foot_pos) * 0.8f;

            // don't want to rotate this one, keep it in the xz plane even if the player is oriented funny

            // draw forward straight out
            if (_forwardline == null)
                _forwardline = _renderer.AddLine_Basic(origin, origin + forward * LINE_LEN, LINE_THICKNESS, Color.cyan);
            else
                DebugRenderer3D.AdjustLinePositions(_forwardline, origin, origin + forward * LINE_LEN);

            // draw look along look
            if (_lookline == null)
                _lookline = _renderer.AddLine_Basic(origin, origin + look * LINE_LEN, LINE_THICKNESS, Color.white);
            else
                DebugRenderer3D.AdjustLinePositions(_lookline, origin, origin + look * LINE_LEN);

            // gaze results
            DrawForwardLook_Gaze(ref _offset_back, ref _offset_fore, _renderer, origin, direction_offset, confidence_offset, UtilityColor.FromHex("3F4F6B"), UtilityColor.FromHex("2176F4"));        // blue
            DrawForwardLook_Gaze(ref _target_back, ref _target_fore, _renderer, origin, direction_target, confidence_target, UtilityColor.FromHex("58486B"), UtilityColor.FromHex("9126F5"));        // purple
            DrawForwardLook_Gaze(ref _final_back, ref _final_fore, _renderer, origin, direction_final, confidence_final, UtilityColor.FromHex("857754"), UtilityColor.FromHex("F5BA28"));        // orange

            // text pos
            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                //Player.local.head.transform.right * -0.75f +
                Player.local.head.transform.up * -0.33f;

            // fill out report
            var text_list = new List<string>();
            text_list.Add("** Confidence **");
            text_list.Add($"offset: {confidence_offset?.ToStringSignificantDigits(2) ?? "--"}");
            text_list.Add($"target: {confidence_target?.ToStringSignificantDigits(2) ?? "--"}");
            text_list.Add($"final: {confidence_final?.ToStringSignificantDigits(2) ?? "--"}");

            string text = string.Join(Environment.NewLine, text_list);

            // draw
            if (_hud_text == null)
                _hud_text = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, Color.cyan, Color.black, TEXT_HEIGHT * text_list.Count);

            _hud_text.Object.transform.position = text_pos;
            _hud_text.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_hud_text, new_text: text);
        }
        private static void DrawForwardLook_Gaze(ref DebugItem back, ref DebugItem fore, DebugRenderer3D renderer, Vector3 origin, Vector3 direction, float? confidence, Color color_back, Color color_fore)
        {
            const float LINE_LEN = 2;

            if (confidence == null)
            {
                // Remove
                if (back != null)
                    renderer.Remove(back);
                back = null;

                if (fore != null)
                    renderer.Remove(fore);
                fore = null;

                return;
            }

            // Back
            if (back == null)
                back = renderer.AddLine_Basic(origin, origin + direction * LINE_LEN, LINE_THICKNESS / 2, color_back);
            else
                DebugRenderer3D.AdjustLinePositions(back, origin, origin + direction * LINE_LEN);

            // Fore
            if (fore == null)
                fore = renderer.AddLine_Basic(origin, origin + direction * LINE_LEN * confidence.Value, LINE_THICKNESS / 2, color_fore);
            else
                DebugRenderer3D.AdjustLinePositions(fore, origin, origin + direction * LINE_LEN * confidence.Value);
        }

        private void DrawDeadZones(Vector3 forward)
        {
            const float LINE_LEN = 1.5f;

            EnsureDebugActive();

            // go partially up the body, but below DrawForwardLook()
            Vector3 head_pos = Player.local.head.anchor.position;
            Vector3 foot_pos = Math3D.GetAverage(Player.local.footLeft.ragdollFoot.root.position, Player.local.footRight.ragdollFoot.root.position);        // Player.local.transform.position is the room level origin
            Vector3 origin = foot_pos + (head_pos - foot_pos) * 0.75f;

            float angle_full = Math1D.Dot_to_Degrees(JetpackScript.YawToLook2_DeadZone_Full);
            float angle_start = Math1D.Dot_to_Degrees(JetpackScript.YawToLook2_DeadZone_Start);

            // don't want to rotate this one, keep it in the xz plane even if the player is oriented funny

            // Inner Left
            Quaternion quat = Quaternion.AngleAxis(angle_full, Vector3.up);
            Vector3 line = quat * forward;
            if (_deadzone_inner_l == null)
                _deadzone_inner_l = _renderer.AddLine_Basic(origin, origin + line * LINE_LEN, LINE_THICKNESS, Color.gray);
            else
                DebugRenderer3D.AdjustLinePositions(_deadzone_inner_l, origin, origin + line * LINE_LEN);

            // Inner Right
            quat = Quaternion.AngleAxis(-angle_full, Vector3.up);
            line = quat * forward;
            if (_deadzone_inner_r == null)
                _deadzone_inner_r = _renderer.AddLine_Basic(origin, origin + line * LINE_LEN, LINE_THICKNESS, Color.gray);
            else
                DebugRenderer3D.AdjustLinePositions(_deadzone_inner_r, origin, origin + line * LINE_LEN);

            // Outer Left
            quat = Quaternion.AngleAxis(angle_start, Vector3.up);
            line = quat * forward;
            if (_deadzone_outer_l == null)
                _deadzone_outer_l = _renderer.AddLine_Basic(origin, origin + line * LINE_LEN, LINE_THICKNESS, Color.gray);
            else
                DebugRenderer3D.AdjustLinePositions(_deadzone_outer_l, origin, origin + line * LINE_LEN);

            // Outer Right
            quat = Quaternion.AngleAxis(-angle_start, Vector3.up);
            line = quat * forward;
            if (_deadzone_outer_r == null)
                _deadzone_outer_r = _renderer.AddLine_Basic(origin, origin + line * LINE_LEN, LINE_THICKNESS, Color.gray);
            else
                DebugRenderer3D.AdjustLinePositions(_deadzone_outer_r, origin, origin + line * LINE_LEN);
        }

        private void DrawCapacitor()
        {
            const float HEIGHT = 0.35f;
            const float TICK_HALF_WIDTH = 0.06f;

            EnsureDebugActive();

            Vector3 up = Player.local.head.transform.up;
            Vector3 right = Player.local.head.transform.right;

            Vector3 bottom = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                right * -0.4f +
                up * -0.2f;

            // vertical line
            if (_capa_vert == null)
                _capa_vert = _renderer.AddLine_Basic(bottom, bottom + up * HEIGHT, LINE_THICKNESS, Color.cyan);

            DebugRenderer3D.AdjustLinePositions(_capa_vert, bottom, bottom + up * HEIGHT);

            // horizontal tick line that is capacitor value
            Vector3 tick_mid = bottom + up * (_capacitor * HEIGHT);

            if (_capa_tick == null)
                _capa_tick = _renderer.AddLine_Basic(tick_mid - right * TICK_HALF_WIDTH, tick_mid + right * TICK_HALF_WIDTH, LINE_THICKNESS, Color.yellow);

            DebugRenderer3D.AdjustLinePositions(_capa_tick, tick_mid - right * TICK_HALF_WIDTH, tick_mid + right * TICK_HALF_WIDTH);

            // text of capacitor value below
            Vector3 text_pos = bottom + up * -0.1f;

            string text = $"capacitor: {_capacitor.ToStringSignificantDigits(2)}";

            if (_capa_text == null)
                _capa_text = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, Color.cyan, Color.black, TEXT_HEIGHT);

            _capa_text.Object.transform.position = text_pos;
            _capa_text.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_capa_text, new_text: text);
        }

        private void DrawTurnRate((Vector3 axis, float degrees_per_sec)? turn_rate)
        {
            EnsureDebugActive();

            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                Player.local.head.transform.right * 0.35f +
                Player.local.head.transform.up * -0.25f;

            string deg_text = turn_rate != null ?
                Mathf.Round(turn_rate.Value.degrees_per_sec).ToString() :
                "--";

            string text = $"turn rate: {deg_text}";

            if (_turnrate == null)
                _turnrate = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, Color.black, Color.cyan, TEXT_HEIGHT);

            _turnrate.Object.transform.position = text_pos;
            _turnrate.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_turnrate, new_text: text);
        }

        #endregion
        #region Private Methods - merge confidence

        internal static (Vector3 direction, float? confidence) GetFinalConfidence(Vector3 direction_offset, float? confidence_offset, Vector3 direction_target, float? confidence_target)
        {
            const float RETURN_CONFIDENCE_THRESHOLD = 0.7f;
            float THRESHOLD_OFFSET = JetpackScript.GazeBuffer_GazeConfidence_Offset;
            float THRESHOLD_TARGET = JetpackScript.GazeBuffer_GazeConfidence_Target;

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
        #region Private Methods - capacitor

        internal static float UpdateCapacitor(float capacitor, Vector3 target, Vector3 look, float? confidence, float deadzone_percent, float elapsed_seconds)
        {
            float upper_dot = JetpackScript.YawToLook_Capacitor_UpperDot;
            float lower_dot = JetpackScript.YawToLook_Capacitor_LowerDot;
            float bottom_dot = JetpackScript.YawToLook_Capacitor_BottomDot;

            float discharge_speed = JetpackScript.YawToLook_Capacitor_DischargeSpeed;

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
                float chargeRate = JetpackScript.YawToLook_Capacitor_ChargeSpeed * Mathf.Pow(chargeFactor, JetpackScript.YawToLook_Capacitor_ChargePower);
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
                float decayRate = discharge_speed * Mathf.Pow(decayFactor, JetpackScript.YawToLook_Capacitor_DischargePower);

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
        #region Private Methods - turn

        // Version 1 doesn't bother with angular momentum

        // This calculates degrees per second.  It's up to the caller to multiply be elapsed time
        private static (Vector3 axis, float degrees_per_sec)? GetTurnRate(Vector3 look, Vector3 forward, Vector3 direction, float? confidence, float deadzone_percent, float capacitor)
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
            float speed = JetpackScript.YawToLook2_TurnRate * capacitor * (1 - deadzone_percent);
            if (rot_angle < 0)
                speed = -speed;     // shouldn't happen

            return (rot_axis, speed);
        }

        private void TurnPlayer(Vector3 axis, float degrees_per_sec, float elapsed_seconds)
        {
            // figure out a point to rotate around
            Vector3 head_pos = Player.local.head.anchor.position;
            Vector3 foot_pos = Math3D.GetAverage(Player.local.footLeft.ragdollFoot.root.position, Player.local.footRight.ragdollFoot.root.position);        // Player.local.transform.position is the room level origin
            Vector3 origin = foot_pos + (head_pos - foot_pos) * 0.5f;

            _rotator.RotateAround(origin, axis, degrees_per_sec, elapsed_seconds);
        }

        #endregion
        #region Private Methods

        private Vector3 TrimForward(Vector3 forward)
        {
            if (!_trim_basedon.IsNearValue(JetpackScript.YawToLook2_ForwardTrimDegrees_Yaw))
            {
                _trim_basedon = JetpackScript.YawToLook2_ForwardTrimDegrees_Yaw;
                _trim = Quaternion.AngleAxis(_trim_basedon, Vector3.up);        // in this yaw class, it's always xz
            }

            return _trim * forward;
        }

        internal static float GetDeadZonePercent(Vector3 forward, Vector3 direction, float dot_full, float dot_start)
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
    }
}
