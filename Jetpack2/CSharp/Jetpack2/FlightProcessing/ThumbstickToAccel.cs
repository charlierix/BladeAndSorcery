using PerfectlyNormalBaS;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.FlightProcessing
{
    public class ThumbstickToAccel
    {
        // TODO: the current input option makes logic sense but is really hard to control, since when rotated 90 degrees,
        // different stick inputs are needed to go the same direction
        //
        // let the current method be one option.  another would be for the thumbsticks to do the same motion, but relative
        // to the nearest 90 or maybe 45 degree plane

        // TODO: figure out how to map right stick's left/right to different rotations.  for example, when facing the floor
        // (forward is -y), have it rotate the player along y instead of head's up

        /// <summary>
        /// Looks at sticks and player's orientation, returns a unit vector
        /// </summary>
        /// <remarks>
        /// This is kind of a copy of AccelHorz and AccelUp.  The output is sent to classes that avoid hitting things, and
        /// is used so they don't fight with the desired input direction
        /// </remarks>
        public static Vector3? GetInputDirection(Vector2 left_stick, Vector2 right_stick, Locomotion loco)
        {
            var pointer = Pointer.GetActive();
            if (pointer.isPointingUI)
                return null;

            Vector3 retVal = Vector3.zero;

            var transform = Player.local.transform;

            retVal += transform.forward * left_stick.y;
            retVal += transform.right * left_stick.x;
            retVal += transform.up * right_stick.y;

            if (retVal.IsNearZero())
                return null;

            return retVal.normalized;
        }

        /// <summary>
        /// This will apply acceleration along the input's direction, but multiply the horz/vert by config mults.
        /// Also adjusts when going with or against gravity
        /// </summary>
        public static void ApplyAccel(Vector3? input_dir, Locomotion loco, float horz_mult, float vert_mult, float gravity)
        {
            if (input_dir == null)
                return;

            Vector3 horz = input_dir.Value.GetProjectedVector_plane(Vector3.up);
            Vector3 vert = input_dir.Value.GetProjectedVector(Vector3.up);

            var transform = Player.local.transform;

            if (!horz.IsNearZero())
                loco.physicBody.AddForce(horz * horz_mult, ForceMode.Acceleration);

            if (!vert.IsNearZero())
            {
                Vector3 vert2 = vert * vert_mult;

                if (vert2.y > 0)
                    vert2 += Vector3.up * gravity;     // when pushing up, cancel out gravity.  When pushing down, it's accelerating down in addition to gravity

                loco.physicBody.AddForce(vert2, ForceMode.Acceleration);
            }
        }
    }
}
