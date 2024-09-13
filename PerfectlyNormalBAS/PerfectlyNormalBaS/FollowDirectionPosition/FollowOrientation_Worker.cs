using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalUnity.FollowDirectionPosition
{
    public class FollowOrientation_Worker
    {
        #region Constructor

        public FollowOrientation_Worker(FollowDirectionType direction, float value, bool isAccel = true, bool isSpring = false, GradientEntry[] gradient = null)
        {
            if (gradient != null && gradient.Length == 1)
            {
                throw new ArgumentException("Gradient must have at least two items if it is populated");
            }

            Direction = direction;
            Value = value;
            IsAccel = isAccel;
            IsSpring = isSpring;

            if (gradient == null || gradient.Length == 0)
            {
                Gradient = null;
            }
            else
            {
                Gradient = gradient;
            }
        }

        #endregion

        #region Public Properties

        public readonly FollowDirectionType Direction;

        public bool IsDrag => Direction != FollowDirectionType.Attract_Direction;        // Direction is attract, all else is drag

        /// <summary>
        /// True: The value is an acceleration
        /// False: The value is a force
        /// </summary>
        public readonly bool IsAccel;
        /// <summary>
        /// True: The value is multiplied by distance (f=kx)
        /// False: The value is it (f=k)
        /// NOTE: Distance is degrees, so any value from 0 to 180
        /// </summary>
        /// <remarks>
        /// Nothing in this class will prevent you from having this true and a gradient at the same time, but that
        /// would give pretty strange results
        /// </remarks>
        public readonly bool IsSpring;

        public readonly float Value;

        /// <summary>
        /// This specifies varying percents based on distance to target
        /// Item1: Distance (in degrees from 0 to 180)
        /// Item2: Percent
        /// </summary>
        /// <remarks>
        /// If a distance is less than what is specified, then the lowest value gradient stop will be used (same with larger distances)
        /// So you could set up a crude s curve (though I don't know why you would):
        ///     at 5: 25%
        ///     at 20: 75%
        /// 
        /// I think the most common use of the gradient would be to set up a dead spot near 0:
        ///     at 0: 0%
        ///     at 10: 100%
        /// 
        /// Or maybe set up a crude 1/x repulsive force near the destination point:
        ///     at 0: 100%
        ///     at 2: 27%
        ///     at 4: 12%
        ///     at 6: 5.7%
        ///     at 8: 2%
        ///     at 10: 0%
        /// </remarks>
        public readonly GradientEntry[] Gradient;

        #endregion

        #region Public Methods

        public Vector3? GetTorque(FollowOrientation_Args e)
        {
            GetDesiredVector(out Vector3 unit, out float length, e, Direction);
            if (unit.IsNearZero())
                return null;

            float torque = Value;

            // Since this is copied into unity and inertia is a vector, just add directly to the rigid body as an acceleration
            //if (IsAccel)
            //{
            //    // f=ma
            //    torque *= e.MomentInertia;
            //}

            if (IsSpring)
            {
                torque *= e.Rotation_Angle;
            }

            if (IsDrag)
            {
                torque *= -length;       // negative, because it needs to be a drag force
            }

            // Gradient %
            if (Gradient != null)
            {
                torque *= GradientEntry.GetGradientPercent(e.Rotation_Angle, Gradient);
            }

            return unit * torque;
        }

        #endregion

        #region Private Methods

        private static void GetDesiredVector(out Vector3 unit, out float length, FollowOrientation_Args e, FollowDirectionType direction)
        {
            switch (direction)
            {
                case FollowDirectionType.Drag_Velocity_Along:
                    unit = e.AngVelocityAlongUnit;
                    length = e.AngVelocityAlongLength;
                    break;

                case FollowDirectionType.Drag_Velocity_AlongIfVelocityAway:
                    unit = e.AngVelocityAlongUnit;
                    length = Vector3.Dot(e.AngVelocityUnit, e.Rotation_Axis) < 0 ?
                        e.AngVelocityAlongLength :
                        0f;
                    break;

                case FollowDirectionType.Drag_Velocity_AlongIfVelocityToward:
                    unit = e.AngVelocityAlongUnit;
                    length = Vector3.Dot(e.AngVelocityUnit, e.Rotation_Axis) >= 0 ?
                        e.AngVelocityAlongLength :
                        0f;
                    break;

                case FollowDirectionType.Attract_Direction:
                    unit = e.Rotation_Axis;
                    length = e.Rotation_Angle;
                    break;

                case FollowDirectionType.Drag_Velocity_Any:
                    unit = e.AngVelocityUnit;
                    length = e.AngVelocityLength;
                    break;

                case FollowDirectionType.Drag_Velocity_Orth:
                    unit = e.AngVelocityOrthUnit;
                    length = e.AngVelocityOrthLength;
                    break;

                default:
                    throw new ApplicationException($"Unknown DirectionType: {direction}");
            }
        }

        #endregion
    }
}
