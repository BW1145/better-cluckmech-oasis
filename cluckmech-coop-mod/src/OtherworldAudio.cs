using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DarkTonic.MasterAudio;
using UnityEngine;
using UnityEngine.Networking;

namespace CluckmechCoop
{
    internal static class OtherworldAudio
    {
        private static AudioClip clip;
        private static Transform group;
        private static Coroutine loading;
        internal static void Begin()
        {if(group==null&&loading==null)loading=Plugin.Instance.StartCoroutine(Load());}
        private static IEnumerator Load()
        {
            while(Plugin.InCombat&&!MasterAudio.SoundsReady)yield return null;
            if(!Plugin.InCombat){loading=null;yield break;}
            if(clip==null)
            {
                var path=Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location),"OtherworldSounds","kamui.ogg");
                using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri,AudioType.OGGVORBIS))
                {
                    yield return request.SendWebRequest();
                    if(request.result!=UnityWebRequest.Result.Success){Plugin.Warn("[Otherworld] 神威音效加载失败");loading=null;yield break;}
                    clip=DownloadHandlerAudioClip.GetContent(request);
                }
            }
            var root=new GameObject("BCO.Otherworld.Kamui");
            var sound=root.AddComponent<DynamicSoundGroup>();
            sound.groupMasterVolume=.65f;sound.spatialBlendType=MasterAudio.ItemSpatialBlendType.ForceTo2D;
            sound.busName="音效总线";sound.isExistingBus=true;sound.limitPolyphony=true;sound.voiceLimitCount=2;
            var variation=new GameObject("kamui");variation.transform.SetParent(root.transform,false);
            var source=variation.AddComponent<AudioSource>();source.playOnAwake=false;source.clip=clip;source.volume=1;
            var settings=variation.AddComponent<DynamicGroupVariation>();settings.audLocation=MasterAudio.AudioLocation.Clip;settings.weight=1;
            sound.groupVariations=new List<DynamicGroupVariation>{settings};
            group=MasterAudio.CreateSoundGroup(sound,null,false);UnityEngine.Object.Destroy(root);loading=null;
        }
        internal static void Play(Vector2 point)
        {if(group!=null)MasterAudio.PlaySound3DAtVector3AndForget("BCO.Otherworld.Kamui",new Vector3(point.x,point.y,0),StratagemAudio.Gain(point));}
        internal static void Clear()
        {if(loading!=null)Plugin.Instance.StopCoroutine(loading);loading=null;}
    }
}
