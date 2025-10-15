using ThunderRoad;
using UnityEngine;

namespace Jetpack2.FlightProcessing
{
    // this logic was copied from flip spell by jenix106
    public class PlayerRotator
    {
        private bool _isStored = false;
        private bool _allowMove;
        private bool _turnBodyByHeadAndHands;

        public void OnPlayerSpawned()
        {
            var local = Player.local;

            if (local == null)
                Debug.Log("OnPlayerSpawned local is null");

            // OnGroundEvent
            if (local?.locomotion == null)
                Debug.Log("OnPlayerSpawned local?.locomotion is null");
            else
                local.locomotion.OnGroundEvent += Locomotion_OnGroundEvent;

            // turnBodyByHeadAndHands
            if (local?.creature?.ragdoll?.ik?.turnBodyByHeadAndHands == null)
            {
                if (local?.creature == null)
                    Debug.Log("OnPlayerSpawned local?.creature is null");       // this is the one that's null.  so this mod is using player spawned, backflip is using spell's loaded
                else if (local?.creature?.ragdoll?.ik == null)
                    Debug.Log("OnPlayerSpawned local?.creature?.ragdoll?.ik is null");
                else
                    Debug.Log("OnPlayerSpawned local?.creature?.ragdoll?.ik?.turnBodyByHeadAndHands is null");

                _turnBodyByHeadAndHands = true;
            }
            else
            {
                _turnBodyByHeadAndHands = local.creature.ragdoll.ik.turnBodyByHeadAndHands;
                Debug.Log($"OnPlayerSpawned local.creature.ragdoll.ik.turnBodyByHeadAndHands: {local.creature.ragdoll.ik.turnBodyByHeadAndHands}");
            }

            // allowMove
            if(local?.locomotion?.allowMove == null)
            {
                Debug.Log("OnPlayerSpawned local?.locomotion?.allowMove is null");
                _allowMove = true;
            }
            else
            {
                _allowMove = local.locomotion.allowMove;
                Debug.Log($"OnPlayerSpawned local.locomotion.allowMove: {local.locomotion.allowMove}");
            }

            _isStored = true;
        }

        private void Locomotion_OnGroundEvent(Locomotion locomotion, Vector3 groundPoint, Vector3 velocity, Collider groundCollider)
        {
            Reset();
        }

        public void OnPlayerDespawned()
        {
            var loco = Player.local?.locomotion;

            if (loco != null)
                loco.OnGroundEvent += Locomotion_OnGroundEvent;

            Reset();
        }

        public void RotateAround(Vector3 point, Vector3 axis, float degrees_per_sec, float elapsedSeconds)
        {
            var local = Player.local;

            var headTransform = local.head.cam.transform;

            Quaternion rot = local.transform.rotation;

            local.transform.RotateAround(point, axis, degrees_per_sec * elapsedSeconds);

            local.autoAlign = false;
            local.creature.ragdoll.ik.turnBodyByHeadAndHands = false;
            local.creature.ragdoll.ik.AddLocomotionDeltaRotation(local.transform.rotation * Quaternion.Inverse(rot), local.creature.ragdoll.targetPart.transform.position);
        }

        /// <summary>
        /// Puts the player back to standard (use when going back to grounded)
        /// </summary>
        public void Reset()
        {
            var local = Player.local;

            if (local?.autoAlign != null)
                local.autoAlign = true;

            if (local?.creature?.ragdoll?.ik?.turnBodyByHeadAndHands != null)
                local.creature.ragdoll.ik.turnBodyByHeadAndHands = _isStored ? _turnBodyByHeadAndHands : true;

            if (local?.locomotion?.allowMove != null)
                local.locomotion.allowMove = _isStored ? _allowMove : true;
        }
    }
}
