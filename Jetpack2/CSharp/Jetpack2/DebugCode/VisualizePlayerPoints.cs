using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.DebugCode
{
    /// <summary>
    /// This places dots, trying to figure out where the player is
    /// </summary>
    public class VisualizePlayerPoints
    {
        private DebugRenderer3D _renderer = null;

        private DebugItem _player_pos = null;
        //private DebugItem _globalOffset_pos = null;
        //private DebugItem _headOffset_pos = null;
        //private DebugItem _handOffset_pos = null;

        private DebugItem _gameobject_pos = null;
        private DebugItem _rigidbody_pos = null;
        private DebugItem _collider_pos = null;

        private DebugItem _lefthand_pos = null;
        private DebugItem _righthand_pos = null;
        private DebugItem _leftfoot_pos = null;
        private DebugItem _rightfoot_pos = null;
        private DebugItem _leftfoot_direction = null;
        private DebugItem _waist_pos = null;
        private DebugItem _head_pos = null;

        private DebugItem _lefthand_forward = null;
        private DebugItem _lefthand_up = null;
        private DebugItem _righthand_forward = null;
        private DebugItem _righthand_up = null;

        private DebugItem _head_line1 = null;
        private DebugItem _head_line2 = null;

        public void Update(float scale)
        {
            // TODO: remove currently drawn objects?  if so, store in a wrapper object so there is only a single null check when they aren't there
            if (!UIModOptions.VisualizePlayerPoints)
                return;

            if (_renderer == null)
                _renderer = DebugRenderer3D.GetOrAddDebugRenderer3D();

            // --------------- room ---------------

            // These are not the player's feet, but the centerpoint of the floor of the living room in game (the player can walk around this point)
            UpdateDot(ref _player_pos, Player.local.transform.position, Color.white, scale);

            // Waist height, but above transform instead of tied to the player
            UpdateDot(ref _waist_pos, Player.local.waist.ikAnchor.position, UtilityColor.FromHex("DBA746"), scale);

            // I'm guessing these are configured offsets - zero by default
            //UpdateDot(ref _globalOffset_pos, Player.local.globalOffsetTransform.position, Color.blue);      // this is the same as transform
            //UpdateDot(ref _headOffset_pos, Player.local.headOffsetTransform.position, Color.green);
            //UpdateDot(ref _handOffset_pos, Player.local.handOffsetTransform.position, Color.red);

            // Both of these were (0.26, 0.96, 1.67)
            //Debug.Log($"headOffsetTransform: {Player.local.headOffsetTransform?.position.ToString() ?? "null"}");
            //Debug.Log($"handOffsetTransform: {Player.local.handOffsetTransform?.position.ToString() ?? "null"}");




            // --------------- untested ---------------


            //UpdateDot(ref _gameobject_pos, Player.local.gameObject.transform.position, Color.blue, scale);
            //Debug.Log($"Player.local.transform.position: {Player.local.transform.position.ToStringSignificantDigits(3)}");
            //Debug.Log($"Player.local.gameObject.transform.position: {Player.local.gameObject.transform.position.ToStringSignificantDigits(3)}");


            //if (Player.local.gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
            //{
            //    UpdateDot(ref _rigidbody_pos, Player.local.gameObject.transform.position, Color.cyan, scale);
            //    Debug.Log($"rb.transform.position: {rb.transform.position.ToStringSignificantDigits(3)}");
            //}
            //else
            //{
            //    UpdateDot(ref _rigidbody_pos, Vector3.zero, Color.cyan, scale);
            //    Debug.Log("no rigid body");
            //}


            //if (Player.local.gameObject.TryGetComponent<Collider>(out Collider col))
            //{
            //    UpdateDot(ref _collider_pos, col.transform.position, Color.yellow, scale);
            //    Debug.Log($"col.transform.position: {col.transform.position.ToStringSignificantDigits(3)}");
            //}
            //else
            //{
            //    UpdateDot(ref _collider_pos, Vector3.zero, Color.cyan, scale);
            //    Debug.Log("no collider");
            //}



            // --------------- player ---------------

            // These stay with the player when in 3rd person mode
            // When the player rotates around, these rotate with

            // These two are active during the character selection scene.  The others (feet, waist, head) are probably null
            Vector3 lefthand = Player.local.handLeft.root.position;
            Vector3 righthand = Player.local.handRight.root.position;
            UpdateDot(ref _lefthand_pos, lefthand, Color.red, scale);
            UpdateDot(ref _righthand_pos, righthand, Color.green, scale);

            UpdateLine(ref _lefthand_forward, lefthand, lefthand + Player.local.handLeft.root.forward, Color.blue, scale);
            UpdateLine(ref _lefthand_up, lefthand, lefthand + Player.local.handLeft.root.up, Color.green, scale);
            UpdateLine(ref _righthand_forward, righthand, righthand + Player.local.handRight.root.forward, Color.blue, scale);
            UpdateLine(ref _righthand_up, righthand, righthand + Player.local.handRight.root.up, Color.green, scale);

            // These two appear to be the same point (center of where the feet are)
            // The legs animate, but this stays stable
            // Maybe they would be different if wearing trackers
            UpdateDot(ref _leftfoot_pos, Player.local.footLeft.ragdollFoot.root.position, UtilityColor.FromHex("B14A47"), scale);
            UpdateDot(ref _rightfoot_pos, Player.local.footRight.ragdollFoot.root.position, UtilityColor.FromHex("59FF7D"), scale);

            // this isn't any better than using spine for forward
            UpdateLine(ref _leftfoot_direction, Player.local.footLeft.ragdollFoot.root.position, Player.local.footLeft.ragdollFoot.root.position + Player.local.footLeft.ragdollFoot.root.forward, UtilityColor.FromHex("B14A47"), scale);

            //UpdateDot(ref _head_pos, Player.local.head.anchor.position, UtilityColor.FromHex("C0CCD9"));      // this would block the view, using lines instead
            UpdateLine(ref _head_line1, Player.local.handLeft.root.position, Player.local.head.anchor.position, UtilityColor.FromHex("C0CCD9"), scale);
            UpdateLine(ref _head_line2, Player.local.handRight.root.position, Player.local.head.anchor.position, UtilityColor.FromHex("C0CCD9"), scale);
        }

        public void Clear()
        {
            if (_renderer == null)
                return;

            if (_player_pos != null)
            {
                _renderer.Remove(_player_pos);
                _player_pos = null;
            }

            if (_lefthand_pos != null)
            {
                _renderer.Remove(_lefthand_pos);
                _lefthand_pos = null;
            }

            if (_righthand_pos != null)
            {
                _renderer.Remove(_righthand_pos);
                _righthand_pos = null;
            }

            if (_leftfoot_pos != null)
            {
                _renderer.Remove(_leftfoot_pos);
                _leftfoot_pos = null;
            }

            if (_rightfoot_pos != null)
            {
                _renderer.Remove(_rightfoot_pos);
                _rightfoot_pos = null;
            }

            if (_leftfoot_direction != null)
            {
                _renderer.Remove(_leftfoot_direction);
                _leftfoot_direction = null;
            }

            if (_waist_pos != null)
            {
                _renderer.Remove(_waist_pos);
                _waist_pos = null;
            }

            if (_head_pos != null)
            {
                _renderer.Remove(_head_pos);
                _head_pos = null;
            }

            if (_lefthand_forward != null)
            {
                _renderer.Remove(_lefthand_forward);
                _lefthand_forward = null;
            }

            if (_lefthand_up != null)
            {
                _renderer.Remove(_lefthand_up);
                _lefthand_up = null;
            }

            if (_righthand_forward != null)
            {
                _renderer.Remove(_righthand_forward);
                _righthand_forward = null;
            }

            if (_righthand_up != null)
            {
                _renderer.Remove(_righthand_up);
                _righthand_up = null;
            }

            if (_head_line1 != null)
            {
                _renderer.Remove(_head_line1);
                _head_line1 = null;
            }

            if (_head_line2 != null)
            {
                _renderer.Remove(_head_line2);
                _head_line2 = null;
            }
        }

        private void UpdateDot(ref DebugItem item, Vector3 pos, Color color, float scale)
        {
            const float SIZE = 0.1f * 2;

            if (item != null && item.Object == null)
                item = null;

            if (item == null)
                item = _renderer.AddDot(pos, 1f, color);
            else
                item.Object.transform.position = pos;

            item.Object.transform.localScale = new Vector3(SIZE * scale, SIZE * scale, SIZE * scale);
        }
        private void UpdateLine(ref DebugItem item, Vector3 pos1, Vector3 pos2, Color color, float scale)
        {
            const float THICKNESS = 0.01f;

            if (item != null && item.Object == null)
                item = null;

            if (item == null)
                item = _renderer.AddLine_Basic(pos1, pos2, THICKNESS * scale, color);
            else
                DebugRenderer3D.AdjustLinePositions(item, pos1, pos2, THICKNESS * scale);
        }
    }
}
