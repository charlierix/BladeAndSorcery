using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.InputWatchers
{
    public class FlightTransitionWatcher
    {
        private FlightActivationType? _activation_type = null;

        public void Update(FlightActivationType activation_type, bool requireBothHands, bool is_flying)
        {
            // detect a switch in transition type from last update.  if so, reset trackers
            if (_activation_type == null || _activation_type.Value != activation_type)
                ChangeActivationType(activation_type);

            if (_activation_type == null)       // null if there is an error setting it up
                return;

            // call corresponding tracker (if there's a need to based on current state)
        }

        private void ChangeActivationType(FlightActivationType activation_type)
        {
            _activation_type = activation_type;

            switch (activation_type)
            {
                case FlightActivationType.HoldUp:
                    break;

                case FlightActivationType.HoldJump:
                    break;

                case FlightActivationType.DoubleJump:
                    break;

                case FlightActivationType.DoubleClick_Use:
                    break;

                case FlightActivationType.HoldBird:
                    break;

                case FlightActivationType.HoldPeace:
                    break;

                case FlightActivationType.HoldDevilHorns:
                    break;

                case FlightActivationType.HoldRockOn:
                    break;

                default:
                    _activation_type = null;
                    Debug.Log($"Unexpected {nameof(FlightActivationType)}: {activation_type}");
                    break;
            }
        }

    }

    public enum FlightActivationType
    {
        HoldUp,
        HoldJump,
        DoubleJump,

        DoubleClick_Use,

        HoldBird,
        HoldPeace,
        HoldDevilHorns,
        HoldRockOn,
    }
}
