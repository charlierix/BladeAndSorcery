using Jetpack2.Core;
using PerfectlyNormalBaS;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.FlightProcessing
{
    // TODO: when wings are out, apply thrust along wing forward instead of body forward (but not if airbrake is active)

    /// <summary>
    /// Applies thrust along body forward when the trigger is pulled in
    /// </summary>
    /// <remarks>
    /// Reusing the wing forceat points
    /// </remarks>
    public class TriggerThrusters
    {
        #region Declaration Section

        private Vector3 _body_forward = Vector3.zero;
        private Vector3 _body_up = Vector3.zero;
        private UtilJetpack.PlayerVRPoints _positions = null;

        #region drawing

        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private DebugRenderer3D _renderer = null;

        #endregion

        #endregion

        public void Clear()
        {
            ClearDebugVisuals();
        }

        public void Update(Vector3 body_forward, Vector3 body_up, UtilJetpack.PlayerVRPoints positions)
        {
            _body_forward = body_forward;
            _body_up = body_up;
            _positions = positions;
        }
        public (Vector3 accel, Vector3 torque)? UpdateFixed()
        {
            if (!UIModOptions.ShouldShouldUseTriggerThrust)
                return null;

            if (_positions == null)
                return null;

            // Get pos where forces will be applied
            var force_at = Wings.GetForceAtPosition(_positions);

            var left = Process(force_at.World_Left, _positions.world.center, _body_forward, PlayerControl.handLeft.useAxis);
            var right = Process(force_at.World_Right, _positions.world.center, _body_forward, PlayerControl.handRight.useAxis);

            if (left == null && right == null)
                return null;
            else if (left != null && right == null)
                return left;
            else if (right != null && left == null)
                return right;

            return
            (
                left.Value.accel + right.Value.accel,
                left.Value.torque + right.Value.torque
            );
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

            //if (_wingvisual_left != null)
            //{
            //    _renderer.Remove(_wingvisual_left);
            //    _wingvisual_left = null;
            //}

            _renderer = null;
        }

        #endregion
        #region Private Methods

        private static (Vector3 accel, Vector3 torque)? Process(Vector3 forceat_world, Vector3 center_mass_world, Vector3 body_forward, float trigger_percent)
        {
            if (trigger_percent.IsNearZero())
                return null;

            // Calculate relative position from center of mass (pretending it's a solid sphere, so center of mass is also center of position: halfway between head and foot)
            Vector3 rel_pos = forceat_world - center_mass_world;

            Vector3 accel = body_forward * UIModOptions.TriggerThrust_Accel * trigger_percent;
            Vector3 force = accel * UIModOptions.Wing_PlayerMass;       // changing to force so that torque can be calculated

            // Split into translation and torque
            var split = Math3D.SplitForceIntoTranslationAndTorque(rel_pos, force);

            // Convert to accelerations
            float momentOfInertia = (2f / 5f) * UIModOptions.Wing_PlayerMass * UIModOptions.Wing_PlayerRadius * UIModOptions.Wing_PlayerRadius;     // for a solid sphere: I = (2/5) * m * r^2
            Vector3 angularAcceleration = split.torque / momentOfInertia;

            return (split.translationForce / UIModOptions.Wing_PlayerMass, angularAcceleration);
        }

        #endregion
    }
}
