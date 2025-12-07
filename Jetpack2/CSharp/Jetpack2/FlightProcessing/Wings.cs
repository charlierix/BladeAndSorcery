using Jetpack2.Core;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.FlightProcessing
{
    public class Wings
    {

        // check if either or both hands are stretched out
        // if both hands stretched out and roughly mirror each other, and fingers are open, continue with wing or braking
        // if only one hand is stretched, only do braking if hand is at correct range of angles


        // see egg wings AeroSurface
        //  that class is too tied into game object and rigid body
        //  make a class that can calculate the same, but is a util function

        #region class: ForceAtPos

        private class ForceAtPos
        {
            // relative to middle point, body_forward, body_up
            public Vector3 Normalized_Left { get; set; }
            public Vector3 Normalized_Right { get; set; }

            public Vector3 Body_Left { get; set; }
            public Vector3 Body_Right { get; set; }

            // these aren't relative to anything, they are in world coords
            public Vector3 World_Left { get; set; }
            public Vector3 World_Right { get; set; }
        }

        #endregion

        #region Declaration Section

        private UtilJetpack.PlayerVRPoints _positions = null;
        private UtilJetpack.HandWings _wings = null;

        private Rigidbody _rigidBody = null;

        private Vector3 _prev_vel = Vector3.zero;
        private (Vector3 accel, Vector3 torque)? _prev_left = null;
        private (Vector3 accel, Vector3 torque)? _prev_right = null;

        #region drawing

        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        // TODO: get these visuals from a prefab in an asset bundle
        private DebugRenderer3D _renderer = null;

        private DebugItem _wingvisual_left = null;
        private DebugItem _wingvisual_right = null;

        private DebugItem _left_up = null;
        private DebugItem _right_up = null;

        private DebugItem _vel_left = null;
        private DebugItem _accel_left = null;
        private DebugItem _torque_left = null;

        private DebugItem _vel_right = null;
        private DebugItem _accel_right = null;
        private DebugItem _torque_right = null;

        private DebugItem _stats = null;

        #endregion

        #endregion

        public void Clear()
        {
            _rigidBody = null;
            ClearDebugVisuals();
        }

        public void Update(Vector3 body_forward, Vector3 body_up)
        {
            if (!UIModOptions.ShouldShouldUseWings && !UIModOptions.ShouldShouldUseAirBrake)
                return;

            _positions = UtilJetpack.GetPlayerPoints(body_forward, body_up);
            _wings = UtilJetpack.GetHandWings(_positions);

            if (UIModOptions.ShowWingStats)
            {
                //DrawWingLines();

                // show velocity in front of the wing
                // show velocity component along normal
                DrawForceLines();

                DrawStats();
            }

            DrawWings();
        }
        public (Vector3 accel, Vector3 torque)? UpdateFixed()
        {
            const float ANGULAR_VELOCITY_MULT = 0.4f;      // if full angular velocity is used, then jitter occurs at high speed with low moment of inertia planes

            if (_positions == null || _wings == null)
                return null;

            if (_rigidBody == null && !Player.local.gameObject.TryGetComponent<Rigidbody>(out _rigidBody))
                return null;

            // Get pos where forces will be applied
            var force_at = GetForceAtPosition(_positions);

            // Treat left and right hands independently
            var left = Process(_wings.Left, _positions.world.center, force_at.World_Left, _rigidBody);
            var right = Process(_wings.Right, _positions.world.center, force_at.World_Right, _rigidBody);

            _prev_vel = Player.local.locomotion.physicBody.velocity;
            _prev_left = left;
            _prev_right = right;

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

            if (_wingvisual_left != null)
            {
                _renderer.Remove(_wingvisual_left);
                _wingvisual_left = null;
            }

            if (_wingvisual_right != null)
            {
                _renderer.Remove(_wingvisual_right);
                _wingvisual_right = null;
            }

            if (_left_up != null)
            {
                _renderer.Remove(_left_up);
                _left_up = null;
            }

            if (_right_up != null)
            {
                _renderer.Remove(_right_up);
                _right_up = null;
            }

            if (_stats != null)
            {
                _renderer.Remove(_stats);
                _stats = null;
            }

            if (_vel_left != null)
            {
                _renderer.Remove(_vel_left);
                _vel_left = null;
            }

            if (_accel_left != null)
            {
                _renderer.Remove(_accel_left);
                _accel_left = null;
            }

            if (_torque_left != null)
            {
                _renderer.Remove(_torque_left);
                _torque_left = null;
            }

            if (_vel_right != null)
            {
                _renderer.Remove(_vel_right);
                _vel_right = null;
            }

            if (_accel_right != null)
            {
                _renderer.Remove(_accel_right);
                _accel_right = null;
            }

            if (_torque_right != null)
            {
                _renderer.Remove(_torque_right);
                _torque_right = null;
            }

            _renderer = null;
        }

        private void DrawWingLines()
        {
            EnsureDebugActive();

            var world = _wings.PlayerPoints.world;

            DrawWingLines_Side(ref _left_up, _renderer, world.left_wing_pos, world.left_wing_up, _wings.Left);
            DrawWingLines_Side(ref _right_up, _renderer, world.right_wing_pos, world.right_wing_up, _wings.Right);
        }
        private static void DrawWingLines_Side(ref DebugItem visual, DebugRenderer3D renderer, Vector3 pos, Vector3 up, UtilJetpack.HandWing wing)
        {
            if (wing == null)
            {
                if (visual != null)
                {
                    renderer.Remove(visual);
                    visual = null;
                }

                return;
            }

            Vector3 dir = StaticRandom.NextBool() ?
                up :
                wing.Normal;

            if (visual == null)
                visual = renderer.AddLine_Basic(pos, pos + dir, LINE_THICKNESS, Color.cyan);
            else
                DebugRenderer3D.AdjustLinePositions(visual, pos, pos + dir);
        }

        private void DrawForceLines()
        {
            EnsureDebugActive();

            var world = _wings.PlayerPoints.world;

            DrawForceLines_Side(ref _vel_left, ref _accel_left, ref _torque_left, _renderer, _prev_left?.accel, _prev_left?.torque, world.left_wing_pos, _prev_vel);
            DrawForceLines_Side(ref _vel_right, ref _accel_right, ref _torque_right, _renderer, _prev_right?.accel, _prev_right?.torque, world.right_wing_pos, _prev_vel);
        }
        private static void DrawForceLines_Side(ref DebugItem viz_vel, ref DebugItem viz_accel, ref DebugItem viz_torque, DebugRenderer3D renderer, Vector3? accel, Vector3? torque, Vector3 pos, Vector3 velocity)
        {
            if (accel == null || torque == null)
            {
                if (viz_vel != null)
                {
                    renderer.Remove(viz_vel);
                    viz_vel = null;
                }

                if (viz_accel != null)
                {
                    renderer.Remove(viz_accel);
                    viz_accel = null;
                }

                if (viz_torque != null)
                {
                    renderer.Remove(viz_torque);
                    viz_torque = null;
                }

                return;
            }

            if (viz_vel == null)
                viz_vel = renderer.AddLine_Basic(pos, pos - velocity, LINE_THICKNESS, UtilityColor.FromHex("6558DC"));
            else
                DebugRenderer3D.AdjustLinePositions(viz_vel, pos, pos - velocity);

            if (viz_accel == null)
                viz_accel = renderer.AddLine_Basic(pos, pos + accel.Value, LINE_THICKNESS, UtilityColor.FromHex("E0D344"));
            else
                DebugRenderer3D.AdjustLinePositions(viz_accel, pos, pos + accel.Value);

            if (viz_torque == null)
                viz_torque = renderer.AddLine_Basic(pos, pos + torque.Value, LINE_THICKNESS, UtilityColor.FromHex("A4E14E"));
            else
                DebugRenderer3D.AdjustLinePositions(viz_torque, pos, pos + torque.Value);
        }

        private void DrawStats()
        {
            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                //Player.local.head.transform.right * 0.35f +
                Player.local.head.transform.up * -0.3f;

            var lines = new List<string>();

            // velocity
            lines.Add($"speed: {_prev_vel.magnitude.ToStringSignificantDigits(3)}");

            // left
            float? dot = _wings?.Left?.WingDotUp;
            string angle = dot != null ?
                Math1D.Dot_to_Degrees(1 - dot.Value).ToStringSignificantDigits(3) :
                "--";

            string accel = _prev_left?.accel.ToStringSignificantDigits(3) ?? "--";
            string torque = _prev_left?.torque.ToStringSignificantDigits(3) ?? "--";

            lines.Add($"left angle: {angle}");
            lines.Add($"left translate accel: {accel}");
            lines.Add($"left torque accel: {torque}");

            // right
            dot = _wings?.Right?.WingDotUp;
            angle = dot != null ?
                Math1D.Dot_to_Degrees(1 - dot.Value).ToStringSignificantDigits(3) :
                "--";

            accel = _prev_right?.accel.ToStringSignificantDigits(3) ?? "--";
            torque = _prev_right?.torque.ToStringSignificantDigits(3) ?? "--";

            lines.Add($"right angle: {angle}");
            lines.Add($"right translate accel: {accel}");
            lines.Add($"right torque accel: {torque}");

            string text = string.Join(Environment.NewLine, lines);

            if (_stats == null)
                _stats = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, UtilityColor.FromHex("74C4BF"), UtilityColor.FromHex("5C4023"), TEXT_HEIGHT * lines.Count);

            _stats.Object.transform.position = text_pos;
            _stats.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_stats, new_text: text);
        }

        private void DrawWings()
        {
            EnsureDebugActive();

            var world = _wings.PlayerPoints.world;

            DrawWing(ref _wingvisual_left, _renderer, _wings.Left, world.left_wing_pos, world.left_wing_forward, world.left_wing_up, true);
            DrawWing(ref _wingvisual_right, _renderer, _wings.Right, world.right_wing_pos, world.right_wing_forward, world.right_wing_up, false);
        }
        private static void DrawWing(ref DebugItem visual, DebugRenderer3D renderer, UtilJetpack.HandWing wing, Vector3 pos, Vector3 forward, Vector3 up, bool is_left)
        {
            if (wing == null || (wing.IsAirBrake && !UIModOptions.ShouldShouldUseAirBrake) || (!wing.IsAirBrake && !UIModOptions.ShouldShouldUseWings))
            {
                if (visual != null)
                {
                    renderer.Remove(visual);
                    visual = null;
                }

                return;
            }

            if (visual == null)
            {
                Vector3 scale = new Vector3(0.15f, 0.01f, 0.5f);
                visual = renderer.AddCube(Vector3.zero, scale, Color.white);
            }

            visual.Object.transform.position = pos;
            visual.Object.transform.rotation = Math3D.GetRotation(new DoubleVector(new Vector3(0, 0, 1), new Vector3(0, 1, 0)), new DoubleVector(forward, up));

            Color color = wing.IsAirBrake ?
                Color.black :
                Color.white;

            if (!wing.Percent.IsNearValue(1))
                color.a = wing.Percent;

            DebugRenderer3D.AdjustColor(visual, color);
        }

        #endregion
        #region Private Methods

        private static ForceAtPos GetForceAtPosition(UtilJetpack.PlayerVRPoints positions)
        {
            // sliders are normalized
            //UIModOptions.Wing_ForceAt_X

            Vector3 normalized_right = new Vector3(UIModOptions.Wing_ForceAt_X, UIModOptions.Wing_ForceAt_Y, UIModOptions.Wing_ForceAt_Z);
            Vector3 normalized_left = new Vector3(-normalized_right.x, normalized_right.y, normalized_right.z);

            Vector3 body_left = normalized_left * positions.height;
            Vector3 body_right = normalized_right * positions.height;

            Vector3 world_left = positions.Transform_ToWorld(body_left);
            Vector3 world_right = positions.Transform_ToWorld(body_right);

            return new ForceAtPos
            {
                Normalized_Left = normalized_left,
                Normalized_Right = normalized_right,

                Body_Left = body_left,
                Body_Right = body_right,

                World_Left = world_left,
                World_Right = world_right,
            };
        }

        private static (Vector3 accel, Vector3 torque)? Process(UtilJetpack.HandWing wing, Vector3 center_world, Vector3 forceat_world, Rigidbody rigidBody)
        {
            if (wing == null)
                return null;

            Vector3 velocity = Player.local.locomotion.physicBody.velocity;

            if (wing.IsAirBrake && UIModOptions.ShouldShouldUseAirBrake)
                return Process_Common(wing, forceat_world, rigidBody, center_world, velocity, UIModOptions.Wing_Airbrake_SurfaceArea);

            else if (!wing.IsAirBrake && UIModOptions.ShouldShouldUseWings)
                return Process_Common(wing, forceat_world, rigidBody, center_world, velocity, UIModOptions.Wing_SurfaceArea);

            return null;
        }

        // TODO: rework to be similar to ProcessWing3 as far as moment of inertia (make a common function, pass in values to it)
        // there isn't a difference physically between airbrake and wing at 90 degrees
        //
        // may want to get rid of airbrake completely, or morph the wing from wing surface area to airbrake surface area above a
        // high angle of attack.  also reduce roll,pitch to zero as the surface area morphs (keep yaw)
        private static void ProcessAirBrake(UtilJetpack.HandWing wing, Vector3 forceat_world, Rigidbody rigidBody, Vector3 velocity)
        {
            // For now, don't look at where the hand is.  Just apply a consistent accel

            float speed = velocity.magnitude;

            // Compute drag force
            float dragForceMagnitude = 0.5f * UIModOptions.Wing_AirDensity * speed * speed * UIModOptions.Wing_Airbrake_DragCoefficient * UIModOptions.Wing_SurfaceArea;

            // Apply force opposite to velocity direction
            Vector3 dragForce = -velocity.normalized * dragForceMagnitude;

            Vector3 accel = dragForce / UIModOptions.Wing_PlayerMass;

            accel *= wing.Percent;

            rigidBody.AddForceAtPosition(accel, forceat_world, ForceMode.Acceleration);
        }

        private static void ProcessWing(UtilJetpack.HandWing wing, Vector3 forceat_world, Rigidbody rigidBody, Vector3 velocity)
        {




        }
        //private static void ProcessWing1(UtilJetpack.HandWing wing, Vector3 forceat_world, Rigidbody rigidBody, Vector3 velocity)
        //{


        //    // this block should just be able to use wing.WingDotForward

        //    // Get the wing's orientation
        //    Vector3 wingForward = wing.IsAirBrake ? Vector3.zero : wing.LeftWingForward; // Or however you determine wing orientation
        //    Vector3 wingUp = wing.IsAirBrake ? Vector3.zero : wing.LeftWingUp; // Or however you determine wing orientation

        //    // Calculate the angle of attack (angle between wing normal and velocity direction)
        //    Vector3 velocityNormalized = velocity.normalized;
        //    Vector3 wingNormal = Vector3.Cross(wingForward, wingUp).normalized;







        //    // Calculate the angle of attack (in radians)
        //    float angleOfAttack = Mathf.Acos(Mathf.Clamp(Vector3.Dot(velocityNormalized, wingNormal), -1f, 1f));

        //    // Convert to degrees for clarity (optional)
        //    float angleOfAttackDegrees = Mathf.Rad2Deg * angleOfAttack;




        //    // Calculate air speed
        //    float speed = velocity.magnitude;




        //    // Calculate lift and drag coefficients based on angle of attack
        //    float liftCoefficient = 0f; // Simple linear model for demonstration
        //    float dragCoefficient = 0f;

        //    // You'll need to define lift/drag characteristics in your mod options or here
        //    // For now, a simple model:
        //    if (angleOfAttackDegrees < 10f) // Small angle of attack
        //    {
        //        liftCoefficient = 2f * Mathf.PI * angleOfAttackDegrees * Mathf.Deg2Rad;
        //        dragCoefficient = 0.01f + 0.02f * angleOfAttackDegrees * angleOfAttackDegrees;
        //    }
        //    else // Large angle of attack (stall condition)
        //    {
        //        liftCoefficient = 0f; // No lift when stalling
        //        dragCoefficient = 1.0f + 0.05f * angleOfAttackDegrees; // High drag when stalling
        //    }






        //    // Calculate force magnitude
        //    float dynamicPressure = 0.5f * UIModOptions.Wing_AirDensity * speed * speed;
        //    float liftForceMagnitude = dynamicPressure * liftCoefficient * UIModOptions.Wing_SurfaceArea;
        //    float dragForceMagnitude = dynamicPressure * dragCoefficient * UIModOptions.Wing_SurfaceArea;

        //    // Calculate lift and drag directions
        //    Vector3 liftDirection = Vector3.Cross(wingNormal, velocityNormalized); // Perpendicular to both wing normal and velocity
        //    Vector3 dragDirection = -velocityNormalized;

        //    // Apply lift force (perpendicular to velocity, in plane formed by wing normal and velocity)
        //    Vector3 liftForce = liftDirection * liftForceMagnitude;
        //    // Apply drag force (opposite to velocity)
        //    Vector3 dragForce = dragDirection * dragForceMagnitude;

        //    // Combine lift and drag forces
        //    Vector3 totalForce = liftForce + dragForce;

        //    // Apply force at the specified point
        //    Vector3 accel = totalForce / UIModOptions.Wing_PlayerMass;
        //    accel *= wing.Percent; // Apply the percentage of wing openness

        //    rigidBody.AddForceAtPosition(accel, forceat_world, ForceMode.Acceleration);
        //}
        private static void ProcessWing2(UtilJetpack.HandWing wing, Vector3 forceat_world, Rigidbody rigidBody, Vector3 velocity)
        {
            // Determine which wing we're processing (left or right)
            // This assumes you have access to wing orientation data elsewhere
            // If wing orientation data is stored in playerpoints, use that:

            // Get the wing's orientation from the player points system
            Vector3 wingForward = Vector3.zero; // You'll need to get this from your wing data
            Vector3 wingUp = Vector3.zero; // You'll need to get this from your wing data

            // Assuming you have access to this via wing data or player points
            // For example, if you stored it in the HandWing object:
            // wingForward = wing.LeftWingForward;
            // wingUp = wing.LeftWingUp;

            // Calculate the angle of attack (angle between wing normal and velocity direction)
            Vector3 velocityNormalized = velocity.normalized;

            // Calculate the wing normal (perpendicular to the wing surface)
            Vector3 wingNormal = Vector3.Cross(wingForward, wingUp).normalized;

            // Calculate the angle of attack (in radians)
            float angleOfAttack = Mathf.Acos(Mathf.Clamp(Vector3.Dot(velocityNormalized, wingNormal), -1f, 1f));

            // Convert to degrees for clarity (optional)
            float angleOfAttackDegrees = Mathf.Rad2Deg * angleOfAttack;

            // Calculate air speed
            float speed = velocity.magnitude;

            // Calculate lift and drag coefficients based on angle of attack
            // This is a simplified model - you might want to use a more complex lookup table or curve
            float liftCoefficient = 0f;
            float dragCoefficient = 0f;

            // Simple linear lift model (you'll want to calibrate this)
            // Lift coefficient should be maximum at angle of attack around 5-15 degrees
            if (Mathf.Abs(angleOfAttackDegrees) < 30f)
            {
                // Linear lift coefficient (should be calibrated for realism)
                liftCoefficient = 0.2f * angleOfAttackDegrees * Mathf.Deg2Rad; // Adjust this constant
                                                                               // Drag coefficient increases with angle of attack
                dragCoefficient = 0.02f + 0.05f * Mathf.Pow(Mathf.Abs(angleOfAttackDegrees), 1.5f); // Adjust this formula
            }
            else
            {
                // At large angles, assume no lift (stall)
                liftCoefficient = 0f;
                dragCoefficient = 2.0f; // High drag
            }

            // Calculate force magnitude
            float dynamicPressure = 0.5f * UIModOptions.Wing_AirDensity * speed * speed;
            float liftForceMagnitude = dynamicPressure * liftCoefficient * UIModOptions.Wing_SurfaceArea;
            float dragForceMagnitude = dynamicPressure * dragCoefficient * UIModOptions.Wing_SurfaceArea;

            // Determine the direction of lift (perpendicular to velocity, in the plane of the wing)
            Vector3 liftDirection = Vector3.Cross(wingNormal, velocityNormalized);
            Vector3 dragDirection = -velocityNormalized;

            // Apply lift force (perpendicular to velocity, in plane formed by wing normal and velocity)
            Vector3 liftForce = liftDirection * liftForceMagnitude;
            // Apply drag force (opposite to velocity)
            Vector3 dragForce = dragDirection * dragForceMagnitude;

            // Combine lift and drag forces
            Vector3 totalForce = liftForce + dragForce;

            // Apply force at the specified point
            Vector3 accel = totalForce / UIModOptions.Wing_PlayerMass;
            accel *= wing.Percent; // Apply the percentage of wing openness

            rigidBody.AddForceAtPosition(accel, forceat_world, ForceMode.Acceleration);
        }
        private static void ProcessWing3(UtilJetpack.HandWing wing, Vector3 forceat_world, Rigidbody rigidBody, Vector3 center_mass_world, Vector3 velocity)
        {
            float speed = velocity.magnitude;
            float dynamicPressure = 0.5f * UIModOptions.Wing_AirDensity * speed * speed;

            // Turn the magnitude of velocity into what the force will be
            float mult = dynamicPressure * UIModOptions.Wing_SurfaceArea / UIModOptions.Wing_PlayerMass;
            Vector3 vel_force = (velocity / speed) * mult;

            // Project force onto wing normal
            Vector3 vel_projected_normal = vel_force.GetProjectedVector(wing.Normal);       // it doesn't matter what direction the wing's normal is, the result vector will be direction of velocity

            // Calculate relative position from center of mass (pretending it's a solid sphere, so center of mass is also center of position: halfway between head and foot)
            Vector3 rel_pos = forceat_world - center_mass_world;

            // Split into translation and torque
            var split = Math3D.SplitForceIntoTranslationAndTorque(rel_pos, vel_projected_normal);

            // Adjust torque with proper angular acceleration calculation
            float momentOfInertia = (2f / 5f) * UIModOptions.Wing_PlayerMass * UIModOptions.Wing_PlayerRadius * UIModOptions.Wing_PlayerRadius;     // for a solid sphere: I = (2/5) * m * r^2
            Vector3 angularAcceleration = split.torque / momentOfInertia;

            // Apply accel
            rigidBody.AddForce(split.translationForce, ForceMode.Acceleration);
            rigidBody.AddTorque(angularAcceleration, ForceMode.Acceleration);
        }

        private static (Vector3 accel, Vector3 torque) Process_Common(UtilJetpack.HandWing wing, Vector3 forceat_world, Rigidbody rigidBody, Vector3 center_mass_world, Vector3 velocity, float surface_area)
        {
            float speed = velocity.magnitude;
            float dynamicPressure = 0.5f * UIModOptions.Wing_AirDensity * speed * speed;

            // Turn the magnitude of velocity into what the force will be
            float mult = dynamicPressure * surface_area / UIModOptions.Wing_PlayerMass;
            Vector3 vel_force = (velocity / speed) * mult;
            vel_force = -vel_force;     // the force acts in the opposite direction of travel

            // Project force onto wing normal
            Vector3 vel_projected_normal = vel_force.GetProjectedVector(wing.Normal);       // it doesn't matter what direction the wing's normal is, the result vector will be direction of velocity

            // Calculate relative position from center of mass (pretending it's a solid sphere, so center of mass is also center of position: halfway between head and foot)
            Vector3 rel_pos = forceat_world - center_mass_world;

            // Split into translation and torque
            var split = Math3D.SplitForceIntoTranslationAndTorque(rel_pos, vel_projected_normal);

            // Adjust torque with proper angular acceleration calculation
            float momentOfInertia = (2f / 5f) * UIModOptions.Wing_PlayerMass * UIModOptions.Wing_PlayerRadius * UIModOptions.Wing_PlayerRadius;     // for a solid sphere: I = (2/5) * m * r^2
            Vector3 angularAcceleration = split.torque / momentOfInertia;

            return (split.translationForce, angularAcceleration);
        }

        #endregion
    }
}
