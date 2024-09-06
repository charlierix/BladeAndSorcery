using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.InputWatchers
{
    /// <summary>
    /// Listens to events and state of controllers, exposes result props
    /// </summary>
    public class InputUtil
    {
        public static bool SupportsFingerTracking()
        {
            // TODO: Figure out if there are others
            return PlayerControl.loader == PlayerControl.Loader.OpenVR;
        }

        // TODO: these functions shouldn't be passing the input specific objects into the trackers.  They should abstract out the data and pass that
        public static void Update_KeyTracker(KeyDoublePressTracker tracker)
        {
            if (PlayerControl.loader == PlayerControl.Loader.OpenVR)
                tracker.Update((InputSteamVR)PlayerControl.input);
        }
        public static void Update_GestureTracker(HoldGestureTracker tracker)
        {
            if (PlayerControl.loader == PlayerControl.Loader.OpenVR)
                tracker.Update((InputSteamVR)PlayerControl.input);
        }

        // Returns the left and right thumbstick positions
        public static Vector2 GetLeftStick()
        {
            switch (PlayerControl.loader)
            {
                case PlayerControl.Loader.Oculus:
                    return ((InputXR_Oculus)PlayerControl.input).leftController.thumbstick.GetValue();

                case PlayerControl.Loader.OpenVR:
                    return ((InputSteamVR)PlayerControl.input).moveAction.axis;

                default:
                    throw new ApplicationException($"Unexpected loader {PlayerControl.loader}");
            }
        }
        public static Vector2 GetRightStick()
        {
            switch(PlayerControl.loader)
            {
                case PlayerControl.Loader.Oculus:
                    return ((InputXR_Oculus)PlayerControl.input).rightController.thumbstick.GetValue();

                case PlayerControl.Loader.OpenVR:
                    return ((InputSteamVR)PlayerControl.input).turnAction.axis;

                default:
                    throw new ApplicationException($"Unexpected loader {PlayerControl.loader}");
            }
        }

        private void Local_OnButtonPressEvent(PlayerControl.Hand hand, PlayerControl.Hand.Button button, bool pressed)
        {
            Debug.Log($"{hand.side} {button} {pressed}\r\n{JsonUtility.ToJson(hand, true)}");
        }
    }
}
