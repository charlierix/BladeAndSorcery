using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalBaS
{
    /// <summary>
    /// This is an easy way to store an HSV color definition
    /// </summary>
    public struct ColorHSV
    {
        #region Constructor

        public ColorHSV(float h, float s, float v)
            : this(h, s, v, 1) { }
        public ColorHSV(float h, float s, float v, float a)
        {
            H = h;
            S = s;
            V = v;
            A = a;
        }
        public ColorHSV(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);

            H = h;
            S = s;
            V = v;
            A = color.a;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Hue: 0 to 1
        /// </summary>
        public readonly float H;

        /// <summary>
        /// Saturation: 0 to 1
        /// </summary>
        public readonly float S;

        /// <summary>
        /// Value: 0 to 1
        /// </summary>
        public readonly float V;

        /// <summary>
        /// Alpha: 0 to 1
        /// </summary>
        public readonly float A;

        #endregion

        #region Public Methods

        public Color ToRGB()
        {
            Color retVal = Color.HSVToRGB(H, S, V, false);

            if (A.IsNearValue(1))
                return retVal;
            else
                return new Color(retVal.r, retVal.g, retVal.b, A);
        }

        public override string ToString()
        {
            return string.Format("H {1}{0}S {2}{0}V {3}{0}A {4}",
                "  |  ",
                A.ToStringSignificantDigits(2),
                H.ToStringSignificantDigits(2),
                S.ToStringSignificantDigits(2),
                V.ToStringSignificantDigits(2));
        }

        #endregion
    }
}
