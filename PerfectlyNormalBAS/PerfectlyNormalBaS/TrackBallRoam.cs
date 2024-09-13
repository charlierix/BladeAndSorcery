using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalUnity
{
    //TODO: Double tap shift key to toggle between orbit and 1st person look (reset the shift first tap if they click the mouse in between to reduce the chance of toggling while they are doing standard shift+right drag)
    //TODO: Let them assign constraints, something like MaxRadius_XZ, MaxRadius_Y
    //TODO: Implement inertia

    /// <summary>
    /// This allows the user to move the camera around with left/right/middle drag, wheel mouse, keyboard.
    /// Attach this script to a camera
    /// </summary>
    /// <remarks>
    /// Attach this script to your camera
    /// </remarks>
    public class TrackBallRoam : MonoBehaviour
    {
        private const float MAXDELTA = 1f / 12f;        // lag can cause the view to jump

        public float MouseSensitivity_Pan = 240f;
        public float MouseSensitivity_Orbit = 360f;
        public float MouseSensitivity_Wheel = 120f;

        public float OrbitRadius = 6;
        public float MaxOrbitRadius = 144f;

        public float MinAngle_Y = -90f;
        public float MaxAngle_Y = 90f;

        private float _eulerX;
        private float _eulerY;

        private bool _isLookAtPointSet = false;
        private Vector3 _lookAtPoint = new Vector3();

        void Start()
        {
            Vector3 angles = transform.eulerAngles;
            _eulerX = angles.y;     // euler x rotates around y axis
            _eulerY = angles.x;

            if (_eulerY < MinAngle_Y)
                _eulerY += 360f;
            else if (_eulerY > MaxAngle_Y)
                _eulerY -= 360f;        // this class runs -90 to 90, but on startup, the value is 358.  So turn that into -2
        }

        void Update()
        {
            // Right Drag: Orbit
            // Shift + Right Drag : Orbit with ray for radius

            // Middle Drag: Pan

            // Wheel: Zoom

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            float mouseWheel = Input.mouseScrollDelta.y * MouseSensitivity_Wheel;

            bool isRightDown = Input.GetMouseButton(1);
            bool isMiddleDown = Input.GetMouseButton(2);

            bool isShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            float deltaTime = Math.Min(Time.deltaTime, MAXDELTA);      

            // Mouse wheel will act directly on position, it won't affect the velocity
            if (!mouseWheel.IsNearZero())
            {
                transform.position += transform.forward * (mouseWheel * deltaTime);
            }

            if (isMiddleDown)
            {
                transform.position -=
                    (transform.up * (mouseY * MouseSensitivity_Pan * deltaTime)) +
                    (transform.right * (mouseX * MouseSensitivity_Pan * deltaTime));
            }

            OrbitCamera(isRightDown, isShiftDown, mouseX, mouseY);
        }

        /// <summary>
        /// Orbits around a point that is OrbitRadius away.  If they are holding in shift, it will
        /// fire a cone ray and orbit around what they are looking at
        /// </summary>
        /// <remarks>
        /// Got this here
        /// http://wiki.unity3d.com/index.php?title=MouseOrbitImproved#Code_C.23
        /// </remarks>
        private void OrbitCamera(bool isRightDown, bool isShiftDown, float mouseX, float mouseY)
        {
            if (isRightDown)
            {
                float deltaTime = Math.Min(Time.deltaTime, MAXDELTA);

                _eulerX += mouseX * MouseSensitivity_Orbit * deltaTime;     // the example multiplies this by orbit radius, but not Y
                _eulerY -= mouseY * MouseSensitivity_Orbit * deltaTime;

                _eulerY = UtilityMath.ClampAngle(_eulerY, MinAngle_Y, MaxAngle_Y);

                Quaternion rotation = Quaternion.Euler(_eulerY, _eulerX, 0);

                Vector3 lookAtPoint;
                float radius;
                if (_isLookAtPointSet)
                {
                    lookAtPoint = _lookAtPoint;
                    radius = (transform.position - lookAtPoint).magnitude;
                }
                else
                {
                    lookAtPoint = transform.position + (transform.forward * OrbitRadius);
                    radius = OrbitRadius;
                }

                if (isShiftDown && !_isLookAtPointSet)
                {
                    Ray lookRay = new Ray(transform.position, transform.forward);

                    var coneHits = UtilityUnity.ConeCastAll(lookRay, radius, MaxOrbitRadius, 12).
                        Select(o => new
                        {
                            hit = o,
                            //intersect = Math3D.GetClosestPoint_Line_Point(lookRay, o.point),        // this finds the closest point on the look ray, perpendicular to the look ray
                            intersect = Math3D.GetIntersection_Plane_Line(new Plane(lookRay.origin - o.point, o.point), lookRay),      // this finds a point on the look ray, perpendicular to the cone match ray (using this because it gives a better indication of what is "closest".  something that is closer to the camera, but higher angle from the look ray could project to be farther away than something that is lower angle, but slightly farther from the camera)
                        }).
                        Where(o => o.intersect != null).       // it should never be null
                        Select(o => new
                        {
                            o.hit,
                            intersect = o.intersect.Value,
                            distance = (o.intersect.Value - lookRay.origin).sqrMagnitude,
                        }).
                        ToArray();

                    if (coneHits.Length > 0)
                    {
                        var closest = coneHits.
                            OrderBy(o => o.distance).
                            First();

                        radius = (float)Math.Sqrt(closest.distance);
                        lookAtPoint = closest.intersect;
                        _lookAtPoint = lookAtPoint;
                        _isLookAtPointSet = true;
                    }
                }

                Vector3 negRadius = new Vector3(0.0f, 0.0f, -radius);
                Vector3 position = rotation * negRadius + lookAtPoint;

                transform.rotation = rotation;
                transform.position = position;
            }
            else
            {
                _isLookAtPointSet = false;
            }
        }
    }
}
