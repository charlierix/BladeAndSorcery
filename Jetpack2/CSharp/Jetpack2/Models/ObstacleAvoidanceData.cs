using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace Jetpack2.Models
{
    // This gets populated automatically at load time from the json in deployed configs folder
    public class ObstacleAvoidanceData : CustomData
    {
        /// <summary>
        /// Angle for the point between major and minor axis
        /// </summary>
        /// <remarks>
        /// slider: 0 to 90, step 1
        /// </remarks>
        public static float ellipsePointAngle;// = 55;
        public float EllipsePointAngle { get => ellipsePointAngle; set => ellipsePointAngle = value; }

        /// <summary>
        /// At each point along the perimiter of the ellipse, there will be two diverging rays at an angle (one toward interior, one away from ellipse)
        /// </summary>
        /// <remarks>
        /// slider: 0 to 24, step 1
        /// </remarks>
        public static float ellipseRayAngle;// = 9;
        public float EllipseRayAngle { get => ellipseRayAngle; set => ellipseRayAngle = value; }

        /// <summary>
        /// Max random yaw of each ray source each frame
        /// </summary>
        /// <remarks>
        /// slider: 0 to 45, step 1
        /// </remarks>
        public static float ellipseRayAngleYaw;// = 8;
        public float EllipseRayAngleYaw { get => ellipseRayAngleYaw; set => ellipseRayAngleYaw = value; }

        /// <summary>
        /// Max random pitch of each ray source each frame
        /// </summary>
        /// <remarks>
        /// slider: 0 to 45, step 1
        /// </remarks>
        public static float ellipseRayAnglePitch;// = 12;
        public float EllipseRayAnglePitch { get => ellipseRayAnglePitch; set => ellipseRayAnglePitch = value; }

        /// <summary>
        /// Length of the ray cast (velocity * mult)
        /// </summary>
        /// <remarks>
        /// slider: 0 to 6, step 0.05
        /// </remarks>
        public static float rayDistMult;// = 2f;
        public float RayDistMult { get => rayDistMult; set => rayDistMult = value; }

        /// <summary>
        /// Accel is zero beyond this distance
        /// </summary>
        /// <remarks>
        /// slider: 0 to 24, step 0.5
        /// </remarks>
        public static float analyze_MaxDist;// = 6f;
        public float Analyze_MaxDist { get => analyze_MaxDist; set => analyze_MaxDist = value; }

        /// <summary>
        /// Percent Dropoff is 1-x^n
        /// </summary>
        /// <remarks>
        /// slider: 1 to 6, step 0.25
        /// </remarks>
        public static float analyze_DropoffPow;// = 3f;
        public float Analyze_DropoffPow { get => analyze_DropoffPow; set => analyze_DropoffPow = value; }

        /// <summary>
        /// Accel is reduced based on speed * this
        /// </summary>
        /// <remarks>
        /// slider: 0 to 2, step 0.05
        /// </remarks>
        public static float analyze_SpeedMult;// = 0.75f;
        public float Analyze_SpeedMult { get => analyze_SpeedMult; set => analyze_SpeedMult = value; }

        /// <summary>
        /// When dot product of input and accel is negative, this is how negative the dot is before accel starts getting cancelled out
        /// </summary>
        /// <remarks>
        /// slider: 0 to 1, step 0.05
        /// </remarks>
        public static float dontFight_DotThreshold_Start;// = 0.1f;
        public float DontFight_DotThreshold_Start { get => dontFight_DotThreshold_Start; set => dontFight_DotThreshold_Start = value; }

        /// <summary>
        /// When dot product of input and accel is negative, this is how negative the dot is when accel is fully cancelled out
        /// </summary>
        /// <remarks>
        /// slider: 0 to 1, step 0.05
        /// </remarks>
        public static float dontFight_DotThreshold_Full;// = 0.7f;
        public float DontFight_DotThreshold_Full { get => dontFight_DotThreshold_Full; set => dontFight_DotThreshold_Full = value; }
    }
}
