using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jetpack2.FlightProcessing
{
    /// <summary>
    /// The rigid body tied to the player seems to have constaints on rotation around axis other than Y.  Instead of trying
    /// to lift those constraints, this takes angular accelerations and manages angular velocity locally, then directly
    /// rotates the player
    /// </summary>
    public class AngularVelocityProcessor
    {
        private readonly PlayerRotator _rotator;

        private Vector3 _vel = Vector3.zero;
        private Vector3 _accel = Vector3.zero;

        public AngularVelocityProcessor(PlayerRotator rotator)
        {
            _rotator = rotator;
        }

        public void Clear()
        {
            _vel = Vector3.zero;
        }

        public void Update(Core.UtilJetpack.PlayerVRPoints positions, float elapsed_seconds)
        {
            if (!_vel.IsNearZero())
                _rotator.RotateAround(positions.world.center, _vel.normalized, _vel.magnitude, elapsed_seconds);
        }
        // NOTE: call this after other classes have had a chance to add accels, because this resets accel to zero at the end
        public void UpdateFixed(float elapsed_seconds)
        {
            // Apply drag
            _vel *= Mathf.Clamp01(1f - UIModOptions.AngularDrag * elapsed_seconds);

            // Adjust velocity based accelerations since last update
            _vel += _accel * elapsed_seconds;

            // Reset accel
            _accel = Vector3.zero;
        }

        public void AddAngularAccel(Vector3 angular_accel)
        {
            // don't worry about elapsed time, that will be done when adding to velocity
            _accel += angular_accel;
        }
    }
}
