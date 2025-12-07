using Jetpack2.Models;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.Core
{
    public static class UtilJetpack
    {
        #region class: PlayerVRPoints

        public class PlayerVRPoints
        {
            public Vector3 basedon_body_forward_world { get; set; }
            public Vector3 basedon_body_up_world { get; set; }

            public PlayerVRPoints_Set world { get; set; }
            // center will be zero.  other points are offsets from that, rotated from body_forward,body_up to (0,0,1)(0,1,0)
            public PlayerVRPoints_Set local { get; set; }

            public Quaternion rot_to_local { get; set; }        // rotation from body_forward,body_up to (0,0,1)(0,1,0)
            public Quaternion rot_to_world { get; set; }        // inverse of to local

            public float height { get; set; }

            public Vector3 Transform_ToLocal(Vector3 point)
            {
                if (world == null)
                    throw new InvalidOperationException("world is null");

                return rot_to_local * (point - world.center);
            }
            public Vector3 Transform_ToWorld(Vector3 point)
            {
                if (world == null)
                    throw new InvalidOperationException("world is null");

                return rot_to_world * point + world.center;
            }
            public static PlayerVRPoints Transform_ToWorld(PlayerVRPoints_Set local, Vector3 body_forward, Vector3 body_up, Vector3 body_center)
            {
                Quaternion to_local = Math3D.GetRotation(new DoubleVector(body_forward, body_up), new DoubleVector(Vector3.forward, Vector3.up));
                Quaternion to_world = Quaternion.Inverse(to_local);

                return new PlayerVRPoints
                {
                    basedon_body_forward_world = body_forward,
                    basedon_body_up_world = body_up,

                    world = new PlayerVRPoints_Set
                    {
                        head = to_world * local.head + body_center,
                        foot = to_world * local.foot + body_center,
                        left = to_world * local.left + body_center,
                        right = to_world * local.right + body_center,
                        center = to_world * local.center + body_center,
                    },
                    local = local,

                    rot_to_local = to_local,
                    rot_to_world = to_world,
                };
            }
        }

        public class PlayerVRPoints_Set
        {
            // Positions
            public Vector3 head { get; set; }
            public Vector3 foot { get; set; }
            public Vector3 left { get; set; }
            public Vector3 right { get; set; }
            public Vector3 center { get; set; }

            // Hand Directions (game)
            // forward is along thumb, up points back toward body
            public Vector3 left_forward { get; set; }
            public Vector3 left_up { get; set; }
            public Vector3 right_forward { get; set; }
            public Vector3 right_up { get; set; }

            // Hand Directions (used for wings)
            // up is out of the top of the hand, forward extends away from the arm with an extra configured rotation
            public Vector3 left_wing_pos { get; set; }
            public Vector3 right_wing_pos { get; set; }
            public Vector3 left_wing_forward { get; set; }
            public Vector3 left_wing_up { get; set; }
            public Vector3 right_wing_forward { get; set; }
            public Vector3 right_wing_up { get; set; }
        }

        #endregion
        #region class: HandWings

        public class HandWings
        {
            public PlayerVRPoints PlayerPoints { get; set; }

            // These will be null if the hands aren't wings (in the wrong region, hand closed, etc)
            public HandWing Left { get; set; }
            public HandWing Right { get; set; }
        }
        public class HandWing
        {
            public float Percent { get; set; }      // can be less than one if arm isn't extended very far, or fingers partially closed
            public bool IsAirBrake { get; set; }
            public Vector3 Normal { get; set; }
            /// <summary>
            /// abs value of velocity direction dot wing's up direction
            /// </summary>
            /// <remarks>
            /// near zero is edge into wind, near one is airbrake
            /// </remarks>
            public float WingDotUp { get; set; }
        }

        #endregion

        /// <summary>
        /// Calculates player's center as mid point between head and hands
        /// Local points are offsets from that center, rotated to (0,0,1)(0,1,0)
        /// </summary>
        public static PlayerVRPoints GetPlayerPoints(Vector3 body_forward, Vector3 body_up)
        {
            Vector3 head_pos = Player.local.head.anchor.position;
            Vector3 foot_pos = Math3D.GetAverage(Player.local.footLeft.ragdollFoot.root.position, Player.local.footRight.ragdollFoot.root.position);        // Player.local.transform.position is the room level origin

            Vector3 foot_to_head = head_pos - foot_pos;

            Vector3 center_player = foot_pos + foot_to_head * 0.5f;

            Vector3 lefthand_pos = Player.local.handLeft.root.position;
            Vector3 righthand_pos = Player.local.handRight.root.position;

            Vector3 left_forward = Player.local.handLeft.root.forward;
            Vector3 left_up = Player.local.handLeft.root.up;
            Vector3 right_forward = Player.local.handRight.root.forward;
            Vector3 right_up = Player.local.handRight.root.up;

            //Vector3 left_wing_up = Vector3.Cross(left_up, left_forward);        
            //Vector3 left_wing_forward = Vector3.Cross(left_wing_up, left_forward);
            //Vector3 right_wing_up = Vector3.Cross(right_forward, right_up);
            //Vector3 right_wing_forward = Vector3.Cross(right_forward, right_wing_up);

            // not sure why these up crosses are correct, but they are (unity is left handed)
            Vector3 left_wing_up = Vector3.Cross(left_forward, left_up);
            Vector3 left_wing_forward = -Vector3.Cross(left_wing_up, left_forward);
            Vector3 right_wing_up = Vector3.Cross(right_up, right_forward);     // NOTE: cross products for left are backward, because of right hand rule
            Vector3 right_wing_forward = -Vector3.Cross(right_forward, right_wing_up);

            float wing_angle = UIModOptions.PlayerPosTracking_WingRotateAngle;
            if (!wing_angle.IsNearZero())
            {
                Quaternion quat = Quaternion.AngleAxis(-wing_angle, left_wing_up);
                left_wing_forward = quat * left_wing_forward;

                quat = Quaternion.AngleAxis(wing_angle, right_wing_up);
                right_wing_forward = quat * right_wing_forward;
            }

            Vector3 left_wing_pos = lefthand_pos;
            Vector3 right_wing_pos = righthand_pos;

            float wing_slide = UIModOptions.PlayerPosTracking_WingTranslateCord;
            if (!wing_slide.IsNearZero())
            {
                Vector3 slide = Vector3.Cross(left_wing_up, left_wing_forward);
                left_wing_pos = lefthand_pos + slide * wing_slide;

                slide = Vector3.Cross(right_wing_forward, right_wing_up);
                right_wing_pos = righthand_pos + slide * wing_slide;
            }

            Quaternion to_local = Math3D.GetRotation(new DoubleVector(body_forward, body_up), new DoubleVector(Vector3.forward, Vector3.up));

            return new PlayerVRPoints
            {
                basedon_body_forward_world = body_forward,
                basedon_body_up_world = body_up,

                world = new PlayerVRPoints_Set
                {
                    head = head_pos,
                    foot = foot_pos,
                    left = lefthand_pos,
                    right = righthand_pos,
                    center = center_player,

                    left_forward = left_forward,
                    left_up = left_up,
                    right_forward = right_forward,
                    right_up = right_up,

                    left_wing_pos = left_wing_pos,
                    right_wing_pos = right_wing_pos,
                    left_wing_forward = left_wing_forward,
                    left_wing_up = left_wing_up,
                    right_wing_forward = right_wing_forward,
                    right_wing_up = right_wing_up,
                },
                local = new PlayerVRPoints_Set
                {
                    head = to_local * (head_pos - center_player),
                    foot = to_local * (foot_pos - center_player),
                    left = to_local * (lefthand_pos - center_player),
                    right = to_local * (righthand_pos - center_player),
                    center = Vector3.zero,

                    left_forward = to_local * left_forward,
                    left_up = to_local * left_up,
                    right_forward = to_local * right_forward,
                    right_up = to_local * right_up,

                    left_wing_pos = to_local * (left_wing_pos - center_player),
                    right_wing_pos = to_local * (right_wing_pos - center_player),
                    left_wing_forward = to_local * left_wing_forward,
                    left_wing_up = to_local * left_wing_up,
                    right_wing_forward = to_local * right_wing_forward,
                    right_wing_up = to_local * right_wing_up,
                },

                height = foot_to_head.magnitude,

                rot_to_local = to_local,
                rot_to_world = Quaternion.Inverse(to_local),
            };
        }

        public static HandWings GetHandWings(PlayerVRPoints positions)
        {
            Vector3 normalized_left = positions.local.left / positions.height;
            Vector3 normalized_right = positions.local.right / positions.height;

            bool in_resting_left = IsIn_Resting(normalized_left, Side.Left);
            bool in_resting_right = IsIn_Resting(normalized_right, Side.Right);

            float? in_transition_left = IsIn_Transition(normalized_left, Side.Left);
            float? in_transition_right = IsIn_Transition(normalized_right, Side.Right);

            bool in_wing_left = IsIn_Wing(normalized_left, Side.Left);
            bool in_wing_right = IsIn_Wing(normalized_right, Side.Right);

            Vector3 velocity = Player.local.locomotion.physicBody.velocity;

            HandWing wing_left = GetWing(in_transition_left, in_wing_left, positions.basedon_body_forward_world, positions.world.left_wing_pos, positions.world.left_wing_forward, positions.world.left_wing_up, velocity, true);
            HandWing wing_right = GetWing(in_transition_right, in_wing_right, positions.basedon_body_forward_world, positions.world.right_wing_pos, positions.world.right_wing_forward, positions.world.right_wing_up, velocity, false);

            return new HandWings
            {
                PlayerPoints = positions,
                Left = wing_left,
                Right = wing_right,
            };
        }

        #region Private Methods - hand wings

        private static bool IsIn_Resting(Vector3 pos, Side side)
        {
            pos = side == Side.Left ?
                new Vector3(-pos.x, pos.y, pos.z) :
                pos;

            bool retVal = true;

            retVal &= pos.x >= UIModOptions.PlayerPosTracking_RestingPos_MinX && pos.x <= UIModOptions.PlayerPosTracking_RestingPos_MaxX;
            retVal &= pos.y >= UIModOptions.PlayerPosTracking_RestingPos_MinY && pos.y <= UIModOptions.PlayerPosTracking_RestingPos_MaxY;
            retVal &= pos.z >= UIModOptions.PlayerPosTracking_RestingPos_MinZ && pos.z <= UIModOptions.PlayerPosTracking_RestingPos_MaxZ;

            return retVal;
        }
        private static float? IsIn_Transition(Vector3 pos, Side side)
        {
            pos = side == Side.Left ?
                new Vector3(-pos.x, pos.y, pos.z) :
                pos;

            bool inRange = true;

            inRange &= pos.x >= UIModOptions.PlayerPosTracking_RestingPos_MaxX && pos.x <= UIModOptions.PlayerPosTracking_WingPos_MinX;     // x is correct

            if (!inRange)
                return null;

            // x is in range, lerp y and z based on percent of x
            float min_y = UtilityMath.GetScaledValue(UIModOptions.PlayerPosTracking_RestingPos_MinY, UIModOptions.PlayerPosTracking_WingPos_MinY, UIModOptions.PlayerPosTracking_RestingPos_MaxX, UIModOptions.PlayerPosTracking_WingPos_MinX, pos.x);
            float max_y = UtilityMath.GetScaledValue(UIModOptions.PlayerPosTracking_RestingPos_MaxY, UIModOptions.PlayerPosTracking_WingPos_MaxY, UIModOptions.PlayerPosTracking_RestingPos_MaxX, UIModOptions.PlayerPosTracking_WingPos_MinX, pos.x);
            float min_z = UtilityMath.GetScaledValue(UIModOptions.PlayerPosTracking_RestingPos_MinZ, UIModOptions.PlayerPosTracking_WingPos_MinZ, UIModOptions.PlayerPosTracking_RestingPos_MaxX, UIModOptions.PlayerPosTracking_WingPos_MinX, pos.x);
            float max_z = UtilityMath.GetScaledValue(UIModOptions.PlayerPosTracking_RestingPos_MaxZ, UIModOptions.PlayerPosTracking_WingPos_MaxZ, UIModOptions.PlayerPosTracking_RestingPos_MaxX, UIModOptions.PlayerPosTracking_WingPos_MinX, pos.x);

            inRange &= pos.y >= min_y && pos.y <= max_y;
            inRange &= pos.z >= min_z && pos.z <= max_z;

            if (!inRange)
                return null;

            return UtilityMath.GetScaledValue(0, 1, UIModOptions.PlayerPosTracking_RestingPos_MaxX, UIModOptions.PlayerPosTracking_WingPos_MinX, pos.x);
        }
        private static bool IsIn_Wing(Vector3 pos, Side side)
        {
            pos = side == Side.Left ?
                new Vector3(-pos.x, pos.y, pos.z) :
                pos;

            bool retVal = true;

            retVal &= pos.x >= UIModOptions.PlayerPosTracking_WingPos_MinX && pos.x <= UIModOptions.PlayerPosTracking_WingPos_MaxX;
            retVal &= pos.y >= UIModOptions.PlayerPosTracking_WingPos_MinY && pos.y <= UIModOptions.PlayerPosTracking_WingPos_MaxY;
            retVal &= pos.z >= UIModOptions.PlayerPosTracking_WingPos_MinZ && pos.z <= UIModOptions.PlayerPosTracking_WingPos_MaxZ;

            return retVal;
        }

        private static HandWing GetWing(float? in_transition, bool in_wing, Vector3 body_forward, Vector3 pos, Vector3 forward, Vector3 up, Vector3 velocity, bool is_left)
        {
            float percent = 1;

            if (in_transition != null)
                percent *= in_transition.Value;

            if (!in_wing && percent.IsNearZero())       // during transition, in_wing will be false
                return null;

            if (UIModOptions.PlayerPosTracking_WingRequireOpenHand)
            {
                float open_percent = (is_left ? PlayerControl.handLeft : PlayerControl.handRight).GetAverageCurlNoThumb();     // 0 is open, 1 is closed

                // Adjust the value against the narrower range
                if (open_percent < WingsData.openhand_min)
                    open_percent = 0;
                else if (open_percent > WingsData.openhand_max)
                    open_percent = 1;
                else
                    open_percent = UtilityMath.GetScaledValue_Capped(0, 1, WingsData.openhand_min, WingsData.openhand_max, open_percent);

                percent *= 1 - open_percent;
            }

            Vector3 compare_forward = velocity.sqrMagnitude < WingsData.minSpeed * WingsData.minSpeed ?
                body_forward :
                velocity.normalized;

            float wing_dot_up = Mathf.Abs(Vector3.Dot(compare_forward, up));
            float dot_mid = (WingsData.dot_airbrake + WingsData.dot_wing) / 2;
            bool is_airbrake = wing_dot_up > dot_mid;

            return new HandWing
            {
                Percent = percent,
                IsAirBrake = is_airbrake,
                Normal = up,
                WingDotUp = wing_dot_up,
            };
        }

        #endregion
    }
}
