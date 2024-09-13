using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace PerfectlyNormalUnity
{
    //NOTE: UnityEngine.Random isn't threadsafe

    /// <summary>
    /// This is a wrapper to unique random classes per thread, each one generated with a random seed
    /// </summary>
    /// <remarks>
    /// Got this here:
    /// http://blogs.msdn.com/b/pfxteam/archive/2009/02/19/9434171.aspx
    /// </remarks>
    public static class StaticRandom
    {
        #region class: RandomWrapper

        private class RandomWrapper
        {
            /// <summary>
            /// This way the random instance gets swapped out using a new crypto seed.  This gives a good blend between
            /// speed and true randomness
            /// </summary>
            private int _elapse = -1;

            private Random _rand = null;
            public Random Random
            {
                get
                {
                    _elapse--;

                    if (_elapse < 0)
                    {
                        // Create a unique seed (can't just instantiate Random without a seed, because if a bunch of threads are spun up quickly, and each requests
                        // its own class, they will all have the same seed - seed is based on tickcount, which is time dependent)
                        byte[] buffer = new byte[4];
                        _globalRandom.GetBytes(buffer);

                        _rand = new Random(BitConverter.ToInt32(buffer, 0));

                        // Figure out how long this should live
                        _elapse = _rand.Next(400, 1500);
                    }

                    return _rand;
                }
            }
        }

        #endregion

        #region Declaration Section

        /// <summary>
        /// This serves as both a lock object and a random number generator whenever a new thread needs its own random class
        /// </summary>
        /// <remarks>
        /// RNGCryptoServiceProvider is slower than Random, but is more random.  So this is used when coming up with a new random
        /// class (because speed isn't as important for the one time call per thread)
        /// </remarks>
        private static RNGCryptoServiceProvider _globalRandom = new RNGCryptoServiceProvider();

        private static ThreadLocal<RandomWrapper> _localRandom = new ThreadLocal<RandomWrapper>(() => new RandomWrapper());

        #endregion

        #region Public Methods

        /// <summary>
        /// This is the random class for the current thread.  It is exposed for optimization reasons.
        /// WARNING: Don't share this instance across threads
        /// </summary>
        /// <remarks>
        /// This is exposed publicly in case many random numbers are needed by the calling function.  The ThreadStatic attribute has a slight expense
        /// to it, so if you have a loop that needs hundreds of random numbers, it's better to call this method, and use the returned class directly, instead
        /// of calling this class's static Next method over and over.
        /// </remarks>
        public static Random GetRandomForThread()
        {
            return _localRandom.Value.Random;
        }

        public static int Next()
        {
            return _localRandom.Value.Random.Next();
        }
        public static int Next(int maxValue)
        {
            return _localRandom.Value.Random.Next(maxValue);
        }
        public static int Next(int minValue, int maxValue)
        {
            return _localRandom.Value.Random.Next(minValue, maxValue);
        }

        public static void NextBytes(byte[] buffer)
        {
            _localRandom.Value.Random.NextBytes(buffer);
        }

        public static float NextFloat()
        {
            return _localRandom.Value.Random.NextFloat();
        }
        public static float NextFloat(float maxValue)
        {
            return _localRandom.Value.Random.NextFloat(maxValue);
        }
        public static float NextFloat(float minValue, float maxValue)
        {
            return _localRandom.Value.Random.NextFloat(minValue, maxValue);
        }

        public static float NextPercent(float midPoint, float percent, bool useRandomPercent = true)
        {
            return _localRandom.Value.Random.NextPercent(midPoint, percent, useRandomPercent);
        }
        public static float NextDrift(float midPoint, float drift, bool useRandomDrift = true)
        {
            return _localRandom.Value.Random.NextDrift(midPoint, drift, useRandomDrift);
        }

        public static float NextPow(float power, float maxValue = 1f, bool isPlusMinus = false)
        {
            return _localRandom.Value.Random.NextPow(power, maxValue, isPlusMinus);
        }

        public static bool NextBool()
        {
            return _localRandom.Value.Random.NextBool();
        }

        public static string NextString(int length)
        {
            return _localRandom.Value.Random.NextString(length);
        }
        public static string NextString(int randLengthFrom, int randLengthTo)
        {
            return _localRandom.Value.Random.NextString(randLengthFrom, randLengthTo);
        }

        public static T NextItem<T>(T[] items)
        {
            return _localRandom.Value.Random.NextItem(items);
        }
        public static T NextItem<T>(IList<T> items)
        {
            return _localRandom.Value.Random.NextItem(items);
        }

        public static UnityEngine.Vector2 InsideUnitCircle()
        {
            return _localRandom.Value.Random.InsideUnitCircle();
        }
        public static UnityEngine.Vector2 OnUnitCircle()
        {
            return _localRandom.Value.Random.OnUnitCircle();
        }
        public static UnityEngine.Vector2 RangeCircle(float minRadius, float maxRadius)
        {
            return _localRandom.Value.Random.RangeCircle(minRadius, maxRadius);
        }
        public static UnityEngine.Vector2 CircleShell(float radius)
        {
            return _localRandom.Value.Random.CircleShell(radius);
        }

        public static UnityEngine.Vector3 InsideUnitSphere()
        {
            return _localRandom.Value.Random.InsideUnitSphere();
        }
        public static UnityEngine.Vector3 OnUnitSphere()
        {
            return _localRandom.Value.Random.OnUnitSphere();
        }
        public static UnityEngine.Vector3 RangeSphere(float minRadius, float maxRadius)
        {
            return _localRandom.Value.Random.RangeSphere(minRadius, maxRadius);
        }
        public static UnityEngine.Vector3 SphereShell(float radius)
        {
            return _localRandom.Value.Random.SphereShell(radius);
        }

        public static UnityEngine.Quaternion RotationUniform()
        {
            return _localRandom.Value.Random.RotationUniform();
        }

        //NOTE: All values are 0 to 1
        public static UnityEngine.Color ColorHSV()
        {
            return _localRandom.Value.Random.ColorHSV();
        }
        public static UnityEngine.Color ColorHSV(float hueMin, float hueMax)
        {
            return _localRandom.Value.Random.ColorHSV(hueMin, hueMax);
        }
        public static UnityEngine.Color ColorHSV(float hueMin, float hueMax, float saturationMin, float saturationMax, float valueMin, float valueMax)
        {
            return _localRandom.Value.Random.ColorHSV(hueMin, hueMax, saturationMin, saturationMax, valueMin, valueMax);
        }

        public static UnityEngine.Color ColorHSVA(float alphaMin, float alphaMax)
        {
            return _localRandom.Value.Random.ColorHSVA(alphaMin, alphaMax);
        }
        public static UnityEngine.Color ColorHSVA(float hueMin, float hueMax, float alphaMin, float alphaMax)
        {
            return _localRandom.Value.Random.ColorHSVA(hueMin, hueMax, alphaMin, alphaMax);
        }
        public static UnityEngine.Color ColorHSVA(float hueMin, float hueMax, float saturationMin, float saturationMax, float valueMin, float valueMax, float alphaMin, float alphaMax)
        {
            return _localRandom.Value.Random.ColorHSVA(hueMin, hueMax, saturationMin, saturationMax, valueMin, valueMax, alphaMin, alphaMax);
        }

        #endregion
    }
}
