using Jetpack2.Core;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using static Jetpack2.Core.UtilJetpack;

namespace Jetpack2.InputWatchers
{
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
            public static SamplePoints FromVector(float[] vector)
            {
                if (vector == null)
                    throw new ArgumentNullException(nameof(vector));

                if (vector.Length != 27)
                    throw new ArgumentException($"vector needs to be length 27: {vector.Length}");

                return new SamplePoints
                {
                    HeadPos = new Vector3(vector[0], vector[1], vector[2]),
                    HeadForward = new Vector3(vector[3], vector[4], vector[5]),
                    HeadUp = new Vector3(vector[6], vector[7], vector[8]),

                    LeftPos = new Vector3(vector[9], vector[10], vector[11]),
                    LeftForward = new Vector3(vector[12], vector[13], vector[14]),
                    LeftUp = new Vector3(vector[15], vector[16], vector[17]),

                    RightPos = new Vector3(vector[18], vector[19], vector[20]),
                    RightForward = new Vector3(vector[21], vector[22], vector[23]),
                    RightUp = new Vector3(vector[24], vector[25], vector[26]),
                };
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


        // figure out best number of clusters (elbow method)
        private const int NUM_CLUSTERS = 4;


        private const int CLUSTER_INTERVAL_SECONDS = 18;

        private DateTime _nextSampleTime = DateTime.UtcNow;

        private List<SamplePoints> _newSamples = new List<SamplePoints>();

        #endregion

        public void Update(Vector3 body_forward, Vector3 body_up)
        {
            DateTime now = DateTime.UtcNow;

            if (now < _nextSampleTime)
                return;

            AddSample(body_forward, body_up);


            // TODO: before going down a rabbit hole of ways to store long term results, show kmeans
            // results on screen
            //
            // most recent is one color, previous is more faded, N prev is most faded



            // see if a new clustering should happen:
            //  make sure time since last cluster >= CLUSTER_INTERVAL_SECONDS
            //  make sure a current cluster isn't running
            //  make sure _newSamples.Count >= MIN_SAMPLES_FOR_CLUSTERING



        }

        public PlayerVRPoints_Set GetAverageHandPositions()
        {

            // figure out what to do if there hasn't been enough time to get samples for clustering
            // maybe just return the average of the first N samples


            // otherwise return the cached best result



            return GetAverage(_newSamples);

        }

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

        private void KickoffCluster()
        {
            // if there's a current one running, throw exception (the check should have happened before calling this function)

            // move _newSamples into batch list (current thread)
            // store that a batch has started

            // in a separate thread:
            //  cluster the batch of samples
            //  add those results into the long term storage
            //      this involves analyzing cluster, maybe merging clusters or doing a new cluster with 150% num nodes
            //  reevaluate the sharable results
            //
            // continue in current thread (or lock):
            //  store the sharable results
            //  mark the batch as finished

            // ------------------

            // when doing kmeans, use elbow method to figure out how many clusters to commit to

            // ------------------


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

        private static PlayerVRPoints_Set GetAverage(List<SamplePoints> samples)
        {
            if (samples.Count == 0)
                return null;

            Vector3 head = Math3D.GetAverage(samples.Select(o => o.HeadPos));
            Vector3 left = Math3D.GetAverage(samples.Select(o => o.LeftPos));
            Vector3 right = Math3D.GetAverage(samples.Select(o => o.RightForward));

            return new PlayerVRPoints_Set
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
