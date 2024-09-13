using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PerfectlyNormalBaS.DebugLogger_Models
{
    [Serializable]
    public class ItemCircle_Edge : ItemBase
    {
        //public Vector3 center;
        public string center;       // "x, y, z"

        //public Vector3 normal;
        public string normal;

        public float radius;
    }
}
