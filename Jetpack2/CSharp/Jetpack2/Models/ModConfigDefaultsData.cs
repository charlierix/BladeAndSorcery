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

        public static string flightActivation;// = "HoldUp";
        public string FlightActivation { get => flightActivation; set => flightActivation = value; }

        /// <summary>
        /// Whether to stop flight when on the ground
        /// </summary>
        public static bool deactivateOnGround;// = true;
        public bool DeactivateOnGround { get => deactivateOnGround; set => deactivateOnGround = value; }

        /// <summary>
        /// Options that are double click or gestures can be required to be done at the same time by both hands or just one\n\nSingle hand is easier but may cause misreads
        /// </summary>
        public static bool requireBothHands;// = true;
        public bool RequireBothHands { get => requireBothHands; set => requireBothHands = value; }

        #endregion

        #region Toggle Behaviors

        /// <summary>
        /// Slows down accelerations when in tight spaces
        /// </summary>
        public static bool shouldDetectConfinedArea;// = true;
        public bool ShouldDetectConfinedArea { get => shouldDetectConfinedArea; set => shouldDetectConfinedArea = value; }

        /// <summary>
        /// Will push the player away from obstacles when moving toward them (no effect if stopped near obstacles)
        /// </summary>
        public static bool shouldAvoidObstacles;// = true;
        public bool ShouldAvoidObstacles { get => shouldAvoidObstacles; set => shouldAvoidObstacles = value; }

        /// <summary>
        /// Will push the player upward from the ground requiring deliberate down pressure on the thumstick to touch the ground
        /// </summary>
        public static bool shouldRepelGround;// = true;
        public bool ShouldRepelGround { get => shouldRepelGround; set => shouldRepelGround = value; }

        /// <summary>
        /// Will rotate the player toward the direction looking
        /// </summary>
        public static bool shouldRotateToLook_Yaw;// = false;
        public bool ShouldRotateToLook_Yaw { get => shouldRotateToLook_Yaw; set => shouldRotateToLook_Yaw = value; }

        /// <summary>
        /// Will rotate the player toward the direction looking
        /// </summary>
        public static bool shouldRotateToLook_Pitch;// = false;
        public bool ShouldRotateToLook_Pitch { get => shouldRotateToLook_Pitch; set => shouldRotateToLook_Pitch = value; }

        /// <summary>
        /// Will rotate the player toward the direction looking
        /// </summary>
        public static bool shouldRotateToLook_Roll;// = false;
        public bool ShouldRotateToLook_Roll { get => shouldRotateToLook_Roll; set => shouldRotateToLook_Roll = value; }

        #endregion

        #region Flight Properties

        /// <summary>
        /// How hard to accelerate horizontally
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 24, step 0.25
        /// </remarks>
        public static float horizontalAccel;// = 8;
        public float HorizontalAccel { get => horizontalAccel; set => horizontalAccel = value; }

        /// <summary>
        /// How hard to accelerate vertically
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 12, step 0.25
        /// </remarks>
        public static float verticalAccel;// = 6;
        public float VerticalAccel { get => verticalAccel; set => verticalAccel = value; }

        /// <summary>
        /// Wind resistance
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 2, step 0.05
        /// </remarks>
        public static float drag;// = 0.2f;
        public float Drag { get => drag; set => drag = value; }

        /// <summary>
        /// 0 is no gravity.  9.8 is standard
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 18, step 0.1
        /// </remarks>
        public static float gravitySetting;// = 0f;
        public float GravitySetting { get => gravitySetting; set => gravitySetting = value; }

        #endregion

        #region Rotate To Look - gaze buffer

        /// <summary>
        /// How long to keep previous look directions
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 3, step 0.01
        /// </remarks>
        public static float gazeBuffer_MaxSeconds;// = 1.1f;
        public float GazeBuffer_MaxSeconds { get => gazeBuffer_MaxSeconds; set => gazeBuffer_MaxSeconds = value; }

        /// <summary>
        /// Max size of buffer
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 500, step 20
        /// </remarks>
        public static int gazeBuffer_MaxCount;// = 60;
        public int GazeBuffer_MaxCount { get => gazeBuffer_MaxCount; set => gazeBuffer_MaxCount = value; }

        /// <summary>
        /// The average of look directions.  This is the min confidence before the look direction is considered
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 1, step 0.01
        /// </remarks>
        public static float gazeBuffer_GazeConfidence_Direct;// = 0.7f;
        public float GazeBuffer_GazeConfidence_Direct { get => gazeBuffer_GazeConfidence_Direct; set => gazeBuffer_GazeConfidence_Direct = value; }

        /// <summary>
        /// The average of look directions.  This is the min confidence before the look direction is considered
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 1, step 0.01
        /// </remarks>
        public static float gazeBuffer_GazeConfidence_Offset;// = 0.7f;
        public float GazeBuffer_GazeConfidence_Offset { get => gazeBuffer_GazeConfidence_Offset; set => gazeBuffer_GazeConfidence_Offset = value; }

        /// <summary>
        /// The average of look directions.  This is the min confidence before the look direction is considered
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 1, step 0.01
        /// </remarks>
        public static float gazeBuffer_GazeConfidence_Target;// = 0.9f;
        public float GazeBuffer_GazeConfidence_Target { get => gazeBuffer_GazeConfidence_Target; set => gazeBuffer_GazeConfidence_Target = value; }

        /// <summary>
        /// Does an exponential decay against standard deviation of dot products with avg.  Large number makes it require tighter groupings
        /// </summary>
        /// <remarks>
        /// slider values: 1 to 300, step 1
        /// </remarks>
        public static float gazeBuffer_Confidence_StdDev_DecayMult;// = 130;
        public float GazeBuffer_Confidence_StdDev_DecayMult { get => gazeBuffer_Confidence_StdDev_DecayMult; set => gazeBuffer_Confidence_StdDev_DecayMult = value; }

        /// <summary>
        /// The smallest radius (when speed is zero)
        /// </summary>
        /// <remarks>
        /// slider values: 2 to 9, step 0.25
        /// </remarks>
        public static float gazeBuffer_GazeTarget_RadiiForSpeed_Min;// = 5f;
        public float GazeBuffer_GazeTarget_RadiiForSpeed_Min { get => gazeBuffer_GazeTarget_RadiiForSpeed_Min; set => gazeBuffer_GazeTarget_RadiiForSpeed_Min = value; }

        /// <summary>
        /// Speed to base radius scaling factor
        /// </summary>
        /// <remarks>
        /// slider values: 0.1 to 2, step 0.05
        /// </remarks>
        public static float gazeBuffer_GazeTarget_RadiiForSpeed_SpeedRatio;// = 0.75f;
        public float GazeBuffer_GazeTarget_RadiiForSpeed_SpeedRatio { get => gazeBuffer_GazeTarget_RadiiForSpeed_SpeedRatio; set => gazeBuffer_GazeTarget_RadiiForSpeed_SpeedRatio = value; }

        /// <summary>
        /// The size of the next largest radius (multiplied by base radius)
        /// </summary>
        /// <remarks>
        /// slider values: 1.5 to 5, step 0.1
        /// </remarks>
        public static float gazeBuffer_GazeTarget_RadiiForSpeed_StepMult;// = 3;
        public float GazeBuffer_GazeTarget_RadiiForSpeed_StepMult { get => gazeBuffer_GazeTarget_RadiiForSpeed_StepMult; set => gazeBuffer_GazeTarget_RadiiForSpeed_StepMult = value; }

        #endregion
        #region Rotate To Look - capacitor

        /// <summary>
        /// Capacitor increases above this (forward dot look)
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 1, step 0.01
        /// </remarks>
        public static float rotToLook_Capacitor_UpperDot;// = 0.95f;
        public float RotToLook_Capacitor_UpperDot { get => rotToLook_Capacitor_UpperDot; set => rotToLook_Capacitor_UpperDot = value; }

        /// <summary>
        /// Capacitor starts discharging below this (forward dot look)
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 1, step 0.01
        /// </remarks>
        public static float rotToLook_Capacitor_LowerDot;// = 0.9f;
        public float RotToLook_Capacitor_LowerDot { get => rotToLook_Capacitor_LowerDot; set => rotToLook_Capacitor_LowerDot = value; }

        /// <summary>
        /// Capacitor discharges fastest below this (forward dot look)
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 1, step 0.01
        /// </remarks>
        public static float rotToLook_Capacitor_BottomDot;// = 0.75f;
        public float RotToLook_Capacitor_BottomDot { get => rotToLook_Capacitor_BottomDot; set => rotToLook_Capacitor_BottomDot = value; }

        /// <summary>
        /// Charge per second when look diff is above upper dot
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 8, step 0.05
        /// </remarks>
        public static float rotToLook_Capacitor_ChargeSpeed;// = 0.9f;
        public float RotToLook_Capacitor_ChargeSpeed { get => rotToLook_Capacitor_ChargeSpeed; set => rotToLook_Capacitor_ChargeSpeed = value; }

        /// <summary>
        /// Look diff beween upper dot and one ramps up by this power
        /// </summary>
        /// <remarks>
        /// slider values: 1 to 6, step 0.1
        /// </remarks>
        public static float rotToLook_Capacitor_ChargePower;// = 3;
        public float RotToLook_Capacitor_ChargePower { get => rotToLook_Capacitor_ChargePower; set => rotToLook_Capacitor_ChargePower = value; }

        /// <summary>
        /// Charge per second when diff is below lower dot
        /// </summary>
        /// <remarks>
        /// slider values: 0 to 8, step 0.05
        /// </remarks>
        public static float rotToLook_Capacitor_DischargeSpeed;// = 2.5f;
        public float RotToLook_Capacitor_DischargeSpeed { get => rotToLook_Capacitor_DischargeSpeed; set => rotToLook_Capacitor_DischargeSpeed = value; }

        /// <summary>
        /// Look diff between lower dot and zero ramps up by this power
        /// </summary>
        /// <remarks>
        /// slider values: 1 to 6, step 0.1
        /// </remarks>
        public static float rotToLook_Capacitor_DischargePower;// = 2;
        public float RotToLook_Capacitor_DischargePower { get => rotToLook_Capacitor_DischargePower; set => rotToLook_Capacitor_DischargePower = value; }

        #endregion
        #region Rotate To Look - look zones


        // --- deadzone initial ---

        /// <summary>
        /// How far from center where there is no turning
        /// </summary>
        /// <remarks>
        /// slider values: 0.8 to 1, step 0.001
        /// </remarks>
        public static float rotToLook_DeadZone_Yaw_Full;// = 0.98f;
        public float RotToLook_DeadZone_Yaw_Full { get => rotToLook_DeadZone_Yaw_Full; set => rotToLook_DeadZone_Yaw_Full = value; }

        /// <summary>
        /// How far from center where there is no turning
        /// </summary>
        /// <remarks>
        /// slider values: 0.8 to 1, step 0.001
        /// </remarks>
        public static float rotToLook_DeadZone_Pitch_Full;// = 0.96f;
        public float RotToLook_DeadZone_Pitch_Full { get => rotToLook_DeadZone_Pitch_Full; set => rotToLook_DeadZone_Pitch_Full = value; }

        /// <summary>
        /// How far from center where there is no turning
        /// </summary>
        /// <remarks>
        /// slider values: 0.8 to 1, step 0.001
        /// </remarks>
        public static float rotToLook_DeadZone_Roll_Full;// = 0.995f;
        public float RotToLook_DeadZone_Roll_Full { get => rotToLook_DeadZone_Roll_Full; set => rotToLook_DeadZone_Roll_Full = value; }



        // --- these starts should be a percent or fixed offset from full ----

        /// <summary>
        /// How far from center before it starts turning at max rate
        /// </summary>
        /// <remarks>
        /// slider values: 0.8 to 1, step 0.001
        /// </remarks>
        public static float rotToLook_DeadZone_Yaw_Start;// = 0.93f;
        public float RotToLook_DeadZone_Yaw_Start { get => rotToLook_DeadZone_Yaw_Start; set => rotToLook_DeadZone_Yaw_Start = value; }

        /// <summary>
        /// How far from center before it starts turning at max rate
        /// </summary>
        /// <remarks>
        /// slider values: 0.8 to 1, step 0.001
        /// </remarks>
        public static float rotToLook_DeadZone_Pitch_Start;// = 0.88f;
        public float RotToLook_DeadZone_Pitch_Start { get => rotToLook_DeadZone_Pitch_Start; set => rotToLook_DeadZone_Pitch_Start = value; }

        /// <summary>
        /// How far from center before it starts turning at max rate
        /// </summary>
        /// <remarks>
        /// slider values: 0.8 to 1, step 0.001
        /// </remarks>
        public static float rotToLook_DeadZone_Roll_Start;// = 0.93f;
        public float RotToLook_DeadZone_Roll_Start { get => rotToLook_DeadZone_Roll_Start; set => rotToLook_DeadZone_Roll_Start = value; }

        #endregion
        #region Rotate To Look - Turn Rates

        /// <summary>
        /// Degrees per second
        /// </summary>
        /// <remarks>
        /// slider values: 1 to 360, step 1
        /// </remarks>
        public static float rotateToLook_TurnRate_Yaw;// = 80;
        public float RotateToLook_TurnRate_Yaw { get => rotateToLook_TurnRate_Yaw; set => rotateToLook_TurnRate_Yaw = value; }

        /// <summary>
        /// Degrees per second
        /// </summary>
        /// <remarks>
        /// slider values: 1 to 360, step 1
        /// </remarks>
        public static float rotateToLook_TurnRate_Pitch;// = 80;
        public float RotateToLook_TurnRate_Pitch { get => rotateToLook_TurnRate_Pitch; set => rotateToLook_TurnRate_Pitch = value; }

        /// <summary>
        /// Degrees per second
        /// </summary>
        /// <remarks>
        /// slider values: 1 to 360, step 1
        /// </remarks>
        public static float rotateToLook_TurnRate_Roll;// = 80;
        public float RotateToLook_TurnRate_Roll { get => rotateToLook_TurnRate_Roll; set => rotateToLook_TurnRate_Roll = value; }

        #endregion
        #region Rotate To Look - ik

        // for now, use in game, but in the future, replace with a custom rig

        /// <summary>
        /// Forward comes from ragdoll spine, which seems to always point to the right a little.  This angle is added to help get it close to zero.  Negative pulls to the left, positive to the right
        /// </summary>
        /// <remarks>
        /// slider values: -20 to 20, step 0.5
        /// </remarks>
        public static float rotToLook_ForwardTrimDegrees_Yaw;// = -7;
        public float RotToLook_ForwardTrimDegrees_Yaw { get => rotToLook_ForwardTrimDegrees_Yaw; set => rotToLook_ForwardTrimDegrees_Yaw = value; }

        /// <summary>
        /// Forward comes from ragdoll spine, so may point up/down a little.  This angle is added to help get it close to zero.  Negative  pulls down, positive pulls up
        /// </summary>
        /// <remarks>
        /// slider values: -20 to 20, step 0.5
        /// </remarks>
        public static float rotToLook_ForwardTrimDegrees_Pitch;// = 11.5f;
        public float RotToLook_ForwardTrimDegrees_Pitch { get => rotToLook_ForwardTrimDegrees_Pitch; set => rotToLook_ForwardTrimDegrees_Pitch = value; }

        #endregion

        #region Debug Drawing

        /// <summary>
        /// Shows what confined area scanner sees
        /// </summary>
        public static bool showConfinedArea;// = false;
        public bool ShowConfinedArea { get => showConfinedArea; set => showConfinedArea = value; }

        /// <summary>
        /// Shows the rays and hits that obstacle avoidance uses
        /// </summary>
        public static bool showObstacleAvoidance;// = false;
        public bool ShowObstacleAvoidance { get => showObstacleAvoidance; set => showObstacleAvoidance = value; }

        /// <summary>
        /// Shows the rays and hits that repel ground uses
        /// </summary>
        public static bool showRepelGround;// = false;
        public bool ShowRepelGround { get => showRepelGround; set => showRepelGround = value; }

        /// <summary>
        /// Shows spheres that the gaze buffer hit scans and returns look when the user holds gaze long and steady enough
        /// </summary>
        public static bool showGazeBuffer_Target;// = false;
        public bool ShowGazeBuffer_Target { get => showGazeBuffer_Target; set => showGazeBuffer_Target = value; }

        /// <summary>
        /// Shows lines that gaze buffer uses to detect when staring at a consistent offset from forward
        /// </summary>
        public static bool showGazeBuffer_Offset;// = false;
        public bool ShowGazeBuffer_Offset { get => showGazeBuffer_Offset; set => showGazeBuffer_Offset = value; }

        /// <summary>
        /// Shows visuals of 'rotate to look' using gaze buffer results
        /// </summary>
        public static bool showRotateToLook;// = false;
        public bool ShowRotateToLook { get => showRotateToLook; set => showRotateToLook = value; }

        /// <summary>
        /// Focused tester showing world to model rotations and back of head up vector
        /// </summary>
        public static bool showHeadUpRotateVisualizer;// = false;
        public bool ShowHeadUpRotateVisualizer { get => showHeadUpRotateVisualizer; set => showHeadUpRotateVisualizer = value; }

        /// <summary>
        /// Shows points/lines on various transforms of the player avatar
        /// </summary>
        public static bool visualizePlayerPoints;// = false;
        public bool VisualizePlayerPoints { get => visualizePlayerPoints; set => visualizePlayerPoints = value; }

        /// <summary>
        /// This one looks like an early tester of figuring out how to render debug visuals - pretty useless beyond that
        /// </summary>
        public static bool showDebugVisuals;// = false;
        public bool ShowDebugVisuals { get => showDebugVisuals; set => showDebugVisuals = value; }

        /// <summary>
        /// Shows various properties in a textbox
        /// </summary>
        public static bool showDebugStats;// = false;
        public bool ShowDebugStats { get => showDebugStats; set => showDebugStats = value; }

        #endregion
    }
}
