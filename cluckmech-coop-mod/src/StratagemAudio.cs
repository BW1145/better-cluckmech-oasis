using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DarkTonic.MasterAudio;
using UnityEngine;
using UnityEngine.Networking;

namespace CluckmechCoop
{
    internal static class StratagemAudio
    {
        private static readonly string[] names = { "deploy","mine","barrage","eagle","laser-start","laser-loop","ems","shield-hit","shield-break","rover" };
        private static readonly Dictionary<string,AudioClip> clips = new Dictionary<string,AudioClip>();
        private static readonly Dictionary<string,Transform> groups = new Dictionary<string,Transform>();
        private static Coroutine loading;
        private static int beams;
        internal static void Begin()
        { if (loading == null && ModuleSettings.Extra.Stratagems) loading = Plugin.Instance.StartCoroutine(Load()); }
        private static IEnumerator Load()
        {
            while (Plugin.InCombat && !MasterAudio.SoundsReady) yield return null;
            if (!Plugin.InCombat) { loading = null; yield break; }
            string directory = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location),"StratagemSounds");
            foreach (string name in names)
            {
                if (!clips.TryGetValue(name,out var clip))
                {
                    using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(Path.Combine(directory,name+".ogg")).AbsoluteUri,AudioType.OGGVORBIS))
                    {
                        yield return request.SendWebRequest();
                        if (request.result != UnityWebRequest.Result.Success) { Plugin.Warn("[Stratagems] Sound load failed: "+name); continue; }
                        clip = DownloadHandlerAudioClip.GetContent(request); clips[name] = clip;
                    }
                }
                if (groups.TryGetValue(name,out var registered) && registered != null) continue;
                var source = new GameObject("BCO.Stratagem."+name);
                var dynamic = source.AddComponent<DynamicSoundGroup>();
                dynamic.groupMasterVolume = .65f; dynamic.spatialBlendType = MasterAudio.ItemSpatialBlendType.ForceTo2D;
                dynamic.limitPolyphony = true; dynamic.voiceLimitCount = name == "barrage" ? 4 : 2;
                var variation = new GameObject(name); variation.transform.SetParent(source.transform,false);
                var audio = variation.AddComponent<AudioSource>(); audio.clip = clip; audio.volume = 1; audio.pitch = 1; audio.loop = name == "laser-loop";
                var settings = variation.AddComponent<DynamicGroupVariation>(); settings.audLocation = MasterAudio.AudioLocation.Clip; settings.weight = 1;
                dynamic.groupVariations = new List<DynamicGroupVariation> { settings };
                groups[name] = MasterAudio.CreateSoundGroup(dynamic,null,false);
                UnityEngine.Object.Destroy(source);
                if (name == "laser-loop" && beams > 0) Play(name,Vector2.zero);
            }
            loading = null;
        }
        internal static void Play(string name,Vector2 position)
        { if (groups.TryGetValue(name,out var group) && group != null) MasterAudio.PlaySound3DAtVector3AndForget("BCO.Stratagem."+name,new Vector3(position.x,position.y,0)); }
        internal static void StopBeam() { if (groups.ContainsKey("laser-loop")) MasterAudio.StopAllOfSound("BCO.Stratagem.laser-loop"); }
        internal static void BeamStarted(Vector2 position) { if (++beams == 1) Play("laser-loop",position); }
        internal static void BeamEnded() { beams = Math.Max(0,beams-1); if (beams == 0) StopBeam(); }
        internal static void Clear()
        {
            if (loading != null) Plugin.Instance.StopCoroutine(loading);
            loading = null; beams = 0; StopBeam();
        }
    }
}
