using Jetpack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThunderRoad;
using UnityEngine;

namespace Jetpack
{
    // Source: https://github.com/sjankowskim/wings

    public class JetpackScript : ThunderScript
    {
        // Source: @Wully on BaS Discord
        // Big help in getting this ready for U12
        public static ModOptionFloat[] ZeroToOneHundered()
        {
            ModOptionFloat[] options = new ModOptionFloat[101];
            float val = 0;
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = new ModOptionFloat(val.ToString("0.0"), val);
                val += 1f;
            }
            return options;
        }
        public static ModOptionFloat[] ZeroToThirtySix()
        {
            const int COUNT = 36;

            ModOptionFloat[] options = new ModOptionFloat[101];
            float val = 0;
            float step = COUNT / (options.Length - 1);

            for (int i = 0; i < options.Length; i++)
            {
                options[i] = new ModOptionFloat(val.ToString("0.0"), val);
                val += step;
            }

            return options;
        }

        [ModOption(name: "Use Jetpack Mod", tooltip: "Turns on/off the Jetpack mod.", defaultValueIndex = 1, order = 0)]
        public static bool useJetpackMod = true;

        [ModOptionSlider]
        [ModOption(name: "Vertical Force", tooltip: "Determines how fast the player can fly vertically.", valueSourceName = nameof(ZeroToThirtySix), order = 1)]
        public static float verticalForce = 6;     // discussion on discord was saying defaultValueIndex is ignored in 1.0.3, need to explicitely set a value

        [ModOptionSlider]
        [ModOption(name: "Horizontal Speed", tooltip: "Determines how fast the player can fly horizontally.", valueSourceName = nameof(ZeroToThirtySix), order = 2)]
        public static float horizontalSpeed = 9;

        // PRE-FLIGHT DATA
        private FlightData _old = null;

        // FLIGHT DATA
        private Locomotion _loco = null;
        private bool _isFlying = false;
        private bool _markedToFly = false;      // will fly once not grounded

        private InputListener _inputListener = null;

        // make another one that needs thumbs closed, but fingers open at the same time for a small amount of time
        private KeyDoublePressTracker _keyTracker = new KeyDoublePressTracker();

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);

            _inputListener = new InputListener(_keyTracker);
        }

        public override void ScriptUpdate()
        {
            base.ScriptUpdate();

            _inputListener.OnUpdate();

            if (_keyTracker.WasBothDoubleClicked)
            {
                _keyTracker.Clear();

                if (Player.local.locomotion.isGrounded)
                {
                    if (_isFlying)
                        DeactivateFly();
                    else
                        _markedToFly = true;        // TODO: when grounded, apply enough impulse upward to not be grounded (if that's possible)
                }
                else
                {
                    if (_isFlying)
                        DeactivateFly();
                    else
                        ActivateFly();
                }
            }

            if (_isFlying && Player.local.locomotion.isGrounded)
                DeactivateFly();

            if (_markedToFly && !Player.local.locomotion.isGrounded)
                ActivateFly();
        }

        public override void ScriptFixedUpdate()
        {
            base.ScriptFixedUpdate();

            if (Player.currentCreature)
            {
                if (_isFlying && !Player.local.locomotion.isGrounded)
                {
                    _loco.horizontalAirSpeed = horizontalSpeed / 100f;

                    DestabilizeHeldNPC(Player.local.handLeft);
                    DestabilizeHeldNPC(Player.local.handRight);

                    TryFlyUp(_inputListener.GetRightStick());
                }
            }
            else
            {
                _isFlying = false;
            }
        }

        private void TryFlyUp(Vector2 axis)
        {
            if (axis.y != 0.0 && (!Pointer.GetActive() || !Pointer.GetActive().isPointingUI))
                _loco.physicBody.AddForce(Vector3.up * verticalForce * axis.y, ForceMode.Acceleration);
        }

        private static void DestabilizeHeldNPC(PlayerHand side)
        {
            if (side.ragdollHand.grabbedHandle)
            {
                Creature grabbedCreature = side.ragdollHand.grabbedHandle.gameObject.GetComponentInParent<Creature>();
                if (grabbedCreature)
                {
                    if (grabbedCreature.ragdoll.state != Ragdoll.State.Inert)
                        grabbedCreature.ragdoll.SetState(Ragdoll.State.Destabilized);
                }
                else
                {
                    foreach (RagdollHand ragdollHand in side.ragdollHand.grabbedHandle.handlers)
                    {
                        Creature creature = ragdollHand.gameObject.GetComponentInParent<Creature>();
                        if (creature && creature != Player.currentCreature)
                            ragdollHand.TryRelease();
                    }
                }
            }
        }

        private void ActivateFly()
        {
            _markedToFly = false;

            if (!useJetpackMod)
            {
                Debug.Log("ActivateFly() called, but mod is disabled");
                return;
            }

            _loco = Player.local.locomotion;

            // STORE ORIGINAL STATS
            _old = new FlightData()
            {
                HorizontalSpeed = _loco.horizontalAirSpeed,
                VerticalSpeed = _loco.verticalAirSpeed,
                MaxAngle = _loco.groundAngle,
                Drag = _loco.physicBody.drag,
                Mass = _loco.physicBody.mass,
                FallDamage = Player.fallDamage,
                CrouchOnJump = Player.crouchOnJump,
                StickJump = GameManager.options.allowStickJump,
            };

            // ENABLE FLIGHT STATS
            _isFlying = true;
            _loco.groundAngle = -359f;
            _loco.physicBody.useGravity = false;
            //_loco.physicBody.mass = 100000f;
            _loco.physicBody.drag = 0.9f;
            _loco.velocity = Vector3.zero;
            Player.fallDamage = false;
            Player.crouchOnJump = false;
            //GameManager.options.allowStickJump = false;       // this doesn't seem to affect anything

            Debug.Log($"Activating Flight:\r\n{JsonUtility.ToJson(_old, true)}");
        }

        private void DeactivateFly()
        {
            _isFlying = false;

            if (_old != null)
            {
                _loco.groundAngle = _old.MaxAngle;
                _loco.physicBody.drag = _old.Drag;
                _loco.physicBody.useGravity = true;
                _loco.physicBody.mass = _old.Mass;
                _loco.horizontalAirSpeed = _old.HorizontalSpeed;
                _loco.verticalAirSpeed = _old.VerticalSpeed;
                Player.fallDamage = _old.FallDamage;
                Player.crouchOnJump = _old.CrouchOnJump;
                GameManager.options.allowStickJump = _old.StickJump;
            }
        }
    }
}
