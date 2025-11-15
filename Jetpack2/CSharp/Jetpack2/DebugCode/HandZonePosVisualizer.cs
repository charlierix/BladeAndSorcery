using Jetpack2.Core;
using Jetpack2.InputWatchers;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.DebugCode
{
    public class HandZonePosVisualizer
    {
        #region class: Sample

        // These are relative to middle point, oriented along body forward/up
        private class Sample
        {
            public Vector3 Pos_Left { get; set; }
            public Vector3 Pos_Right { get; set; }

            public Vector3 PosNormalized_Left { get; set; }
            public Vector3 PosNormalized_Right { get; set; }
        }

        #endregion

        #region Declaration Section

        private const float DOT_SIZE = 0.008f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private DateTime _last_sample_added = DateTime.UtcNow;
        private List<Sample> _samples = new List<Sample>();

        private DebugRenderer3D _renderer = null;

        private int _dot_index = -1;
        private List<DebugItem> _dots = new List<DebugItem>();       // these are dots (showing contents of buffer)

        private DebugItem _sample_stats = null;

        #endregion

        public void Clear()
        {
            _samples.Clear();
            ClearDebugVisuals();
        }

        public void Update(Vector3 body_forward, Vector3 body_up)
        {
            if (!UIModOptions.VisualizeHandZonePositions)
                return;

            var positions = UtilJetpack.GetPlayerPoints(body_forward, body_up);

            // clear / add dots
            if (InputUtil.IsButtonPressed(Side.Left, PlayerControl.Hand.Button.AlternateUse))
                _samples.Clear();
            else if (InputUtil.IsButtonPressed(Side.Right, PlayerControl.Hand.Button.AlternateUse))
                MaybeAddHandSamples(positions);

            // ------ DRAWING ------

            PrepareForDraw();

            // a note saying right alt to add dots, left alt to clear dots

            DrawHandDots(positions.Transform_ToWorld);

            // text summary of the dots
            DrawSampleSummary(positions.height);


            // body forward


            FinishedDraw();
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

            if (_sample_stats != null)
            {
                _renderer.Remove(_sample_stats);
                _sample_stats = null;
            }

            foreach (DebugItem item in _dots)
                _renderer.Remove(item);
            _dots.Clear();

            _renderer = null;
        }

        // These manage visuals that persist across frames.  Prepare removes despawned, finsih sets visibility based on how many are used this frame
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

        private void DrawHandDots(Func<Vector3, Vector3> toWorld)
        {
            EnsureDebugActive();

            foreach (var sample in _samples)
            {
                AddDot(toWorld(sample.Pos_Left), UtilityColor.FromHex("3685E0"));
                AddDot(toWorld(sample.Pos_Right), UtilityColor.FromHex("E3AA39"));
            }
        }

        private void DrawSampleSummary(float player_height)
        {
            EnsureDebugActive();

            if (_samples.Count == 0)
            {
                if (_sample_stats != null)
                {
                    _renderer.Remove(_sample_stats);
                    _sample_stats = null;
                }

                return;
            }

            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                Player.local.head.transform.right * -0.35f +
                Player.local.head.transform.up * -0.25f;


            // analyze left and right separately

            // avg center
            // aabb (this should work pretty well since they are rotated into local coords, so x,y,z have consistent meanings)

            // count

            Vector3[] left_normalized = new Vector3[_samples.Count];
            Vector3[] right_normalized = new Vector3[_samples.Count];

            for (int i = 0; i < _samples.Count; i++)
            {
                left_normalized[i] = _samples[i].PosNormalized_Left;
                right_normalized[i] = _samples[i].PosNormalized_Right;
            }

            Vector3 avg_left = Math3D.GetAverage(left_normalized);
            Vector3 avg_right = Math3D.GetAverage(right_normalized);

            var aabb_left = Math3D.GetAABB(left_normalized);
            var aabb_right = Math3D.GetAABB(right_normalized);

            float cylinder_dist_left = Math3D.GetClosestDistance_Line_Point(new Ray(Vector3.zero, Vector3.up), avg_left);
            cylinder_dist_left /= player_height;

            float cylinder_dist_right = Math3D.GetClosestDistance_Line_Point(new Ray(Vector3.zero, Vector3.up), avg_right);
            cylinder_dist_right /= player_height;

            string[] lines = new[]
            {
                $"avg left: {avg_left.ToStringSignificantDigits(2)}",
                $"avg right: {avg_right.ToStringSignificantDigits(2)}",
                $"aabb left: {aabb_left.min.ToStringSignificantDigits(2)} to {aabb_left.max.ToStringSignificantDigits(2)}",
                $"aabb right: {aabb_right.min.ToStringSignificantDigits(2)} to {aabb_right.max.ToStringSignificantDigits(2)}",
                $"cylinder dist left: {cylinder_dist_left.ToStringSignificantDigits(2)}",
                $"cylinder dist right: {cylinder_dist_right.ToStringSignificantDigits(2)}",
                $"sample count: {_samples.Count}",
            };
            string text = string.Join(Environment.NewLine, lines);

            if (_sample_stats == null)
                _sample_stats = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, UtilityColor.FromHex("A5D186"), Color.black, TEXT_HEIGHT * lines.Length);

            _sample_stats.Object.transform.position = text_pos;
            _sample_stats.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_sample_stats, new_text: text);
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

        private void MaybeAddHandSamples(UtilJetpack.PlayerVRPoints positions)
        {
            DateTime now = DateTime.UtcNow;

            if ((now - _last_sample_added).TotalMilliseconds < 333)
                return;

            _samples.Add(new Sample
            {
                Pos_Left = positions.local.left,
                Pos_Right = positions.local.right,
                PosNormalized_Left = positions.local.left / positions.height,
                PosNormalized_Right = positions.local.right / positions.height,
            });
        }

        #endregion
    }
}
