using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PerfectlyNormalBaS.DebugLogger_Models
{
    [Serializable]
    public abstract class ItemBase
    {
        // All of these properties are optional

        //public Category category;
        public string category;     // the viewer loads this into an intermediate type.  Then it uses this string as a lookup to store in the final type

        //public Color? color;      // unity's json serializer can't handle a lot of types
        public string color;        // AARRGGBB or null

        public float? size_mult;

        public string tooltip;
    }
}
