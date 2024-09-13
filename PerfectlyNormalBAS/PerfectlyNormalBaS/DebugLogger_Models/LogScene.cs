using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfectlyNormalBaS.DebugLogger_Models
{
    [Serializable]
    public class LogScene
    {
        public Category[] categories;

        public LogFrame[] frames;

        public Text[] text;
    }
}
