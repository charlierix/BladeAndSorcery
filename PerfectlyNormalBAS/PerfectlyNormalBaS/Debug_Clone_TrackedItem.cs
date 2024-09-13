using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalBaS
{
    /// <summary>
    /// An instance of Debug_CloneViewer can be placed in the camera game object.  It will show debug visuals in front
    /// of the camera of any game object that has an instance of Debug_Clone_TrackedItem attached
    /// </summary>
    public class Debug_Clone_TrackedItem : MonoBehaviour
    {
        //TODO: Enum for the type of visual

        /// <summary>
        /// Allows for groups of points to be enabled/disabled by the viewer
        /// </summary>
        public string GroupName = "";

        public string Color = "FFF";

        public float Size_Scale = 1f;

        public bool ShowLineToParent = false;

        public bool ShowAxisLines = false;
    }
}
