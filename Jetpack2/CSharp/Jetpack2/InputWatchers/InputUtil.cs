using System;
using System.Collections.Concurrent;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.InputWatchers
{
    /// <summary>
    /// Listens to events and state of controllers, exposes result props
    /// </summary>
    public class InputUtil
    {
        public const float CURL_CLOSED = 0.9f;     // how far the finger should be closed to consider it closed

        // key is side+button.  if the key isn't in the dictionary, assume false
        private static Lazy<ConcurrentDictionary<(Side side, PlayerControl.Hand.Button button), bool>> _button_state = new Lazy<ConcurrentDictionary<(Side side, PlayerControl.Hand.Button button), bool>>(() => new ConcurrentDictionary<(Side side, PlayerControl.Hand.Button button), bool>());

        public void HookEvents()
        {
            //PlayerControl.local.OnButtonPressEvent
            //PlayerControl.local.OnJoystickMoveEvent
            //PlayerControl.local.OnJumpButtonEvent

            _button_state.Value.Clear();

            PlayerControl.local.OnButtonPressEvent += Local_OnButtonPressEvent;
        }
        public void UnHookEvents()
        {
            PlayerControl.local.OnButtonPressEvent -= Local_OnButtonPressEvent;

            _button_state.Value.Clear();
        }

        public static void Update_KeyTracker(KeyDoublePressTracker tracker, bool include_thumbcurl_foralt = true)
        {
            _button_state.Value.TryGetValue((Side.Left, PlayerControl.Hand.Button.AlternateUse), out bool left_alt_pressed);        // if the entry isn't in the dictionary, the bool will get the default value of false
            _button_state.Value.TryGetValue((Side.Right, PlayerControl.Hand.Button.AlternateUse), out bool right_alt_pressed);

            float left_curl = MergeButtonWithCurl_AsFloat(left_alt_pressed, Side.Left, include_thumbcurl_foralt);
            float right_curl = MergeButtonWithCurl_AsFloat(right_alt_pressed, Side.Right, include_thumbcurl_foralt);

            //float left_curl = left_alt_pressed ? 1 : 0;
            //float right_curl = right_alt_pressed ? 1 : 0;

            tracker.Update(left_curl, right_curl);
        }

        public static void Update_GestureTracker(HoldGestureTracker tracker)
        {
            float[] left = new[] { PlayerControl.handLeft.thumbCurl, PlayerControl.handLeft.indexCurl, PlayerControl.handLeft.middleCurl, PlayerControl.handLeft.ringCurl, PlayerControl.handLeft.littleCurl };
            float[] right = new[] { PlayerControl.handRight.thumbCurl, PlayerControl.handRight.indexCurl, PlayerControl.handRight.middleCurl, PlayerControl.handRight.ringCurl, PlayerControl.handRight.littleCurl };

            tracker.Update(left, right);
        }

        // Returns the left and right thumbstick positions
        public static Vector2 GetLeftStick()
        {
            return PlayerControl.handLeft.JoystickAxis;
        }
        public static Vector2 GetRightStick()
        {
            return PlayerControl.handRight.JoystickAxis;
        }

        public static bool IsButtonPressed(Side side, PlayerControl.Hand.Button button, bool include_thumbcurl_foralt = true)
        {
            _button_state.Value.TryGetValue((side, button), out bool retVal);        // if the entry isn't in the dictionary, the bool will get the default value of false

            // keep getting false hits when touching the thumbsticks
            //if (button == PlayerControl.Hand.Button.AlternateUse)
            //    retVal = MergeButtonWithCurl_AsBool(retVal, side, include_thumbcurl_foralt);

            return retVal;
        }

        private void Local_OnButtonPressEvent(PlayerControl.Hand hand, PlayerControl.Hand.Button button, bool pressed)
        {
            // hand.Side
            //     Right,
            //     Left

            // Button
            //     Use,
            //     AlternateUse,
            //     Grip,
            //     Stick

            var key = (hand.side, button);
            _button_state.Value[key] = pressed;
        }

        private static bool MergeButtonWithCurl_AsBool(bool state, Side side, bool should_merge)
        {
            if (!should_merge)
                return state;

            float curl = side == Side.Left ?
                PlayerControl.handLeft.thumbCurl :
                PlayerControl.handRight.thumbCurl;

            if (curl >= CURL_CLOSED)
                return true;

            return state;
        }
        private static float MergeButtonWithCurl_AsFloat(bool state, Side side, bool should_merge)
        {
            // Convert the bool to float
            float ret_button = state ?
                1 :
                0;

            if (!should_merge)
                return ret_button;

            // The double click tracker has a different threshold for open (not zero), so return whatever curl
            // is there.  Unless curl doesn't work with this controller, then there's a chance curl will be
            // zero, but the button handler saw button as pressed and the value is one
            float curl = side == Side.Left ?
                PlayerControl.handLeft.thumbCurl :
                PlayerControl.handRight.thumbCurl;

            return Mathf.Max(ret_button, curl);
        }
    }
}
