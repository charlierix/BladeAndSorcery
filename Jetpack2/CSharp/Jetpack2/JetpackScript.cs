using Jetpack2.Core;
using Jetpack2.DebugCode;
using Jetpack2.FlightProcessing;
using Jetpack2.InputWatchers;
using Jetpack2.Models;
using Jetpack2.Scanning;
using PerfectlyNormalBaS;
using System;
using System.IO;
using System.Reflection;
using ThunderRoad;
using UnityEngine;



// TODOS:
// have floating orbs that make it easy to toggle rotate to looks.  digging through the menu is tedious, this may
// be a decision to toggle for only a small amount of time


// make a class that rotates to velocity


// have an option to muffle accels when in odd orientations.  especially when indoors


// have an option for gravity to be relative to player's feet direction


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


// separate out mod config ui settings from json settings (fine details of ground repel should be json)


// make a way to virtually grab stationary items like trees, boulders and swing around


// make a spell that fires a "drone" put a visual of a stone of the player and have the drone fly around


namespace Jetpack2
{
    // Source: https://github.com/sjankowskim/wings

    public class JetpackScript : ThunderScript
    {
        // https://kospy.github.io/BasSDK/Components/Guides/ModOptions/#how-do-i-use-modoptions

        private bool _isFlying = false;
        private bool _markedToFly = false;      // will fly once not grounded
        private float _last_applied_drag = -1;

        private RayCastStorage _raycast_storage = null;
        private FlightTransitionWatcher _transitions = null;
        private PlayerRotator _rotator = null;
        private FlightJetpack _flight_jetpack = null;

        private DebugVisuals _debugVisuals = new DebugVisuals();
        private DebugStats _debugStats = new DebugStats();
        private VisualizePlayerPoints _visualizePlayerPoints = new VisualizePlayerPoints();
        private ScaleAdjuster _scaleAdjuster = new ScaleAdjuster();

        private bool _isPlayerSpawned = false;

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);

            _raycast_storage = new RayCastStorage();
            _transitions = new FlightTransitionWatcher();
            _rotator = new PlayerRotator();
            _flight_jetpack = new FlightJetpack(_raycast_storage, _rotator, _debugStats);

            //MaterialShaderFinder.Report();

            Player.onSpawn += Player_onSpawn;
            Player.onDespawn += Player_onDespawn;

            //TryLoadConfig();
            //TryLoadConfig2();
            TryLoadConfig3();
        }

        private void Player_onSpawn(Player player)
        {
            _isPlayerSpawned = true;

            if (Player.local != null)
            {
                //Debug.Log("showing morphology");
                Player.local.showMorphology = true;
            }

            _rotator.OnPlayerSpawned();
            _debugStats.Clear();
        }
        private void Player_onDespawn(Player player)
        {
            _isPlayerSpawned = false;

            _rotator.OnPlayerDespawned();
            _debugStats.Clear();
            _debugVisuals.RemoveVisuals();
            _visualizePlayerPoints.Clear();

            DeactivateFly();
        }

        public override void ScriptUpdate()
        {
            // TODO: use Time.deltaTime to get time since last update

            base.ScriptUpdate();

            if (!_isPlayerSpawned || Player.local == null)
                return;

            _visualizePlayerPoints.Update(UIModOptions.playerScale / 200);
            _debugStats.Update_Pre();

            bool should_switch = _transitions.Update(UIModOptions.FlightActivation_cast, UIModOptions.RequireBothHands, UIModOptions.DeactivateOnGround, _isFlying);

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

            if (_isFlying && UIModOptions.DeactivateOnGround && Player.local.locomotion.isGrounded)
                DeactivateFly();

            if (_markedToFly && !Player.local.locomotion.isGrounded)
                ActivateFly();

            if (UIModOptions.ShowDebugStats && _isPlayerSpawned)
            {
                PopulateDebug();
                _debugStats.Update_Final();
            }
        }
        public override void ScriptFixedUpdate()
        {
            // TODO: use Time.fixedDeltaTime to get time since last fixed update

            base.ScriptFixedUpdate();

            if (Player.currentCreature)
            {
                if (_isFlying && !Player.local.locomotion.isGrounded)
                    _flight_jetpack.Update(UIModOptions.Drag, UIModOptions.HorizontalAccel, UIModOptions.VerticalAccel, UIModOptions.GravitySetting);
            }
            else
            {
                _isFlying = false;
            }
        }

        private void ActivateFly()
        {
            _markedToFly = false;

            if (!UIModOptions.UseJetpackMod)
            {
                //Debug.Log("ActivateFly() called, but mod is disabled");
                return;
            }

            _isFlying = true;

            _flight_jetpack.Activate(UIModOptions.Drag);

            _debugVisuals.AddVisuals();


            // TODO: look at scale/visibility settings and apply if checked
            // though it may be better to leave scale alone and turn off collision detection.  make the player hidden and show a small avatar instead



            //PlaySounds.Play(SoundName.Jetpack_Activate);
        }
        private void DeactivateFly()
        {
            _isFlying = false;

            _flight_jetpack.Deactivate();
            _rotator.Reset();

            //PlaySounds.Play(SoundName.Jetpack_Deactivate);
        }

        private void PopulateDebug()
        {
            _debugStats.AddEntry("is flying", _isFlying.ToString());
            _debugStats.AddEntry("is grounded", Player.local.locomotion.isGrounded.ToString());
            _debugStats.AddEntry("is marked to fly", _markedToFly.ToString());

            Vector3 velocity = Player.local.locomotion.physicBody.velocity;
            _debugStats.AddEntry("velocity", velocity.ToStringSignificantDigits(2));
            _debugStats.AddEntry("vel speed", velocity.magnitude.ToStringSignificantDigits(2));

            _debugStats.AddEntry("stick left", InputUtil.GetLeftStick().ToStringSignificantDigits(2));
            _debugStats.AddEntry("stick right", InputUtil.GetRightStick().ToStringSignificantDigits(2));
        }

        #region debug research

        private static void TryLoadConfig()
        {
            try
            {
                //2025-10-18T04:32:09.078 ERROR ThunderRoad.ModManager.LoadThunderScripts       : [ModManager][ThunderScript][Jetpack2] Exception during ThunderScript ScriptLoaded for: Jetpack2.JetpackScript on mod: Jetpack2 in assembly: Jetpack2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null, System.ArgumentException: Invalid path
                //  at System.IO.Path.GetDirectoryName (System.String path) [0x0000d] in <80e08c2cc04049bf931fc9038d04f397>:0 
                //  at Jetpack2.JetpackScript.TryLoadConfig () [0x0000b] in <b850afc2f47b4688ba4d419a27caa709>:0 
                //  at Jetpack2.JetpackScript.ScriptLoaded (ThunderRoad.ModManager+ModData modData) [0x0006b] in <b850afc2f47b4688ba4d419a27caa709>:0 
                //  at ThunderRoad.ModManager.LoadThunderScripts (System.Type type, ThunderRoad.ModManager+ModData mod, System.Reflection.Assembly assembly) [0x000b4] in D:\VSTS-Agent\_work\TR_2021.3.38f1\Assets\SDK\Scripts\ModManager.cs:1167 

                string current_folder = Assembly.GetExecutingAssembly().Location;       // this returns empty string
                Debug.Log($"current_folder: {current_folder}");

                current_folder = @"D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2";
                Debug.Log($"current_folder (hardcoded): {current_folder}");

                var files = Directory.GetFiles(current_folder);
                Debug.Log($"files: {string.Join(Environment.NewLine, files)}");     // this finds the files in that folder



                // This seems to be the way to get at custom files in the mod folder.  But json configs should be handled
                // with how TryLoadConfig2 does it

                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\catalog_Jetpack2.hash
                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\catalog_Jetpack2.json
                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\config.json
                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\Jetpack2.dll
                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\jetpack2assets_assets_all.bundle
                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\manifest.json
                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\StreamingAssets\Mods\Jetpack2\PerfectlyNormalBaS.dll
                files = Directory.GetFiles(".");
                Debug.Log($"files (.): {string.Join(Environment.NewLine, files)}");






                // D:\SteamLibrary\steamapps\common\Blade & Sorcery
                Debug.Log($"Directory.GetCurrentDirectory(): {Directory.GetCurrentDirectory()}");


                // D:\SteamLibrary\steamapps\common\Blade & Sorcery\BladeAndSorcery_Data\Managed
                string test = Directory.GetParent(typeof(object).Module.FullyQualifiedName).FullName;
                Debug.Log($"Directory.GetParent(typeof(object).Module.FullyQualifiedName).FullName: {test}");



                //File.ReadAllText();

            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
        }
        private void TryLoadConfig2()
        {
            try
            {
                TestConfigData _config = null;      // pretending this would be a member variable


                // from inside ConfigData.Init...
                // this happens early in game loading, so no need to instantiate, just use static.  intance is probably referenced by asking Catalog for it (or some other b&s static dictionary)
                // ConfigData init.  testInt: 52


                // about to instantiate ConfigData.  ConfigData.testInt: 52
                Debug.Log($"about to instantiate ConfigData.  ConfigData.testInt: {TestConfigData.testInt}");
                _config = new TestConfigData();

                // manually calling _config.Init()
                // System.NullReferenceException: Object reference not set to an instance of an object
                Debug.Log("manually calling _config.Init()");
                _config.Init();

                Debug.Log("after calling _config.Init()");
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
        }
        private void TryLoadConfig3()
        {
            try
            {
                Debug.Log($"ConfinedAreaData.FalloffPower: {ConfinedAreaData.falloffPower}");
                Debug.Log($"RepelGroundData.InverseSqr_MaxAccel: {RepelGroundData.inverseSqr_MaxAccel}");
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
        }

        #endregion
    }
}
