using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;


// look at:
// PlayerControl.GetHand(Side.Right).usePressed


namespace Jetpack.InputWatchers
{
    /// <summary>
    /// This watches for both thumbpads to be double clicked at the same time
    /// Once in a doubleclicked state, clear needs to be called before it starts watching again
    /// </summary>
    /// <remarks>
    /// There doesn't seem to be an action that fires.  Maybe input.telekinesisRepel2Action.onStateDown, but I never
    /// saw it fire (may need gravity spell)
    /// 
    /// So this looks at the thumb curl percent, looking for rapid changes
    /// </remarks>
    public class KeyDoublePressTracker
    {
        #region Declaration Section

        private const double SYNC_MILLISECONDS = 250;

        private KeyDoublePressTracker_SingleKey _left = new KeyDoublePressTracker_SingleKey();
        private KeyDoublePressTracker_SingleKey _right = new KeyDoublePressTracker_SingleKey();

        #endregion

        public bool WasBothDoubleClicked { get; set; }

        DateTime _prevTime = DateTime.UtcNow;

        public void Update(InputSteamVR input)
        {
            // It was averaging 10 - 15 ms
            //DateTime now = DateTime.UtcNow;
            //Debug.Log($"KeyDoublePressTracker Tick: {(now - _prevTime).TotalMilliseconds:N0}");
            //_prevTime = now;

            if (WasBothDoubleClicked)
                return;

            _left.Update(input.skeletonLeftAction.thumbCurl);
            _right.Update(input.skeletonRightAction.thumbCurl);

            if (_left.DoubleClickTime == null || _right.DoubleClickTime == null)
                return;

            if (Math.Abs((_left.DoubleClickTime.Value - _right.DoubleClickTime.Value).TotalMilliseconds) <= SYNC_MILLISECONDS)
                WasBothDoubleClicked = true;
        }

        public void Clear()
        {
            WasBothDoubleClicked = false;
            _left.Clear();
            _right.Clear();
        }
    }

    /// <summary>
    /// Looks for the thumb starting open, then close (single click), then another open and close
    /// That all has to happen within a certain amount of time
    /// </summary>
    public class KeyDoublePressTracker_SingleKey
    {
        #region class: ThresholdCrossed

        private class ThresholdCrossed
        {
            // True: Open toward Closed
            // False: Closed toward touching pad
            // NOTE: Only keeping track of down strokes (thumb going from open to closed)
            public bool ThroughOpen { get; set; }

            // When it happened (UTC)
            public DateTime Time { get; set; }

            public override string ToString()
            {
                string desc = ThroughOpen ?
                    "Thru Open" :
                    "Thru Close";

                return $"{desc} {Time.ToLocalTime():HH:mm:ss.fff}";
            }
        }

        #endregion

        #region Declaration Section

        //private const float OPEN = 0f;        // the value when the thumb is fully open
        //private const float CLOSED = 0.5f;    // the value when the thumb is on the thumbpad
        private const float OPEN = 0.2f;        // allowing for some fuzziness.  The curl just needs to pass these thresholds
        private const float CLOSED = 0.42f;

        private const double TOTAL_MILLISECONDS = 650;

        private List<ThresholdCrossed> _thresholds = new List<ThresholdCrossed>();      // count is kept to four or less

        private float _prev = (OPEN + CLOSED) / 2f;

        #endregion

        public DateTime? DoubleClickTime { get; set; }

        public void Update(float value)
        {
            if (_prev <= OPEN && value > OPEN)
            {
                AddThreshold(true);
            }
            
            if (_prev < CLOSED && value >= CLOSED)      // it's possible to cross both the open and closed thresholds in a single tick
            {
                AddThreshold(false);
                CheckIfDoubleClick();
            }

            _prev = value;
        }

        public void Clear()
        {
            DoubleClickTime = null;
            _thresholds.Clear();
        }

        #region Private Methods

        private void AddThreshold(bool through_open)
        {
            _thresholds.Add(new ThresholdCrossed
            {
                ThroughOpen = through_open,
                Time = DateTime.UtcNow,
            });

            while (_thresholds.Count > 4)
                _thresholds.RemoveAt(0);
        }

        private void CheckIfDoubleClick()
        {
            // Make sure there are at least four threshold events
            if (_thresholds.Count < 4)
                return;

            // Walk back four, looking for alternating close/open
            bool expected = false;      // it needs to end closed
            for (int i = _thresholds.Count - 1; i >= _thresholds.Count - 4; i--)
            {
                if (_thresholds[i].ThroughOpen != expected)
                    return;

                expected = !expected;
            }

            // Make sure elapsed time is small enough
            if ((_thresholds[_thresholds.Count - 1].Time - _thresholds[_thresholds.Count - 4].Time).TotalMilliseconds > TOTAL_MILLISECONDS)
                return;

            // DoubleClick detected
            DoubleClickTime = _thresholds[_thresholds.Count - 1].Time;

            //Debug.Log($"Double Click: {(_thresholds[_thresholds.Count - 1].Time - _thresholds[_thresholds.Count - 4].Time).TotalMilliseconds:N0}");
        }

        #endregion
    }
}
