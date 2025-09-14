using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

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
        #region struct: GazeSample

        private interface IGazeSample
        {
            DateTime Timestamp { get; set; }
        }

        private struct GazeSample_Direct : IGazeSample
        {
            public Vector3 Direction { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private struct GazeSample_Offset : IGazeSample
        {
            public Quaternion Quaternion { get; set; }

            public Vector3 Axis { get; set; }       // these are copied from quat for convenience
            public float Angle { get; set; }

            public DateTime Timestamp { get; set; }
        }

        private struct GazeSample_SphereTarget : IGazeSample
        {
            public Vector3 SphereOrigin { get; set; }
            public float SphereRadius { get; set; }
            public Vector3 Hit { get; set; }

            public DateTime Timestamp { get; set; }
        }

        #endregion

        #region Declaration Section

        private readonly List<GazeSample_Direct> _direct = new List<GazeSample_Direct>();
        private readonly List<GazeSample_Offset> _offset = new List<GazeSample_Offset>();

        private readonly Dictionary<string, Dictionary<string, List<GazeSample_SphereTarget>>> _target = new Dictionary<string, Dictionary<string, List<GazeSample_SphereTarget>>>();

        private DateTime _prevRadiusCleanup = DateTime.MinValue;

        #endregion

        // Update the buffer with new frame data
        // NOTE: directions must be unit vectors
        public void AddSample_Direct(Vector3 direction)
        {
            DateTime now = DateTime.UtcNow;

            // Remove old samples exceeding the window
            RemoveOldEntries(_direct, now);

            // Add new sample
            _direct.Add(new GazeSample_Direct
            {
                Direction = direction,
                Timestamp = now,
            });

            // Keep size controlled
            RemoveExcessEntries(_direct);
        }
        public void AddSample_Offset(Vector3 direction, Vector3 relativeTo)
        {
            DateTime now = DateTime.UtcNow;

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

            // Keep size controlled
            RemoveExcessEntries(_offset);
        }
        public void AddSample_Target(Vector3 pos, Vector3 direction, float speed)
        {
            const float MIN_ALLOWED_DISTANCE = 1;
            const float SPACING_RATIO = 0.33f;      // spacing = radius * ratio

            float[] radii = GetRadiiForSpeed(speed);

            // remove any buckets that aren't in this set of radii
            RemoveUnusedRadii(radii);

            foreach (float radius in radii)
            {
                string radius_key = GetRadiusKey(radius);

                if (!_target.TryGetValue(radius_key, out var by_origin))
                {
                    by_origin = new Dictionary<string, List<GazeSample_SphereTarget>>();
                    _target.Add(radius_key, by_origin);
                }

                Vector3[] origins = GetRelevantSphereOrigins(pos, radius * SPACING_RATIO, radius, MIN_ALLOWED_DISTANCE);

                foreach (Vector3 origin in origins)
                {
                    string origin_key = GetOriginKey(origin);

                    if (!by_origin.TryGetValue(origin_key, out var origin_bucket))
                    {
                        origin_bucket = new List<GazeSample_SphereTarget>();
                        by_origin.Add(origin_key, origin_bucket);
                    }

                    DateTime now = DateTime.UtcNow;
                    RemoveOldEntries(origin_bucket, now);

                    origin_bucket.Add(new GazeSample_SphereTarget
                    {
                        SphereOrigin = origin,
                        SphereRadius = radius,
                        Hit = SphereExitPoint(origin, radius, pos, direction),
                        Timestamp = now,
                    });

                    RemoveExcessEntries(_direct);
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

        public void Clear()
        {
            _direct.Clear();
            _offset.Clear();
        }

        #region Private Methods

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
        private static void RemoveExcessEntries<T>(IList<T> items) where T : IGazeSample
        {
            int max_count = JetpackScript.YawToLook_Buffer_MaxCount;

            while (items.Count > max_count)
                items.RemoveAt(0);        // zero is oldest entry
        }

        private void RemoveUnusedRadii(float[] radii)
        {
            // No need to do this cleanup every frame
            DateTime now = DateTime.UtcNow;

            if ((now - _prevRadiusCleanup).TotalSeconds < 1)
                return;

            _prevRadiusCleanup = now;

            // Convert radius into key
            string[] radii_keys = new string[radii.Length];
            for (int i = 0; i < radii.Length; i++)
                radii_keys[i] = GetRadiusKey(radii[i]);

            // Remove any radius that is not in the list passed in
            foreach (string key in _target.Keys.Except(radii_keys).ToArray())
                _target.Remove(key);
        }

        private static string GetRadiusKey(float radius) => radius.ToStringSignificantDigits(1);
        private static string GetOriginKey(Vector3 origin) => origin.ToStringSignificantDigits(1);

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

            return confidence;
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

            return minConfidence;
        }
        // Calculates confience by subracting hit from pos, then very similar to direct overload
        private static float CalculateDirectionConfidence(List<GazeSample_SphereTarget> samples, Vector3 pos)
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
                Vector3 direction = (pos - sample.Hit).normalized;
                float normalized_dot = (Vector3.Dot(direction, average) + 1f) / 2f;
                confidence = Mathf.Min(confidence, normalized_dot);
            }

            return confidence;
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
            const float MIN = 3;                    // Minimum base radius
            const float SPEED_RATIO = 0.5f;         // Speed to base radius scaling factor
            const float MULT = 2;                   // Multiplier for step progression

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

        private IEnumerable<List<GazeSample_SphereTarget>> IterateTargetBuckets()
        {
            foreach (var by_radius in _target.Values)
                foreach (var bucket in by_radius.Values)
                    yield return bucket;
        }

        #endregion
    }
}
