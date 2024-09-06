using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using Valve.VR;

namespace Jetpack.InputWatchers
{
    // Looks for both hands holding fingers in a certain position for some amount of time
    public class HoldGestureTracker
    {
        #region enum: CurlState

        private enum CurlState
        {
            Open,
            Closed,
            Intermediate,
        }

        #endregion

        //private const float OPEN = 0f;        // the value when the thumb is fully open
        //private const float CLOSED = 1f;      // the value when the thumb is on the thumbpad
        private const float OPEN = 0.6f;        // allowing for some fuzziness.  The curl just needs to pass these thresholds
        private const float CLOSED = 0.8f;

        // TODO: may want to ignore ring finger when pinky needs to be open

        // 0 = thumb ... 4 = pinky
        private readonly bool[] _whichClosed;

        // The time that each hand started holding the position (null if not holding that position)
        private DateTime? _left = null;
        private DateTime? _right = null;

        private bool _requirereset_left = false;
        private bool _requirereset_right = false;

        /// <summary>
        /// Sets up to watch each finger's position
        /// </summary>
        public HoldGestureTracker(bool thumb_closed, bool index_closed, bool middle_closed, bool ring_closed, bool pinky_closed)
        {
            _whichClosed = new[] { thumb_closed, index_closed, middle_closed, ring_closed, pinky_closed };
        }

        /// <summary>
        /// Call this regularly.  If there's a break in holding the gesture, it will need to start over
        /// </summary>
        public void Update(float[] left, float[] right)
        {
            if (IsHoldingGesture(left, _whichClosed))
            {
                if (_left == null)
                    _left = DateTime.UtcNow;
            }
            else
            {
                _left = null;

                if (_requirereset_left)
                    _requirereset_left = false;
            }

            if (IsHoldingGesture(right, _whichClosed))
            {
                if (_right == null)
                    _right = DateTime.UtcNow;
            }
            else
            {
                _right = null;

                if (_requirereset_right)
                    _requirereset_right = false;
            }
        }

        public bool IsHeld(bool require_both, float duration_ms = 500)
        {
            bool is_left = IsHeld_Hand(_left, _requirereset_left, duration_ms);
            bool is_right = IsHeld_Hand(_right, _requirereset_right, duration_ms);

            return require_both ?
                is_left && is_right :
                is_left || is_right;
        }
        private static bool IsHeld_Hand(DateTime? hand, bool require_reset, float duration_ms)
        {
            if (hand == null || require_reset)
                return false;

            return (DateTime.UtcNow - hand.Value).TotalMilliseconds >= duration_ms;
        }

        public void Clear()
        {
            _left = null;
            _right = null;

            _requirereset_left = false;
            _requirereset_right = false;
        }

        /// <summary>
        /// IsHeld won't return true until they've released their gesture, then done the gesture again
        /// </summary>
        public void RequireReset()
        {
            _requirereset_left = true;
            _requirereset_right = true;
        }

        private static bool IsHoldingGesture(float[] hand, bool[] desired)
        {
            if (hand.Length != desired.Length)
                throw new ArgumentException($"hand and desired aren't the same length (should be 5).  hand: {hand.Length}, desired: {desired.Length}");

            for (int i = 0; i < hand.Length; i++)
                if (!MatchesDesired(hand[i], desired[i]))
                    return false;

            return true;
        }

        private static bool MatchesDesired(float value, bool desired)
        {
            CurlState state = GetCurlState(value);

            return desired ?
                state == CurlState.Closed :
                state == CurlState.Open;
        }

        private static CurlState GetCurlState(float value)
        {
            if (value <= OPEN)
                return CurlState.Open;

            else if (value >= CLOSED)
                return CurlState.Closed;

            else
                return CurlState.Intermediate;
        }
    }
}
