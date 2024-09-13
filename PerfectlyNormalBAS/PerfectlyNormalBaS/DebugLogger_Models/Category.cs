using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PerfectlyNormalUnity.DebugLogger_Models
{
    [Serializable]
    public class Category
    {
        public string name;

        //public Color? color;        // optional       // serialize has trouble with non strings
        public string color;

        public float? size_mult;        // optional
    }
}
