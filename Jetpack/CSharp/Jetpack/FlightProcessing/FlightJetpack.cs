using Jetpack.DebugCode;
using Jetpack.InputWatchers;
using Jetpack.Models;
using Jetpack.Scanning;
using PerfectlyNormalBaS;
using System;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.FlightProcessing
{

    // TODO: in deactivate, make sure the player isn't roated

    public class FlightJetpack
    {
        private readonly RayCastStorage _raycast_storage;
        private readonly PlayerRotator _rotator;
        private readonly DebugStats _debugStats;
        private readonly ConfinedArea _confinedScanner;
        private readonly RepelGround _repelGround;
        private readonly ObstacleAvoidance _obstacleAvoidance;
        private readonly PullYawToLook _pullYawToLook;
        private readonly PullYawToLook2 _pullYawToLook2;
        private readonly RotateToLook _rotateToLook;
        private readonly GazeBufferVisualizer_Target _gazeBufferVisualizer_target;
        private readonly GazeBufferVisualizer_Offset _gazeBufferVisualizer_offset;
        private readonly HeadUpRotateVisualizer _headUpRotateVisualizer;

        private FlightData _standardState = null;

        private float _last_applied_drag = -1;

        private DateTime _activation_time = DateTime.UtcNow;
        private DateTime _prevTick = DateTime.UtcNow;

        public FlightJetpack(RayCastStorage raycast_storage, PlayerRotator rotator, DebugStats debugStats)
        {
            _raycast_storage = raycast_storage;
            _rotator = rotator;
            _debugStats = debugStats;
            _confinedScanner = new ConfinedArea(_raycast_storage);
            _repelGround = new RepelGround(_raycast_storage);
            _obstacleAvoidance = new ObstacleAvoidance(_raycast_storage);
            _pullYawToLook = new PullYawToLook();
            _pullYawToLook2 = new PullYawToLook2(rotator);
            _rotateToLook = new RotateToLook(rotator);
            _gazeBufferVisualizer_target = new GazeBufferVisualizer_Target();
            _gazeBufferVisualizer_offset = new GazeBufferVisualizer_Offset();
            _headUpRotateVisualizer = new HeadUpRotateVisualizer();
        }

        public void Activate(float drag)
        {
            _prevTick = DateTime.UtcNow;

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

            _activation_time = DateTime.UtcNow;

            // Reset scanner
            _confinedScanner.Clear();
            _repelGround.Clear();
            _obstacleAvoidance.Clear();

            // Others
            _pullYawToLook.Clear();
            _pullYawToLook2.Clear();
            _rotateToLook.Clear();
            _gazeBufferVisualizer_target.Clear();
            _gazeBufferVisualizer_offset.Clear();
            _headUpRotateVisualizer.Clear();
        }
        public void Deactivate()
        {
            _prevTick = DateTime.UtcNow;

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

            _confinedScanner.Clear();
            _repelGround.Clear();
            _obstacleAvoidance.Clear();
            _pullYawToLook.Clear();
            _pullYawToLook2.Clear();
            _rotateToLook.Clear();
            _gazeBufferVisualizer_target.Clear();
            _gazeBufferVisualizer_offset.Clear();
            _headUpRotateVisualizer.Clear();
        }

        public void Update(float drag, float horz_accel, float vert_accel, float gravity)
        {
            // TODO: may need a second elapsed that considers time slowdown
            DateTime now = DateTime.UtcNow;
            float elapsed_seconds = (float)Math1D.Clamp((now - _prevTick).TotalSeconds, 0, 0.25);       
            _prevTick = now;

            Locomotion loco = Player.local.locomotion;

            if (_last_applied_drag != drag)
            {
                loco.physicBody.drag = drag;
                _last_applied_drag = drag;
            }

            _raycast_storage.Clear();
            _confinedScanner.Update_CastRays();
            _repelGround.Update_CastRays(loco);
            _obstacleAvoidance.Update_CastRays();

            _confinedScanner.Update_Finish(elapsed_seconds);
            float percent_accel = UtilityMath.GetScaledValue_Capped(0.25f, 1f, 1f, 0f, _confinedScanner.ConfinedPercent);

            DestabilizeHeldNPC(Player.local.handLeft);
            DestabilizeHeldNPC(Player.local.handRight);

            Vector2 left_stick = InputUtil.GetLeftStick();
            Vector2 right_stick = InputUtil.GetRightStick();
            Vector3? input_dir = GetInputDirection(left_stick, right_stick, loco);

            // Only do these when already in the air
            if (!Player.local.locomotion.isGrounded && (DateTime.UtcNow - _activation_time).TotalMilliseconds > 500)
            {
                Vector3? accel_repel = _repelGround.Update_Finish(loco, input_dir);
                if (accel_repel != null)
                    loco.physicBody.AddForce(accel_repel.Value, ForceMode.Acceleration);

                Vector3? accel_obstacle = _obstacleAvoidance.Update_Finish(input_dir);
                if (accel_obstacle != null)
                    loco.physicBody.AddForce(accel_obstacle.Value, ForceMode.Acceleration);

                _pullYawToLook.Update(elapsed_seconds);
                _pullYawToLook2.Update(elapsed_seconds);
                _rotateToLook.Update(elapsed_seconds);
                _gazeBufferVisualizer_target.Update();
                _gazeBufferVisualizer_offset.Update();
                _headUpRotateVisualizer.Update(elapsed_seconds);
            }

            // TODO: make an option for horiztonal control mode (direct or accel)
            //loco.horizontalAirSpeed = horizontalSpeed / 100f;

            AccelHorz(left_stick, loco, horz_accel * percent_accel);
            AccelUp(right_stick, loco, vert_accel * percent_accel, gravity);
        }

        // This is kind of a copy of AccelHorz and AccelUp.  The output is sent to classes that avoid hitting things, and
        // is used so they don't fight with the desired input direction
        private Vector3? GetInputDirection(Vector2 left_stick, Vector2 right_stick, Locomotion loco)
        {
            var pointer = Pointer.GetActive();
            if (pointer.isPointingUI)
                return null;

            Vector3 retVal = Vector3.zero;

            var transform = Player.local.transform;

            retVal += transform.forward * left_stick.y;
            retVal += transform.right * left_stick.x;
            retVal += Vector3.up * right_stick.y;

            if (retVal.IsNearZero())
                return null;

            return retVal.normalized;
        }

        // These two actually apply accelerations
        private void AccelHorz(Vector2 axis, Locomotion loco, float horz_accel)
        {
            if (axis.x == 0 && axis.y == 0)
                return;

            var transform = Player.local.transform;

            loco.physicBody.AddForce(transform.forward * horz_accel * axis.y, ForceMode.Acceleration);
            loco.physicBody.AddForce(transform.right * horz_accel * axis.x, ForceMode.Acceleration);
        }



        // TODO: change this to be relative to transform.up
        // take that gravity logic into account, but only the component of accel that is along y

        private void AccelUp(Vector2 axis, Locomotion loco, float vert_accel, float gravity)
        {
            float up_accel = 0f;

            float axis_y = axis.y;

            if (!axis_y.IsNearZero())
            {
                var pointer = Pointer.GetActive();
                if (!pointer.isPointingUI)
                {
                    up_accel = vert_accel * axis_y;

                    if (axis_y > 0)
                        up_accel += gravity;     // when pushing up, cancel out gravity.  When pushing down, it's accelerating down in addition to gravity
                }
            }
            up_accel -= gravity;

            loco.physicBody.AddForce(Vector3.up * up_accel, ForceMode.Acceleration);
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