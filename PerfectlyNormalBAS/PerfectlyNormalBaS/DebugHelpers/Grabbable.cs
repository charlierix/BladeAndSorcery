using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PerfectlyNormalUnity.DebugHelpers
{
    /// <summary>
    /// Attach this script to an object that you want the player to be able to drag around.  When the left mouse
    /// button is down, the object will be placed under the mouse cursor
    /// </summary>
    public class Grabbable : MonoBehaviour
    {
        private Camera _camera;

        private bool _isSelected = false;
        private Plane _clickPlane;

        private void Start()
        {
            _camera = Camera.main;
        }

        void Update()
        {
            // See if the left mouse is down
            if (!Input.GetMouseButton(0))
                return;

            // When they first hit the mouse down, define the click point to be along the camera look and intersecting the grab ball
            bool isFirstClick = Input.GetMouseButtonDown(0);
            if (isFirstClick)
                _clickPlane = new Plane(_camera.transform.forward, transform.position);

            Vector3 mousePos = Input.mousePosition;

            Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
            if (!screenRect.Contains(mousePos))     // this can happen when they start on screen and keep dragging off screen
                return;

            Ray ray = _camera.ScreenPointToRay(mousePos);

            // Move the grab object to where the mouse ray intersects the click plane
            Vector3? newPos = Math3D.GetIntersection_Plane_Ray(_clickPlane, ray);
            if (newPos == null)
                return;

            if (isFirstClick)
                _isSelected = (transform.position - newPos.Value).magnitude <= Math1D.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z) * 1.2f;

            if (!_isSelected)
                return;

            transform.position = newPos.Value;
        }
    }
}
