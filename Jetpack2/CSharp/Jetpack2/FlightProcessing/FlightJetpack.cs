using Jetpack2.Core;
using Jetpack2.DebugCode;
using Jetpack2.InputWatchers;
using Jetpack2.Models;
using Jetpack2.Scanning;
using PerfectlyNormalBaS;
using System;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.FlightProcessing
{

    // TODO: in deactivate, make sure the player isn't roated

    // TODO: listen for grounded event

    // TODO: boost excel during time dilation?

    // TODO: notice when picking up an item and compensate for the added weight

    public class FlightJetpack
    {
        private readonly RayCastStorage _raycast_storage;
        private readonly PlayerRagdollUtil _ragdollUtil = new PlayerRagdollUtil();
        private readonly PlayerRotator _rotator;
        private readonly DebugStats _debugStats;
        //private readonly AvgHandPositionTracker _handPositionTracker;
        private readonly ConfinedArea _confinedScanner;
        private readonly RepelGround _repelGround;
        private readonly ObstacleAvoidance _obstacleAvoidance;
        private readonly RotateToLook _rotateToLook;
        private readonly GazeBufferVisualizer_Target _gazeBufferVisualizer_target;
        private readonly GazeBufferVisualizer_Offset _gazeBufferVisualizer_offset;
        private readonly HeadUpRotateVisualizer _headUpRotateVisualizer;
        private readonly HandZonePosVisualizer _handZonePosVisualizer;
        private readonly Wings _wings;

        private FlightData _standardState = null;

        private float _last_applied_drag = -1;

        private DateTime _activation_time = DateTime.UtcNow;
        private DateTime _prevTick = DateTime.UtcNow;
        private DateTime _prevTick_fixed = DateTime.UtcNow;

        private bool _wereRaycastsConsumed = true;      // default to true so that the first call to update will do raycasts and set to false

        private Vector2 _left_stick = Vector2.zero;
        private Vector2 _right_stick = Vector2.zero;

        //public FlightJetpack(RayCastStorage raycast_storage, PlayerRotator rotator, DebugStats debugStats, AvgHandPositionTracker handPositionTracker)
        public FlightJetpack(RayCastStorage raycast_storage, PlayerRotator rotator, DebugStats debugStats)
        {
            _raycast_storage = raycast_storage;
            _rotator = rotator;
            _debugStats = debugStats;
            //_handPositionTracker = handPositionTracker;
            _confinedScanner = new ConfinedArea(_raycast_storage);
            _repelGround = new RepelGround(_raycast_storage);
            _obstacleAvoidance = new ObstacleAvoidance(_raycast_storage);
            _rotateToLook = new RotateToLook(rotator);
            _gazeBufferVisualizer_target = new GazeBufferVisualizer_Target();
            _gazeBufferVisualizer_offset = new GazeBufferVisualizer_Offset();
            _headUpRotateVisualizer = new HeadUpRotateVisualizer();
            _handZonePosVisualizer = new HandZonePosVisualizer();
            _wings = new Wings();
        }

        public void Activate(float drag)
        {
            _prevTick = DateTime.UtcNow;
            _prevTick_fixed = DateTime.UtcNow;

            if (_standardState == null)
                _standardState = GetCurrentState();

            Locomotion loco = Player.local.locomotion;

            loco.groundAngle = -359f;
            loco.physicBody.useGravity = false;        // this mod will do its own gravity, if the slider is non zero
            //loco.physicBody.mass = 100000f;       // not sure what good this would do

            loco.physicBody.drag = drag;
            _last_applied_drag = drag;

            loco.velocity = Vector3.zero;
            Player.fallDamage = false;
            Player.crouchOnJump = false;
            //GameManager.options.allowStickJump = false;       // this doesn't seem to affect anything

            // these supress the thumbsticks from moving the player during flight.  this mod should be doing that with accelerations
            //PlayerControl.local.MoveActive(false);        // can't disable these, because PlayerControl move and turn are called directly form the various inputs (oculus, steamvr, etc) and the top of the move and turn functions return if these bools are false.  under that is where they invoke move/turn, which is what populates PlayerControl.handLeft.JoystickAxis
            //PlayerControl.local.TurnActive(false);
            //loco.allowMove = false;       // these are checked during locomotion, after the thumbsticks have been populated
            //loco.allowTurn = false;

            _activation_time = DateTime.UtcNow;

            // Reset scanner
            _confinedScanner.Clear();
            _repelGround.Clear();
            _obstacleAvoidance.Clear();

            // Others
            _rotateToLook.Clear();
            _gazeBufferVisualizer_target.Clear();
            _gazeBufferVisualizer_offset.Clear();
            _headUpRotateVisualizer.Clear();
            _handZonePosVisualizer.Clear();
            _wings.Clear();
        }
        public void Deactivate()
        {
            _prevTick = DateTime.UtcNow;
            _prevTick_fixed = DateTime.UtcNow;

            Locomotion loco = Player.local.locomotion;

            if (_standardState != null)
            {
                loco.groundAngle = _standardState.MaxAngle;
                loco.physicBody.drag = _standardState.Drag;
                loco.physicBody.useGravity = true;
                loco.physicBody.mass = _standardState.Mass;
                loco.horizontalAirSpeed = _standardState.HorizontalSpeed;
                loco.verticalAirSpeed = _standardState.VerticalSpeed;
                Player.fallDamage = _standardState.FallDamage;
                Player.crouchOnJump = _standardState.CrouchOnJump;
                GameManager.options.allowStickJump = _standardState.StickJump;
            }

            //PlayerControl.local.MoveActive(true);
            //PlayerControl.local.TurnActive(true);
            //loco.allowMove = true;
            //loco.allowTurn = true;

            _confinedScanner.Clear();
            _repelGround.Clear();
            _obstacleAvoidance.Clear();
            _rotateToLook.Clear();
            _gazeBufferVisualizer_target.Clear();
            _gazeBufferVisualizer_offset.Clear();
            _headUpRotateVisualizer.Clear();
            _handZonePosVisualizer.Clear();
            _wings.Clear();
        }

        public void Update()
        {
            // TODO: see if Time.deltaTime is better
            // TODO: may need a second elapsed that considers time slowdown
            DateTime now = DateTime.UtcNow;
            float elapsed_seconds = (float)Math1D.Clamp((now - _prevTick).TotalSeconds, 0, 0.25);
            _prevTick = now;

            Locomotion loco = Player.local.locomotion;

            if (_wereRaycastsConsumed)      // don't need to fire rays more often than fixedupdate can use them
            {
                // TODO: instead of blindly clearing, each enum should have a time setting for how long between clears
                // then each of the ray cast functions should exit early if storage already has that enum

                _raycast_storage.Clear();
                _confinedScanner.Update_CastRays();
                _repelGround.Update_CastRays(loco);
                _obstacleAvoidance.Update_CastRays();
                _wereRaycastsConsumed = false;
            }


            // TODO: listen for thumbstick events off of loco instead of hardcoding against input hardware
            _left_stick = InputUtil.GetLeftStick();
            _right_stick = InputUtil.GetRightStick();



            // Only do these when already in the air
            if (!Player.local.locomotion.isGrounded && (DateTime.UtcNow - _activation_time).TotalMilliseconds > 500)
            {
                var (body_forward, body_up) = _ragdollUtil.GetRagdollForwardUp();

                _rotateToLook.Update(body_forward, body_up, elapsed_seconds);

                _gazeBufferVisualizer_target.Update();
                _gazeBufferVisualizer_offset.Update(body_forward);
                _headUpRotateVisualizer.Update(elapsed_seconds, body_forward, body_up);
                _handZonePosVisualizer.Update(body_forward, body_up);
                _wings.Update(body_forward, body_up);
            }
        }
        public void UpdateFixed()
        {
            float drag = UIModOptions.Drag;
            float horz_accel = UIModOptions.HorizontalAccel;
            float vert_accel = UIModOptions.VerticalAccel;
            float gravity = UIModOptions.GravitySetting;

            // TODO: see if Time.fixedDeltaTime is better (this may also fix the time dilation error)
            // TODO: may need a second elapsed that considers time slowdown
            DateTime now = DateTime.UtcNow;
            float elapsed_seconds = (float)Math1D.Clamp((now - _prevTick_fixed).TotalSeconds, 0, 0.25);
            _prevTick_fixed = now;

            _wereRaycastsConsumed = true;

            Locomotion loco = Player.local.locomotion;

            if (_last_applied_drag != drag)
            {
                loco.physicBody.drag = drag;
                _last_applied_drag = drag;
            }

            _confinedScanner.Update_Finish(elapsed_seconds);
            float percent_accel = UtilityMath.GetScaledValue_Capped(0.25f, 1f, 1f, 0f, _confinedScanner.ConfinedPercent);

            DestabilizeHeldNPC(Player.local.handLeft);
            DestabilizeHeldNPC(Player.local.handRight);

            Vector3? input_dir = ThumbstickToAccel.GetInputDirection(_left_stick, _right_stick, loco);

            // Only do these when already in the air
            if (!Player.local.locomotion.isGrounded && (DateTime.UtcNow - _activation_time).TotalMilliseconds > 500)
            {
                var (body_forward, body_up) = _ragdollUtil.GetRagdollForwardUp();

                //_handPositionTracker.Update_Flying(body_forward, body_up, input_dir != null);

                Vector3? accel_repel = _repelGround.Update_Finish(loco, input_dir);
                if (accel_repel != null)
                    loco.physicBody.AddForce(accel_repel.Value, ForceMode.Acceleration);

                Vector3? accel_obstacle = _obstacleAvoidance.Update_Finish(input_dir);
                if (accel_obstacle != null)
                    loco.physicBody.AddForce(accel_obstacle.Value, ForceMode.Acceleration);

                var accel_wings = _wings.UpdateFixed();
                if (accel_wings != null)
                {
                    loco.physicBody.AddForce(accel_wings.Value.accel, ForceMode.Acceleration);
                    loco.physicBody.AddTorque(accel_wings.Value.torque, ForceMode.Acceleration);
                }
            }

            // TODO: make an option for horiztonal control mode (direct or accel)
            //loco.horizontalAirSpeed = horizontalSpeed / 100f;

            if (!Player.local.locomotion.isGrounded)
                ThumbstickToAccel.ApplyAccel(input_dir, loco, horz_accel, vert_accel, gravity);
        }

        private static void DestabilizeHeldNPC(PlayerHand side)
        {
            if (side.ragdollHand.grabbedHandle)
            {
                Creature grabbedCreature = side.ragdollHand.grabbedHandle.gameObject.GetComponentInParent<Creature>();
                if (grabbedCreature)
                {
                    if (grabbedCreature.ragdoll.state != Ragdoll.State.Inert)
                        grabbedCreature.ragdoll.SetState(Ragdoll.State.Destabilized);
                }
                else
                {
                    foreach (RagdollHand ragdollHand in side.ragdollHand.grabbedHandle.handlers)
                    {
                        Creature creature = ragdollHand.gameObject.GetComponentInParent<Creature>();
                        if (creature && creature != Player.currentCreature)
                            ragdollHand.TryRelease();
                    }
                }
            }
        }

        private static FlightData GetCurrentState()
        {
            Locomotion loco = Player.local.locomotion;

            return new FlightData()
            {
                HorizontalSpeed = loco.horizontalAirSpeed,
                VerticalSpeed = loco.verticalAirSpeed,
                MaxAngle = loco.groundAngle,
                Drag = loco.physicBody.drag,
                Mass = loco.physicBody.mass,
                FallDamage = Player.fallDamage,
                CrouchOnJump = Player.crouchOnJump,
                StickJump = GameManager.options.allowStickJump,

                //Height = Player.local.creature.GetHeight(),
                //Morphology = Player.local.creature.morphology.Clone(),
                //HeadLocalPosition = Player.local.headOffsetTransform.localPosition,     // this is zero
            };

            //Debug.Log($"Activating Flight:\r\n{JsonUtility.ToJson(retVal, true)}");
            /*
{{
    "Drag": 0.30000001192092898,
    "Mass": 70.0,
    "HorizontalSpeed": 0.03999999910593033,
    "VerticalSpeed": 0.0,
    "MaxAngle": 2.7823486328125,
    "FallDamage": true,
    "CrouchOnJump": true,
    "StickJump": true,
    "Height": 1.895983338356018,
    "Morphology": {{
        "eyesHeight": 1.9534728527069092,
        "eyesForward": 0.11809086799621582,
        "headHeight": 1.8641363382339478,
        "headForward": 0.031885743141174319,
        "chestHeight": 1.2766860723495484,
        "spineHeight": 1.0798486471176148,
        "hipsHeight": 1.0564287900924683,
        "armsSpacing": 0.36119115352630618,
        "armsLength": 0.6440020799636841,
        "armsHeight": 1.6500314474105836,
        "armsToEyesHeight": 0.0,
        "height": 2.0837044715881349,
        "legsLength": 1.01776123046875,
        "legsSpacing": 0.2640935182571411,
        "upperLegsHeight": 1.1216603517532349,
        "lowerLegsHeight": 0.5887104272842407,
        "footHeight": 0.1138991191983223
    }},
    "HeadLocalPosition": {{
        "x": 0.0,
        "y": 0.0,
        "z": 0.0
    }}
}}
            */
        }
    }
}