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
        private FlightActivationType? _activation_type = null;
        //private string _activation_type = null;

        public void Update(FlightActivationType activation_type, bool requireBothHands, bool is_flying)
        {
            // detect a switch in transition type from last update.  if so, reset trackers
            if (_activation_type == null || _activation_type.Value != activation_type)
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
