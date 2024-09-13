using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PerfectlyNormalUnity.DebugLogger_Models
{
    [Serializable]
    public class LogFrame
    {
        public string name;     // optional

        //public Color? back_color;       // optional
        public string back_color;

        public ItemBase[] items;

        public Text[] text;
    }
}
