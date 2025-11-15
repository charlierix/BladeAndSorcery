using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jetpack2.FlightProcessing
{
    public class Wings
    {

        // check if either or both hands are stretched out

        // if both hands stretched out and roughly mirror each other, and fingers are open, continue with wing or braking

        // if only one hand is stretched, only do braking if hand is at correct range of angles



        // hand has good angle of attack: wing
        // hand is perpendicular to velocity: brake


        // see egg wings AeroSurface
        //  that class is too tied into game object and rigid body
        //  make a class that can calculate the same, but is a util function

        public void Update()
        {
            // figure out thresholds based on height using head to foot distance

            //float left_outstretched = GetPercentOutstretched();
            //float right_outstretched = GetPercentOutstretched();

            //if(left_outstretched)     // get angle of attack


            // --------------------------------------

            // the shape may be a cylinder or truncated cone

            // when alt button is pressed, draw a point every N ms





        }
        public void UpdateFixed()
        {

        }


        private float GetPercentOutstretched()
        {
            return 0;
        }
    }
}
