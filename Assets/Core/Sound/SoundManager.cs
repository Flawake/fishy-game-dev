using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sounds
{
    public class SoundManager : MonoBehaviour
    {
        [Serializable]
        public struct AreaSound
        {
            public Area area;
            public SoundSO sound;
        }

        [SerializeField] public List<AreaSound> areaWalkSounds = new List<AreaSound>();
        [SerializeField] public SoundSO defaultWalkSound;
        [SerializeField] public List<AreaSound> areaThrowInSounds = new List<AreaSound>();
        [SerializeField] public SoundSO defaultThrowInSound;
        [SerializeField] public SoundSO buttonHoverSound;
        public static SoundManager instance;

        private Dictionary<Area, SoundSO> _walkSoundsByArea;
        private Dictionary<Area, SoundSO> _throwInSoundsByArea;

        private void Awake()
        {
            instance = this;

            _walkSoundsByArea = BuildAreaLookup(areaWalkSounds);
            _throwInSoundsByArea = BuildAreaLookup(areaThrowInSounds);
        }

        /// <summary>
        /// The walk sound of the given area, or the default walk sound when that area has no custom one
        /// </summary>
        public SoundSO GetWalkSound(Area area)
        {
            return GetAreaSound(_walkSoundsByArea, area, defaultWalkSound);
        }

        /// <summary>
        /// The throw in sound of the given area, or the default throw in sound when that area has no custom one
        /// </summary>
        public SoundSO GetThrowInSound(Area area)
        {
            return GetAreaSound(_throwInSoundsByArea, area, defaultThrowInSound);
        }

        /// <summary>
        /// The area the client is currently in. Only meaningful on a client, the server has players from every area.
        /// </summary>
        public static Area CurrentClientArea
        {
            get { return SceneToAreaMapper.GetAreaFromSceneName(GameNetworkManager.ClientsActiveScene.name); }
        }

        private static Dictionary<Area, SoundSO> BuildAreaLookup(List<AreaSound> areaSounds)
        {
            Dictionary<Area, SoundSO> lookup = new Dictionary<Area, SoundSO>();
            foreach (AreaSound areaSound in areaSounds)
            {
                if (areaSound.sound == null)
                {
                    continue;
                }
                lookup[areaSound.area] = areaSound.sound;
            }
            return lookup;
        }

        private static SoundSO GetAreaSound(Dictionary<Area, SoundSO> lookup, Area area, SoundSO defaultSound)
        {
            if (lookup != null && lookup.TryGetValue(area, out SoundSO areaSound))
            {
                return areaSound;
            }
            return defaultSound;
        }

        public void Play(SoundSO sound, AudioSource source)
        {
            if (sound == null)
            {
                return;
            }
            Play(sound.GetClip(), source, sound.volume, sound.pitch);
        }

        public void Play(AudioClip sound, AudioSource source, float volume, float pitch)
        {
            if (sound != null && sound != null)
            {
                source.clip = sound;
                source.pitch = pitch;
                source.volume = volume;
                source.Play();
            }
        }
    }
}
