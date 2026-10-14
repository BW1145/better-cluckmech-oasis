using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CluckmechCoop;
using DarkTonic.MasterAudio;
using UnityEngine;

namespace HarmonyLib { [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch : Attribute { public HarmonyPatch(Type type,string name,Type[] args){} public HarmonyPatch(Type type,string name){} } }
namespace UnityEngine
{
    public class Object { public static void Destroy(Object value){} }
    public class AudioClip : Object { public string name; }
    public class MonoBehaviour : Object { public void StartCoroutine(IEnumerator body){Scheduler.Start(body);} }
    public static class Random { public static int Choice; public static int Range(int min,int max)=>Choice % max; }
    public enum AudioType { OGGVORBIS }
}
namespace UnityEngine.Networking
{
    public class DownloadHandlerAudioClip { public bool streamAudio; public static AudioClip GetContent(UnityWebRequest request)=>new AudioClip(); }
    public class UnityWebRequest : IDisposable
    {
        public enum Result { Success }
        public Result result=Result.Success;public string error;public DownloadHandlerAudioClip downloadHandler=new DownloadHandlerAudioClip();
        public object SendWebRequest()=>new object();public void Dispose(){}
    }
    public static class UnityWebRequestMultimedia { public static UnityWebRequest Last;public static UnityWebRequest GetAudioClip(string uri,AudioType type)=>Last=new UnityWebRequest(); }
}
namespace DarkTonic.MasterAudio
{
    public class MusicSetting
    {
        public string alias,resourceFileName;public AudioClip clip;public float volume=1,pitch=1;public bool isLoop;public MasterAudio.AudioLocation audLocation;
        public static MusicSetting Clone(MusicSetting song,MasterAudio.Playlist list)=>new MusicSetting{clip=song.clip,volume=song.volume,pitch=song.pitch,isLoop=song.isLoop,resourceFileName=song.resourceFileName};
    }
    public static class MasterAudio
    {
        public enum AudioLocation { Clip }
        public class Playlist
        {
            public string playlistName;public int songTransitionType,crossfadeMode;public float crossFadeTime;public bool fadeInFirstSong,fadeOutLastSong;public object songMetadataProps;
            public List<MusicSetting> MusicSettings=new List<MusicSetting>();
        }
        public static Dictionary<string,Playlist> All=new Dictionary<string,Playlist>();
        public static Playlist GrabPlaylist(string name,bool error=true)=>All.TryGetValue(name,out var list)?list:null;
        public static void CreatePlaylist(Playlist list,bool error)=>All.Add(list.playlistName,list);
    }
    public class PlaylistController
    {
        public bool isShuffle;public string LastPlaylist,LastClip;public int NativeRestarts;
        public string PlaylistName=>LastPlaylist;
        public void ChangePlaylist(string name,bool first,string clip=null){if(Plugin.Music.Change(this,ref name,ref clip,first)&&LastPlaylist!=name){LastPlaylist=name;LastClip=clip;}}
        public void StartPlaylist(string name,string clip=null)
        {
            if(!Plugin.Music.StartRequest(this,name,clip))return;
            if(LastPlaylist==name){NativeRestarts++;LastClip=clip;}else ChangePlaylist(name,true,clip);
        }
        public void NativeStart(string name){LastPlaylist=name;LastClip="native-auto";Plugin.Music.ControllerStarted(this);}
    }
}
namespace CluckmechCoop
{
    public enum ModuleId { Music }
    public static class ModuleSettings { public static bool InRun=true,Music=true,MenuMusic=true;public static bool Preference(ModuleId id)=>MenuMusic; }
    public static class Plugin { public static MusicExpansion Music=new MusicExpansion();public static List<string> Logs=new List<string>();public static void Log(string value){Logs.Add(value);}public static void Warn(string value){Logs.Add(value);} }
}
static class Scheduler
{
    private static List<IEnumerator> pending=new List<IEnumerator>();
    public static void Start(IEnumerator body){if(body.MoveNext())pending.Add(body);}
    public static void Complete(){foreach(var body in pending.ToArray()){while(body.MoveNext()){}pending.Remove(body);}}
}
static class Program
{
    private static int checks;
    private static object Field(string name)=>typeof(MusicExpansion).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Plugin.Music);
    private static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
    private static MusicTrack Track(string key,string title,string clip,float gain)
    {
        var t=new MusicTrack {Playlist=key,Title=title,File=title+".ogg",OriginalGains=new Dictionary<string,float>{{clip,gain}}};
        ((Dictionary<string,MusicTrack>)Field("tracks")).Add(key,t);return t;
    }
    static void Main()
    {
        typeof(MusicExpansion).GetField("directory",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Plugin.Music,System.IO.Path.Combine(System.IO.Path.GetTempPath(),"CluckmechMusic"));
        var source=new MasterAudio.Playlist {playlistName="beach",crossFadeTime=.7f};source.MusicSettings.Add(new MusicSetting{clip=new AudioClip{name="BGM_beach"},volume=.31622777f});MasterAudio.All.Add("beach",source);
        Track("beach","海风装配线","BGM_beach",1.095216f);((Dictionary<string,AudioClip>)Field("clips")).Add("海风装配线",new AudioClip{name="海风装配线"});
        var controller=new PlaylistController();UnityEngine.Random.Choice=1;controller.ChangePlaylist("beach",true);
        var pool=MasterAudio.GrabPlaylist(controller.LastPlaylist);
        Check(pool.MusicSettings.Count==2&&controller.isShuffle,"native and new song form random pool");
        Check(controller.LastClip.EndsWith(".new"),"entry can select new song with equal index probability");
        Check(Math.Abs(pool.MusicSettings[0].volume-.3463379f)<.00002,"calibration includes beach native per-song attenuation");
        Check(source.MusicSettings[0].volume==.31622777f&&source.MusicSettings.Count==1,"original playlist and song data remain intact");
        Check(pool.crossFadeTime==.7f&&!pool.MusicSettings[0].isLoop&&!pool.MusicSettings[1].isLoop,"pool preserves native transition and allows auto advance");
        var mineSource=new MasterAudio.Playlist{playlistName="mine"};mineSource.MusicSettings.Add(new MusicSetting{clip=new AudioClip{name="BGM_mine"}});MasterAudio.All.Add("mine",mineSource);
        var mine=Track("mine","岩窟快车","BGM_mine",.95f);mine.AdditionalSongs=new[]{new MusicSong{Title="矿脉引擎",File="矿脉引擎.ogg"}};
        ((Dictionary<string,AudioClip>)Field("clips")).Add("岩窟快车",new AudioClip{name="岩窟快车"});
        ((Dictionary<string,AudioClip>)Field("clips")).Add("矿脉引擎",new AudioClip{name="矿脉引擎"});
        for(int choice=0;choice<3;choice++)
        {
            UnityEngine.Random.Choice=choice;controller.ChangePlaylist("mine",true);
            var minePool=MasterAudio.GrabPlaylist(controller.LastPlaylist);
            Check(minePool.MusicSettings.Count==3&&minePool.MusicSettings[choice].alias==controller.LastClip,"mine draws each actual song with one of three equal indices");
        }
        Check(mineSource.MusicSettings.Count==1&&mineSource.MusicSettings[0].volume==1,"mine keeps original source calibration data unchanged");
        UnityEngine.Random.Choice=0;
        int lists=MasterAudio.All.Count;UnityEngine.Random.Choice=0;controller.ChangePlaylist("beach",true);
        Check(MasterAudio.All.Count==lists&&controller.LastClip.EndsWith(".original.0"),"repeat entry reuses pool and can select original song");
        controller.ChangePlaylist("layer-boss-intro",true,"intro");Check(!controller.isShuffle&&controller.LastPlaylist=="layer-boss-intro"&&controller.LastClip=="intro","layer boss retains ordered phase and explicit starting clip");
        ModuleSettings.Music=false;controller.ChangePlaylist("beach",true);Check(controller.LastPlaylist=="beach"&&!controller.isShuffle,"disabled music module uses original playback");ModuleSettings.Music=true;
        var desert=new MasterAudio.Playlist{playlistName="desert"};desert.MusicSettings.Add(new MusicSetting{clip=new AudioClip{name="BGM_desert"}});MasterAudio.All.Add("desert",desert);Track("desert","沙海齿轮","BGM_desert",.79f);
        controller.ChangePlaylist("desert",true);Check(controller.LastPlaylist=="beach"&&UnityEngine.Networking.UnityWebRequestMultimedia.Last.downloadHandler.streamAudio,"cold load streams audio while retaining current playback");
        controller.ChangePlaylist("layer-boss-climax",true,"climax");Scheduler.Complete();Check(controller.LastPlaylist=="layer-boss-climax","finished old load cannot replace subsequent boss phase");
        controller.ChangePlaylist("desert",true);Check(controller.LastPlaylist.EndsWith("desert")&&MasterAudio.GrabPlaylist(controller.LastPlaylist).MusicSettings.Count==2,"loaded clip is cached for next valid entry");
        var menu=new MasterAudio.Playlist{playlistName="menu"};menu.MusicSettings.Add(new MusicSetting{clip=new AudioClip{name="native-menu"},volume=.56f});MasterAudio.All.Add("menu",menu);
        var replacement=Track("menu","春日影","native-menu",1);replacement.Menu=true;replacement.Replace=true;
        ((Dictionary<string,AudioClip>)Field("clips")).Add("春日影",new AudioClip{name="春日影"});ModuleSettings.InRun=false;ModuleSettings.Music=false;
        controller.ChangePlaylist("menu",true);var menuPool=MasterAudio.GrabPlaylist(controller.LastPlaylist);
        Check(menuPool.MusicSettings.Count==1&&menuPool.MusicSettings[0].clip.name=="春日影","menu replacement works outside an active run");
        Check(menuPool.MusicSettings[0].isLoop&&!controller.isShuffle,"replacement loops without randomizing native song");
        Check(menu.MusicSettings.Count==1&&menu.MusicSettings[0].clip.name=="native-menu","replacement keeps original menu playlist");
        ModuleSettings.MenuMusic=false;controller.ChangePlaylist("menu",true);Check(controller.LastPlaylist=="menu","local music preference selects native menu track");ModuleSettings.MenuMusic=true;
        var cold=new MasterAudio.Playlist{playlistName="cold-menu"};cold.MusicSettings.Add(new MusicSetting{clip=new AudioClip{name="native-cold-menu"}});MasterAudio.All.Add("cold-menu",cold);
        var coldTrack=Track("cold-menu","menu-cold","native-cold-menu",1);coldTrack.Menu=coldTrack.Replace=true;
        controller.ChangePlaylist("cold-menu",true);controller.ChangePlaylist("layer-boss-intro",true,"intro");Scheduler.Complete();Check(controller.LastPlaylist=="layer-boss-intro","menu asynchronous load cannot interrupt subsequent scene music");
        controller.StartPlaylist("menu");var restarts=controller.NativeRestarts;controller.StartPlaylist("menu");
        Check(controller.LastPlaylist=="CluckmechCoop.Music.menu"&&controller.NativeRestarts==restarts+1,"same-menu StartPlaylist restarts replacement exactly once");
        var bootSource=new MasterAudio.Playlist{playlistName="boot-menu"};bootSource.MusicSettings.Add(new MusicSetting{clip=new AudioClip{name="native-boot"}});MasterAudio.All.Add("boot-menu",bootSource);
        var bootTrack=Track("boot-menu","boot-song","native-boot",1);bootTrack.Menu=bootTrack.Replace=true;
        var boot=new PlaylistController();boot.NativeStart("boot-menu");
        Check(boot.LastPlaylist=="boot-menu","native auto startup begins without ChangePlaylist");Scheduler.Complete();
        Check(boot.LastPlaylist=="CluckmechCoop.Music.boot-menu"&&MasterAudio.GrabPlaylist(boot.LastPlaylist).MusicSettings[0].clip.name=="boot-song","startup postfix replaces direct native initialization after audio load");
        var staleSource=new MasterAudio.Playlist{playlistName="stale-menu"};staleSource.MusicSettings.Add(new MusicSetting{clip=new AudioClip{name="stale-original"}});MasterAudio.All.Add("stale-menu",staleSource);
        var staleTrack=Track("stale-menu","stale-song","stale-original",1);staleTrack.Menu=staleTrack.Replace=true;
        controller.ChangePlaylist("layer-boss-intro",true,"intro");controller.StartPlaylist("stale-menu");controller.StartPlaylist("layer-boss-intro","intro");Scheduler.Complete();
        Check(controller.LastPlaylist=="layer-boss-intro","same-playlist restart invalidates previous asynchronous menu request");
        ModuleSettings.MenuMusic=false;controller.StartPlaylist("menu");Check(controller.LastPlaylist=="menu","disabled menu replacement keeps native StartPlaylist");
        ModuleSettings.InRun=ModuleSettings.Music=true;
        var bossEntries=Newtonsoft.Json.JsonConvert.DeserializeObject<MusicTrack[]>(System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"mix.json")));
        var bossNames=new[]{"背景音乐_Boss出场_普通Boss","背景音乐_Boss出场_普通Boss2"};
        foreach(var entry in bossEntries)
        {
            if(entry.Title!="铜管通缉令")continue;
            ((Dictionary<string,MusicTrack>)Field("tracks")).Add(entry.Playlist,entry);
            var native=new MasterAudio.Playlist{playlistName=entry.Playlist};
            native.MusicSettings.Add(new MusicSetting{clip=new AudioClip{name=entry.Playlist.Replace("背景音乐_","BGM_")}});
            MasterAudio.All.Add(entry.Playlist,native);
            Check(entry.OriginalPlaylists.Length==2&&entry.OriginalPlaylists[0]==bossNames[0]&&entry.OriginalPlaylists[1]==bossNames[1]&&entry.OriginalGains.Count==2,"installed boss manifest contains both native sources and gains");
        }
        UnityEngine.Random.Choice=2;controller.StartPlaylist(bossNames[1]);Scheduler.Complete();
        var bossPool=MasterAudio.GrabPlaylist(controller.LastPlaylist);
        Check(bossPool.MusicSettings.Count==3&&bossPool.MusicSettings[0].clip.name=="BGM_Boss出场_普通Boss"&&bossPool.MusicSettings[1].clip.name=="BGM_Boss出场_普通Boss2"&&bossPool.MusicSettings[2].clip.name=="铜管通缉令","cold second boss entry builds one three-song pool");
        Check(Math.Abs(bossPool.MusicSettings[0].volume-.434510224f)<.000001&&Math.Abs(bossPool.MusicSettings[1].volume-.461317574f)<.000001,"both original boss loudness calibrations survive merge");
        int sharedLists=MasterAudio.All.Count;
        for(int choice=0;choice<3;choice++)
        {
            UnityEngine.Random.Choice=choice;int beforeRestart=controller.NativeRestarts;
            controller.StartPlaylist(bossNames[choice%2]);
            Check(object.ReferenceEquals(bossPool,MasterAudio.GrabPlaylist(controller.LastPlaylist))&&controller.LastClip==bossPool.MusicSettings[choice].alias&&controller.NativeRestarts==beforeRestart+1,"either native boss entry can play each of three equally indexed songs while current pool is unchanged");
        }
        Check(MasterAudio.All.Count==sharedLists&&!bossPool.MusicSettings[0].isLoop&&!bossPool.MusicSettings[1].isLoop&&!bossPool.MusicSettings[2].isLoop,"shared boss pool caches three independent songs");
        Check(MasterAudio.GrabPlaylist(bossNames[0]).MusicSettings.Count==1&&MasterAudio.GrabPlaylist(bossNames[1]).MusicSettings.Count==1&&MasterAudio.GrabPlaylist(bossNames[1]).MusicSettings[0].volume==1,"native boss lists and gains remain intact");
        ModuleSettings.Music=false;controller.StartPlaylist(bossNames[1]);Check(controller.LastPlaylist==bossNames[1],"disabled extension uses original second boss list");
        string configDir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"CluckmechMusicConfig-"+Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(configDir);
        try
        {
            Plugin.Music=new MusicExpansion();
            typeof(MusicExpansion).GetField("directory",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Plugin.Music,configDir);
            System.IO.File.WriteAllText(System.IO.Path.Combine(configDir,"valid.ogg"),"");
            var valid=new MusicTrack{Playlist="beach",Title="valid",File="valid.ogg",OriginalGains=new Dictionary<string,float>{{"BGM_beach",1}}};
            string validJson=JsonConvert.SerializeObject(new[]{valid});
            foreach(string invalidJson in new[]{"{","null","[]","[null]","[1]","",null,"[{}]"})
            {
                Plugin.Music.ReadManifest(invalidJson);
                Check(((Dictionary<string,MusicTrack>)Field("tracks")).Count==0,"invalid or empty manifest falls back to original music");
            }
            Plugin.Music.ReadManifest("[null,{},"+JsonConvert.SerializeObject(valid)+"]");
            Check(((Dictionary<string,MusicTrack>)Field("tracks")).Count==1,"valid song survives adjacent invalid entries");
            Plugin.Music.ReadManifest(JsonConvert.SerializeObject(new[]{valid,valid}));
            Check(((Dictionary<string,MusicTrack>)Field("tracks")).Count==1,"duplicate playlist entries do not overwrite accepted configuration");
            foreach(var bad in new[]{
                new MusicTrack{Playlist="a",Title="x",File="../outside.ogg",Replace=true},
                new MusicTrack{Playlist="a",Title="x",File=System.IO.Path.Combine(configDir,"valid.ogg"),Replace=true},
                new MusicTrack{Playlist="a",Title="x",File="missing.ogg",Replace=true},
                new MusicTrack{Playlist="a",Title="x",File="valid.ogg",OriginalPlaylists=Array.Empty<string>(),Replace=true},
                new MusicTrack{Playlist="a",Title="x",File="valid.ogg",OriginalPlaylists=new string[]{null},Replace=true},
                new MusicTrack{Playlist="a",Title="x",File="valid.ogg",OriginalGains=null},
                new MusicTrack{Playlist="a",Title="x",File="valid.ogg",OriginalGains=new Dictionary<string,float>{{"song",float.NaN}}},
                new MusicTrack{Playlist="a",Title="x",File="valid.ogg",OriginalGains=new Dictionary<string,float>{{"song",-1}}}
            })
            {
                Plugin.Music.ReadManifest(JsonConvert.SerializeObject(new[]{bad}));
                Check(((Dictionary<string,MusicTrack>)Field("tracks")).Count==0,"invalid file, source list or gain skips song safely");
            }
            Check(Plugin.Logs.Count>0,"invalid configuration emits diagnostics");
            ModuleSettings.InRun=ModuleSettings.Music=true;
            controller.ChangePlaylist("beach",true);Check(controller.LastPlaylist=="beach","rejected configuration retains native playback");
            controller.ChangePlaylist(null,true);Check(controller.LastPlaylist==null,"null playlist name passes through without dictionary exception");
        }
        finally { System.IO.Directory.Delete(configDir,true); }
        Console.WriteLine($"PASS: {checks} production music pool, calibration, phase ordering and asynchronous playback checks");
    }
}
