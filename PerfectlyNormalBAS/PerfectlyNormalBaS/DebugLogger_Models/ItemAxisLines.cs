using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalBaS.DebugLogger_Models
{
    [Serializable]
    public class ItemAxisLines : ItemBase
    {
        //public Vector3 position;
        public string position;

        //public Vector3 axis_x;
        public string axis_x;

        //public Vector3 axis_y;
        public string axis_y;

        //public Vector3 axis_z;
        public string axis_z;

        public float size;
    }
}
