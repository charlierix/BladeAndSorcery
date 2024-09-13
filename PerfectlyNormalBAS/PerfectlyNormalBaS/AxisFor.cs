using System;
using System.Collections.Generic;
using System.Text;
using static UnityEngine.RectTransform;

namespace PerfectlyNormalUnity
{
    #region enum: AxisDim

    public enum AxisDim
    {
        X,
        Y,
        Z,
        //W,
        //d5,
        //d6,
        //d7,
        //etc (or some other common way of identifying higher dimensions)
    }

    #endregion
    #region struct: AxisFor

    /// <summary>
    /// This helps with running for loops against an axis
    /// </summary>
    public partial struct AxisFor
    {
        public AxisFor(AxisDim axis, int start, int stop)
        {
            Axis = axis;
            Start = start;
            Stop = stop;

            IsPos = Stop > Start;
            Increment = IsPos ? 1 : -1;
        }

        public readonly AxisDim Axis;
        public readonly int Start;
        public readonly int Stop;
        public readonly int Increment;
        public readonly bool IsPos;

        public int Length => Math.Abs(Stop - Start) + 1;

        /// <summary>
        /// This will set one of the output x,y,z to index3D based on this.Axis
        /// </summary>
        public void Set3DIndex(ref int x, ref int y, ref int z, int index3D)
        {
            switch (Axis)
            {
                case AxisDim.X:
                    x = index3D;
                    break;

                case AxisDim.Y:
                    y = index3D;
                    break;

                case AxisDim.Z:
                    z = index3D;
                    break;

                default:
                    throw new ApplicationException($"Unknown Axis: {Axis}");
            }
        }
        public void Set2DIndex(ref int x, ref int y, int index2D)
        {
            switch (Axis)
            {
                case AxisDim.X:
                    x = index2D;
                    break;

                case AxisDim.Y:
                    y = index2D;
                    break;

                case AxisDim.Z:
                    throw new ApplicationException("Didn't expect Z axis");

                default:
                    throw new ApplicationException($"Unknown Axis: {Axis}");
            }
        }
        public int GetValueForOffset(int value2D)
        {
            if (IsPos)
                return value2D;

            else
                return Start - value2D;        // using start, because it's negative, so start is the larger value
        }

        public IEnumerable<int> Iterate()
        {
            for (int cntr = Start; IsPos ? cntr <= Stop : cntr >= Stop; cntr += Increment)
            {
                yield return cntr;
            }
        }

        public bool IsBetween(int test)
        {
            if (IsPos)
                return test >= Start && test <= Stop;

            else
                return test >= Stop && test <= Start;
        }

        public override string ToString()
        {
            string by = Math.Abs(Increment) == 1 ?
                "" :
                $" by {Increment}";

            return string.Format("{0}: {1} to {2}{3}", Axis, Start, Stop, by);
        }
    }

    #endregion
    #region struct: AxisForFloat

    /// <summary>
    /// This helps with running for loops against an axis
    /// </summary>
    public struct AxisForFloat
    {
        /// <summary>
        /// This overload will walk from start to stop, across steps+1 number of times (
        /// </summary>
        /// <remarks>
        /// Iterate() will return start up to and including stop
        /// </remarks>
        public AxisForFloat(AxisDim axis, float start, float stop, int steps)
        {
            if (steps <= 0)
                throw new ArgumentException($"steps must be positive: {steps}");

            else if (start.IsNearValue(stop))
                throw new ArgumentException($"start and stop can't be the same value: {start}");

            Axis = axis;
            Start = start;
            Stop = stop;

            IsPos = Stop > Start;
            Increment = (stop - start) / steps;
        }
        /// <summary>
        /// This overload sets up the struct to only have one value.  When you call Iterate(), it returns that one value, then stops
        /// </summary>
        public AxisForFloat(AxisDim axis, float value)
        {
            Axis = axis;
            Start = value;
            Stop = value;

            IsPos = true;
            Increment = 100;       // this way iterate will only return one value
        }

        public readonly AxisDim Axis;
        public readonly float Start;
        public readonly float Stop;
        public readonly float Increment;
        public readonly bool IsPos;

        /// <summary>
        /// This will set one of the output x,y,z to value2D based on this.Axis
        /// </summary>
        public void SetCorrespondingValue(ref float x, ref float y, ref float z, float value)
        {
            switch (Axis)
            {
                case AxisDim.X:
                    x = value;
                    break;

                case AxisDim.Y:
                    y = value;
                    break;

                case AxisDim.Z:
                    z = value;
                    break;

                default:
                    throw new ApplicationException($"Unknown Axis: {Axis}");
            }
        }

        public IEnumerable<float> Iterate()
        {
            float retVal = Start;

            while ((IsPos ? retVal < Stop : retVal > Stop) || retVal.IsNearValue(Stop))
            {
                yield return retVal;
                retVal += Increment;
            }
        }

        public override string ToString()
        {
            string by = Math.Abs(Increment).IsNearValue(1) ?
                "" :
                $" by {Increment.ToStringSignificantDigits(2)}";

            return string.Format("{0}: {1} to {2}{3}", Axis, Start.ToStringSignificantDigits(2), Stop.ToStringSignificantDigits(2), by);
        }
    }

    #endregion
    #region struct: Mapping_2D_1D

    /// <summary>
    /// This is a mapping between 2D and 1D (good for bitmaps, or other rectangle grids that are physically stored as 1D arrays)
    /// </summary>
    public struct Mapping_2D_1D
    {
        public Mapping_2D_1D(int x, int y, int offset1D)
        {
            X = x;
            Y = y;
            Offset1D = offset1D;
        }

        public readonly int X;
        public readonly int Y;
        public readonly int Offset1D;
    }

    #endregion
    #region struct: Mapping_3D_1D

    /// <summary>
    /// This is a mapping between 3D and 1D
    /// </summary>
    public struct Mapping_3D_1D
    {
        public Mapping_3D_1D(int x, int y, int z, int offset1D)
        {
            X = x;
            Y = y;
            Z = z;
            Offset1D = offset1D;
        }

        public readonly int X;
        public readonly int Y;
        public readonly int Z;
        public readonly int Offset1D;
    }

    #endregion
}
