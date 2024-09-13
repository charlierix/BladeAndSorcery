using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace PerfectlyNormalUnity.IK
{
    public class IK_SingleChain_LooseRoot : MonoBehaviour
    {
#region Declaration Section

        //  root                                                       target
        //  (bone0) (boneLen 0) (bone1) (boneLen 1) (bone2)   ...     (boneN)
        //   x--------------------x--------------------x----- ... -------x

        /// <summary>
        /// Number of arms in the chain (the number of bone joints will be this + 1)
        /// </summary>
        public int ChainCount = 2;

        /// <summary>
        /// Target that the chain should bend to
        /// </summary>
        public Transform Target;

        /// <summary>
        /// This will pull all intermediate bones toward this point
        /// </summary>
        /// <remarks>
        /// This is a good cheap way to avoid joint constraints, but have a point that tells this what direction
        /// to bend
        /// 
        /// NOTE: This doesn't perform very well in the loose anchor variant.  Leaving it in just in case it's
        /// ever useful
        /// </remarks>
        public Transform PullToward;

        /// <summary>
        /// Solver iterations per update
        /// </summary>
        [Header("Solver Parameters")]
        public int Iterations = 12;

        /// <summary>
        /// If leaf is this close to target, iterations stop early
        /// </summary>
        public float Epsilon = .001f;

        private float[] _boneLengths = null;        // the length of each bone (target to origin)
        private float _completeLength = -1f;        // this is the sum of the bone's lengths
        private Transform[] _bones = null;      // element 0 is the root bone
        private Vector3[] _positions = null;

        private Vector3[] _startDirections;     // the initial direction from bone to bone
        private Quaternion[] _startRotations;       // the initial rotation of each bone relative to root
        private Quaternion _startRotationTarget;        // the initial rotation of the target relative to root

        /// <summary>
        /// This is the object that is the parent of the first bone
        /// </summary>
        private Transform _root = null;

#endregion

        private void LateUpdate()
        {
            ResolveIK();
        }

        void OnDrawGizmos()
        {
            var current = transform;

            for (int cntr = 0; cntr < ChainCount && current != null && current.parent != null; cntr++)
            {
                Vector3 direction = current.parent.position - current.position;

                float scale = direction.magnitude * .1f;

#if UNITY_EDITOR
                Handles.matrix = Matrix4x4.TRS(current.position, Quaternion.FromToRotation(Vector3.up, direction), new Vector3(scale, direction.magnitude, scale));
                Handles.color = Color.Lerp(Color.yellow, Color.green, (float)cntr / (float)(ChainCount - 1));
                Handles.DrawWireCube(Vector3.up * .5f, Vector3.one);
#endif

                current = current.parent;
            }
        }

#region Private Methods

        private void Init()
        {
            // Find root (parent of the first bone)
            _root = FindRoot(transform, ChainCount);

            // Make sure there is a target
            if (Target == null)
            {
                Target = new GameObject(gameObject.name + " Target").transform;
                SetPositionRootSpace(Target, GetPositionRootSpace(transform, _root), _root);
            }

            // Create arrays
            _bones = new Transform[ChainCount + 1];
            _positions = new Vector3[ChainCount + 1];
            _boneLengths = new float[ChainCount];
            _startDirections = new Vector3[ChainCount + 1];
            _startRotations = new Quaternion[ChainCount + 1];

            // Store positions
            _startRotationTarget = GetRotationRootSpace(Target, _root);

            _completeLength = 0;
            var current = transform;

            for (int cntr = _bones.Length - 1; cntr >= 0; cntr--)
            {
                _bones[cntr] = current;
                _startRotations[cntr] = GetRotationRootSpace(current, _root);

                if (cntr == _bones.Length - 1)
                {
                    // Leaf
                    _startDirections[cntr] = GetPositionRootSpace(Target, _root) - GetPositionRootSpace(current, _root);
                }
                else
                {
                    // Mid Bone
                    _startDirections[cntr] = GetPositionRootSpace(_bones[cntr + 1], _root) - GetPositionRootSpace(current, _root);

                    _boneLengths[cntr] = Vector3.Distance(_bones[cntr + 1].position, _bones[cntr].position);

                    _completeLength += _boneLengths[cntr];
                }

                current = current.parent;
            }
        }

        private void ResolveIK()
        {
            if (Target == null)
                return;

            if (_bones == null || _boneLengths == null || _boneLengths.Length != ChainCount)
                Init();

            // Load initial positions
            for (int cntr = 0; cntr < _bones.Length; cntr++)
            {
                _positions[cntr] = GetPositionRootSpace(_bones[cntr], _root);
            }

            var targetPosition = GetPositionRootSpace(Target, _root);
            var targetRotation = GetRotationRootSpace(Target, _root);

            // Calculate new positions
            ResolveIK_Inside(targetPosition, targetRotation);       // since this class moves the root, there is only "inside".  See IK_SingleChain_AnchoredRoot for the alternate

            PullToPole();

            // Commit the positions
            // TODO: Apply forces instead of instantly snapping to those positions
            for (int cntr = 0; cntr < _bones.Length; cntr++)
            {
                SetPositionRootSpace(_bones[cntr], _positions[cntr], _root);

                if (cntr == _positions.Length - 1)
                {
                    SetRotationRootSpace(_bones[cntr], Quaternion.Inverse(targetRotation) * _startRotationTarget * Quaternion.Inverse(_startRotations[cntr]), _root);
                }
                else
                {
                    SetRotationRootSpace(_bones[cntr], Quaternion.FromToRotation(_startDirections[cntr], _positions[cntr + 1] - _positions[cntr]) * Quaternion.Inverse(_startRotations[cntr]), _root);
                }
            }
        }

        private void ResolveIK_Inside(Vector3 targetPosition, Quaternion targetRotation)
        {
            for (int cntr = 0; cntr < Iterations; cntr++)
            {
                if ((_positions[_positions.Length - 1] - targetPosition).sqrMagnitude <= Epsilon * Epsilon)
                    return;

                ResolveIK_Inside_Backward(targetPosition);

                ResolveIK_Inside_Forward();
            }
        }

        /// <summary>
        /// This walks from the leaf through to the root (root will be moved)
        /// </summary>
        /// <remarks>
        /// After this function runs, the bone between the leaf and it's parent will be the proper length, but all other
        /// parent bones are pushed/pulled to the wrong length
        /// </remarks>
        private void ResolveIK_Inside_Backward(Vector3 targetPosition)
        {
            //for (int cntr = _positions.Length - 1; cntr > 0; cntr--)      // when stopping at one, it leave root alone
            for (int cntr = _positions.Length - 1; cntr >= 0; cntr--)       // when going to zero, it pulls the root along
            {
                if (cntr == _positions.Length - 1)
                {
                    _positions[cntr] = targetPosition;
                }
                else
                {
                    _positions[cntr] = _positions[cntr + 1] + ((_positions[cntr] - _positions[cntr + 1]).normalized * _boneLengths[cntr]);
                }
            }
        }

        /// <summary>
        /// This walks from root to leaf
        /// </summary>
        /// <remarks>
        /// This is a copy of the backward, but going the other direction
        /// 
        /// The point of this is to enforce bone lengths
        /// </remarks>
        private void ResolveIK_Inside_Forward()
        {
            for (int cntr = 1; cntr < _positions.Length; cntr++)
            {
                _positions[cntr] = _positions[cntr - 1] + ((_positions[cntr] - _positions[cntr - 1]).normalized * _boneLengths[cntr - 1]);
            }
        }

        private void PullToPole()
        {
            if (PullToward == null)
                return;

            Vector3 polePosition = GetPositionRootSpace(PullToward, _root);

            for (int cntr = 1; cntr < _positions.Length - 1; cntr++)
            {
                var plane = new Plane(_positions[cntr + 1] - _positions[cntr - 1], _positions[cntr - 1]);
                var projectedPole = plane.ClosestPointOnPlane(polePosition);
                var projectedBone = plane.ClosestPointOnPlane(_positions[cntr]);
                var angle = Vector3.SignedAngle(projectedBone - _positions[cntr - 1], projectedPole - _positions[cntr - 1], plane.normal);
                _positions[cntr] = Quaternion.AngleAxis(angle, plane.normal) * (_positions[cntr] - _positions[cntr - 1]) + _positions[cntr - 1];
            }
        }

        private static Transform FindRoot(Transform current, int chainCount)
        {
            Transform retVal = current;

            for (var cntr = 0; cntr <= chainCount; cntr++)
            {
                if (retVal == null)
                    return null;

                retVal = retVal.parent;
            }

            return retVal;
        }

        private static Vector3 GetPositionRootSpace(Transform current, Transform root)
        {
            if (root == null)
                return current.position;
            else
                return Quaternion.Inverse(root.rotation) * (current.position - root.position);
        }
        private static void SetPositionRootSpace(Transform current, Vector3 position, Transform root)
        {
            if (root == null)
                current.position = position;
            else
                current.position = root.rotation * position + root.position;
        }

        private static Quaternion GetRotationRootSpace(Transform current, Transform root)
        {
            // inverse(after) * before => rot: before -> after
            if (root == null)
                return current.rotation;
            else
                return Quaternion.Inverse(current.rotation) * root.rotation;
        }
        private static void SetRotationRootSpace(Transform current, Quaternion rotation, Transform root)
        {
            if (root == null)
                current.rotation = rotation;
            else
                current.rotation = root.rotation * rotation;
        }

#endregion
    }
}
