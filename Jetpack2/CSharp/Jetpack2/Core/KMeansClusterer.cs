using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Jetpack2.Core
{
    // TODO: once this is proven, move to PerfectlyNormalBaS

    public static class KMeansClusterer
    {
        #region class: Sample

        public class Sample<T>
        {
            public float[] Vector { get; set; }
            public T Source { get; set; }
        }

        #endregion
        #region class: Cluster

        public class Cluster<T>
        {
            public float[] Center { get; set; }

            // these arrays are the same size
            public Sample<T>[] Items { get; set; }
            public float[] Item_DistSqr_FromCenter { get; set; }
        }

        private class Cluster_Building<T>
        {
            public float[] Center { get; set; }
            public List<(Sample<T> item, float distSqr)> Items { get; } = new List<(Sample<T>, float)>();
        }

        #endregion
        #region class: ElbowResult

        private class ElbowResult<T>
        {
            public int NumClusters { get; set; }
            public Cluster<T>[] Result { get; set; }
            public float SumSquares { get; set; }
        }

        #endregion
        #region class: ElbowRunStats

        public class ElbowRunStats<T>
        {
            public int MinK { get; set; }
            public int MaxK { get; set; }

            public int BestIndex { get; set; }

            public ElbowRunStats_Run<T>[] Runs { get; set; }
        }
        public class ElbowRunStats_Run<T>
        {
            public int k { get; set; }
            public float sse { get; set; }
            public float distance { get; set; }
            public Cluster<T>[] clusters { get; set; }
        }

        #endregion



        // TODO: need to make a function that goes farther than sqrt(count), draws results and shows the trend lines
        // try with pure random data, and also try with more realistic data to see which performs better

        // after seeing the results, returning the first dip won't always get the best one.  it's probably best to run
        // them all

        /// <summary>
        /// This does multiple kmeans and uses elbow method to return the one with the best number of clusters
        /// </summary>
        public static Cluster<T>[] DoClustering_ALL<T>(IList<Sample<T>> data)
        {
            // Determine the maximum number of clusters to test
            int maxK = Math.Max(2, (int)Math.Sqrt(data.Count));

            // Store (k, sse) pairs for all tested cluster counts
            var kSSEList = new List<(int k, float sse, Cluster<T>[] clusters)>();

            for (int k = 1; k <= maxK; k++)
            {
                // Perform clustering for k clusters (Assume a method DoClustering is available)
                Cluster<T>[] clusters = DoClustering(data, k);

                // Compute the total within-cluster sum of squares (WSS)
                float totalSSE = 0f;

                foreach (var cluster in clusters)
                    for (int i = 0; i < cluster.Item_DistSqr_FromCenter.Length; i++)
                        totalSSE += cluster.Item_DistSqr_FromCenter[i];

                kSSEList.Add((k, totalSSE, clusters));
            }

            // Extract first and last (k, sse) points for line reference
            var first = kSSEList[0];
            var last = kSSEList[kSSEList.Count - 1];
            int optimalK = 1;
            Cluster<T>[] optimal_clusters = first.clusters;
            float maxDistance = 0f;

            // Loop through all points except the first and last to find the maximum perpendicular distance
            for (int i = 1; i < kSSEList.Count - 1; i++)
            {
                var point = kSSEList[i];
                float x = point.k, y = point.sse;

                float x1 = first.k, y1 = first.sse;
                float x2 = last.k, y2 = last.sse;

                // Calculate perpendicular distance using line equation
                float numerator = (y2 - y1) * x - (x2 - x1) * y + (x2 * y1 - y2 * x1);
                float distance = Math.Abs(numerator);

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    optimalK = point.k;
                    optimal_clusters = point.clusters;
                }
            }

            // Fallback: if no elbow is found, use the last k (lowest SSE)
            if (maxDistance == 0)
            {
                optimalK = last.k;
                optimal_clusters = last.clusters;
            }

            // Return final clustering with the chosen optimal number of clusters
            return optimal_clusters;
        }
        public static ElbowRunStats<T> DoClustering_ALL2<T>(IList<Sample<T>> data)
        {
            // Determine the maximum number of clusters to test
            int maxK = Math.Max(2, (int)Math.Sqrt(data.Count));

            // Store (k, sse) pairs for all tested cluster counts
            //var kSSEList = new List<(int k, float sse, Cluster<T>[] clusters)>();
            var runs = new List<ElbowRunStats_Run<T>>();

            for (int k = 1; k <= maxK; k++)
            {
                // Perform clustering for k clusters (Assume a method DoClustering is available)
                Cluster<T>[] clusters = DoClustering(data, k);

                // Compute the total within-cluster sum of squares (WSS)
                float totalSSE = 0f;

                foreach (var cluster in clusters)
                    for (int i = 0; i < cluster.Item_DistSqr_FromCenter.Length; i++)
                        totalSSE += cluster.Item_DistSqr_FromCenter[i];

                runs.Add(new ElbowRunStats_Run<T>
                {
                    k = k,
                    sse = totalSSE,
                    clusters = clusters,
                });
            }

            // Extract first and last (k, sse) points for line reference
            var first = runs[0];
            var last = runs[runs.Count - 1];
            int optimalIndex = 0;
            float maxDistance = 0f;

            // Loop through all points except the first and last to find the maximum perpendicular distance
            for (int i = 1; i < runs.Count - 1; i++)
            {
                var point = runs[i];
                float x = point.k, y = point.sse;

                float x1 = first.k, y1 = first.sse;
                float x2 = last.k, y2 = last.sse;

                // Calculate perpendicular distance using line equation
                float numerator = (y2 - y1) * x - (x2 - x1) * y + (x2 * y1 - y2 * x1);
                float distance = Math.Abs(numerator);
                runs[i].distance = distance;

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    optimalIndex = i;
                }
            }

            // Fallback: if no elbow is found, use the last k (lowest SSE)
            if (maxDistance == 0)
                optimalIndex = runs.Count - 1;

            // Return final clustering with the chosen optimal number of clusters
            return new ElbowRunStats<T>
            {
                MinK = first.k,
                MaxK = last.k,
                BestIndex = optimalIndex,
                Runs = runs.ToArray(),
            };
        }
        public static Cluster<T>[] DoClustering_FIRST<T>(IList<Sample<T>> data)
        {
            // Determine the maximum number of clusters to test
            int maxK = Math.Max(2, (int)Math.Sqrt(data.Count));

            // Get first and last so that a line can be drawn from (1,first.sumsqr) to (maxK,last.sumsqr)
            var first = GetElbowSample(1, data);
            var last = GetElbowSample(maxK, data);

            float maxDistance = 0f;
            Cluster<T>[] optimal_clusters = first.Result;

            for (int i = 2; i < maxK; i++)
            {
                var middle = GetElbowSample(i, data);

                float distance = GetElbowDist(i, middle.SumSquares, first.NumClusters, first.SumSquares, last.NumClusters, last.SumSquares);

                if (distance < maxDistance)
                    return optimal_clusters;

                maxDistance = distance;
                optimal_clusters = middle.Result;
            }

            return last.Result;
        }




        public static Cluster<T>[] DoClustering<T>(IList<Sample<T>> data, int num_clusters)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (data.Count < num_clusters)
                num_clusters = data.Count;

            if (num_clusters <= 0)
                throw new ArgumentException($"Not enough clusters: {num_clusters}");

            // Initialize cluster centers with random samples
            var retVal = GetInitialClusters(data, num_clusters);

            // Keep shuffling until each cluster's item is closer to its center than other node centers
            while (true)
                if (!Cluster_Step(retVal, data, num_clusters))       // keep refining until the cluster centers stop moving
                    break;

            return BuildFinalReturn(retVal);
        }

        /// <summary>
        /// Returns the index of the cluster that this point is closest to
        /// </summary>
        public static int GetClusterIndex<T>(Cluster<T>[] clusters, float[] vector)
        {
            int nearest_index = 0;
            float min_distSqr = DistanceSqr(vector, clusters[0].Center);

            for (int i = 1; i < clusters.Length; i++)
            {
                float distSqr = DistanceSqr(vector, clusters[i].Center);
                if (distSqr < min_distSqr)
                {
                    min_distSqr = distSqr;
                    nearest_index = i;
                }
            }

            return nearest_index;
        }

        /// <summary>
        /// Makes new clusters, but with the items sorted so that index 0 is closest to center
        /// </summary>
        public static Cluster<T>[] SortItemsByDistFromCenters<T>(Cluster<T>[] result)
        {
            var retVal = new Cluster<T>[result.Length];

            for (int i = 0; i < result.Length; i++)
                retVal[i] = SortCluster(result[i]);

            return retVal;
        }

        #region Private Methods - elbow

        private static ElbowResult<T> GetElbowSample<T>(int num_clusters, IList<Sample<T>> data)
        {
            // Perform clustering for k clusters (Assume a method DoClustering is available)
            Cluster<T>[] clusters = DoClustering(data, num_clusters);

            // Compute the total within-cluster sum of squares (WSS)
            float totalSSE = 0f;

            foreach (var cluster in clusters)
                for (int i = 0; i < cluster.Item_DistSqr_FromCenter.Length; i++)
                    totalSSE += cluster.Item_DistSqr_FromCenter[i];

            return new ElbowResult<T>
            {
                NumClusters = num_clusters,
                Result = clusters,
                SumSquares = totalSSE,
            };
        }

        private static float GetElbowDist(int num_clusters, float sumSquares, int first_numclusters, float first_sumsqr, int last_numclusters, float last_sumsqr)
        {
            //var point = kSSEList[i];
            float x = num_clusters;
            float y = sumSquares;

            float x1 = first_numclusters;
            float y1 = first_sumsqr;

            float x2 = last_numclusters;
            float y2 = last_sumsqr;

            // Calculate perpendicular distance using line equation
            float numerator = (y2 - y1) * x - (x2 - x1) * y + (x2 * y1 - y2 * x1);
            float distance = Math.Abs(numerator);

            return distance;
        }

        #endregion
        #region Private Methods - kmeans

        private static Cluster_Building<T>[] GetInitialClusters<T>(IList<Sample<T>> data, int num_clusters)
        {
            var retVal = new Cluster_Building<T>[num_clusters];

            int[] sample_indices = UtilityCore.RandomRange(0, data.Count, num_clusters).ToArray();      // this return unique random indices.  DoClustering already made sure num_clusters <= data.Count

            for (int i = 0; i < num_clusters; i++)
            {
                retVal[i] = new Cluster_Building<T>
                {
                    Center = data[sample_indices[i]].Vector,
                };
            }

            return retVal.ToArray();
        }

        private static bool Cluster_Step<T>(Cluster_Building<T>[] clusters, IList<Sample<T>> data, int num_clusters)
        {
            var clusters_step = new Cluster_Building<T>[num_clusters];

            for (int i = 0; i < num_clusters; i++)
                clusters_step[i] = new Cluster_Building<T>
                {
                    Center = clusters[i].Center,
                };

            // Assign each data point to the nearest cluster
            foreach (var sample in data)
                AddToNearestCluster(sample, clusters_step, num_clusters);

            // Update clusters's centers and items according to what's in clusters_step
            bool changed = false;
            for (int i = 0; i < num_clusters; i++)
                changed |= UpdateClusterCenters(clusters, clusters_step, i);

            return changed;
        }

        /// <summary>
        /// Adds each sample to the best node in clusters_step
        /// </summary>
        private static void AddToNearestCluster<T>(Sample<T> sample, Cluster_Building<T>[] clusters_step, int num_clusters)
        {
            int nearest_index = 0;
            float min_distSqr = DistanceSqr(sample.Vector, clusters_step[0].Center);

            for (int i = 1; i < num_clusters; i++)
            {
                float distSqr = DistanceSqr(sample.Vector, clusters_step[i].Center);
                if (distSqr < min_distSqr)
                {
                    min_distSqr = distSqr;
                    nearest_index = i;
                }
            }

            clusters_step[nearest_index].Items.Add((sample, min_distSqr));
        }

        /// <summary>
        /// This sets clusters[index].center and makes items same as clusters_step[index].item
        /// also returns true if center moved
        /// </summary>
        private static bool UpdateClusterCenters<T>(Cluster_Building<T>[] clusters, Cluster_Building<T>[] clusters_step, int index)
        {
            clusters[index].Items.Clear();
            clusters[index].Items.AddRange(clusters_step[index].Items);

            if (clusters_step[index].Items.Count == 0)
                return false;

            // Figure out the center of this cluster
            float[] new_center = new float[clusters[index].Center.Length];
            for (int i = 0; i < new_center.Length; i++)
            {
                new_center[i] = 0f;

                foreach (var sample in clusters_step[index].Items)
                    new_center[i] += sample.item.Vector[i];

                new_center[i] /= clusters_step[index].Items.Count;
            }

            // See if that cluster differs from the existing
            if (!ArraysEqual(new_center, clusters[index].Center))
            {
                clusters[index].Center = new_center;
                return true;
            }

            return false;
        }

        private static Cluster<T>[] BuildFinalReturn<T>(Cluster_Building<T>[] clusters)
        {
            var retVal = new Cluster<T>[clusters.Length];

            for (int i = 0; i < clusters.Length; i++)
            {
                Sample<T>[] items = new Sample<T>[clusters[i].Items.Count];
                float[] item_distSqr_fromcenter = new float[clusters[i].Items.Count];

                for (int j = 0; j < items.Length; j++)
                {
                    items[j] = clusters[i].Items[j].item;
                    item_distSqr_fromcenter[j] = clusters[i].Items[j].distSqr;
                }

                retVal[i] = new Cluster<T>
                {
                    Center = clusters[i].Center,
                    Items = items,
                    Item_DistSqr_FromCenter = item_distSqr_fromcenter,
                };
            }

            return retVal;
        }

        private static Cluster<T> SortCluster<T>(Cluster<T> cluster)
        {
            // Ensure the arrays are valid
            if (cluster.Items == null || cluster.Item_DistSqr_FromCenter == null || cluster.Items.Length != cluster.Item_DistSqr_FromCenter.Length)
                throw new InvalidOperationException("Items and distances must be non-null and the same length");

            var retVal = new Cluster<T>()
            {
                Center = cluster.Center,
            };

            int n = cluster.Items.Length;

            // Generate array of indices
            int[] indices = new int[n];
            for (int i = 0; i < n; i++)
                indices[i] = i;

            // Sort indices based on the corresponding distance values
            Array.Sort(indices, (a, b) => cluster.Item_DistSqr_FromCenter[a].CompareTo(cluster.Item_DistSqr_FromCenter[b]));

            // Create new sorted arrays
            Sample<T>[] sortedItems = new Sample<T>[n];
            float[] sortedDistances = new float[n];

            for (int i = 0; i < n; i++)
            {
                sortedItems[i] = cluster.Items[indices[i]];
                sortedDistances[i] = cluster.Item_DistSqr_FromCenter[indices[i]];
            }

            // Store the sorted arrays
            retVal.Items = sortedItems;
            retVal.Item_DistSqr_FromCenter = sortedDistances;

            return retVal;
        }

        private static float DistanceSqr(float[] a, float[] b)
        {
            float sum = 0f;

            for (int i = 0; i < a.Length; i++)
                sum += (a[i] - b[i]) * (a[i] - b[i]);

            //return Mathf.Sqrt(sum);
            return sum;
        }

        private static bool ArraysEqual(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
                if (!a[i].IsNearValue(b[i]))
                    return false;

            return true;
        }

        #endregion
    }
}
