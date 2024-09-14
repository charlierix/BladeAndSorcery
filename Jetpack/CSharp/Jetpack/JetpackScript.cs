using Jetpack.InputWatchers;
using Jetpack.Models;
using PerfectlyNormalBaS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ThunderRoad;
using UnityEngine;
using static ThunderRoad.EffectModuleMesh;



// TODOS:

// Make a Flying class to handle actual flying


// Change when player stats are stored.  Move it from flight activate to higher up like a startup.  Or maybe just store
// on first activation, then keep from that point on


// Figure out how to make debug graphics


// Options to reduce accel if in confined space (probably just a checkbox, the raycast dist and % reduction can probably be hardcoded - it may not be linear)
//  Physics.OverlapSphere
//  Physics.SphereCastAll
//  Physics.Raycast


// activate sound sounds more like a fireball than flight


// Put scale logic as a separate thing from activate/deactivate flight
//  create extra sliders and checkboxes
//  make an apply scale button and back to default button


// instead of a single flight mode for the whole mod, create a few modes that can have activation gestures assigned
//  jetpack - default to up activation, double jump would be a good alternative
//      has some gravity, basic flight
//
//  winged - some combination of bird and airplane
//
//  iron man - thruster based at low speed, winged at higher speeds
//
//  fpv drone - try to emulate a drone with thumbstick inputs



namespace Jetpack
{
    // Source: https://github.com/sjankowskim/wings

    public class JetpackScript : ThunderScript
    {
        // https://kospy.github.io/BasSDK/Components/Guides/ModOptions/#how-do-i-use-modoptions

        #region Mod Options

        private const string CATEGORY_ACTIVATE = "Activation / Deactivation";
        private const string CATEGORY_FLIGHTPROPS = "Flight Properties";
        private const string CATEGORY_SOUNDS = "Sounds";        // TODO: add this
        private const string CATEGORY_SCALE = "Player Size";

        //[ModOptionTextDisplay("description of section", null)]
        //[ModOption("Info")]
        //private static void label1(string value) { }

        [ModOption(name: "Use Jetpack Mod", tooltip: "Turns on/off the Jetpack mod")]
        public static bool UseJetpackMod = true;

        // ******************** Activation / Deactivation ********************

        public static ModOptionString[] FlightActivation_Options = new[]
        {
            new ModOptionString("Hold Up (right stick)", null, FlightActivationType.HoldUp.ToString()),
            //new ModOptionString("Hold Jump", null, FlightActivationType.HoldJump.ToString()),     // TODO: check the difference between jump on click and jump on up
            //new ModOptionString("Double Jump", null, FlightActivationType.DoubleJump.ToString()),
            new ModOptionString("Double Click Thumbpad", null, FlightActivationType.DoubleClick_Thumbpad.ToString()),
            new ModOptionString("Hold The Bird", null, FlightActivationType.HoldBird.ToString()),     // 🖕
            new ModOptionString("Hold Peace Sign", null, FlightActivationType.HoldPeace.ToString()),      // ✌️
            new ModOptionString("Hold Devil Horns", null, FlightActivationType.HoldDevilHorns.ToString()),        // 🤘
            new ModOptionString("Hold Rock On", null, FlightActivationType.HoldRockOn.ToString()),        // 🤟
        };

        private static string _flightActivation = FlightActivationType.HoldUp.ToString();
        private static FlightActivationType _flightActivation_cast = FlightActivationType.HoldUp;

        [ModOptionCategory(CATEGORY_ACTIVATE, -99)]
        [ModOption(name: "Flight Activation/Deactivation", tooltip: "How to activate flight (some options will also be used to deactivate)\n\nThe hold options are for controllers that have finger tracking", valueSourceName: nameof(FlightActivation_Options), order = 0)]
        public static string FlightActivation
        {
            get
            {
                return _flightActivation;
            }
            set
            {
                _flightActivation = value;

                if (!Enum.TryParse<FlightActivationType>(value, out _flightActivation_cast))
                    Debug.Log($"Couldn't parse FlightActivationType: {value}.  Leaving it as {_flightActivation_cast}");
            }
        }

        [ModOptionCategory(CATEGORY_ACTIVATE, -99)]
        [ModOption(name: "Stop flying on ground", tooltip: "Whether to stop flight when on the ground", order = 1)]
        public static bool DeactivateOnGround = true;

        [ModOptionCategory(CATEGORY_ACTIVATE, -99)]
        [ModOption(name: "Require Both Hands", tooltip: "Options that are double click or gestures can be required to be done at the same time by both hands or just one\n\nSingle hand is easier but may cause misreads", order = 2)]
        public static bool RequireBothHands = true;

        // ******************** Flight Properties ********************

        [ModOptionCategory(CATEGORY_FLIGHTPROPS, -99)]
        [ModOptionSlider]
        [ModOption(name: "Horizontal Accel", tooltip: "How hard to accelerate horizontally", order = 0)]
        [ModOptionFloatValues(0, 24, 0.25f)]
        public static float HorizontalSpeed = 9;

        [ModOptionCategory(CATEGORY_FLIGHTPROPS, -99)]
        [ModOptionSlider]
        [ModOption(name: "Vertical Accel", tooltip: "How hard to accelerate vertically", order = 1)]
        [ModOptionFloatValues(0, 12, 0.25f)]
        public static float VerticalForce = 6;     // discussion on discord was saying defaultValueIndex is ignored in 1.0.3, need to explicitely set a value

        [ModOptionCategory(CATEGORY_FLIGHTPROPS, -99)]
        [ModOptionSlider]
        [ModOption(name: "Drag", tooltip: "Wind resistance", order = 2)]
        [ModOptionFloatValues(0, 2, 0.05f)]
        public static float Drag = 0.9f;

        [ModOptionCategory(CATEGORY_FLIGHTPROPS, -99)]
        [ModOptionSlider]
        [ModOption(name: "Gravity", tooltip: "0 is no gravity.  9.8 is standard", order = 3)]
        [ModOptionFloatValues(0, 18, 0.1f)]
        public static float GravitySetting = 0f;

        // ******************** Player Size ********************

        [ModOptionCategory(CATEGORY_SCALE, -99)]
        [ModOptionSlider]
        [ModOption(name: "Player Size %", tooltip: "Can shrink the player so there is more room to fly")]
        [ModOptionFloatValues(0, 100, 1f)]
        public static float playerScale = 100;

        public static ModOptionString[] scaleApplyButtonLabel = new[]
        {
            new ModOptionString("Apply Scale", "Apply Scale")
        };

        [ModOptionCategory(CATEGORY_SCALE, -99)]
        [ModOptionButton]
        [ModOption("Set scale to current settings", null, nameof(scaleApplyButtonLabel))]      // "Push the button" is the label to the left of the button
        public static void OnApplyScale(string value)
        {
            Debug.Log("OnApplyScale Pressed");
        }

        public static ModOptionString[] scaleRevertButtonLabel = new[]
        {
            new ModOptionString("Default Scale", "Default Scale")
        };

        [ModOptionCategory(CATEGORY_SCALE, -99)]
        [ModOptionButton]
        [ModOption("Put scale back to normal", null, nameof(scaleRevertButtonLabel))]      // "Push the button" is the label to the left of the button
        public static void OnRevertScale(string value)
        {
            Debug.Log("OnRevertScale Pressed");
        }

        #endregion

        // PRE-FLIGHT DATA
        private FlightData _old = null;

        // FLIGHT DATA
        private Locomotion _loco = null;
        private bool _isFlying = false;
        private bool _markedToFly = false;      // will fly once not grounded
        private float _last_applied_drag = -1;

        private FlightTransitionWatcher _transitions = new FlightTransitionWatcher();

        //private DebugRenderer3D _renderer = null;

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);

            //Debug.Log("Wiring up DebugRenderer3D");
            //_renderer = Player.local.gameObject.AddComponent<DebugRenderer3D>();
            //Debug.Log("Wired up DebugRenderer3D");

            ReportResourceMaterials2();
        }

        public override void ScriptUpdate()
        {
            base.ScriptUpdate();

            bool should_switch = _transitions.Update(_flightActivation_cast, RequireBothHands, DeactivateOnGround, _isFlying);

            if (should_switch)
            {
                PlaySounds.Play(SoundName.Jetpack_Activate, cache_effect: false);       // for some reason, the cached version only plays once.  Maybe it gets disabled once the the sound stops?  or needs to be reset somehow?

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

            if (_isFlying && DeactivateOnGround && Player.local.locomotion.isGrounded)
                DeactivateFly();

            if (_markedToFly && !Player.local.locomotion.isGrounded)
                ActivateFly();

            if (_isFlying && _last_applied_drag != Drag)
            {
                _loco.physicBody.drag = Drag;
                _last_applied_drag = Drag;
            }
        }

        public override void ScriptFixedUpdate()
        {
            base.ScriptFixedUpdate();

            if (Player.currentCreature)
            {
                if (_isFlying && !Player.local.locomotion.isGrounded)
                {
                    DestabilizeHeldNPC(Player.local.handLeft);
                    DestabilizeHeldNPC(Player.local.handRight);

                    // TODO: make an option for horiztonal control mode (direct or accel)
                    //_loco.horizontalAirSpeed = horizontalSpeed / 100f;

                    AccelHorz(InputUtil.GetLeftStick());
                    AccelUp(InputUtil.GetRightStick());
                }
            }
            else
            {
                _isFlying = false;
            }
        }

        private void AccelHorz(Vector2 axis)
        {
            if (axis.x == 0 && axis.y == 0)
                return;

            var transform = Player.local.transform;

            // TODO: may need to project transform's forward and right to horizontal plane

            _loco.physicBody.AddForce(transform.forward * HorizontalSpeed * axis.y, ForceMode.Acceleration);
            _loco.physicBody.AddForce(transform.right * HorizontalSpeed * axis.x, ForceMode.Acceleration);
        }
        private void AccelUp(Vector2 axis)
        {
            float up_accel = 0f;

            float axis_y = axis.y;

            if (axis_y != 0.0 && (!Pointer.GetActive() || !Pointer.GetActive().isPointingUI))
            {
                up_accel = VerticalForce * axis_y;

                if (axis_y > 0)
                    up_accel += GravitySetting;     // when pushing up, cancel out gravity.  When pushing down, it's accelerating down in addition to gravity
            }

            up_accel -= GravitySetting;

            _loco.physicBody.AddForce(Vector3.up * up_accel, ForceMode.Acceleration);
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

            if (!UseJetpackMod)
            {
                Debug.Log("ActivateFly() called, but mod is disabled");
                return;
            }

            _loco = Player.local.locomotion;



            // TODO: it's risky storing these in flight activate.  If this function is called twice before deactivating flight, the values will be corrupt

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

                Height = Player.local.creature.GetHeight(),
                Morphology = Player.local.creature.morphology.Clone(),
                HeadLocalPosition = Player.local.headOffsetTransform.localPosition,     // this is zero
            };



            // ENABLE FLIGHT STATS
            _isFlying = true;
            _loco.groundAngle = -359f;
            _loco.physicBody.useGravity = false;        // this mod will do its own gravity, if the slider is non zero
            //_loco.physicBody.mass = 100000f;

            _loco.physicBody.drag = Drag;
            _last_applied_drag = Drag;

            _loco.velocity = Vector3.zero;
            Player.fallDamage = false;
            Player.crouchOnJump = false;
            //GameManager.options.allowStickJump = false;       // this doesn't seem to affect anything

            //ApplyScale();


            //PlaySounds.Play(SoundName.Jetpack_Activate);

            //Debug.Log($"Activating Flight:\r\n{JsonUtility.ToJson(_old, true)}");
            /*
{{
    "Drag": 0.30000001192092898,
    "Mass": 70.0,
    "HorizontalSpeed": 0.03999999910593033,
    "VerticalSpeed": 0.0,
    "MaxAngle": 2.7823486328125,
    "FallDamage": true,
    "CrouchOnJump": true,
    "StickJump": true,
    "Height": 1.895983338356018,
    "Morphology": {{
        "eyesHeight": 1.9534728527069092,
        "eyesForward": 0.11809086799621582,
        "headHeight": 1.8641363382339478,
        "headForward": 0.031885743141174319,
        "chestHeight": 1.2766860723495484,
        "spineHeight": 1.0798486471176148,
        "hipsHeight": 1.0564287900924683,
        "armsSpacing": 0.36119115352630618,
        "armsLength": 0.6440020799636841,
        "armsHeight": 1.6500314474105836,
        "armsToEyesHeight": 0.0,
        "height": 2.0837044715881349,
        "legsLength": 1.01776123046875,
        "legsSpacing": 0.2640935182571411,
        "upperLegsHeight": 1.1216603517532349,
        "lowerLegsHeight": 0.5887104272842407,
        "footHeight": 0.1138991191983223
    }},
    "HeadLocalPosition": {{
        "x": 0.0,
        "y": 0.0,
        "z": 0.0
    }}
}}
            */


            // Creates a dot that stays where it's spawned
            //SpawnDot2();

            // Creates a dot that stays with transform.pos as the player flies around (on the floor, center of the irl room that player walks around)
            //SpawnDot3();
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

                //RevertScale();
            }

            //PlaySounds.Play(SoundName.Jetpack_Deactivate);
        }


        private void ApplyScale()
        {
            float scale = Mathf.Clamp(playerScale / 100f, 0.05f, 1);
            Player.local.transform.localScale = new Vector3(scale, scale, scale);
            Player.local.headOffsetTransform.localScale = new Vector3(scale, scale, scale);     // this helps, but the head slowly drifts forward

            //Player.local.handOffsetTransform.localScale = new Vector3(scale, scale, scale);       // transform and headOffsetTransform are pretty good, but handOffsetTransform makes the hands extra large as the scale gets small



            //Player.local.headOffsetTransform.localPosition =      // maybe


            Player.local.creature.morphology = new Morphology(_old.Morphology.eyesHeight * scale)
            {
                eyesHeight = _old.Morphology.eyesHeight * scale,
                eyesForward = _old.Morphology.eyesForward * scale,
                headHeight = _old.Morphology.headHeight * scale,
                headForward = _old.Morphology.headForward * scale,
                chestHeight = _old.Morphology.chestHeight * scale,
                spineHeight = _old.Morphology.spineHeight * scale,
                hipsHeight = _old.Morphology.hipsHeight * scale,
                armsSpacing = _old.Morphology.armsSpacing * scale,
                armsLength = _old.Morphology.armsLength * scale,
                armsHeight = _old.Morphology.armsHeight * scale,
                armsToEyesHeight = _old.Morphology.armsToEyesHeight * scale,
                height = _old.Morphology.height * scale,
                legsLength = _old.Morphology.legsLength * scale,
                legsSpacing = _old.Morphology.legsSpacing * scale,
                upperLegsHeight = _old.Morphology.upperLegsHeight * scale,
                lowerLegsHeight = _old.Morphology.lowerLegsHeight * scale,
                footHeight = _old.Morphology.footHeight * scale,
            };


            // I think the next step should be to look at the player model carefully.  See what all these transforms, creature, etc look like


            // This causes the hands to go nuts
            //Player.local.creature.SetHeight(_old.Height * scale);

        }
        private void RevertScale()
        {
            Player.local.transform.localScale = new Vector3(1, 1, 1);
            //Player.local.handOffsetTransform.localScale = new Vector3(1, 1, 1);
            Player.local.headOffsetTransform.localScale = new Vector3(1, 1, 1);

            //Player.local.creature.SetHeight(_old.Height);


            // TODO: _old.Morphology was set in activate flight, change this to some kind of startup event
            Player.local.creature.morphology = _old.Morphology;
        }


        private void SpawnDot()
        {
            Vector3 pos = Player.local.transform.position;
            Debug.Log($"Player.local.transform.position: {pos}");

            //pos = Player.local.transform.TransformPoint(pos);
            //Debug.Log($"Player.local.transform.TransformPoint: {pos}");

            Debug.Log($"Adding debug dot: {pos.ToStringSignificantDigits(2)}");
            //_renderer.AddDot(pos, 1f, UtilityUnity.ColorFromHex("FF0"));



            // This is invisible, but still darkens the area
            //  maybe the wrong shader?
            //  maybe needs a different layer?
            //
            // It would probably be better to follow a tutorial and make a simple weapon mod.  That might show what is going wrong



            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            //Collider collider = dot.GetComponent<Collider>();
            //if (collider != null)
            //    UnityEngine.Object.Destroy(collider);

            dot.transform.position = pos;
            dot.transform.localScale = new Vector3(9, 9, 9);

            MeshRenderer mesh = dot.GetComponent<MeshRenderer>();


            // This goes straight for the shader
            //mesh.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            // This creates a new instance of SimpleLit
            //Material simpleLitCopy = Instantiate(Resources.Load<Material>("Packages/Universal RP/Runtime/Materials/SimpleLit"));


            //mesh.material = Material.

            //mesh.material.color = Color.yellow;
            //mesh.material.SetColor("Base Map", Color.yellow);


            Debug.Log($"Added debug dot: {pos.ToStringSignificantDigits(2)}");
        }
        private void SpawnDot2()
        {
            Vector3 pos = Player.local.transform.position;
            Debug.Log($"Adding debug dot: {pos.ToStringSignificantDigits(2)}");

            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            Collider collider = dot.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);

            dot.transform.position = pos;
            dot.transform.localScale = new Vector3(9, 9, 9);

            MeshRenderer mesh = dot.GetComponent<MeshRenderer>();

            // Can also use "ThunderRoad/LitMoss"
            // D:\blade and sorcery\BasSDK_2\Assets\SDK\Shaders\Lit_Moss.shader
            // https://kospy.github.io/BasSDK/Components/Guides/Shader/LitMoss.html

            mesh.material.shader = Shader.Find("Sprites/Default");      // BREAD — the default shader that's applied isn't in the game, need to change it

            //mesh.material.SetColor("_BaseColor", Color.yellow);     // doesn't do anything
            mesh.material.color = StaticRandom.ColorHSV();

            Debug.Log($"Added debug dot: {pos.ToStringSignificantDigits(2)}, {mesh.material.name}");
        }
        private void SpawnDot3()
        {
            // lyneca, bladey dancey man:
            // Call this in a loop to make a dot that moves to transform.position every frame
            // ThunderRoad.DebugViz.Viz.Dot(this, Player.local.transform.position).Color(Color.blue);

            Debug.Log($"Adding VizDot");

            EffectData effect_data = Catalog.GetData<EffectData>("PerfNormBeastJetpackVizDot2");
            EffectInstance instance = effect_data.Spawn(Player.local.transform);
            instance.Play();

            Debug.Log($"Added VizDot");
        }

        private static void ReportResourceMaterials()
        {
            Material[] materials = Resources.LoadAll<Material>(""); // The empty string "" means it will load all materials from the "Resources" folder and its subfolders.

            string[] paths = materials.
                //Select(o => AssetDatabase.GetAssetPath(o)).
                //Select(o => "AssetDatabase is only available in UnityEditor").
                Select(o => o.name).
                SelectMany(o => SplitExtension(o)).
                SelectMany(o => GetAltPaths(o)).
                ToArray();

            var found = paths.
                Select(o => new
                {
                    path = o,
                    mat = Resources.Load<Material>(o),
                }).
                Where(o => o.mat != null).
                ToArray();

            //string report = string.Join("\n", found.Select(o => o.path));

            foreach (var item in found)
                Debug.Log(item.path);
        }
        private static string[] SplitExtension(string path)
        {
            Match match = Regex.Match(path, @"\.\w+$");

            if (!match.Success)
                return new[] { path };

            return new[]
            {
                path,
                path.Substring(0, match.Index),
            };
        }
        private static string[] GetAltPaths(string path)
        {
            string[] name_split = path.Split('/');

            string[] retVal = new string[name_split.Length];

            for (int i = 0; i < name_split.Length; i++)
                retVal[i] = string.Join("/", Enumerable.Range(i, name_split.Length - i).Select(o => name_split[o]));
            return retVal;
        }

        private static void ReportResourceMaterials2()
        {
            Material[] materials = Resources.LoadAll<Material>(""); // The empty string "" means it will load all materials from the "Resources" folder and its subfolders.

            Debug.Log("Resources.LoadAll<Material>(\"\");");

            foreach (Material material in materials)
                Debug.Log(material.name);
        }

    }
}
