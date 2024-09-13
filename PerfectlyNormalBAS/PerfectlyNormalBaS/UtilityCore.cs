using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PerfectlyNormalBaS
{
    public static class UtilityCore
    {
        #region misc

        /// <summary>
        /// After this method: 1 = 2, 2 = 1
        /// </summary>
        public static void Swap<T>(ref T item1, ref T item2)
        {
            T temp = item1;
            item1 = item2;
            item2 = temp;
        }

        public static bool IsWhitespace(char text)
        {
            //http://stackoverflow.com/questions/18169006/all-the-whitespace-characters-is-it-language-independent

            // Here are some more chars that could be space
            //http://www.fileformat.info/info/unicode/category/Zs/list.htm

            switch (text)
            {
                case '\0':
                case '\t':
                case '\r':
                case '\v':
                case '\f':
                case '\n':
                case ' ':
                case '\u00A0':      // NO-BREAK SPACE
                case '\u1680':      // OGHAM SPACE MARK
                case '\u2000':      // EN QUAD
                case '\u2001':      // EM QUAD
                case '\u2002':      // EN SPACE
                case '\u2003':      // EM SPACE
                case '\u2004':      // THREE-PER-EM SPACE
                case '\u2005':      // FOUR-PER-EM SPACE
                case '\u2006':      // SIX-PER-EM SPACE
                case '\u2007':      // FIGURE SPACE
                case '\u2008':      // PUNCTUATION SPACE
                case '\u2009':      // THIN SPACE
                case '\u200A':      // HAIR SPACE
                case '\u202F':      // NARROW NO-BREAK SPACE
                case '\u205F':      // MEDIUM MATHEMATICAL SPACE
                case '\u3000':      // IDEOGRAPHIC SPACE
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// This compares two arrays.  If they are the same size, and each element equals, then this returns true
        /// </summary>
        public static bool IsArrayEqual<T>(T[] arr1, T[] arr2)
        {
            if (arr1 == null && arr2 == null)
            {
                return true;
            }
            else if (arr1 == null || arr2 == null)
            {
                return false;
            }
            else if (arr1.Length != arr2.Length)
            {
                return false;
            }

            for (int cntr = 0; cntr < arr1.Length; cntr++)
            {
                if (!arr1[cntr].Equals(arr2[cntr]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// This will replace invalid chars with underscores, there are also some reserved words that it adds underscore to
        /// </summary>
        /// <remarks>
        /// https://stackoverflow.com/questions/1976007/what-characters-are-forbidden-in-windows-and-linux-directory-names
        /// </remarks>
        /// <param name="containsFolder">Pass in true if filename represents a folder\file (passing true will allow slash)</param>
        public static string EscapeFilename_Windows(string filename, bool containsFolder = false)
        {
            StringBuilder builder = new StringBuilder(filename.Length + 12);

            int index = 0;

            // Allow colon if it's part of the drive letter
            if (containsFolder)
            {
                Match match = Regex.Match(filename, @"^\s*[A-Z]:\\", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    builder.Append(match.Value);
                    index = match.Length;
                }
            }

            // Character substitutions
            for (int cntr = index; cntr < filename.Length; cntr++)
            {
                char c = filename[cntr];

                switch (c)
                {
                    case '\u0000':
                    case '\u0001':
                    case '\u0002':
                    case '\u0003':
                    case '\u0004':
                    case '\u0005':
                    case '\u0006':
                    case '\u0007':
                    case '\u0008':
                    case '\u0009':
                    case '\u000A':
                    case '\u000B':
                    case '\u000C':
                    case '\u000D':
                    case '\u000E':
                    case '\u000F':
                    case '\u0010':
                    case '\u0011':
                    case '\u0012':
                    case '\u0013':
                    case '\u0014':
                    case '\u0015':
                    case '\u0016':
                    case '\u0017':
                    case '\u0018':
                    case '\u0019':
                    case '\u001A':
                    case '\u001B':
                    case '\u001C':
                    case '\u001D':
                    case '\u001E':
                    case '\u001F':

                    case '<':
                    case '>':
                    case ':':
                    case '"':
                    case '/':
                    case '|':
                    case '?':
                    case '*':
                        builder.Append('_');
                        break;

                    case '\\':
                        builder.Append(containsFolder ? c : '_');
                        break;

                    default:
                        builder.Append(c);
                        break;
                }
            }

            string built = builder.ToString();

            if (built == "")
            {
                return "_";
            }

            if (built.EndsWith(" ") || built.EndsWith("."))
            {
                built = built.Substring(0, built.Length - 1) + "_";
            }

            // These are reserved names, in either the folder or file name, but they are fine if following a dot
            // CON, PRN, AUX, NUL, COM0 .. COM9, LPT0 .. LPT9
            builder = new StringBuilder(built.Length + 12);
            index = 0;
            foreach (Match match in Regex.Matches(built, @"(^|\\)\s*(?<bad>CON|PRN|AUX|NUL|COM\d|LPT\d)\s*(\.|\\|$)", RegexOptions.IgnoreCase))
            {
                Group group = match.Groups["bad"];
                if (group.Index > index)
                {
                    builder.Append(built.Substring(index, match.Index - index + 1));
                }

                builder.Append(group.Value);
                builder.Append("_");        // putting an underscore after this keyword is enough to make it acceptable

                index = group.Index + group.Length;
            }

            if (index == 0)
            {
                return built;
            }

            if (index < built.Length - 1)
            {
                builder.Append(built.Substring(index));
            }

            return builder.ToString();
        }

        #endregion

        #region enums

        public static T GetRandomEnum<T>(T excluding) where T : struct
        {
            return GetRandomEnum<T>(new T[] { excluding });
        }
        public static T GetRandomEnum<T>(IEnumerable<T> excluding) where T : struct
        {
            while (true)
            {
                T retVal = GetRandomEnum<T>();
                if (!excluding.Contains(retVal))
                {
                    return retVal;
                }
            }
        }
        public static T GetRandomEnum<T>() where T : struct
        {
            Array allValues = Enum.GetValues(typeof(T));
            if (allValues.Length == 0)
            {
                throw new ArgumentException("This enum has no values");
            }

            return (T)allValues.GetValue(StaticRandom.Next(allValues.Length));
        }

        /// <summary>
        /// This is just a wrapper to Enum.GetValues.  Makes the caller's code a bit less ugly
        /// </summary>
        public static T[] GetEnums<T>() where T : struct
        {
            return (T[])Enum.GetValues(typeof(T));
        }
        public static T[] GetEnums<T>(T excluding) where T : struct
        {
            return GetEnums<T>(new T[] { excluding });
        }
        public static T[] GetEnums<T>(IEnumerable<T> excluding) where T : struct
        {
            T[] all = (T[])Enum.GetValues(typeof(T));

            return all.
                Where(o => !excluding.Contains(o)).
                ToArray();
        }

        /// <summary>
        /// This is a strongly typed wrapper to Enum.Parse
        /// </summary>
        public static T EnumParse<T>(string text, bool ignoreCase = true) where T : struct // can't constrain to enum
        {
            return (T)Enum.Parse(typeof(T), text, ignoreCase);
        }

        #endregion

        #region lists

        /// <summary>
        /// This acts like Enumerable.Range, but the values returned are in a random order
        /// </summary>
        public static IEnumerable<int> RandomRange(int start, int count)
        {
            // Prepare a list of indices (these represent what's left to return)
            //int[] indices = Enumerable.Range(start, count).ToArray();		// this is a smaller amount of code, but slower
            int[] indices = new int[count];
            for (int cntr = 0; cntr < count; cntr++)
            {
                indices[cntr] = start + cntr;
            }

            Random rand = StaticRandom.GetRandomForThread();

            for (int cntr = count - 1; cntr >= 0; cntr--)
            {
                // Come up with a random value that hasn't been returned yet
                int index1 = rand.Next(cntr + 1);
                int index2 = indices[index1];
                indices[index1] = indices[cntr];

                yield return index2;
            }
        }
        /// <summary>
        /// This overload wont iterate over all the values, just some of them
        /// </summary>
        /// <param name="rangeCount">When returning a subset of a big list, rangeCount is the size of the big list</param>
        /// <param name="iterateCount">When returning a subset of a big list, iterateCount is the size of the subset</param>
        /// <remarks>
        /// Example:
        ///		start=0, rangeCount=10, iterateCount=3
        ///		This will return 3 values, but their range is from 0 to 10 (and it will never return dupes)
        /// </remarks>
        public static IEnumerable<int> RandomRange(int start, int rangeCount, int iterateCount)
        {
            if (iterateCount > rangeCount)
            {
                //throw new ArgumentOutOfRangeException(string.Format("iterateCount can't be greater than rangeCount.  iterateCount={0}, rangeCount={1}", iterateCount.ToString(), rangeCount.ToString()));
                iterateCount = rangeCount;
            }

            if (iterateCount < rangeCount / 3)
            {
                #region While Loop

                Random rand = StaticRandom.GetRandomForThread();

                // Rather than going through the overhead of building an array of all values up front, just remember what's been returned
                List<int> used = new List<int>();
                int maxValue = start + rangeCount;

                for (int cntr = 0; cntr < iterateCount; cntr++)
                {
                    // Find a value that hasn't been returned yet
                    int retVal = 0;
                    while (true)
                    {
                        retVal = rand.Next(start, maxValue);

                        if (!used.Contains(retVal))
                        {
                            used.Add(retVal);
                            break;
                        }
                    }

                    // Return this
                    yield return retVal;
                }

                #endregion
            }
            else if (iterateCount > 0)
            {
                #region Maintain Array

                // Reuse the other overload, just stop prematurely

                int cntr = 0;
                foreach (int retVal in RandomRange(start, rangeCount))
                {
                    yield return retVal;

                    cntr++;
                    if (cntr == iterateCount)
                    {
                        break;
                    }
                }

                #endregion
            }
        }
        /// <summary>
        /// This overload lets the user pass in their own random function -- ex: rand.NextPow(2)
        /// </summary>
        /// <param name="rand">
        /// int1 = min value
        /// int2 = max value (up to, but not including max value)
        /// return = a random index
        /// </param>
        public static IEnumerable<int> RandomRange(int start, int rangeCount, int iterateCount, Func<int, int, int> rand)
        {
            if (iterateCount > rangeCount)
            {
                //throw new ArgumentOutOfRangeException(string.Format("iterateCount can't be greater than rangeCount.  iterateCount={0}, rangeCount={1}", iterateCount.ToString(), rangeCount.ToString()));
                iterateCount = rangeCount;
            }

            if (iterateCount < rangeCount * .15)
            {
                #region While Loop

                // Rather than going through the overhead of building an array of all values up front, just remember what's been returned
                List<int> used = new List<int>();
                int maxValue = start + rangeCount;

                for (int cntr = 0; cntr < iterateCount; cntr++)
                {
                    // Find a value that hasn't been returned yet
                    int retVal = 0;
                    while (true)
                    {
                        retVal = rand(start, maxValue);

                        if (!used.Contains(retVal))
                        {
                            used.Add(retVal);
                            break;
                        }
                    }

                    // Return this
                    yield return retVal;
                }

                #endregion
            }
            else if (iterateCount > 0)
            {
                #region Destroy list

                // Since the random passed in is custom, there is a good chance that the list the indices will be used for is sorted.
                // So create a list of candidate indices, and whittle that down

                List<int> available = new List<int>(Enumerable.Range(start, rangeCount));

                for (int cntr = 0; cntr < iterateCount; cntr++)
                {
                    int index = rand(0, available.Count);

                    yield return available[index];
                    available.RemoveAt(index);
                }

                #endregion
            }
        }

        /// <summary>
        /// This enumerates the array in a random order
        /// </summary>
        public static IEnumerable<T> RandomOrder<T>(T[] array, int? max = null)
        {
            int actualMax = max ?? array.Length;
            if (actualMax > array.Length)
            {
                actualMax = array.Length;
            }

            foreach (int index in RandomRange(0, array.Length, actualMax))
            {
                yield return array[index];
            }
        }
        /// <summary>
        /// This enumerates the list in a random order
        /// </summary>
        public static IEnumerable<T> RandomOrder<T>(IList<T> list, int? max = null)
        {
            int actualMax = max ?? list.Count;
            if (actualMax > list.Count)
            {
                actualMax = list.Count;
            }

            foreach (int index in RandomRange(0, list.Count, actualMax))
            {
                yield return list[index];
            }
        }

        /// <summary>
        /// I had a case where I had several arrays that may or may not be null, and wanted to iterate over all of the non null ones
        /// Usage: foreach(T item in Iterate(array1, array2, array3))
        /// </summary>
        /// <remarks>
        /// I just read about a method called Concat, which seems to be very similar to this Iterate (but iterate can handle null inputs)
        /// </remarks>
        public static IEnumerable<T> Iterate<T>(IEnumerable<T> list1 = null, IEnumerable<T> list2 = null, IEnumerable<T> list3 = null, IEnumerable<T> list4 = null, IEnumerable<T> list5 = null, IEnumerable<T> list6 = null, IEnumerable<T> list7 = null, IEnumerable<T> list8 = null)
        {
            if (list1 != null)
            {
                foreach (T item in list1)
                {
                    yield return item;
                }
            }

            if (list2 != null)
            {
                foreach (T item in list2)
                {
                    yield return item;
                }
            }

            if (list3 != null)
            {
                foreach (T item in list3)
                {
                    yield return item;
                }
            }

            if (list4 != null)
            {
                foreach (T item in list4)
                {
                    yield return item;
                }
            }

            if (list5 != null)
            {
                foreach (T item in list5)
                {
                    yield return item;
                }
            }

            if (list6 != null)
            {
                foreach (T item in list6)
                {
                    yield return item;
                }
            }

            if (list7 != null)
            {
                foreach (T item in list7)
                {
                    yield return item;
                }
            }

            if (list8 != null)
            {
                foreach (T item in list8)
                {
                    yield return item;
                }
            }
        }
        /// <summary>
        /// This lets T's and IEnumerable(T)'s be intermixed
        /// </summary>
        public static IEnumerable<T> Iterate<T>(params object[] items)
        {
            foreach (object item in items)
            {
                if (item == null)
                {
                    continue;
                }
                else if (item is T)
                {
                    yield return (T)item;
                }
                else if (item is IEnumerable<T>)
                {
                    foreach (T child in (IEnumerable<T>)item)
                    {
                        //NOTE: child could be null.  I originally had if(!null), but that is inconsistent with how the other overload is written
                        yield return (T)child;
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format("Unexpected type ({0}).  Should have been singular or enumerable ({1})", item.GetType().ToString(), typeof(T).ToString()));
                }
            }
        }

        /// <summary>
        /// This returns all combinations of the lists passed in.  This is a nested loop, which makes it easier to
        /// write linq statements against
        /// </summary>
        public static IEnumerable<(T1, T2)> Collate<T1, T2>(IEnumerable<T1> t1s, IEnumerable<T2> t2s)
        {
            T2[] t2Arr = t2s.ToArray();

            foreach (T1 t1 in t1s)
            {
                foreach (T2 t2 in t2Arr)
                {
                    yield return (t1, t2);
                }
            }
        }
        public static IEnumerable<(T1, T2, T3)> Collate<T1, T2, T3>(IEnumerable<T1> t1s, IEnumerable<T2> t2s, IEnumerable<T3> t3s)
        {
            T2[] t2Arr = t2s.ToArray();
            T3[] t3Arr = t3s.ToArray();

            foreach (T1 t1 in t1s)
            {
                foreach (T2 t2 in t2Arr)
                {
                    foreach (T3 t3 in t3Arr)
                    {
                        yield return (t1, t2, t3);
                    }
                }
            }
        }
        public static IEnumerable<(T1, T2, T3, T4)> Collate<T1, T2, T3, T4>(IEnumerable<T1> t1s, IEnumerable<T2> t2s, IEnumerable<T3> t3s, IEnumerable<T4> t4s)
        {
            T2[] t2Arr = t2s.ToArray();
            T3[] t3Arr = t3s.ToArray();
            T4[] t4Arr = t4s.ToArray();

            foreach (T1 t1 in t1s)
            {
                foreach (T2 t2 in t2Arr)
                {
                    foreach (T3 t3 in t3Arr)
                    {
                        foreach (T4 t4 in t4Arr)
                        {
                            yield return (t1, t2, t3, t4);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This iterates over all possible pairs of the items
        /// </summary>
        /// <remarks>
        /// if you pass in:
        /// { A, B, C, D, E}
        /// 
        /// you get:
        /// { A, B }, { A, C}, { A, D }, { A, E }, { B, C }, { B, D }, { B, E }, { C, D }, { C, E }, { D, E }
        /// </remarks>
        public static IEnumerable<(T, T)> GetPairs<T>(T[] items)
        {
            for (int outer = 0; outer < items.Length - 1; outer++)
            {
                for (int inner = outer + 1; inner < items.Length; inner++)
                {
                    yield return (items[outer], items[inner]);
                }
            }
        }
        public static IEnumerable<(T, T)> GetPairs<T>(IList<T> items)
        {
            for (int outer = 0; outer < items.Count - 1; outer++)
            {
                for (int inner = outer + 1; inner < items.Count; inner++)
                {
                    yield return (items[outer], items[inner]);
                }
            }
        }
        public static IEnumerable<(int, int)> GetPairs(int count)
        {
            for (int outer = 0; outer < count - 1; outer++)
            {
                for (int inner = outer + 1; inner < count; inner++)
                {
                    yield return (outer, inner);
                }
            }
        }

        /// <summary>
        /// This is like calling RandomRange() on GetPairs().  But is optimized to not build all intermediate pairs
        /// </summary>
        /// <param name="itemCount">This would be the count passed into GetPairs</param>
        /// <param name="returnCount">This is how many random samples to take</param>
        public static IEnumerable<(int, int)> GetRandomPairs(int itemCount, int returnCount)
        {
            int linkCount = ((itemCount * itemCount) - itemCount) / 2;

            return RandomRange(0, linkCount, returnCount).
                Select(o => GetPair(o, itemCount));
        }

        /// <summary>
        /// WARNING: Only use this overload if the type is comparable with .Equals - (like int)
        /// </summary>
        public static (T[] links, bool isClosed)[] GetChains<T>(IEnumerable<(T, T)> segments)
        {
            return GetChains(segments, (o, p) => o.Equals(p));
        }
        /// <summary>
        /// This converts the set of segments into chains (good for making polygons out of line segments)
        /// WARNING: This method fails if the segments form a spoke wheel (more than 2 segments share a point)
        /// </summary>
        /// <returns>
        /// Item1=A chain or loop of items
        /// Item2=True: Loop, False: Chain
        /// </returns>
        public static (T[] links, bool isClosed)[] GetChains<T>(IEnumerable<(T, T)> segments, Func<T, T, bool> compare)
        {
            // Convert the segments into chains
            List<T[]> chains = segments.
                Select(o => new[] { o.Item1, o.Item2 }).
                ToList();

            // Keep trying to merge the chains until no more merges are possible
            while (true)
            {
                #region Merge pass

                if (chains.Count == 1) break;

                bool hadJoin = false;

                for (int outer = 0; outer < chains.Count - 1; outer++)
                {
                    for (int inner = outer + 1; inner < chains.Count; inner++)
                    {
                        // See if these two can be merged
                        T[] newChain = TryJoinChains(chains[outer], chains[inner], compare);

                        if (newChain != null)
                        {
                            // Swap the sub chains with the new combined one
                            chains.RemoveAt(inner);
                            chains.RemoveAt(outer);

                            chains.Add(newChain);

                            hadJoin = true;
                            break;
                        }
                    }

                    if (hadJoin) break;
                }

                if (!hadJoin) break;        // compared all the mini chains, and there were no merges.  Quit looking

                #endregion
            }

            #region Detect loops

            var retVal = new List<(T[], bool)>();

            foreach (T[] chain in chains)
            {
                if (compare(chain[0], chain[chain.Length - 1]))
                {
                    T[] loop = chain.Skip(1).ToArray();
                    retVal.Add((loop, true));
                }
                else
                {
                    retVal.Add((chain, false));
                }
            }

            #endregion

            return retVal.ToArray();
        }

        /// <summary>
        /// If there are 10 items, and the percent is .43, then the 4th item will be returned (index=3)
        /// </summary>
        /// <param name="percent">A percent (from 0 to 1)</param>
        /// <param name="count">The number of items in the list</param>
        /// <returns>The index into the list</returns>
        public static int GetIndexIntoList(float percent, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException("Count must be greater than zero");
            }

            int retVal = Convert.ToInt32(Math.Floor(count * percent));
            if (retVal < 0) retVal = 0;
            if (retVal >= count) retVal = count - 1;

            return retVal;
        }
        /// <summary>
        /// This walks fractionsOfWhole, and returns the index that percent lands on
        /// NOTE: fractionsOfWhole should be sorted descending
        /// WARNING: fractionsOfWhole.Sum(o => o.Item2) must be one.  If it's less, this method will sometimes return -1.  If it's more, the items over one will never be chosen
        /// </summary>
        /// <param name="percent">The percent to seek</param>
        /// <param name="fractionsOfWhole">
        /// Item1=Index into original list (this isn't used by this method, but will be a link to the item represented by this item)
        /// Item2=Percent of whole that this item represents (the sum of the percents should add up to 1)
        /// </param>
        public static int GetIndexIntoList(float percent, (int index, float percent)[] fractionsOfWhole)
        {
            float used = 0;

            for (int cntr = 0; cntr < fractionsOfWhole.Length; cntr++)
            {
                if (percent >= used && percent <= used + fractionsOfWhole[cntr].percent)
                {
                    return cntr;
                }

                used += fractionsOfWhole[cntr].percent;
            }

            return -1;
        }

        /// <summary>
        /// This tells where to insert to keep it sorted
        /// </summary>
        public static int GetInsertIndex<T>(IEnumerable<T> items, T newItem) where T : IComparable<T>
        {
            int index = 0;

            foreach (T existing in items)
            {
                if (existing.CompareTo(newItem) > 0)
                {
                    return index;
                }

                index++;
            }

            return index;
        }

        /// <summary>
        /// This creates a new array with the item added to the end
        /// </summary>
        public static T[] ArrayAdd<T>(T[] array, T item)
        {
            if (array == null)
            {
                return new T[] { item };
            }

            T[] retVal = new T[array.Length + 1];

            Array.Copy(array, retVal, array.Length);
            retVal[retVal.Length - 1] = item;

            return retVal;
        }
        /// <summary>
        /// This creates a new array with the items added to the end
        /// </summary>
        public static T[] ArrayAdd<T>(T[] array, T[] items)
        {
            if (array == null)
            {
                return items.ToArray();
            }
            else if (items == null)
            {
                return array.ToArray();
            }

            T[] retVal = new T[array.Length + items.Length];

            Array.Copy(array, retVal, array.Length);
            Array.Copy(items, 0, retVal, array.Length, items.Length);

            return retVal;
        }

        /// <summary>
        /// Returns true if both lists share the same item
        /// </summary>
        /// <remarks>
        /// Example of True:
        ///     { 1, 2, 3, 4 }
        ///     { 5, 6, 7, 2 }
        /// 
        /// Example of False:
        ///     { 1, 2, 3, 4 }
        ///     { 5, 6, 7, 8 }
        /// </remarks>
        public static bool SharesItem<T>(IEnumerable<T> list1, IEnumerable<T> list2)
        {
            foreach (T item1 in list1)
            {
                if (list2.Any(item2 => item2.Equals(item1)))
                {
                    return true;
                }
            }

            return false;
        }

        public static T[][] ConvertJaggedArray<T>(object[][] jagged)
        {
            return jagged.
                Select(o => o.Select(p => (T)p).ToArray()).
                ToArray();
        }

        /// <summary>
        /// This returns a map between the old index and new index after some items are removed
        /// </summary>
        /// <remarks>
        /// ex: count=6, remove={ 2, 4, 4, 0 }
        /// final={ -1, 0, -1, 1, -1, 2 }
        /// 0: -1
        /// 1: 0
        /// 2: -1
        /// 3: 1
        /// 4: -1
        /// 5: 2
        /// </remarks>
        /// <param name="count">how many items were in the original list</param>
        /// <param name="removeIndices">indices that will be removed from the original list</param>
        /// <returns>
        /// map: an array the size of count.  Each element will be the index to the reduced array, or -1 for removed items
        /// from_to: an array the same size as the reduced list that tells old and new index
        /// </returns>
        public static (int[] map, (int from, int to)[] from_to) GetIndexMap(int count, IEnumerable<int> removeIndices)
        {
            List<int> preMap = Enumerable.Range(0, count).
                Select(o => o).
                ToList();

            foreach (int rem in removeIndices.Distinct())
            {
                preMap.Remove(rem);
            }

            var midMap = preMap.
                Select((o, i) => (from: o, to: i)).
                ToArray();

            int[] map = Enumerable.Range(0, count).
                //Select(o => midMap.FirstOrDefault(p => p.from == o)?.to ?? -1).       // can't use ?. with a new style tuple
                Select(o =>
                {
                    foreach (var mid in midMap)
                        if (mid.from == o)
                            return mid.to;
                    return -1;
                }).
                ToArray();

            return (map, midMap);
        }

        /// <summary>
        /// This is useful if you have some outer loop that needs to access a set of items in a round robin.  Just hand
        /// that loop an enumerator of this
        /// </summary>
        /// <remarks>
        /// var getIndex = UtilityCore.InfiniteRoundRobin(count).GetEnumerator();
        /// 
        /// while (someCondition)
        /// {
        ///     getIndex.MoveNext();
        ///     int index = getIndex.Current;
        ///     
        ///     ...
        /// }
        /// </remarks>
        public static IEnumerable<int> InfiniteRoundRobin(int itemCount)
        {
            while (true)
            {
                for (int cntr = 0; cntr < itemCount; cntr++)
                {
                    yield return cntr;
                }
            }
        }
        public static IEnumerable<T> InfiniteRoundRobin<T>(T[] items)
        {
            foreach (int index in InfiniteRoundRobin(items.Length))
            {
                yield return items[index];
            }
        }

        #endregion

        #region Private Methods

        private static T[] TryJoinChains<T>(T[] chain1, T[] chain2, Func<T, T, bool> compare)
        {
            if (compare(chain1[0], chain2[0]))
            {
                return chain1.Reverse<T>().
                    Concat(chain2.Skip(1)).
                    ToArray();
            }
            else if (compare(chain1[chain1.Length - 1], chain2[0]))
            {
                return chain1.
                    Concat(chain2.Skip(1)).
                    ToArray();
            }
            else if (compare(chain1[0], chain2[chain2.Length - 1]))
            {
                return chain2.
                    Concat(chain1.Skip(1)).
                    ToArray();
            }
            else if (compare(chain1[chain1.Length - 1], chain2[chain2.Length - 1]))
            {
                return chain2.
                    Concat(chain1.Reverse<T>().Skip(1)).
                    ToArray();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// This calculates which pair the index points to
        /// </summary>
        /// <remarks>
        /// See GetPairs() to see how these are generated
        /// 
        /// Here is what the pairs look like for 7.  So if you pass in an index of 15, this returns [3,4].
        /// 
        ///index    left    right
        ///0	0	1
        ///1	0	2
        ///2	0	3
        ///3	0	4
        ///4	0	5
        ///5	0	6
        ///6	1	2
        ///7	1	3
        ///8	1	4
        ///9	1	5
        ///10	1	6
        ///11	2	3
        ///12	2	4
        ///13	2	5
        ///14	2	6
        ///15	3	4
        ///16	3	5
        ///17	3	6
        ///18	4	5
        ///19	4	6
        ///20	5	6
        /// 
        /// The linkCount is (c^2-c)/2, but I couldn't think of a way to do the reverse with some kind of sqrt or division (the divisor
        /// shrinks per set).  So I went with a loop to find the current set
        /// </remarks>
        private static (int, int) GetPair(int index, int itemCount)
        {
            // Init to point to the first set
            int left = 0;
            int maxIndex = itemCount - 2;
            int setSize = maxIndex + 1;

            // Loop to find the set that the index falls into
            while (setSize > 0)
            {
                if (index <= maxIndex)
                {
                    int right = left + (setSize - (maxIndex - index));
                    return (left, right);
                }

                setSize -= 1;
                maxIndex += setSize;
                left++;
            }

            throw new ArgumentException(string.Format("Index is too large\r\nIndex={0}\r\nItemCount={1}\r\nLinkCount={2}", index, itemCount, ((itemCount * itemCount) - itemCount) / 2));
        }

        #endregion
    }
}
