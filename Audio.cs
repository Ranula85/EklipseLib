using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;
namespace EklipseLib
{
    public class AudioManager
    {
        public static AudioManager Instance;
        public static void Initialize(string path = "")
        {
            if (!string.IsNullOrEmpty(path)) Instance = new AudioManager(path);
            else Instance = new AudioManager();
        }
        public float MasterVolume = 1f, MusicVolume = 1f, SfxVolume = 1f, Pitch = 1f;
        public Dictionary<string, SFX> sfx = new();
        public Dictionary<string, MUSIC> music = new();
        public List<MUSIC> LoopPlaying = new();
        public AudioManager()
        {
            Instance = this;
            Raylib.InitAudioDevice();
        }
        /// <summary>
        /// Searches for .wav files in the given folder path and on subfolders. <br></br>
        /// - Adds everything to SFX excepts files inside a folder caled MUSIC
        /// </summary>
        /// <param name="soundFolder"> The path to the folder to retreive sounds from</param>
        public AudioManager(string soundFolder)
        {
            Instance = this;
            Raylib.InitAudioDevice();
            FileInfo[] sounds = new DirectoryInfo(Raylib.GetApplicationDirectoryAsString() + soundFolder).GetFiles("*.wav", SearchOption.AllDirectories);
            foreach (FileInfo sound in sounds)
            {
                //check name to see if its music of sfx and add to directories
                if (sound.DirectoryName.EndsWith("MUSIC"))
                {
                    AddMusic(sound.Name, new MUSIC(sound.FullName));
                }
                else
                {
                    AddSFX(sound.Name, new SFX(sound.FullName));
                }
            }
        }
        public void AddSFX(string name, SFX sound)
        {
            if (!sfx.ContainsKey(name))
            {
                sfx.Add(name, sound);
            }
        }
        public void AddMusic(string name, MUSIC sound)
        {
            if (!music.ContainsKey(name))
            {
                music.Add(name, sound);
            }
        }
        public void RemoveSound(string name)
        {
            if (sfx.ContainsKey(name))
            {
                sfx.Remove(name);
            }
            else if (music.ContainsKey(name))
            {
                music.Remove(name);
            }
        }
        public void ClearMusic() { music.Clear(); }
        public void ClearSFX() { sfx.Clear(); }
        public void PlaySFX(string name, bool variation = false)
        {
            if (sfx.TryGetValue(name, out SFX? sound))
            {
                float pitchVariation = variation ? (float)(new Random().NextDouble() * 0.2 - 0.1) : 0;
                sound.Play(MasterVolume * SfxVolume, Pitch + pitchVariation);
            }
        }
        public void PlayMusic(string name, float time = 0)
        {
            if (music.TryGetValue(name, out MUSIC? sound))
            {
                sound.Play(MasterVolume * MusicVolume, Pitch, time);
                if (sound.Loops) LoopPlaying.Add(sound);
            }
        }
        public void PauseSound(string name)
        {
            if (sfx.TryGetValue(name, out SFX? _sfx)) //one of them will fail so no problem
            {
                _sfx?.Pause();
            }
            else if (music.TryGetValue(name, out MUSIC? _music))
            {
                _music?.Pause();
            }
        }
        public void ResumeSound(string name)
        {
            if (sfx.TryGetValue(name, out SFX? _sfx)) //one of them will fail so no problem
            {
                _sfx?.Resume();
            }
            else if (music.TryGetValue(name, out MUSIC? _music))
            {
                _music?.Resume();
            }
        }
        public void StopSound(string name)
        {
            if (sfx.TryGetValue(name, out SFX? _sfx)) //one of them will fail so no problem
            {
                _sfx?.Stop();
            }
            else if (music.TryGetValue(name, out MUSIC? _music))
            {
                _music?.Stop();
            }
        }
        public void Update()
        {
            foreach (MUSIC music in LoopPlaying)
            {
                if (!Raylib.IsMusicStreamPlaying(music.sound))
                {
                    music.Play(MasterVolume * MusicVolume, Pitch);
                }
            }
        }
    }
    public class SFX
    {
        public string Path;
        public float Volume = 1f, Pitch = 1f;
        Raylib_cs.Sound sound;
        public SFX(string path)
        {
            Path = path; sound = Raylib.LoadSound(path);
        }
        public void Play(float volumeMod = 1, float pitchMod = 1)
        {
            Raylib.SetSoundVolume(sound, Volume * volumeMod);
            Raylib.SetSoundPitch(sound, Pitch * pitchMod);
            Raylib.PlaySound(sound);
        }
        public void Pause()
        {
            Raylib.PauseSound(sound);
        }
        public void Resume()
        {
            Raylib.ResumeSound(sound);
        }
        public void Stop()
        {
            Raylib.StopSound(sound);
        }
    }
    public class MUSIC
    {
        public string Path;
        public float Volume = 1f, Pitch = 1f;
        public bool Loops = false;
        public Raylib_cs.Music sound { get; private set; }
        public MUSIC(string path, bool loops = false)
        {
            Path = path; sound = Raylib.LoadMusicStream(path); Loops = loops;
        }
        public void Play(float volumeMod = 1, float pitchMod = 1, float time = 0)
        {
            Raylib.SetMusicVolume(sound, Volume * volumeMod);
            Raylib.SetMusicPitch(sound, Pitch * pitchMod);
            Raylib.SeekMusicStream(sound, time);
        }
        public void Pause()
        {
            Raylib.PauseMusicStream(sound);
        }
        public void Resume()
        {
            Raylib.ResumeMusicStream(sound);
        }
        public void Stop()
        {
            Raylib.StopMusicStream(sound);
        }
    }
    public struct LoopingData
    {
        //might use later for looping sounds for point A to B, and breaking the loops and stuff maybe
    }
}