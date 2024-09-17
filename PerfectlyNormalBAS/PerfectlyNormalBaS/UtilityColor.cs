using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalBaS
{
    public static class UtilityColor
    {
        /// <summary>
        /// Takes RGB, RGBA, RRGGBB, RRGGBBAA.  # in front is optional
        /// </summary>
        public static Color FromHex(string hexRGBA)
        {
            string final = hexRGBA;

            if (!final.StartsWith("#"))
            {
                final = "#" + final;
            }

            if (final.Length == 5)     // compressed format, has alpha
            {
                // #RGBA -> #RRGGBBAA
                final = new string(new[] { '#', final[1], final[1], final[2], final[2], final[3], final[3], final[4], final[4] });
            }

            if (!ColorUtility.TryParseHtmlString(final, out Color retVal))
            {
                retVal = Color.magenta;
            }

            return retVal;
        }
        public static string ToHex(Color color, bool includeAlpha = true, bool includePound = true)
        {
            // I think color.ToString does the same thing, but this is explicit
            return string.Format("{0}{1}{2}{3}{4}",
                includePound ? "#" : "",
                includeAlpha ? ((int)(color.a * 255)).ToString("X2") : "",      //  throws an exception with float (must be int)
                ((int)(color.r * 255)).ToString("X2"),
                ((int)(color.g * 255)).ToString("X2"),
                ((int)(color.b * 255)).ToString("X2"));
        }

        /// <summary>
        /// This returns a color that is the result of the two colors blended
        /// </summary>
        /// <remarks>
        /// HSV is a little more expensive, but may give better colors - need to test
        /// </remarks>
        /// <param name="percent">0 is all back color, 1 is all fore color, .5 is half way between</param>
        public static Color LERP_RGB(Color foreColor, Color backColor, float percent)
        {
            // Figure out the new color
            float a, r, g, b;
            if (foreColor.a == 0)
            {
                // Fore is completely transparent, so only worry about blending the alpha
                a = backColor.a + ((foreColor.a - backColor.a) * percent);
                r = backColor.r;
                g = backColor.g;
                b = backColor.b;
            }
            else if (backColor.a == 0)
            {
                // Back is completely transparent, so only worry about blending the alpha
                a = backColor.a + ((foreColor.a - backColor.a) * percent);
                r = foreColor.r;
                g = foreColor.g;
                b = foreColor.b;
            }
            else
            {
                a = backColor.a + ((foreColor.a - backColor.a) * percent);
                r = backColor.r + ((foreColor.r - backColor.r) * percent);
                g = backColor.g + ((foreColor.g - backColor.g) * percent);
                b = backColor.b + ((foreColor.b - backColor.b) * percent);
            }

            return GetColorCapped(a, r, g, b);
        }
        public static Color LERP_HSV(Color foreColor, Color backColor, float percent)
        {
            // Figure out the new color
            float a, r, g, b;
            if (foreColor.a == 0)
            {
                // Fore is completely transparent, so only worry about blending the alpha
                a = backColor.a + ((foreColor.a - backColor.a) * percent);
                r = backColor.r;
                g = backColor.g;
                b = backColor.b;
            }
            else if (backColor.a == 0)
            {
                // Back is completely transparent, so only worry about blending the alpha
                a = backColor.a + ((foreColor.a - backColor.a) * percent);
                r = foreColor.r;
                g = foreColor.g;
                b = foreColor.b;
            }
            else
            {
                a = backColor.a + ((foreColor.a - backColor.a) * percent);

                ColorHSV backHSV = backColor.ToHSV();
                ColorHSV foreHSV = foreColor.ToHSV();

                float h = backHSV.H + ((foreHSV.H - backHSV.H) * percent);
                float s = backHSV.S + ((foreHSV.S - backHSV.S) * percent);
                float v = backHSV.V + ((foreHSV.V - backHSV.V) * percent);

                return new ColorHSV(h, s, v, a).ToRGB();
            }

            return GetColorCapped(a, r, g, b);
        }

        //NOTE: All values are 0 to 1
        public static Color RandomHSV()
        {
            return StaticRandom.ColorHSV();
        }
        public static Color RandomHSV(float hueMin, float hueMax)
        {
            return StaticRandom.ColorHSV(hueMin, hueMax);
        }
        public static Color RandomHSV(float hueMin, float hueMax, float saturationMin, float saturationMax, float valueMin, float valueMax)
        {
            return StaticRandom.ColorHSV(hueMin, hueMax, saturationMin, saturationMax, valueMin, valueMax);
        }
                            
        public static Color RandomHSVA(float alphaMin, float alphaMax)
        {
            return StaticRandom.ColorHSVA(alphaMin, alphaMax);
        }
        public static Color RandomHSVA(float hueMin, float hueMax, float alphaMin, float alphaMax)
        {
            return StaticRandom.ColorHSVA(hueMin, hueMax, alphaMin, alphaMax);
        }
        public static Color RandomHSVA(float hueMin, float hueMax, float saturationMin, float saturationMax, float valueMin, float valueMax, float alphaMin, float alphaMax)
        {
            return StaticRandom.ColorHSVA(hueMin, hueMax, saturationMin, saturationMax, valueMin, valueMax, alphaMin, alphaMax);
        }

        #region Private Methods

        private static Color GetColorCapped(float a, float r, float g, float b)
        {
            return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), Mathf.Clamp01(a));
        }

        #endregion
    }
}
