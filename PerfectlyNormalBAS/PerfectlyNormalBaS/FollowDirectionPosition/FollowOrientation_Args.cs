using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalUnity.FollowDirectionPosition
{
    public class FollowOrientation_Args
    {
        public FollowOrientation_Args(Vector3 angularVelocity, Quaternion rotation)
        {
            rotation.ToAngleAxis(out Rotation_Angle, out Rotation_Axis);
            Vector3 direction = Rotation_Axis;

            // Angular Velocity
            AngVelocityLength = angularVelocity.magnitude;
            AngVelocityUnit = angularVelocity.normalized;

            // Along
            Vector3 velocityAlong = (AngVelocityUnit * AngVelocityLength).GetProjectedVector(direction);
            AngVelocityAlongLength = velocityAlong.magnitude;
            AngVelocityAlongUnit = velocityAlong.normalized;
            IsAngVelocityAlongTowards = Vector3.Dot(direction, AngVelocityUnit) > 0d;

            // Orth
            Vector3 orth = Vector3.Cross(direction, AngVelocityUnit);       // the first cross is orth to both (outside the plane)
            orth = Vector3.Cross(orth, direction);       // the second cross is in the plane, but orth to distance
            Vector3 velocityOrth = (AngVelocityUnit * AngVelocityLength).GetProjectedVector(orth);

            AngVelocityOrthLength = velocityOrth.magnitude;
            AngVelocityOrthUnit = velocityOrth.normalized;
        }

        public readonly Vector3 Rotation_Axis;
        public readonly float Rotation_Angle;

        public readonly Vector3 AngVelocityUnit;
        public readonly float AngVelocityLength;

        public readonly bool IsAngVelocityAlongTowards;
        public readonly Vector3 AngVelocityAlongUnit;
        public readonly float AngVelocityAlongLength;

        public readonly Vector3 AngVelocityOrthUnit;
        public readonly float AngVelocityOrthLength;
    }
}
