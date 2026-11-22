using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace OccultSanctum
{
    /// (the user, 2026-11-22) the sanctum has its own music: SanctumMusic/*.ogg (or .wav/.mp3) next to the dll,
    /// played by the game's own MusicController (its volume, its on/off switch, its fades) while the hero is in the
    /// sanctum, in place of the dungeon music; it loops for as long as one stays there.
    public static class Music
    {
        static readonly List<AudioClip> clips = new List<AudioClip>();
        static bool loading;
        public static MonoBehaviour Host;

        public static void Load()
        {
            if (loading) return;
            loading = true;
            Host.StartCoroutine(LoadAll());
        }

        static IEnumerator LoadAll()
        {
            string dir = Path.Combine(SanctumPlugin.Dir, "SanctumMusic");
            if (!Directory.Exists(dir)) { SanctumPlugin.Log.LogInfo("Sanctum: no music folder"); yield break; }
            foreach (var f in Directory.GetFiles(dir))
            {
                string ext = Path.GetExtension(f).ToLowerInvariant();
                AudioType type = ext == ".ogg" ? AudioType.OGGVORBIS : ext == ".wav" ? AudioType.WAV : ext == ".mp3" ? AudioType.MPEG : AudioType.UNKNOWN;
                if (type == AudioType.UNKNOWN) continue;
                using (var rq = UnityWebRequestMultimedia.GetAudioClip(new Uri(f).AbsoluteUri, type))
                {
                    yield return rq.SendWebRequest();
                    if (rq.result != UnityWebRequest.Result.Success) { SanctumPlugin.Log.LogWarning("Sanctum: music " + f + ": " + rq.error); continue; }
                    var clip = DownloadHandlerAudioClip.GetContent(rq);
                    if (clip == null) continue;
                    clip.name = Path.GetFileNameWithoutExtension(f);
                    UnityEngine.Object.DontDestroyOnLoad(clip);
                    clips.Add(clip);
                    SanctumPlugin.Log.LogInfo("Sanctum: music " + clip.name + " (" + clip.length.ToString("0") + " s)");
                }
            }
            if (Flow.InSanctum) Start();
        }

        /// the sanctum's music now (on arrival)
        public static void Start()
        {
            try { if (clips.Count > 0 && MusicController.Instance != null) MusicController.Instance.NextClip(); } catch { }
        }

        internal static bool Pick(MusicController mc)
        {
            if (clips.Count == 0 || !Flow.InSanctum) return false;
            mc.ChangeSampleFuction(clips);
            return true;
        }
    }

    [HarmonyPatch(typeof(MusicController), "NextSample")]
    internal static class MusicPatch
    {
        static bool Prefix(MusicController __instance)
        {
            try { return !Music.Pick(__instance); } catch { return true; }
        }
    }
}
