using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack
{
    public class PlaySounds
    {
        private static Lazy<PlaySounds> _instance = new Lazy<PlaySounds>(() => new PlaySounds());

        private readonly object _lock = new object();       // overkill since unity should be single threaded, but I can't bring myself to write shitty code

        private readonly Dictionary<SoundName, EffectInstance> _cache = new Dictionary<SoundName, EffectInstance>();

        /// <summary>
        /// Plays a sound at the specified transform
        /// </summary>
        /// <param name="play_at">If null is passed in, then Player.local.transform is used</param>
        /// <param name="cache_effect">Only cache it if it's a consistent transform every time the sound is played (like player)</param>
        public static void Play(SoundName name, Transform play_at = null, bool cache_effect = true)
        {
            play_at = play_at ?? Player.local.transform;

            if (cache_effect)
                Play_Cache(name, play_at);
            else
                Catalog.GetData<EffectData>(GetAddressableID(name)).Spawn(play_at).Play();
        }

        public static string GetAddressableID(SoundName name)
        {
            // These are defined in unity sdk Addressables Groups tab (see the readme in !HowTo of the repo)

            switch (name)
            {
                case SoundName.Jetpack_Activate:
                    return "PerfNormBeastJetpackActivateFlight";

                case SoundName.Jetpack_Deactivate:
                    return "PerfNormBeastJetpackDeactivateFlight";

                default:
                    throw new ArgumentException($"Unknown {nameof(SoundName)}: {name}");
            }
        }

        private static void Play_Cache(SoundName name, Transform play_at)
        {
            var instance = _instance.Value;

            lock (instance._lock)
            {
                if (!instance._cache.TryGetValue(name, out EffectInstance effect))
                {
                    effect = Catalog.GetData<EffectData>(GetAddressableID(name)).Spawn(play_at);
                    instance._cache.Add(name, effect);
                }

                effect.Play();      // hopefully this is async, otherwise this will hold the lock
            }
        }
    }

    // TODO: deactivate is so quiet you can't hear it
    // activate needs to be boosted in the json by 12 db
    // activate takes too long.  it should be sped up 3 or 4 times

    // TODO: make some debug beep sounds

    public enum SoundName
    {
        Jetpack_Activate,
        Jetpack_Deactivate,
    }
}
