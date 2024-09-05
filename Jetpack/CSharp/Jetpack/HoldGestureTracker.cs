﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using Valve.VR;
using static UnityEngine.Rendering.DebugUI.Table;

namespace Jetpack
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
        //private const float CLOSED = 0.5f;    // the value when the thumb is on the thumbpad
        private const float OPEN = 0.35f;        // allowing for some fuzziness.  The curl just needs to pass these thresholds
        private const float CLOSED = 0.42f;

        // 0 = thumb ... 4 = pinky
        private readonly bool[] _whichClosed;

        // The time that each hand started holding the position (null if not holding that position)
        private DateTime? _left = null;
        private DateTime? _right = null;

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
        public void Update(InputSteamVR input)
        {
            if (IsHoldingGesture(input.skeletonLeftAction, _whichClosed))
            {
                if (_left == null)
                    _left = DateTime.UtcNow;
            }
            else
            {
                _left = null;
            }

            if (IsHoldingGesture(input.skeletonRightAction, _whichClosed))
            {
                if (_right == null)
                    _right = DateTime.UtcNow;
            }
            else
            {
                _right = null;
            }
        }

        public bool IsHeld(bool require_both, float duration_ms = 750)
        {
            bool is_left = IsHeld_Hand(_left, duration_ms);
            bool is_right = IsHeld_Hand(_right, duration_ms);

            return require_both ?
                is_left && is_right :
                is_left || is_right;
        }
        private static bool IsHeld_Hand(DateTime? hand, float duration_ms)
        {
            if (hand == null)
                return false;

            return (DateTime.UtcNow - hand.Value).TotalMilliseconds >= duration_ms;
        }

        public void Clear()
        {
            _left = null;
            _right = null;
        }

        private static bool IsHoldingGesture(SteamVR_Action_Skeleton hand, bool[] desired)
        {
            if (!MatchesDesired(hand.thumbCurl, desired[0]))
                return false;

            if (!MatchesDesired(hand.indexCurl, desired[1]))
                return false;

            if (!MatchesDesired(hand.middleCurl, desired[2]))
                return false;

            if (!MatchesDesired(hand.ringCurl, desired[3]))
                return false;

            if (!MatchesDesired(hand.pinkyCurl, desired[4]))
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
