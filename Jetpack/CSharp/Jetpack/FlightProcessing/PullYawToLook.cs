using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.FlightProcessing
{
    /// <summary>
    /// While flying, this will slowly rotate the player's body toward look direction
    /// </summary>
    public class PullYawToLook
    {
        private readonly ITriangle _xzplane = new Triangle(new Vector3(1, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 1));

        private float _capacitor = 0f;

        DateTime _prevTick = DateTime.UtcNow;

        // This should be called when entering/leaving flight
        public void Clear()
        {
            _capacitor = 0f;
            _prevTick = DateTime.UtcNow;
        }

        // This should only be called while in flight
        public void Update()
        {
            DateTime now = DateTime.UtcNow;
            float elapsedSeconds = (float)Math1D.Clamp((now - _prevTick).TotalSeconds, 0, 0.25);
            _prevTick = now;

            if (!JetpackScript.ShouldYawToLook)
                return;

            // Get look and forward snapped to the xz plane
            Vector3 look = Player.local.head.transform.forward.GetProjectedVector(_xzplane).normalized;
            Vector3 forward = Player.local.transform.forward.GetProjectedVector(_xzplane).normalized;

            // Update the capacitor
            UpdateCapacitor2(look, forward, elapsedSeconds);


            // draw the two lines in an overhead view
            // draw the capcitor charge like a progress bar: a line with a dot, text underneath



        }

        /// <summary>
        /// Updates the charge state of the capacitor based on the alignment between look and forward vectors.
        /// Uses a power function for charge/discharge to create a slow-to-start, fast-to-fill behavior.
        /// </summary>
        /// <param name="look">Normalized direction vector the user is looking</param>
        /// <param name="forward">Normalized direction vector of the object's forward direction</param>
        /// <param name="elapsedSeconds">Time since last update in seconds</param>
        private void UpdateCapacitor1(Vector3 look, Vector3 forward, float elapsedSeconds)
        {
            // Constants for tuning the capacitor behavior
            const float THRESHOLD = 0.95f;       // Minimum dot product to consider direction consistent
            const float CHARGE_SPEED = 5.0f;     // Base charge rate multiplier
            const float CHARGE_POWER = 2.0f;     // Exponent for charge acceleration
            const float DECAY_SPEED = 3.0f;      // Base discharge rate multiplier
            const float DECAY_POWER = 1.5f;      // Exponent for discharge rate

            // Calculate alignment between look and forward directions
            float dot = Vector3.Dot(look, forward);

            if (dot > THRESHOLD)
            {
                // When consistent, charge capacitor with power-based acceleration
                float chargeFactor = dot - THRESHOLD;
                float chargeRate = CHARGE_SPEED * Mathf.Pow(chargeFactor, CHARGE_POWER);
                _capacitor += chargeRate * (float)elapsedSeconds;
            }
            else
            {
                // When inconsistent, discharge with power-based decay
                float decayFactor = 1 - dot;
                float decayRate = DECAY_SPEED * Mathf.Pow(decayFactor, DECAY_POWER);
                _capacitor -= decayRate * (float)elapsedSeconds;
            }

            // Ensure capacitor stays within valid range
            _capacitor = Mathf.Clamp(_capacitor, 0f, 1f);
        }
        /// <summary>
        /// Updates the charge state of the capacitor with a neutral window between thresholds.
        /// </summary>
        private void UpdateCapacitor2(Vector3 look, Vector3 forward, double elapsedSeconds)
        {
            // Thresholds for capacitor behavior
            const float LOWER_THRESHOLD = 0.90f;    // Start discharging below this
            const float UPPER_THRESHOLD = 0.95f;    // Start charging above this
            const float CHARGE_SPEED = 5.0f;
            const float CHARGE_POWER = 2.0f;
            const float DECAY_SPEED = 3.0f;
            const float DECAY_POWER = 1.5f;

            // Calculate alignment between look and forward directions
            float dot = Vector3.Dot(look, forward);

            // Determine which region we're in
            if (dot > UPPER_THRESHOLD)
            {
                // CHARGE REGION: dot > upper threshold
                // Normalize to 0-1 range based on available threshold window
                float chargeFactor = (dot - UPPER_THRESHOLD) / (1f - UPPER_THRESHOLD);
                float chargeRate = CHARGE_SPEED * Mathf.Pow(chargeFactor, CHARGE_POWER);
                _capacitor += chargeRate * (float)elapsedSeconds;
            }
            else if (dot < LOWER_THRESHOLD)
            {
                // DISCHARGE REGION: dot < lower threshold
                // Normalize to 0-1 range based on threshold position
                float decayFactor = (LOWER_THRESHOLD - dot) / LOWER_THRESHOLD;
                float decayRate = DECAY_SPEED * Mathf.Pow(decayFactor, DECAY_POWER);
                _capacitor -= decayRate * (float)elapsedSeconds;
            }
            // else: NEUTRAL WINDOW - capacitor remains unchanged

            // Enforce capacitor bounds
            _capacitor = Mathf.Clamp(_capacitor, 0f, 1f);
        }

    }
}
