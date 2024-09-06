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

        public static void Update_KeyTracker(KeyDoublePressTracker tracker)
        {
            if (PlayerControl.loader == PlayerControl.Loader.OpenVR)
            {
                InputSteamVR input = (InputSteamVR)PlayerControl.input;
                tracker.Update(NormalizeCurl_Index(input.skeletonLeftAction.thumbCurl), NormalizeCurl_Index(input.skeletonRightAction.thumbCurl));
            }
        }
        public static void Update_GestureTracker(HoldGestureTracker tracker)
        {
            if (PlayerControl.loader == PlayerControl.Loader.OpenVR)
            {
                InputSteamVR input = (InputSteamVR)PlayerControl.input;

                float[] left = new[] { NormalizeCurl_Index(input.skeletonLeftAction.thumbCurl), NormalizeCurl_Index(input.skeletonLeftAction.indexCurl), NormalizeCurl_Index(input.skeletonLeftAction.middleCurl), NormalizeCurl_Index(input.skeletonLeftAction.ringCurl), NormalizeCurl_Index(input.skeletonLeftAction.pinkyCurl )};
                float[] right = new[] { NormalizeCurl_Index(input.skeletonRightAction.thumbCurl), NormalizeCurl_Index(input.skeletonRightAction.indexCurl), NormalizeCurl_Index(input.skeletonRightAction.middleCurl), NormalizeCurl_Index(input.skeletonRightAction.ringCurl), NormalizeCurl_Index(input.skeletonRightAction.pinkyCurl )};

                tracker.Update(left, right);
            }
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

        private static float NormalizeCurl_Index(float curl)
        {
            // Index is 0 when open, .5 when closed.  Changing to go 0 to 1
            return Mathf.Clamp01(curl * 2);
        }
    }
}
