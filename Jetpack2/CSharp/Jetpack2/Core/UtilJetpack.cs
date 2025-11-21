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
    }
}
