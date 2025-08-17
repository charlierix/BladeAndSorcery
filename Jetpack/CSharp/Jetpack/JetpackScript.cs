using Jetpack.DebugCode;
using Jetpack.FlightProcessing;
using Jetpack.InputWatchers;
using Jetpack.Models;
using Jetpack.Scanning;
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


// option to slowly pull yaw toward look direction


// separate out mod config ui settings from json settings (fine details of ground repel should be json)


// figure out how to change orientation of the player, maybe a combination of look direction and wrists


// make a way to virtually grab stationary items like trees, boulders and swing around


// make a spell that fires a "drone" put a visual of a stone of the player and have the drone fly around


namespace Jetpack
{
    // Source: https://github.com/sjankowskim/wings

    public class JetpackScript : ThunderScript
    {
        // https://kospy.github.io/BasSDK/Components/Guides/ModOptions/#how-do-i-use-modoptions

        #region Mod Options

        private const string CATEGORY_ACTIVATE = "Activation / Deactivation";
        private const string CATEGORY_OBSTACLEAVOIDANCE = "Obstacle Avoidance";
        private const string CATEGORY_FLIGHTPROPS = "Flight Properties";
        private const string CATEGORY_SOUNDS = "Sounds";        // TODO: add this
        private const string CATEGORY_SCALE = "Player Size";
        private const string CATEGORY_VISIBILITY = "Player Visibility";
        private const string CATEGORY_DEBUGDRAWING = "Debug Drawing";

        private const int ORDER_ACTIVATE = 1;
        private const int ORDER_OBSTACLEAVOIDANCE = 2;
        private const int ORDER_FLIGHTPROPS = 3;
        private const int ORDER_SOUNDS = 4;
        private const int ORDER_SCALE = 5;
        private const int ORDER_VISIBILITY = 6;
        private const int ORDER_DEBUGDRAWING = 7;

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

        // ******************** Obstacle Avoidance ********************


        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOption(name: "Should Repel Ground", tooltip: "Will push the player upward from the ground requiring deliberate down pressure on the thumstick to touch the ground", order = 0)]
        public static bool ShouldRepelGround = true;


        // for now, just treat this like a percent against the other accels
        // NOTE: this is currently ignored until the other fine tune props are figured out
        /// <summary>
        /// This isn't a simple accel.  It will only apply upward accel when velocity is downward.  There's also a dropoff
        /// distance based on player's size
        /// </summary>
        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Strength", tooltip: "CURRENTLY IGNORED - How strong the ground repel should be", order = 1)]
        [ModOptionFloatValues(0, 100, 1)]
        public static float RepelGroundStrength = 0;


        // figure out which of these to expose, or maybe a single slider that affects several at the same time (one for dist, and make strength directly affect the other values directly)

        // Distance
        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Max Distance", tooltip: "relative to height * scale, taken from foot pos", order = 2)]
        [ModOptionFloatValues(0, 1, 0.05f)]
        public static float RepelGround_MaxDistance = 0.25f;

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Vertical Speed Distance Mult", tooltip: "increases ground distance based on vertical speed down (this * speed)", order = 3)]
        [ModOptionFloatValues(0, 3, 0.1f)]
        public static float RepelGround_VertSpeedDistMult = 1;

        // Linear
        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Max Accel [linear]", tooltip: "a linear gradient where there is zero force at max distance and max force at zero distance", order = 4)]
        [ModOptionFloatValues(0, 6, 0.25f)]
        public static float RepelGround_Linear_MaxAccel = 2.5f;

        // 1/x
        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Max Accel [1/(cx)]", tooltip: "the distance is normalized, where x is 0 to 1", order = 5)]
        [ModOptionFloatValues(0, 18, 0.25f)]
        public static float RepelGround_Inverse_MaxAccel = 9;

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Inverse C [1/(cx)]", tooltip: "any value less than 4 is meaningless (plot it in desmos for easy visualization/manipulation)", order = 6)]
        [ModOptionFloatValues(4, 24, 0.25f)]
        public static float RepelGround_Inverse_C = 8;

        // 1/x^2
        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Max Accel [1/(cx)^2]", tooltip: "the distance is normalized, where x is 0 to 1", order = 7)]
        [ModOptionFloatValues(0, 40, 0.25f)]
        public static float RepelGround_InverseSqr_MaxAccel = 20;

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Inverse^2 C [1/(cx)^2]", tooltip: "any value less than 4 is meaningless (plot it in desmos for easy visualization/manipulation)", order = 8)]
        [ModOptionFloatValues(4, 80, 0.5f)]
        public static float RepelGround_InverseSqr_C = 40;

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Max Upward Speed", tooltip: "stop accelerating upward beyond this speed (to avoid excessive pop up speeds)", order = 9)]
        [ModOptionFloatValues(0, 1, 0.02f)]
        public static float RepelGround_UpSpeed_ZeroAccel = 0.1f;

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
        [ModOptionFloatValues(0, 600, 1f)]
        public static float playerScale = 100;

        [ModOptionCategory(CATEGORY_SCALE, ORDER_SCALE)]
        [ModOption(name: "Use Morphology", tooltip: "Whether to apply a scaled morphology -- needs to be true", order = 2)]
        public static bool ScaleSetMorphology = true;

        [ModOptionCategory(CATEGORY_SCALE, ORDER_SCALE)]
        [ModOption(name: "Set Ragdoll Scale", tooltip: "Whether to apply a scaled ragdoll -- needs to be false", order = 3)]
        public static bool ScaleSetRagdoll = false;

        public static ModOptionString[] scaleApplyButtonLabel = new[]
        {
            new ModOptionString("Apply Scale", "ApplyScale")
        };

        // NOTE: these OnClick functions always get called when the mod first loads
        private static bool _onApplyScale_called = false;

        [ModOptionCategory(CATEGORY_SCALE, ORDER_SCALE)]
        [ModOptionButton]
        [ModOption("Set scale to current settings", null, nameof(scaleApplyButtonLabel), order = 4)]
        public static void OnApplyScale(string value)
        {
            if (!_onApplyScale_called)
            {
                _onApplyScale_called = true;
                return;
            }

            ScaleAdjuster.ApplyScale(playerScale / 100, ScaleSetMorphology, ScaleSetRagdoll);
        }

        public static ModOptionString[] scaleRevertButtonLabel = new[]
        {
            new ModOptionString("Default Scale", "DefaultScale")
        };

        private static bool _onRevertScale_called = false;

        [ModOptionCategory(CATEGORY_SCALE, ORDER_SCALE)]
        [ModOptionButton]
        [ModOption("Put scale back to normal", null, nameof(scaleRevertButtonLabel), order = 5)]
        public static void OnRevertScale(string value)
        {
            if (!_onRevertScale_called)
            {
                _onRevertScale_called = true;
                return;
            }

            ScaleAdjuster.RevertScale();
        }

        // ******************** Player Visibility ********************

        public static ModOptionString[] visibilityInvisibleButtonLabel = new[]
        {
            new ModOptionString("Make Invisible", "MakeInvisible")
        };

        // NOTE: these OnClick functions always get called when the mod first loads
        private static bool _onMakeInvisible_called = false;

        [ModOptionCategory(CATEGORY_VISIBILITY, ORDER_VISIBILITY)]
        [ModOptionButton]
        [ModOption("Make Invisible", null, nameof(visibilityInvisibleButtonLabel), order = 4)]
        public static void OnMakeInvisible(string value)
        {
            if (!_onMakeInvisible_called)
            {
                _onMakeInvisible_called = true;
                return;
            }

            PlayerVisibility.MakeInvisible();
        }

        public static ModOptionString[] visibilityVisibleButtonLabel = new[]
        {
            new ModOptionString("Make Visible", "MakeVisible")
        };

        private static bool _onMakeVisible_called = false;

        [ModOptionCategory(CATEGORY_VISIBILITY, ORDER_VISIBILITY)]
        [ModOptionButton]
        [ModOption("Make Visible", null, nameof(visibilityVisibleButtonLabel), order = 5)]
        public static void OnMakeVisible(string value)
        {
            if (!_onMakeVisible_called)
            {
                _onMakeVisible_called = true;
                return;
            }

            PlayerVisibility.MakeVisible();
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

        private RayCastStorage _raycast_storage = null;
        private FlightTransitionWatcher _transitions = null;
        private FlightJetpack _flight_jetpack = null;

        private DebugVisuals _debugVisuals = new DebugVisuals();
        private VisualizePlayerPoints _visualizePlayerPoints = new VisualizePlayerPoints();
        private ScaleAdjuster _scaleAdjuster = new ScaleAdjuster();

        private bool _isPlayerSpawned = false;

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);

            _raycast_storage = new RayCastStorage();
            _transitions = new FlightTransitionWatcher();
            _flight_jetpack = new FlightJetpack(_raycast_storage);

            //MaterialShaderFinder.Report();

            Player.onSpawn += Player_onSpawn;
            Player.onDespawn += Player_onDespawn;
        }

        private void Player_onSpawn(Player player)
        {
            _isPlayerSpawned = true;

            if (Player.local != null)
            {
                Debug.Log("showing morphology");
                Player.local.showMorphology = true;
            }
        }
        private void Player_onDespawn(Player player)
        {
            _isPlayerSpawned = false;

            _debugVisuals.RemoveVisuals();
            _visualizePlayerPoints.Clear();

            DeactivateFly();
        }

        public override void ScriptUpdate()
        {
            base.ScriptUpdate();

            if (!_isPlayerSpawned || Player.local == null)
                return;

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


            // TODO: look at scale/visibility settings and apply if checked
            // though it may be better to leave scale alone and turn off collision detection.  make the player hidden and show a small avatar instead



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
