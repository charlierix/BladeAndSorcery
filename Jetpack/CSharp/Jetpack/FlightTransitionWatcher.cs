using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jetpack
{
    public class FlightTransitionWatcher
    {
        //public const string HOLD_UP = "Hold Up (right stick)";      // this can't be used to deactivate flight, so if this option is selected, ignore the deactivateOnGround setting
        //public const string HOLD_JUMP = "Hold Jump";
        //public const string DOUBLE_JUMP = "Double Jump";

        //// For below, add an option to require both hands, or just either hand
        //public const string DOUBLECLICK_USE = "Double Click Use (index only)";      // use?  alternate use?  thumbpad?
        //public const string HOLD_BIRD = "Hold The Bird 🖕 (index only)";
        //public const string HOLD_DEVILHORNS = "Hold Devil Horns 🤘 (index only)";
        //public const string HOLD_ROCKON = "Hold Rock On 🤟 (index only)";
        //public const string HOLD_PEACE = "Hold Peace Sign ✌️ (index only)";



        public const string HOLD_UP_DESC = "Hold Up (right stick)";      // this can't be used to deactivate flight, so if this option is selected, ignore the deactivateOnGround setting
        public const string HOLD_UP = "HoldUp";
        
        public const string HOLD_JUMP_DESC = "Hold Jump";
        public const string HOLD_JUMP = "HoldJump";

        public const string DOUBLE_JUMP_DESC = "Double Jump";
        public const string DOUBLE_JUMP = "DoubleJump";

        // For below, add an option to require both hands, or just either hand
        public const string DOUBLECLICK_USE = "Double Click Use (index only)";      // use?  alternate use?  thumbpad?
        public const string HOLD_BIRD = "Hold The Bird 🖕 (index only)";
        public const string HOLD_DEVILHORNS = "Hold Devil Horns 🤘 (index only)";
        public const string HOLD_ROCKON = "Hold Rock On 🤟 (index only)";
        public const string HOLD_PEACE = "Hold Peace Sign ✌️ (index only)";




        // ModOptionString has an overload with string and int, but nothing for string and enum.  So just define as int constants
        //public const int HOLD_UP = 0;      // this can't be used to deactivate flight, so if this option is selected, ignore the deactivateOnGround setting
        //public const int HOLD_JUMP = 1;
        //public const int DOUBLE_JUMP = 2;

        //// For below, add an option to require both hands, or just either hand
        //public const int DOUBLECLICK_USE = 3;      // use?  alternate use?  thumbpad?
        //public const int HOLD_BIRD = 4;
        //public const int HOLD_DEVILHORNS = 5;
        //public const int HOLD_ROCKON = 6;
        //public const int HOLD_PEACE = 7;

        //private FlightActivationType? _activation_type = null;
        private string _activation_type = null;

        //public void Update(FlightActivationType activation_type, bool is_flying)
        //{
        //    // detect a switch in transition type from last update.  if so, reset trackers
        //    if (_activation_type == null || _activation_type.Value != activation_type)
        //    {
        //        _activation_type = activation_type;
        //        Debug.Log($"Jetpack switching FlightActivationType: {activation_type}");
        //    }

        //    // call corresponding tracker (if there's a need to based on current state)
        //}
        public void Update(string activation_type, bool is_flying)
        {
            // detect a switch in transition type from last update.  if so, reset trackers
            if (_activation_type != activation_type)
            {
                _activation_type = activation_type;
                Debug.Log($"Jetpack switching FlightActivationType: {activation_type}");
            }

            // call corresponding tracker (if there's a need to based on current state)
        }

    }

    public enum FlightActivationType
    {
        HoldUp,
        HoldJump,
        DoubleJump,

        DoubleClick_Use,

        HoldBird,
        HoldDevilHorns,
        HoldRockOn,
        HoldPeace,
    }
}
