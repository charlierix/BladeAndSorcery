using Jetpack2.InputWatchers;
using PerfectlyNormalBaS;
using ThunderRoad;
using UnityEngine;

namespace Jetpack2.DebugCode
{
    public class HeadUpRotateVisualizer
    {
        #region struct: SetCoords

        private struct SetCoords
        {
            public Vector3 One { get; set; }
            public Vector3 Two { get; set; }
            public Vector3 Three { get; set; }
            public Vector3 Four { get; set; }
        }

        #endregion
        #region class: DrawingSet

        private class DrawingSet
        {
            public DebugItem sanity_line { get; set; }
            public DebugItem body_forward { get; set; }
            public DebugItem body_up { get; set; }
            public DebugItem head_forward { get; set; }
            public DebugItem head_up { get; set; }
            public DebugItem direction_roll { get; set; }
        }

        #endregion

        private const float LINE_LEN = 0.25f;
        //private const float SET_MARGIN = 0.05f;
        private const float SET_MARGIN = -0.15f;        // they only draw in one quadrant at a time, so won't run into each other


        private const float DOT_SIZE = 0.05f;
        private const float LINE_THICKNESS = 0.005f;
        private const float TEXT_HEIGHT = 0.06f;

        private readonly PlayerRagdollUtil _ragdollUtil = new PlayerRagdollUtil();
        private readonly GazeBuffer _gazebuffer_roll = new GazeBuffer();

        private DebugRenderer3D _renderer = null;

        private DrawingSet _set1 = null;
        private DrawingSet _set2 = null;
        private DrawingSet _set3 = null;
        private DrawingSet _set4 = null;

        public void Clear()
        {
            _gazebuffer_roll.Clear();

            if (JetpackScript.ShowHeadUpRotateVisualizer)
                ClearDebugVisuals();
        }

        public void Update(float elapsed_seconds)
        {
            if (!JetpackScript.ShowHeadUpRotateVisualizer)
                return;


            // vectors to track:
            //  body_forward
            //  body_up
            //  head_forward
            //  head_up
            //  direction_roll

            // quats
            //  quat3 = to local
            //  quat4 = to world



            // 1: initial values (world coords)
            var (body_forward1, body_up1) = _ragdollUtil.GetRagdollForwardUp();
            Vector3 head_forward1 = Player.local.head.transform.forward;
            Vector3 head_up1 = Player.local.head.transform.up;



            // 2: trimmed (world coords)
            Vector3 body_forward2 = body_forward1;
            Vector3 body_up2 = body_up1;
            TrimForwardUp(ref body_forward2, ref body_up2);

            // pull head into the plane of body up and right
            var (head_up2, head_forward2) = GetProjecteHeadUp(head_up1, head_forward1, body_forward2);



            // 3: rotated so that forward is Z, up is Y
            var (body_up3, head_up3, quat3) = RotateUps(body_forward2, body_up2, head_up2);
            Vector3 body_forward3 = quat3 * body_forward2;
            Vector3 head_forward3 = quat3 * head_forward2;

            _gazebuffer_roll.AddSample_Offset(head_up3, body_up3);

            float? confidence_roll = null;
            if (_gazebuffer_roll.TryGetDominantDirection_Offset(out Vector3 direction_roll3, out float confidence, body_up3))
                confidence_roll = confidence;



            // 4: locals rotated back into world coords
            Quaternion quat4 = Quaternion.Inverse(quat3);

            Vector3 body_forward4 = quat4 * body_forward3;
            Vector3 body_up4 = quat4 * body_up3;
            Vector3 head_forward4 = quat4 * head_forward3;
            Vector3 head_up4 = quat4 * head_up3;
            Vector3 direction_roll4 = quat4 * direction_roll3;



            // drawing
            var coords = GetSetCoords();
            DrawSet(ref _set1, coords.One, body_forward1, body_up1, head_forward1, head_up1, Vector3.zero, false);
            DrawSet(ref _set2, coords.Two, body_forward2, body_up2, head_forward2, head_up2, Vector3.zero, false);
            DrawSet(ref _set3, coords.Three, body_forward3, body_up3, head_forward3, head_up3, direction_roll3, confidence_roll != null);
            DrawSet(ref _set4, coords.Four, body_forward4, body_up4, head_forward4, head_up4, direction_roll4, confidence_roll != null);
        }

        #region Private Methods - draw

        private void EnsureDebugActive()
        {
            if (_renderer == null)
                _renderer = DebugRenderer3D.GetOrAddDebugRenderer3D();
        }

        private void ClearDebugVisuals()
        {
            if (_renderer == null)
                return;

            ClearDebugVisuals_Set(ref _set1);
            ClearDebugVisuals_Set(ref _set2);
            ClearDebugVisuals_Set(ref _set3);
            ClearDebugVisuals_Set(ref _set4);

            _renderer = null;
        }
        private void ClearDebugVisuals_Set(ref DrawingSet set)
        {
            if (set == null)
                return;

            if (set.sanity_line != null)
                _renderer.Remove(set.sanity_line);

            if (set.body_forward != null)
                _renderer.Remove(set.body_forward);

            if (set.body_up != null)
                _renderer.Remove(set.body_up);

            if (set.head_forward != null)
                _renderer.Remove(set.head_forward);

            if (set.head_up != null)
                _renderer.Remove(set.head_up);

            if (set.direction_roll != null)
                _renderer.Remove(set.direction_roll);

            set = null;
        }

        private static SetCoords GetSetCoords()
        {
            Vector3 up = Player.local.head.transform.up;
            Vector3 right = Player.local.head.transform.right;

            Vector3 origin = Player.local.head.anchor.position +
                Player.local.head.transform.forward * 1.33f;

            Vector3 set_right_dir = right * (SET_MARGIN / 2f + LINE_LEN);

            //  2 and 4 are next to each other
            Vector3 two = origin - set_right_dir;
            Vector3 four = origin + set_right_dir;

            //  1 above 2
            Vector3 one = two + up * (LINE_LEN + SET_MARGIN + LINE_LEN);

            //  3 is centered below 2 and 4
            Vector3 three = origin - up * (LINE_LEN + SET_MARGIN + LINE_LEN);

            return new SetCoords
            {
                One = one,
                Two = two,
                Three = three,
                Four = four,
            };
        }

        private void DrawSet(ref DrawingSet drawing, Vector3 center, Vector3 body_forward, Vector3 body_up, Vector3 head_forward, Vector3 head_up, Vector3 direction_roll, bool shoulddraw_dirroll)
        {
            EnsureDebugActive();

            if (drawing == null)
                drawing = new DrawingSet();

            //Vector3 head_pos = Player.local.head.anchor.position;

            //if (drawing.sanity_line == null)
            //    drawing.sanity_line = _renderer.AddLine_Basic(head_pos, center, LINE_THICKNESS, Color.gray);
            //else
            //    DebugRenderer3D.AdjustLinePositions(drawing.sanity_line, head_pos, center);

            if (drawing.body_forward == null)
                drawing.body_forward = _renderer.AddLine_Basic(center, center + body_forward * LINE_LEN, LINE_THICKNESS, Color.blue);
            else
                DebugRenderer3D.AdjustLinePositions(drawing.body_forward, center, center + body_forward * LINE_LEN);

            if (drawing.body_up == null)
                drawing.body_up = _renderer.AddLine_Basic(center, center + body_up * LINE_LEN, LINE_THICKNESS, Color.green);
            else
                DebugRenderer3D.AdjustLinePositions(drawing.body_up, center, center + body_up * LINE_LEN);

            if (drawing.head_forward == null)
                drawing.head_forward = _renderer.AddLine_Basic(center, center + head_forward * LINE_LEN, LINE_THICKNESS, Color.black);
            else
                DebugRenderer3D.AdjustLinePositions(drawing.head_forward, center, center + head_forward * LINE_LEN);

            if (drawing.head_up == null)
                drawing.head_up = _renderer.AddLine_Basic(center, center + head_up * LINE_LEN, LINE_THICKNESS, Color.white);
            else
                DebugRenderer3D.AdjustLinePositions(drawing.head_up, center, center + head_up * LINE_LEN);

            if (shoulddraw_dirroll)
            {
                if (drawing.direction_roll == null)
                    drawing.direction_roll = _renderer.AddLine_Basic(center, center + direction_roll * LINE_LEN, LINE_THICKNESS, UtilityColor.FromHex("F5BA28"));
                else
                    DebugRenderer3D.AdjustLinePositions(drawing.direction_roll, center, center + direction_roll * LINE_LEN);
            }
            else
            {
                if (drawing.direction_roll != null)
                {
                    _renderer.Remove(drawing.direction_roll);
                    drawing.direction_roll = null;
                }
            }
        }

        #endregion
        #region Private Methods

        private void TrimForwardUp(ref Vector3 forward, ref Vector3 up)
        {
            // Yaw Trim
            if (!JetpackScript.RotToLook_ForwardTrimDegrees_Yaw.IsNearZero())
            {
                Quaternion yaw = Quaternion.AngleAxis(JetpackScript.RotToLook_ForwardTrimDegrees_Yaw, up);

                // Apply Yaw Rotation to both forward and up
                forward = yaw * forward;
                up = yaw * up;
            }

            // Pitch Trim
            if (!JetpackScript.RotToLook_ForwardTrimDegrees_Pitch.IsNearZero())
            {
                Vector3 right = Vector3.Cross(forward, up);
                Quaternion pitch = Quaternion.AngleAxis(JetpackScript.RotToLook_ForwardTrimDegrees_Pitch, right);

                // Apply Pitch Rotation to both forward and up
                forward = pitch * forward;
                up = pitch * up;
            }
        }

        /// <summary>
        /// Projects head_up into the plane of body_up and body_right
        /// </summary>
        private static (Vector3 up, Vector3 forward) GetProjecteHeadUp(Vector3 head_up, Vector3 head_forward, Vector3 body_forward)
        {
            var quat = Quaternion.FromToRotation(head_forward, body_forward);
            return (quat * head_up, quat * head_forward);
        }

        private static (Vector3 body_up, Vector3 head_up, Quaternion quat) RotateUps(Vector3 body_forward, Vector3 body_up, Vector3 head_up)
        {
            DoubleVector from = new DoubleVector(body_forward, body_up);
            DoubleVector to = new DoubleVector(new Vector3(0, 0, 1), new Vector3(0, 1, 0));

            Quaternion quat = Math3D.GetRotation(from, to);

            return (quat * body_up, quat * head_up, quat);
        }

        #endregion
    }
}
