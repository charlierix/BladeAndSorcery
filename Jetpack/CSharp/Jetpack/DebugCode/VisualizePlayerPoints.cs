using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.DebugCode
{
    /// <summary>
    /// This places dots, trying to figure out where the player is
    /// </summary>
    public class VisualizePlayerPoints
    {
        private const bool SHOULD_DRAW = true;

        private DebugRenderer3D _renderer = null;

        private DebugItem _player_pos = null;
        //private DebugItem _globalOffset_pos = null;
        //private DebugItem _headOffset_pos = null;
        //private DebugItem _handOffset_pos = null;

        private DebugItem _lefthand_pos = null;
        private DebugItem _righthand_pos = null;
        private DebugItem _leftfoot_pos = null;
        private DebugItem _rightfoot_pos = null;
        private DebugItem _waist_pos = null;
        private DebugItem _head_pos = null;

        private DebugItem _head_line1 = null;
        private DebugItem _head_line2 = null;

        public void Update()
        {
            if (!SHOULD_DRAW)
                return;

            if (_renderer == null)
                _renderer = new DebugRenderer3D();

            // --------------- room ---------------

            // These are not the player's feet, but the centerpoint of the floor of the living room in game (the player can walk around this point)
            UpdateDot(ref _player_pos, Player.local.transform.position, Color.white);

            // Waist height, but above transform instead of tied to the player
            UpdateDot(ref _waist_pos, Player.local.waist.ikAnchor.position, UtilityColor.FromHex("DBA746"));

            // I'm guessing these are configured offsets - zero by default
            //UpdateDot(ref _globalOffset_pos, Player.local.globalOffsetTransform.position, Color.blue);      // this is the same as transform
            //UpdateDot(ref _headOffset_pos, Player.local.headOffsetTransform.position, Color.green);
            //UpdateDot(ref _handOffset_pos, Player.local.handOffsetTransform.position, Color.red);

            // Both of these were (0.26, 0.96, 1.67)
            //Debug.Log($"headOffsetTransform: {Player.local.headOffsetTransform?.position.ToString() ?? "null"}");
            //Debug.Log($"handOffsetTransform: {Player.local.handOffsetTransform?.position.ToString() ?? "null"}");


            // --------------- player ---------------

            // These stay with the player when in 3rd person mode
            // When the player rotates around, these rotate with

            // These two are active during the character selection scene.  The others (feet, waist, head) are probably null
            UpdateDot(ref _lefthand_pos, Player.local.handLeft.root.position, Color.red);
            UpdateDot(ref _righthand_pos, Player.local.handRight.root.position, Color.green);

            // These two appear to be the same point (center of where the feet are)
            // The legs animate, but this stays stable
            // Maybe they would be different if wearing trackers
            UpdateDot(ref _leftfoot_pos, Player.local.footLeft.ragdollFoot.root.position, UtilityColor.FromHex("B14A47"));
            UpdateDot(ref _rightfoot_pos, Player.local.footRight.ragdollFoot.root.position, UtilityColor.FromHex("59FF7D"));

            //UpdateDot(ref _head_pos, Player.local.head.anchor.position, UtilityColor.FromHex("C0CCD9"));      // this would block the view, using lines instead
            UpdateLine(ref _head_line1, Player.local.handLeft.root.position, Player.local.head.anchor.position, UtilityColor.FromHex("C0CCD9"));
            UpdateLine(ref _head_line2, Player.local.handRight.root.position, Player.local.head.anchor.position, UtilityColor.FromHex("C0CCD9"));
        }

        private void UpdateDot(ref DebugItem item, Vector3 pos, Color color)
        {
            if (item != null && item.Object == null)
                item = null;

            if (item == null)
                item = _renderer.AddDot(pos, 0.15f, color);
            else
                item.Object.transform.position = pos;
        }
        private void UpdateLine(ref DebugItem item, Vector3 pos1, Vector3 pos2, Color color)
        {
            if (item != null && item.Object == null)
                item = null;

            if (item == null)
                item = _renderer.AddLine_Basic(pos1, pos2, 0.02f, color);
            else
                DebugRenderer3D.AdjustLinePositions(item, pos1, pos2);
        }
    }
}
