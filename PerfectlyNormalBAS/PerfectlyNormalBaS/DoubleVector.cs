using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalBaS
{
    public struct DoubleVector
    {
        public Vector3 Standard;
        public Vector3 Orth;

        public DoubleVector(Vector3 standard, Vector3 orthogonalToStandard)
        {
            this.Standard = standard;
            this.Orth = orthogonalToStandard;
        }
        public DoubleVector(float standardX, float standardY, float standardZ, float orthogonalX, float orthogonalY, float orthogonalZ)
        {
            this.Standard = new Vector3(standardX, standardY, standardZ);
            this.Orth = new Vector3(orthogonalX, orthogonalY, orthogonalZ);
        }

        public Quaternion GetRotation(DoubleVector destination)
        {
            return Math3D.GetRotation(this, destination);
        }

        /// <summary>
        /// Rotates the double vector around the angle in degrees
        /// </summary>
        public DoubleVector GetRotatedVector(Vector3 axis, float angle)
        {
            Quaternion quat = Quaternion.AngleAxis(angle, axis);

            return new DoubleVector
            (
                quat * Standard,
                quat * Orth
            );
        }
    }
}
