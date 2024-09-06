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
        private KeyDoublePressTracker _keyDoublePressTracker = null;
        private HoldGestureTracker _gestureTracker = null;

        /// <summary>
        /// Watches the inputs according to activation_type and returns true when is_flying needs to be changed
        /// </summary>
        public bool Update(FlightActivationType activation_type, bool requireBothHands, bool deactivateOnGround, bool is_flying)
        {
            // Detect a switch in transition type from last update.  if so, reset trackers
            if (_activation_type == null || _activation_type.Value != activation_type)
                ChangeActivationType(activation_type);

            if (_activation_type == null)       // null if there is an error setting it up
                return false;

            // Cancel flight if grounded
            if (is_flying && Player.local.locomotion.isGrounded && (deactivateOnGround || AlwaysDeactivateOnGround(_activation_type.Value)))
                return true;

            // Call corresponding tracker
            switch (_activation_type.Value)
            {
                case FlightActivationType.HoldUp:
                    return Update_HoldUp(is_flying);

                case FlightActivationType.DoubleClick_Thumbpad:
                    return Update_DoubleClick_Thumbpad(requireBothHands, deactivateOnGround, is_flying);

                case FlightActivationType.HoldBird:
                case FlightActivationType.HoldPeace:
                case FlightActivationType.HoldDevilHorns:
                case FlightActivationType.HoldRockOn:
                    return Update_Gesture(requireBothHands);

                case FlightActivationType.HoldJump:
                case FlightActivationType.DoubleJump:
                    Debug.Log($"Finish this: {_activation_type.Value}");
                    return false;

                default:
                    Debug.Log($"Unexpected {nameof(FlightActivationType)}: {_activation_type.Value}");
                    return false;
            }
        }

        private bool Update_HoldUp(bool is_flying)
        {
            if (is_flying)
                return false;       // holding up on the right thumbstick will only activate flight, never deactivate

            _holdUpTracker.Update(InputUtil.GetRightStick());

            return _holdUpTracker.IsHeldUp();
        }
        private bool Update_DoubleClick_Thumbpad(bool requireBothHands, bool deactivateOnGround, bool is_flying)
        {
            if (_keyDoublePressTracker == null)
                return false;

            InputUtil.Update_KeyTracker(_keyDoublePressTracker);

            bool retVal = requireBothHands ?
                _keyDoublePressTracker.WasBothDoubleClicked :
                _keyDoublePressTracker.WasEitherDoubleClicked;

            if (retVal)
                _keyDoublePressTracker.Clear();

            return retVal;
        }
        private bool Update_Gesture(bool requireBothHands)
        {
            InputUtil.Update_GestureTracker(_gestureTracker);

            bool retVal = _gestureTracker.IsHeld(requireBothHands);

            if (retVal)
                _gestureTracker.RequireReset();        

            return retVal;
        }

        private void ChangeActivationType(FlightActivationType activation_type)
        {
            _activation_type = activation_type;

            _holdUpTracker = null;
            _keyDoublePressTracker = null;
            _gestureTracker = null;

            switch (activation_type)
            {
                case FlightActivationType.HoldUp:
                    _holdUpTracker = new HoldUpTracker();
                    break;

                case FlightActivationType.HoldJump:
                    break;

                case FlightActivationType.DoubleJump:
                    break;

                case FlightActivationType.DoubleClick_Thumbpad:
                    if (InputUtil.SupportsFingerTracking())
                        _keyDoublePressTracker = new KeyDoublePressTracker();
                    break;

                case FlightActivationType.HoldBird:
                    _gestureTracker = new HoldGestureTracker(true, true, false, true, true);
                    break;

                case FlightActivationType.HoldPeace:
                    _gestureTracker = new HoldGestureTracker(true, false, false, true, true);
                    break;

                case FlightActivationType.HoldDevilHorns:
                    _gestureTracker = new HoldGestureTracker(true, false, true, true, false);
                    break;

                case FlightActivationType.HoldRockOn:
                    _gestureTracker = new HoldGestureTracker(false, false, true, true, false);
                    break;

                default:
                    _activation_type = null;
                    Debug.Log($"Unexpected {nameof(FlightActivationType)}: {activation_type}");
                    break;
            }
        }

        private static bool AlwaysDeactivateOnGround(FlightActivationType activation_type)
        {
            // TODO: HoldJump and DoubleJump need to differentiate between holding up and clicking stick
            return activation_type == FlightActivationType.HoldUp;
        }
    }

    public enum FlightActivationType
    {
        HoldUp,
        HoldJump,
        DoubleJump,

        DoubleClick_Thumbpad,

        HoldBird,
        HoldPeace,
        HoldDevilHorns,
        HoldRockOn,
    }
}
