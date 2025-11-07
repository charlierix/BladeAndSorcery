using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.DebugCode
{
    public class DebugStats
    {
        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private List<string> _lines_left = new List<string>();
        private List<string> _lines_right = new List<string>();

        private DebugRenderer3D _renderer = null;

        private DebugItem _report_left = null;
        private DebugItem _report_right = null;

        public void Update_Pre()
        {
            _lines_left.Clear();
            _lines_right.Clear();
        }
        public void Update_Final()
        {
            if (!UIModOptions.ShowDebugStats)
                return;

            EnsureDebugActive();

            DrawText(ref _report_left, _renderer, _lines_left, true);
            DrawText(ref _report_right, _renderer, _lines_right, false);
        }

        public void AddEntry_Left(string key, string value)
        {
            AddEntry(_lines_left, key, value);
        }
        public void AddEntry_Right(string key, string value)
        {
            AddEntry(_lines_right, key, value);
        }

        public void Clear()
        {
            _lines_left.Clear();
            _lines_right.Clear();

            if (_renderer == null)
                return;

            if (_report_left != null)
            {
                _renderer.Remove(_report_left);
                _report_left = null;
            }

            if (_report_right != null)
            {
                _renderer.Remove(_report_right);
                _report_right = null;
            }

            _renderer = null;
        }

        private void EnsureDebugActive()
        {
            if (_renderer == null)
                _renderer = DebugRenderer3D.GetOrAddDebugRenderer3D();
        }

        private static void AddEntry(List<string> list, string key, string value)
        {
            list.Add($"{key}:\t{value}");
        }

        private static void DrawText(ref DebugItem visual, DebugRenderer3D renderer, List<string> list, bool isLeft)
        {
            if (list.Count == 0)
            {
                if (visual != null)
                {
                    renderer.Remove(visual);
                    visual = null;
                }

                return;
            }

            string text = string.Join(Environment.NewLine, list.OrderBy());

            float side = isLeft ? -1 : 1;

            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                Player.local.head.transform.right * 0.4f * side;

            if (visual == null)
                visual = renderer.AddText(text, text_pos, Player.local.head.transform.forward, UtilityColor.FromHex("1B1B6E"), Color.white, TEXT_HEIGHT * list.Count);

            visual.Object.transform.position = text_pos;
            visual.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(visual, new_text: text);
        }
    }
}
