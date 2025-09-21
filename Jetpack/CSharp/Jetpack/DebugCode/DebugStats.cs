using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.DebugCode
{
    public class DebugStats
    {
        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private List<string> _lines = new List<string>();

        private DebugRenderer3D _renderer = null;

        private DebugItem _report = null;

        public void Update_Pre()
        {
            _lines.Clear();
        }
        public void Update_Final()
        {
            if (!JetpackScript.ShowDebugStats)
                return;

            EnsureDebugActive();

            string text = string.Join(Environment.NewLine, _lines.OrderBy());

            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                Player.local.head.transform.right * -0.75f;

            if (_report == null)
                _report = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, UtilityColor.FromHex("1B1B6E"), Color.white, TEXT_HEIGHT * _lines.Count);

            _report.Object.transform.position = text_pos;
            _report.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_report, new_text: text);
        }

        public void AddEntry(string key, string value)
        {
            _lines.Add($"{key}:\t{value}");
        }

        public void Clear()
        {
            _lines.Clear();

            if (_renderer == null)
                return;

            if (_report != null)
                _renderer.Remove(_report);

            _report = null;

            _renderer = null;
        }

        private void EnsureDebugActive()
        {
            if (_renderer == null)
                _renderer = DebugRenderer3D.GetOrAddDebugRenderer3D();
        }
    }
}
