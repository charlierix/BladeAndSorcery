using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectlyNormalBaS
{
    public static class Math1D
    {
        #region simple

        public static bool IsDivisible(float larger, float smaller)
        {
            if (smaller.IsNearZero())
            {
                // Divide by zero.  Nothing is divisible by zero, not even zero.  (I looked up "is zero divisible by zero", and got very
                // technical reasons why it's not.  It would be cool to be able to view the world the way math people do.  Visualizing
                // complex equations, etc)
                return false;
            }

            // Divide the larger by the smaller.  If the result is an integer (or very close to an integer), then they are divisible
            double division = larger / smaller;
            double divisionInt = Math.Round(division);

            return division.IsNearValue(divisionInt);
        }

        public static bool IsSameSign(float value1, float value2)
        {
            // dot product of two scalars is just multiplication
            return value1 * value2 > 0;
        }

        #endregion

        #region misc

        /// <remarks>
        /// http://www.mathsisfun.com/data/standard-deviation.html
        /// </remarks>
        public static (float avg, float stdDev) Get_Average_StandardDeviation(IEnumerable<float> values)
        {
            float mean = values.Average();

            // Variance is the average of the of the distance squared from the mean
            float variance = values.
                Select(o =>
                {
                    float diff = o - mean;
                    return diff * diff;     // squaring makes sure it's positive
                }).
                Average();

            return (mean, (float)Math.Sqrt(variance));
        }

        public static (float avg, float stdDev) Get_Average_StandardDeviation(IEnumerable<int> values)
        {
            return Get_Average_StandardDeviation(values.Select(o => (float)o));
        }
        public static (DateTime avg, TimeSpan stdDev) Get_Average_StandardDeviation(IEnumerable<DateTime> dates)
        {
            // I don't know if this is the best way (timezones and all that craziness)

            DateTime first = dates.First();

            float[] hours = dates.
                Select(o => (float)(o - first).TotalHours).
                ToArray();

            var retVal = Get_Average_StandardDeviation(hours);

            return (first + TimeSpan.FromHours(retVal.avg), TimeSpan.FromHours(retVal.stdDev));
        }

        public static int Min(params int[] values)
        {
            return values.Min();
        }
        public static float Min(params float[] values)
        {
            return values.Min();
        }

        public static int Max(params int[] values)
        {
            return values.Max();
        }
        public static float Max(params float[] values)
        {
            return values.Max();
        }

        public static float Avg(params float[] values)
        {
            return values.Average();
        }
        public static float Avg((float value, float weight)[] weightedValues)
        {
            if (weightedValues == null || weightedValues.Length == 0)
            {
                return 0;
            }

            float totalWeight = weightedValues.Sum(o => o.Item2);
            if (totalWeight.IsNearZero())
            {
                return weightedValues.Average(o => o.value);
            }

            float sum = weightedValues.Sum(o => o.value * o.weight);

            return sum / totalWeight;
        }

        /// <summary>
        /// This will try various inputs and come up with an input that produces the desired output
        /// NOTE: This method will only try positive inputs
        /// NOTE: This method assumes an increase in input causes the output to increase
        /// </summary>
        /// <remarks>
        /// NOTE: This first attempt doesn't try to guess the power of the equation (linear, square, sqrt, etc).
        /// It starts at 1, then either keeps multiplying or dividing by 10 until a high and low are found
        /// Then it does a binary search between high and low
        /// 
        /// TODO: Take a parameter: Approximate function
        ///     this is the definition of a function that could be run in reverse to approximate a good starting point, and help with coming up with
        ///     decent input attempts.  This should be refined within this method and returned by this method.  Use Math.NET's Fit.Polynomial?
        /// http://stackoverflow.com/questions/20786756/use-math-nets-fit-polynomial-method-on-functions-of-multiple-parameters
        /// http://www.mathdotnet.com/
        ///     
        /// TODO: If a more robust finder is needed, consider using GeneticSharp
        /// </remarks>
        public static float GetInputForDesiredOutput_PosInput_PosCorrelation(float desiredOutput, float allowableError, Func<float, float> getOutput, int? maxIterations = 5000)
        {
            if (allowableError <= 0)
            {
                throw new ArgumentException("allowableError must be positive: " + allowableError.ToString());
            }

            // Start with an input of 1
            (float input, float output) current = (1f, getOutput(1f));

            if (IsInRange(current.output, desiredOutput, allowableError))
            {
                return current.input;       // lucky guess
            }

            // See if it's above or below the desired output
            (float input, float output)? low = null;
            (float input, float output)? high = null;
            if (current.output < desiredOutput)
            {
                low = current;
            }
            else
            {
                high = current;
            }

            int count = 0;

            while (maxIterations == null || count < maxIterations.Value)
            {
                float nextInput;

                if (low == null)
                {
                    #region too high

                    // Floor hasn't been found.  Try something smaller

                    nextInput = high.Value.input / 12f;

                    current = (nextInput, getOutput(nextInput));

                    if (current.output < desiredOutput)
                    {
                        low = current;      // floor and ceiling are now known
                    }
                    else if (current.output < high.Value.output)
                    {
                        high = current;     // floor still isn't known, but this is closer than the previous ceiling
                    }

                    #endregion
                }
                else if (high == null)
                {
                    #region too low

                    // Ceiling hasn't been found.  Try something larger

                    nextInput = low.Value.input * 12f;

                    current = (nextInput, getOutput(nextInput));

                    if (current.output > desiredOutput)
                    {
                        high = current;     // floor and ceiling are now known
                    }
                    else if (current.output > low.Value.output)
                    {
                        low = current;      // ceiling still isn't known, but this is closer than the previous floor
                    }

                    #endregion
                }
                else
                {
                    #region straddle

                    // Floor and ceiling are known.  Try an input that is between them

                    nextInput = Math1D.Avg(low.Value.input, high.Value.input);

                    current = (nextInput, getOutput(nextInput));

                    if (current.output < desiredOutput && current.Item2 > low.Value.output)
                    {
                        low = current;      // increase the floor
                    }
                    else if (current.output > desiredOutput && current.output < high.Value.output)
                    {
                        high = current;     // decrease the ceiling
                    }

                    #endregion
                }

                if (IsInRange(current.output, desiredOutput, allowableError))
                {
                    return current.input;
                }

                count++;
            }

            //TODO: Take in a param whether to throw an exception or return the best guess
            throw new ApplicationException("Couldn't find a solution");
        }

        /// <summary>
        /// This is a sigmoid that is stretched so that when:
        /// x=0, y=0
        /// x=maxY, y=~.8*maxY
        /// </summary>
        /// <param name="x">Expected range is 0 to infinity (negative x will return a negative y)</param>
        /// <param name="maxY">The return will approach this, but never hit it (asymptote)</param>
        /// <param name="slope">
        /// This is roughly the slope of the curve
        /// NOTE: Slope is probably the wrong term, because it is scaled to maxY (so if maxY is 10, then the actual slope would be about 10, even though you pass in slope=1)
        /// </param>
        public static double PositiveSCurve(double x, double maxY, double slope = 1)
        {
            //const double LN_19 = 2.94443897916644;        // y=.9*maxY at x=maxY
            const double LN_9 = 2.19722457733621;       // y=.8*maxY at x=maxY

            // Find a constant for the maxY passed in so that when x=maxY, the below function will equal .8*maxY (when slope is 1)
            //double factor = Math.Log(9) / (maxY * Math.E);
            double factor = LN_9 / (maxY * Math.E);

            //return -1 + (2 / (1 + e ^ (-slope * e * x)));
            //return -maxY + ((2 * maxY) / (1 + Math.E ^ (-slope * Math.E * x)));
            return -maxY + ((2 * maxY) / (1 + Math.Pow(Math.E, (-(factor * slope) * Math.E * x))));
        }

        /// <summary>
        /// A bell curve
        /// </summary>
        /// <remarks>
        /// http://hyperphysics.phy-astr.gsu.edu/hbase/Math/gaufcn.html
        /// 
        /// Paste this into desmos for a visualization
        /// https://www.desmos.com/calculator
        /// \frac{1}{\sqrt{2\cdot\pi\cdot s^2}}\cdot e^{\frac{-\left(x-m\right)^2}{2s^2}}
        /// 
        /// NOTE: For the opposite of the bell curve, see MathND.RandN.  If you took a large sample of those randn results
        /// and ran them through Debug3DWindow.GetCountGraph(), you would get the bell curve that this function returns
        /// (for mean=0 and stddev=1)
        /// </remarks>
        /// <param name="mean">This is where the hump is centered over</param>
        /// <param name="stddev">A positive number.  The smaller it is, the more of a spike the curve becomes</param>
        public static double GetGaussian(double x, double mean = 0, double stddev = 1)
        {
            double two_s_sqr = 2 * stddev * stddev;

            double num1 = x - mean;
            num1 *= num1;
            num1 = -num1;

            double e_to_x = Math.Pow(Math.E, num1 / two_s_sqr);

            return e_to_x / Math.Sqrt(Math.PI * two_s_sqr);
        }

        /// <summary>
        /// Full framework Math has this function, Mathf supports float, so adding a double overload here
        /// </summary>
        public static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            else if (value > max)
                return max;
            else
                return value;
        }

        #endregion

        #region Private Methods

        private static bool IsInRange(double testValue, double compareTo, double allowableError)
        {
            return testValue >= compareTo - allowableError && testValue <= compareTo + allowableError;
        }

        #endregion
    }
}
