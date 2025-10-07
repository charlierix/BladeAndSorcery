using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using ThunderRoad.DebugViz;
using UnityEngine;

namespace Jetpack.FlightProcessing
{
    // this logic was copied from flip spell by jenix106
    public class PlayerRotator
    {
        private bool _allowMove;
        private bool _turnBodyByHeadAndHands;

        public void OnPlayerSpawned()
        {
            var local = Player.local;

            //_turnBodyByHeadAndHands = local.creature.ragdoll.ik.turnBodyByHeadAndHands;
            //_allowMove = local.locomotion.allowMove;
        }
        public void OnPlayerDespawned()
        {
            //var local = Player.local;

            //local.creature.ragdoll.ik.turnBodyByHeadAndHands = _turnBodyByHeadAndHands;
            //local.locomotion.allowMove = _allowMove;
        }

        public void RotateAround(Vector3 point, Vector3 axis, float degrees_per_sec, float elapsedSeconds)
        {
            var local = Player.local;

            //Quaternion rotation1 = local.transform.rotation;
            //Quaternion rotation2 = local.head.cam.transform.rotation;
            //rotation2.Set(0f, rotation2.y, 0f, 0f);



            // this may be improperly sending world axis to local transform
            local.transform.RotateAround(point, axis, degrees_per_sec * elapsedSeconds);



            //local.autoAlign = false;
            //local.creature.ragdoll.ik.turnBodyByHeadAndHands = false;
            //local.creature.ragdoll.ik.AddLocomotionDeltaRotation(local.transform.rotation * Quaternion.Inverse(rotation1), local.creature.ragdoll.targetPart.transform.position);
        }

        /// <summary>
        /// Puts the player back to standard (use when going back to grounded)
        /// </summary>
        public void Reset()
        {
            var local = Player.local;

            //local.creature.ragdoll.ik.turnBodyByHeadAndHands = _turnBodyByHeadAndHands;
            //local.locomotion.allowMove = _allowMove;

            if (local?.creature?.ragdoll?.ik?.turnBodyByHeadAndHands != null)
                local.creature.ragdoll.ik.turnBodyByHeadAndHands = true;

            if (local?.locomotion?.allowMove != null)
                local.locomotion.allowMove = true;
        }
    }
}
