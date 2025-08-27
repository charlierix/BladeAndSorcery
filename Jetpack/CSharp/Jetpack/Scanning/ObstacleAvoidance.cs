using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.Scanning
{
    // This will focus on not running into walls, trees, etc

    public class ObstacleAvoidance
    {
        #region class: EllipsePoints

        private class EllipsePoints
        {
            public float Angle { get; set; }
            public float AlongMajor { get; set; }
            public float AlongMinor { get; set; }
        }

        #endregion

        private readonly RayCastStorage _raycast_storage;

        private EllipsePoints _ellipsePoints = null;

        #region debug drawing vars

        private const bool SHOULD_DRAW = true;

        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private DebugRenderer3D _renderer = null;

        private (DebugItem point, DebugItem line)[] _viz_ellipse = null;

        #endregion

        // probably split this into two classes.  the low flying v class (repelground) should be generalized for ground or wall


        // if about to run into something along vel, do a slight push to avoid
        //  this will be new behavior

        // if about to brush along a wall, do a slight push away from the wall
        //  this will be a modification of low flying v

        public ObstacleAvoidance(RayCastStorage raycast_storage)
        {
            _raycast_storage = raycast_storage;
        }

        public void Update_CastRays()
        {
            Vector3 velocity = Player.local.locomotion.physicBody.velocity;
            if (velocity.IsNearZero())
                return;

            // Define points on an ellipse around the player
            var ellipse_points = GetEllipsePoints(velocity);

            if (SHOULD_DRAW)
                DrawEllipsePointsLines(ellipse_points.origin, ellipse_points.perimiter, velocity);






        }
        public Vector3? Update_Finish()
        {
            return null;
        }

        public void Clear()
        {
            ClearDebugVisuals();
        }

        #region Private Methods - Debug Drawing

        private void EnsureDebugActive()
        {
            if (_renderer == null)
                _renderer = DebugRenderer3D.GetOrAddDebugRenderer3D();
        }

        private void ClearDebugVisuals()
        {
            if (_renderer == null)
                return;

            if (_viz_ellipse != null)
            {
                for (int i = 0; i < _viz_ellipse.Length; i++)
                {
                    if (_viz_ellipse[i].point != null)
                        _renderer.Remove(_viz_ellipse[i].point);

                    if (_viz_ellipse[i].line != null)
                        _renderer.Remove(_viz_ellipse[i].line);
                }

                _viz_ellipse = null;
            }

            _renderer = null;
        }

        private void DrawEllipsePointsLines(Vector3 origin, Vector3[] perimiter, Vector3 velocity)
        {
            EnsureDebugActive();

            if (_viz_ellipse == null)
            {
                _viz_ellipse = new (DebugItem point, DebugItem line)[perimiter.Length];

                for (int i = 0; i < perimiter.Length; i++)
                    _viz_ellipse[i] = (_renderer.AddDot(origin, DOT_SIZE, Color.yellow), _renderer.AddLine_Basic(origin, origin + velocity, LINE_THICKNESS, Color.yellow));
            }

            for (int i = 0; i < perimiter.Length; i++)
            {
                Vector3 pos = perimiter[i];

                _viz_ellipse[i].point.Object.transform.position = pos;
                DebugRenderer3D.AdjustLinePositions(_viz_ellipse[i].line, pos, pos + velocity);
            }
        }

        #endregion
        #region Private Methods

        private (Vector3[] perimiter, Vector3 origin) GetEllipsePoints(Vector3 velocity)
        {
            const float WIDTH_PERCENT_OF_HEIGHT = 0.25f;
            const float HEIGHT_EXPAND_PERCENT = 1.1f;

            // Top and Bottom
            Vector3 head_pos = Player.local.head.anchor.position;
            Vector3 foot_pos = Math3D.GetAverage(Player.local.footLeft.ragdollFoot.root.position, Player.local.footRight.ragdollFoot.root.position);        // Player.local.transform.position is the room level origin

            float height = (head_pos - foot_pos).magnitude;
            Vector3 major_axis_dir = (head_pos - foot_pos).normalized;      // could divide by height, but normalized feels safer
            Vector3 minor_axis_dir = Vector3.Cross(velocity, major_axis_dir).normalized;

            // Expand height so head and foot are above and below sensor points
            height *= HEIGHT_EXPAND_PERCENT;

            // Origin
            Vector3 mid_pos = foot_pos + ((head_pos - foot_pos) * 0.5f);

            // Left and Right
            float minor_axis_half_width = height * WIDTH_PERCENT_OF_HEIGHT * 0.5f;

            Vector3 right_pos = mid_pos + (minor_axis_dir * minor_axis_half_width);
            Vector3 left_pos = mid_pos - (minor_axis_dir * minor_axis_half_width);

            // Four points between major/minor axiis
            if (_ellipsePoints == null || !_ellipsePoints.Angle.IsNearValue(JetpackScript.EllipsePointAngle))
                _ellipsePoints = GetEllipsePoints(JetpackScript.EllipsePointAngle);

            Vector3 along_major = major_axis_dir * (height / 2 * _ellipsePoints.AlongMajor);
            Vector3 along_minor = minor_axis_dir * (minor_axis_half_width * _ellipsePoints.AlongMinor);

            // Return
            Vector3[] perimiter = new[]
            {
                mid_pos + major_axis_dir * (height * 0.5f),
                mid_pos + along_minor + along_major,
                right_pos,
                mid_pos + along_minor - along_major,
                mid_pos - major_axis_dir * (height * 0.5f),
                mid_pos - along_minor - along_major,
                left_pos,
                mid_pos - along_minor + along_major,
            };

            return (perimiter, mid_pos);
        }

        // TODO: instead of returning a single vector, return two lengths (along major and minor axiis)
        // TODO: rework so the cosine and sine are calculated according to angle, then cached
        private static Vector3 GetPointOnEllipse(Vector3 majoraxis_dir, Vector3 minoraxis_dir, float majoraxis_len, float minoraxis_len, float angle_degrees)
        {
            // Convert angle to radians
            float radians = angle_degrees * Mathf.Deg2Rad;

            // Compute the cosine and sine components
            float cosTheta = Mathf.Cos(radians);
            float sinTheta = Mathf.Sin(radians);

            // Compute the displacement from the ellipse center
            Vector3 displacement =
                majoraxis_dir * (majoraxis_len / 2 * cosTheta) +
                minoraxis_dir * (minoraxis_len / 2 * sinTheta);

            return displacement;
        }

        private static EllipsePoints GetEllipsePoints(float angle_degrees)
        {
            float radians = angle_degrees * Mathf.Deg2Rad;

            float cosTheta = Mathf.Cos(radians);
            float sinTheta = Mathf.Sin(radians);

            return new EllipsePoints()
            {
                Angle = angle_degrees,
                AlongMajor = cosTheta,
                AlongMinor = sinTheta,
            };
        }

        #endregion
    }
}
