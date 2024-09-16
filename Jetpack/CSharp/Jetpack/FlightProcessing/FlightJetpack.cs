using Jetpack.InputWatchers;
using Jetpack.Models;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.FlightProcessing
{
    public class FlightJetpack
    {
        private FlightData _standardState = null;

        private float _last_applied_drag = -1;

        public void Activate(float drag)
        {
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
        }
        public void Deactivate()
        {
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
        }

        public void Update(float drag, float horz_accel, float vert_accel, float gravity)
        {
            Locomotion loco = Player.local.locomotion;

            if (_last_applied_drag != drag)
            {
                loco.physicBody.drag = drag;
                _last_applied_drag = drag;
            }



            // TODO: detect if in a confined space and reduce accelerations




            DestabilizeHeldNPC(Player.local.handLeft);
            DestabilizeHeldNPC(Player.local.handRight);

            // TODO: make an option for horiztonal control mode (direct or accel)
            //loco.horizontalAirSpeed = horizontalSpeed / 100f;

            AccelHorz(InputUtil.GetLeftStick(), loco, horz_accel);
            AccelUp(InputUtil.GetRightStick(), loco, vert_accel, gravity);
        }

        private void AccelHorz(Vector2 axis, Locomotion loco, float horz_accel)
        {
            if (axis.x == 0 && axis.y == 0)
                return;

            var transform = Player.local.transform;

            // TODO: need to project transform's forward and right to horizontal plane

            loco.physicBody.AddForce(transform.forward * horz_accel * axis.y, ForceMode.Acceleration);
            loco.physicBody.AddForce(transform.right * horz_accel * axis.x, ForceMode.Acceleration);
        }
        private void AccelUp(Vector2 axis, Locomotion loco, float vert_accel, float gravity)
        {
            float up_accel = 0f;

            float axis_y = axis.y;

            if (axis_y != 0.0 && (!Pointer.GetActive() || !Pointer.GetActive().isPointingUI))
            {
                up_accel = vert_accel * axis_y;

                if (axis_y > 0)
                    up_accel += gravity;     // when pushing up, cancel out gravity.  When pushing down, it's accelerating down in addition to gravity
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

                Height = Player.local.creature.GetHeight(),
                Morphology = Player.local.creature.morphology.Clone(),
                HeadLocalPosition = Player.local.headOffsetTransform.localPosition,     // this is zero
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