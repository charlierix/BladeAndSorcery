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

        private HoldUpTracker _holdUpTracker = null;

        public bool Update(FlightActivationType activation_type, bool requireBothHands, bool deactivateOnGround, bool is_flying)
        {
            // detect a switch in transition type from last update.  if so, reset trackers
            if (_activation_type == null || _activation_type.Value != activation_type)
                ChangeActivationType(activation_type);

            if (_activation_type == null)       // null if there is an error setting it up
                return false;

            if (is_flying && Player.local.locomotion.isGrounded && (deactivateOnGround || AlwaysDeactivateOnGround(_activation_type.Value)))
                return true;

            // Call corresponding tracker
            switch (_activation_type.Value)
            {
                case FlightActivationType.HoldUp:
                    return Update_HoldUp(is_flying);

                case FlightActivationType.DoubleClick_Use:

                case FlightActivationType.HoldBird:

                case FlightActivationType.HoldPeace:

                case FlightActivationType.HoldDevilHorns:

                case FlightActivationType.HoldRockOn:

                case FlightActivationType.HoldJump:
                case FlightActivationType.DoubleJump:
                    Debug.Log($"Finish this: {_activation_type.Value}");
                    return false;

                default:
                    Debug.Log($"Unexpected {nameof(FlightActivationType)}: {_activation_type.Value}");
                    return false;
            }
        }

        private static bool AlwaysDeactivateOnGround(FlightActivationType activation_type)
        {
            // TODO: HoldJump and DoubleJump need to differentiate between holding up and clicking stick
            return activation_type == FlightActivationType.HoldUp;
        }

        private bool Update_HoldUp(bool is_flying)
        {
            if (is_flying)
                return false;       // holding up on the right thumbstick will only activate flight, never deactivate

            _holdUpTracker.Update(InputListener.GetRightStick());

            return _holdUpTracker.IsHeldUp();
        }

        private void ChangeActivationType(FlightActivationType activation_type)
        {
            _activation_type = activation_type;

            _holdUpTracker = null;

            switch (activation_type)
            {
                case FlightActivationType.HoldUp:
                    _holdUpTracker = new HoldUpTracker();
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
