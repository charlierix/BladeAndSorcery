using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SocialPlatforms.Impl;
using static UnityEngine.Rendering.DebugUI.Table;
using static UnityEngine.UIElements.UxmlAttributeDescription;

namespace Jetpack.InputWatchers
{
    /// <summary>
    /// Keeps track of the user having consistent gaze
    /// </summary>
    /// <remarks>
    /// There are several ways that the user could be consistently looking:
    /// 
    /// - Direct: simply looking in the same direction over time (this would only be useful when the player is stationary)
    /// 
    /// - Offset: looking in a direction offset from the forward direction (as the player rotates, the gaze stays at that same angle away from forward)
    ///     stores the offset quaternion each frame
    /// 
    /// - Target: user is looking at some stationary target point while flying around
    ///     does raycasts against imaginary spheres to see if any have a hit that is being consistently fixated on
    /// </remarks>
    public class GazeBuffer
    {
        // NOTE: member variables are internal so GazeBufferVisualizer can see them.  otherwise they should be private

        #region struct: GazeSample

        internal interface IGazeSample
        {
            DateTime Timestamp { get; set; }
        }

        internal struct GazeSample_Direct : IGazeSample
        {
            public Vector3 Direction { get; set; }
            public DateTime Timestamp { get; set; }
        }

        internal struct GazeSample_Offset : IGazeSample
        {
            public Quaternion Quaternion { get; set; }

            public Vector3 Axis { get; set; }       // these are copied from quat for convenience
            public float Angle { get; set; }

            public DateTime Timestamp { get; set; }
        }

        internal struct GazeSample_SphereTarget : IGazeSample
        {
            public Vector3 SphereOrigin { get; set; }
            public float SphereRadius { get; set; }
            public Vector3 Hit { get; set; }

            public DateTime Timestamp { get; set; }
        }

        #endregion
        #region class: FrameSkip

        private class FrameSkip
        {
            public int BasedOnCount { get; set; }
            public float BasedOnSeconds { get; set; }

            public double Milliseconds_Between_Frames { get; set; }

            public DateTime NextFrameTime { get; set; }
        }

        #endregion

        #region Declaration Section

        private const float CLEANUP_RADIUS_ORIGIN_SECONDS = 0.5f;
        private const int MAX_PER_RADIUS = 1;       // how many spheres at each radius.  tried with 2, but that seemed like overkill (multi radius, a few extra while flying since cleanup isn't immediate)

        private readonly List<GazeSample_Direct> _direct = new List<GazeSample_Direct>();
        internal readonly List<GazeSample_Offset> _offset = new List<GazeSample_Offset>();
        internal readonly Dictionary<string, Dictionary<string, List<GazeSample_SphereTarget>>> _target = new Dictionary<string, Dictionary<string, List<GazeSample_SphereTarget>>>();

        private FrameSkip _frameskip_direct = null;
        private FrameSkip _frameskip_offset = null;
        private FrameSkip _frameskip_target = null;

        private DateTime _prev_cleanup = DateTime.MinValue;

        #endregion

        // Update the buffer with new frame data
        // NOTE: directions must be unit vectors
        public void AddSample_Direct(Vector3 direction)
        {
            DateTime now = DateTime.UtcNow;

            if (!EstimateFrameSkip(ref _frameskip_direct, now))
                return;

            // Remove old samples exceeding the window
            RemoveOldEntries(_direct, now);

            // Add new sample
            _direct.Add(new GazeSample_Direct
            {
                Direction = direction,
                Timestamp = now,
            });
        }
        public void AddSample_Offset(Vector3 direction, Vector3 relativeTo)
        {
            DateTime now = DateTime.UtcNow;

            if (!EstimateFrameSkip(ref _frameskip_offset, now))
                return;

            // Remove old samples exceeding the window
            RemoveOldEntries(_offset, now);

            // Add new sample
            Quaternion quat = Quaternion.FromToRotation(relativeTo, direction);
            quat.ToAngleAxis(out float angle, out Vector3 axis);

            _offset.Add(new GazeSample_Offset
            {
                Quaternion = quat,
                Axis = axis,
                Angle = angle,
                Timestamp = now,
            });
        }
        public void AddSample_Target(Vector3 pos, Vector3 direction, float speed)
        {
            DateTime now = DateTime.UtcNow;

            if (!EstimateFrameSkip(ref _frameskip_target, now))
                return;

            // No need to do origin cleanup every frame
            bool should_cleanup = ShouldCleanupRadiiOrigins(now);

            float[] radii = GetRadiiForSpeed(speed);

            if (should_cleanup)
                RemoveUnusedRadii(radii);       // remove any buckets that aren't in this set of radii

            foreach (float radius in radii)
            {
                string radius_key = GetRadiusKey(radius);

                if (!_target.TryGetValue(radius_key, out var by_origin))
                {
                    by_origin = new Dictionary<string, List<GazeSample_SphereTarget>>();
                    _target.Add(radius_key, by_origin);
                }

                //float spacing = JetpackScript.YawToLook_GazeTarget_SpacingRatio;

                // not so simple, this ratio made 225 out of 3000 samples with 0 origins
                // NOTE: after running the numbers, this shouldn't be an independent slider, it can be a simple
                // y=mx+b.  This will produce between 1 and 2 spheres.  Radius doesn't have much influence
                //float mindist_percentof_radius = JetpackScript.YawToLook_GazeTarget_MinAllowedDistance;
                //float mindist_percentof_radius = -0.5721f * spacing + 0.8997f;

                // this gives between 1 and 4 origins (slight chance of 0)
                float spacing = 0.65f;
                float mindist_percentof_radius = 0.45f;

                Vector3[] origins = GetRelevantSphereOrigins(pos, radius * spacing, radius, radius * mindist_percentof_radius);
                if (origins.Length == 0)
                    origins = GetRelevantSphereOrigins(pos, radius * spacing, radius, radius * 0.2f);       // using a smaller min dist from surface of sphere to make sure there is something returned

                // keep N closest spheres (it gets inefficient with too many spheres)
                var origins_keys = KeepNClosest(origins, pos, MAX_PER_RADIUS, by_origin);

                if (should_cleanup)
                    RemoveUnusedOrigins(by_origin, origins);      // remove any buckets that aren't in this set of origins

                foreach (var origin in origins_keys)
                {
                    if (!by_origin.TryGetValue(origin.key, out var origin_bucket))
                    {
                        origin_bucket = new List<GazeSample_SphereTarget>();
                        by_origin.Add(origin.key, origin_bucket);
                    }

                    RemoveOldEntries(origin_bucket, now);

                    origin_bucket.Add(new GazeSample_SphereTarget
                    {
                        SphereOrigin = origin.origin,
                        SphereRadius = radius,
                        Hit = SphereExitPoint(origin.origin, radius, pos, direction),
                        Timestamp = now,
                    });
                }
            }
        }

        // Get current dominant look direction and confidence
        public bool TryGetDominantDirection_Direct(out Vector3 dominant_direction, out float confidence)
        {
            dominant_direction = Vector3.zero;
            confidence = 0f;

            if (_direct.Count < 2)
                return false;

            // Calculate confidence
            float confidence2 = CalculateDirectionConfidence(_direct);

            if (confidence2 > JetpackScript.YawToLook_Buffer_GazeConfidence)
            {
                dominant_direction = GetWeightedAverage(_direct);
                confidence = confidence2;
                return true;
            }

            return false;
        }
        public bool TryGetDominantDirection_Offset(out Vector3 dominant_direction, out float confidence, Vector3 relativeTo)
        {
            dominant_direction = Vector3.zero;
            confidence = 0f;

            if (_offset.Count < 2)
                return false;

            // Calculate confidence
            float confidence2 = CalculateDirectionConfidence(_offset);

            if (confidence2 > JetpackScript.YawToLook_Buffer_GazeConfidence)
            {
                dominant_direction = GetWeightedAverage(_offset, relativeTo);
                confidence = confidence2;
                return true;
            }

            return false;
        }
        public bool TryGetDominantDirection_Target(out Vector3 dominant_direction, out float confidence, Vector3 pos)
        {
            dominant_direction = Vector3.zero;
            confidence = 0f;
            bool found_one = false;

            // Iterate over all buckets, finding the strongest result and return that
            foreach (var bucket in IterateTargetBuckets())
            {
                float confidence2 = CalculateDirectionConfidence(bucket, pos);      // may not need to pass in pos

                if (confidence2 < JetpackScript.YawToLook_Buffer_GazeConfidence)
                    continue;

                if (confidence2 < confidence)
                    continue;

                found_one = true;
                confidence = confidence2;
                dominant_direction = GetWeightedAverage(bucket, pos);
            }

            return found_one;
        }
        internal bool TryGetDominantDirection_Target_Debug(out Vector3 dominant_direction, out float confidence, out float sphere_radius, out Vector3 sphere_origin, Vector3 pos)
        {
            dominant_direction = Vector3.zero;
            confidence = 0f;
            sphere_radius = 0f;
            sphere_origin = Vector3.zero;
            bool found_one = false;

            // Iterate over all buckets, finding the strongest result and return that
            foreach (var bucket in IterateTargetBuckets())
            {
                float confidence2 = CalculateDirectionConfidence(bucket, pos);      // may not need to pass in pos

                if (confidence2 < JetpackScript.YawToLook_Buffer_GazeConfidence)
                    continue;

                if (confidence2 < confidence)
                    continue;

                found_one = true;
                confidence = confidence2;
                sphere_radius = bucket[0].SphereRadius;
                sphere_origin = bucket[0].SphereOrigin;
                dominant_direction = GetWeightedAverage(bucket, pos);
            }

            return found_one;
        }

        public void Clear()
        {
            _direct.Clear();
            _offset.Clear();
        }

        #region Private Methods - AddSample

        private static bool EstimateFrameSkip(ref FrameSkip frameskip, DateTime now)
        {
            int count = JetpackScript.YawToLook_Buffer_MaxCount;
            float seconds = JetpackScript.YawToLook_Buffer_MaxSeconds;

            if (frameskip == null || frameskip.BasedOnCount != count || frameskip.BasedOnSeconds != seconds)
            {
                frameskip = new FrameSkip()
                {
                    BasedOnCount = count,
                    BasedOnSeconds = seconds,
                    Milliseconds_Between_Frames = GetMillisecondsBetweenFrames(count, seconds),
                };

                Debug.Log($"frameskip.Milliseconds_Between_Frames: {frameskip.Milliseconds_Between_Frames} (count: {count}, seconds: {seconds})");

                frameskip.NextFrameTime = now.AddMilliseconds(frameskip.Milliseconds_Between_Frames);

                return true;
            }

            if (now < frameskip.NextFrameTime)
                return false;

            frameskip.NextFrameTime = now.AddMilliseconds(frameskip.Milliseconds_Between_Frames);

            return true;
        }
        private static double GetMillisecondsBetweenFrames(int count, float seconds)
        {
            double retVal = 1000d * seconds / count;

            // Reduce it a bit, because framerate won't be perfect.  This will allow for some hiccups and still get close to ideal count
            retVal *= 0.9167;

            return retVal;
        }

        private static void RemoveOldEntries<T>(IList<T> items, DateTime now) where T : IGazeSample
        {
            DateTime min_time = now - TimeSpan.FromSeconds(JetpackScript.YawToLook_Buffer_MaxSeconds);

            while (items.Count > 0)
            {
                if (items[0].Timestamp > min_time)      // they are stored in ascending time order, so once one is too new, everything after will be as well
                    return;

                items.RemoveAt(0);
            }
        }

        #endregion
        #region Private Methods - AddSample - spheres

        private bool ShouldCleanupRadiiOrigins(DateTime now)
        {
            if ((now - _prev_cleanup).TotalSeconds < CLEANUP_RADIUS_ORIGIN_SECONDS)
                return false;

            _prev_cleanup = now;

            return true;
        }
        private void RemoveUnusedRadii(float[] radii)
        {
            // Convert radius into key
            string[] radii_keys = new string[radii.Length];
            for (int i = 0; i < radii.Length; i++)
                radii_keys[i] = GetRadiusKey(radii[i]);

            // Remove any radius that is not in the list passed in
            foreach (string key in _target.Keys.Except(radii_keys).ToArray())
                _target.Remove(key);
        }
        private void RemoveUnusedOrigins(Dictionary<string, List<GazeSample_SphereTarget>> by_origin, Vector3[] origins)
        {
            // Convert origin into key
            string[] origin_keys = new string[origins.Length];
            for (int i = 0; i < origins.Length; i++)
                origin_keys[i] = GetOriginKey(origins[i]);

            // Remove any origin that is not in the list passed in
            foreach (string key in by_origin.Keys.Except(origin_keys).ToArray())
                by_origin.Remove(key);
        }

        private static Vector3[] GetRelevantSphereOrigins(Vector3 pos, float originSpacing, float sphereRadius, float minAllowedDistance)
        {
            float maxDistanceFromOrigin = sphereRadius - minAllowedDistance;

            if (maxDistanceFromOrigin < 0f)
                return new Vector3[0]; // No valid spheres when threshold is too high

            float maxDistanceSq = maxDistanceFromOrigin * maxDistanceFromOrigin;

            // Calculate the range of grid indices to check
            int minX = Mathf.FloorToInt((pos.x - maxDistanceFromOrigin) / originSpacing);
            int maxX = Mathf.CeilToInt((pos.x + maxDistanceFromOrigin) / originSpacing);
            int minY = Mathf.FloorToInt((pos.y - maxDistanceFromOrigin) / originSpacing);
            int maxY = Mathf.CeilToInt((pos.y + maxDistanceFromOrigin) / originSpacing);
            int minZ = Mathf.FloorToInt((pos.z - maxDistanceFromOrigin) / originSpacing);
            int maxZ = Mathf.CeilToInt((pos.z + maxDistanceFromOrigin) / originSpacing);

            var retVal = new List<Vector3>();

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        Vector3 origin = new Vector3(x * originSpacing, y * originSpacing, z * originSpacing);
                        float dx = origin.x - pos.x;
                        float dy = origin.y - pos.y;
                        float dz = origin.z - pos.z;
                        float distanceSq = dx * dx + dy * dy + dz * dz;

                        if (distanceSq <= maxDistanceSq)
                            retVal.Add(origin);
                    }
                }
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// This gives priority to origins that are already created, else uses new ones.  Then sorts by distance from user
        /// </summary>
        private static (Vector3 origin, string key)[] KeepNClosest(Vector3[] origins, Vector3 pos, int count, Dictionary<string, List<GazeSample_SphereTarget>> by_origin)
        {
            // Split into what's in and not in the by_origin bucket
            var in_list = new List<(Vector3 origin, string key)>();
            var out_list = new List<(Vector3 origin, string key)>();

            foreach (var origin in origins)
            {
                string key = GetOriginKey(origin);

                if (by_origin.ContainsKey(key))
                    in_list.Add((origin, key));
                else
                    out_list.Add((origin, key));
            }

            // If total doesn't exceed max, then return those
            if (in_list.Count + out_list.Count <= count)
                return in_list.Concat(out_list).ToArray();

            // If there's exactly enough from the in list, return those without needing to do a sort
            if (in_list.Count == count)
                return in_list.ToArray();

            // Define the sorting and take N function
            var takesorted = new Func<IEnumerable<(Vector3 origin, string key)>, int, (Vector3 origin, string key)[]>((list, cnt) =>
                list.Select(o => new
                {
                    origin = o,
                    dist_sqr = (pos - o.origin).sqrMagnitude,
                }).
                OrderBy(o => o.dist_sqr).
                Take(cnt).
                Select(o => o.origin).
                ToArray());

            // If there's too many in the in list, only take from that
            if (in_list.Count > count)
                return takesorted(in_list, count);

            // If execution gets here, then some or all are needed from the out list

            // Get what will be needed from the out list
            var outs = takesorted(out_list, count - in_list.Count);

            // Return the combined
            return in_list.Concat(outs).ToArray();
        }

        private static Vector3 SphereExitPoint(Vector3 origin, float radius, Vector3 ray_start, Vector3 ray_direction)
        {
            Vector3 v = ray_start - origin;
            float b = 2 * Vector3.Dot(ray_direction, v);
            float c = Vector3.Dot(v, v) - radius * radius;

            // Ray origin is on or outside the sphere → invalid
            if (c >= 0f)
                throw new ArgumentException("Ray origin is on or outside the sphere.");

            float discriminant = b * b - 4 * c;
            if (discriminant < 0f)
                throw new InvalidOperationException("No real roots (sphere-ray miss).");

            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float t1 = (-b - sqrtDiscriminant) / 2;
            float t2 = (-b + sqrtDiscriminant) / 2;

            // Take the exit point (positive t)
            float t = Mathf.Max(t1, t2);

            // t should always be > 0 (since c < 0 and discriminant >= 0)
            return ray_start + ray_direction * t;
        }

        /// <summary>
        /// Returns 3 different radius (small, medium, large) that make sense for the speed
        /// </summary>
        /// <remarks>
        /// The radii returned are multiples of MIN radius, so the tostring of radius can be used as
        /// a key into a bucket
        /// </remarks>
        private static float[] GetRadiiForSpeed(float speed)
        {
            float MIN = JetpackScript.YawToLook_GazeTarget_RadiiForSpeed_Min;                       // Minimum base radius
            float SPEED_RATIO = JetpackScript.YawToLook_GazeTarget_RadiiForSpeed_SpeedRatio;        // Speed to base radius scaling factor
            float MULT = JetpackScript.YawToLook_GazeTarget_RadiiForSpeed_StepMult;                 // Multiplier for step progression

            // Step 1: Calculate the base radius based on speed
            float calculatedBase = Math.Max(MIN, SPEED_RATIO * speed);

            // Step 2: Find the next power of MULT that is >= calculatedBase
            float ratio = calculatedBase / MIN;     // Normalize to the MIN base
            double logBase2 = Math.Log(ratio, 2);   // Log base 2 of the ratio
            int n = (int)Math.Ceiling(logBase2);    // Smallest integer exponent
            float baseRadius = MIN * (float)Math.Pow(MULT, n);

            // Step 3: Return the radii as a geometric progression
            return new float[]
            {
                baseRadius,
                baseRadius * MULT,
                //baseRadius * MULT * MULT      // having three sizes seems excessive
            };
        }

        internal static string GetRadiusKey(float radius) => radius.ToStringSignificantDigits(3);
        internal static string GetOriginKey(Vector3 origin) => origin.ToStringSignificantDigits(3);

        #endregion

        #region Private Methods - TryGetDominantDirection

        // Calculate confidence based on direction consistency
        private static float CalculateDirectionConfidence(List<GazeSample_Direct> samples)
        {
            Vector3 average = GetWeightedAverage(samples);

            if (average.IsNearZero())
                return 0;

            average = average.normalized;
            float confidence = 1f;

            foreach (var sample in samples)
            {
                float normalized_dot = (Vector3.Dot(sample.Direction, average) + 1f) / 2f;      // sample.Direction is normalized, no need to do it again
                confidence = Mathf.Min(confidence, normalized_dot);
            }

            float time_percent = GetTimePercent(samples[0].Timestamp);

            return confidence * time_percent;
        }
        // Calculates confidence based on axis and angle similarity between samples
        private static float CalculateDirectionConfidence(List<GazeSample_Offset> samples)
        {
            if (samples.Count == 0)
                return 0f;

            GazeSample_Offset referenceSample = samples[0];
            Vector3 referenceAxis = referenceSample.Axis;
            float referenceAngle = referenceSample.Angle;

            float minConfidence = 1f;

            for (int i = 0; i < samples.Count; i++)
            {
                GazeSample_Offset sample = samples[i];

                // Axis confidence: dot product between axis and reference axis
                float axisDot = Vector3.Dot(sample.Axis, referenceAxis);
                axisDot = (axisDot + 1f) / 2f; // Normalize to 0..1

                // Angle confidence: difference between angle and reference angle
                float angleDiff = Mathf.Abs(sample.Angle - referenceAngle) / 180f;
                float angleConfidence = 1f - angleDiff;

                // Take the minimum of axis and angle confidence for this sample
                float sampleConfidence = Mathf.Min(axisDot, angleConfidence);

                // Track the lowest confidence across all samples
                if (sampleConfidence < minConfidence)
                    minConfidence = sampleConfidence;
            }

            float time_percent = GetTimePercent(samples[0].Timestamp);

            return minConfidence * time_percent;
        }


        // Calculates confience by subracting hit from pos, then very similar to direct overload
        private static float CalculateDirectionConfidence_ATTEMPT1(List<GazeSample_SphereTarget> samples, Vector3 pos)
        {
            if (samples.Count == 0)
                return 0f;

            Vector3 average = GetWeightedAverage(samples, pos);

            if (average.IsNearZero())
                return 0;

            average = average.normalized;
            float confidence = 1f;

            foreach (var sample in samples)
            {
                Vector3 direction = (sample.Hit - pos).normalized;
                float normalized_dot = (Vector3.Dot(direction, average) + 1f) / 2f;

                confidence = Mathf.Min(confidence, normalized_dot);
            }

            float time_percent = GetTimePercent(samples[0].Timestamp);

            Debug.Log($"confidence: {confidence}, time_percent: {time_percent}, max time: {(DateTime.UtcNow - samples[0].Timestamp).TotalSeconds}");

            return confidence * time_percent;
        }
        private static float CalculateDirectionConfidence(List<GazeSample_SphereTarget> samples, Vector3 pos)
        {
            if (samples.Count == 0)
                return 0f;

            float time_percent = GetTimePercent(samples[0].Timestamp);

            if (time_percent < JetpackScript.YawToLook_Buffer_GazeConfidence)        // even if all the hits are perfectly aligned, the low amount of time they are around won't make it worth calculating
                return 0f;

            Vector3 avg_dir = GetWeightedAverage(samples, pos);

            if (avg_dir.IsNearZero())
                return 0f;

            avg_dir = avg_dir.normalized;

            // Compute normalized dot products
            float[] normalized_dots = new float[samples.Count];
            for (int i = 0; i < samples.Count; i++)
            {
                Vector3 direction = (samples[i].Hit - pos).normalized;
                float dot = Vector3.Dot(direction, avg_dir);
                normalized_dots[i] = (dot + 1f) / 2f; // Map to [0,1]
            }

            // Compute average confidence
            float sum_confidence = normalized_dots.Sum();
            float avg_confidence = sum_confidence / samples.Count;

            // Compute standard deviation
            float sum_squared_diff = 0f;
            foreach (float dot in normalized_dots)
            {
                float diff = dot - avg_confidence;
                sum_squared_diff += diff * diff;
            }

            float variance = sum_squared_diff / samples.Count;
            float std_dev = Mathf.Sqrt(variance);





            // Normalize standard deviation to [0,1]
            const float MAX_STDDEV = 0.5f;
            //float normalized_stddev = 1f - (std_dev / MAX_STDDEV);

            // Your current normalization (normalized_stddev = 1 - std_dev) is linear. Replace it with a non-linear function to make
            // small standard deviations (tight clusters) dominate the confidence

            // Exponential decay for normalized standard deviation
            float normalized_stddev = Mathf.Exp(-std_dev * JetpackScript.YawToLook_Buffer_Confidence_StdDev_DecayMult); // Aggressive drop for std_dev > 0.001 (even a value of 12 is pretty aggressive - add a slider for this)




            // Combine average and normalized standard deviation
            float confidence = avg_confidence * normalized_stddev;


            // both of these seem touchy.  I think the std dev adjustment should be enough

            // Non-Linear Activation for Final Confidence
            // Use a sigmoid - like function to compress the final confidence score.This ensures:
            // - Only very tight clusters(e.g., avg_confidence > 0.995) yield high confidence.
            // - Gradually penalizes clusters with slightly higher spread.

            // Apply sigmoid-like scaling to final confidence
            //confidence = 1f - (1f / (1f + Mathf.Exp(100f * (confidence - 0.995f))));      // these numbers are off.  it needs to shift left

            // Or use a logarithmic transformation
            //confidence = Mathf.Log(confidence) / Mathf.Log(0.999f);




            Debug.Log($"final: {confidence * time_percent}, confidence: {confidence}, time_percent: {time_percent}, std_dev: {std_dev}, avg_confidence: {avg_confidence}, normalized_stddev: {normalized_stddev} (decay mult: {JetpackScript.YawToLook_Buffer_Confidence_StdDev_DecayMult})");

            // Reduce if the bucket is too new
            return confidence * time_percent;
        }



        private static float GetTimePercent(DateTime oldest)
        {
            float max_seconds = JetpackScript.YawToLook_Buffer_MaxSeconds;

            float elapsed = (float)(DateTime.UtcNow - oldest).TotalSeconds;

            if (elapsed >= max_seconds)
                return 1;

            return (float)Math.Pow(elapsed / max_seconds, 4);       // linear is too forgiving.  wait until it's closer to full before giving a higher score
        }

        private static Vector3 GetWeightedAverage(List<GazeSample_Direct> samples)
        {
            if (samples.Count == 0)
                return Vector3.zero;

            float totalWeight = 0f;
            Vector3 weightedSum = Vector3.zero;

            foreach (var sample in samples)
            {
                float weight = 1f - Vector3.Magnitude(sample.Direction - samples[0].Direction);     // Simple weighting
                weightedSum += sample.Direction * weight;
                totalWeight += weight;
            }

            return totalWeight > 0f ?
                weightedSum / totalWeight :
                Vector3.zero;
        }
        private static Vector3 GetWeightedAverage(List<GazeSample_Offset> samples, Vector3 relativeTo)
        {
            if (samples.Count == 0)
                return Vector3.zero;

            Vector4 sumQuat = Vector4.zero;
            float totalWeight = 0f;

            GazeSample_Offset referenceSample = samples[0];
            Vector3 referenceAxis = referenceSample.Axis;
            float referenceAngle = referenceSample.Angle;

            for (int i = 0; i < samples.Count; i++)
            {
                GazeSample_Offset sample = samples[i];

                // Calculate axis confidence (normalized to 0..1)
                float axisDot = Vector3.Dot(sample.Axis, referenceAxis);
                axisDot = (axisDot + 1f) / 2f;

                // Calculate angle confidence (normalized to 0..1)
                float angleDiff = Mathf.Abs(sample.Angle - referenceAngle) / 180f;
                float angleConfidence = 1f - angleDiff;

                // Weight is the product of axis and angle confidence
                float weight = axisDot * angleConfidence;

                // Add to sum of quaternions weighted by their confidence
                sumQuat += new Vector4(samples[i].Quaternion.x * weight, samples[i].Quaternion.y * weight, samples[i].Quaternion.z * weight, samples[i].Quaternion.w * weight);
                totalWeight += weight;
            }

            if (totalWeight == 0f)
                return Vector3.zero;

            // Normalize the sum to get the average quaternion
            Vector4 avgQuat = sumQuat / totalWeight;
            Quaternion averageQ = new Quaternion(avgQuat.x, avgQuat.y, avgQuat.z, avgQuat.w).normalized;

            // Apply the average rotation to the base direction
            return averageQ * relativeTo;
        }
        private static Vector3 GetWeightedAverage(List<GazeSample_SphereTarget> samples, Vector3 pos)
        {
            if (samples.Count == 0)
                return Vector3.zero;

            float totalWeight = 0f;
            Vector3 weightedSum = Vector3.zero;

            Vector3 direction0 = (samples[0].Hit - pos).normalized;

            for (int i = 0; i < samples.Count; i++)
            {
                Vector3 direction = i > 0 ?
                    (samples[i].Hit - pos).normalized :
                    direction0;

                float weight = 1f - Vector3.Magnitude(direction - direction0);     // Simple weighting
                weightedSum += direction * weight;
                totalWeight += weight;
            }

            return totalWeight > 0f ?
                weightedSum / totalWeight :
                Vector3.zero;
        }

        #endregion
        #region Private Methods - TryGetDominantDirection - spheres

        private IEnumerable<List<GazeSample_SphereTarget>> IterateTargetBuckets()
        {
            foreach (var by_radius in _target.Values)
                foreach (var bucket in by_radius.Values)
                    yield return bucket;
        }

        #endregion
    }
}
