using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DarkTonic.MasterAudio;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace CluckmechCoop
{
    internal static class OtherworldAudio
    {
        private static readonly string[] names={"kamui","perlica-shot","perlica-thunder","perlica-orbital","obito-throw","obito-fire"};
        private static readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        private static readonly Dictionary<string,Transform> groups=new Dictionary<string,Transform>();
        private static Coroutine loading;
        internal static void Begin()
        {if(Plugin.InCombat&&loading==null)loading=Plugin.Instance.StartCoroutine(Load());}
        private static IEnumerator Load()
        {
            while(Plugin.InCombat&&!MasterAudio.SoundsReady)yield return null;
            if(!Plugin.InCombat){loading=null;yield break;}
            string directory=Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location),"OtherworldSounds");
            foreach(string name in names)
            {
                if(!clips.TryGetValue(name,out var clip))
                {
                    using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(Path.Combine(directory,name+".ogg")).AbsoluteUri,AudioType.OGGVORBIS))
                    {
                        yield return request.SendWebRequest();
                        if(request.result!=UnityWebRequest.Result.Success){Plugin.Warn("[Otherworld] Sound load failed: "+name);continue;}
                        clip=DownloadHandlerAudioClip.GetContent(request);clips[name]=clip;
                    }
                }
                if(groups.TryGetValue(name,out var registered)&&registered!=null)continue;
                string key=GroupName(name);
                var root=new GameObject(key);
                var sound=root.AddComponent<DynamicSoundGroup>();
                sound.groupMasterVolume=name=="kamui"?.75f:.65f;sound.spatialBlendType=MasterAudio.ItemSpatialBlendType.ForceTo2D;
                sound.busName="音效总线";sound.isExistingBus=true;sound.limitPolyphony=true;sound.voiceLimitCount=2;
                var variation=new GameObject(name);variation.transform.SetParent(root.transform,false);
                var source=variation.AddComponent<AudioSource>();source.playOnAwake=false;source.clip=clip;source.volume=1;
                var settings=variation.AddComponent<DynamicGroupVariation>();settings.audLocation=MasterAudio.AudioLocation.Clip;settings.weight=1;
                sound.groupVariations=new List<DynamicGroupVariation>{settings};
                groups[name]=MasterAudio.CreateSoundGroup(sound,null,false);UnityEngine.Object.Destroy(root);
            }
            loading=null;
        }
        internal static void Play(Vector2 point)
        {Play("kamui",point);}
        internal static void Play(string name,Vector2 point)
        {if(groups.TryGetValue(name,out var group)&&group!=null)MasterAudio.PlaySound3DAtVector3AndForget(GroupName(name),new Vector3(point.x,point.y,0),StratagemAudio.Gain(point));}
        private static string GroupName(string name)=>"BCO.Otherworld."+(name=="kamui"?"Kamui":name);
        internal static void Normal(OtherworldActor actor)
        {
            if(actor.Space==OtherworldWorld.Space(OtherworldActions.Player(Plugin.Role)?.gameObject))
                Play(actor.Hero==12?"perlica-shot":"obito-throw",actor.transform.position);
        }
        internal static void Clear()
        {if(loading!=null)Plugin.Instance.StopCoroutine(loading);loading=null;}
    }
    [HarmonyPatch]
    internal static class OtherworldAttackSound
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {yield return AccessTools.Method(typeof(模块_玩家音效控制),"攻击发动");yield return AccessTools.Method(typeof(模块_单位音效控制),"攻击发动");}
        private static bool Prefix(Component __instance)=>__instance.GetComponent<OtherworldActor>()==null;
    }
}
