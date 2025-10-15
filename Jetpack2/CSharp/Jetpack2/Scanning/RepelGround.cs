using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.Scanning
{

    // TODO: make separate classes for hover vs obstacle avoidance


    // this shouldn't be hardcoded to vector.up, it should be against the hit's normal

    // if the normals aren't enough, a small extra speed mult for vel_horz should be added (that will make it impossible to go through doors at high speed, so should be avoided)


    public class RepelGround
    {
        #region Declaration Section

        #region debug drawing vars

        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private DebugRenderer3D _renderer = null;

        private DebugItem _vel_text = null;
        private DebugItem _vel_line = null;
        private DebugItem _pos_foot = null;

        //private DebugItem _head_forward = null;
        //private DebugItem _head_up = null;
        //private DebugItem _head_right = null;

        private DebugItem _heightscale = null;

        private List<DebugItem> _ray_starts = new List<DebugItem>();
        private List<DebugItem> _ray_lines = new List<DebugItem>();
        private List<DebugItem> _ray_hits = new List<DebugItem>();

        private DebugItem _avghit_from = null;
        private DebugItem _avghit_to = null;
        private DebugItem _avghit_line = null;
        private DebugItem _avghit_text = null;

        private DebugItem _accel_text = null;

        #endregion

        private readonly RayCastStorage _raycast_storage;

        // These get reset each time Update_CastRays, and are then used by the corresponding call to GetGroundAccel
        private Vector3 _velocity;
        private Vector3 _vel_horz;
        private Vector3 _vel_vert;
        private float _speed_vert;
        private Vector3 _foot_pos;
        private float _height;
        private float _scale;
        private float _ray_len;

        #endregion

        public RepelGround(RayCastStorage raycast_storage)
        {
            _raycast_storage = raycast_storage;
        }

        public void Update_CastRays(ThunderRoad.Locomotion loco)
        {
            if (!JetpackScript.ShouldRepelGround)
                return;

            _velocity = loco.physicBody.velocity;

            // NOTE: removing this check, hover could still be needed if going slightly up, like encountering a cliff
            // Check if traveling downward
            //if (velocity.y >= 0)
            //{
            //    if (SHOWDEBUG)
            //        ClearDebugVisuals();
            //    return null;
            //}

            // Figure how ray start point (player's feet)
            _foot_pos = Math3D.GetAverage(Player.local.footLeft.ragdollFoot.root.position, Player.local.footRight.ragdollFoot.root.position);        // Player.local.transform.position is the room level origin

            // Split velocity into horizontal and vertical components
            _vel_horz = _velocity.GetProjectedVector_plane(Vector3.up);
            _vel_vert = _velocity.GetProjectedVector(Vector3.up);

            float speed_horz = _vel_horz.magnitude;

            _speed_vert = _vel_vert.magnitude;
            if (Vector3.Dot(_vel_vert, Vector3.up) < 0)
                _speed_vert *= -1;

            _height = Player.local.creature.morphology?.height ?? 1.5f;
            Vector3 scale_vect = Player.local.transform.localScale;
            _scale = Math1D.Avg(scale_vect.x, scale_vect.y, scale_vect.z);

            var rays = GetRays(_foot_pos, _vel_horz, _height, _scale, speed_horz, _speed_vert);

            _ray_len = rays.len;

            var results = new RayCastStorage.RayInfo[rays.rays.Length];

            for (int i = 0; i < rays.rays.Length; i++)
            {
                results[i] = new RayCastStorage.RayInfo()
                {
                    Origin = rays.rays[i].origin,
                    Direction = rays.rays[i].direction,
                    MaxLen = rays.len,
                };

                if (Physics.Raycast(rays.rays[i].origin, rays.rays[i].direction, out RaycastHit hit, rays.len, ScanningUtil.SolidObject_LayerMask.Value, QueryTriggerInteraction.Ignore))
                    results[i].Hit = hit;
            }

            _raycast_storage.AddRayCasts(
                RayCastStorage.RayCategory.RepelGround,
                new RayCastStorage.RayCastBundle
                {
                    Rays = results,
                });
        }
        public Vector3? Update_Finish(ThunderRoad.Locomotion loco, Vector3? input_dir)
        {
            if (!JetpackScript.ShouldRepelGround)
            {
                if (JetpackScript.ShowRepelGround)
                    ClearDebugVisuals();
                return null;
            }

            var rays = _raycast_storage.GetRayCasts(RayCastStorage.RayCategory.RepelGround);

            if (rays == null || rays.Length == 0)
                return null;


            //Vector3?[] hits = FireRays(rays.rays, rays.len);
            Vector3?[] hits = AnalyzeRays(rays[0].Rays);

            // Get the avg hit point and divide total strength by number of hits vs number of rays fired
            // This is done to reduce the number of calculations that would be needed for each hit.  Since
            // hover accel will be straight up, there's no need to calculate push forces at different hits
            // (they all contribute to up)
            var avg_hit = GetAverageHit(rays[0].Rays, hits);

            if (JetpackScript.ShowRepelGround)
            {
                DrawFootPos(_foot_pos);
                DrawVelocity(_foot_pos, _velocity, _vel_horz, _vel_vert);
                DrawHeightScale(_height, _scale);
                DrawRays(rays[0].Rays, _ray_len, hits);
                DrawAvgHit(avg_hit.has_hit, avg_hit.avg_from, avg_hit.avg_to, avg_hit.percent);
            }

            if (!avg_hit.has_hit)
            {
                if (JetpackScript.ShowRepelGround)
                    RemoveAccel();
                return null;
            }

            // Increase accel based on distance, speed, strength
            Vector3? accel = GetAccel(avg_hit.avg_from, avg_hit.avg_to, avg_hit.percent, _vel_vert, _ray_len, _speed_vert);

            accel = ObstacleAvoidance.DontFightInput(accel, input_dir);

            return accel;
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

            if (_vel_text != null)
            {
                _renderer.Remove(_vel_text);
                _vel_text = null;
            }

            if (_vel_line != null)
            {
                _renderer.Remove(_vel_line);
                _vel_line = null;
            }

            if (_pos_foot != null)
            {
                _renderer.Remove(_pos_foot);
                _pos_foot = null;
            }

            if (_heightscale != null)
            {
                _renderer.Remove(_heightscale);
                _heightscale = null;
            }

            foreach (var item in _ray_starts.Concat(_ray_lines).Concat(_ray_hits))
                _renderer.Remove(item);

            _ray_starts.Clear();
            _ray_lines.Clear();
            _ray_hits.Clear();

            if (_avghit_from != null)
            {
                _renderer.Remove(_avghit_from);
                _avghit_from = null;
            }

            if (_avghit_to != null)
            {
                _renderer.Remove(_avghit_to);
                _avghit_to = null;
            }

            if (_avghit_line != null)
            {
                _renderer.Remove(_avghit_line);
                _avghit_line = null;
            }

            if (_avghit_text != null)
            {
                _renderer.Remove(_avghit_text);
                _avghit_text = null;
            }

            if (_accel_text != null)
            {
                _renderer.Remove(_accel_text);
                _accel_text = null;
            }

            _renderer = null;
        }

        private void DrawVelocity(Vector3 pos, Vector3 velocity, Vector3 vel_horz, Vector3 vel_vert)
        {
            EnsureDebugActive();

            if (_vel_line == null)
                _vel_line = _renderer.AddLine_Basic(pos, pos + velocity, LINE_THICKNESS, Color.yellow);

            string text = $"vel: {velocity.ToStringSignificantDigits(1)}{Environment.NewLine}speed: {velocity.magnitude.ToStringSignificantDigits(3)}{Environment.NewLine}speed horz: {vel_horz.magnitude.ToStringSignificantDigits(3)}{Environment.NewLine}speed vert: {vel_vert.magnitude.ToStringSignificantDigits(3)}";

            #region draw head orientation

            //Vector3 headaxis_pos = Player.local.head.anchor.position + Player.local.head.transform.forward * 1f;

            //if (_head_forward == null)
            //{
            //    _head_forward = _renderer.AddLine_Basic(headaxis_pos, headaxis_pos + Player.local.head.transform.forward, LINE_THICKNESS, Color.blue);
            //    _head_right = _renderer.AddLine_Basic(headaxis_pos, headaxis_pos + Player.local.head.transform.right, LINE_THICKNESS, Color.red);
            //    _head_up = _renderer.AddLine_Basic(headaxis_pos, headaxis_pos + Player.local.head.transform.up, LINE_THICKNESS, Color.green);
            //}

            //DebugRenderer3D.AdjustLinePositions(_head_forward, headaxis_pos, headaxis_pos + Player.local.head.transform.forward);       // NOTE: head.anchor is rotated 90 degrees clockwise, need to use head.transform for directions
            //DebugRenderer3D.AdjustLinePositions(_head_right, headaxis_pos, headaxis_pos + Player.local.head.transform.right);
            //DebugRenderer3D.AdjustLinePositions(_head_up, headaxis_pos, headaxis_pos + Player.local.head.transform.up);

            #endregion

            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                Player.local.head.transform.right * 0.75f +
                Player.local.head.transform.up * -0.33f;

            if (_vel_text == null)
                _vel_text = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, Color.yellow, Color.black, TEXT_HEIGHT * 4);

            DebugRenderer3D.AdjustLinePositions(_vel_line, pos, pos + velocity);
            _vel_text.Object.transform.position = text_pos;
            _vel_text.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_vel_text, new_text: text);
        }

        private void DrawFootPos(Vector3 pos)
        {
            EnsureDebugActive();

            if (_pos_foot == null)
                _pos_foot = _renderer.AddDot(pos, DOT_SIZE, Color.yellow);

            _pos_foot.Object.transform.position = pos;
        }

        private void DrawHeightScale(float height, float scale)
        {
            EnsureDebugActive();

            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                Player.local.head.transform.right * 0.75f +
                Player.local.head.transform.up * 0.33f;

            string text = $"height: {height.ToStringSignificantDigits(3)} scale: {scale.ToStringSignificantDigits(3)}";

            if (_heightscale == null)
                _heightscale = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, UtilityColor.FromHex("F5D951"), Color.black, TEXT_HEIGHT);

            _heightscale.Object.transform.position = text_pos;
            _heightscale.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_heightscale, new_text: text);
        }

        private void DrawRays(RayCastStorage.RayInfo[] rays, float ray_len, Vector3?[] hits)
        {
            EnsureDebugActive();

            int hit_index = -1;

            // Add/Update
            for (int i = 0; i < rays.Length; i++)
            {
                if (_ray_starts.Count <= i)
                    _ray_starts.Add(_renderer.AddDot(rays[i].Origin, DOT_SIZE, UtilityColor.FromHex("51E1F5")));

                _ray_starts[i].Object.transform.position = rays[i].Origin;

                if (_ray_lines.Count <= i)
                    _ray_lines.Add(_renderer.AddLine_Basic(rays[i].Origin, rays[i].Origin + rays[i].Direction * ray_len, LINE_THICKNESS, Color.black));

                DebugRenderer3D.AdjustLinePositions(_ray_lines[i], rays[i].Origin, rays[i].Origin + rays[i].Direction * ray_len);
                DebugRenderer3D.AdjustColor(_ray_lines[i], hits[i] != null ? UtilityColor.FromHex("47CF38") : UtilityColor.FromHex("CC3835"));

                if (hits[i] != null)
                {
                    hit_index++;
                    if (_ray_hits.Count <= hit_index)
                        _ray_hits.Add(_renderer.AddDot(hits[i].Value, DOT_SIZE, UtilityColor.FromHex("1CB50B")));

                    _ray_hits[hit_index].Object.transform.position = hits[i].Value;
                }
            }

            // Remove Extra
            while (_ray_starts.Count > rays.Length)
            {
                _renderer.Remove(_ray_starts[_ray_starts.Count - 1]);
                _ray_starts.RemoveAt(_ray_starts.Count - 1);
            }

            while (_ray_lines.Count > rays.Length)
            {
                _renderer.Remove(_ray_lines[_ray_lines.Count - 1]);
                _ray_lines.RemoveAt(_ray_lines.Count - 1);
            }

            while (_ray_hits.Count > hit_index + 1)     // hit_index is still -1 if there are no hits
            {
                _renderer.Remove(_ray_hits[_ray_hits.Count - 1]);
                _ray_hits.RemoveAt(_ray_hits.Count - 1);
            }
        }

        private void DrawAvgHit(bool has_hit, Vector3 avg_from, Vector3 avg_to, float percent)
        {
            if (!has_hit)
            {
                if (_avghit_from != null)
                    _renderer.Remove(_avghit_from);

                if (_avghit_to != null)
                    _renderer.Remove(_avghit_to);

                if (_avghit_line != null)
                    _renderer.Remove(_avghit_line);

                if (_avghit_text != null)
                    _renderer.Remove(_avghit_text);

                _avghit_from = null;
                _avghit_to = null;
                _avghit_line = null;
                _avghit_text = null;
                return;
            }

            EnsureDebugActive();

            Color color = UtilityColor.FromHex("12B6CC");

            if (_avghit_from == null)
                _avghit_from = _renderer.AddDot(avg_from, DOT_SIZE * 2, color);

            _avghit_from.Object.transform.position = avg_from;

            if (_avghit_to == null)
                _avghit_to = _renderer.AddDot(avg_to, DOT_SIZE * 2, color);

            _avghit_to.Object.transform.position = avg_to;

            if (_avghit_line == null)
                _avghit_line = _renderer.AddLine_Basic(avg_from, avg_to, LINE_THICKNESS * 2, color);

            DebugRenderer3D.AdjustLinePositions(_avghit_line, avg_from, avg_to);

            string text = $"{(avg_to - avg_from).magnitude.ToStringSignificantDigits(3)} | {Math.Round(percent * 100)}%";
            Vector3 text_pos = avg_from + (avg_to - avg_from) * 0.75f;
            if (_avghit_text == null)
                _avghit_text = _renderer.AddText(text, text_pos, Vector3.down, color, Color.black, TEXT_HEIGHT * 2);

            _avghit_text.Object.transform.position = text_pos;
            DebugRenderer3D.AdjustText(_avghit_text, new_text: text);
        }

        private void DrawAccel(Vector3 linear, Vector3 inverse, Vector3 invsqr, float percent)
        {
            EnsureDebugActive();

            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                Player.local.head.transform.right * -0.75f; //+
                                                            //Player.local.head.transform.up * -0.33f;

            Vector3 total = linear + inverse + invsqr;

            var text = new StringBuilder();
            text.AppendLine($"linear: {linear.magnitude.ToStringSignificantDigits(3)}");
            text.AppendLine($"inverse: {inverse.magnitude.ToStringSignificantDigits(3)}");
            text.AppendLine($"invsqr: {invsqr.magnitude.ToStringSignificantDigits(3)}");
            text.AppendLine($"total: {total.magnitude.ToStringSignificantDigits(3)}");
            text.AppendLine($"percent: {percent.ToStringSignificantDigits(2)}");
            text.AppendLine($"total %: {(total.magnitude * percent).ToStringSignificantDigits(3)}");

            float text_height = TEXT_HEIGHT * 6;        // there are 6 lines

            if (_accel_text == null)
                _accel_text = _renderer.AddText(text.ToString(), text_pos, Player.local.head.transform.forward, UtilityColor.FromHex("334226"), Color.white, text_height);

            _accel_text.Object.transform.position = text_pos;
            _accel_text.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_accel_text, new_text: text.ToString());
        }
        private void RemoveAccel()
        {
            if (_accel_text != null)
            {
                _renderer.Remove(_accel_text);
                _accel_text = null;
            }
        }

        #endregion
        #region Private Methods

        private static (Ray[] rays, float len) GetRays(Vector3 foot_pos, Vector3 vel_horz, float height, float scale, float speed_horz, float speed_vert)
        {
            // When traveling downward quickly, there needs to be more room to slow down
            // When there is no vertical speed down, the distance should be small so the player can float close to the ground (get through doors, interact with npcs)
            float dist_increase = 0;

            if (speed_horz > 0)
                dist_increase += JetpackScript.RepelGround_HorzSpeedDistMult * speed_horz;

            if (speed_vert < 0)
                dist_increase += JetpackScript.RepelGround_VertSpeedDistMult * Math.Abs(speed_vert);

            // Figure out ray length (some combination of down velocity and player's height * scale)
            float ray_len = dist_increase + (height * scale * JetpackScript.RepelGround_MaxDistance);

            // When the distance is small, one ray is enough
            if (vel_horz.sqrMagnitude < 1f * 1f)
            {
                Ray[] rays1 = new[]
                {
                    new Ray(foot_pos + vel_horz * 0.5f, Vector3.down),
                };

                return (rays1, ray_len);
            }

            // Figure out start points
            Vector3 start_near = foot_pos + vel_horz * 0.25f;

            Vector3 start_far = foot_pos + vel_horz * 0.75f;

            float len_far = (start_far - foot_pos).magnitude;
            Vector3 far_orth = Vector3.Cross(vel_horz, Vector3.up).normalized * (len_far * 0.15f);      // don't just make one, make two a small distance from each other, so it forms a narrow triangle

            Vector3 start_far1 = start_far + far_orth;
            Vector3 start_far2 = start_far - far_orth;

            Ray[] rays3 = new[]
            {
                new Ray(start_near, Vector3.down),
                new Ray(start_far1, Vector3.down),
                new Ray(start_far2, Vector3.down),
            };

            return (rays3, ray_len);
        }

        private static Vector3?[] FireRays(Ray[] rays, float ray_len)
        {
            var retVal = new Vector3?[rays.Length];

            for (int i = 0; i < rays.Length; i++)
                if (Physics.Raycast(rays[i].origin, rays[i].direction, out RaycastHit hit, ray_len, ScanningUtil.SolidObject_LayerMask.Value, QueryTriggerInteraction.Ignore))
                    if (Math.Abs(Vector3.Dot(hit.normal, Vector3.up)) > 0.7)        // about 45 degrees
                        retVal[i] = hit.point;

            return retVal;
        }
        private static Vector3?[] AnalyzeRays(RayCastStorage.RayInfo[] rays)
        {
            var retVal = new Vector3?[rays.Length];

            for (int i = 0; i < rays.Length; i++)
            {
                if (rays[i].Hit == null)
                    continue;

                if (Math.Abs(Vector3.Dot(rays[i].Hit.Value.normal, Vector3.up)) < 0.7)        // about 45 degrees
                    continue;

                retVal[i] = rays[i].Hit.Value.point;
            }

            return retVal;
        }

        private static (bool has_hit, Vector3 avg_from, Vector3 avg_to, float percent) GetAverageHit(RayCastStorage.RayInfo[] rays, Vector3?[] hits)
        {
            // Copying Math3D.GetAverage as an optimization

            if (hits.Length == 0)
                return (false, Vector3.zero, Vector3.zero, 0);

            float x1 = 0f;
            float y1 = 0f;
            float z1 = 0f;

            float x2 = 0f;
            float y2 = 0f;
            float z2 = 0f;

            int length = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null)
                    continue;

                x1 += rays[i].Origin.x;
                y1 += rays[i].Origin.y;
                z1 += rays[i].Origin.z;

                x2 += hits[i].Value.x;
                y2 += hits[i].Value.y;
                z2 += hits[i].Value.z;

                length++;
            }

            if (length == 0)
                return (false, Vector3.zero, Vector3.zero, 0);

            float oneOverLen = 1f / (float)length;

            Vector3 avg1 = new Vector3(x1 * oneOverLen, y1 * oneOverLen, z1 * oneOverLen);
            Vector3 avg2 = new Vector3(x2 * oneOverLen, y2 * oneOverLen, z2 * oneOverLen);

            return (true, avg1, avg2, (float)length / (float)hits.Length);
        }

        private Vector3? GetAccel(Vector3 ray_origin, Vector3 hit_pos, float percent, Vector3 vel_vert, float max_dist, float speed_vert)
        {
            bool is_up = speed_vert > 0;

            if (is_up && speed_vert > JetpackScript.RepelGround_UpSpeed_ZeroAccel)
                return null;

            Vector3 direction = Vector3.up;

            float distance = (hit_pos - ray_origin).magnitude;

            if (distance > max_dist)        // should never happen, since this function is only called when there is a raycast hit, and max dist is the length of the raycast
                return null;

            // TODO: may want to increase max accel if speed downward is large

            Vector3 linear = GetAccel_Linear(direction, distance, max_dist, JetpackScript.RepelGround_Linear_MaxAccel);
            Vector3 inverse = GetAccel_Inverse(direction, distance, max_dist, JetpackScript.RepelGround_Inverse_MaxAccel, JetpackScript.RepelGround_Inverse_C);
            Vector3 invsqr = GetAccel_InvSqr(direction, distance, max_dist, JetpackScript.RepelGround_InverseSqr_MaxAccel, JetpackScript.RepelGround_InverseSqr_C);

            if (JetpackScript.ShowRepelGround)
                DrawAccel(linear, inverse, invsqr, percent);

            Vector3 retVal = (linear + inverse + invsqr) * percent;

            // Limit the accel when velocity is upward.  The player should land softly near but not touching the ground,
            // the player shouldn't be flung back up in the air
            if (is_up)
            {
                float up_reduce_perc = UtilityMath.GetScaledValue_Capped(0, 1, max_dist, 0, speed_vert);
                retVal *= up_reduce_perc;
            }

            return retVal;
        }

        // Force climbs linearly from 0 to max
        private static Vector3 GetAccel_Linear(Vector3 direction_unit, float distance, float max_dist, float max_accel)
        {
            float normalized_dist = distance / max_dist;

            float accel = -normalized_dist + 1;      // force will be 1 at dist0 and 0 at dist1
            accel *= max_accel;

            return direction_unit * accel;
        }

        // Force grows 1/x
        private static Vector3 GetAccel_Inverse(Vector3 direction_unit, float distance, float max_dist, float max_accel, float constant)
        {
            float normalized_dist = distance / max_dist;

            float accel = 1 / (constant * normalized_dist);
            accel *= max_accel;

            if (accel > max_accel)
                accel = max_accel;

            return direction_unit * accel;
        }

        // Force is 1/x^2
        private static Vector3 GetAccel_InvSqr(Vector3 direction_unit, float distance, float max_dist, float max_accel, float constant)
        {
            float normalized_dist = distance / max_dist;

            float accel = constant * normalized_dist;
            accel *= accel;
            accel = 1 / accel;
            accel *= max_accel;

            if (accel > max_accel)
                accel = max_accel;

            return direction_unit * accel;
        }

        #endregion
    }
}
