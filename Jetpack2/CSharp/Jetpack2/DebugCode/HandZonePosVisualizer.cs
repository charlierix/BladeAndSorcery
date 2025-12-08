using Jetpack2.Core;
using Jetpack2.InputWatchers;
using Jetpack2.Models;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.IO;
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
        #region class: SampleStats

        private class SampleStats
        {
            public Vector3 avg_left { get; set; }
            public Vector3 avg_right { get; set; }
            public (Vector3 min, Vector3 max) aabb_left { get; set; }
            public (Vector3 min, Vector3 max) aabb_right { get; set; }
            public float cylinder_dist_left { get; set; }
            public float cylinder_dist_right { get; set; }
        }

        #endregion

        #region Declaration Section

        private const float DOT_SIZE = 0.008f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private const string COLOR_LEFT = "3685E0";
        private const string COLOR_RIGHT = "E3AA39";
        private const string COLOR_BODY = "5F735C";

        private DateTime _last_sample_added = DateTime.UtcNow;
        private List<Sample> _samples = new List<Sample>();

        private DebugRenderer3D _renderer = null;

        private int _dot_index = -1;
        private List<DebugItem> _dots = new List<DebugItem>();       // these are dots (showing contents of buffer)

        private DebugItem _sample_stats = null;

        private DebugItem _resting_box_left = null;
        private DebugItem _resting_box_right = null;

        private DebugItem _transition_box_left = null;
        private DebugItem _transition_box_right = null;

        private DebugItem _wing_box_left = null;
        private DebugItem _wing_box_right = null;

        private DebugItem _wing_left = null;
        private DebugItem _wing_right = null;

        private DebugItem _forward_left = null;
        private DebugItem _up_left = null;

        private DebugItem _forward_right = null;
        private DebugItem _up_right = null;

        private DebugItem _region_left = null;
        private DebugItem _region_right = null;

        private float _hash = -1;

        #endregion

        public void Clear()
        {
            _samples.Clear();
            ClearDebugVisuals();
        }

        public void Update(Vector3 body_forward, Vector3 body_up, UtilJetpack.PlayerVRPoints positions)
        {
            if (!UIModOptions.VisualizeHandZonePositions)
                return;

            float hash = GetConfigHash();
            if (!hash.IsNearValue(_hash))       // if config sliders changed, clear debug visuals so they can be redrawn
            {
                ClearDebugVisuals();
                _hash = hash;
            }

            Vector3 normalized_left = positions.local.left / positions.height;
            Vector3 normalized_right = positions.local.right / positions.height;


            // clear / add dots
            if (InputUtil.IsButtonPressed(Side.Left, PlayerControl.Hand.Button.Use))        // trigger
                SaveSamplesAndClear(positions);
            else if (InputUtil.IsButtonPressed(Side.Left, PlayerControl.Hand.Button.AlternateUse))      // thumbpad
                _samples.Clear();
            else if (InputUtil.IsButtonPressed(Side.Right, PlayerControl.Hand.Button.AlternateUse))
                MaybeAddHandSamples(positions);

            bool in_resting_left = IsIn_Resting(normalized_left, Side.Left);
            bool in_resting_right = IsIn_Resting(normalized_right, Side.Right);

            float? in_transition_left = IsIn_Transition(normalized_left, Side.Left);
            float? in_transition_right = IsIn_Transition(normalized_right, Side.Right);

            bool in_wing_left = IsIn_Wing(normalized_left, Side.Left);
            bool in_wing_right = IsIn_Wing(normalized_right, Side.Right);



            // TODO: option for only when hand is open

            // TODO: when wing is active, keep track of it so that swipes that would bite into air can be recognized and turned into increased accelerations
            //  look at the swimming code


            // ------ DRAWING ------

            PrepareForDraw();

            // a note saying right alt to add dots, left alt to clear dots

            DrawHandDots(positions.Transform_ToWorld);

            // text summary of the dots
            DrawSampleSummary(positions.height);

            // visualization of the regions
            DrawRestingRegions(positions);
            DrawTransitionRegions(positions);
            DrawWingRegions(positions);

            // which regions the hands are in
            DrawRegionBools(in_resting_left, in_resting_right, in_transition_left, in_transition_right, in_wing_left, in_wing_right);

            DrawWings(positions, in_transition_left, in_transition_right, in_wing_left, in_wing_right, body_forward);

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

            if (_resting_box_left != null)
            {
                _renderer.Remove(_resting_box_left);
                _resting_box_left = null;
            }

            if (_resting_box_right != null)
            {
                _renderer.Remove(_resting_box_right);
                _resting_box_right = null;
            }

            if (_transition_box_left != null)
            {
                _renderer.Remove(_transition_box_left);
                _transition_box_left = null;
            }

            if (_transition_box_right != null)
            {
                _renderer.Remove(_transition_box_right);
                _transition_box_right = null;
            }

            if (_wing_box_left != null)
            {
                _renderer.Remove(_wing_box_left);
                _wing_box_left = null;
            }

            if (_wing_box_right != null)
            {
                _renderer.Remove(_wing_box_right);
                _wing_box_right = null;
            }

            if (_wing_left != null)
            {
                _renderer.Remove(_wing_left);
                _wing_left = null;
            }

            if (_wing_right != null)
            {
                _renderer.Remove(_wing_right);
                _wing_right = null;
            }

            if (_forward_left != null)
            {
                _renderer.Remove(_forward_left);
                _forward_left = null;
            }

            if (_up_left != null)
            {
                _renderer.Remove(_up_left);
                _up_left = null;
            }

            if (_forward_right != null)
            {
                _renderer.Remove(_forward_right);
                _forward_right = null;
            }

            if (_up_right != null)
            {
                _renderer.Remove(_up_right);
                _up_right = null;
            }

            if (_region_left != null)
            {
                _renderer.Remove(_region_left);
                _region_left = null;
            }

            if (_region_right != null)
            {
                _renderer.Remove(_region_right);
                _region_right = null;
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
                AddDot(toWorld(sample.Pos_Left), UtilityColor.FromHex(COLOR_LEFT));
                AddDot(toWorld(sample.Pos_Right), UtilityColor.FromHex(COLOR_RIGHT));
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

            var stats = GetSampleStats(_samples, player_height);

            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                Player.local.head.transform.right * -0.35f +
                Player.local.head.transform.up * -0.25f;

            string[] lines = new[]
            {
                $"avg left: {stats.avg_left.ToStringSignificantDigits(2)}",
                $"avg right: {stats.avg_right.ToStringSignificantDigits(2)}",
                $"aabb left: {stats.aabb_left.min.ToStringSignificantDigits(2)} to {stats.aabb_left.max.ToStringSignificantDigits(2)}",
                $"aabb right: {stats.aabb_right.min.ToStringSignificantDigits(2)} to {stats.aabb_right.max.ToStringSignificantDigits(2)}",
                $"cylinder dist left: {stats.cylinder_dist_left.ToStringSignificantDigits(2)}",
                $"cylinder dist right: {stats.cylinder_dist_right.ToStringSignificantDigits(2)}",
                $"sample count: {_samples.Count}",
            };
            string text = string.Join(Environment.NewLine, lines);

            if (_sample_stats == null)
                _sample_stats = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, UtilityColor.FromHex("A5D186"), Color.black, TEXT_HEIGHT * lines.Length);

            _sample_stats.Object.transform.position = text_pos;
            _sample_stats.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_sample_stats, new_text: text);
        }

        private void DrawRestingRegions(UtilJetpack.PlayerVRPoints positions)
        {
            EnsureDebugActive();

            DrawRestingRegion(ref _resting_box_left, _renderer, positions, true);
            DrawRestingRegion(ref _resting_box_right, _renderer, positions, false);
        }
        private static void DrawRestingRegion(ref DebugItem resting, DebugRenderer3D renderer, UtilJetpack.PlayerVRPoints positions, bool is_left)
        {
            float x_mult = is_left ? -1 : 1;

            // NOTE: the points are normalized to height, so need to multiply by height to get them in world units
            float x1 = UIModOptions.PlayerPosTracking_RestingPos_MinX * x_mult * positions.height;
            float x2 = UIModOptions.PlayerPosTracking_RestingPos_MaxX * x_mult * positions.height;
            float y1 = UIModOptions.PlayerPosTracking_RestingPos_MinY * positions.height;
            float y2 = UIModOptions.PlayerPosTracking_RestingPos_MaxY * positions.height;
            float z1 = UIModOptions.PlayerPosTracking_RestingPos_MinZ * positions.height;
            float z2 = UIModOptions.PlayerPosTracking_RestingPos_MaxZ * positions.height;

            DrawWireframeBox(ref resting, renderer, x1, x2, y1, y2, z1, z2, positions, is_left);
        }

        private void DrawTransitionRegions(UtilJetpack.PlayerVRPoints positions)
        {
            EnsureDebugActive();

            DrawTransitionRegion(ref _transition_box_left, _renderer, positions, true);
            DrawTransitionRegion(ref _transition_box_right, _renderer, positions, false);
        }
        private static void DrawTransitionRegion(ref DebugItem resting, DebugRenderer3D renderer, UtilJetpack.PlayerVRPoints positions, bool is_left)
        {
            float x_mult = is_left ? -1 : 1;

            // NOTE: the points are normalized to height, so need to multiply by height to get them in world units
            float x1 = UIModOptions.PlayerPosTracking_RestingPos_MaxX * x_mult * positions.height;
            float x2 = UIModOptions.PlayerPosTracking_WingPos_MinX * x_mult * positions.height;
            float y1 = ((UIModOptions.PlayerPosTracking_RestingPos_MinY + UIModOptions.PlayerPosTracking_WingPos_MinY) / 2) * positions.height;
            float y2 = ((UIModOptions.PlayerPosTracking_RestingPos_MaxY + UIModOptions.PlayerPosTracking_WingPos_MaxY) / 2) * positions.height;
            float z1 = ((UIModOptions.PlayerPosTracking_RestingPos_MinZ + UIModOptions.PlayerPosTracking_WingPos_MinZ) / 2) * positions.height;
            float z2 = ((UIModOptions.PlayerPosTracking_RestingPos_MaxZ + UIModOptions.PlayerPosTracking_WingPos_MaxZ) / 2) * positions.height;

            DrawWireframeBox(ref resting, renderer, x1, x2, y1, y2, z1, z2, positions, is_left);
        }

        private void DrawWingRegions(UtilJetpack.PlayerVRPoints positions)
        {
            EnsureDebugActive();

            DrawWingRegion(ref _wing_box_left, _renderer, positions, true);
            DrawWingRegion(ref _wing_box_right, _renderer, positions, false);
        }
        private static void DrawWingRegion(ref DebugItem resting, DebugRenderer3D renderer, UtilJetpack.PlayerVRPoints positions, bool is_left)
        {
            float x_mult = is_left ? -1 : 1;

            // NOTE: the points are normalized to height, so need to multiply by height to get them in world units
            float x1 = UIModOptions.PlayerPosTracking_WingPos_MinX * x_mult * positions.height;
            float x2 = UIModOptions.PlayerPosTracking_WingPos_MaxX * x_mult * positions.height;
            float y1 = UIModOptions.PlayerPosTracking_WingPos_MinY * positions.height;
            float y2 = UIModOptions.PlayerPosTracking_WingPos_MaxY * positions.height;
            float z1 = UIModOptions.PlayerPosTracking_WingPos_MinZ * positions.height;
            float z2 = UIModOptions.PlayerPosTracking_WingPos_MaxZ * positions.height;

            DrawWireframeBox(ref resting, renderer, x1, x2, y1, y2, z1, z2, positions, is_left);
        }

        private static void DrawWireframeBox(ref DebugItem lines, DebugRenderer3D renderer, float x1, float x2, float y1, float y2, float z1, float z2, UtilJetpack.PlayerVRPoints positions, bool is_left)
        {
            var segments = new (Vector3, Vector3)[]
            {
                (new Vector3(x1, y1, z1), new Vector3(x2, y1, z1)),
                (new Vector3(x1, y2, z1), new Vector3(x2, y2, z1)),

                (new Vector3(x1, y1, z1), new Vector3(x1, y2, z1)),
                (new Vector3(x2, y1, z1), new Vector3(x2, y2, z1)),

                (new Vector3(x1, y1, z1), new Vector3(x1, y1, z2)),
                (new Vector3(x2, y1, z1), new Vector3(x2, y1, z2)),

                (new Vector3(x1, y2, z1), new Vector3(x1, y2, z2)),
                (new Vector3(x2, y2, z1), new Vector3(x2, y2, z2)),

                (new Vector3(x1, y1, z2), new Vector3(x2, y1, z2)),
                (new Vector3(x1, y2, z2), new Vector3(x2, y2, z2)),

                (new Vector3(x1, y1, z2), new Vector3(x1, y2, z2)),
                (new Vector3(x2, y1, z2), new Vector3(x2, y2, z2)),
            };

            string color = is_left ? COLOR_LEFT : COLOR_RIGHT;

            if (lines == null)
                lines = renderer.AddLine_Basic(Vector3.zero, segments, LINE_THICKNESS, UtilityColor.FromHex(color));

            lines.Object.transform.position = positions.Transform_ToWorld(Vector3.zero);
            lines.Object.transform.rotation = positions.rot_to_world;
        }

        private void DrawWings(UtilJetpack.PlayerVRPoints positions, float? in_transition_left, float? in_transition_right, bool in_wing_left, bool in_wing_right, Vector3 body_forward)
        {
            EnsureDebugActive();

            DrawWing(ref _wing_left, ref _forward_left, ref _up_left, _renderer, in_transition_left, in_wing_left, body_forward, positions.world.left_wing_pos, positions.world.left_wing_forward, positions.world.left_wing_up, true);
            DrawWing(ref _wing_right, ref _forward_right, ref _up_right, _renderer, in_transition_right, in_wing_right, body_forward, positions.world.right_wing_pos, positions.world.right_wing_forward, positions.world.right_wing_up, false);
        }
        private static void DrawWing(ref DebugItem wing, ref DebugItem visual_forward, ref DebugItem visual_up, DebugRenderer3D renderer, float? in_transition, bool in_wing, Vector3 body_forward, Vector3 pos, Vector3 forward, Vector3 up, bool is_left)
        {
            float percent = 1;

            if (in_transition != null)
                percent *= in_transition.Value;

            if (UIModOptions.PlayerPosTracking_WingRequireOpenHand)
                percent *= 1 - (is_left ? PlayerControl.handLeft : PlayerControl.handRight).GetAverageCurl();       // if hand is closed, this will be zero

            float wing_dot_forward = Vector3.Dot(-body_forward, up);
            bool in_gap = wing_dot_forward < WingsData.dot_airbrake && wing_dot_forward > WingsData.dot_wing;

            // Exit early if no wing
            if ((in_transition == null && !in_wing) || in_gap || percent.IsNearZero())
            {
                if (wing != null)
                {
                    renderer.Remove(wing);
                    wing = null;
                }

                if (visual_forward != null)
                {
                    renderer.Remove(visual_forward);
                    visual_forward = null;
                }

                if (visual_up != null)
                {
                    renderer.Remove(visual_up);
                    visual_up = null;
                }

                return;
            }

            // Wing
            if (wing == null)
            {
                Vector3 scale = new Vector3(0.15f, 0.01f, 0.45f);
                wing = renderer.AddCube(Vector3.zero, scale, Color.white);
            }

            wing.Object.transform.position = pos;
            wing.Object.transform.rotation = Math3D.GetRotation(new DoubleVector(new Vector3(0, 0, 1), new Vector3(0, 1, 0)), new DoubleVector(forward, up));

            // Forward/Up
            if (visual_forward == null)
                visual_forward = renderer.AddLine_Basic(pos, pos + forward, LINE_THICKNESS, Color.blue);
            else
                DebugRenderer3D.AdjustLinePositions(visual_forward, pos, pos + forward);

            if (visual_up == null)
                visual_up = renderer.AddLine_Basic(pos, pos + up, LINE_THICKNESS, Color.green);
            else
                DebugRenderer3D.AdjustLinePositions(visual_up, pos, pos + up);

            // Color
            bool is_airbrake = wing_dot_forward >= WingsData.dot_airbrake;

            // color is based on dot product with velocity (wing or air brake)
            Color color = is_airbrake ?
                Color.black :
                Color.white;

            if (!percent.IsNearValue(1))
                color.a = percent;

            DebugRenderer3D.AdjustColor(wing, color);
        }

        private void DrawRegionBools(bool in_resting_left, bool in_resting_right, float? in_transition_left, float? in_transition_right, bool in_wing_left, bool in_wing_right)
        {
            EnsureDebugActive();

            DrawRegionBools_Draw(ref _region_left, _renderer, in_resting_left, in_transition_left, in_wing_left, true);
            DrawRegionBools_Draw(ref _region_right, _renderer, in_resting_right, in_transition_right, in_wing_right, false);
        }
        private static void DrawRegionBools_Draw(ref DebugItem region, DebugRenderer3D renderer, bool in_resting, float? in_transition, bool in_wing, bool is_left)
        {
            if (!(in_resting || in_transition != null || in_wing))
            {
                if (region != null)
                {
                    renderer.Remove(region);
                    region = null;
                }

                return;
            }

            float leftright_mult = is_left ? -1 : 1;

            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                Player.local.head.transform.right * 0.1f * leftright_mult +
                Player.local.head.transform.up * 0.4f;

            var lines = new List<string>();

            if (in_resting)
                lines.Add("resting");

            if (in_transition != null)
                lines.Add($"transition {Mathf.Round(in_transition.Value * 100)}%");

            if (in_wing)
                lines.Add("wing");

            string color = is_left ? COLOR_LEFT : COLOR_RIGHT;

            string text = string.Join(Environment.NewLine, lines);

            if (region == null)
                region = renderer.AddText(text, text_pos, Player.local.head.transform.forward, UtilityColor.FromHex(color), Color.black, TEXT_HEIGHT * lines.Count);

            region.Object.transform.position = text_pos;
            region.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(region, new_text: text);
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

        private void SaveSamplesAndClear(UtilJetpack.PlayerVRPoints positions)
        {
            const string CATEGORY_LEFT = "left";
            const string CATEGORY_RIGHT = "right";
            const string CATEGORY_BODY = "body";
            const string CATEGORY_STATS = "stats";

            Debug.Log($"saving samples called, sample count: {_samples.Count}");

            if (_samples.Count == 0)
                return;



            // TODO: this is going off the blade & sorcery root folder.  figure out how to get the mod's folder
            string path = Path.GetFullPath(".");
            Debug.Log($"root path: '{path}'");

            path = Path.Combine(path, "logs");
            Debug.Log($"log path: '{path}'");


            // if this returns files in the mod folder while simply calling Path.GetFullPath(".") returns the game's root
            // folder, then use this and get the first file's folder
            string[] files = Directory.GetFiles(".");
            Debug.Log($"files (.): {string.Join(Environment.NewLine, files)}");



            var log = new DebugLogger(path, true);

            log.DefineCategory(CATEGORY_LEFT, UtilityColor.FromHex(COLOR_LEFT));
            log.DefineCategory(CATEGORY_RIGHT, UtilityColor.FromHex(COLOR_RIGHT));
            log.DefineCategory(CATEGORY_BODY, UtilityColor.FromHex(COLOR_BODY));
            log.DefineCategory(CATEGORY_STATS, UtilityColor.FromHex("BD71AB"), 1.75f);

            //log.NewFrame("combined - normalized");        // in case more specific frames should be added

            log.Add_Dot(Vector3.zero, CATEGORY_BODY);
            log.Add_Dot(Vector3.up * 0.5f, CATEGORY_BODY);
            log.Add_Line(Vector3.zero, Vector3.forward, CATEGORY_BODY);
            log.Add_Line(Vector3.up, Vector3.up, CATEGORY_BODY);

            foreach (var sample in _samples)
            {
                log.Add_Dot(sample.PosNormalized_Left, CATEGORY_LEFT, tooltip: sample.PosNormalized_Left.ToStringSignificantDigits(4));
                log.Add_Dot(sample.PosNormalized_Right, CATEGORY_RIGHT, tooltip: sample.PosNormalized_Right.ToStringSignificantDigits(4));
            }

            var stats = GetSampleStats(_samples, positions.height);

            log.Add_Dot(stats.aabb_left.min, CATEGORY_STATS, tooltip: stats.aabb_left.min.ToStringSignificantDigits(4));
            log.Add_Dot(stats.aabb_left.max, CATEGORY_STATS, tooltip: stats.aabb_left.max.ToStringSignificantDigits(4));
            log.Add_Dot(stats.avg_left, CATEGORY_STATS, tooltip: stats.avg_left.ToStringSignificantDigits(4));

            log.Add_Dot(stats.aabb_right.min, CATEGORY_STATS, tooltip: stats.aabb_right.min.ToStringSignificantDigits(4));
            log.Add_Dot(stats.aabb_right.max, CATEGORY_STATS, tooltip: stats.aabb_right.max.ToStringSignificantDigits(4));
            log.Add_Dot(stats.avg_right, CATEGORY_STATS, tooltip: stats.avg_right.ToStringSignificantDigits(4));

            log.WriteLine_Frame($"avg left: {stats.avg_left.ToStringSignificantDigits(2)}");
            log.WriteLine_Frame($"avg right: {stats.avg_right.ToStringSignificantDigits(2)}");
            log.WriteLine_Frame($"aabb left: {stats.aabb_left.min.ToStringSignificantDigits(2)} to {stats.aabb_left.max.ToStringSignificantDigits(2)}");
            log.WriteLine_Frame($"aabb right: {stats.aabb_right.min.ToStringSignificantDigits(2)} to {stats.aabb_right.max.ToStringSignificantDigits(2)}");
            log.WriteLine_Frame($"cylinder dist left: {stats.cylinder_dist_left.ToStringSignificantDigits(2)}");
            log.WriteLine_Frame($"cylinder dist right: {stats.cylinder_dist_right.ToStringSignificantDigits(2)}");
            log.WriteLine_Frame($"sample count: {_samples.Count}");

            log.Save("samples");

            _samples.Clear();

            Debug.Log("samples saved");
        }

        private static SampleStats GetSampleStats(List<Sample> samples, float player_height)
        {
            Vector3[] left_normalized = new Vector3[samples.Count];
            Vector3[] right_normalized = new Vector3[samples.Count];

            for (int i = 0; i < samples.Count; i++)
            {
                left_normalized[i] = samples[i].PosNormalized_Left;
                right_normalized[i] = samples[i].PosNormalized_Right;
            }

            Vector3 avg_left = Math3D.GetAverage(left_normalized);
            Vector3 avg_right = Math3D.GetAverage(right_normalized);

            var aabb_left = Math3D.GetAABB(left_normalized);
            var aabb_right = Math3D.GetAABB(right_normalized);

            float cylinder_dist_left = Math3D.GetClosestDistance_Line_Point(new Ray(Vector3.zero, Vector3.up), avg_left);
            cylinder_dist_left /= player_height;

            float cylinder_dist_right = Math3D.GetClosestDistance_Line_Point(new Ray(Vector3.zero, Vector3.up), avg_right);
            cylinder_dist_right /= player_height;

            return new SampleStats
            {
                avg_left = avg_left,
                avg_right = avg_right,
                aabb_left = aabb_left,
                aabb_right = aabb_right,
                cylinder_dist_left = cylinder_dist_left,
                cylinder_dist_right = cylinder_dist_right,
            };
        }

        private static bool IsIn_Resting(Vector3 pos, Side side)
        {
            pos = side == Side.Left ?
                new Vector3(-pos.x, pos.y, pos.z) :
                pos;

            bool retVal = true;

            retVal &= pos.x >= UIModOptions.PlayerPosTracking_RestingPos_MinX && pos.x <= UIModOptions.PlayerPosTracking_RestingPos_MaxX;
            retVal &= pos.y >= UIModOptions.PlayerPosTracking_RestingPos_MinY && pos.y <= UIModOptions.PlayerPosTracking_RestingPos_MaxY;
            retVal &= pos.z >= UIModOptions.PlayerPosTracking_RestingPos_MinZ && pos.z <= UIModOptions.PlayerPosTracking_RestingPos_MaxZ;

            return retVal;
        }
        private static float? IsIn_Transition(Vector3 pos, Side side)
        {
            pos = side == Side.Left ?
                new Vector3(-pos.x, pos.y, pos.z) :
                pos;

            bool inRange = true;

            inRange &= pos.x >= UIModOptions.PlayerPosTracking_RestingPos_MaxX && pos.x <= UIModOptions.PlayerPosTracking_WingPos_MinX;     // x is correct

            if (!inRange)
                return null;

            // x is in range, lerp y and z based on percent of x
            float min_y = UtilityMath.GetScaledValue(UIModOptions.PlayerPosTracking_RestingPos_MinY, UIModOptions.PlayerPosTracking_WingPos_MinY, UIModOptions.PlayerPosTracking_RestingPos_MaxX, UIModOptions.PlayerPosTracking_WingPos_MinX, pos.x);
            float max_y = UtilityMath.GetScaledValue(UIModOptions.PlayerPosTracking_RestingPos_MaxY, UIModOptions.PlayerPosTracking_WingPos_MaxY, UIModOptions.PlayerPosTracking_RestingPos_MaxX, UIModOptions.PlayerPosTracking_WingPos_MinX, pos.x);
            float min_z = UtilityMath.GetScaledValue(UIModOptions.PlayerPosTracking_RestingPos_MinZ, UIModOptions.PlayerPosTracking_WingPos_MinZ, UIModOptions.PlayerPosTracking_RestingPos_MaxX, UIModOptions.PlayerPosTracking_WingPos_MinX, pos.x);
            float max_z = UtilityMath.GetScaledValue(UIModOptions.PlayerPosTracking_RestingPos_MaxZ, UIModOptions.PlayerPosTracking_WingPos_MaxZ, UIModOptions.PlayerPosTracking_RestingPos_MaxX, UIModOptions.PlayerPosTracking_WingPos_MinX, pos.x);

            inRange &= pos.y >= min_y && pos.y <= max_y;
            inRange &= pos.z >= min_z && pos.z <= max_z;

            if (!inRange)
                return null;

            return UtilityMath.GetScaledValue(0, 1, UIModOptions.PlayerPosTracking_RestingPos_MaxX, UIModOptions.PlayerPosTracking_WingPos_MinX, pos.x);
        }
        private static bool IsIn_Wing(Vector3 pos, Side side)
        {
            pos = side == Side.Left ?
                new Vector3(-pos.x, pos.y, pos.z) :
                pos;

            bool retVal = true;

            retVal &= pos.x >= UIModOptions.PlayerPosTracking_WingPos_MinX && pos.x <= UIModOptions.PlayerPosTracking_WingPos_MaxX;
            retVal &= pos.y >= UIModOptions.PlayerPosTracking_WingPos_MinY && pos.y <= UIModOptions.PlayerPosTracking_WingPos_MaxY;
            retVal &= pos.z >= UIModOptions.PlayerPosTracking_WingPos_MinZ && pos.z <= UIModOptions.PlayerPosTracking_WingPos_MaxZ;

            return retVal;
        }

        private float GetConfigHash()
        {
            return
                UIModOptions.PlayerPosTracking_RestingPos_MinX +
                UIModOptions.PlayerPosTracking_RestingPos_MaxX +
                UIModOptions.PlayerPosTracking_RestingPos_MinY +
                UIModOptions.PlayerPosTracking_RestingPos_MaxY +
                UIModOptions.PlayerPosTracking_RestingPos_MinZ +
                UIModOptions.PlayerPosTracking_RestingPos_MaxZ +
                UIModOptions.PlayerPosTracking_WingPos_MinX +
                UIModOptions.PlayerPosTracking_WingPos_MaxX +
                UIModOptions.PlayerPosTracking_WingPos_MinY +
                UIModOptions.PlayerPosTracking_WingPos_MaxY +
                UIModOptions.PlayerPosTracking_WingPos_MinZ +
                UIModOptions.PlayerPosTracking_WingPos_MaxZ +
                UIModOptions.PlayerPosTracking_WingRotateAngle +
                UIModOptions.PlayerPosTracking_WingTranslateCord;
        }

        #endregion
    }
}
