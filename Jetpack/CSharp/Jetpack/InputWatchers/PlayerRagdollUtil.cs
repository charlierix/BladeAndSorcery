using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.InputWatchers
{
    public class PlayerRagdollUtil
    {
        private readonly List<Vector3> _ragdollforwards = new List<Vector3>();
        internal readonly RagdollPart.Type[] _spine_parts = new[]       // made internal so they can be debug drawn
        {
            //RagdollPart.Type.Tail,        // this also points down a bit
            RagdollPart.Type.Torso,     // I think there are two parts labeled as torso
            //RagdollPart.Type.Neck,        // this one points downward a little
        };

        // Returns average of the spine part's forward.  This works fairly well, but fails when one hand is in front of the
        // player and the other is behind.  The spine kind of takes the average
        public Vector3 GetRagdollForward()
        {
            //Vector3 forward = Player.local.transform.forward;     // relative to room, irl turning will move this around
            //Vector3 forward = Player.local.waist.ikAnchor.forward;      // same as prev

            var ragdoll = Player.currentCreature.ragdoll;

            _ragdollforwards.Clear();

            foreach (var part in ragdoll.parts)
                if (part.type.In(_spine_parts))
                    _ragdollforwards.Add(part.transform.forward);

            return Math3D.GetAverage(_ragdollforwards);
        }
    }
}
