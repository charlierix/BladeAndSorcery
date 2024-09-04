using Jetpack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThunderRoad;
using UnityEngine;
using UnityEngine.UIElements;


// Do all of these big changes in a separate branch


// Make a Flying class to handle actual flying


// Change when player stats are stored.  Move it from flight activate to higher up like a startup.  Or maybe just store
// on first activation, then keep from that point on


// activate sound sounds more like a fireball than flight

// Add sound options



namespace Jetpack
{
    // Source: https://github.com/sjankowskim/wings

    public class JetpackScript : ThunderScript
    {
        // https://kospy.github.io/BasSDK/Components/Guides/ModOptions/#how-do-i-use-modoptions

        [ModOption(name: "Use Jetpack Mod", tooltip: "Turns on/off the Jetpack mod", order = 0)]
        public static bool useJetpackMod = true;


        //public static ModOptionString[] flightActivation_Options = new[]
        //{
        //    new ModOptionString("Hold Up (right stick)", FlightTransitionWatcher.HOLD_UP),
        //    new ModOptionString("Hold Jump", FlightTransitionWatcher.HOLD_JUMP),
        //    new ModOptionString("Double Jump", FlightTransitionWatcher.DOUBLE_JUMP),
        //    new ModOptionString("Double Click Use (index only)", FlightTransitionWatcher.DOUBLECLICK_USE),
        //    new ModOptionString("Hold The Bird 🖕 (index only)", FlightTransitionWatcher.HOLD_BIRD),
        //    new ModOptionString("Hold Devil Horns 🤘 (index only)", FlightTransitionWatcher.HOLD_DEVILHORNS),
        //    new ModOptionString("Hold Rock On 🤟 (index only)", FlightTransitionWatcher.HOLD_ROCKON),
        //    new ModOptionString("Hold Peace Sign ✌️ (index only)", FlightTransitionWatcher.HOLD_PEACE),
        //};

        //public static ModOptionString[] flightActivation_Options = new[]
        //{
        //    new ModOptionString("Hold Up (right stick)", FlightActivationType.HoldUp),
        //    new ModOptionString("Hold Jump", FlightActivationType.HoldJump),
        //    new ModOptionString("Double Jump", FlightActivationType.DoubleJump),
        //    new ModOptionString("Double Click Use (index only)", FlightActivationType.DoubleClick_Use),
        //    new ModOptionString("Hold The Bird 🖕 (index only)", FlightActivationType.HoldBird),
        //    new ModOptionString("Hold Devil Horns 🤘 (index only)", FlightActivationType.HoldDevilHorns),
        //    new ModOptionString("Hold Rock On 🤟 (index only)", FlightActivationType.HoldRockOn),
        //    new ModOptionString("Hold Peace Sign ✌️ (index only)", FlightActivationType.HoldPeace),
        //};

        //[ModOption(name: "Flight Activation/Deactivation", tooltip: "", valueSourceName: nameof(flightActivation_Options), order = 1)]
        //public static FlightActivationType flightActivation = FlightActivationType.HoldUp;

        public static ModOptionString[] flightActivation_Options = new[]
        {
            new ModOptionString("Hold Up (right stick)", FlightActivationType.HoldUp.ToString()),
            new ModOptionString("Hold Jump", FlightActivationType.HoldJump.ToString()),
            new ModOptionString("Double Jump", FlightActivationType.DoubleJump.ToString()),
            new ModOptionString("Double Click Use", FlightActivationType.DoubleClick_Use.ToString()),
            new ModOptionString("Hold The Bird 🖕", FlightActivationType.HoldBird.ToString()),
            new ModOptionString("Hold Devil Horns 🤘", FlightActivationType.HoldDevilHorns.ToString()),
            new ModOptionString("Hold Rock On 🤟", FlightActivationType.HoldRockOn.ToString()),
            new ModOptionString("Hold Peace Sign ✌️", FlightActivationType.HoldPeace.ToString()),
        };


        

        //public static ModOptionString[] flightActivation_Options = new[]
        //{
        //    new ModOptionString(FlightTransitionWatcher.HOLD_UP, FlightTransitionWatcher.HOLD_UP),
        //    new ModOptionString(FlightTransitionWatcher.HOLD_JUMP, FlightTransitionWatcher.HOLD_JUMP),
        //    new ModOptionString(FlightTransitionWatcher.DOUBLE_JUMP, FlightTransitionWatcher.DOUBLE_JUMP),
        //    new ModOptionString(FlightTransitionWatcher.DOUBLECLICK_USE, FlightTransitionWatcher.DOUBLECLICK_USE),
        //    new ModOptionString(FlightTransitionWatcher.HOLD_BIRD, FlightTransitionWatcher.HOLD_BIRD),
        //    new ModOptionString(FlightTransitionWatcher.HOLD_DEVILHORNS, FlightTransitionWatcher.HOLD_DEVILHORNS),
        //    new ModOptionString(FlightTransitionWatcher.HOLD_ROCKON, FlightTransitionWatcher.HOLD_ROCKON),
        //    new ModOptionString(FlightTransitionWatcher.HOLD_PEACE, FlightTransitionWatcher.HOLD_PEACE),
        //};
        //public static ModOptionString[] flightActivation_Options = new[]
        //{
        //    new ModOptionString(FlightTransitionWatcher.HOLD_UP, "a"),
        //    new ModOptionString(FlightTransitionWatcher.HOLD_JUMP, "b"),
        //    new ModOptionString(FlightTransitionWatcher.DOUBLE_JUMP, "c"),
        //    new ModOptionString(FlightTransitionWatcher.DOUBLECLICK_USE, "d"),
        //    new ModOptionString(FlightTransitionWatcher.HOLD_BIRD, "e"),
        //    new ModOptionString(FlightTransitionWatcher.HOLD_DEVILHORNS, "f"),
        //    new ModOptionString(FlightTransitionWatcher.HOLD_ROCKON, "g"),
        //    new ModOptionString(FlightTransitionWatcher.HOLD_PEACE, "h"),
        //};


        [ModOption(name: "Flight Activation/Deactivation", tooltip: "hello", valueSourceName: nameof(flightActivation_Options), order = 1)]
        public static string flightActivation = FlightActivationType.HoldUp.ToString();




        [ModOption(name: "Stop flying on ground", tooltip: "Whether to stop flight when on the ground", order = 1)]
        public static bool deactivateOnGround = true;

        [ModOptionSlider]
        [ModOption(name: "Horizontal Speed", tooltip: "Determines how fast the player can fly horizontally", order = 2)]
        [ModOptionFloatValues(0, 24, 0.25f)]
        public static float horizontalSpeed = 9;

        [ModOptionSlider]
        [ModOption(name: "Vertical Force", tooltip: "Determines how fast the player can fly vertically", order = 3)]
        [ModOptionFloatValues(0, 12, 0.25f)]
        public static float verticalForce = 6;     // discussion on discord was saying defaultValueIndex is ignored in 1.0.3, need to explicitely set a value



        // TODO:
        // Put scale logic as a separate thing from activate/deactivate flight
        //  create extra sliders and checkboxes
        //  make an apply scale button and back to default button

        [ModOptionSlider]
        [ModOption(name: "Player Size %", tooltip: "Can shrink the player so there is more room to fly", order = 4)]
        [ModOptionFloatValues(0, 100, 1f)]
        public static float playerScale = 100;

        //[ModOptionButton]
        //[ModOption(name: "Apply Scale Settings", order = 6)]
        //public static void ApplyScale_UI()
        //{
        //    Debug.Log("ApplyScale Clicked");
        //}



        //public static ModOptionString[] stringOptionHighLow = new[]
        //{
        //    new ModOptionString("High", "Default.High"),
        //    new ModOptionString("Medium", "Default.Medium"),
        //    new ModOptionString("Low", "Default.Low"),
        //    new ModOptionString("Test", (object)"mystringvalue"),
        //    new ModOptionString("Test", "", "mystringvalue"),
        //};

        //[ModOption("DefaultValStringField", null, nameof(stringOptionHighLow))]
        //public static string DefaultValStringField = "Medium";


        // PRE-FLIGHT DATA
        private FlightData _old = null;

        // FLIGHT DATA
        private Locomotion _loco = null;
        private bool _isFlying = false;
        private bool _markedToFly = false;      // will fly once not grounded


        private FlightTransitionWatcher _transitions = new FlightTransitionWatcher();


        // These listeners should be managed by transition watcher
        private InputListener _inputListener = null;
        private KeyDoublePressTracker _keyTracker = new KeyDoublePressTracker();





        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);

            _inputListener = new InputListener(_keyTracker);
        }

        public override void ScriptUpdate()
        {
            base.ScriptUpdate();


            _transitions.Update(flightActivation, _isFlying);


            _inputListener.OnUpdate();

            if (_keyTracker.WasBothDoubleClicked)
            {
                _keyTracker.Clear();

                PlaySounds.Play(SoundName.Jetpack_Activate, cache_effect: false);       // for some reason, the cached version only plays once.  Maybe it gets disabled once the the sound stops?  or needs to be reset somehow?

                if (Player.local.locomotion.isGrounded)
                {
                    if (_isFlying)
                        DeactivateFly();
                    else
                        _markedToFly = true;        // TODO: when grounded, apply enough impulse upward to not be grounded (if that's possible)
                }
                else
                {
                    if (_isFlying)
                        DeactivateFly();
                    else
                        ActivateFly();
                }
            }

            if (_isFlying && deactivateOnGround && Player.local.locomotion.isGrounded)
                DeactivateFly();

            if (_markedToFly && !Player.local.locomotion.isGrounded)
                ActivateFly();
        }

        public override void ScriptFixedUpdate()
        {
            base.ScriptFixedUpdate();

            if (Player.currentCreature)
            {
                if (_isFlying && !Player.local.locomotion.isGrounded)
                {
                    DestabilizeHeldNPC(Player.local.handLeft);
                    DestabilizeHeldNPC(Player.local.handRight);

                    // TODO: make an option for horiztonal control mode (direct or accel)
                    //_loco.horizontalAirSpeed = horizontalSpeed / 100f;

                    AccelHorz(InputListener.GetLeftStick());
                    AccelUp(InputListener.GetRightStick());
                }
            }
            else
            {
                _isFlying = false;
            }
        }

        private void AccelHorz(Vector2 axis)
        {
            if (axis.x == 0 && axis.y == 0)
                return;

            var transform = Player.local.transform;

            // TODO: may need to project transform's forward and right to horizontal plane

            _loco.physicBody.AddForce(transform.forward * horizontalSpeed * axis.y, ForceMode.Acceleration);
            _loco.physicBody.AddForce(transform.right * horizontalSpeed * axis.x, ForceMode.Acceleration);
        }
        private void AccelUp(Vector2 axis)
        {
            if (axis.y != 0.0 && (!Pointer.GetActive() || !Pointer.GetActive().isPointingUI))
                _loco.physicBody.AddForce(Vector3.up * verticalForce * axis.y, ForceMode.Acceleration);
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

        private void ActivateFly()
        {
            _markedToFly = false;

            if (!useJetpackMod)
            {
                Debug.Log("ActivateFly() called, but mod is disabled");
                return;
            }

            _loco = Player.local.locomotion;



            // TODO: it's risky storing these in flight activate.  If this function is called twice before deactivating flight, the values will be corrupt

            // STORE ORIGINAL STATS
            _old = new FlightData()
            {
                HorizontalSpeed = _loco.horizontalAirSpeed,
                VerticalSpeed = _loco.verticalAirSpeed,
                MaxAngle = _loco.groundAngle,
                Drag = _loco.physicBody.drag,
                Mass = _loco.physicBody.mass,
                FallDamage = Player.fallDamage,
                CrouchOnJump = Player.crouchOnJump,
                StickJump = GameManager.options.allowStickJump,

                Height = Player.local.creature.GetHeight(),
                Morphology = Player.local.creature.morphology.Clone(),
                HeadLocalPosition = Player.local.headOffsetTransform.localPosition,     // this is zero
            };



            // ENABLE FLIGHT STATS
            _isFlying = true;
            _loco.groundAngle = -359f;
            _loco.physicBody.useGravity = false;
            //_loco.physicBody.mass = 100000f;
            _loco.physicBody.drag = 0.9f;
            _loco.velocity = Vector3.zero;
            Player.fallDamage = false;
            Player.crouchOnJump = false;
            //GameManager.options.allowStickJump = false;       // this doesn't seem to affect anything

            //ApplyScale();


            //PlaySounds.Play(SoundName.Jetpack_Activate);

            //Debug.Log($"Activating Flight:\r\n{JsonUtility.ToJson(_old, true)}");
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
        private void DeactivateFly()
        {
            _isFlying = false;

            if (_old != null)
            {
                _loco.groundAngle = _old.MaxAngle;
                _loco.physicBody.drag = _old.Drag;
                _loco.physicBody.useGravity = true;
                _loco.physicBody.mass = _old.Mass;
                _loco.horizontalAirSpeed = _old.HorizontalSpeed;
                _loco.verticalAirSpeed = _old.VerticalSpeed;
                Player.fallDamage = _old.FallDamage;
                Player.crouchOnJump = _old.CrouchOnJump;
                GameManager.options.allowStickJump = _old.StickJump;

                //RevertScale();
            }

            //PlaySounds.Play(SoundName.Jetpack_Deactivate);
        }


        private void ApplyScale()
        {
            float scale = Mathf.Clamp(playerScale / 100f, 0.05f, 1);
            Player.local.transform.localScale = new Vector3(scale, scale, scale);
            Player.local.headOffsetTransform.localScale = new Vector3(scale, scale, scale);     // this helps, but the head slowly drifts forward

            //Player.local.handOffsetTransform.localScale = new Vector3(scale, scale, scale);       // transform and headOffsetTransform are pretty good, but handOffsetTransform makes the hands extra large as the scale gets small



            //Player.local.headOffsetTransform.localPosition =      // maybe


            Player.local.creature.morphology = new Morphology(_old.Morphology.eyesHeight * scale)
            {
                eyesHeight = _old.Morphology.eyesHeight * scale,
                eyesForward = _old.Morphology.eyesForward * scale,
                headHeight = _old.Morphology.headHeight * scale,
                headForward = _old.Morphology.headForward * scale,
                chestHeight = _old.Morphology.chestHeight * scale,
                spineHeight = _old.Morphology.spineHeight * scale,
                hipsHeight = _old.Morphology.hipsHeight * scale,
                armsSpacing = _old.Morphology.armsSpacing * scale,
                armsLength = _old.Morphology.armsLength * scale,
                armsHeight = _old.Morphology.armsHeight * scale,
                armsToEyesHeight = _old.Morphology.armsToEyesHeight * scale,
                height = _old.Morphology.height * scale,
                legsLength = _old.Morphology.legsLength * scale,
                legsSpacing = _old.Morphology.legsSpacing * scale,
                upperLegsHeight = _old.Morphology.upperLegsHeight * scale,
                lowerLegsHeight = _old.Morphology.lowerLegsHeight * scale,
                footHeight = _old.Morphology.footHeight * scale,
            };


            // I think the next step should be to look at the player model carefully.  See what all these transforms, creature, etc look like


            // This causes the hands to go nuts
            //Player.local.creature.SetHeight(_old.Height * scale);

        }
        private void RevertScale()
        {
            Player.local.transform.localScale = new Vector3(1, 1, 1);
            //Player.local.handOffsetTransform.localScale = new Vector3(1, 1, 1);
            Player.local.headOffsetTransform.localScale = new Vector3(1, 1, 1);

            //Player.local.creature.SetHeight(_old.Height);


            // TODO: _old.Morphology was set in activate flight, change this to some kind of startup event
            Player.local.creature.morphology = _old.Morphology;
        }

    }
}
