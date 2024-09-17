using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace PerfectlyNormalBaS
{
    public static class Extenders
    {
        #region string

        /// <summary>
        /// This capitalizes the first letter of each word
        /// </summary>
        /// <param name="convertToLowerFirst">
        /// True: The input string is converted to lowercase first.  I think this gives the most expected results
        /// False: Words in all caps look like they're left alone
        /// </param>
        public static string ToProper(this string text, bool convertToLowerFirst = true)
        {
            if (convertToLowerFirst)
            {
                return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
            }
            else
            {
                return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(text);
            }
        }

        public static string ToInvert(this string text)
        {
            char[] inverted = text.ToCharArray();

            for (int cntr = 0; cntr < inverted.Length; cntr++)
            {
                if (char.IsLetter(inverted[cntr]))
                {
                    if (char.IsUpper(inverted[cntr]))
                    {
                        inverted[cntr] = char.ToLower(inverted[cntr]);
                    }
                    else
                    {
                        inverted[cntr] = char.ToUpper(inverted[cntr]);
                    }
                }
            }

            return new string(inverted);
        }

        /// <summary>
        /// This is a string.Join, but written to look like a linq statement
        /// </summary>
        public static string ToJoin(this IEnumerable<string> strings, string separator)
        {
            return string.Join(separator, strings);
        }
        public static string ToJoin(this IEnumerable<string> strings, char separator)
        {
            return string.Join(separator.ToString(), strings);
        }

        public static bool In_ignorecase(this string value, params string[] compare)
        {
            if (compare == null)
            {
                return false;
            }
            else if (value == null)
            {
                return compare.Any(o => o == null);
            }

            return compare.Any(o => value.Equals(o, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region IEnumerable

        //public static IEnumerable<T> Descendants_DepthFirst<T>(this T head, Func<T, IEnumerable<T>> childrenFunc)
        //{
        //    yield return head;

        //    foreach (var node in childrenFunc(head))
        //    {
        //        foreach (var child in Descendants_DepthFirst(node, childrenFunc))
        //        {
        //            yield return child;
        //        }
        //    }
        //}
        //public static IEnumerable<T> Descendants_BreadthFirst<T>(this T head, Func<T, IEnumerable<T>> childrenFunc)
        //{
        //    yield return head;

        //    var last = head;
        //    foreach (var node in Descendants_BreadthFirst(head, childrenFunc))
        //    {
        //        foreach (var child in childrenFunc(node))
        //        {
        //            yield return child;
        //            last = child;
        //        }

        //        if (last.Equals(node)) yield break;
        //    }
        //}

        /// <summary>
        /// Lets you walk a tree as a 1D list (hard coded to depth first)
        /// (commented this because it shows up for all T.  uncomment if needed)
        /// </summary>
        /// <remarks>
        /// Got this here:
        /// http://www.claassen.net/geek/blog/2009/06/searching-tree-of-objects-with-linq.html
        /// 
        /// Ex (assuming Node has a property IEnumerable<Node> Children):
        ///     Node[] all = root.Descendants(o => o.Children).ToArray();
        ///     
        /// The original code has two: depth first, breadth first.  But for simplicity, I'm just using depth first.  Uncomment the
        /// more explicit methods if neeeded
        /// </remarks>
        //public static IEnumerable<T> Descendants<T>(this T head, Func<T, IEnumerable<T>> childrenFunc)
        //{
        //    yield return head;

        //    var children = childrenFunc(head);
        //    if (children != null)
        //    {
        //        foreach (var node in childrenFunc(head))
        //        {
        //            foreach (var child in Descendants(node, childrenFunc))
        //            {
        //                yield return child;
        //            }
        //        }
        //    }
        //}

        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            List<TSource> retVal = new List<TSource>();

            retVal.AddRangeUnique(source, keySelector);

            return retVal;
        }
        /// <summary>
        /// WARNING: this doesn't scale well.  Implement your own IEqualityComparer that has a good GetHashCode
        /// </summary>
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, Func<T, T, bool> comparer)
        {
            //List<T> retVal = new List<T>();
            //retVal.AddRangeUnique(source, comparer);
            //return retVal;

            return source.Distinct(new DelegateComparer<T>(comparer));
        }

        // Added these so the caller doesn't need pass a lambda
        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(o => o);
        }
        public static IOrderedEnumerable<T> OrderByDescending<T>(this IEnumerable<T> source)
        {
            return source.OrderByDescending(o => o);
        }

        /// <summary>
        /// This acts like the standard IndexOf, but with a custom comparer (good for floating point numbers, or objects that don't properly implement IEquatable)
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> source, T item, Func<T, T, bool> comparer)
        {
            int index = 0;

            foreach (T candidate in source)
            {
                if (comparer(candidate, item))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }
        public static int IndexOf<T>(this IEnumerable<T> source, T item)
        {
            return source.IndexOf(item, (t1, t2) => t1.Equals(t2));
        }

        public static bool Contains<T>(this IEnumerable<T> source, T item, Func<T, T, bool> comparer)
        {
            foreach (T candidate in source)
            {
                if (comparer(candidate, item))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<IGrouping<T, T>> GroupBy<T>(this IEnumerable<T> source)
        {
            return source.GroupBy(o => o);
        }
        /// <summary>
        /// This overload lets you pass the comparer as a delegate
        /// WARNING: these doesn't scale well.  Implement your own IEqualityComparer that has a good GetHashCode
        /// </summary>
        /// <remarks>
        /// ex (ignoring length checks):
        /// points.GroupBy((o,p) => Math3D.IsNearValue(o, p))
        /// </remarks>
        public static IEnumerable<IGrouping<T, T>> GroupBy<T>(this IEnumerable<T> source, Func<T, T, bool> comparer)
        {
            return source.GroupBy(o => o, new DelegateComparer<T>(comparer));
        }
        /// <summary>
        /// WARNING: this doesn't scale well.  Implement your own IEqualityComparer that has a good GetHashCode
        /// </summary>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TKey, bool> comparer)
        {
            return source.GroupBy(keySelector, new DelegateComparer<TKey>(comparer));
        }

        public static ILookup<T, T> ToLookup<T>(this IEnumerable<T> source)
        {
            return source.ToLookup(o => o);
        }
        /// <summary>
        /// WARNING: this doesn't scale well.  Implement your own IEqualityComparer that has a good GetHashCode
        /// </summary>
        public static ILookup<T, T> ToLookup<T>(this IEnumerable<T> source, Func<T, T, bool> comparer)
        {
            return source.ToLookup(o => o, new DelegateComparer<T>(comparer));
        }
        /// <summary>
        /// WARNING: this doesn't scale well.  Implement your own IEqualityComparer that has a good GetHashCode
        /// </summary>
        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TKey, bool> comparer)
        {
            return source.ToLookup(keySelector, new DelegateComparer<TKey>(comparer));
        }

        /// <summary>
        /// This is just like FirstOrDefault, but is meant to be used when you know that TSource is a value type.  This is hard
        /// coded to return nullable T
        /// </summary>
        /// <remarks>
        /// It's really annoying trying to use FirstOrDefault with value types, because they have to be converted to nullable first.
        /// Extra annoying if the value type is a tuple with named items:
        ///     (int index, double weight, SomeType obj)
        /// would need to be duplicated verbatim, but with a ? at the end
        ///     (int index, double weight, SomeType obj)?
        /// </remarks>
        public static TSource? FirstOrDefault_val<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) where TSource : struct
        {
            // Here is an alternative
            //return source.
            //    Cast<TSource?>().
            //    FirstOrDefault(o => predicate(o.Value));

            foreach (TSource item in source)
            {
                if (predicate(item))
                {
                    return item;
                }
            }

            return null;
        }
        public static TSource? FirstOrDefault_val<TSource>(this IEnumerable<TSource> source) where TSource : struct
        {
            foreach (TSource item in source)
            {
                return item;
            }

            return null;
        }

        #endregion

        #region IList

        //NOTE: AsEnumerable doesn't exist for IList, but when I put it here, it was ambiguous with other collections.  So this is spelled with an i
        public static IEnumerable<object> AsEnumerabIe(this IList list)
        {
            foreach (object item in list)
            {
                yield return item;
            }
        }
        public static IEnumerable<T> AsEnumerabIe<T>(this IList<T> list)
        {
            foreach (T item in list)
            {
                yield return item;
            }
        }

        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                list.Add(item);
            }
        }

        /// <summary>
        /// This only adds the items that aren't already in list
        /// This overload can be used when T has a comparable that makes sense
        /// </summary>
        public static void AddRangeUnique<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (T item in items.Distinct())
            {
                if (!list.Contains(item))
                {
                    list.Add(item);
                }
            }
        }
        /// <summary>
        /// This only adds the items that aren't already in list
        /// This overload takes a func that just returns a key that is comparable
        /// </summary>
        /// <remarks>
        /// Usage:
        /// 
        /// SomeList.AddRangeUnique(items, o => o.Prop);
        /// </remarks>
        public static void AddRangeUnique<TSource, TKey>(this IList<TSource> list, IEnumerable<TSource> items, Func<TSource, TKey> keySelector)
        {
            List<TKey> keys = new List<TKey>();

            foreach (TSource item in items)
            {
                TKey key = keySelector(item);

                bool foundIt = false;

                for (int cntr = 0; cntr < list.Count; cntr++)
                {
                    if (keys.Count <= cntr)
                    {
                        keys.Add(keySelector(list[cntr]));
                    }

                    if (keys[cntr].Equals(key))
                    {
                        foundIt = true;
                        break;
                    }
                }

                if (!foundIt)
                {
                    list.Add(item);
                    keys.Add(key);      // if execution got here, then keys is the same size as list
                }
            }
        }
        /// <summary>
        /// This only adds the items that aren't already in list
        /// This overload takes a custom comparer
        /// </summary>
        /// <remarks>
        /// Usage:
        /// 
        /// SomeList.AddRangeUnique(items, (o,p) => o.Prop1 == p.Prop1 && o.Prop2 == p.Prop2);
        /// </remarks>
        public static void AddRangeUnique<T>(this IList<T> list, IEnumerable<T> items, Func<T, T, bool> comparer)
        {
            foreach (T item in items)
            {
                bool foundIt = false;

                foreach (T listItem in list)
                {
                    if (comparer(item, listItem))
                    {
                        foundIt = true;
                        break;
                    }
                }

                if (!foundIt)
                {
                    list.Add(item);
                }
            }
        }

        /// <summary>
        /// NOTE: This removes from the list.  The returned items are what was removed
        /// </summary>
        public static IEnumerable<T> RemoveWhere<T>(this IList<T> list, Func<T, bool> constraint)
        {
            List<T> removed = new List<T>();

            int index = 0;

            while (index < list.Count)
            {
                if (constraint(list[index]))
                {
                    //yield return list[index];     //NOTE: can't use yield, because if there are no consumers of the returned results, the compiler wasn't even calling this method
                    removed.Add(list[index]);
                    list.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            return removed;
        }

        public static void RemoveAll<T>(this IList<T> list, IEnumerable<T> itemsToRemove)
        {
            if (itemsToRemove is IList<T> itemsList)
            {
                // The remove list already supports random access, so use it directly
                list.RemoveWhere(o => itemsList.Contains(o));
            }
            else
            {
                // Cache the values in case the list is the result of an expensive linq statement (it would get reevaluated for every
                // item in list)
                T[] array = itemsToRemove.ToArray();

                list.RemoveWhere(o => array.Contains(o));
            }
        }

        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }

        #endregion

        #region MatchCollection

        public static IEnumerable<Match> AsEnumerable(this MatchCollection matches)
        {
            foreach (Match match in matches)
            {
                yield return match;
            }
        }

        #endregion

        #region SortedList

        /// <summary>
        /// TryGetValue is really useful, but the out param can't be used in linq.  So this wraps that method to return a tuple instead
        /// </summary>
        public static (bool isSuccessful, TValue value) TryGetValue<TKey, TValue>(this SortedList<TKey, TValue> list, TKey key)
        {
            bool found = list.TryGetValue(key, out TValue value);

            return (found, value);
        }

        #endregion

        #region T

        public static bool In<T>(this T value, params T[] compare)
        {
            if (compare == null)
            {
                return false;
            }
            else if (value == null)
            {
                return compare.Any(o => o == null);
            }

            return compare.Any(o => value.Equals(o));
        }

        #endregion

        #region color

        public static ColorHSV ToHSV(this Color color)
        {
            return new ColorHSV(color);
        }

        #endregion

        #region system.random

        //NOTE: UnityEngine.Random isn't threadsafe, so use StaticRandom instead

        public static float NextFloat(this System.Random rand)
        {
            return (float)rand.NextDouble();
        }
        public static float NextFloat(this System.Random rand, float maxValue)
        {
            return (float)(rand.NextDouble() * maxValue);
        }
        public static float NextFloat(this System.Random rand, float minValue, float maxValue)
        {
            return (float)(minValue + (rand.NextDouble() * (maxValue - minValue)));
        }

        /// <summary>
        /// Returns between: (mid / 1+%) to (mid * 1+%)
        /// </summary>
        /// <param name="percent">0=0%, .1=10%</param>
        /// <param name="useRandomPercent">
        /// True=Percent is random from 0*percent to 1*percent
        /// False=Percent is always percent (the only randomness is whether to go up or down)
        /// </param>
        public static float NextPercent(this System.Random rand, float midPoint, float percent, bool useRandomPercent = true)
        {
            float actualPercent = 1f + (useRandomPercent ? percent * (float)rand.NextDouble() : percent);

            if (rand.Next(2) == 0)
            {
                // Add
                return midPoint * actualPercent;
            }
            else
            {
                // Remove
                return midPoint / actualPercent;
            }
        }
        /// <summary>
        /// Returns between: (mid - drift) to (mid + drift)
        /// </summary>
        /// <param name="useRandomDrift">
        /// True=Drift is random from 0*drift to 1*drift
        /// False=Drift is always drift (the only randomness is whether to go up or down)
        /// </param>
        public static float NextDrift(this System.Random rand, float midPoint, float drift, bool useRandomDrift = true)
        {
            float actualDrift = useRandomDrift ? drift * (float)rand.NextDouble() : drift;

            if (rand.Next(2) == 0)
            {
                // Add
                return midPoint + actualDrift;
            }
            else
            {
                // Remove
                return midPoint - actualDrift;
            }
        }

        /// <summary>
        /// This runs System.Random.NextDouble^power.  This skews the probability of what values get returned
        /// </summary>
        /// <remarks>
        /// The standard call to random gives an even chance of any number between 0 and 1.  This is not an even chance
        /// 
        /// If power is greater than 1, lower numbers are preferred
        /// If power is less than 1, larger numbers are preferred
        /// 
        /// My first attempt was to use a bell curve instead of power, but the result still gave a roughly even chance of values
        /// (except a spike right around 0).  Then it ocurred to me that the bell curve describes distribution.  If you want the output
        /// to follow that curve, use the integral of that equation - or something like that :)
        /// </remarks>
        /// <param name="power">
        /// .5 would be square root
        /// 2 would be squared
        /// </param>
        /// <param name="maxValue">The output is scaled by this value</param>
        /// <param name="isPlusMinus">
        /// True=output is -maxValue to maxValue
        /// False=output is 0 to maxValue
        /// </param>
        public static float NextPow(this System.Random rand, float power, float maxValue = 1f, bool isPlusMinus = false)
        {
            // Run the random method through power.  This will give a greater chance of returning zero than
            // one (or the opposite if power is less than one)
            //NOTE: This works, because random only returns between 0 and 1
            float random = (float)Math.Pow(rand.NextDouble(), power);

            float retVal = random * maxValue;

            if (!isPlusMinus)
            {
                // They only want positive
                return retVal;
            }

            // They want - to +
            if (rand.Next(2) == 0)
            {
                return retVal;
            }
            else
            {
                return -retVal;
            }
        }

        public static bool NextBool(this System.Random rand)
        {
            return rand.Next(2) == 0;
        }
        /// <summary>
        /// Returns a boolean, but allows a threshold for true
        /// </summary>
        /// <param name="rand"></param>
        /// <param name="chanceForTrue">0 would be no chance of true.  1 would be 100% chance of true</param>
        /// <returns></returns>
        public static bool NextBool(this System.Random rand, float chanceForTrue = .5f)
        {
            return rand.NextDouble() < chanceForTrue;
        }

        /// <summary>
        /// Returns a string of random upper case characters
        /// </summary>
        /// <remarks>
        /// TODO: Make NextSentence method that returns several "words" separated by spaces
        /// </remarks>
        public static string NextString(this System.Random rand, int length, string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            return new string
            (
                Enumerable.Range(0, length).
                    Select(o => chars[rand.Next(chars.Length)]).
                    ToArray()
            );
        }
        public static string NextString(this System.Random rand, int randLengthFrom, int randLengthTo, string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            return rand.NextString(rand.Next(randLengthFrom, randLengthTo), chars);
        }

        /// <summary>
        /// This chooses a random item from the list.  It doesn't save much typing
        /// </summary>
        public static T NextItem<T>(this System.Random rand, T[] items)
        {
            return items[rand.Next(items.Length)];
        }
        public static T NextItem<T>(this System.Random rand, IList<T> items)
        {
            return items[rand.Next(items.Count)];
        }

        public static Vector2 InsideUnitCircle(this System.Random rand)
        {
            return RangeCircle(rand, 0f, 1f);
        }
        public static Vector2 OnUnitCircle(this System.Random rand)
        {
            return CircleShell(rand, 1f);
        }
        public static Vector2 RangeCircle(this System.Random rand, float minRadius, float maxRadius)
        {
            // The sqrt idea came from here:
            // http://dzindzinovic.blogspot.com/2010/05/xna-random-point-in-circle.html

            float radius = minRadius + ((maxRadius - minRadius) * (float)Math.Sqrt(rand.NextDouble()));		// without the square root, there is more chance at the center than the edges

            return CircleShell(rand, radius);
        }
        public static Vector2 CircleShell(this System.Random rand, float radius)
        {
            double angle = rand.NextDouble() * Math.PI * 2d;

            float x = radius * (float)Math.Cos(angle);
            float y = radius * (float)Math.Sin(angle);

            return new Vector2(x, y);
        }

        public static Vector3 InsideUnitSphere(this System.Random rand)
        {
            return RangeSphere(rand, 0f, 1f);
        }
        public static Vector3 OnUnitSphere(this System.Random rand)
        {
            return SphereShell(rand, 1f);
        }
        public static Vector3 RangeSphere(this System.Random rand, float minRadius, float maxRadius)
        {
            // A sqrt, sin and cos  :(           can it be made cheaper?
            float radius = minRadius + ((maxRadius - minRadius) * (float)Math.Sqrt(rand.NextDouble()));		// without the square root, there is more chance at the center than the edges

            return SphereShell(rand, radius);
        }
        public static Vector3 SphereShell(this System.Random rand, float radius)
        {
            double theta = rand.NextDouble() * Math.PI * 2d;

            float phi = Math3D.GetPhiForRandom(rand.NextFloat(-1, 1));

            double sinPhi = Math.Sin(phi);

            double x = radius * Math.Cos(theta) * sinPhi;
            double y = radius * Math.Sin(theta) * sinPhi;
            double z = radius * Math.Cos(phi);

            return new Vector3((float)x, (float)y, (float)z);
        }

        public static Quaternion RotationUniform(this System.Random rand)
        {
            return Quaternion.AngleAxis(rand.NextFloat(0, 360), rand.OnUnitSphere());
        }

        //NOTE: All values are 0 to 1
        public static Color ColorHSV(this System.Random rand)
        {
            return ColorHSV(rand, 0, 1, 0, 1, 0, 1);
        }
        public static Color ColorHSV(this System.Random rand, float hueMin, float hueMax)
        {
            return ColorHSV(rand, hueMin, hueMax, 0, 1, 0, 1);
        }
        public static Color ColorHSV(this System.Random rand, float hueMin, float hueMax, float saturationMin, float saturationMax, float valueMin, float valueMax)
        {
            return Color.HSVToRGB
            (
                Mathf.Clamp(rand.NextFloat(hueMin, hueMax), 0, 1),
                Mathf.Clamp(rand.NextFloat(saturationMin, saturationMax), 0, 1),
                Mathf.Clamp(rand.NextFloat(valueMin, valueMax), 0, 1)
            );
        }

        public static Color ColorHSVA(this System.Random rand, float alphaMin, float alphaMax)
        {
            return ColorHSVA(rand, 0, 1, 0, 1, 0, 1, alphaMin, alphaMax);
        }
        public static Color ColorHSVA(this System.Random rand, float hueMin, float hueMax, float alphaMin, float alphaMax)
        {
            return ColorHSVA(rand, hueMin, hueMax, 0, 1, 0, 1, alphaMin, alphaMax);
        }
        public static Color ColorHSVA(this System.Random rand, float hueMin, float hueMax, float saturationMin, float saturationMax, float valueMin, float valueMax, float alphaMin, float alphaMax)
        {
            Color color = Color.HSVToRGB
            (
                Mathf.Clamp(rand.NextFloat(hueMin, hueMax), 0, 1),
                Mathf.Clamp(rand.NextFloat(saturationMin, saturationMax), 0, 1),
                Mathf.Clamp(rand.NextFloat(valueMin, valueMax), 0, 1)
            );

            return new Color(color.r, color.g, color.b, rand.NextFloat(alphaMin, alphaMax));
        }

        public static Color ColorHSV(this System.Random rand, string hex, float driftH = 0, float driftS = 0, float driftV = 0, float driftA = 0)
        {
            ColorHSV color = UtilityColor.FromHex(hex).ToHSV();

            Color retVal = Color.HSVToRGB
            (
                Mathf.Clamp(rand.NextDrift(color.H, driftH), 0, 1),
                Mathf.Clamp(rand.NextDrift(color.S, driftS), 0, 1),
                Mathf.Clamp(rand.NextDrift(color.V, driftV), 0, 1),
                false
            );

            if (color.A.IsNearValue(1f) && driftA.IsNearZero())
            {
                return retVal;
            }
            else
            {
                return new Color
                (
                    retVal.r,
                    retVal.g,
                    retVal.b,
                    Mathf.Clamp(rand.NextDrift(color.A, driftA), 0, 1)
                );
            }
        }

        #endregion

        #region int

        public static byte ToByte(this int value)
        {
            if (value < 0) value = 0;
            else if (value > 255) value = 255;

            return Convert.ToByte(value);
        }

        #endregion

        #region long

        public static byte ToByte(this long value)
        {
            if (value < 0) value = 0;
            else if (value > 255) value = 255;

            return Convert.ToByte(value);
        }

        #endregion

        #region float

        public static bool IsNearZero(this float item, float? threshold = null)
        {
            return Math.Abs(item) <= (threshold ?? Mathf.Epsilon);
        }

        public static bool IsNearValue(this float item, float compare, float? threshold = null)
        {
            float t = threshold ?? Mathf.Epsilon;
            return item >= compare - t && item <= compare + t;
        }

        public static bool IsInvalid(this float item)
        {
            return float.IsNaN(item) || float.IsInfinity(item);
        }

        public static int ToInt_Round(this float value)
        {
            return ToIntSafe(Math.Round(value));
        }
        public static int ToInt_Floor(this float value)
        {
            return ToIntSafe(Math.Floor(value));
        }
        public static int ToInt_Ceiling(this float value)
        {
            return ToIntSafe(Math.Ceiling(value));
        }

        public static byte ToByte_Round(this float value)
        {
            return ToByteSafe(Math.Round(value));
        }
        public static byte ToByte_Floor(this float value)
        {
            return ToByteSafe(Math.Floor(value));
        }
        public static byte ToByte_Ceiling(this float value)
        {
            return ToByteSafe(Math.Ceiling(value));
        }

        /// <summary>
        /// This is useful for displaying a value in a textbox when you don't know the range (could be
        /// 1000001 or .1000001 or 10000.5 etc)
        /// </summary>
        public static string ToStringSignificantDigits(this float value, int significantDigits, bool shouldRound = true)
        {
            if (shouldRound)
                value = (float)Math.Round(value, significantDigits);

            int numDecimals = GetNumDecimals(value);

            if (numDecimals < 0)
            {
                return ToStringSignificantDigits_PossibleScientific(value, significantDigits);
            }
            else
            {
                return ToStringSignificantDigits_Standard(value, significantDigits, true);
            }
        }

        #endregion

        #region double

        public static bool IsNearZero(this double item, double? threshold = null)
        {
            return Math.Abs(item) <= (threshold ?? Mathf.Epsilon);
        }

        public static bool IsNearValue(this double item, double compare, double? threshold = null)
        {
            double t = threshold ?? Mathf.Epsilon;
            return item >= compare - t && item <= compare + t;
        }

        public static bool IsInvalid(this double item)
        {
            return double.IsNaN(item) || double.IsInfinity(item);
        }

        public static int ToInt_Round(this double value)
        {
            return ToIntSafe(Math.Round(value));
        }
        public static int ToInt_Floor(this double value)
        {
            return ToIntSafe(Math.Floor(value));
        }
        public static int ToInt_Ceiling(this double value)
        {
            return ToIntSafe(Math.Ceiling(value));
        }

        public static byte ToByte_Round(this double value)
        {
            return ToByteSafe(Math.Round(value));
        }
        public static byte ToByte_Floor(this double value)
        {
            return ToByteSafe(Math.Floor(value));
        }
        public static byte ToByte_Ceiling(this double value)
        {
            return ToByteSafe(Math.Ceiling(value));
        }

        /// <summary>
        /// This is useful for displaying a double value in a textbox when you don't know the range (could be
        /// 1000001 or .1000001 or 10000.5 etc)
        /// </summary>
        public static string ToStringSignificantDigits(this double value, int significantDigits, bool shouldRound = true)
        {
            if (shouldRound)
                value = Math.Round(value, significantDigits);

            int numDecimals = GetNumDecimals(value);

            if (numDecimals < 0)
            {
                return ToStringSignificantDigits_PossibleScientific(value, significantDigits);
            }
            else
            {
                return ToStringSignificantDigits_Standard(value, significantDigits, true);
            }
        }

        #endregion

        #region decimal

        /// <summary>
        /// This is useful for displaying a double value in a textbox when you don't know the range (could be
        /// 1000001 or .1000001 or 10000.5 etc)
        /// </summary>
        public static string ToStringSignificantDigits(this decimal value, int significantDigits, bool shouldRound = true)
        {
            if (shouldRound)
                value = Math.Round(value, significantDigits);

            int numDecimals = GetNumDecimals(value);

            if (numDecimals < 0)
            {
                return ToStringSignificantDigits_PossibleScientific(value, significantDigits);
            }
            else
            {
                return ToStringSignificantDigits_Standard(value, significantDigits, true);
            }
        }

        #endregion

        #region Vector2

        public static bool IsZero(this Vector2 vector)
        {
            return vector.x == 0f &&
                vector.y == 0f;
        }

        public static bool IsNearZero(this Vector2 vector)
        {
            return vector.x.IsNearZero() &&
                vector.y.IsNearZero();
        }
        public static bool IsNearValue(this Vector2 vector, Vector2 compare)
        {
            return vector.x.IsNearValue(compare.x) &&
                vector.y.IsNearValue(compare.y);
        }

        public static bool IsInvalid(this Vector2 vector)
        {
            return vector.x.IsInvalid() ||
                vector.y.IsInvalid();
        }

        public static Vector3 ToVector3(this Vector2 vector)
        {
            return new Vector3(vector.x, vector.y, 0);
        }

        public static string ToStringSignificantDigits(this Vector2 vector, int significantDigits, bool shouldRound = true)
        {
            return string.Format("{0}, {1}",
                vector.x.ToStringSignificantDigits(significantDigits, shouldRound),
                vector.y.ToStringSignificantDigits(significantDigits, shouldRound));
        }

        #endregion

        #region Vector3

        public static bool IsZero(this Vector3 vector)
        {
            return vector.x == 0f &&
                vector.y == 0f &&
                vector.z == 0f;
        }

        public static bool IsNearZero(this Vector3 vector)
        {
            return vector.x.IsNearZero() &&
                vector.y.IsNearZero() &&
                vector.z.IsNearZero();
        }
        public static bool IsNearValue(this Vector3 vector, Vector3 compare)
        {
            return vector.x.IsNearValue(compare.x) &&
                vector.y.IsNearValue(compare.y) &&
                vector.z.IsNearValue(compare.z);
        }

        public static bool IsInvalid(this Vector3 vector)
        {
            return vector.x.IsInvalid() ||
                vector.y.IsInvalid() ||
                vector.z.IsInvalid();
        }

        public static Vector2 ToVector2(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.y);
        }

        public static string ToStringSignificantDigits(this Vector3 vector, int significantDigits, bool shouldRound = true)
        {
            return string.Format("{0}, {1}, {2}",
                vector.x.ToStringSignificantDigits(significantDigits, shouldRound),
                vector.y.ToStringSignificantDigits(significantDigits, shouldRound),
                vector.z.ToStringSignificantDigits(significantDigits, shouldRound));
        }

        /// <summary>
        /// Returns the portion of this vector that lies along the other vector
        /// NOTE: The return will be the same direction as alongVector, but the length from zero to this vector's full length
        /// </summary>
        /// <remarks>
        /// Lookup "vector projection" to see the difference between this and dot product
        /// http://en.wikipedia.org/wiki/Vector_projection
        /// </remarks>
        public static Vector3 GetProjectedVector(this Vector3 vector, Vector3 alongVector, bool eitherDirection = true)
        {
            // c = (a dot unit(b)) * unit(b)

            if (vector.IsNearZero() || alongVector.IsNearZero())
            {
                return new Vector3(0, 0, 0);
            }

            Vector3 alongVectorUnit = alongVector.normalized;

            float length = Vector3.Dot(vector, alongVectorUnit);

            if (!eitherDirection && length < 0)
            {
                // It's in the oppositie direction, and that isn't allowed
                return new Vector3(0, 0, 0);
            }

            return alongVectorUnit * length;
        }

        #endregion

        #region quaternion

        public static Vector3 ToWorld(this Quaternion quaternion, Vector3 directionLocal)
        {
            return quaternion * directionLocal;
        }
        public static Vector3 FromWorld(this Quaternion quaternion, Vector3 directionWorld)
        {
            return Quaternion.Inverse(quaternion) * directionWorld;
        }

        public static bool IsNearValue(this Quaternion quaternion, Quaternion compare)
        {
            return quaternion.x.IsNearValue(compare.x) &&
                quaternion.y.IsNearValue(compare.y) &&
                quaternion.z.IsNearValue(compare.z) &&
                quaternion.w.IsNearValue(compare.w);
        }

        #endregion

        #region ITriangle

        public static Plane ToPlane(this ITriangle triangle)
        {
            return new Plane(triangle.Point0, triangle.Point1, triangle.Point2);
        }

        #endregion

        #region Mesh

        public static IEnumerable<ITriangle> IterateTriangles(this Mesh mesh)
        {
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                yield return new Triangle
                (
                    mesh.vertices[mesh.triangles[i + 0]],
                    mesh.vertices[mesh.triangles[i + 1]],
                    mesh.vertices[mesh.triangles[i + 2]]
                );
            }
        }

        public static IEnumerable<(Vector3 point0, Vector3 point1, Vector3 point2)> IterateTrianglePoints(this Mesh mesh)
        {
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                yield return
                (
                    mesh.vertices[mesh.triangles[i + 0]],
                    mesh.vertices[mesh.triangles[i + 1]],
                    mesh.vertices[mesh.triangles[i + 2]]
                );

            }
        }

        public static IEnumerable<(int index0, int index1, int index2)> IterateTriangleIndices(this Mesh mesh)
        {
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                yield return
                (
                    mesh.triangles[i + 0],
                    mesh.triangles[i + 1],
                    mesh.triangles[i + 2]
                );

            }
        }

        public static int TriangleCount(this Mesh mesh)
        {
            if (mesh.triangles.Length % 3 != 0)
                throw new ApplicationException($"Expected triangle count to be divisible by 3: {mesh.triangles.Length}");

            return mesh.triangles.Length / 3;
        }

        #endregion

        #region Private Methods

        private static int ToIntSafe(double value)
        {
            double retVal = value;

            if (retVal < int.MinValue) retVal = int.MinValue;
            else if (retVal > int.MaxValue) retVal = int.MaxValue;
            else if (retVal.IsInvalid()) retVal = int.MaxValue;

            return Convert.ToInt32(retVal);
        }
        private static byte ToByteSafe(double value)
        {
            int retVal = ToIntSafe(Math.Ceiling(value));

            if (retVal < 0) retVal = 0;
            else if (retVal > 255) retVal = 255;
            else retVal = 255;

            return Convert.ToByte(retVal);
        }

        private static int GetNumDecimals(float value)
        {
            return GetNumDecimals_ToString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));      // I think this forces decimal to always be a '.' ?
        }
        private static int GetNumDecimals(double value)
        {
            return GetNumDecimals_ToString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));      // I think this forces decimal to always be a '.' ?
        }
        private static int GetNumDecimals(decimal value)
        {
            return GetNumDecimals_ToString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        private static int GetNumDecimals_ToString(string text)
        {
            if (Regex.IsMatch(text, "[a-z]", RegexOptions.IgnoreCase))
            {
                // This is in exponential notation, just give up (or maybe NaN)
                return -1;
            }

            int decimalIndex = text.IndexOf(".");

            if (decimalIndex < 0)
            {
                // It's an integer
                return 0;
            }
            else
            {
                // Just count the decimals
                return (text.Length - 1) - decimalIndex;
            }
        }

        private static string ToStringSignificantDigits_PossibleScientific(float value, int significantDigits)
        {
            return ToStringSignificantDigits_PossibleScientific_ToString(
                value.ToString(System.Globalization.CultureInfo.InvariantCulture),      // I think this forces decimal to always be a '.' ?
                value.ToString(),
                significantDigits);
        }
        private static string ToStringSignificantDigits_PossibleScientific(double value, int significantDigits)
        {
            return ToStringSignificantDigits_PossibleScientific_ToString(
                value.ToString(System.Globalization.CultureInfo.InvariantCulture),      // I think this forces decimal to always be a '.' ?
                value.ToString(),
                significantDigits);
        }
        private static string ToStringSignificantDigits_PossibleScientific(decimal value, int significantDigits)
        {
            return ToStringSignificantDigits_PossibleScientific_ToString(
                value.ToString(System.Globalization.CultureInfo.InvariantCulture),      // I think this forces decimal to always be a '.' ?
                value.ToString(),
                significantDigits);
        }
        private static string ToStringSignificantDigits_PossibleScientific_ToString(string textInvariant, string text, int significantDigits)
        {
            Match match = Regex.Match(textInvariant, @"^(?<num>(-|)\d\.\d+)(?<exp>E(-|)\d+)$");
            if (!match.Success)
            {
                // Unknown
                return text;
            }

            string standard = ToStringSignificantDigits_Standard(Convert.ToDouble(match.Groups["num"].Value), significantDigits, false);

            return standard + match.Groups["exp"].Value;
        }

        private static string ToStringSignificantDigits_Standard(float value, int significantDigits, bool useN)
        {
            return ToStringSignificantDigits_Standard(Convert.ToDecimal(value), significantDigits, useN);
        }
        private static string ToStringSignificantDigits_Standard(double value, int significantDigits, bool useN)
        {
            return ToStringSignificantDigits_Standard(Convert.ToDecimal(value), significantDigits, useN);
        }
        private static string ToStringSignificantDigits_Standard(decimal value, int significantDigits, bool useN)
        {
            // Get the integer portion
            //long intPortion = Convert.ToInt64(Math.Truncate(value));		// going directly against the value for this (min could go from 1 to 1000.  1 needs two decimal places, 10 needs one, 100+ needs zero)
            var intPortion = new System.Numerics.BigInteger(Math.Truncate(value));       // ran into a case that didn't fit in a long
            int numInt;
            if (intPortion == 0)
            {
                numInt = 0;
            }
            else
            {
                numInt = intPortion.ToString().Length;
            }

            // Limit the number of significant digits
            int numPlaces;
            if (numInt == 0)
            {
                numPlaces = significantDigits;
            }
            else if (numInt >= significantDigits)
            {
                numPlaces = 0;
            }
            else
            {
                numPlaces = significantDigits - numInt;
            }

            // I was getting an exception from round, but couldn't recreate it, so I'm just throwing this in to avoid the exception
            if (numPlaces < 0)
            {
                numPlaces = 0;
            }
            else if (numPlaces > 15)
            {
                numPlaces = 15;
            }

            // Show a rounded number
            decimal rounded = Math.Round(value, numPlaces);
            int numActualDecimals = GetNumDecimals(rounded);
            if (numActualDecimals < 0 || !useN)
            {
                return rounded.ToString();		// it's weird, don't try to make it more readable
            }
            else
            {
                return rounded.ToString("N" + numActualDecimals);
            }
        }

        #endregion
    }
}
