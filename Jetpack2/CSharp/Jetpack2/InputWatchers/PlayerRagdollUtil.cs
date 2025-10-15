using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.InputWatchers
{
    // TODO: instead of relying on in game ragdoll, make a custom one that is just torso and gives the
    // hips some momentum.  Also tell it when rotations are being applied by this mod so that the whole
    // ik body can be rotated

    public class PlayerRagdollUtil
    {
        private readonly List<Vector3> _ragdoll_forwards = new List<Vector3>();
        private readonly List<Vector3> _ragdoll_ups = new List<Vector3>();
        internal readonly RagdollPart.Type[] _spine_parts = new[]       // made internal so they can be debug drawn
        {
            //RagdollPart.Type.Tail,        // this also points down a bit
            RagdollPart.Type.Torso,     // I think there are two parts labeled as torso
            //RagdollPart.Type.Neck,        // this one points downward a little
        };

        // Returns average of the spine part's forward.  This works fairly well, but fails when one hand is in front of the
        // player and the other is behind.  The spine kind of takes the average
        public (Vector3 forward, Vector3 up) GetRagdollForwardUp()
        {
            //Vector3 forward = Player.local.transform.forward;     // relative to room, irl turning will move this around
            //Vector3 forward = Player.local.waist.ikAnchor.forward;      // same as prev

            var ragdoll = Player.currentCreature.ragdoll;

            _ragdoll_forwards.Clear();
            _ragdoll_ups.Clear();

            foreach (var part in ragdoll.parts)
            {
                if (part.type.In(_spine_parts))
                {
                    _ragdoll_forwards.Add(part.transform.forward);
                    _ragdoll_ups.Add(Vector3.Cross(part.transform.forward, part.transform.up));        // this appears to be to the left instead of up, so doing a cross product
                }
            }

            Vector3 forward = Math3D.GetAverage(_ragdoll_forwards).normalized;
            Vector3 up = Math3D.GetAverage(_ragdoll_ups).normalized;

            return (forward, up);
        }
    }
}
