using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jetpack2.Scanning
{
    public static class ScanningUtil
    {
        public static Lazy<int> SolidObject_LayerMask = new Lazy<int>(() => GetLayerMask());

        // https://kospy.github.io/BasSDK/Components/Guides/SDK-HowTo/Layers.html
        private static int GetLayerMask()
        {
            return LayerMask.GetMask(
                "Default",      // lots of static objects were this
                                //"TransparentFX",
                                //"Ignore Raycast",
                "Reflections",
                "Water",
                //"UI",
                "PhysicObject",
                "Mirror",
                //"LightProbeVolume",
                //"Touch",
                //"DroppedItem",
                //"MovingItem",
                //"PlayerLocomotionObject",
                //"Ragdoll",
                //"LiquidFlow",
                //"LocomotionOnly",     // this is invisible barriers, like the invisible ceiling of a map
                "SpectatorHide",
                "NoLocomotion",     // various environment items where this (rocks, walls)
                                    //"Highlighter",
                                    //"LoadingCamera",
                                    //"SkyDome",
                "MovingObjectOnly",
                //"PlayerLocomotion",
                //"BodyLocomotion",
                "ItemAndRagdollOnly"
                //"TouchObject"
                //"Avatar",
                //"NPC",
                //"FPVHide",
                //"Zone"
                //"ObjectViewer"
                //"PlayerHandAndFoot"
                );
        }
    }
}
