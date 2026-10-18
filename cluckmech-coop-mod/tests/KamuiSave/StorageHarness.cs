using System;
using System.Collections.Generic;
using System.Reflection;
using CluckmechCoop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Storage, configuration and Unity entry points are doubled. ModuleSettings and
// the state/validation models are the production sources.
namespace 函数库 { public static class 常数类 { public const string 存档_存档加密密码="fixture"; } }
public class 存档脚本 { public string 文件名="danju",后缀=".cd"; }
public class ES3Settings { public ES3.EncryptionType encryptionType;public string encryptionPassword;public ES3Settings(ES3.Location location){} }
public static class ES3
{
    public enum Location { Cache } public enum EncryptionType { AES }
    internal static readonly Dictionary<string,object> Data=new();internal static int Writes;
    public static bool KeyExists(string key,string path,ES3Settings settings)=>Data.ContainsKey(path+key);
    public static T Load<T>(string key,string path,ES3Settings settings)=>(T)Data[path+key];
    public static void Save<T>(string key,T value,string path,ES3Settings settings){Writes++;Data[path+key]=value;}
}
namespace CluckmechCoop
{
    internal static class Plugin
    {
        internal static int Role;
        internal static readonly List<string> Warnings=new();
        internal static readonly FakeNetwork Network=new();
        internal static readonly FakeSummary Summary=new();
        internal static readonly FakeSpawner Spawner=new();
        internal static void Warn(string text)=>Warnings.Add(text);
        internal static void Log(string text){}
    }
    internal sealed class FakeNetwork
    {
        internal int Published,Sent;
        internal void PublishSettings()=>Published++;
        internal void SendProgress(PlayerProgress progress)=>Sent++;
    }
    internal sealed class FakeSummary
    {
        internal readonly FakeMap 地图存档=new();
        internal void 保存单局存档(){}
    }
    internal sealed class FakeMap { internal string 当前场景名="desert",当前地图页="page",当前节点名="node"; }
    internal sealed class FakeSpawner { internal readonly FakeScene 数据=new(); }
    internal sealed class FakeScene { internal bool 是否最终关卡; }
    internal static class RunSaveRepair
    {
        internal static bool Blocked;
        internal static bool IsRunSlot(存档脚本 save)=>save.文件名=="danju"||save.文件名=="lianji";
        internal static bool CanSave(存档脚本 save)=>true;
    }
    internal static class StratagemDeployment { internal static void Capture(){} }
    internal static class NativeRepairs { internal static bool Enabled; }
    internal static class ArtisanEconomy { internal static int Refund(int paid,int tier)=>0; }
    internal static class CarryCapacity { internal static int Applied;internal static void Apply()=>Applied++; }
    internal static class OtherworldProgress { internal static int Cleared;internal static void Clear()=>Cleared++; }
    internal static partial class KamuiWorld
    {
        internal static OtherworldSave Loaded;
        internal static OtherworldSave Save()=>Loaded??new OtherworldSave();
        internal static void Load(OtherworldSave value)=>Loaded=value;
        internal static void NewRun()=>Loaded=new OtherworldSave();
    }
}
namespace BepInEx.Configuration
{
    public sealed class ConfigEntry<T> { public T Value; }
    public sealed class ConfigFile
    {
        private readonly Dictionary<string,object> entries=new();
        public ConfigEntry<T> Bind<T>(string group,string name,T value,string description)
        {
            string key=group+"/"+name;
            if(!entries.TryGetValue(key,out var found))entries[key]=found=new ConfigEntry<T>{Value=value};
            return (ConfigEntry<T>)found;
        }
    }
}
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch:Attribute
    { public HarmonyPatch(Type type,string method){} }
}
static class StorageHarness
{
    private const string Characters="CluckmechCoop.Characters.v1";
    private static int checks;
    private static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
    internal static void Run()
    {
        foreach(string slot in new[]{"danju","lianji"})
        {
            var save=new 存档脚本{文件名=slot};string path=slot+"_m.cd";
            foreach(string broken in new[]{
                "{\"Players\":{},\"Rooms\":[{\"Owner\":1,\"Units\":[{\"Key\":\"x\",\"Id\":20002,\"Stratagem\":{\"Fields\":[null]}}]}]}",
                "{\"Players\":{},\"Rooms\":[{\"Owner\":1,\"Units\":[{\"Key\":\"x\",\"Id\":20002,\"Stratagem\":{\"Impacts\":[null]}}]}]}",
                "{\"Players\":{},\"Rooms\":[{\"Owner\":1,\"Units\":[{\"Key\":\"x\",\"Id\":20002,\"Buffs\":[null]}]}]}",
                "{\"Players\":{},\"Rooms\":[{\"Owner\":1,\"Units\":[{\"Key\":\"x\",\"Id\":20002,\"Underlying\":[null]}]}]}",
                "{\"Players\":{},\"Rooms\":[{\"Owner\":1,\"Units\":[{\"Key\":\"x\",\"Id\":20002,\"States\":[null]}]}]}"
            })
            {
                ES3.Data.Clear();ES3.Writes=0;Plugin.Warnings.Clear();ModuleSettings.InRun=false;Plugin.Role=slot=="danju"?0:1;
                ES3.Data[path+"native"]="unchanged native save";ES3.Data[path+Characters]=broken;
                ModuleSettings.Load(save);
                Check(ModuleSettings.InRun&&KamuiWorld.Loaded.Rooms.Count==0,"invalid nested addon returns defaults and continuation resumes");
                Check(Plugin.Warnings.Count==1&&Plugin.Warnings[0].Contains(Characters),"fallback identifies characters addon");
                Check(ES3.Writes==0&&Equals(ES3.Data[path+Characters],broken),"failed read keeps exact original addon bytes");
                ModuleSettings.Save(save);
                Check(ES3.Writes==0&&Equals(ES3.Data[path+Characters],broken)&&Equals(ES3.Data[path+"native"],"unchanged native save"),"autosave preserves failed addon and native snapshot");
                ModuleSettings.MainMenu();ModuleSettings.EnterRun();ModuleSettings.Save(save);
                Check(ES3.Writes==0&&Equals(ES3.Data[path+Characters],broken),"menu/new run does not clear protected slot");
            }
            ES3.Data[path+Characters]="{\"Players\":{},\"Rooms\":[]}";ModuleSettings.Load(save);ModuleSettings.Save(save);
            Check(ES3.Writes==3,"successful repaired reread re-enables all three addon writes");
        }
        Console.WriteLine($"PASS: {checks} addon fallback/storage checks using complete production module persistence and real character validation");
    }
}
