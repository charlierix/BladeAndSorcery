using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalUnity.FollowDirectionPosition
{
    /// <summary>
    /// This chases an orientation
    /// </summary>
    public class FollowOrientation
    {
        #region Declaration Section

        private readonly Rigidbody _body;

        public readonly Vector3 _initialDirectionLocal;

        private readonly FollowOrientation_Worker[] _workers;

        /// <summary>
        /// If they are using a spring, this is the point to move to
        /// </summary>
        public Vector3? _desiredOrientation = null;

        #endregion

        #region Constructor

        /// <remarks>
        /// Note that the offset is stored when the constructor is called.  Calls to SetOrientation will rotate the
        /// body to that direction relative to this initial direction
        /// </remarks>
        /// <param name="body">The item that will be rotated</param>
        /// <param name="directionWorld">
        /// This is the default direction.  Think of it like a lever.  When a different direction is passed into
        /// SetOrientation(), the orientation will be changed so that this direction aligns with the new direction
        /// </param>
        /// <param name="workers">
        /// These tell this class how to get to the desired direction.  Think in terms of a car's suspension - there
        /// is a spring, dampening, etc.  Call GetStandard() for a good default set
        /// </param>
        public FollowOrientation(Rigidbody body, FollowOrientation_Worker[] workers, Vector3 directionWorld)
        {
            _body = body;

            _initialDirectionLocal = _body.rotation.FromWorld(directionWorld);

            _workers = workers;
        }

        #endregion

        #region Public Properties

        // If these are populated, then a torque will get reduced if it will exceed one of these
        public float? MaxTorque { get; set; }
        public float? MaxAcceleration { get; set; }

        /// <summary>
        /// This gives an option of only applying a percent of the full force
        /// </summary>
        /// <remarks>
        /// This way objects can be gradually ramped up to full force (good when first creating an object)
        /// </remarks>
        public float Percent { get; set; } = 1f;

        #endregion

        #region Public Methods

        /// <summary>
        /// This returns a definition that is pretty good
        /// </summary>
        public static FollowOrientation_Worker[] GetStandard(float mult = 1f)
        {
            mult *= 48;

            var retVal = new List<FollowOrientation_Worker>();

            // Attraction
            GradientEntry[] gradient = new[]
            {
                new GradientEntry(0f, 0f),
                new GradientEntry(2.5f, 0f),        // giving it a dead spot so it doesn't jitter
                new GradientEntry(3f, 1f),
            };
            retVal.Add(new FollowOrientation_Worker(FollowDirectionType.Attract_Direction, mult, gradient: gradient));

            // Without orth drag, the body will orbit the point like a cone
            gradient = new[]        //TODO: See if this is really needed for standard.  It was originally there so asteroid miner ship could spin freely
            {
                new GradientEntry(0f, 0f),     // distance, %
                //new GradientEntry(1f, 1f),
                new GradientEntry(5f, 1f),
            };
            retVal.Add(new FollowOrientation_Worker(FollowDirectionType.Drag_Velocity_Orth, .9f * mult, gradient: gradient));

            // This creates a drag when the body's rotation overshoots the point
            gradient = new[]
            {
                new GradientEntry(0f, 0f),
                new GradientEntry(2f, 1f),
            };
            retVal.Add(new FollowOrientation_Worker(FollowDirectionType.Drag_Velocity_AlongIfVelocityAway, .9f * mult, gradient: gradient));

            // As it gets close the the direction, this ramps up the drag, helping to reduce overshooting
            gradient = new[]
            {
                new GradientEntry(0f, 0f),
                new GradientEntry(.1f, 1f),
                new GradientEntry(4f, 0f),
            };
            retVal.Add(new FollowOrientation_Worker(FollowDirectionType.Drag_Velocity_AlongIfVelocityToward, 1.2f * mult, gradient: gradient));

            // This is a small constant drag to help dampen the rotation
            retVal.Add(new FollowOrientation_Worker(FollowDirectionType.Drag_Velocity_Any, .05f * mult));

            return retVal.ToArray();
        }

        public void SetOrientation(Vector3 direction)
        {
            _desiredOrientation = direction;
        }
        public void StopChasing()
        {
            _desiredOrientation = null;
        }

        public void Tick()
        {
            if (_desiredOrientation == null)
                return;

            Quaternion rotation = Quaternion.FromToRotation(_body.rotation.ToWorld(_initialDirectionLocal), _desiredOrientation.Value);
            if (rotation == Quaternion.identity)        // there might be some exotic torque definitions that only care about velocity.  Just wait a frame and the diff won't be identity
                return;

            var args = new FollowOrientation_Args(_body.angularVelocity, rotation);

            // Call each worker
            foreach (var worker in _workers)
            {
                Vector3? localForce = worker.GetTorque(args);

                if (localForce == null)
                    continue;

                ForceMode mode = worker.IsAccel ?
                    ForceMode.Acceleration :
                    ForceMode.Force;

                _body.AddTorque(localForce.Value * Percent, mode);
            }
        }

        #endregion
    }
}
