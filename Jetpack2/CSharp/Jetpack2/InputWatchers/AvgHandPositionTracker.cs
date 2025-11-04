using Jetpack2.Core;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using ThunderRoad.DebugViz;
using UnityEngine;

namespace Jetpack2.InputWatchers
{
    // NOTE: this was made to help figure out when they have hands near the body and when stretched out,
    // but that is a flawed idea, since flying around with hands out to be a wing would influence this
    // class's results
    //
    // a simple threshold distance based on head to foot distance should be good enough
    //
    // so maybe this class could be useful in the future, but if used, will need to be improved.  it is
    // currently just keeping the last cluster, so is very short term memory

    // TODO: clustering has a lot of complexity and long term storage is expensive
    // instead of returning cluster positions, return resting sphere, max sphere, halfway sphere (halfway
    // plane between resting and outstretched)

    public class AvgHandPositionTracker
    {
        #region class: SamplePoints

        /// <summary>
        /// These are relative to center of body and body forward/up (from PlayerRagdollUtil)
        /// </summary>
        public class SamplePoints
        {
            public Vector3 HeadPos { get; set; }
            public Vector3 HeadForward { get; set; }
            public Vector3 HeadUp { get; set; }

            public Vector3 LeftPos { get; set; }
            public Vector3 LeftForward { get; set; }
            public Vector3 LeftUp { get; set; }

            public Vector3 RightPos { get; set; }
            public Vector3 RightForward { get; set; }
            public Vector3 RightUp { get; set; }

            public float[] ToVector()
            {
                return ToVector(this);
            }
            public static float[] ToVector(SamplePoints sample)
            {
                if (sample == null)
                    throw new ArgumentNullException(nameof(sample));

                return new float[]
                {
                    sample.HeadPos.x, sample.HeadPos.y, sample.HeadPos.z,
                    sample.HeadForward.x, sample.HeadForward.y, sample.HeadForward.z,
                    sample.HeadUp.x, sample.HeadUp.y, sample.HeadUp.z,

                    sample.LeftPos.x, sample.LeftPos.y, sample.LeftPos.z,
                    sample.LeftForward.x, sample.LeftForward.y, sample.LeftForward.z,
                    sample.LeftUp.x, sample.LeftUp.y, sample.LeftUp.z,

                    sample.RightPos.x, sample.RightPos.y, sample.RightPos.z,
                    sample.RightForward.x, sample.RightForward.y, sample.RightForward.z,
                    sample.RightUp.x, sample.RightUp.y, sample.RightUp.z
                };
            }
            public static SamplePoints FromVector(float[] vector, bool repair_dirs = true)
            {
                if (vector == null)
                    throw new ArgumentNullException(nameof(vector));

                if (vector.Length != 27)
                    throw new ArgumentException($"vector needs to be length 27: {vector.Length}");

                var head_dirs = GetRepairedForwardUp(new Vector3(vector[3], vector[4], vector[5]), new Vector3(vector[6], vector[7], vector[8]), repair_dirs);
                var left_dirs = GetRepairedForwardUp(new Vector3(vector[12], vector[13], vector[14]), new Vector3(vector[15], vector[16], vector[17]), repair_dirs);
                var right_dirs = GetRepairedForwardUp(new Vector3(vector[21], vector[22], vector[23]), new Vector3(vector[24], vector[25], vector[26]), repair_dirs);

                return new SamplePoints
                {
                    HeadPos = new Vector3(vector[0], vector[1], vector[2]),
                    HeadForward = head_dirs.forward,
                    HeadUp = head_dirs.up,

                    LeftPos = new Vector3(vector[9], vector[10], vector[11]),
                    LeftForward = left_dirs.forward,
                    LeftUp = left_dirs.up,

                    RightPos = new Vector3(vector[18], vector[19], vector[20]),
                    RightForward = right_dirs.forward,
                    RightUp = right_dirs.up,
                };
            }

            public static float[] GetWeights(float headPos = 1, float headForward = 1, float headUp = 1, float leftPos = 1, float leftForward = 1, float leftUp = 1, float rightPos = 1, float rightForward = 1, float rightUp = 1)
            {
                return new[] {
                    headPos, headPos, headPos,
                    headForward, headForward, headForward,
                    headUp, headUp, headUp,
                    leftPos, leftPos, leftPos,
                    leftForward, leftForward, leftForward,
                    leftUp, leftUp, leftUp,
                    rightPos, rightPos, rightPos,
                    rightForward, rightForward, rightForward,
                    rightUp, rightUp, rightUp };
            }

            private static (Vector3 forward, Vector3 up) GetRepairedForwardUp(Vector3 forward, Vector3 up, bool repair_dirs)
            {
                if (!repair_dirs)       // don't take the expense if they aren't used
                    return (forward, up);

                forward = forward.normalized;
                up = up.normalized;

                float dot = Vector3.Dot(forward, up);

                // If already perpendicular (or nearly so), return as is
                if (dot.IsNearZero())
                    return (forward, up);

                if (Math.Abs(dot).IsNearValue(1))
                    return (forward, Math3D.GetArbitraryOrthogonal(forward));        // special case where they are parallel or opposed.  there's no good correct answer, so assume that forward is correct and return an arbitrary up

                // Create a perpendicular vector using cross product
                Vector3 perpendicular = Vector3.Cross(forward, up).normalized;

                float angle = Math1D.Dot_to_Degrees(dot);
                angle = 90 - angle;

                var quat = Quaternion.AngleAxis(-angle / 2, perpendicular);

                forward = quat * forward;
                up = Quaternion.Inverse(quat) * up;

                return (forward, up);
            }
        }

        #endregion
        #region class: Result

        public class Result
        {
            // left and right hand positions



            // ------ these props need to be in a dedicated object that is used for more than just this tracker ------
            // center point
            //  and maybe how it relates to head and foot positions?

            // a transform that can convert to world coords
        }

        #endregion

        #region Declaration Section

        private const float SAMPLE_INTERVAL_SECONDS = 0.1f;
        private const int MIN_SAMPLES_FOR_CLUSTERING = 100;
        private const int CLUSTER_INTERVAL_SECONDS = 18;

        private PlayerRagdollUtil _ragdollUtil = null;

        private DateTime _nextSampleTime = DateTime.UtcNow;

        private List<SamplePoints> _newSamples = new List<SamplePoints>();

        private readonly object _lock = new object();
        private bool _isClustering = false;
        private KMeansClusterer.Cluster<SamplePoints>[] _clusters = null;
        private DateTime _nextClusterTime = DateTime.UtcNow;

        #region debug drawing vars

        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private DebugRenderer3D _renderer = null;

        private DebugItem _stats = null;

        private List<Color> _cluster_colors = new List<Color>();

        private int _dot_index = -1;
        private List<DebugItem> _dots = new List<DebugItem>();       // these are dots (showing contents of buffer)

        #endregion

        #endregion

        public void Update_Flying(Vector3 body_forward, Vector3 body_up, bool had_thumbstick_input)
        {
            // This class cares about resting hand position and that is most likely when they are using
            // the thumbsticks.  This check will remove a lot of noise (like swinging a sword, putting the
            // controllers down to go into the kitchen, etc)
            if (!had_thumbstick_input)
                return;

            DateTime now = DateTime.UtcNow;

            if (now < _nextSampleTime)
                return;

            _nextSampleTime = now + TimeSpan.FromSeconds(SAMPLE_INTERVAL_SECONDS);

            AddSample(body_forward, body_up);
        }
        public void Update_Any(bool isFlying)
        {
            DateTime now = DateTime.UtcNow;

            TryKickoffCluster(now);

            if (UIModOptions.ShowPlayerPosTracking)
            {
                PrepareForDraw();

                DrawStats();

                if (_ragdollUtil == null)
                    _ragdollUtil = new PlayerRagdollUtil();

                var (forward, up) = _ragdollUtil.GetRagdollForwardUp();
                DrawClusterResults(forward, up);

                FinishedDraw();
            }
        }

        public void Clear()
        {
            ClearDebugVisuals();
        }

        public UtilJetpack.PlayerVRPoints_Set GetAverageHandPositions()
        {

            // figure out what to do if there hasn't been enough time to get samples for clustering
            // maybe just return the average of the first N samples


            // otherwise return the cached best result



            return GetAverage(_newSamples);

        }

        #region Private Methods - drawing

        private void EnsureDebugActive()
        {
            if (_renderer == null)
                _renderer = DebugRenderer3D.GetOrAddDebugRenderer3D();
        }

        private void ClearDebugVisuals()
        {
            if (_renderer == null)
                return;

            if (_stats != null)
            {
                _renderer.Remove(_stats);
                _stats = null;
            }

            foreach (DebugItem item in _dots)
                _renderer.Remove(item);
            _dots.Clear();

            _renderer = null;
        }

        // These manage visuals that persist across frames.  Prepare removes despawned, finsih sets visibility based on how many are used this frame
        private void PrepareForDraw()
        {
            RemoveDespawned(_dots);

            _dot_index = -1;
        }
        private void FinishedDraw()
        {
            SetActive(_dots, _dot_index + 1);
        }

        private static void RemoveDespawned(List<DebugItem> items)
        {
            int index = 0;

            while (index < items.Count)
            {
                if (items[index].Object == null)
                {
                    //Debug.Log($"Removing despawned visual: {items[index].Token}");
                    items.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        private static void SetActive(List<DebugItem> items, int count)
        {
            for (int i = 0; i < items.Count; i++)
                items[i].Object.SetActive(i < count);     // this should be cheaper than removing/adding
        }

        private void DrawStats()
        {
            EnsureDebugActive();

            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                Player.local.head.transform.right * -0.35f +
                Player.local.head.transform.up * -0.25f;

            bool isClustering = false;
            int? cluster_count = null;
            lock (_lock)
            {
                isClustering = _isClustering;
                cluster_count = _clusters?.Length;
            }

            string[] lines = new[]
            {
                $"num samples: {_newSamples.Count}",
                $"is clustering: {isClustering}",
                $"num clusters: {cluster_count?.ToString() ?? "--"}",
            };
            string text = string.Join(Environment.NewLine, lines);

            if (_stats == null)
                _stats = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, Color.black, Color.green, TEXT_HEIGHT * lines.Length);

            _stats.Object.transform.position = text_pos;
            _stats.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_stats, new_text: text);
        }

        private void DrawClusterResults(Vector3 body_forward, Vector3 body_up)
        {
            EnsureDebugActive();

            // Get info from cluster
            (Vector3 left, Vector3 right)[] centers = null;
            lock (_lock)
            {
                if (_clusters != null)
                {
                    centers = new (Vector3 left, Vector3 right)[_clusters.Length];

                    for (int i = 0; i < _clusters.Length; i++)
                    {
                        var sample = SamplePoints.FromVector(_clusters[i].Center, false);
                        centers[i] = (sample.LeftPos, sample.RightPos);
                    }
                }
            }

            if (centers == null)
                return;

            // Draw the positions
            while (_cluster_colors.Count < centers.Length)
                _cluster_colors.Add(UtilityColor.RandomHSV(0, 1, 0.65f, 1f, 0.5f, 0.85f));

            var positions = UtilJetpack.GetPlayerPoints(body_forward, body_up);

            for (int i = 0; i < centers.Length; i++)
            {
                AddDot(positions.Transform_ToWorld(centers[i].left), _cluster_colors[i]);
                AddDot(positions.Transform_ToWorld(centers[i].right), _cluster_colors[i]);
            }
        }

        private void AddDot(Vector3 pos, Color color)
        {
            _dot_index++;

            if (_dot_index < _dots.Count)
            {
                _dots[_dot_index].Object.transform.position = pos;
                DebugRenderer3D.AdjustColor(_dots[_dot_index], color);
            }
            else
            {
                _dots.Add(_renderer.AddDot(pos, DOT_SIZE, color));
            }
        }

        #endregion
        #region Private Methods

        private void AddSample(Vector3 body_forward, Vector3 body_up)
        {
            var positions = UtilJetpack.GetPlayerPoints(body_forward, body_up);

            _newSamples.Add(new SamplePoints
            {
                HeadPos = positions.local.head,
                HeadForward = positions.rot_to_local * Player.local.head.transform.forward,
                HeadUp = positions.rot_to_local * Player.local.head.transform.up,

                LeftPos = positions.local.left,
                LeftForward = positions.rot_to_local * Player.local.handLeft.root.forward,
                LeftUp = positions.rot_to_local * Player.local.handLeft.root.up,

                RightPos = positions.local.right,
                RightForward = positions.rot_to_local * Player.local.handRight.root.forward,
                RightUp = positions.rot_to_local * Player.local.handRight.root.up,
            });
        }

        private void TryKickoffCluster(DateTime now)
        {
            if (_newSamples.Count < MIN_SAMPLES_FOR_CLUSTERING)
                return;

            lock (_lock)
            {
                if (_isClustering)
                    return;

                if (now < _nextClusterTime)
                    return;
            }

            // ------ Prep for clustering in main thread ------
            _isClustering = true;

            var samples = new KMeansClusterer.Sample<SamplePoints>[_newSamples.Count];

            for (int i = 0; i < _newSamples.Count; i++)
                samples[i] = new KMeansClusterer.Sample<SamplePoints>
                {
                    Vector = _newSamples[i].ToVector(),
                    Source = _newSamples[i],
                };

            _newSamples.Clear();

            float[] weights = SamplePoints.GetWeights(
                headPos: UIModOptions.PlayerPosTracking_Weight_HeadPos,
                headForward: UIModOptions.PlayerPosTracking_Weight_Directions,
                headUp: UIModOptions.PlayerPosTracking_Weight_Directions,

                leftPos: UIModOptions.PlayerPosTracking_Weight_HandPos,
                leftForward: UIModOptions.PlayerPosTracking_Weight_Directions,
                leftUp: UIModOptions.PlayerPosTracking_Weight_Directions,

                rightPos: UIModOptions.PlayerPosTracking_Weight_HandPos,
                rightForward: UIModOptions.PlayerPosTracking_Weight_Directions,
                rightUp: UIModOptions.PlayerPosTracking_Weight_Directions);



            // ------ Do clustering in separate thread ------

            var clusters = KMeansClusterer.DoClustering(samples, weights);

            // analyze cluster results, merge with long term result


            // ------ Store results ------

            lock (_lock)
            {
                _clusters = clusters;       // for now, just store directly
                _nextClusterTime = DateTime.UtcNow + TimeSpan.FromSeconds(CLUSTER_INTERVAL_SECONDS);
                _isClustering = false;
            }








            // once enough kmeans outputs are generated, do a bundle kmeans pass:

            // result_roundrobin = UtilityCore.InfiniteRoundRobin(kmeans_results)
            // while(samples.Count < max_count)
            //    samples.add(GetRandomSample(result_roundrobin.Next()))        // returns a random sample from the next kmeans result set that hasn't been picked yet


            // GetRandomSample(kmeans_output):
            //  the results should already be stored sorted by distance from center
            //  while(true)
            //      int index = (1 - rand.nextpow(2)) * count
            //      if index hasn't been picked before, return it


        }

        private static UtilJetpack.PlayerVRPoints_Set GetAverage(List<SamplePoints> samples)
        {
            if (samples.Count == 0)
                return null;

            Vector3 head = Math3D.GetAverage(samples.Select(o => o.HeadPos));
            Vector3 left = Math3D.GetAverage(samples.Select(o => o.LeftPos));
            Vector3 right = Math3D.GetAverage(samples.Select(o => o.RightForward));

            return new UtilJetpack.PlayerVRPoints_Set
            {
                center = Vector3.zero,      // local is centered on center, which is always zero
                head = head,
                foot = -head,       // center is midpoint between head and foot, so foot is opposite of head
                left = left,
                right = right,
            };
        }

        #endregion
    }
}
