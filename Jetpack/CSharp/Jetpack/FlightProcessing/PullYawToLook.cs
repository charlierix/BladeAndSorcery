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
    public class PullYawToLook
    {
        private readonly ITriangle _xzplane = new Triangle(new Vector3(1, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 1));

        private readonly List<Vector3> _ragdollforwards = new List<Vector3>();
        private readonly RagdollPart.Type[] _spine_parts = new[]
        {
            //RagdollPart.Type.Tail,        // this also points down a bit
            RagdollPart.Type.Torso,     // I think there are two parts labeled as torso
            //RagdollPart.Type.Neck,        // this one points downward a little
        };

        private readonly GazeBuffer _gazeBuffer = new GazeBuffer();

        private float _capacitor = 0f;

        DateTime _prevTick = DateTime.UtcNow;

        #region debug drawing vars

        private const bool SHOULD_DRAW = true;

        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private DebugRenderer3D _renderer = null;

        private DebugItem _diag_forwardline = null;
        private DebugItem _diag_lookline = null;
        private DebugItem _diag_text = null;

        private DebugItem _hud_forwardline = null;
        private DebugItem _hud_lookline = null;

        private DebugItem _capa_vert = null;
        private DebugItem _capa_tick = null;
        private DebugItem _capa_text = null;

        private List<DebugItem> _ragdoll = new List<DebugItem>();

        #endregion

        // This should be called when entering/leaving flight
        public void Clear()
        {
            _capacitor = 0f;
            _gazeBuffer.Clear();
            _prevTick = DateTime.UtcNow;

            if (SHOULD_DRAW)
                ClearDebugVisuals();
        }

        // This should only be called while in flight
        public void Update()
        {
            DateTime now = DateTime.UtcNow;
            float elapsedSeconds = (float)Math1D.Clamp((now - _prevTick).TotalSeconds, 0, 0.25);
            _prevTick = now;

            if (!JetpackScript.ShouldYawToLook)
                return;

            // Get look and forward snapped to the xz plane
            Vector3 look = Player.local.head.transform.forward.GetProjectedVector(_xzplane).normalized;

            //Vector3 forward = Player.local.transform.forward.GetProjectedVector(_xzplane).normalized;     // relative to room, irl turning will move this around
            //Vector3 forward = Player.local.waist.ikAnchor.forward.GetProjectedVector(_xzplane).normalized;      // same as prev
            Vector3 forward = GetRagdollForward().GetProjectedVector(_xzplane).normalized;

            // Update the capacitor
            //UpdateCapacitor2(look, forward, elapsedSeconds);

            float upper_dot = JetpackScript.YawToLook_Capacitor_UpperDot;
            float lower_dot = JetpackScript.YawToLook_Capacitor_LowerDot;
            float bottom_dot = JetpackScript.YawToLook_Capacitor_BottomDot;

            _capacitor = UpdateCapacitor3(_capacitor, look, forward, elapsedSeconds, upper_dot, lower_dot, bottom_dot);

            if (SHOULD_DRAW)
            {
                DrawForwardLookDiagram(look, forward);
                DrawForwardLookHUD(look, forward);
                DrawCapacitor();
                DrawRagdoll();
            }





        }

        #region Private Methods - Debug Drawing

        private void EnsureDebugActive()
        {
            if (_renderer == null)
                _renderer = DebugRenderer3D.GetOrAddDebugRenderer3D();
        }

        private void ClearDebugVisuals()
        {
            if (_renderer == null)
                return;

            // diagram
            if (_diag_forwardline != null)
            {
                _renderer.Remove(_diag_forwardline);
                _diag_forwardline = null;
            }

            if (_diag_lookline != null)
            {
                _renderer.Remove(_diag_lookline);
                _diag_lookline = null;
            }

            if (_diag_text != null)
            {
                _renderer.Remove(_diag_text);
                _diag_text = null;
            }

            // hud
            if (_hud_forwardline != null)
            {
                _renderer.Remove(_hud_forwardline);
                _hud_forwardline = null;
            }

            if (_hud_lookline != null)
            {
                _renderer.Remove(_hud_lookline);
                _hud_lookline = null;
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

            // ragdoll
            foreach (DebugItem part in _ragdoll)
                _renderer.Remove(part);

            _ragdoll.Clear();

            _renderer = null;
        }

        private void DrawForwardLookDiagram(Vector3 look, Vector3 forward)
        {
            const float LINE_LEN = 0.35f;

            EnsureDebugActive();

            Vector3 up = Player.local.head.transform.up;
            Vector3 right = Player.local.head.transform.right;

            // start point
            Vector3 origin = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                right * -0.4f +
                up * 0.3f;

            // forward is straight up
            if (_diag_forwardline == null)
                _diag_forwardline = _renderer.AddLine_Basic(origin, origin + up * LINE_LEN, LINE_THICKNESS, Color.cyan);

            DebugRenderer3D.AdjustLinePositions(_diag_forwardline, origin, origin + up * LINE_LEN);

            // look is whereever it is (relative to forward)
            float angle = Vector3.SignedAngle(forward, look, Vector3.Cross(forward, look));
            Vector3 axis = Vector3.Cross(right, up);

            Quaternion quat = Quaternion.AngleAxis(angle, axis);

            if (_diag_lookline == null)
                _diag_lookline = _renderer.AddLine_Basic(origin, origin + quat * up * LINE_LEN, LINE_THICKNESS, Color.yellow);

            DebugRenderer3D.AdjustLinePositions(_diag_lookline, origin, origin + quat * up * LINE_LEN);

            // text of dot product below
            Vector3 text_pos = origin + up * -0.1f;

            string text = $"dot: {Vector3.Dot(look, forward).ToStringSignificantDigits(2)}";

            if (_diag_text == null)
                _diag_text = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, Color.cyan, Color.black, TEXT_HEIGHT);

            _diag_text.Object.transform.position = text_pos;
            _diag_text.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_diag_text, new_text: text);
        }

        private void DrawForwardLookHUD(Vector3 look, Vector3 forward)
        {
            const float LINE_LEN = 2;

            // go partially up the body
            Vector3 head_pos = Player.local.head.anchor.position;
            Vector3 foot_pos = Math3D.GetAverage(Player.local.footLeft.ragdollFoot.root.position, Player.local.footRight.ragdollFoot.root.position);        // Player.local.transform.position is the room level origin
            Vector3 origin = foot_pos + (head_pos - foot_pos) * 0.8f;

            // don't want to rotate this one, keep it in the xz plane even if the player is oriented funny

            // draw forward straight out
            if (_hud_forwardline == null)
                _hud_forwardline = _renderer.AddLine_Basic(origin, origin + forward * LINE_LEN, LINE_THICKNESS, Color.cyan);

            DebugRenderer3D.AdjustLinePositions(_hud_forwardline, origin, origin + forward * LINE_LEN);

            // draw look along look
            if (_hud_lookline == null)
                _hud_lookline = _renderer.AddLine_Basic(origin, origin + look * LINE_LEN, LINE_THICKNESS, Color.yellow);

            DebugRenderer3D.AdjustLinePositions(_hud_lookline, origin, origin + look * LINE_LEN);
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
                up * -0.3f;

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

        private void DrawRagdoll()
        {
            EnsureDebugActive();

            foreach (DebugItem part in _ragdoll)
                _renderer.Remove(part);

            _ragdoll.Clear();


            //SpellCaster caster;
            //caster.magicSource.

            //var test1 = Player.local.globalOffsetTransform.position;

            //var test = Player.local.creature.morphology;        // definitions, not realtime data


            var ragdoll = Player.currentCreature.ragdoll;

            //var foot1 = Player.currentCreature.footLeft;
            //var foot2 = Player.local.footLeft.ragdollFoot;
            //var foot3 = ragdoll.ik.footLeftTarget;

            Vector3 avg_forward = Vector3.zero;

            foreach (var part in ragdoll.parts)
            {
                if (!part.type.In(_spine_parts))
                    continue;

                //part.root

                //part.transform
                //part.meshBone


                //_ragdoll.Add(_renderer.AddDot(part.transform.position, DOT_SIZE, Color.white));       // this causes errors with colliders.  it must be getting placed in scene before the collider is removed
                _ragdoll.Add(_renderer.AddAxisLines(part.transform, 0.2f, LINE_THICKNESS));
            }
        }

        #endregion
        #region Private Methods

        // Returns average of the spine part's forward.  This works fairly well, but fails when one hand is in front of the
        // player and the other is behind.  The spine kind of takes the average
        private Vector3 GetRagdollForward()
        {
            var ragdoll = Player.currentCreature.ragdoll;

            _ragdollforwards.Clear();

            foreach (var part in ragdoll.parts)
                if (part.type.In(_spine_parts))
                    _ragdollforwards.Add(part.transform.forward);

            return Math3D.GetAverage(_ragdollforwards);
        }

        /// <summary>
        /// Updates the charge state of the capacitor based on the alignment between look and forward vectors.
        /// Uses a power function for charge/discharge to create a slow-to-start, fast-to-fill behavior.
        /// </summary>
        /// <param name="look">Normalized direction vector the user is looking</param>
        /// <param name="forward">Normalized direction vector of the object's forward direction</param>
        /// <param name="elapsedSeconds">Time since last update in seconds</param>
        private void UpdateCapacitor1(Vector3 look, Vector3 forward, float elapsedSeconds)
        {
            // Constants for tuning the capacitor behavior
            const float THRESHOLD = 0.95f;       // Minimum dot product to consider direction consistent
            const float CHARGE_SPEED = 5.0f;     // Base charge rate multiplier
            const float CHARGE_POWER = 2.0f;     // Exponent for charge acceleration
            const float DECAY_SPEED = 3.0f;      // Base discharge rate multiplier
            const float DECAY_POWER = 1.5f;      // Exponent for discharge rate

            // Calculate alignment between look and forward directions
            float dot = Vector3.Dot(look, forward);

            if (dot > THRESHOLD)
            {
                // When consistent, charge capacitor with power-based acceleration
                float chargeFactor = dot - THRESHOLD;
                float chargeRate = CHARGE_SPEED * Mathf.Pow(chargeFactor, CHARGE_POWER);
                _capacitor += chargeRate * (float)elapsedSeconds;
            }
            else
            {
                // When inconsistent, discharge with power-based decay
                float decayFactor = 1 - dot;
                float decayRate = DECAY_SPEED * Mathf.Pow(decayFactor, DECAY_POWER);
                _capacitor -= decayRate * (float)elapsedSeconds;
            }

            // Ensure capacitor stays within valid range
            _capacitor = Mathf.Clamp(_capacitor, 0f, 1f);
        }
        /// <summary>
        /// Updates the charge state of the capacitor with a neutral window between thresholds.
        /// </summary>
        private void UpdateCapacitor2(Vector3 look, Vector3 forward, double elapsedSeconds)
        {
            float upper_dot = JetpackScript.YawToLook_Capacitor_UpperDot;
            float lower_dot = JetpackScript.YawToLook_Capacitor_LowerDot;
            float bottom_dot = JetpackScript.YawToLook_Capacitor_BottomDot;

            // Calculate alignment between look and forward directions
            float dot = Vector3.Dot(look, forward);

            // Determine which region we're in
            if (dot > upper_dot)
            {
                // CHARGE REGION: dot > upper threshold
                // Normalize to 0-1 range based on available threshold window
                float chargeFactor = (dot - upper_dot) / (1f - upper_dot);
                float chargeRate = JetpackScript.YawToLook_Capacitor_ChargeSpeed * Mathf.Pow(chargeFactor, JetpackScript.YawToLook_Capacitor_ChargePower);
                _capacitor += chargeRate * (float)elapsedSeconds;
            }
            else if (dot < bottom_dot)
            {
                // DISCHARGE REGION: max amount
                _capacitor -= JetpackScript.YawToLook_Capacitor_DischargeSpeed * (float)elapsedSeconds;
            }
            else if (dot < lower_dot)
            {
                // DISCHARGE REGION: dot < lower threshold
                // Normalize to 0-1 range based on threshold position
                //float decayFactor = (lower_dot - dot) / lower_dot;
                float decayFactor = UtilityMath.GetScaledValue_Capped(0, 1, bottom_dot, lower_dot, dot);
                float decayRate = JetpackScript.YawToLook_Capacitor_DischargeSpeed * Mathf.Pow(decayFactor, JetpackScript.YawToLook_Capacitor_DischargePower);
                _capacitor -= decayRate * (float)elapsedSeconds;
            }
            // else: NEUTRAL WINDOW - capacitor remains unchanged

            // Enforce capacitor bounds
            _capacitor = Mathf.Clamp(_capacitor, 0f, 1f);
        }

        private static float UpdateCapacitor3(float capacitor, Vector3 target, Vector3 forward, double elapsedSeconds, float upper_dot, float lower_dot, float bottom_dot)
        {
            // Calculate alignment between target and forward directions
            float dot = Vector3.Dot(target, forward);

            var retVal = capacitor;

            // Determine which region we're in
            if (dot > upper_dot)
            {
                // CHARGE REGION: dot > upper threshold
                // Normalize to 0-1 range based on available threshold window
                float chargeFactor = (dot - upper_dot) / (1f - upper_dot);
                float chargeRate = JetpackScript.YawToLook_Capacitor_ChargeSpeed * Mathf.Pow(chargeFactor, JetpackScript.YawToLook_Capacitor_ChargePower);
                retVal += chargeRate * (float)elapsedSeconds;
            }
            else if (dot < bottom_dot)
            {
                // DISCHARGE REGION: max amount
                retVal -= JetpackScript.YawToLook_Capacitor_DischargeSpeed * (float)elapsedSeconds;
            }
            else if (dot < lower_dot)
            {
                // DISCHARGE REGION: dot < lower threshold
                // Normalize to 0-1 range based on threshold position
                //float decayFactor = (lower_dot - dot) / lower_dot;
                float decayFactor = UtilityMath.GetScaledValue_Capped(0, 1, bottom_dot, lower_dot, dot);
                float decayRate = JetpackScript.YawToLook_Capacitor_DischargeSpeed * Mathf.Pow(decayFactor, JetpackScript.YawToLook_Capacitor_DischargePower);
                retVal -= decayRate * (float)elapsedSeconds;
            }
            // else: NEUTRAL WINDOW - capacitor remains unchanged

            // Enforce capacitor bounds
            retVal = Mathf.Clamp(retVal, 0f, 1f);

            return retVal;
        }

        #endregion
    }
}
