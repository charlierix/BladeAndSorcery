using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Windows;

namespace Jetpack
{
    /// <summary>
    /// Listens to events and state of controllers, exposes result props
    /// </summary>
    public class InputListener
    {
        private readonly KeyDoublePressTracker _keyTracker;

        // Should be instantiated from ScriptLoaded
        public InputListener(KeyDoublePressTracker keyTracker)
        {
            _keyTracker = keyTracker;

            //PlayerControl.local.OnButtonPressEvent += Local_OnButtonPressEvent;
        }

        // Called from ScriptUpdate (multiple times a second)
        public void OnUpdate()
        {
            if (PlayerControl.loader == PlayerControl.Loader.OpenVR)
                Update_OpenVR((InputSteamVR)PlayerControl.input);
        }

        /// <summary>
        /// Returns the right thumbstick position
        /// </summary>
        public Vector2 GetRightStick()
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

        private void Update_OpenVR(InputSteamVR input)
        {
            _keyTracker.Update(input);
        }
    }
}
