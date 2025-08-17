using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jetpack.Scanning
{
    // This will focus on not running into walls, trees, etc

    public class ObstacleAvoidance
    {

        // probably split this into two classes.  the low flying v class (repelground) should be generalized for ground or wall


        // if about to run into something along vel, do a slight push to avoid
        //  this will be new behavior

        // if about to brush along a wall, do a slight push away from the wall
        //  this will be a modification of low flying v

        public void Update_CastRays()
        {

        }
        public void Update_Finish()
        {

        }
    }
}
