using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jetpack.InputWatchers
{
    /// <summary>
    /// Keeps track of whether the thumbstick is held up for some duration
    /// </summary>
    public class HoldUpTracker
    {
        private DateTime? _start = null;

        public void Update(Vector2 thumbstick)
        {
            bool is_up = thumbstick.y > 0.95f;

            if (!is_up)
                _start = null;

            else if (_start == null)
                _start = DateTime.UtcNow;
        }

        public void Clear()
        {
            _start = null;
        }

        public bool IsHeldUp(float duration_ms = 500)
        {
            if (_start == null)
                return false;

            return (DateTime.UtcNow - _start.Value).TotalMilliseconds >= duration_ms;
        }
    }
}
