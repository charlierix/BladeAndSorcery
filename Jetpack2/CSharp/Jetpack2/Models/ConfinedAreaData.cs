using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.Models
{
    // This gets populated automatically at load time from the json in deployed configs folder
    public class ConfinedAreaData : CustomData
    {
        /// <summary>
        /// Multiplied by delta time.  Larger values adjust final blocked percent faster
        /// </summary>
        /// <remarks>
        /// slider values: 0.5f to 4f, step 0.05f
        /// </remarks>
        public static float gainFactor;// = 1.75f;
        public float GainFactor { get => gainFactor; set => gainFactor = value; }

        /// <summary>
        /// How far the rays should go
        /// </summary>
        /// <remarks>
        /// slider values: 6 to 36, step 1
        /// </remarks>
        public static float rayLength;// = 24;
        public float RayLength { get => rayLength; set => rayLength = value; }

        /// <summary>
        /// Ray hit disance / Max Distance is run through a bell curve dropoff.  Higher power makes it drop off faster
        /// </summary>
        /// <remarks>
        /// slider values: 0f to 3f, step 0.05f
        /// </remarks>
        public static float falloffPower;// = 1f;
        public float FalloffPower { get => falloffPower; set => falloffPower = value; }

        //public override void Init()
        //{
        //    base.Init();

        //    Debug.Log($"ConfinedAreaData init.  FalloffPower: {falloffPower}");
        //}
    }
}
