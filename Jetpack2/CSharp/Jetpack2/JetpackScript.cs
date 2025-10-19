using Jetpack2.DebugCode;
using Jetpack2.FlightProcessing;
using Jetpack2.InputWatchers;
using Jetpack2.Models;
using Jetpack2.Scanning;
using PerfectlyNormalBaS;
using System;
using System.IO;
using System.Reflection;
using ThunderRoad;
using UnityEngine;



// TODOS:
// have floating orbs that make it easy to toggle rotate to looks.  digging through the menu is tedious, this may
// be a decision to toggle for only a small amount of time


// make a class that rotates to velocity


// have an option to muffle accels when in odd orientations.  especially when indoors


// have an option for gravity to be relative to player's feet direction


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


// separate out mod config ui settings from json settings (fine details of ground repel should be json)


// make a way to virtually grab stationary items like trees, boulders and swing around


// make a spell that fires a "drone" put a visual of a stone of the player and have the drone fly around


namespace Jetpack2
{
    // Source: https://github.com/sjankowskim/wings

    public class JetpackScript : ThunderScript
    {
        // https://kospy.github.io/BasSDK/Components/Guides/ModOptions/#how-do-i-use-modoptions


        private const string CATEGORY_ACTIVATE = "Activation / Deactivation";
        private const string CATEGORY_TOGGLEBEHAVIORS = "Toggle Behaviors";
        private const string CATEGORY_FLIGHTPROPS = "Flight Properties";

        private const string CATEGORY_ROTATELOOK_GAZEBUFFER = "Rotate Toward Look - Gaze Buffer";
        private const string CATEGORY_ROTATELOOK_CAPACITOR = "Rotate Toward Look - Capacitor";
        private const string CATEGORY_ROTATELOOK_LOOKZONES = "Rotate Toward Look - Look Zones";
        private const string CATEGORY_ROTATELOOK_IK = "Rotate Toward Look - IK Rig";

        private const string CATEGORY_SOUNDS = "Sounds";        // TODO: add this
        private const string CATEGORY_SCALE = "Player Size";
        private const string CATEGORY_VISIBILITY = "Player Visibility";
        private const string CATEGORY_DEBUGDRAWING = "Debug Drawing";


        private const int ORDER_ACTIVATE = 1;
        private const int ORDER_TOGGLEBEHAVIORS = 2;
        private const int ORDER_FLIGHTPROPS = 3;

        private const int ORDER_ROTATELOOK_GAZEBUFFER = 4;
        private const int ORDER_ROTATELOOK_CAPACITOR = 5;
        private const int ORDER_ROTATELOOK_LOOKZONES = 6;
        private const int ORDER_ROTATELOOK_IK = 7;

        private const int ORDER_SOUNDS = 50;
        private const int ORDER_SCALE = 51;
        private const int ORDER_VISIBILITY = 52;
        private const int ORDER_DEBUGDRAWING = 53;




        private const string CATEGORY_CONFINEDAREA = "Confined Area";
        private const string CATEGORY_OBSTACLEAVOIDANCE = "Obstacle Avoidance";
        private const string CATEGORY_REPELGROUND = "Repel Ground";
        private const string CATEGORY_GAZEBUFFER = "Gaze Buffer";
        private const string CATEGORY_LOOKYAW = "Yaw Toward Look (old)";
        private const string CATEGORY_LOOKYAW2 = "Yaw Toward Look";
        private const string CATEGORY_ROTATELOOK = "Rotate Toward Look";

        private const int ORDER_CONFINEDAREA = 22;
        private const int ORDER_OBSTACLEAVOIDANCE = 23;
        private const int ORDER_REPELGROUND = 24;
        private const int ORDER_GAZEBUFFER = 27;
        private const int ORDER_LOOKYAW = 28;
        private const int ORDER_LOOKYAW2 = 29;
        private const int ORDER_ROTATELOOK = 30;


        #region Mod Options -- ORIG


        //[ModOptionTextDisplay("description of section", null)]
        //[ModOption("Info")]
        //private static void label1(string value) { }




        #region Confined Area

        // these should be json

        [ModOptionCategory(CATEGORY_CONFINEDAREA, ORDER_CONFINEDAREA)]
        [ModOptionSlider]
        [ModOption(name: "Ray Length", tooltip: "How far the rays should go", order = 1)]
        [ModOptionFloatValues(6, 36, 1)]
        public static float ConfinedArea_RayLength = 18;

        [ModOptionCategory(CATEGORY_CONFINEDAREA, ORDER_CONFINEDAREA)]
        [ModOptionSlider]
        [ModOption(name: "Falloff Power", tooltip: "Ray hit disance / Max Distance is run through a bell curve dropoff.  Higher power makes it drop off faster", order = 3)]
        [ModOptionFloatValues(0f, 3f, 0.05f)]
        public static float ConfinedArea_FalloffPower = 1f;

        #endregion
        #region Obstacle Avoidance

        // put in json

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Ellipse Point Angle", tooltip: "Angle for the point between major and minor axis", order = 1)]
        [ModOptionFloatValues(0, 90, 1)]
        public static float ObstAvoid_EllipsePointAngle = 55;

        // Don't bother with inward angle
        //[ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        //[ModOptionSlider]
        //[ModOption(name: "Ellipse Ray Angle (inward)", tooltip: "At each point along the perimiter of the ellipse, there will be two diverging rays at an angle (one toward interior, one away from ellipse)", order = 2)]
        //[ModOptionFloatValues(0, 3, 0.1f)]
        //public static float ObstAvoid_EllipseRayAngleIn = 0;

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Ellipse Ray Angle (outward)", tooltip: "At each point along the perimiter of the ellipse, there will be two diverging rays at an angle (one toward interior, one away from ellipse)", order = 2)]
        [ModOptionFloatValues(0, 24, 1)]
        public static float ObstAvoid_EllipseRayAngleOut = 12;

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Ellipse Ray Angle (yaw)", tooltip: "Max random yaw of each ray source each frame", order = 3)]
        [ModOptionFloatValues(0, 45, 1)]
        public static float ObstAvoid_EllipseRayAngleYaw = 8;

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Ellipse Ray Angle (pitch)", tooltip: "Max random pitch of each ray source each frame", order = 4)]
        [ModOptionFloatValues(0, 45, 1)]
        public static float ObstAvoid_EllipseRayAnglePitch = 12;

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Ray Distance Multiplier", tooltip: "Length of the ray cast (velocity * mult)", order = 5)]
        [ModOptionFloatValues(0, 6, 0.05f)]
        public static float ObstAvoid_RayDistMult = 3f;

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Analyze Max Distance", tooltip: "Accel is zero beyond this distance", order = 6)]
        [ModOptionFloatValues(0, 24, 0.5f)]
        public static float ObstAvoid_Analyze_MaxDist = 9f;

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Analyze Dropoff Power", tooltip: "Percent Dropoff is 1-x^n", order = 7)]
        [ModOptionFloatValues(1, 6, 0.25f)]
        public static float ObstAvoid_Analyze_DropoffPow = 3f;

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Analyze Speed Percent Mult", tooltip: "Accel is reduced based on speed * this", order = 8)]
        [ModOptionFloatValues(0, 2, 0.05f)]
        public static float ObstAvoid_Analyze_SpeedMult = 0.75f;

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Don't Fight Dot Threshold (start)", tooltip: "When dot product of input and accel is negative, this is how negative the dot is before accel starts getting cancelled out", order = 9)]
        [ModOptionFloatValues(0, 1, 0.05f)]
        public static float ObstAvoid_DontFight_DotThreshold_Start = 0.1f;

        [ModOptionCategory(CATEGORY_OBSTACLEAVOIDANCE, ORDER_OBSTACLEAVOIDANCE)]
        [ModOptionSlider]
        [ModOption(name: "Don't Fight Dot Threshold (full block)", tooltip: "When dot product of input and accel is negative, this is how negative the dot is when accel is fully cancelled out", order = 9)]
        [ModOptionFloatValues(0, 1, 0.05f)]
        public static float ObstAvoid_DontFight_DotThreshold_Full = 0.7f;

        #endregion
        #region Repel Ground

        // put in json


        // for now, just treat this like a percent against the other accels
        // NOTE: this is currently ignored until the other fine tune props are figured out
        /// <summary>
        /// This isn't a simple accel.  It will only apply upward accel when velocity is downward.  There's also a dropoff
        /// distance based on player's size
        /// </summary>
        [ModOptionCategory(CATEGORY_REPELGROUND, ORDER_REPELGROUND)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Strength", tooltip: "CURRENTLY IGNORED - How strong the ground repel should be", order = 1)]
        [ModOptionFloatValues(0, 100, 1)]
        public static float RepelGroundStrength = 0;


        // figure out which of these to expose, or maybe a single slider that affects several at the same time (one for dist, and make strength directly affect the other values directly)

        // Distance
        [ModOptionCategory(CATEGORY_REPELGROUND, ORDER_REPELGROUND)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Max Distance", tooltip: "relative to height * scale, taken from foot pos", order = 2)]
        [ModOptionFloatValues(0, 1, 0.05f)]
        public static float RepelGround_MaxDistance = 0.25f;

        [ModOptionCategory(CATEGORY_REPELGROUND, ORDER_REPELGROUND)]
        [ModOptionSlider]
        [ModOption(name: "Horizontal Speed Distance Mult", tooltip: "increases ground distance based on horizontal speed (this * speed)", order = 3)]
        [ModOptionFloatValues(0, 2, 0.05f)]
        public static float RepelGround_HorzSpeedDistMult = 0.25f;

        [ModOptionCategory(CATEGORY_REPELGROUND, ORDER_REPELGROUND)]
        [ModOptionSlider]
        [ModOption(name: "Vertical Speed Distance Mult", tooltip: "increases ground distance based on vertical speed down (this * speed)", order = 4)]
        [ModOptionFloatValues(0, 3, 0.1f)]
        public static float RepelGround_VertSpeedDistMult = 1;

        // Linear
        [ModOptionCategory(CATEGORY_REPELGROUND, ORDER_REPELGROUND)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Max Accel [linear]", tooltip: "a linear gradient where there is zero force at max distance and max force at zero distance", order = 5)]
        [ModOptionFloatValues(0, 6, 0.25f)]
        public static float RepelGround_Linear_MaxAccel = 2.5f;

        // 1/x
        [ModOptionCategory(CATEGORY_REPELGROUND, ORDER_REPELGROUND)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Max Accel [1/(cx)]", tooltip: "the distance is normalized, where x is 0 to 1", order = 6)]
        [ModOptionFloatValues(0, 18, 0.25f)]
        public static float RepelGround_Inverse_MaxAccel = 9;

        [ModOptionCategory(CATEGORY_REPELGROUND, ORDER_REPELGROUND)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Inverse C [1/(cx)]", tooltip: "any value less than 4 is meaningless (plot it in desmos for easy visualization/manipulation)", order = 7)]
        [ModOptionFloatValues(4, 24, 0.25f)]
        public static float RepelGround_Inverse_C = 8;

        // 1/x^2
        [ModOptionCategory(CATEGORY_REPELGROUND, ORDER_REPELGROUND)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Max Accel [1/(cx)^2]", tooltip: "the distance is normalized, where x is 0 to 1", order = 8)]
        [ModOptionFloatValues(0, 40, 0.25f)]
        public static float RepelGround_InverseSqr_MaxAccel = 20;

        [ModOptionCategory(CATEGORY_REPELGROUND, ORDER_REPELGROUND)]
        [ModOptionSlider]
        [ModOption(name: "Repel Ground Inverse^2 C [1/(cx)^2]", tooltip: "any value less than 4 is meaningless (plot it in desmos for easy visualization/manipulation)", order = 9)]
        [ModOptionFloatValues(4, 80, 0.5f)]
        public static float RepelGround_InverseSqr_C = 40;

        [ModOptionCategory(CATEGORY_REPELGROUND, ORDER_REPELGROUND)]
        [ModOptionSlider]
        [ModOption(name: "Max Upward Speed", tooltip: "stop accelerating upward beyond this speed (to avoid excessive pop up speeds)", order = 10)]
        [ModOptionFloatValues(0, 1, 0.02f)]
        public static float RepelGround_UpSpeed_ZeroAccel = 0.1f;

        #endregion
        #region dead zone

        // put in json

        // these starts should be a percent or fixed offset from full

        [ModOptionCategory(CATEGORY_LOOKYAW2, ORDER_LOOKYAW2)]
        [ModOptionSlider]
        [ModOption(name: "Dead Zone Dot Product (start)", tooltip: "How far from center before it starts turning at max rate", order = 2)]
        [ModOptionFloatValues(0.9f, 1, 0.001f)]
        public static float YawToLook2_DeadZone_Start = 0.98f;

        [ModOptionCategory(CATEGORY_ROTATELOOK, ORDER_ROTATELOOK)]
        [ModOptionSlider]
        [ModOption(name: "Dead Zone Dot Product - roll (start)", tooltip: "How far from center before it starts turning at max rate", order = 3)]
        [ModOptionFloatValues(0.9f, 1, 0.001f)]
        public static float RotToLook_DeadZone_Roll_Start = 0.985f;

        [ModOptionCategory(CATEGORY_ROTATELOOK, ORDER_ROTATELOOK)]
        [ModOptionSlider]
        [ModOption(name: "Dead Zone Dot Product - pitch (start)", tooltip: "How far from center before it starts turning at max rate", order = 5)]
        [ModOptionFloatValues(0.9f, 1, 0.001f)]
        public static float RotToLook_DeadZone_Pitch_Start = 0.98f;

        #endregion





        #endregion
        #region Mod Options

        [ModOption(name: "Use Jetpack Mod", tooltip: "Turns on/off the Jetpack mod")]
        public static bool UseJetpackMod = true;

        #region Activation / Deactivation

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

        #endregion

        #region Toggle Behaviors

        [ModOptionCategory(CATEGORY_TOGGLEBEHAVIORS, ORDER_TOGGLEBEHAVIORS)]
        [ModOption(name: "Should Detect Confined Area", tooltip: "Slows down accelerations when in tight spaces", order = 0)]
        public static bool ShouldDetectConfinedArea = true;

        // keep the idea of a slider, but use more sensible min/max range (like 0 to 100)

        [ModOptionCategory(CATEGORY_TOGGLEBEHAVIORS, ORDER_TOGGLEBEHAVIORS)]
        [ModOptionSlider]
        [ModOption(name: "Gain Factor", tooltip: "Multiplied by delta time.  Larger values adjust final blocked percent faster", order = 1)]
        [ModOptionFloatValues(0.5f, 4f, 0.05f)]
        public static float ConfinedArea_GainFactor = 1.75f;


        [ModOptionCategory(CATEGORY_TOGGLEBEHAVIORS, ORDER_TOGGLEBEHAVIORS)]
        [ModOption(name: "Should Avoid Obstacles", tooltip: "Will push the player away from obstacles when moving toward them (no effect if stopped near obstacles)", order = 2)]
        public static bool ShouldAvoidObstacles = true;

        [ModOptionCategory(CATEGORY_TOGGLEBEHAVIORS, ORDER_TOGGLEBEHAVIORS)]
        [ModOption(name: "Should Repel Ground", tooltip: "Will push the player upward from the ground requiring deliberate down pressure on the thumstick to touch the ground", order = 3)]
        public static bool ShouldRepelGround = true;

        [ModOptionCategory(CATEGORY_TOGGLEBEHAVIORS, ORDER_TOGGLEBEHAVIORS)]
        [ModOption(name: "Should Rotate Toward Look - yaw", tooltip: "Will rotate the player toward the direction looking", order = 4)]
        public static bool ShouldRotateToLook_Yaw = false;

        [ModOptionCategory(CATEGORY_TOGGLEBEHAVIORS, ORDER_TOGGLEBEHAVIORS)]
        [ModOption(name: "Should Rotate Toward Look - pitch", tooltip: "Will rotate the player toward the direction looking", order = 5)]
        public static bool ShouldRotateToLook_Pitch = false;

        [ModOptionCategory(CATEGORY_TOGGLEBEHAVIORS, ORDER_TOGGLEBEHAVIORS)]
        [ModOption(name: "Should Rotate Toward Look - roll", tooltip: "Will rotate the player toward the direction looking", order = 6)]
        public static bool ShouldRotateToLook_Roll = false;

        [ModOptionCategory(CATEGORY_TOGGLEBEHAVIORS, ORDER_TOGGLEBEHAVIORS)]
        [ModOptionSlider]
        [ModOption(name: "rot to look: Max Turn Rate Degrees", tooltip: "Degrees per second", order = 7)]
        [ModOptionFloatValues(1, 360, 1f)]
        public static float RotateToLook_TurnRate = 60;

        #endregion

        #region Flight Properties

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

        #endregion

        #region Rotate To Look - gaze buffer


        // gaze buffer can be reduced to buffer time, looseness of confidence

        // need a snap look detector (large fast sweep away from body forward)


        [ModOptionCategory(CATEGORY_ROTATELOOK_GAZEBUFFER, ORDER_ROTATELOOK_GAZEBUFFER)]
        [ModOptionSlider]
        [ModOption(name: "Buffer Max Time (seconds)", tooltip: "How long to keep previous look directions", order = 1)]
        [ModOptionFloatValues(0, 3, 0.01f)]
        public static float GazeBuffer_MaxSeconds = 1.1f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_GAZEBUFFER, ORDER_ROTATELOOK_GAZEBUFFER)]
        [ModOptionSlider]
        [ModOption(name: "Buffer Max Count", tooltip: "Max size of buffer", order = 2)]
        [ModOptionIntValues(0, 500, 20)]
        public static int GazeBuffer_MaxCount = 60;

        [ModOptionCategory(CATEGORY_ROTATELOOK_GAZEBUFFER, ORDER_ROTATELOOK_GAZEBUFFER)]
        [ModOptionSlider]
        [ModOption(name: "Gaze Confidence (direct)", tooltip: "The average of look directions.  This is the min confidence before the look direction is considered", order = 3)]
        [ModOptionFloatValues(0, 1, 0.01f)]
        public static float GazeBuffer_GazeConfidence_Direct = 0.7f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_GAZEBUFFER, ORDER_ROTATELOOK_GAZEBUFFER)]
        [ModOptionSlider]
        [ModOption(name: "Gaze Confidence (offset)", tooltip: "The average of look directions.  This is the min confidence before the look direction is considered", order = 4)]
        [ModOptionFloatValues(0, 1, 0.01f)]
        public static float GazeBuffer_GazeConfidence_Offset = 0.7f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_GAZEBUFFER, ORDER_ROTATELOOK_GAZEBUFFER)]
        [ModOptionSlider]
        [ModOption(name: "Gaze Confidence (target)", tooltip: "The average of look directions.  This is the min confidence before the look direction is considered", order = 5)]
        [ModOptionFloatValues(0, 1, 0.01f)]
        public static float GazeBuffer_GazeConfidence_Target = 0.9f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_GAZEBUFFER, ORDER_ROTATELOOK_GAZEBUFFER)]
        [ModOptionSlider]
        [ModOption(name: "Gaze Confidence StdDev Decay Mult", tooltip: "Does an exponential decay against standard deviation of dot products with avg.  Large number makes it require tighter groupings", order = 6)]
        [ModOptionFloatValues(1, 300, 1)]
        public static float GazeBuffer_Confidence_StdDev_DecayMult = 100;


        // NOTE: after running the numbers, this shouldn't be an independent slider, it should be based on spacing:
        // MinAllowedDistance = -0.5721 * GazeBuffer_GazeTarget_SpacingRatio + 0.8997
        //
        // This will produce between 1 and 2 spheres when calling GetRelevantSphereOrigins.  Radius doesn't have much influence
        //[ModOptionCategory(CATEGORY_ROTATELOOK_GAZEBUFFER, ORDER_ROTATELOOK_GAZEBUFFER)]
        //[ModOptionSlider]
        //[ModOption(name: "Gaze Sphere Min Dist to Surface", tooltip: "The minimum allowed distance between head position and the surface of a sphere", order = 7)]
        //[ModOptionFloatValues(0.25f, 2.5f, 0.05f)]
        //public static float GazeBuffer_GazeTarget_MinAllowedDistance = 1f;


        //[ModOptionCategory(CATEGORY_ROTATELOOK_GAZEBUFFER, ORDER_ROTATELOOK_GAZEBUFFER)]
        //[ModOptionSlider]
        //[ModOption(name: "Gaze Sphere Spacing Ratio of Radius", tooltip: "How far apart spheres should be spaced", order = 8)]
        //[ModOptionFloatValues(0.65f, 0.85f, 0.01f)]
        //public static float GazeBuffer_GazeTarget_SpacingRatio = 0.75f;


        [ModOptionCategory(CATEGORY_ROTATELOOK_GAZEBUFFER, ORDER_ROTATELOOK_GAZEBUFFER)]
        [ModOptionSlider]
        [ModOption(name: "Gaze Sphere Min Radius", tooltip: "The smallest radius (when speed is zero)", order = 9)]
        [ModOptionFloatValues(2, 9, 0.25f)]
        public static float GazeBuffer_GazeTarget_RadiiForSpeed_Min = 5f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_GAZEBUFFER, ORDER_ROTATELOOK_GAZEBUFFER)]
        [ModOptionSlider]
        [ModOption(name: "Gaze Sphere Radius Speed Ratio", tooltip: "Speed to base radius scaling factor", order = 10)]
        [ModOptionFloatValues(0.1f, 2, 0.05f)]
        public static float GazeBuffer_GazeTarget_RadiiForSpeed_SpeedRatio = 0.75f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_GAZEBUFFER, ORDER_ROTATELOOK_GAZEBUFFER)]
        [ModOptionSlider]
        [ModOption(name: "Gaze Sphere Radius Step Mult", tooltip: "The size of the next largest radius (multiplied by base radius)", order = 11)]
        [ModOptionFloatValues(1.5f, 5, 0.1f)]
        public static float GazeBuffer_GazeTarget_RadiiForSpeed_StepMult = 3;

        #endregion
        #region Rotate To Look - capacitor

        [ModOptionCategory(CATEGORY_ROTATELOOK_CAPACITOR, ORDER_ROTATELOOK_CAPACITOR)]
        [ModOptionSlider]
        [ModOption(name: "Capacitor Charging Dot", tooltip: "Capacitor increases above this (forward dot look)", order = 1)]
        [ModOptionFloatValues(0, 1, 0.01f)]
        public static float RotToLook_Capacitor_UpperDot = 0.95f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_CAPACITOR, ORDER_ROTATELOOK_CAPACITOR)]
        [ModOptionSlider]
        [ModOption(name: "Capacitor Start Discharge Dot", tooltip: "Capacitor starts discharging below this (forward dot look)", order = 2)]
        [ModOptionFloatValues(0, 1, 0.01f)]
        public static float RotToLook_Capacitor_LowerDot = 0.9f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_CAPACITOR, ORDER_ROTATELOOK_CAPACITOR)]
        [ModOptionSlider]
        [ModOption(name: "Capacitor Full Discharge Dot", tooltip: "Capacitor discharges fastest below this (forward dot look)", order = 3)]
        [ModOptionFloatValues(0, 1, 0.01f)]
        public static float RotToLook_Capacitor_BottomDot = 0.75f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_CAPACITOR, ORDER_ROTATELOOK_CAPACITOR)]
        [ModOptionSlider]
        [ModOption(name: "Capacitor Charge Speed", tooltip: "Charge per second when look diff is above upper dot", order = 4)]
        [ModOptionFloatValues(0, 8, 0.05f)]
        public static float RotToLook_Capacitor_ChargeSpeed = 0.5f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_CAPACITOR, ORDER_ROTATELOOK_CAPACITOR)]
        [ModOptionSlider]
        [ModOption(name: "Capacitor Charge Power", tooltip: "Look diff beween upper dot and one ramps up by this power", order = 5)]
        [ModOptionFloatValues(1, 6, 0.1f)]
        public static float RotToLook_Capacitor_ChargePower = 2;

        [ModOptionCategory(CATEGORY_ROTATELOOK_CAPACITOR, ORDER_ROTATELOOK_CAPACITOR)]
        [ModOptionSlider]
        [ModOption(name: "Capacitor Discharge Speed", tooltip: "Charge per second when diff is below lower dot", order = 6)]
        [ModOptionFloatValues(0, 8, 0.05f)]
        public static float RotToLook_Capacitor_DischargeSpeed = 2f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_CAPACITOR, ORDER_ROTATELOOK_CAPACITOR)]
        [ModOptionSlider]
        [ModOption(name: "Capacitor Discharge Power", tooltip: "Look diff between lower dot and zero ramps up by this power", order = 7)]
        [ModOptionFloatValues(1, 6, 0.1f)]
        public static float RotToLook_Capacitor_DischargePower = 2;

        #endregion
        #region Rotate To Look - look zones

        // TODO: these dot products need to be presented differently in the sliders.  the game rounds to 2 decimals when displaying value

        // deadzone initial
        [ModOptionCategory(CATEGORY_ROTATELOOK_LOOKZONES, ORDER_ROTATELOOK_LOOKZONES)]
        [ModOptionSlider]
        [ModOption(name: "Dead Zone Dot Product - yaw", tooltip: "How far from center where there is no turning", order = 1)]
        [ModOptionFloatValues(0.9f, 1, 0.001f)]
        public static float RotToLook_DeadZone_Yaw_Full = 0.995f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_LOOKZONES, ORDER_ROTATELOOK_LOOKZONES)]
        [ModOptionSlider]
        [ModOption(name: "Dead Zone Dot Product - pitch", tooltip: "How far from center where there is no turning", order = 2)]
        [ModOptionFloatValues(0.9f, 1, 0.001f)]
        public static float RotToLook_DeadZone_Pitch_Full = 0.995f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_LOOKZONES, ORDER_ROTATELOOK_LOOKZONES)]
        [ModOptionSlider]
        [ModOption(name: "Dead Zone Dot Product - roll", tooltip: "How far from center where there is no turning", order = 3)]
        [ModOptionFloatValues(0.9f, 1, 0.001f)]
        public static float RotToLook_DeadZone_Roll_Full = 0.995f;



        // deadzone affected by speed

        // zero delay zone / zero wait zone / immediate response zone


        #endregion
        #region Rotate To Look - ik

        // for now, use in game, but in the future, replace with a custom rig

        [ModOptionCategory(CATEGORY_ROTATELOOK_IK, ORDER_ROTATELOOK_IK)]
        [ModOptionSlider]
        [ModOption(name: "Forward Trim Degrees (yaw)", tooltip: "Forward comes from ragdoll spine, which seems to always point to the right a little.  This angle is added to help get it close to zero.  Negative pulls to the left, positive to the right", order = 1)]
        [ModOptionFloatValues(-20, 20, 0.5f)]
        public static float RotToLook_ForwardTrimDegrees_Yaw = -7;

        [ModOptionCategory(CATEGORY_ROTATELOOK_IK, ORDER_ROTATELOOK_IK)]
        [ModOptionSlider]
        [ModOption(name: "Forward Trim Degrees (pitch)", tooltip: "Forward comes from ragdoll spine, so may point up/down a little.  This angle is added to help get it close to zero.  Negative  pulls down, positive pulls up", order = 2)]
        [ModOptionFloatValues(-20, 20, 0.5f)]
        public static float RotToLook_ForwardTrimDegrees_Pitch = 0;

        #endregion

        #region Player Size

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

        #endregion

        #region Player Visibility

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

        #endregion

        #region Debug Drawing

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Show Confined Area", tooltip: "Shows what confined area scanner sees", order = 1)]
        public static bool ShowConfinedArea = false;

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Show Obstacle Avoidance", tooltip: "Shows the rays and hits that obstacle avoidance uses", order = 2)]
        public static bool ShowObstacleAvoidance = false;

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Show Repel Ground", tooltip: "Shows the rays and hits that repel ground uses", order = 3)]
        public static bool ShowRepelGround = false;

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Show Gaze Buffer - Target", tooltip: "Shows spheres that the gaze buffer hit scans and returns look when the user holds gaze long and steady enough", order = 4)]
        public static bool ShowGazeBuffer_Target = false;

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Show Gaze Buffer - Offset", tooltip: "Shows lines that gaze buffer uses to detect when staring at a consistent offset from forward", order = 5)]
        public static bool ShowGazeBuffer_Offset = false;

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Show Rotate To Look", tooltip: "Shows visuals of 'rotate to look' using gaze buffer results", order = 8)]
        public static bool ShowRotateToLook = false;

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Show HeadUpRotateVisualizer", tooltip: "Focused tester showing world to model rotations and back of head up vector", order = 9)]
        public static bool ShowHeadUpRotateVisualizer = false;

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Visualize Player Points", tooltip: "Shows points/lines on various transforms of the player avatar", order = 10)]
        public static bool VisualizePlayerPoints = false;

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Show Debug Visuals", tooltip: "This one looks like an early tester of figuring out how to render debug visuals - pretty useless beyond that", order = 11)]
        public static bool ShowDebugVisuals = false;

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Show Debug Status", tooltip: "Shows various properties in a textbox", order = 12)]
        public static bool ShowDebugStats = false;

        #endregion

        #endregion

        // PRE-FLIGHT DATA
        private FlightData _old = null;

        // FLIGHT DATA
        private bool _isFlying = false;
        private bool _markedToFly = false;      // will fly once not grounded
        private float _last_applied_drag = -1;

        private RayCastStorage _raycast_storage = null;
        private FlightTransitionWatcher _transitions = null;
        private PlayerRotator _rotator = null;
        private FlightJetpack _flight_jetpack = null;

        private DebugVisuals _debugVisuals = new DebugVisuals();
        private DebugStats _debugStats = new DebugStats();
        private VisualizePlayerPoints _visualizePlayerPoints = new VisualizePlayerPoints();
        private ScaleAdjuster _scaleAdjuster = new ScaleAdjuster();

        private bool _isPlayerSpawned = false;

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);

            _raycast_storage = new RayCastStorage();
            _transitions = new FlightTransitionWatcher();
            _rotator = new PlayerRotator();
            _flight_jetpack = new FlightJetpack(_raycast_storage, _rotator, _debugStats);

            //MaterialShaderFinder.Report();

            Player.onSpawn += Player_onSpawn;
            Player.onDespawn += Player_onDespawn;

            //TryLoadConfig();
            //TryLoadConfig2();
        }

        private void Player_onSpawn(Player player)
        {
            _isPlayerSpawned = true;

            if (Player.local != null)
            {
                //Debug.Log("showing morphology");
                Player.local.showMorphology = true;
            }

            _rotator.OnPlayerSpawned();
            _debugStats.Clear();
        }
        private void Player_onDespawn(Player player)
        {
            _isPlayerSpawned = false;

            _rotator.OnPlayerDespawned();
            _debugStats.Clear();
            _debugVisuals.RemoveVisuals();
            _visualizePlayerPoints.Clear();

            DeactivateFly();
        }

        public override void ScriptUpdate()
        {
            // TODO: use Time.deltaTime to get time since last update

            base.ScriptUpdate();

            if (!_isPlayerSpawned || Player.local == null)
                return;

            _visualizePlayerPoints.Update(playerScale / 100);
            _debugStats.Update_Pre();

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

            if (ShowDebugStats && _isPlayerSpawned)
            {
                PopulateDebug();
                _debugStats.Update_Final();
            }
        }
        public override void ScriptFixedUpdate()
        {
            // TODO: use Time.fixedDeltaTime to get time since last fixed update

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
                //Debug.Log("ActivateFly() called, but mod is disabled");
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
            _rotator.Reset();

            //PlaySounds.Play(SoundName.Jetpack_Deactivate);
        }

        private void PopulateDebug()
        {
            _debugStats.AddEntry("is flying", _isFlying.ToString());
            _debugStats.AddEntry("is grounded", Player.local.locomotion.isGrounded.ToString());
            _debugStats.AddEntry("is marked to fly", _markedToFly.ToString());

            Vector3 velocity = Player.local.locomotion.physicBody.velocity;
            _debugStats.AddEntry("velocity", velocity.ToStringSignificantDigits(2));
            _debugStats.AddEntry("vel speed", velocity.magnitude.ToStringSignificantDigits(2));

            _debugStats.AddEntry("stick left", InputUtil.GetLeftStick().ToStringSignificantDigits(2));
            _debugStats.AddEntry("stick right", InputUtil.GetRightStick().ToStringSignificantDigits(2));
        }

        #region debug research

        private static void TryLoadConfig()
        {
            try
            {
                //2025-10-18T04:32:09.078 ERROR ThunderRoad.ModManager.LoadThunderScripts       : [ModManager][ThunderScript][Jetpack2] Exception during ThunderScript ScriptLoaded for: Jetpack2.JetpackScript on mod: Jetpack2 in assembly: Jetpack2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null, System.ArgumentException: Invalid path
                //  at System.IO.Path.GetDirectoryName (System.String path) [0x0000d] in <80e08c2cc04049bf931fc9038d04f397>:0 
                //  at Jetpack2.JetpackScript.TryLoadConfig () [0x0000b] in <b850afc2f47b4688ba4d419a27caa709>:0 
                //  at Jetpack2.JetpackScript.ScriptLoaded (ThunderRoad.ModManager+ModData modData) [0x0006b] in <b850afc2f47b4688ba4d419a27caa709>:0 
                //  at ThunderRoad.ModManager.LoadThunderScripts (System.Type type, ThunderRoad.ModManager+ModData mod, System.Reflection.Assembly assembly) [0x000b4] in D:\VSTS-Agent\_work\TR_2021.3.38f1\Assets\SDK\Scripts\ModManager.cs:1167 

                string current_folder = Assembly.GetExecutingAssembly().Location;       // this returns empty string
                Debug.Log($"current_folder: {current_folder}");

                current_folder = @"D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2";
                Debug.Log($"current_folder (hardcoded): {current_folder}");

                var files = Directory.GetFiles(current_folder);
                Debug.Log($"files: {string.Join(Environment.NewLine, files)}");     // this finds the files in that folder



                // This seems to be the way to get at custom files in the mod folder.  But json configs should be handled
                // with how TryLoadConfig2 does it

                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\catalog_Jetpack2.hash
                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\catalog_Jetpack2.json
                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\config.json
                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\Jetpack2.dll
                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\jetpack2assets_assets_all.bundle
                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\manifest.json
                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\PerfectlyNormalBaS.dll
                files = Directory.GetFiles(".");
                Debug.Log($"files (.): {string.Join(Environment.NewLine, files)}");






                // D:\SteamLibrary\steamapps\common\Blade & Sorcery
                Debug.Log($"Directory.GetCurrentDirectory(): {Directory.GetCurrentDirectory()}");


                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\Managed
                string test = Directory.GetParent(typeof(object).Module.FullyQualifiedName).FullName;
                Debug.Log($"Directory.GetParent(typeof(object).Module.FullyQualifiedName).FullName: {test}");



                //File.ReadAllText();

            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
        }
        private void TryLoadConfig2()
        {
            try
            {
                ConfigData _config = null;      // pretending this would be a member variable


                // from inside ConfigData.Init...
                // this happens early in game loading, so no need to instantiate, just use static.  intance is probably referenced by asking Catalog for it (or some other b&s static dictionary)
                // ConfigData init.  testInt: 52


                // about to instantiate ConfigData.  ConfigData.testInt: 52
                Debug.Log($"about to instantiate ConfigData.  ConfigData.testInt: {ConfigData.testInt}");
                _config = new ConfigData();

                // manually calling _config.Init()
                // System.NullReferenceException: Object reference not set to an instance of an object
                Debug.Log("manually calling _config.Init()");
                _config.Init();

                Debug.Log("after calling _config.Init()");
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
        }

        #endregion
    }
}
