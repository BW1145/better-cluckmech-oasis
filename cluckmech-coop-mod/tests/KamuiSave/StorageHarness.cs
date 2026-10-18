using System;
using System.Collections.Generic;
using System.Reflection;
using CluckmechCoop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Only storage and unrelated module dependencies are doubled. The build extracts
// the current production ReadAddon/Load/Save methods instead of copying their logic.
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
    internal static class Plugin { internal static int Role;internal static readonly List<string> Warnings=new();internal static void Warn(string text)=>Warnings.Add(text); }
    internal static class RunSaveRepair { internal static bool Blocked;internal static bool IsRunSlot(存档脚本 save)=>save.文件名=="danju"||save.文件名=="lianji"; }
    internal static class StratagemDeployment { internal static void Capture(){} }
    internal sealed class RunProgress { public int Marker; }
    internal static partial class ModuleSettings
    {
        private const string SaveKey="CluckmechCoop.Expedition.v3",ProgressKey="CluckmechCoop.Progress.v4",CharactersKey="CluckmechCoop.Characters.v1";
        private static readonly HashSet<string> protectedSaves=new(StringComparer.Ordinal);
        internal static bool InRun;internal static RunProgress Progress;internal static ExpeditionChoice Choice;
        internal static void EnterRun()=>InRun=true;
        private static bool ValidProgress(JObject data,RunProgress progress)=>true;
        private static bool ValidChoice(JObject data,ExpeditionChoice choice)=>true;
    }
    internal static partial class KamuiWorld
    {
        internal static OtherworldSave Loaded;
        internal static OtherworldSave Save()=>Loaded??new OtherworldSave();
        internal static void Load(OtherworldSave value)=>Loaded=value;
    }
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
                ModuleSettings.InRun=false;ModuleSettings.EnterRun();ModuleSettings.Save(save);
                Check(ES3.Writes==0&&Equals(ES3.Data[path+Characters],broken),"menu/new run does not clear protected slot");
            }
            ES3.Data[path+Characters]="{\"Players\":{},\"Rooms\":[]}";ModuleSettings.Load(save);ModuleSettings.Save(save);
            Check(ES3.Writes==3,"successful repaired reread re-enables all three addon writes");
        }
        Console.WriteLine($"PASS: {checks} addon fallback/storage checks using extracted production ReadAddon/Load/Save and real character validation");
    }
}
