using Jetpack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jetpack.DebugCode
{
    public class ScaleAdjuster
    {
        private static Lazy<ScaleAdjuster> _instance = new Lazy<ScaleAdjuster>(() => new ScaleAdjuster());

        private readonly object _lock = new object();

        private ScaleData _origScale = null;

        private bool _isScaleApplied = false;


        // The points in VisualizePlayerPoints are accurate according to any scale, but the avatar visual (ragdoll) doesn't properly sync

        // The avatar body (ragdoll) changes when on the ground vs in air

        // At scales under 25% or so, the player's feet clip under terrain, blocking the ability to walk

        // When scale is really small, it would be best to make the ragdoll invisible and use custom body graphics


        public static void ApplyScale(float scale, bool set_morphology = true, bool set_ragdoll = false)
        {
            if (Player.local?.transform == null || Player.local?.creature?.morphology == null)
                return;

            ScaleAdjuster instance = _instance.Value;
            lock (instance._lock)
            {
                instance.ApplyScale_private(scale, set_morphology, set_ragdoll);
            }
        }
        public static void RevertScale()
        {
            if (Player.local?.transform == null || Player.local?.creature?.morphology == null)
                return;

            ScaleAdjuster instance = _instance.Value;
            lock (instance._lock)
            {
                instance.RevertScale_private();
            }
        }

        private void ApplyScale_private(float scale, bool set_morphology, bool set_ragdoll)
        {
            scale = Mathf.Clamp(scale, 0.05f, 8);

            if (_isScaleApplied)
                RevertScale_private();

            if (_origScale == null)
                _origScale = new ScaleData()
                {
                    Morphology = Player.local.creature.morphology.Clone(),
                };



            //Player.local.creature.SetHeight
            //Player.characterData;
            //Player.characterData.calibration.height


            // This is the parent to most of the player
            Player.local.transform.localScale = new Vector3(scale, scale, scale);

            // This seems to be neccessary
            if (set_morphology)
                Player.local.creature.morphology = new Morphology(_origScale.Morphology.eyesHeight * scale)
                {
                    eyesHeight = _origScale.Morphology.eyesHeight * scale,
                    eyesForward = _origScale.Morphology.eyesForward * scale,
                    headHeight = _origScale.Morphology.headHeight * scale,
                    headForward = _origScale.Morphology.headForward * scale,
                    chestHeight = _origScale.Morphology.chestHeight * scale,
                    spineHeight = _origScale.Morphology.spineHeight * scale,
                    hipsHeight = _origScale.Morphology.hipsHeight * scale,
                    armsSpacing = _origScale.Morphology.armsSpacing * scale,
                    armsLength = _origScale.Morphology.armsLength * scale,
                    armsHeight = _origScale.Morphology.armsHeight * scale,
                    armsToEyesHeight = _origScale.Morphology.armsToEyesHeight * scale,
                    height = _origScale.Morphology.height * scale,
                    legsLength = _origScale.Morphology.legsLength * scale,
                    legsSpacing = _origScale.Morphology.legsSpacing * scale,
                    upperLegsHeight = _origScale.Morphology.upperLegsHeight * scale,
                    lowerLegsHeight = _origScale.Morphology.lowerLegsHeight * scale,
                    footHeight = _origScale.Morphology.footHeight * scale,
                };

            // The ragdoll is already smaller based on the root transform's scale, but the body is still way
            // above and hands way in front when scale is tiny.  It only seems to be visual though, the collider
            // is probably around feet to head
            //
            // So setting this just double applies scale to the body visual
            if (set_ragdoll)
                Player.local.creature.ragdoll.transform.localScale = new Vector3(scale, scale, scale);

            _isScaleApplied = true;
        }
        private void RevertScale_private()
        {
            Player.local.transform.localScale = new Vector3(1, 1, 1);

            Player.local.creature.ragdoll.transform.localScale = new Vector3(1, 1, 1);

            if (_origScale != null)
                Player.local.creature.morphology = _origScale.Morphology;

            _isScaleApplied = false;
        }

        #region OLD

        //Height = Player.local.creature.GetHeight(),
        //Morphology = Player.local.creature.morphology.Clone(),
        //HeadLocalPosition = Player.local.headOffsetTransform.localPosition,     // this is zero

        /*
        "Height": 1.895983338356018,
        "Morphology": {{
            "eyesHeight": 1.9534728527069092,
            "eyesForward": 0.11809086799621582,
            "headHeight": 1.8641363382339478,
            "headForward": 0.031885743141174319,
            "chestHeight": 1.2766860723495484,
            "spineHeight": 1.0798486471176148,
            "hipsHeight": 1.0564287900924683,
            "armsSpacing": 0.36119115352630618,
            "armsLength": 0.6440020799636841,
            "armsHeight": 1.6500314474105836,
            "armsToEyesHeight": 0.0,
            "height": 2.0837044715881349,
            "legsLength": 1.01776123046875,
            "legsSpacing": 0.2640935182571411,
            "upperLegsHeight": 1.1216603517532349,
            "lowerLegsHeight": 0.5887104272842407,
            "footHeight": 0.1138991191983223
        }},
        "HeadLocalPosition": {{
            "x": 0.0,
            "y": 0.0,
            "z": 0.0
        }}
        */



        /*

        private void ApplyScale()
        {
            float scale = Mathf.Clamp(playerScale / 100f, 0.05f, 1);
            Player.local.transform.localScale = new Vector3(scale, scale, scale);
            Player.local.headOffsetTransform.localScale = new Vector3(scale, scale, scale);     // this helps, but the head slowly drifts forward

            //Player.local.handOffsetTransform.localScale = new Vector3(scale, scale, scale);       // transform and headOffsetTransform are pretty good, but handOffsetTransform makes the hands extra large as the scale gets small



            //Player.local.headOffsetTransform.localPosition =      // maybe


            Player.local.creature.morphology = new Morphology(_old.Morphology.eyesHeight * scale)
            {
                eyesHeight = _old.Morphology.eyesHeight * scale,
                eyesForward = _old.Morphology.eyesForward * scale,
                headHeight = _old.Morphology.headHeight * scale,
                headForward = _old.Morphology.headForward * scale,
                chestHeight = _old.Morphology.chestHeight * scale,
                spineHeight = _old.Morphology.spineHeight * scale,
                hipsHeight = _old.Morphology.hipsHeight * scale,
                armsSpacing = _old.Morphology.armsSpacing * scale,
                armsLength = _old.Morphology.armsLength * scale,
                armsHeight = _old.Morphology.armsHeight * scale,
                armsToEyesHeight = _old.Morphology.armsToEyesHeight * scale,
                height = _old.Morphology.height * scale,
                legsLength = _old.Morphology.legsLength * scale,
                legsSpacing = _old.Morphology.legsSpacing * scale,
                upperLegsHeight = _old.Morphology.upperLegsHeight * scale,
                lowerLegsHeight = _old.Morphology.lowerLegsHeight * scale,
                footHeight = _old.Morphology.footHeight * scale,
            };


            // I think the next step should be to look at the player model carefully.  See what all these transforms, creature, etc look like


            // This causes the hands to go nuts
            //Player.local.creature.SetHeight(_old.Height * scale);

        }
        private void RevertScale()
        {
            Player.local.transform.localScale = new Vector3(1, 1, 1);
            //Player.local.handOffsetTransform.localScale = new Vector3(1, 1, 1);
            Player.local.headOffsetTransform.localScale = new Vector3(1, 1, 1);

            //Player.local.creature.SetHeight(_old.Height);


            // TODO: _old.Morphology was set in activate flight, change this to some kind of startup event
            Player.local.creature.morphology = _old.Morphology;
        }

        */

        #endregion
    }
}
