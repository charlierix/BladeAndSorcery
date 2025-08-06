using Microsoft.SqlServer.Server;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jetpack.Scanning
{
    public class ObstacleAvoidance
    {
        private const bool SHOWDEBUG = true;
        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.04f;
        private DebugRenderer3D _renderer = null;
        private DebugItem _vel_text = null;
        private DebugItem _vel_line = null;
        private DebugItem _pos_foot = null;

        private DebugItem _head_forward = null;
        private DebugItem _head_up = null;
        private DebugItem _head_right = null;


        public Vector3? GetGroundAccel(ThunderRoad.Locomotion loco)
        {
            if (!JetpackScript.ShouldRepelGround)
            {
                if (SHOWDEBUG)
                    ClearDebugVisuals();
                return null;
            }

            // Check if traveling downward
            if (loco.physicBody.velocity.y >= 0)
            {
                if (SHOWDEBUG)
                    ClearDebugVisuals();
                return null;
            }

            // Figure how ray start point (player's feet)
            Vector3 foot_pos = Math3D.GetAverage(Player.local.footLeft.ragdollFoot.root.position, Player.local.footRight.ragdollFoot.root.position);        // Player.local.transform.position is the room level origin

            if (SHOWDEBUG)
                DrawFootPos(foot_pos);

            if (SHOWDEBUG)
                DrawVelocity(foot_pos, loco.physicBody.velocity);



            // Figure out ray length (some combination of down velocity and player's height * scale)



            // Increase accel based on distance, speed, strength



            return null;
        }

        #region Private Methods - Debug Drawing

        private void EnsureDebugActive()
        {
            if (_renderer == null)
                //_renderer = Player.local.gameObject.AddComponent<DebugRenderer3D>();
                _renderer = new DebugRenderer3D();
        }

        private void ClearDebugVisuals()
        {
            if (_renderer == null)
                return;

            if (_vel_text != null)
            {
                _renderer.Remove(_vel_text);
                _vel_text = null;
            }

            if (_vel_line != null)
            {
                _renderer.Remove(_vel_line);
                _vel_line = null;
            }

            if (_pos_foot != null)
            {
                _renderer.Remove(_pos_foot);
                _pos_foot = null;
            }
        }

        private void DrawVelocity(Vector3 pos, Vector3 velocity)
        {
            EnsureDebugActive();

            if (_vel_line == null)
                _vel_line = _renderer.AddLine_Basic(pos, pos + velocity, LINE_THICKNESS, Color.yellow);

            string text = $"{velocity.magnitude.ToStringSignificantDigits(3)} - {velocity.ToStringSignificantDigits(1)}";

            #region draw head orientation

            //Vector3 headaxis_pos = Player.local.head.anchor.position + Player.local.head.transform.forward * 1f;

            //if (_head_forward == null)
            //{
            //    _head_forward = _renderer.AddLine_Basic(headaxis_pos, headaxis_pos + Player.local.head.transform.forward, LINE_THICKNESS, Color.blue);
            //    _head_right = _renderer.AddLine_Basic(headaxis_pos, headaxis_pos + Player.local.head.transform.right, LINE_THICKNESS, Color.red);
            //    _head_up = _renderer.AddLine_Basic(headaxis_pos, headaxis_pos + Player.local.head.transform.up, LINE_THICKNESS, Color.green);
            //}

            //DebugRenderer3D.AdjustLinePositions(_head_forward, headaxis_pos, headaxis_pos + Player.local.head.transform.forward);       // NOTE: head.anchor is rotated 90 degrees clockwise, need to use head.transform for directions
            //DebugRenderer3D.AdjustLinePositions(_head_right, headaxis_pos, headaxis_pos + Player.local.head.transform.right);
            //DebugRenderer3D.AdjustLinePositions(_head_up, headaxis_pos, headaxis_pos + Player.local.head.transform.up);

            #endregion

            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                Player.local.head.transform.right * 0.75f +
                Player.local.head.transform.up * -0.33f;

            if (_vel_text == null)
                _vel_text = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, Color.yellow, Color.black, TEXT_HEIGHT);

            DebugRenderer3D.AdjustLinePositions(_vel_line, pos, pos + velocity);
            _vel_text.Object.transform.position = text_pos;
            _vel_text.Object.transform.rotation = Quaternion.LookRotation(Player.local.head.transform.forward, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_vel_text, new_text: text);
        }

        private void DrawFootPos(Vector3 pos)
        {
            EnsureDebugActive();

            if (_pos_foot == null)
                _pos_foot = _renderer.AddDot(pos, DOT_SIZE, Color.yellow);

            _pos_foot.Object.transform.position = pos;
        }

        #endregion
    }
}
