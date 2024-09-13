using System;
using System.Collections.Generic;
using System.Text;

namespace PerfectlyNormalBaS.FollowDirectionPosition
{
    public class GradientEntry
    {
        public GradientEntry(float distance, float percent)
        {
            Distance = distance;
            Percent = percent;
        }

        public readonly float Distance;
        public readonly float Percent;

        public static float GetGradientPercent(float distance, GradientEntry[] gradient)
        {
            // See if they are outside the gradient (if so, use that cap's %)
            if (distance <= gradient[0].Distance)
            {
                return gradient[0].Percent;
            }
            else if (distance >= gradient[gradient.Length - 1].Distance)
            {
                return gradient[gradient.Length - 1].Percent;
            }

            //  It is inside the gradient.  Find the two stops that are on either side
            for (int cntr = 0; cntr < gradient.Length - 1; cntr++)
            {
                if (distance > gradient[cntr].Distance && distance <= gradient[cntr + 1].Distance)
                {
                    // LERP between the from % and to %
                    return UtilityMath.GetScaledValue(gradient[cntr].Percent, gradient[cntr + 1].Percent, gradient[cntr].Distance, gradient[cntr + 1].Distance, distance);        //NOTE: Not calling the capped overload, because max could be smaller than min (and capped would fail)
                }
            }

            throw new ApplicationException("Execution should never get here");
        }
    }
}
