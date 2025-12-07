using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace Jetpack2.Models
{
    // This gets populated automatically at load time from the json in deployed configs folder
    public class WingsData : CustomData
    {
        // Dot product between these two is neither wing or airbrake (it's nothing)

        /// <summary>
        /// Dot product above this is airbrake
        /// </summary>
        /// <remarks>
        /// dot product is with velocity, unless standing still, then it's with body forward
        /// </remarks>
        public static float dot_airbrake;// = 0.9f;
        public float Dot_Airbrake { get => dot_airbrake; set => dot_airbrake = value; }

        /// <summary>
        /// Dot product below this is a wing
        /// </summary>
        /// <remarks>
        /// dot product is with velocity, unless standing still, then it's with body forward
        /// </remarks>
        public static float dot_wing;// = 0.8f;
        public float Dot_Wing { get => dot_wing; set => dot_wing = value; }

        // 0 is fully open, 1 is closed.  This adjusts so that min becomes zero, max becomes one, then the percent
        // is between min and max
        public static float openhand_min;// = 0.2f;        // any value below this will be considered fully open (useful if there are slight errors with a finger not fully opening/closing)
        public float OpenHand_Min { get => openhand_min; set => openhand_min = value; }

        public static float openhand_max;// = 0.8f;
        public float Openhand_Max { get => openhand_max; set => openhand_max = value; }

        public static float minSpeed;
        public float MinSpeed { get => minSpeed; set => minSpeed = value; }
    }
}
