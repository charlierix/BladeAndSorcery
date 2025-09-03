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
        #region class: HitDetails

        private class HitDetails
        {
            public Vector3 Pos { get; set; }
            public Vector3 Normal { get; set; }     // hit's normal (may be negated from the actual surface's normal so that it's always facing the velocity)

            public Vector3 Accel { get; set; }
            public Vector3 TowardCenter_Dir { get; set; }

            public float dist_reduce_percent { get; set; }
            public float speed_reduce_percent { get; set; }
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

        private List<DebugItem> _ray_lines = new List<DebugItem>();
        private List<DebugItem> _ray_hit_points = new List<DebugItem>();
        private List<DebugItem> _ray_hit_normals = new List<DebugItem>();

        private List<DebugItem> _hitanal_pos = new List<DebugItem>();
        private List<DebugItem> _hitanal_accel = new List<DebugItem>();
        private List<(DebugItem orth, DebugItem back)> _hitanal_dirs = new List<(DebugItem, DebugItem)>();
        private List<DebugItem> _hitanal_reports = new List<DebugItem>();

        #endregion

        // These get reset each time Update_CastRays, and are then used by the corresponding call to GetGroundAccel
        private Vector3 _pos;       // this is the center of the body (hips)
        private Vector3 _velocity;
        private Vector3 _velocity_dir;
        private float _speed;
        private float _ray_len;

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
            _velocity = Player.local.locomotion.physicBody.velocity;
            if (_velocity.IsNearZero())
                return;

            _velocity_dir = _velocity.normalized;
            _speed = _velocity.magnitude;

            // Define points on an ellipse around the player
            var ellipse_points = GetEllipsePoints(_velocity);
            _pos = ellipse_points.origin;

            if (SHOULD_DRAW)
                DrawEllipsePointsLines(ellipse_points.origin, ellipse_points.perimiter, _velocity);

            // Define the rays
            // NOTE: this also has a ray coming out of origin
            var rays = GetEllipseRays(ellipse_points.origin, ellipse_points.perimiter, _velocity);

            _ray_len = _speed * JetpackScript.ObstAvoid_RayDistMult;

            CastAndStoreRays(rays, _velocity, _ray_len);
        }
        public Vector3? Update_Finish()
        {
            if (!JetpackScript.ShouldAvoidObstacles)
            {
                if (SHOULD_DRAW)
                    ClearDebugVisuals();
                return null;
            }

            if (_velocity.IsNearZero())
                return null;

            var rays = _raycast_storage.GetRayCasts(RayCastStorage.RayCategory.ObstacleAvoidance_BodyEllipse);

            if (rays == null || rays.Length == 0)
            {
                if (SHOULD_DRAW)
                {
                    Debug.Log("empty rays");
                    DrawRayCasts(new RayCastStorage.RayInfo[0], _ray_len);
                    DrawHitAnalysis(new HitDetails[0], _pos, _velocity_dir);
                }
                return null;
            }

            var hits = AnalyzeHits(rays[0].Rays, _ray_len, _pos, _velocity_dir, _speed);

            if (SHOULD_DRAW)
            {
                DrawRayCasts(rays[0].Rays, _ray_len);
                DrawHitAnalysis(hits, _pos, _velocity_dir);
            }






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

            foreach (var item in _ray_lines.
                Concat(_ray_hit_points).
                Concat(_ray_hit_normals).
                Concat(_hitanal_pos).
                Concat(_hitanal_accel).
                Concat(_hitanal_reports))
                _renderer.Remove(item);

            _ray_lines.Clear();
            _ray_hit_points.Clear();
            _ray_hit_normals.Clear();
            _hitanal_pos.Clear();
            _hitanal_accel.Clear();
            _hitanal_reports.Clear();

            foreach (var items in _hitanal_dirs)
            {
                _renderer.Remove(items.orth);
                _renderer.Remove(items.back);
            }

            _hitanal_dirs.Clear();

            _renderer = null;
        }

        private void DrawEllipsePointsLines(Vector3 origin, Vector3[] perimiter, Vector3 velocity)
        {
            EnsureDebugActive();

            if (_viz_ellipse == null)
            {
                _viz_ellipse = new (DebugItem point, DebugItem line)[perimiter.Length + 1];

                for (int i = 0; i < perimiter.Length; i++)
                    _viz_ellipse[i] = (_renderer.AddDot(origin, DOT_SIZE, Color.yellow), _renderer.AddLine_Basic(origin, origin + velocity, LINE_THICKNESS, Color.yellow));

                _viz_ellipse[_viz_ellipse.Length - 1] = (_renderer.AddDot(origin, DOT_SIZE, Color.yellow), _renderer.AddLine_Basic(origin, origin + velocity, LINE_THICKNESS, Color.yellow));
            }

            for (int i = 0; i < perimiter.Length; i++)
            {
                Vector3 pos = perimiter[i];

                _viz_ellipse[i].point.Object.transform.position = pos;
                DebugRenderer3D.AdjustLinePositions(_viz_ellipse[i].line, pos, pos + velocity);
            }

            _viz_ellipse[_viz_ellipse.Length - 1].point.Object.transform.position = origin;
            DebugRenderer3D.AdjustLinePositions(_viz_ellipse[_viz_ellipse.Length - 1].line, origin, origin + velocity);
        }

        private void DrawRayCasts(RayCastStorage.RayInfo[] rays, float ray_len)
        {
            EnsureDebugActive();

            int hit_index = -1;

            // Add/Update
            for (int i = 0; i < rays.Length; i++)
            {
                if (_ray_lines.Count <= i)
                    _ray_lines.Add(_renderer.AddLine_Basic(rays[i].Origin, rays[i].Origin + rays[i].Direction * ray_len, LINE_THICKNESS * 1.1f, Color.black));

                DebugRenderer3D.AdjustLinePositions(_ray_lines[i], rays[i].Origin, rays[i].Origin + rays[i].Direction * ray_len);
                DebugRenderer3D.AdjustColor(_ray_lines[i], rays[i].Hit != null ? UtilityColor.FromHex("47CF38") : UtilityColor.FromHex("CC3835"));

                if (rays[i].Hit != null)
                {
                    hit_index++;
                    if (_ray_hit_points.Count - 1 <= hit_index)
                        _ray_hit_points.Add(_renderer.AddDot(rays[i].Hit.Value.point, DOT_SIZE, UtilityColor.FromHex("1CB50B")));

                    _ray_hit_points[hit_index].Object.transform.position = rays[i].Hit.Value.point;

                    if (_ray_hit_normals.Count - 1 <= hit_index)
                        _ray_hit_normals.Add(_renderer.AddLine_Basic(rays[i].Hit.Value.point - rays[i].Hit.Value.normal, rays[i].Hit.Value.point + rays[i].Hit.Value.normal, LINE_THICKNESS, Color.blue));

                    DebugRenderer3D.AdjustLinePositions(_ray_hit_normals[hit_index], rays[i].Hit.Value.point - rays[i].Hit.Value.normal, rays[i].Hit.Value.point + rays[i].Hit.Value.normal);
                }
            }

            // Remove Extra
            while (_ray_lines.Count > rays.Length)
            {
                _renderer.Remove(_ray_lines[_ray_lines.Count - 1]);
                _ray_lines.RemoveAt(_ray_lines.Count - 1);
            }

            while (_ray_hit_points.Count > hit_index + 1)     // hit_index is still -1 if there are no hits
            {
                _renderer.Remove(_ray_hit_points[_ray_hit_points.Count - 1]);
                _ray_hit_points.RemoveAt(_ray_hit_points.Count - 1);
            }

            while (_ray_hit_normals.Count > hit_index + 1)
            {
                _renderer.Remove(_ray_hit_normals[_ray_hit_normals.Count - 1]);
                _ray_hit_normals.RemoveAt(_ray_hit_normals.Count - 1);
            }
        }

        private void DrawHitAnalysis(HitDetails[] hits, Vector3 origin, Vector3 velocity_dir)
        {
            EnsureDebugActive();

            Vector3 head_pos = Player.local.head.anchor.position;

            // Add/Update
            for (int i = 0; i < hits.Length; i++)
            {
                if (_hitanal_pos.Count <= i)
                    _hitanal_pos.Add(_renderer.AddDot(hits[i].Pos, DOT_SIZE, UtilityColor.FromHex("71518D")));

                _hitanal_pos[i].Object.transform.position = hits[i].Pos;

                if (_hitanal_accel.Count <= i)
                    _hitanal_accel.Add(_renderer.AddLine_Basic(hits[i].Pos, hits[i].Pos + hits[i].Accel, LINE_THICKNESS, UtilityColor.FromHex("71518D")));

                DebugRenderer3D.AdjustLinePositions(_hitanal_accel[i], hits[i].Pos, hits[i].Pos + hits[i].Accel);

                if (_hitanal_dirs.Count <= i)
                    _hitanal_dirs.Add((
                        _renderer.AddLine_Basic(hits[i].Pos, hits[i].Pos + hits[i].TowardCenter_Dir, LINE_THICKNESS, Color.white),
                        _renderer.AddLine_Basic(hits[i].Pos, hits[i].Pos - velocity_dir, LINE_THICKNESS, Color.white)));

                DebugRenderer3D.AdjustLinePositions(_hitanal_dirs[i].orth, hits[i].Pos, hits[i].Pos + hits[i].TowardCenter_Dir);
                DebugRenderer3D.AdjustLinePositions(_hitanal_dirs[i].back, hits[i].Pos, hits[i].Pos - velocity_dir);

                //Vector3 text_pos = hits[i].Pos - (hits[i].TowardCenter_Dir * 0.5f);       // can't put it offset to the hit, because it will often be behind terrain
                Vector3 text_pos = head_pos +
                    (hits[i].Pos - head_pos).normalized * 0.75f + // so go a fix amount away
                    hits[i].TowardCenter_Dir * -0.75f;

                string text = $"dist %: {hits[i].dist_reduce_percent.ToStringSignificantDigits(3)}{Environment.NewLine}speed %: {hits[i].speed_reduce_percent.ToStringSignificantDigits(3)}";

                if (_hitanal_reports.Count <= i)
                    _hitanal_reports.Add(_renderer.AddText(text, text_pos, (text_pos - head_pos).normalized, UtilityColor.FromHex("574963"), Color.white, TEXT_HEIGHT * 2));

                _hitanal_reports[i].Object.transform.position = text_pos;
                DebugRenderer3D.AdjustText(_hitanal_reports[i], new_text: text);
            }

            // Remove Extra
            while (_hitanal_pos.Count > hits.Length)
            {
                _renderer.Remove(_hitanal_pos[_hitanal_pos.Count - 1]);
                _hitanal_pos.RemoveAt(_hitanal_pos.Count - 1);
            }

            while (_hitanal_accel.Count > hits.Length)
            {
                _renderer.Remove(_hitanal_accel[_hitanal_accel.Count - 1]);
                _hitanal_accel.RemoveAt(_hitanal_accel.Count - 1);
            }

            while (_hitanal_dirs.Count > hits.Length)
            {
                _renderer.Remove(_hitanal_dirs[_hitanal_dirs.Count - 1].orth);
                _renderer.Remove(_hitanal_dirs[_hitanal_dirs.Count - 1].back);
                _hitanal_dirs.RemoveAt(_hitanal_dirs.Count - 1);
            }

            while (_hitanal_reports.Count > hits.Length)
            {
                _renderer.Remove(_hitanal_reports[_hitanal_reports.Count - 1]);
                _hitanal_reports.RemoveAt(_hitanal_reports.Count - 1);
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
            if (_ellipsePoints == null || !_ellipsePoints.Angle.IsNearValue(JetpackScript.ObstAvoid_EllipsePointAngle))
                _ellipsePoints = GetEllipsePoints(JetpackScript.ObstAvoid_EllipsePointAngle);

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

        private Ray[] GetEllipseRays(Vector3 origin, Vector3[] perimiter, Vector3 velocity)
        {
            var retVal = new List<Ray>();

            Vector3 ray_dir = velocity.normalized;

            retVal.Add(new Ray(origin, ray_dir));

            foreach (Vector3 perim_point in perimiter)
            {
                Vector3 orth_axis = Vector3.Cross(velocity, perim_point - origin).normalized;

                //retVal.Add(new Ray(perim_point, Quaternion.AngleAxis(JetpackScript.ObstAvoid_EllipseRayAngleIn, -orth_axis) * ray_dir));
                retVal.Add(new Ray(perim_point, ray_dir));
                retVal.Add(new Ray(perim_point, Quaternion.AngleAxis(JetpackScript.ObstAvoid_EllipseRayAngleOut, orth_axis) * ray_dir));
            }

            return retVal.ToArray();
        }

        private void CastAndStoreRays(Ray[] rays, Vector3 velocity, float ray_len)
        {
            var results = new RayCastStorage.RayInfo[rays.Length];

            for (int i = 0; i < rays.Length; i++)
            {
                results[i] = new RayCastStorage.RayInfo()
                {
                    Origin = rays[i].origin,
                    Direction = rays[i].direction,
                    MaxLen = ray_len,
                };

                if (Physics.Raycast(rays[i].origin, rays[i].direction, out RaycastHit hit, ray_len, ScanningUtil.SolidObject_LayerMask.Value, QueryTriggerInteraction.Ignore))
                    results[i].Hit = hit;
            }

            _raycast_storage.AddRayCasts(
                RayCastStorage.RayCategory.ObstacleAvoidance_BodyEllipse,
                new RayCastStorage.RayCastBundle
                {
                    Rays = results,
                });
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

        private static HitDetails[] AnalyzeHits(RayCastStorage.RayInfo[] rays, float ray_len, Vector3 pos, Vector3 velocity_dir, float speed)
        {
            var retVal = new List<HitDetails>();

            foreach (var ray in rays)
            {
                if (ray.Hit == null)
                    continue;

                // Ignore the origin of the ray cast and instead get the vect to the player's center pos
                Vector3 to_hit = ray.Hit.Value.point - pos;

                float to_hit_dist = to_hit.magnitude;
                if (to_hit_dist > JetpackScript.ObstAvoid_Analyze_MaxDist)
                    continue;

                if (!ray.Hit.Value.normal.magnitude.IsNearValue(1))
                    Debug.Log($"hit normal isn't one: {ray.Hit.Value.normal.magnitude}");

                Vector3 normal = ray.Hit.Value.normal;

                float dot = Vector3.Dot(velocity_dir, normal);

                if (dot < 0)
                    normal = -normal;

                float normalized_dist = to_hit_dist / JetpackScript.ObstAvoid_Analyze_MaxDist;

                float dist_reduce_percent = 1 - (float)Math.Pow(normalized_dist, JetpackScript.ObstAvoid_Analyze_DropoffPow);
                float speed_reduce_percent = speed * JetpackScript.ObstAvoid_Analyze_SpeedMult;

                Vector3 line_point = Math3D.GetClosestPoint_Line_Point(new Ray(pos, velocity_dir), ray.Hit.Value.point);
                Vector3 axis_line = line_point - ray.Hit.Value.point;

                retVal.Add(new HitDetails
                {
                    Pos = ray.Hit.Value.point,
                    Normal = normal,

                    Accel = normal * dist_reduce_percent * speed_reduce_percent,
                    TowardCenter_Dir = axis_line.normalized,

                    dist_reduce_percent = dist_reduce_percent,
                    speed_reduce_percent = speed_reduce_percent,
                });
            }

            return retVal.ToArray();
        }

        #endregion
    }
}
