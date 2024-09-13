using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalBaS
{
    //TODO: Base10 to Base12 string (and parse base12 string as decimal)

    public static class UtilityMath
    {
        /// <summary>
        /// This is good for converting a trackbar into a float
        /// </summary>
        /// <param name="minReturn">This is the value that will be returned when valueRange == minRange</param>
        /// <param name="maxReturn">This is the value that will be returned with valueRange == maxRange</param>
        /// <param name="minRange">The lowest value that valueRange can be</param>
        /// <param name="maxRange">The highest value that valueRange can be</param>
        /// <param name="valueRange">The trackbar's value</param>
        /// <returns>Somewhere between minReturn and maxReturn</returns>
        public static float GetScaledValue(float minReturn, float maxReturn, float minRange, float maxRange, float valueRange)
        {
            if (minRange.IsNearValue(maxRange))
            {
                return minReturn;
            }

            // Get the percent of value within the range
            float percent = (valueRange - minRange) / (maxRange - minRange);

            // Get the lerp between the return range
            return minReturn + (percent * (maxReturn - minReturn));
        }
        /// <summary>
        /// This overload ensures that the return doen't go beyond the min and max return
        /// </summary>
        public static float GetScaledValue_Capped(float minReturn, float maxReturn, float minRange, float maxRange, float valueRange)
        {
            float retVal = GetScaledValue(minReturn, maxReturn, minRange, maxRange, valueRange);

            // Cap the return value
            if (minReturn < maxReturn)
            {
                if (retVal < minReturn)
                {
                    retVal = minReturn;
                }
                else if (retVal > maxReturn)
                {
                    retVal = maxReturn;
                }
            }
            else if (minReturn > maxReturn)
            {
                if (retVal < maxReturn)
                {
                    retVal = maxReturn;
                }
                else if (retVal > minReturn)
                {
                    retVal = minReturn;
                }
            }
            else
            {
                retVal = minReturn;
            }

            return retVal;
        }

        /// <summary>
        /// This makes sure that min is less than max.  If they are passed in backward, they get swapped
        /// </summary>
        public static void MinMax(ref int min, ref int max)
        {
            if (max < min)
            {
                UtilityCore.Swap(ref min, ref max);
            }
        }
        public static void MinMax(ref float min, ref float max)
        {
            if (max < min)
            {
                UtilityCore.Swap(ref min, ref max);
            }
        }
        public static void MinMax(ref double min, ref double max)
        {
            if (max < min)
            {
                UtilityCore.Swap(ref min, ref max);
            }
        }
        public static void MinMax(ref decimal min, ref decimal max)
        {
            if (max < min)
            {
                UtilityCore.Swap(ref min, ref max);
            }
        }
        public static void MinMax(ref byte min, ref byte max)
        {
            if (max < min)
            {
                UtilityCore.Swap(ref min, ref max);
            }
        }
        public static void MinMax(ref string min, ref string max)
        {
            if (max.CompareTo(min) < 0)
            {
                UtilityCore.Swap(ref min, ref max);
            }
        }

        /// <summary>
        /// This returns the minimum and maximum value (throws exception if empty list)
        /// </summary>
        public static (float min, float max) MinMax(IEnumerable<float> values)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            bool hasEntry = false;      // don't want to use Count(), because that would iterate the whole list

            foreach (float value in values)
            {
                hasEntry = true;

                if (value < min)
                {
                    min = value;
                }

                if (value > max)
                {
                    max = value;
                }
            }

            if (!hasEntry)
            {
                throw new InvalidOperationException("Sequence contains no elements");       // this is the same error that .Max() gives
            }

            return (min, max);
        }

        /// <summary>
        /// Converts a base 10 number to base 2
        /// </summary>
        public static bool[] ConvertToBase2(long value, int vectorSize)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("This method can't handle negative numbers: " + value.ToString());
            }

            return Convert.ToString(value, 2).       // convert the base10 cntr to base2
                PadLeft(vectorSize, '0').     // force constant width
                Select(o => o == '1').      // convert to bool
                ToArray();
        }
        /// <summary>
        /// Converts a base 2 number to base 10
        /// </summary>
        public static long ConvertToBase10(bool[] bits)
        {
            string text = new string(bits.Select(o => o ? '1' : '0').ToArray());
            return Convert.ToInt64(text, 2);
        }

        public static int? GetCommonIndex((int, int) edge1, (int, int) edge2)
        {
            //This is a copy of Edge3D.GetCommonIndex

            // 1.1 : 2.1
            if (edge1.Item1 == edge2.Item1)
                return edge1.Item1;

            // 1.1 : 2.2
            if (edge1.Item1 == edge2.Item2)
                return edge1.Item1;

            // 1.2 : 2.1
            if (edge1.Item2 == edge2.Item1)
                return edge1.Item2;

            // 1.2 : 2.2
            if (edge1.Item2 == edge2.Item2)
                return edge1.Item2;

            return null;
        }
        public static int[] GetPolygon((int, int)[] edges)
        {
            // This is a copy of Edge2D.GetPolygon

            if (edges.Length < 2)
            {
                throw new ArgumentException($"Not enough segments to make a polygon: {edges.Length}");
            }

            List<int> retVal = new List<int>();
            int? commonIndex;

            for (int cntr = 0; cntr < edges.Length - 1; cntr++)
            {
                #region cntr, cntr+1

                // Add the point from cntr that isn't shared with cntr + 1
                commonIndex = GetCommonIndex(edges[cntr], edges[cntr + 1]);
                if (commonIndex == null)
                {
                    // While in this main loop, there can't be any breaks
                    throw new ApplicationException("Didn't find common point between edges");
                }
                else
                {
                    retVal.Add(commonIndex.Value);
                }

                #endregion
            }

            #region last edge

            if (edges.Length == 2)
            {
                // These edges define an open polygon - looks like a U

                // Take the item from the second edge that isn't in the list
                if (edges[1].Item1 == retVal[0])
                {
                    retVal.Add(edges[1].Item2);
                }
                else
                {
                    retVal.Add(edges[1].Item1);
                }

                // Now loop back, taking the item from the first edge that isn't in the list
                if (edges[0].Item1 == retVal[0])
                {
                    retVal.Add(edges[0].Item2);
                }
                else
                {
                    retVal.Add(edges[0].Item1);
                }
            }
            else
            {
                commonIndex = GetCommonIndex(edges[0], edges[edges.Length - 1]);
                if (commonIndex == null)
                {
                    throw new ApplicationException("Didn't find common point between edges");
                }

                retVal.Add(commonIndex.Value);
            }

            #endregion

            return retVal.ToArray();
        }

        private static string[] _suffix = { " ", "K", "M", "G", "T", "P", "E", "Z", "Y" };  // longs run out around EB -- yotta is bigger than zetta :)
        public static string GetSizeDisplay(long size, int decimalPlaces = 0, bool includeB = false)
        {
            //http://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net

            if (size == 0)
            {
                return "0 " + _suffix[0] + (includeB ? "B" : "");
            }

            long abs = Math.Abs(size);

            int place = Convert.ToInt32(Math.Floor(Math.Log(abs, 1024)));

            string numberText;
            if (decimalPlaces > 0)
            {
                double num = abs / Math.Pow(1024, place);
                numberText = (Math.Sign(size) * num).ToStringSignificantDigits(decimalPlaces);
            }
            else
            {
                double num = Math.Ceiling(abs / Math.Pow(1024, place));        //NOTE: windows uses ceiling, so doing the same (showing decimal places just clutters the view if looking at a list)
                numberText = (Math.Sign(size) * num).ToString("N0");
            }

            return numberText + " " + _suffix[place] + (includeB ? "B" : "");
        }

        /// <summary>
        /// This can be used to iterate over line segments of a polygon
        /// </summary>
        /// <remarks>
        /// If 4 is passed in, this will return:
        ///     0,1
        ///     1,2
        ///     2,3
        ///     3,0
        /// </remarks>
        public static IEnumerable<(int from, int to)> IterateEdges(int count)
        {
            for (int cntr = 0; cntr < count - 1; cntr++)
            {
                yield return (cntr, cntr + 1);
            }

            yield return (count - 1, 0);
        }

        /// <summary>
        /// This rotates the angle until it's between -360 and 360.  Then it clamps between min and max
        /// </summary>
        public static float ClampAngle(float angle, float min, float max)
        {
            while (true)
            {
                if (angle < -360f)
                    angle += 360f;
                else if (angle > 360f)
                    angle -= 360f;
                else break;
            }

            return Mathf.Clamp(angle, min, max);
        }
    }
}
