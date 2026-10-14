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
        private static readonly string[] names = {"barrage","eagle","laser-start","laser-loop"};
        private static readonly Dictionary<string,AudioClip> clips = new Dictionary<string,AudioClip>();
        private static readonly Dictionary<string,Transform> groups = new Dictionary<string,Transform>();
        private static Coroutine loading;
        private static readonly HashSet<SoundGroupVariation> beams=new HashSet<SoundGroupVariation>();
        private static Camera camera;
        internal static void Begin()
        { camera=Camera.main;if (loading == null && ModuleSettings.Extra.Stratagems) loading = Plugin.Instance.StartCoroutine(Load()); }
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
                dynamic.busName="音效总线";dynamic.isExistingBus=true;
                dynamic.limitPolyphony = true; dynamic.voiceLimitCount = name == "barrage" ? 4 : name=="laser-loop"?6:2;
                var variation = new GameObject(name); variation.transform.SetParent(source.transform,false);
                var audio = variation.AddComponent<AudioSource>(); audio.clip = clip; audio.volume = 1; audio.pitch = 1; audio.loop = name == "laser-loop";
                var settings = variation.AddComponent<DynamicGroupVariation>(); settings.audLocation = MasterAudio.AudioLocation.Clip; settings.weight = 1;
                dynamic.groupVariations = new List<DynamicGroupVariation> { settings };
                groups[name] = MasterAudio.CreateSoundGroup(dynamic,null,false);
                UnityEngine.Object.Destroy(source);
            }
            loading = null;
        }
        internal static void Play(string name,Vector2 position)
        { if (groups.TryGetValue(name,out var group) && group != null) MasterAudio.PlaySound3DAtVector3AndForget("BCO.Stratagem."+name,new Vector3(position.x,position.y,0),Gain(position)); }
        internal static float Gain(Vector2 point)
        {
            var player=StratagemWorld.Player(Plugin.Role);
            Vector2 listener=player!=null?(Vector2)player.transform.position:camera!=null?(Vector2)camera.transform.position:point;
            return Mathf.Clamp01(1-Mathf.Max(0,Vector2.Distance(point,listener)-4)/24);
        }
        internal static SoundGroupVariation BeamStarted(Vector2 position)
        {
            if(!groups.TryGetValue("laser-loop",out var group)||group==null)return null;
            var result=MasterAudio.PlaySound3DAtVector3("BCO.Stratagem.laser-loop",new Vector3(position.x,position.y,0),Gain(position));
            var variation=result?.ActingVariation;if(variation!=null)beams.Add(variation);return variation;
        }
        internal static void BeamEnded(SoundGroupVariation variation)
        { if(variation!=null && beams.Remove(variation))variation.Stop(); }
        internal static void Clear()
        {
            if (loading != null) Plugin.Instance.StopCoroutine(loading);
            loading = null;foreach(var beam in beams)if(beam!=null)beam.Stop();beams.Clear();
        }
    }
}
