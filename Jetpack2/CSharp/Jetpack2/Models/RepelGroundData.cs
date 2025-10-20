using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace Jetpack2.Models
{
    // This gets populated automatically at load time from the json in deployed configs folder
    public class RepelGroundData : CustomData
    {
        // --- Distance ---

        /// <summary>
        /// relative to height * scale, taken from foot pos
        /// </summary>
        /// <remarks>
        /// slider: 0 to 1, step 0.01
        /// </remarks>
        public static float maxDistance;// = 0.05f;
        public float MaxDistance { get => maxDistance; set => maxDistance = value; }

        /// <summary>
        /// increases ground distance based on horizontal speed (this * speed)
        /// </summary>
        /// <remarks>
        /// slider: 0 to 1, step 0.01
        /// </remarks>
        public static float horzSpeedDistMult;// = 0.15f;
        public float HorzSpeedDistMult { get => horzSpeedDistMult; set => horzSpeedDistMult = value; }

        /// <summary>
        /// increases ground distance based on vertical speed down (this * speed)
        /// </summary>
        /// <remarks>
        /// slider: 0 to 3, step 0.1
        /// </remarks>
        public static float vertSpeedDistMult;// = 1;
        public float VertSpeedDistMult { get => vertSpeedDistMult; set => vertSpeedDistMult = value; }

        // --- Linear ---

        /// <summary>
        /// a linear gradient where there is zero force at max distance and max force at zero distance
        /// </summary>
        /// <remarks>
        /// slider: 0 to 6, step 0.25
        /// </remarks>
        public static float linear_MaxAccel;// = 2.75f;
        public float Linear_MaxAccel { get => linear_MaxAccel; set => linear_MaxAccel = value; }

        // --- 1/x ---

        /// <summary>
        /// the distance is normalized, where x is 0 to 1
        /// </summary>
        /// <remarks>
        /// slider: 0 to 18, step 0.25
        /// </remarks>
        public static float inverse_MaxAccel;// = 8.5;
        public float Inverse_MaxAccel { get => inverse_MaxAccel; set => inverse_MaxAccel = value; }

        /// <summary>
        /// any value less than 4 is meaningless (plot it in desmos for easy visualization/manipulation)
        /// </summary>
        /// <remarks>
        /// slider: 4 to 24, step 0.25
        /// </remarks>
        public static float inverse_C;// = 8;
        public float Inverse_C { get => inverse_C; set => inverse_C = value; }

        // --- 1/x^2 ---

        /// <summary>
        /// the distance is normalized, where x is 0 to 1
        /// </summary>
        /// <remarks>
        /// slider: 0 to 40, step 0.25
        /// </remarks>
        public static float inverseSqr_MaxAccel;// = 19.25;
        public float InverseSqr_MaxAccel { get => inverseSqr_MaxAccel; set => inverseSqr_MaxAccel = value; }

        /// <summary>
        /// any value less than 4 is meaningless (plot it in desmos for easy visualization/manipulation)
        /// </summary>
        /// <remarks>
        /// slider: 4 to 80, step 0.5
        /// </remarks>
        public static float inverseSqr_C;// = 4;
        public float InverseSqr_C { get => inverseSqr_C; set => inverseSqr_C = value; }

        /// <summary>
        /// stop accelerating upward beyond this speed (to avoid excessive pop up speeds)
        /// </summary>
        /// <remarks>
        /// slider: 0 to 1, step 0.01
        /// </remarks>
        public static float upSpeed_ZeroAccel;// = 0.04f;
        public float UpSpeed_ZeroAccel { get => upSpeed_ZeroAccel; set => upSpeed_ZeroAccel = value; }
    }
}
