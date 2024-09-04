using ThunderRoad;
using UnityEngine;

namespace Jetpack.Models
{
    [System.Serializable]
    public class FlightData
    {
        // JsonUtility doesn’t serialize properties, only fields that are either public or have the SerializeField attribute

        public float Drag;
        public float Mass;
        public float HorizontalSpeed;
        public float VerticalSpeed;
        public float MaxAngle;
        public bool FallDamage;
        public bool CrouchOnJump;
        public bool StickJump;

        public float Height;
        public Morphology Morphology;
        public Vector3 HeadLocalPosition;
    }
}