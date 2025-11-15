using Jetpack2.Core;
using Jetpack2.InputWatchers;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.DebugCode
{
    public class GazeBufferVisualizer_Offset
    {
        #region Declaration Section

        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private readonly GazeBuffer _gazeBuffer = new GazeBuffer();

        private DebugRenderer3D _renderer = null;

        private DebugItem _forward = null;
        private DebugItem _look = null;

        private int _buffer_index = -1;
        private List<DebugItem> _buffers = new List<DebugItem>();       // these are lines (showing contents of buffer)

        private DebugItem _dominant_direction = null;

        private DebugItem _status = null;

        #endregion

        public void Clear()
        {
            _gazeBuffer.Clear();
            ClearDebugVisuals();
        }

        public void Update(Vector3 body_forward)
        {
            if (!UIModOptions.ShowGazeBuffer_Offset)
                return;

            Vector3 pos = Player.local.head.anchor.position;
            Vector3 look = Player.local.head.transform.forward;

            PrepForBufferUpdate();
            _gazeBuffer.PrepareForNewFrame();

            DrawForwardLook(pos, look, body_forward);

            _gazeBuffer.AddSample_Offset(look, body_forward);

            float? confidence = null;
            if (_gazeBuffer.TryGetDominantDirection_Offset(out Vector3 dominant_direction, out float confidence2, body_forward))
            {
                confidence = confidence2;
                DrawDirection(dominant_direction, pos);
            }
            else
            {
                // Remove visual that DrawDirection populates
                if (_dominant_direction != null)
                    _renderer.Remove(_dominant_direction);
                _dominant_direction = null;
            }

            DrawBuffer(pos, body_forward);
            DrawStatus(confidence);

            FinishBufferUpdate();
        }

        #region Private Methods

        private void EnsureDebugActive()
        {
            if (_renderer == null)
                _renderer = DebugRenderer3D.GetOrAddDebugRenderer3D();
        }

        private void ClearDebugVisuals()
        {
            if (_renderer == null)
                return;

            if (_dominant_direction != null)
                _renderer.Remove(_dominant_direction);
            _dominant_direction = null;

            if (_forward != null)
                _renderer.Remove(_forward);
            _forward = null;

            if (_look != null)
                _renderer.Remove(_look);
            _look = null;

            if (_status != null)
                _renderer.Remove(_status);
            _status = null;

            _renderer = null;
        }

        private void PrepForBufferUpdate()
        {
            _buffer_index = -1;
            RemoveDespawned(_buffers);
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

        private void FinishBufferUpdate()
        {
            for (int i = 0; i < _buffers.Count; i++)
                _buffers[i].Object.SetActive(i <= _buffer_index);     // this should be cheaper than removing/adding
        }

        private void DrawForwardLook(Vector3 pos, Vector3 look, Vector3 forward)
        {
            EnsureDebugActive();

            // Forward
            if (_forward == null)
                _forward = _renderer.AddLine_Basic(pos, pos + forward, LINE_THICKNESS, Color.cyan);

            else
                DebugRenderer3D.AdjustLinePositions(_forward, pos, pos + forward);

            // Look
            if (_look == null)
                _look = _renderer.AddLine_Basic(pos, pos + look, LINE_THICKNESS, Color.white);

            else
                DebugRenderer3D.AdjustLinePositions(_look, pos, pos + look);
        }

        private void DrawDirection(Vector3 dominant_direction, Vector3 pos)
        {
            EnsureDebugActive();

            // Create or update the line
            if (_dominant_direction == null)
                _dominant_direction = _renderer.AddLine_Basic(pos, pos + dominant_direction, LINE_THICKNESS * 2, Color.yellow);

            else
                DebugRenderer3D.AdjustLinePositions(_dominant_direction, pos, pos + dominant_direction);
        }

        private void DrawBuffer(Vector3 pos, Vector3 forward)
        {
            EnsureDebugActive();

            foreach(var sample in _gazeBuffer._offset)
            {
                Vector3 direction = sample.Quaternion * forward;

                _buffer_index++;

                if (_buffer_index < _buffers.Count)
                    DebugRenderer3D.AdjustLinePositions(_buffers[_buffer_index], pos, pos + direction);

                else
                    _buffers.Add(_renderer.AddLine_Basic(pos, pos + direction, LINE_THICKNESS * 0.5f, Color.gray));
            }
        }

        private void DrawStatus(float? confidence)
        {
            EnsureDebugActive();

            var bucket = _gazeBuffer._offset;

            // text pos
            Vector3 text_pos = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.5f +
                //Player.local.head.transform.right * -0.75f +
                Player.local.head.transform.up * -0.33f;

            // fill out report
            var text_list = new List<string>();

            text_list.Add($"num samples: {bucket.Count}");
            text_list.Add($"confidence: {confidence?.ToStringSignificantDigits(2) ?? "--"}");

            string text = string.Join(Environment.NewLine, text_list);

            // draw
            if (_status == null)
                _status = _renderer.AddText(text, text_pos, Player.local.head.transform.forward, Color.cyan, Color.black, TEXT_HEIGHT * text_list.Count);

            _status.Object.transform.position = text_pos;
            _status.Object.transform.rotation = Quaternion.LookRotation((text_pos - Player.local.head.anchor.position).normalized, Player.local.head.transform.up);

            DebugRenderer3D.AdjustText(_status, new_text: text);
        }

        #endregion
    }
}
