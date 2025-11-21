using Jetpack2.DebugCode;
using Jetpack2.InputWatchers;
using Jetpack2.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2
{
    public static class UIModOptions
    {
        #region constants

        private const string CATEGORY_ACTIVATE = "Activation / Deactivation";
        private const string CATEGORY_TOGGLEBEHAVIORS = "Toggle Behaviors";
        private const string CATEGORY_FLIGHTPROPS = "Flight Properties";
        private const string CATEGORY_ROTATELOOK_GAZEBUFFER = "Rotate To Look - Gaze Buffer";
        private const string CATEGORY_ROTATELOOK_CAPACITOR = "Rotate To Look - Capacitor";
        private const string CATEGORY_ROTATELOOK_LOOKZONES = "Rotate To Look - Look Zones";
        private const string CATEGORY_ROTATELOOK_TURNRATES = "Rotate To Look - Turn Rates";
        private const string CATEGORY_ROTATELOOK_IK = "Rotate To Look - IK Rig";
        private const string CATEGORY_PLAYERPOSTRACKING = "Player Pos Tracking";
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
        private const int ORDER_ROTATELOOK_TURNRATES = 7;
        private const int ORDER_ROTATELOOK_IK = 8;
        private const int ORDER_PLAYERPOSTRACKING = 9;
        private const int ORDER_SOUNDS = 50;
        private const int ORDER_SCALE = 51;
        private const int ORDER_VISIBILITY = 52;
        private const int ORDER_DEBUGDRAWING = 53;

        #endregion

        #region base configs

        //[ModOptionTextDisplay("description of section", null)]
        //[ModOption("Info")]
        //private static void label1(string value) { }

        public static ModOptionString[] loadDefaultsButtonLabel = new[]
        {
            new ModOptionString("Load Defaults", "LoadDefaults")
        };

        // NOTE: these OnClick functions always get called when the mod first loads
        private static bool _onLoadDefaults_called = false;

        [ModOptionButton]
        [ModOption("Put all settings back to defaults - DOESN'T WORK", "those default values are stored in configs\\ModConfigDefaults.json", nameof(loadDefaultsButtonLabel), order = 0)]
        public static void OnLoadDefaults(string value)
        {
            if (!_onLoadDefaults_called)
            {
                _onLoadDefaults_called = true;
                return;
            }

            //SetDefaults_ATTEMPT1();
            //SetDefaults_ATTEMPT2();
        }
        private static void SetDefaults_ATTEMPT1()
        {
            // Get all public static fields from the model class
            var modelFields = typeof(ModConfigDefaultsData).GetFields(BindingFlags.Public | BindingFlags.Static);

            // Get all public static fields and properties from UIModOptions
            var uiFields = typeof(UIModOptions).GetFields(BindingFlags.Public | BindingFlags.Static).ToList();
            var uiProperties = typeof(UIModOptions).GetProperties(BindingFlags.Public | BindingFlags.Static).ToList();

            // Iterate the public static fields from the model class and set the corresponding field in this class
            foreach (var modelField in modelFields)
            {
                // Generate the corresponding field name in UIModOptions (capitalize first letter)
                string uiName = char.ToUpper(modelField.Name[0]) + modelField.Name.Substring(1);

                // NOTE: these compares are case sensitive (it should be an exact match)

                // Try to find the matching field in UIModOptions
                var uiField = uiFields.FirstOrDefault(f => f.Name == uiName && f.FieldType == modelField.FieldType);
                if (uiField != null)
                {
                    // Set the field value directly
                    uiField.SetValue(null, modelField.GetValue(null));
                    Debug.Log($"set field: {uiName}");
                    continue;
                }

                // If field not found, try to find a matching public static property
                var uiProperty = uiProperties.FirstOrDefault(p =>
                    p.Name == uiName &&
                    p.PropertyType == modelField.FieldType &&
                    p.CanWrite);

                if (uiProperty != null)
                {
                    // Set the property value via the setter
                    uiProperty.SetValue(null, modelField.GetValue(null));
                    Debug.Log($"set prop: {uiName}");
                    continue;
                }

                Debug.Log($"didn't find: {uiName}");
            }
        }
        private static void SetDefaults_ATTEMPT2()
        {
            // TODO: this isn't working.  see if there's a function that needs to be called to tell the mod option system that the value has changed
            //
            // instead of directly setting the float value:
            //  get the corresponding ModOption from attribute
            //  get params for that
            //  find nearest param for new value
            //  call modoption.apply(index)


            // I think I'm correct:
            // GameManager.options.ApplyModOptions();
            //public IEnumerator ApplyModOptions()
            //{
            //    Debug.Log("[Options] Applying mod options");
            //    CleanModOptions();
            //    int modsCount = ModManager.loadedMods.Count;
            //    int i = 0;
            //    LoadingCamera.SetLoadingStage(LoadingCamera.Stage.ApplyOptions, setAutomaticPercentage: false);
            //    foreach (ModManager.ModData loadedMod in ModManager.loadedMods)
            //    {
            //        ModOption.Save modOptionSave;
            //        bool flag = TryGetModOption(loadedMod.folderName, out modOptionSave);
            //        foreach (ModOption modOption in loadedMod.modOptions)
            //        {
            //            modOption.LoadModOptionParameters();
            //            if (flag && modOption.saveValue && modOptionSave.TryGetParameter(modOption.name, out var parameterValue))
            //            {
            //                modOption.Apply(parameterValue.index);                    // ************************ this ************************
            //            }
            //            else
            //            {
            //                modOption.Apply(modOption.defaultValueIndex);
            //            }
            //        }

            //        i++;
            //        yield return LoadingCamera.SetPercentageYield(i * 100 / modsCount);
            //    }

            //    Debug.Log("[Options] Applied mod options");
            //}
        }

        [ModOption(name: "Use Jetpack Mod", tooltip: "Turns on/off the Jetpack mod", order = 1)]
        public static bool UseJetpackMod = true;

        #endregion

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
        public static FlightActivationType FlightActivation_cast => _flightActivation_cast;

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
        public static bool RequireBothHands = false;

        #endregion

        #region Toggle Behaviors

        [ModOptionCategory(CATEGORY_TOGGLEBEHAVIORS, ORDER_TOGGLEBEHAVIORS)]
        [ModOption(name: "Should Detect Confined Area", tooltip: "Slows down accelerations when in tight spaces", order = 0)]
        public static bool ShouldDetectConfinedArea = true;

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

        #endregion

        #region Flight Properties

        [ModOptionCategory(CATEGORY_FLIGHTPROPS, ORDER_FLIGHTPROPS)]
        [ModOptionSlider]
        [ModOption(name: "Horizontal Accel", tooltip: "How hard to accelerate horizontally", order = 0)]
        [ModOptionFloatValues(0, 24, 0.25f)]
        public static float HorizontalAccel = 8;

        [ModOptionCategory(CATEGORY_FLIGHTPROPS, ORDER_FLIGHTPROPS)]
        [ModOptionSlider]
        [ModOption(name: "Vertical Accel", tooltip: "How hard to accelerate vertically", order = 1)]
        [ModOptionFloatValues(0, 12, 0.25f)]
        public static float VerticalAccel = 6;

        [ModOptionCategory(CATEGORY_FLIGHTPROPS, ORDER_FLIGHTPROPS)]
        [ModOptionSlider]
        [ModOption(name: "Drag", tooltip: "Wind resistance", order = 2)]
        [ModOptionFloatValues(0, 2, 0.05f)]
        public static float Drag = 0.2f;

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
        [ModOptionIntValues(0, 180, 10)]
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
        public static float GazeBuffer_Confidence_StdDev_DecayMult = 130;


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
        public static float RotToLook_Capacitor_ChargeSpeed = 0.9f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_CAPACITOR, ORDER_ROTATELOOK_CAPACITOR)]
        [ModOptionSlider]
        [ModOption(name: "Capacitor Charge Power", tooltip: "Look diff beween upper dot and one ramps up by this power", order = 5)]
        [ModOptionFloatValues(1, 6, 0.1f)]
        public static float RotToLook_Capacitor_ChargePower = 3;

        [ModOptionCategory(CATEGORY_ROTATELOOK_CAPACITOR, ORDER_ROTATELOOK_CAPACITOR)]
        [ModOptionSlider]
        [ModOption(name: "Capacitor Discharge Speed", tooltip: "Charge per second when diff is below lower dot", order = 6)]
        [ModOptionFloatValues(0, 8, 0.05f)]
        public static float RotToLook_Capacitor_DischargeSpeed = 2.5f;

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
        [ModOptionFloatValues(0.8f, 1, 0.001f)]
        public static float RotToLook_DeadZone_Yaw_Full = 0.98f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_LOOKZONES, ORDER_ROTATELOOK_LOOKZONES)]
        [ModOptionSlider]
        [ModOption(name: "Dead Zone Dot Product - pitch", tooltip: "How far from center where there is no turning", order = 2)]
        [ModOptionFloatValues(0.8f, 1, 0.001f)]
        public static float RotToLook_DeadZone_Pitch_Full = 0.96f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_LOOKZONES, ORDER_ROTATELOOK_LOOKZONES)]
        [ModOptionSlider]
        [ModOption(name: "Dead Zone Dot Product - roll", tooltip: "How far from center where there is no turning", order = 3)]
        [ModOptionFloatValues(0.8f, 1, 0.001f)]
        public static float RotToLook_DeadZone_Roll_Full = 0.995f;



        // these starts should be a percent or fixed offset from full

        [ModOptionCategory(CATEGORY_ROTATELOOK_LOOKZONES, ORDER_ROTATELOOK_LOOKZONES)]
        [ModOptionSlider]
        [ModOption(name: "Dead Zone Dot Product - yaw (start)", tooltip: "How far from center before it starts turning at max rate", order = 4)]
        [ModOptionFloatValues(0.8f, 1, 0.001f)]
        public static float RotToLook_DeadZone_Yaw_Start = 0.93f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_LOOKZONES, ORDER_ROTATELOOK_LOOKZONES)]
        [ModOptionSlider]
        [ModOption(name: "Dead Zone Dot Product - pitch (start)", tooltip: "How far from center before it starts turning at max rate", order = 5)]
        [ModOptionFloatValues(0.8f, 1, 0.001f)]
        public static float RotToLook_DeadZone_Pitch_Start = 0.88f;

        [ModOptionCategory(CATEGORY_ROTATELOOK_LOOKZONES, ORDER_ROTATELOOK_LOOKZONES)]
        [ModOptionSlider]
        [ModOption(name: "Dead Zone Dot Product - roll (start)", tooltip: "How far from center before it starts turning at max rate", order = 6)]
        [ModOptionFloatValues(0.8f, 1, 0.001f)]
        public static float RotToLook_DeadZone_Roll_Start = 0.97f;





        // deadzone affected by speed

        // zero delay zone / zero wait zone / immediate response zone


        #endregion
        #region Rotate To Look - Turn Rates

        [ModOptionCategory(CATEGORY_ROTATELOOK_TURNRATES, ORDER_ROTATELOOK_TURNRATES)]
        [ModOptionSlider]
        [ModOption(name: "rot to look: Max Turn Rate Degrees - yaw", tooltip: "Degrees per second", order = 0)]
        [ModOptionFloatValues(1, 270, 1f)]
        public static float RotateToLook_TurnRate_Yaw = 80;

        [ModOptionCategory(CATEGORY_ROTATELOOK_TURNRATES, ORDER_ROTATELOOK_TURNRATES)]
        [ModOptionSlider]
        [ModOption(name: "rot to look: Max Turn Rate Degrees - pitch", tooltip: "Degrees per second", order = 1)]
        [ModOptionFloatValues(1, 270, 1f)]
        public static float RotateToLook_TurnRate_Pitch = 80;

        [ModOptionCategory(CATEGORY_ROTATELOOK_TURNRATES, ORDER_ROTATELOOK_TURNRATES)]
        [ModOptionSlider]
        [ModOption(name: "rot to look: Max Turn Rate Degrees - roll", tooltip: "Degrees per second", order = 2)]
        [ModOptionFloatValues(1, 270, 1f)]
        public static float RotateToLook_TurnRate_Roll = 120;

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
        public static float RotToLook_ForwardTrimDegrees_Pitch = 11.5f;

        #endregion

        #region Player Pos Tracking

        //[ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        //[ModOptionSlider]
        //[ModOption(name: "Clustering Weight - Head Pos", tooltip: "Priority of head position while clustering", order = 1)]
        //[ModOptionFloatValues(0, 4, 0.1f)]
        //public static float PlayerPosTracking_Weight_HeadPos = 0.7f;

        //[ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        //[ModOptionSlider]
        //[ModOption(name: "Clustering Weight - Hand Pos", tooltip: "Priority of hand positions while clustering", order = 2)]
        //[ModOptionFloatValues(0, 4, 0.1f)]
        //public static float PlayerPosTracking_Weight_HandPos = 2;

        //[ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        //[ModOptionSlider]
        //[ModOption(name: "Clustering Weight - Directions", tooltip: "Priority of head/hand orientations while clustering", order = 3)]
        //[ModOptionFloatValues(0, 4, 0.1f)]
        //public static float PlayerPosTracking_Weight_Directions = 0.3f;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Resting Pos - Min X", tooltip: "Defines a box where hands are considered to be in the resting position", order = 4)]
        [ModOptionFloatValues(-0.1f, 0.2f, 0.01f)]
        public static float PlayerPosTracking_RestingPos_MinX = 0;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Resting Pos - Max X", tooltip: "Defines a box where hands are considered to be in the resting position", order = 5)]
        [ModOptionFloatValues(0, 0.35f, 0.01f)]
        public static float PlayerPosTracking_RestingPos_MaxX = 0.24f;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Resting Pos - Min Y", tooltip: "Defines a box where hands are considered to be in the resting position", order = 6)]
        [ModOptionFloatValues(-0.25f, 0.1f, 0.01f)]
        public static float PlayerPosTracking_RestingPos_MinY = -0.1f;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Resting Pos - Max Y", tooltip: "Defines a box where hands are considered to be in the resting position", order = 7)]
        [ModOptionFloatValues(0.3f, 0.6f, 0.01f)]
        public static float PlayerPosTracking_RestingPos_MaxY = 0.45f;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Resting Pos - Min Z", tooltip: "Defines a box where hands are considered to be in the resting position", order = 8)]
        [ModOptionFloatValues(-0.15f, 0.15f, 0.01f)]
        public static float PlayerPosTracking_RestingPos_MinZ = -0.08f;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Resting Pos - Max Z", tooltip: "Defines a box where hands are considered to be in the resting position", order = 9)]
        [ModOptionFloatValues(0.25f, 0.5f, 0.01f)]
        public static float PlayerPosTracking_RestingPos_MaxZ = 0.39f;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Wing Pos - Min X", tooltip: "Defines a box where hands are considered to be in the wing extended position", order = 10)]
        [ModOptionFloatValues(0.2f, 0.4f, 0.01f)]
        public static float PlayerPosTracking_WingPos_MinX = 0.3f;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Wing Pos - Max X", tooltip: "Defines a box where hands are considered to be in the wing extended position", order = 11)]
        [ModOptionFloatValues(0.45f, 0.7f, 0.01f)]
        public static float PlayerPosTracking_WingPos_MaxX = 0.6f;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Wing Pos - Min Y", tooltip: "Defines a box where hands are considered to be in the wing extended position", order = 12)]
        [ModOptionFloatValues(0, 0.2f, 0.01f)]
        public static float PlayerPosTracking_WingPos_MinY = 0.1f;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Wing Pos - Max Y", tooltip: "Defines a box where hands are considered to be in the wing extended position", order = 13)]
        [ModOptionFloatValues(0.4f, 0.65f, 0.01f)]
        public static float PlayerPosTracking_WingPos_MaxY = 0.5f;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Wing Pos - Min Z", tooltip: "Defines a box where hands are considered to be in the wing extended position", order = 14)]
        [ModOptionFloatValues(-0.15f, 0, 0.01f)]
        public static float PlayerPosTracking_WingPos_MinZ = -0.1f;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Wing Pos - Max Z", tooltip: "Defines a box where hands are considered to be in the wing extended position", order = 15)]
        [ModOptionFloatValues(0.3f, 0.5f, 0.01f)]
        public static float PlayerPosTracking_WingPos_MaxZ = 0.4f;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Wing Rotate Angle", tooltip: "Rotates the wing so it's not perfectly in line with the arm.  Natural arm position is to be held at a 45 degree angle, so this compensates", order = 16)]
        [ModOptionFloatValues(-90, 90, 1)]
        public static float PlayerPosTracking_WingRotateAngle = 0;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOptionSlider]
        [ModOption(name: "Wing Slide Offset", tooltip: "Slides the wing back so the center is hand and not at thumb/finger", order = 17)]
        [ModOptionFloatValues(-0.3f, 0.3f, 0.01f)]
        public static float PlayerPosTracking_WingTranslateCord = 0;

        [ModOptionCategory(CATEGORY_PLAYERPOSTRACKING, ORDER_PLAYERPOSTRACKING)]
        [ModOption(name: "Require Open Hand", tooltip: "Wing won't appear if hand is closed", order = 18)]
        public static bool PlayerPosTracking_WingRequireOpenHand = false;

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
        [ModOption(name: "Show Player Pos Tracking", tooltip: "Watches head and hand positions over time, clusters on stable positions", order = 9)]
        public static bool ShowPlayerPosTracking = false;

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Visualize Player Points", tooltip: "Shows points/lines on various transforms of the player avatar", order = 10)]
        public static bool VisualizePlayerPoints = false;

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Visualize Hand Zone Positions", tooltip: "Shows regions where the hands could be", order = 11)]
        public static bool VisualizeHandZonePositions = false;

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Show Debug Visuals", tooltip: "This one looks like an early tester of figuring out how to render debug visuals - pretty useless beyond that", order = 12)]
        public static bool ShowDebugVisuals = false;

        [ModOptionCategory(CATEGORY_DEBUGDRAWING, ORDER_DEBUGDRAWING)]
        [ModOption(name: "Show Debug Status", tooltip: "Shows various properties in a textbox", order = 13)]
        public static bool ShowDebugStats = false;

        #endregion
    }
}
