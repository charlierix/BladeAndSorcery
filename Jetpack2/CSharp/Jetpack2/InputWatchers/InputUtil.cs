using ThunderRoad;
using UnityEngine;

namespace Jetpack2.InputWatchers
{
    /// <summary>
    /// Listens to events and state of controllers, exposes result props
    /// </summary>
    public class InputUtil
    {
        public void HookEvents()
        {
            //PlayerControl.local.OnButtonPressEvent
            //PlayerControl.local.OnJoystickMoveEvent
            //PlayerControl.local.OnJumpButtonEvent
        }

        public void UnHookEvents()
        {
        }



        // TODO: rework this by listening to PlayerControl.local.OnButtonPressEvent
        public static void Update_KeyTracker(KeyDoublePressTracker tracker)
        {
            tracker.Update(PlayerControl.handLeft.thumbCurl, PlayerControl.handRight.thumbCurl);
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
    }
}
