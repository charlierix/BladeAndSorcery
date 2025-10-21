using Jetpack2.InputWatchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace Jetpack2.Models
{
    public class ModConfigDefaultsData
    {
        #region Activation / Deactivation

        public static string FlightActivation;// = "HoldUp";

        /// <summary>
        /// Whether to stop flight when on the ground
        /// </summary>
        public static bool DeactivateOnGround;// = true;

        /// <summary>
        /// Options that are double click or gestures can be required to be done at the same time by both hands or just one\n\nSingle hand is easier but may cause misreads
        /// </summary>
        public static bool RequireBothHands;// = true;

        #endregion

        #region Toggle Behaviors

        /// <summary>
        /// Slows down accelerations when in tight spaces
        /// </summary>
        public static bool ShouldDetectConfinedArea;// = true;

        /// <summary>
        /// Will push the player away from obstacles when moving toward them (no effect if stopped near obstacles)
        /// </summary>
        public static bool ShouldAvoidObstacles;// = true;

        /// <summary>
        /// Will push the player upward from the ground requiring deliberate down pressure on the thumstick to touch the ground
        /// </summary>
        public static bool ShouldRepelGround;// = true;

        /// <summary>
        /// Will rotate the player toward the direction looking
        /// </summary>
        public static bool ShouldRotateToLook_Yaw;// = false;

        /// <summary>
        /// Will rotate the player toward the direction looking
        /// </summary>
        public static bool ShouldRotateToLook_Pitch;// = false;

        /// <summary>
        /// Will rotate the player toward the direction looking
        /// </summary>
        public static bool ShouldRotateToLook_Roll;// = false;

        #endregion

        #region Flight Properties

        /// <summary>
        /// How hard to accelerate horizontally
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 24, step 0.25
        /// </remarks>
        public static float HorizontalAccel;// = 8;

        /// <summary>
        /// How hard to accelerate vertically
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 12, step 0.25
        /// </remarks>
        public static float VerticalAccel;// = 6;

        /// <summary>
        /// Wind resistance
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 2, step 0.05
        /// </remarks>
        public static float Drag;// = 0.2f;

        /// <summary>
        /// 0 is no gravity.  9.8 is standard
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 18, step 0.1
        /// </remarks>
        public static float GravitySetting;// = 0f;

        #endregion

        #region Rotate To Look - gaze buffer

        /// <summary>
        /// How long to keep previous look directions
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 3, step 0.01
        /// </remarks>
        public static float GazeBuffer_MaxSeconds;// = 1.1f;

        /// <summary>
        /// Max size of buffer
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 500, step 20
        /// </remarks>
        public static int GazeBuffer_MaxCount;// = 60;

        /// <summary>
        /// The average of look directions.  This is the min confidence before the look direction is considered
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 1, step 0.01
        /// </remarks>
        public static float GazeBuffer_GazeConfidence_Direct;// = 0.7f;

        /// <summary>
        /// The average of look directions.  This is the min confidence before the look direction is considered
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 1, step 0.01
        /// </remarks>
        public static float GazeBuffer_GazeConfidence_Offset;// = 0.7f;

        /// <summary>
        /// The average of look directions.  This is the min confidence before the look direction is considered
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 1, step 0.01
        /// </remarks>
        public static float GazeBuffer_GazeConfidence_Target;// = 0.9f;

        /// <summary>
        /// Does an exponential decay against standard deviation of dot products with avg.  Large number makes it require tighter groupings
        /// </summary>
        /// <remarks>
        /// slider values: 1 to 300, step 1
        /// </remarks>
        public static float GazeBuffer_Confidence_StdDev_DecayMult;// = 130;

        /// <summary>
        /// The smallest radius (when speed is zero)
        /// </summary>
        /// <remarks>
        /// slider values: 2 to 9, step 0.25
        /// </remarks>
        public static float GazeBuffer_GazeTarget_RadiiForSpeed_Min;// = 5f;

        /// <summary>
        /// Speed to base radius scaling factor
        /// </summary>
        /// <remarks>
        /// slider values: 0.1 to 2, step 0.05
        /// </remarks>
        public static float GazeBuffer_GazeTarget_RadiiForSpeed_SpeedRatio;// = 0.75f;

        /// <summary>
        /// The size of the next largest radius (multiplied by base radius)
        /// </summary>
        /// <remarks>
        /// slider values: 1.5 to 5, step 0.1
        /// </remarks>
        public static float GazeBuffer_GazeTarget_RadiiForSpeed_StepMult;// = 3;

        #endregion
        #region Rotate To Look - capacitor

        /// <summary>
        /// Capacitor increases above this (forward dot look)
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 1, step 0.01
        /// </remarks>
        public static float RotToLook_Capacitor_UpperDot;// = 0.95f;

        /// <summary>
        /// Capacitor starts discharging below this (forward dot look)
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 1, step 0.01
        /// </remarks>
        public static float RotToLook_Capacitor_LowerDot;// = 0.9f;

        /// <summary>
        /// Capacitor discharges fastest below this (forward dot look)
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 1, step 0.01
        /// </remarks>
        public static float RotToLook_Capacitor_BottomDot;// = 0.75f;

        /// <summary>
        /// Charge per second when look diff is above upper dot
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 8, step 0.05
        /// </remarks>
        public static float RotToLook_Capacitor_ChargeSpeed;// = 0.9f;

        /// <summary>
        /// Look diff beween upper dot and one ramps up by this power
        /// </summary>
        /// <remarks>
        /// slider values: 1 to 6, step 0.1
        /// </remarks>
        public static float RotToLook_Capacitor_ChargePower;// = 3;

        /// <summary>
        /// Charge per second when diff is below lower dot
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 8, step 0.05
        /// </remarks>
        public static float RotToLook_Capacitor_DischargeSpeed;// = 2.5f;

        /// <summary>
        /// Look diff between lower dot and zero ramps up by this power
        /// </summary>
        /// <remarks>
        /// slider values: 1 to 6, step 0.1
        /// </remarks>
        public static float RotToLook_Capacitor_DischargePower;// = 2;

        #endregion
        #region Rotate To Look - look zones


        // --- deadzone initial ---

        /// <summary>
        /// How far from center where there is no turning
        /// </summary>
        /// <remarks>
        /// slider values: 0.8 to 1, step 0.001
        /// </remarks>
        public static float RotToLook_DeadZone_Yaw_Full;// = 0.98f;

        /// <summary>
        /// How far from center where there is no turning
        /// </summary>
        /// <remarks>
        /// slider values: 0.8 to 1, step 0.001
        /// </remarks>
        public static float RotToLook_DeadZone_Pitch_Full;// = 0.96f;

        /// <summary>
        /// How far from center where there is no turning
        /// </summary>
        /// <remarks>
        /// slider values: 0.8 to 1, step 0.001
        /// </remarks>
        public static float RotToLook_DeadZone_Roll_Full;// = 0.995f;



        // --- these starts should be a percent or fixed offset from full ----

        /// <summary>
        /// How far from center before it starts turning at max rate
        /// </summary>
        /// <remarks>
        /// slider values: 0.8 to 1, step 0.001
        /// </remarks>
        public static float RotToLook_DeadZone_Yaw_Start;// = 0.93f;

        /// <summary>
        /// How far from center before it starts turning at max rate
        /// </summary>
        /// <remarks>
        /// slider values: 0.8 to 1, step 0.001
        /// </remarks>
        public static float RotToLook_DeadZone_Pitch_Start;// = 0.88f;

        /// <summary>
        /// How far from center before it starts turning at max rate
        /// </summary>
        /// <remarks>
        /// slider values: 0.8 to 1, step 0.001
        /// </remarks>
        public static float RotToLook_DeadZone_Roll_Start;// = 0.93f;

        #endregion
        #region Rotate To Look - Turn Rates

        /// <summary>
        /// Degrees per second
        /// </summary>
        /// <remarks>
        /// slider values: 1 to 360, step 1
        /// </remarks>
        public static float RotateToLook_TurnRate_Yaw;// = 80;

        /// <summary>
        /// Degrees per second
        /// </summary>
        /// <remarks>
        /// slider values: 1 to 360, step 1
        /// </remarks>
        public static float RotateToLook_TurnRate_Pitch;// = 80;

        /// <summary>
        /// Degrees per second
        /// </summary>
        /// <remarks>
        /// slider values: 1 to 360, step 1
        /// </remarks>
        public static float RotateToLook_TurnRate_Roll;// = 80;

        #endregion
        #region Rotate To Look - ik

        // for now, use in game, but in the future, replace with a custom rig

        /// <summary>
        /// Forward comes from ragdoll spine, which seems to always point to the right a little.  This angle is added to help get it close to zero.  Negative pulls to the left, positive to the right
        /// </summary>
        /// <remarks>
        /// slider values: -20 to 20, step 0.5
        /// </remarks>
        public static float RotToLook_ForwardTrimDegrees_Yaw;// = -7;

        /// <summary>
        /// Forward comes from ragdoll spine, so may point up/down a little.  This angle is added to help get it close to zero.  Negative  pulls down, positive pulls up
        /// </summary>
        /// <remarks>
        /// slider values: -20 to 20, step 0.5
        /// </remarks>
        public static float RotToLook_ForwardTrimDegrees_Pitch;// = 11.5f;

        #endregion

        #region Debug Drawing

        /// <summary>
        /// Shows what confined area scanner sees
        /// </summary>
        public static bool ShowConfinedArea;// = false;

        /// <summary>
        /// Shows the rays and hits that obstacle avoidance uses
        /// </summary>
        public static bool ShowObstacleAvoidance;// = false;

        /// <summary>
        /// Shows the rays and hits that repel ground uses
        /// </summary>
        public static bool ShowRepelGround;// = false;

        /// <summary>
        /// Shows spheres that the gaze buffer hit scans and returns look when the user holds gaze long and steady enough
        /// </summary>
        public static bool ShowGazeBuffer_Target;// = false;

        /// <summary>
        /// Shows lines that gaze buffer uses to detect when staring at a consistent offset from forward
        /// </summary>
        public static bool ShowGazeBuffer_Offset;// = false;

        /// <summary>
        /// Shows visuals of 'rotate to look' using gaze buffer results
        /// </summary>
        public static bool ShowRotateToLook;// = false;

        /// <summary>
        /// Focused tester showing world to model rotations and back of head up vector
        /// </summary>
        public static bool ShowHeadUpRotateVisualizer;// = false;

        /// <summary>
        /// Shows points/lines on various transforms of the player avatar
        /// </summary>
        public static bool VisualizePlayerPoints;// = false;

        /// <summary>
        /// This one looks like an early tester of figuring out how to render debug visuals - pretty useless beyond that
        /// </summary>
        public static bool ShowDebugVisuals;// = false;

        /// <summary>
        /// Shows various properties in a textbox
        /// </summary>
        public static bool ShowDebugStats;// = false;

        #endregion
    }
}
