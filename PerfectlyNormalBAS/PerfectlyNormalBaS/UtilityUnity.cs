using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalBaS
{
    public static class UtilityUnity
    {
        /// <summary>
        /// Use this to find colliders within a cone-shaped volume
        /// </summary>
        /// <remarks>
        /// Got this here
        /// https://github.com/walterellisfun/ConeCast/blob/master/ConeCastExtension.cs
        /// 
        /// It uses SphereCastAll, which is like a RayCast tube, but then it uses Vector3.Angle to filter
        /// out hitpoints according to a cone
        /// 
        /// Using it is very similar to using SphereCastAll
        /// </remarks>
        public static RaycastHit[] ConeCastAll(Ray axis, float maxRadius, float maxDistance, float coneAngle)
        {
            RaycastHit[] sphereCastHits = Physics.SphereCastAll(axis.origin - new Vector3(0, 0, maxRadius), maxRadius, axis.direction, maxDistance);
            List<RaycastHit> coneCastHitList = new List<RaycastHit>();

            if (sphereCastHits.Length > 0)
            {
                for (int i = 0; i < sphereCastHits.Length; i++)
                {
                    //sphereCastHits[i].collider.gameObject.GetComponent<Renderer>().material.color = new Color(1f, 1f, 1f);        // for debugging

                    Vector3 hitPoint = sphereCastHits[i].point;
                    Vector3 directionToHit = hitPoint - axis.origin;
                    float angleToHit = Vector3.Angle(axis.direction, directionToHit);

                    if (angleToHit < coneAngle)
                    {
                        coneCastHitList.Add(sphereCastHits[i]);
                    }
                }
            }

            return coneCastHitList.ToArray();
        }
    }
}
