using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public enum AudioCategory
    {
        Music = 0,
        Sfx = 1,
        Voice = 2
    }

    // Central volume control for the three audio levels in the Audio Control menu.
    // Sources register with their own base volume, and the level (0 to 10) scales it.
    public static class GameAudio
    {
        public const int MaxLevel = 10;

        private static readonly int[] DefaultLevels = { 7, 7, 10 };
        private static readonly string[] PrefKeys = { "audio.music", "audio.sfx", "audio.voice" };
        private static readonly int[] Levels = new int[3];
        private static readonly Dictionary<AudioSource, Entry> Entries = new Dictionary<AudioSource, Entry>();
        private static bool _loaded;
        private static int _registerCount;

        private class Entry
        {
            public AudioCategory Category;
            public float BaseVolume;
        }

        public static event Action Changed;

        private static void Load()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            for (int i = 0; i < Levels.Length; i++)
            {
                Levels[i] = DefaultLevels[i];
                try
                {
                    Levels[i] = Mathf.Clamp(PlayerPrefs.GetInt(PrefKeys[i], DefaultLevels[i]), 0, MaxLevel);
                }
                catch (Exception)
                {
                    // Preferences unavailable, keep the default.
                }
            }
        }

        public static int GetLevel(AudioCategory category)
        {
            Load();
            return Levels[(int)category];
        }

        public static float Scale(AudioCategory category)
        {
            return GetLevel(category) / (float)MaxLevel;
        }

        public static void SetLevel(AudioCategory category, int level)
        {
            Load();
            level = Mathf.Clamp(level, 0, MaxLevel);
            if (Levels[(int)category] == level)
            {
                return;
            }

            Levels[(int)category] = level;
            try
            {
                PlayerPrefs.SetInt(PrefKeys[(int)category], level);
                PlayerPrefs.Save();
            }
            catch (Exception)
            {
                // Not being able to save is not worth interrupting the game.
            }

            ApplyAll();
            if (Changed != null)
            {
                Changed();
            }
        }

        // Steps a level up or down. With wrap, going past the top returns to 0 (used for plain clicks).
        public static void ChangeLevel(AudioCategory category, int delta, bool wrap)
        {
            int level = GetLevel(category) + delta;
            if (wrap)
            {
                level = (level % (MaxLevel + 1) + (MaxLevel + 1)) % (MaxLevel + 1);
            }

            SetLevel(category, level);
        }

        public static AudioSource Register(AudioSource source, AudioCategory category)
        {
            if (source == null)
            {
                return source;
            }

            if (++_registerCount % 128 == 0)
            {
                Prune();
            }

            Entry entry;
            if (!Entries.TryGetValue(source, out entry))
            {
                entry = new Entry();
                Entries[source] = entry;
            }

            entry.Category = category;
            entry.BaseVolume = source.volume;
            source.volume = entry.BaseVolume * Scale(category);
            return source;
        }

        // Use instead of setting AudioSource.volume directly on a registered source.
        public static void SetBaseVolume(AudioSource source, float volume)
        {
            if (source == null)
            {
                return;
            }

            Entry entry;
            if (Entries.TryGetValue(source, out entry))
            {
                entry.BaseVolume = volume;
                source.volume = volume * Scale(entry.Category);
            }
            else
            {
                source.volume = volume;
            }
        }

        private static void ApplyAll()
        {
            Prune();
            foreach (KeyValuePair<AudioSource, Entry> pair in Entries)
            {
                pair.Key.volume = pair.Value.BaseVolume * Scale(pair.Value.Category);
            }
        }

        private static void Prune()
        {
            List<AudioSource> dead = null;
            foreach (AudioSource source in Entries.Keys)
            {
                if (source == null)
                {
                    if (dead == null)
                    {
                        dead = new List<AudioSource>();
                    }

                    dead.Add(source);
                }
            }

            if (dead != null)
            {
                foreach (AudioSource source in dead)
                {
                    Entries.Remove(source);
                }
            }
        }
    }
}
