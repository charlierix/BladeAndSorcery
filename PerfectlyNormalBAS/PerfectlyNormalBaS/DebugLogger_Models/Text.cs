using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PerfectlyNormalBaS.DebugLogger_Models
{
    [Serializable]
    public class Text
    {
        public string text;

        //public Color? color;        // optional
        public string color;

        public float? fontsize_mult;        // optional
    }
}
