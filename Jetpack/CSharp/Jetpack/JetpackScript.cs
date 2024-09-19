using Jetpack.DebugCode;
using Jetpack.FlightProcessing;
using Jetpack.InputWatchers;
using Jetpack.Models;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ThunderRoad;
using UnityEngine;



// TODOS:


// Options to reduce accel if in confined space (probably just a checkbox, the raycast dist and % reduction can probably be hardcoded - it may not be linear)
//  Physics.OverlapSphere
//  Physics.SphereCastAll
//  Physics.Raycast


// activate sound sounds more like a fireball than flight


// Put scale logic as a separate thing from activate/deactivate flight
//  create extra sliders and checkboxes
//  make an apply scale button and back to default button


// instead of a single flight mode for the whole mod, create a few modes that can have activation gestures assigned
//  jetpack - default to up activation, double jump would be a good alternative
//      has some gravity, basic flight
//
//  winged - some combination of bird and airplane
//
//  iron man - thruster based at low speed, winged at higher speeds
//
//  fpv drone - try to emulate a drone with thumbstick inputs



namespace Jetpack
{
    // Source: https://github.com/sjankowskim/wings

    public class JetpackScript : ThunderScript
    {
        // https://kospy.github.io/BasSDK/Components/Guides/ModOptions/#how-do-i-use-modoptions

        #region Mod Options

        private const string CATEGORY_ACTIVATE = "Activation / Deactivation";
        private const string CATEGORY_FLIGHTPROPS = "Flight Properties";
        private const string CATEGORY_SOUNDS = "Sounds";        // TODO: add this
        private const string CATEGORY_SCALE = "Player Size";
        private const string CATEGORY_DEBUGDRAWING = "Debug Drawing";

        private const int ORDER_ACTIVATE = 1;
        private const int ORDER_FLIGHTPROPS = 2;
        private const int ORDER_SOUNDS = 3;
        private const int ORDER_SCALE = 4;
        private const int ORDER_DEBUGDRAWING = 5;

        //[ModOptionTextDisplay("description of section", null)]
        //[ModOption("Info")]
        //private static void label1(string value) { }

        [ModOption(name: "Use Jetpack Mod", tooltip: "Turns on/off the Jetpack mod")]
        public static bool UseJetpackMod = true;

        // ******************** Activation / Deactivation ********************

        public static ModOptionString[] FlightActivation_Options = new[]
        {
            new ModOptionString("Hold Up (right stick)", null, FlightActivationType.HoldUp.ToString()),
            //new ModOptionString("Hold Jump", null, FlightActivationType.HoldJump.ToString()),     // TODO: check the difference between jump on click and jump on up
            //new ModOptionString("Double Jump", null, FlightActivationType.DoubleJump.ToString()),
            new ModOptionString("Double Click Thumbpad", null, FlightActivationType.DoubleClick_Thumbpad.ToString()),
            new ModOptionString("Hold The Bird", null, FlightActivationType.HoldBird.ToString()),     // 🖕
            new ModOptionString("Hold Peace Sign", null, FlightActivationType.HoldPeace.ToString()),      // ✌️
            new ModOptionString("Hold Devil Horns", null, FlightActivationType.HoldDevilHorns.ToString()),        // 🤘
            new ModOptionString("Hold Rock On", null, FlightActivationType.HoldRockOn.ToString()),        // 🤟
        };

        private static string _flightActivation = FlightActivationType.HoldUp.ToString();
        private static FlightActivationType _flightActivation_cast = FlightActivationType.HoldUp;

        [ModOptionCategory(CATEGORY_ACTIVATE, ORDER_ACTIVATE)]
        [ModOption(name: "Flight Activation/Deactivation", tooltip: "How to activate flight (some options will also be used to deactivate)\n\nThe hold options are for controllers that have finger tracking", valueSourceName: nameof(FlightActivation_Options), order = 0)]
        public static string FlightActivation
        {
            get
            {
                return _flightActivation;
            }
            set
            {
                _flightActivation = value;

                if (!Enum.TryParse<FlightActivationType>(value, out _flightActivation_cast))
                    Debug.Log($"Couldn't parse FlightActivationType: {value}.  Leaving it as {_flightActivation_cast}");
            }
        }

        [ModOptionCategory(CATEGORY_ACTIVATE, ORDER_ACTIVATE)]
        [ModOption(name: "Stop flying on ground", tooltip: "Whether to stop flight when on the ground", order = 1)]
        public static bool DeactivateOnGround = true;

        [ModOptionCategory(CATEGORY_ACTIVATE, ORDER_ACTIVATE)]
        [ModOption(name: "Require Both Hands", tooltip: "Options that are double click or gestures can be required to be done at the same time by both hands or just one\n\nSingle hand is easier but may cause misreads", order = 2)]
        public static bool RequireBothHands = true;

        // ******************** Flight Properties ********************

        [ModOptionCategory(CATEGORY_FLIGHTPROPS, ORDER_FLIGHTPROPS)]
        [ModOptionSlider]
        [ModOption(name: "Horizontal Accel", tooltip: "How hard to accelerate horizontally", order = 0)]
        [ModOptionFloatValues(0, 24, 0.25f)]
        public static float HorizontalAccel = 9;

        [ModOptionCategory(CATEGORY_FLIGHTPROPS, ORDER_FLIGHTPROPS)]
        [ModOptionSlider]
        [ModOption(name: "Vertical Accel", tooltip: "How hard to accelerate vertically", order = 1)]
        [ModOptionFloatValues(0, 12, 0.25f)]
        public static float VerticalAccel = 6;

        [ModOptionCategory(CATEGORY_FLIGHTPROPS, ORDER_FLIGHTPROPS)]
        [ModOptionSlider]
        [ModOption(name: "Drag", tooltip: "Wind resistance", order = 2)]
        [ModOptionFloatValues(0, 2, 0.05f)]
        public static float Drag = 0.9f;

        [ModOptionCategory(CATEGORY_FLIGHTPROPS, ORDER_FLIGHTPROPS)]
        [ModOptionSlider]
        [ModOption(name: "Gravity", tooltip: "0 is no gravity.  9.8 is standard", order = 3)]
        [ModOptionFloatValues(0, 18, 0.1f)]
        public static float GravitySetting = 0f;

        // ******************** Player Size ********************

        [ModOptionCategory(CATEGORY_SCALE, ORDER_SCALE)]
        [ModOptionSlider]
        [ModOption(name: "Player Size %", tooltip: "Can shrink the player so there is more room to fly", order = 1)]
        [ModOptionFloatValues(0, 300, 1f)]
        public static float playerScale = 100;

        [ModOptionCategory(CATEGORY_SCALE, ORDER_SCALE)]
        [ModOption(name: "Use Morphology", tooltip: "Whether to apply a scaled morphology -- needs to be true", order = 2)]
        public static bool ScaleSetMorphology = true;

        [ModOptionCategory(CATEGORY_SCALE, ORDER_SCALE)]
        [ModOption(name: "Set Ragdoll Scale", tooltip: "Whether to apply a scaled ragdoll -- needs to be false", order = 3)]
        public static bool ScaleSetRagdoll = false;

        // NOTE: these OnClick functions always get called when the mod first loads
        public static ModOptionString[] scaleApplyButtonLabel = new[]
        {
            new ModOptionString("Apply Scale", "ApplyScale")
        };

        [ModOptionCategory(CATEGORY_SCALE, ORDER_SCALE)]
        [ModOptionButton]
        [ModOption("Set scale to current settings", null, nameof(scaleApplyButtonLabel), order = 4)]
        public static void OnApplyScale(string value)
        {
            ScaleAdjuster.ApplyScale(playerScale / 100, ScaleSetMorphology, ScaleSetRagdoll);
        }

        public static ModOptionString[] scaleRevertButtonLabel = new[]
        {
            new ModOptionString("Default Scale", "DefaultScale")
        };

        [ModOptionCategory(CATEGORY_SCALE, ORDER_SCALE)]
        [ModOptionButton]
        [ModOption("Put scale back to normal", null, nameof(scaleRevertButtonLabel), order = 5)]
        public static void OnRevertScale(string value)
        {
            ScaleAdjuster.RevertScale();
        }

        // ******************** Debug Drawing ********************

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Visualize Player Points", tooltip: "Shows points/lines on various transforms of the player avatar", order = 1)]
        public static bool VisualizePlayerPoints = false;

        #endregion

        // PRE-FLIGHT DATA
        private FlightData _old = null;

        // FLIGHT DATA
        private Locomotion _loco = null;
        private bool _isFlying = false;
        private bool _markedToFly = false;      // will fly once not grounded
        private float _last_applied_drag = -1;

        private FlightTransitionWatcher _transitions = new FlightTransitionWatcher();
        private FlightJetpack _flight_jetpack = new FlightJetpack();

        private DebugVisuals _debugVisuals = new DebugVisuals();
        private VisualizePlayerPoints _visualizePlayerPoints = new VisualizePlayerPoints();
        private ScaleAdjuster _scaleAdjuster = new ScaleAdjuster();

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);

            MaterialShaderFinder.Report();
        }
        public override void ScriptUpdate()
        {
            base.ScriptUpdate();

            _visualizePlayerPoints.Update(VisualizePlayerPoints, playerScale / 100);

            bool should_switch = _transitions.Update(_flightActivation_cast, RequireBothHands, DeactivateOnGround, _isFlying);

            if (should_switch)
            {
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

            if (_isFlying && DeactivateOnGround && Player.local.locomotion.isGrounded)
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
                    _flight_jetpack.Update(Drag, HorizontalAccel, VerticalAccel, GravitySetting);
            }
            else
            {
                _isFlying = false;
            }
        }

        private void ActivateFly()
        {
            _markedToFly = false;

            if (!UseJetpackMod)
            {
                Debug.Log("ActivateFly() called, but mod is disabled");
                return;
            }

            _isFlying = true;

            _flight_jetpack.Activate(Drag);

            _debugVisuals.AddVisuals();

            //PlaySounds.Play(SoundName.Jetpack_Activate);
        }
        private void DeactivateFly()
        {
            _isFlying = false;

            _flight_jetpack.Deactivate();

            //PlaySounds.Play(SoundName.Jetpack_Deactivate);
        }
    }
}
