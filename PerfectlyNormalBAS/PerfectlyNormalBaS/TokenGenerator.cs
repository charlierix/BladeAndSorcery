using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PerfectlyNormalUnity
{
    /// <summary>
    /// This hands out a unique token each time its called
    /// NOTE: This IS threadsafe
    /// </summary>
    /// <remarks>
    /// Random trivia:
    /// int64 can go up to 9 quintillion
    /// If you burn through 1 billion tokens a second, it will take 286 years to reach long.Max (double if starting at long.Min instead of zero)
    /// </remarks>
    public class TokenGenerator
    {
        private static long _nextToken;

        public static long NextToken()
        {
            return Interlocked.Increment(ref _nextToken);
        }

        public static long CurrentToken_DEBUGGINGONLY()
        {
            return Interlocked.Read(ref _nextToken);
        }
    }
}
