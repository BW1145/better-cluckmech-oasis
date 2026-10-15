using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CluckmechCoop;
using Newtonsoft.Json;

namespace UnityEngine
{
    public sealed class Coroutine { public IEnumerator Body; }
    public sealed class WaitForSecondsRealtime { public WaitForSecondsRealtime(float seconds) { } }
    public static class Random { public static int Draws; public static int Range(int min, int max) { Draws++; return (Draws - 1) % (max - min) + min; } }
    public class Object
    {
        public static T FindObjectOfType<T>() => Harness.Current.Objects.TryGetValue(typeof(T), out var value) ? (T)value : default;
        public static T[] FindObjectsOfType<T>() => Array.Empty<T>();
        public static GameObject Instantiate(GameObject prefab, Transform parent)
        {
            var copy=new GameObject();copy.transform.parent=parent;copy.AddComponent(new ParticleSystem());copy.AddComponent(new 音效_播放音效());return copy;
        }
        public static void Destroy(GameObject value){value.Destroyed=true;}
    }
    public sealed class GameObject
    {
        public bool Destroyed; public Transform transform=new Transform();
        private readonly Dictionary<Type,object> components=new Dictionary<Type,object>();
        public void AddComponent<T>(T value){components[typeof(T)]=value;}
        public T GetComponent<T>() => components.TryGetValue(typeof(T),out var value)?(T)value:default;
        public T[] GetComponentsInChildren<T>() => components.TryGetValue(typeof(T),out var value)?new[]{(T)value}:Array.Empty<T>();
    }
    public static class Mathf { public static int Max(int a, int b) => Math.Max(a,b); }
}
namespace UnityEngine.UI { public sealed class Toggle { public void SetIsOnWithoutNotify(bool value) { } } }
namespace UnityEngine
{
}
namespace BepInEx.Configuration
{
    public sealed class ConfigEntry<T>
    {
        public string Key;
        public T Value { get => (T)Harness.Current.Preferences[Key]; set => Harness.Current.Preferences[Key] = value; }
    }
    public sealed class ConfigFile
    {
        public ConfigEntry<T> Bind<T>(string group, string key, T value, string description) { if (!Harness.Current.Preferences.ContainsKey(key)) Harness.Current.Preferences[key] = value; return new ConfigEntry<T> { Key = key }; }
    }
}
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string name) { } public HarmonyPatch() { } }
    public static class AccessTools
    {
        public static MethodInfo Method(Type type, string name, Type[] parameters) => type.GetMethod(name, parameters);
        public static IEnumerable<MethodInfo> GetDeclaredMethods(Type type) => type.GetMethods();
    }
}
namespace 函数库 { public static class 常数类 { public const string 存档_存档加密密码 = "fixture"; } }
public sealed class ES3Settings
{
    public ES3.EncryptionType encryptionType;
    public string encryptionPassword;
    public ES3Settings(ES3.Location location) { }
}
public static class ES3
{
    public enum Location { Cache }
    public enum EncryptionType { AES }
    public static readonly Dictionary<string, object> Data = new Dictionary<string, object>();
    public static Func<string, string, Exception> ReadFailure;
    public static int Writes;
    public static void Save<T>(string key, T value, string path, ES3Settings settings) { Writes++; Data[path + key] = value; }
    public static T Load<T>(string key, string path, ES3Settings settings) { Fail("load", key); return (T)Data[path + key]; }
    public static bool KeyExists(string key, string path, ES3Settings settings) { Fail("exists", key); return Data.ContainsKey(path + key); }
    private static void Fail(string operation, string key) { var error = ReadFailure?.Invoke(operation, key); if (error != null) throw error; }
}
public sealed class 存档脚本 { public string 文件名 = "fixture", 后缀 = ".cd"; }
public sealed class 刷怪器 { public 场景数据 数据 = new 场景数据(); public bool TryGetComponent<T>(out T value) { value = default; return false; } public T GetComponent<T>() => typeof(T)==typeof(效果物体引用器)?(T)(object)new 效果物体引用器():default; }
public sealed class 场景数据 { public bool 是否最终关卡; }
public sealed class 总结脚本
{
    public sealed class Map { public bool 是否开启挑战; public string 当前地图页 = "page", 当前节点名 = "node", 当前场景名 = "沙漠"; }
    public Map 地图存档 = new Map();
    public sealed class 背包单元 { public int 炮台id, 强化上限; public List<int> 强化词条 = new List<int>(); }
    public List<背包单元> 当前拥有炮台 = new List<背包单元>(), 当前携带炮台 = new List<背包单元>();
    public int 当前金钱数;
    public int 目标场景id = 10;
    public void 保存单局存档() { Harness.Current.Saves++; ModuleSettings.Save(Harness.Current.Save); }
}
public sealed class 关卡奖励模版 { public int id, 金币奖励, 动力奖励, 鸡械奖励; }
public sealed class 数据库_关卡奖励
{
    private readonly 关卡奖励模版[] values = { new 关卡奖励模版 { id = 0, 金币奖励 = 50 }, new 关卡奖励模版 { id = 1, 动力奖励 = 1 }, new 关卡奖励模版 { id = 2, 鸡械奖励 = 1 } };
    public 关卡奖励模版 查询关卡奖励(int id) => values[id];
}
public sealed class 奖励列表脚本
{
    public int 金币奖励, 动力炉奖励, 炮台奖励, Calls;
    public void 添加奖励数据(关卡奖励模版 value) { Calls++; 金币奖励 += value.金币奖励; 动力炉奖励 += value.动力奖励; 炮台奖励 += value.鸡械奖励; }
}
public sealed class 场景切换按钮 { public void 切换场景(int value) { } }
public sealed class 战斗背包 { public 背包类 背包类 = new 背包类(); }
public sealed class 背包类
{
    public List<int> 当前已选单元 = new List<int>();
    public List<UnityEngine.GameObject> 单元列表 = new List<UnityEngine.GameObject>();
    public void 更新背包(int count) { }
}
public sealed class 玩家背包脚本 { public void 更新背包() { } }
public sealed class 联机_2P存档 { public 联机玩家控件.联机总结数据包 联机存档_2P; }
public sealed class 联机玩家控件
{
    public sealed class 联机总结数据包 { public int 当前金钱数; public List<总结脚本.背包单元> 当前拥有炮台 = new List<总结脚本.背包单元>(); }
    public 联机总结数据包 Network总结数据_联机 = new 联机总结数据包();
    public void 上传玩家总结数据()
    {
        var snapshot=new 联机总结数据包 { 当前金钱数=Harness.Current.Summary.当前金钱数, 当前拥有炮台=Harness.QueueTransport ? Harness.Current.Summary.当前拥有炮台.ConvertAll(x=>new 总结脚本.背包单元{炮台id=x.炮台id,强化上限=x.强化上限,强化词条=new List<int>(x.强化词条)}) : Harness.Current.Summary.当前拥有炮台 };
        Harness.Current.Uploads++;
        Harness.Deliver(()=>{Network总结数据_联机=snapshot;});
    }
    public bool isLocalPlayer = true;
    public int Role;
    public void 发送消息广播_cmd(string key, string value)
    {
        Harness.Messages.Add((key, value));
        Harness.Deliver(()=>{ using (Harness.Use(Harness.Host))
        {
            if (key == NetworkService.RequestKey) Plugin.Network.ServerReceive(this, value);
            else if (key == TransferService.RequestKey) Plugin.Transfers.ServerReceive(this, value);
            else throw new Exception("Wrong request channel");
        }});
    }
    public void 发送消息广播_Rpc(string key, string value)
    {
        Harness.Messages.Add((key, value));
        if (key == NetworkService.ReplyKey) { Harness.Deliver(()=>{ using (Harness.Use(Harness.Guest)) Plugin.Network.Receive(key, value); }); }
        else
        {
            if (!Harness.DeliverTransfers) return;
            Harness.Deliver(()=>{ using (Harness.Use(Harness.Guest)) Plugin.Transfers.Receive(key, value); });
            Harness.Deliver(()=>{ using (Harness.Use(Harness.Host)) Plugin.Transfers.Receive(key, value); });
        }
    }
}
public static class Steam联机管理器
{
    public static 联机玩家控件 玩家_主机 = new 联机玩家控件 { Role = 1 }, 玩家_客户端 = new 联机玩家控件 { Role = 2 };
}
namespace CluckmechCoop
{
    internal sealed class FakeInstance
    {
        internal readonly List<UnityEngine.Coroutine> Tasks = new List<UnityEngine.Coroutine>();
        internal UnityEngine.Coroutine StartCoroutine(IEnumerator body) { var task = new UnityEngine.Coroutine { Body = body }; Tasks.Add(task); return task; }
        internal void StopCoroutine(UnityEngine.Coroutine task) => Tasks.Remove(task);
    }
    internal sealed class CoopPanel
    {
        internal void RefreshState() { }
        internal void RefreshInventory() { Harness.Current.InventoryRefreshes++; }
        internal void RefreshWallet() { }
        internal void Open() { }
    }
    internal static class PreparationPanel { internal static void RefreshAll() { } internal static void ShowStatus(string text) { } }
    internal static class Difficulty
    {
        internal static void ClientFinished() { ModuleSettings.CompleteReward(); Harness.Current.Completed++; }
        internal static void Playback(int stage, float delay) { Harness.Current.PlaybackStage = stage; Harness.Current.PlaybackDelay = delay; Harness.Current.PlaybackMessages++; }
    }
    internal static class Plugin
    {
        internal const string Version = "0.4.8";
        internal static CombatFeatures Combat => Harness.Current.Combat;
        internal static bool InCombat => Harness.Current.CombatScene;
        internal static int Role => Harness.Current.Role;
        internal static NetworkService Network => Harness.Current.Network;
        internal static 联机玩家控件 LocalPlayer => Role == 1 ? Steam联机管理器.玩家_主机 : Steam联机管理器.玩家_客户端;
        internal static 总结脚本 Summary => Harness.Current.Summary;
        internal static 刷怪器 Spawner => Harness.Current.Spawner;
        internal static FakeInstance Instance => Harness.Current.Instance;
        internal static TransferService Transfers => Harness.Current.Transfers;
        internal static bool CanTrade => ModuleSettings.Transfer;
        internal static CoopPanel Panel = new CoopPanel();
        internal static void Log(string text) { }
        internal static readonly List<string> Warnings = new List<string>();
        internal static void Warn(string text) => Warnings.Add(text);
    }
}
sealed class Context
{
    internal int Role, Completed, InventoryRefreshes, Uploads, Saves;
    internal int PlaybackStage, PlaybackMessages;
    internal float PlaybackDelay;
    internal TransferService Transfers = new TransferService();
    internal NetworkService Network = new NetworkService();
    internal FakeInstance Instance = new FakeInstance();
    internal 总结脚本 Summary = new 总结脚本();
    internal 刷怪器 Spawner = new 刷怪器();
    internal 存档脚本 Save = new 存档脚本();
    internal readonly Dictionary<Type, object> Objects = new Dictionary<Type, object>();
    internal readonly Dictionary<string, object> Preferences = new Dictionary<string, object> { ["Expedition"] = true, ["Transfer"] = true };
    internal bool InRun, Expedition, Transfer;
    internal bool Music, CombatScene = true;
    internal float TimeScale = 1;
    internal CombatFeatures Combat = new CombatFeatures();
    internal GameplayModules Extra = new GameplayModules();
    internal RunProgress Progress = new RunProgress();
    internal ExpeditionChoice Choice;
    internal object Rewards, RewardList;
    internal Context(int role)
    {
        Role = role; Save.文件名 = role == 0 ? "danju" : "lianji";
        foreach (var info in ModuleCatalog.All) if (!Preferences.ContainsKey(info.Id.ToString())) Preferences[info.Id.ToString()] = true;
        Objects[typeof(数据库_关卡奖励)] = new 数据库_关卡奖励();
        Objects[typeof(奖励列表脚本)] = new 奖励列表脚本();
    }
    internal 奖励列表脚本 Reward => (奖励列表脚本)Objects[typeof(奖励列表脚本)];
}
static class Harness
{
    internal static bool DeliverTransfers = true;
    internal static bool QueueTransport;
    internal static readonly Queue<Action> Transport = new Queue<Action>();
    internal static void Deliver(Action action) { if (QueueTransport) Transport.Enqueue(action); else action(); }
    internal static void Drain() { int count=0; while(Transport.Count>0) { if(++count>100) throw new Exception("Transport did not settle"); Transport.Dequeue()(); } }
    internal static readonly List<(string Key, string Json)> Messages = new List<(string,string)>();
    internal static Context Host = new Context(1), Guest = new Context(2), Current = Host;
    private static readonly FieldInfo rewards = typeof(ModuleSettings).GetField("rewards", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo rewardList = typeof(ModuleSettings).GetField("rewardList", BindingFlags.Static | BindingFlags.NonPublic);
    private sealed class Scope : IDisposable
    {
        private readonly Context old;
        internal Scope(Context context) { old = Current; Switch(context); }
        public void Dispose() => Switch(old);
    }
    internal static IDisposable Use(Context context) => new Scope(context);
    private static void Switch(Context target)
    {
        Current.InRun = ModuleSettings.InRun; Current.Expedition = ModuleSettings.Expedition; Current.Transfer = ModuleSettings.Transfer; Current.Choice = ModuleSettings.Choice;
        Current.Extra = ModuleSettings.Extra; Current.Progress = ModuleSettings.Progress; Current.Music = ModuleSettings.Music;
        Current.Rewards = rewards.GetValue(null); Current.RewardList = rewardList.GetValue(null);
        Current = target;
        ModuleSettings.InRun = target.InRun; ModuleSettings.Expedition = target.Expedition; ModuleSettings.Transfer = target.Transfer; ModuleSettings.Choice = target.Choice;
        ModuleSettings.Extra = target.Extra; ModuleSettings.Progress = target.Progress; ModuleSettings.Music = target.Music;
        rewards.SetValue(null, target.Rewards); rewardList.SetValue(null, target.RewardList);
    }
}
static class Program
{
    private static int checks;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    static void Main(string[] args)
    {
        ModuleSettings.Initialize(new BepInEx.Configuration.ConfigFile());
        if(args.Length==1 && args[0]=="--growth-055")
        { GrowthEventChecks.Update055(Check);Console.WriteLine($"PASS: {checks} updated event, shared energy, toggle and persistence checks");return; }
        if(args.Length==1 && args[0]=="--growth-events")
        {
            GrowthEventChecks.Run(Check);Console.WriteLine($"PASS: {checks} growth-event reward, persistence and six-slot protocol checks");return;
        }
        ModuleSettings.MainMenu();
        var global = new 存档脚本 { 文件名 = "quanju", 后缀 = ".qjcd" };
        ModuleSettings.Load(global);
        Check(!ModuleSettings.InRun, "global save load cannot start or freeze run modules");
        ModuleSettings.EnterRun();
        int stored = ES3.Data.Count;
        ModuleSettings.Save(global);
        Check(ES3.Data.Count == stored, "run addon fields never enter global progression file");
        ModuleSettings.MainMenu(); RunSaveRepair.Blocked = true;
        ModuleSettings.Load(Harness.Current.Save);
        Check(!ModuleSettings.InRun, "rejected native continuation cannot load addon run state");
        RunSaveRepair.Blocked = false;
        for (int mask = 0; mask < 4; mask++)
        {
            ModuleSettings.MainMenu();
            ModuleSettings.ExpeditionPreference = (mask & 1) != 0; ModuleSettings.TransferPreference = (mask & 2) != 0;
            ModuleSettings.EnterRun();
            Check(ModuleSettings.Expedition == ((mask & 1) != 0) && ModuleSettings.Transfer == ((mask & 2) != 0), "four module combinations");
            ModuleSettings.ExpeditionPreference = !ModuleSettings.Expedition; ModuleSettings.TransferPreference = !ModuleSettings.Transfer;
            ModuleSettings.EnterRun();
            Check(ModuleSettings.Expedition == ((mask & 1) != 0) && ModuleSettings.Transfer == ((mask & 2) != 0), "run settings remain frozen");
        }
        for (int mask = 0; mask < 4; mask++)
        {
            using (Harness.Use(new Context(0)))
            {
                ModuleSettings.MainMenu(); ModuleSettings.EnterRun(); ModuleSettings.Prepare();
                bool vanilla = (mask & 1) != 0, expedition = (mask & 2) != 0;
                Plugin.Summary.地图存档.是否开启挑战 = vanilla;
                ModuleSettings.Select(expedition);
                var reward = Harness.Current.Reward;
                var database = (数据库_关卡奖励)Harness.Current.Objects[typeof(数据库_关卡奖励)];
                if (vanilla) reward.添加奖励数据(database.查询关卡奖励(0));
                int beforeCalls = reward.Calls;
                ModuleSettings.Freeze(); ModuleSettings.BindRewards(); ModuleSettings.CompleteReward(); ModuleSettings.CompleteReward();
                Check(Plugin.Summary.地图存档.是否开启挑战 == vanilla && ModuleSettings.Active == expedition, "four level choices preserve independent vanilla state");
                var bonus = database.查询关卡奖励(ModuleSettings.Choice.RewardId);
                Check(reward.Calls == beforeCalls + (expedition ? 1 : 0) && reward.金币奖励 == (vanilla ? 50 : 0) + (expedition ? bonus.金币奖励 : 0) && reward.动力炉奖励 == (expedition ? bonus.动力奖励 : 0) && reward.炮台奖励 == (expedition ? bonus.鸡械奖励 : 0), "native challenge reward and expedition bonus accumulate once independently");
            }
        }
        using (Harness.Use(new Context(0)))
        {
            ModuleSettings.MainMenu(); ModuleSettings.EnterRun();
            Plugin.Summary.目标场景id = 4;
            ModuleSettings.Prepare();
            string preparedToken = ModuleSettings.Choice.Token;
            int preparedReward = ModuleSettings.Choice.RewardId, preparedDraws = UnityEngine.Random.Draws;
            Plugin.Summary.目标场景id = 139;
            ModuleSettings.Select(true); ModuleSettings.Freeze();
            Check(ModuleSettings.Active && ModuleSettings.Choice.Token == preparedToken && ModuleSettings.Choice.Locked, "native preparation-before-target-assignment preserves checked expedition through start");
            Check(ModuleSettings.Choice.RewardId == preparedReward && UnityEngine.Random.Draws == preparedDraws, "native target assignment does not redraw reward");
            ModuleSettings.Choice.Node += ":139";
            ModuleSettings.Prepare();
            Check(ModuleSettings.Choice.Token == preparedToken && ModuleSettings.Active && !ModuleSettings.Choice.Node.EndsWith(":139"), "existing saved node choice retains selection and reward token");
            Plugin.Summary.目标场景id = 4; ModuleSettings.Prepare();
            Check(ModuleSettings.Active && ModuleSettings.Choice.Token == preparedToken, "scene routing does not recreate same node choice");
        }
        ModuleSettings.MainMenu(); ModuleSettings.ExpeditionPreference = true; ModuleSettings.TransferPreference = false;
        using (Harness.Use(Harness.Guest)) { ModuleSettings.MainMenu(); ModuleSettings.ExpeditionPreference = false; ModuleSettings.TransferPreference = true; }
        ModuleSettings.EnterRun(); ModuleSettings.Prepare();
        Check(Plugin.Network.SettingsReady, "host receives guest acknowledgement");
        using (Harness.Use(Harness.Guest))
        {
            Check(ModuleSettings.Expedition && !ModuleSettings.Transfer, "guest follows host module switches");
            Check(!ModuleSettings.ExpeditionPreference && ModuleSettings.TransferPreference, "guest local preferences preserved");
            Check(Plugin.Network.SettingsReady, "guest acknowledges current revision");
        }
        int draws = UnityEngine.Random.Draws, id = ModuleSettings.Choice.RewardId;
        string token = ModuleSettings.Choice.Token;
        Check(!ModuleSettings.Choice.Enabled && id >= 0 && id < 3, "new node defaults off with one native reward");
        ModuleSettings.Select(true); ModuleSettings.Select(false); ModuleSettings.Prepare(); ModuleSettings.Select(true);
        Check(UnityEngine.Random.Draws == draws && ModuleSettings.Choice.RewardId == id, "toggle and reopen cannot reroll reward");
        ModuleSettings.Freeze(); ModuleSettings.Select(false);
        Check(ModuleSettings.Choice.Enabled && ModuleSettings.Choice.Locked, "level choice frozen at start");
        Plugin.Network.Playback(token, 0, 0);
        Check(Harness.Guest.PlaybackMessages == 1 && Harness.Guest.PlaybackStage == 0 && Harness.Guest.PlaybackDelay == 0, "guest begins additional combat without a preparation wait");
        Plugin.Network.Playback(token, 1, 8);
        Check(Harness.Guest.PlaybackMessages == 2 && Harness.Guest.PlaybackStage == 1 && Harness.Guest.PlaybackDelay == 8, "host countdown interval synchronizes to guest");
        Plugin.Network.Playback("previous-node", 2, 30);
        Check(Harness.Guest.PlaybackMessages == 2, "previous-node countdown is ignored");
        Plugin.Network.Playback(token, 1, 0);
        Check(Harness.Guest.PlaybackMessages == 3 && Harness.Guest.PlaybackDelay == 0, "host spawn start hides guest countdown");
        ModuleSettings.BindRewards(); ModuleSettings.CompleteReward(); ModuleSettings.CompleteReward();
        Check(Harness.Host.Reward.Calls == 1, "host gets one bonus with transfer module disabled");
        Plugin.Network.PublishSettings();
        using (Harness.Use(Harness.Guest))
        {
            Check(!ModuleSettings.Choice.RewardApplied, "host reward completion cannot mark guest reward as claimed");
            ModuleSettings.Freeze(); ModuleSettings.BindRewards();
        }
        Plugin.Network.Complete(token); Plugin.Network.Complete(token);
        using (Harness.Use(Harness.Guest)) Check(Harness.Guest.Reward.Calls == 1, "duplicate completion gives guest exactly one reward");
        Plugin.Network.PublishSettings(); Plugin.Network.Complete(token);
        using (Harness.Use(Harness.Guest)) Check(Harness.Guest.Reward.Calls == 1, "resynchronization preserves local reward deduplication");
        ModuleSettings.Save(Harness.Host.Save);
        ModuleSettings.MainMenu(); ModuleSettings.ExpeditionPreference = true; ModuleSettings.TransferPreference = true;
        ModuleSettings.Load(Harness.Host.Save);
        Check(ModuleSettings.Expedition && ModuleSettings.Transfer, "continue reads current main menu module settings");
        Check(ModuleSettings.Choice.Token == token && ModuleSettings.Choice.RewardId == id && ModuleSettings.Choice.RewardApplied, "continue restores selection reward and completion");
        Harness.Host.Summary.地图存档.当前节点名 = "next"; ModuleSettings.Prepare();
        Check(!ModuleSettings.Choice.Enabled && ModuleSettings.Choice.Token != token && UnityEngine.Random.Draws == draws + 1, "next node starts a new fixed choice");
        Harness.Host.Summary.地图存档.当前节点名 = "oasis"; Harness.Host.Summary.地图存档.当前场景名 = "绿洲"; ModuleSettings.Prepare(); ModuleSettings.Select(true);
        int before = Harness.Host.Reward.Calls; ModuleSettings.BindRewards(); ModuleSettings.CompleteReward();
        Check(ModuleSettings.Choice.RewardId == -1 && Harness.Host.Reward.Calls == before, "oasis goes directly to victory without ordinary bonus");
        var stale = new SessionMessage { Type = "complete", Token = token };
        using (Harness.Use(Harness.Guest)) Plugin.Network.Receive(NetworkService.ReplyKey, JsonConvert.SerializeObject(stale));
        Check(Harness.Guest.Reward.Calls == 1, "completion from a previous node ignored");
        Harness.Host.Summary.地图存档.当前场景名 = "沙漠";
        ModuleSettings.Expedition = ModuleSettings.Transfer = true;
        Plugin.Network.PublishSettings();
        Harness.Host.Summary.当前金钱数 = 600; Harness.Guest.Summary.当前金钱数 = 90;
        Harness.Host.Summary.当前拥有炮台.Add(new 总结脚本.背包单元 { 炮台id = 12, 强化上限 = 7, 强化词条 = new List<int> { 4, 9, 9 } });
        Harness.Host.Summary.当前携带炮台.Add(Harness.Host.Summary.当前拥有炮台[0]);
        Steam联机管理器.玩家_主机.Network总结数据_联机.当前拥有炮台 = Harness.Host.Summary.当前拥有炮台;
        int messagesBefore = Harness.Messages.Count;
        Plugin.Transfers.Request("gold", 250, -1);
        Check(Harness.Host.Summary.当前金钱数 == 350 && Harness.Guest.Summary.当前金钱数 == 340, "production gold transfer commits both balances");
        Check(Harness.Host.InventoryRefreshes == 0 && Harness.Guest.InventoryRefreshes == 0 && Harness.Host.Uploads == 1 && Harness.Guest.Uploads == 1, "gold transfer refreshes native server balances once without rebuilding mechanical UI");
        Check(ReferenceEquals(Steam联机管理器.玩家_主机.Network总结数据_联机.当前拥有炮台, Harness.Host.Summary.当前拥有炮台), "gold native summary retains mechanical list reference");
        string commit = null;
        foreach (var msg in Harness.Messages.GetRange(messagesBefore, Harness.Messages.Count - messagesBefore))
        {
            var payload = Newtonsoft.Json.Linq.JObject.Parse(msg.Json);
            Check(payload["Wallet"] == null && payload["Result"] == null && !msg.Json.Contains("Machines"), "gold message contains no machine wallet");
            if ((string)payload["Type"] == "commit") commit = msg.Json;
        }
        using (Harness.Use(Harness.Guest)) Plugin.Transfers.Receive(TransferService.ReplyKey, commit);
        Check(Harness.Guest.Summary.当前金钱数 == 340, "duplicate gold commit ignored");
        using (Harness.Use(Harness.Guest)) Plugin.Transfers.Request("gold", 100, -1);
        Check(Harness.Host.Summary.当前金钱数 == 450 && Harness.Guest.Summary.当前金钱数 == 240, "production guest gold transfer commits both balances");
        Plugin.Transfers.Request("machine", 0, 0, TransferService.Snapshot(Harness.Host.Summary.当前拥有炮台[0]));
        Check(Harness.Host.Summary.当前拥有炮台.Count == 0 && Harness.Guest.Summary.当前拥有炮台.Count == 1, "production mechanical transfer conserves inventory");
        var transferred = Harness.Guest.Summary.当前拥有炮台[0];
        Check(transferred.强化上限 == 7 && transferred.强化词条.Count == 3 && transferred.强化词条[2] == 9, "production transfer preserves every upgrade");
        Check(Harness.Host.Summary.当前携带炮台[0].炮台id == 0, "removed carried mechanical selection cleared");
        Check(Harness.Host.InventoryRefreshes == 1 && Harness.Guest.InventoryRefreshes == 1, "mechanical transfer refreshes only mechanical changes");
        var first = new 总结脚本.背包单元 { 炮台id = 20, 强化上限 = 4, 强化词条 = new List<int> { 2 } };
        var chosen = new 总结脚本.背包单元 { 炮台id = 20, 强化上限 = 4, 强化词条 = new List<int> { 3, 3 } };
        Harness.Host.Summary.当前拥有炮台.Add(first);
        Harness.Host.Summary.当前拥有炮台.Add(chosen);
        var visible = TransferService.Snapshot(chosen);
        messagesBefore = Harness.Messages.Count;
        Plugin.Transfers.Request("machine", 0, 0, visible);
        Check(Harness.Messages.Count == messagesBefore && !Plugin.Transfers.Locked, "displayed machine mismatch rejected before network traffic");
        Check(Harness.Host.Summary.当前拥有炮台.Count == 2 && Harness.Guest.Summary.当前拥有炮台.Count == 1, "mismatch changes neither inventory");
        Check(Plugin.Transfers.Status.Contains("背包已变化"), "mismatch reports inventory change");
        Plugin.Transfers.Request("machine", 0, 1);
        Check(Harness.Messages.Count == messagesBefore && !Plugin.Transfers.Locked, "machine request requires displayed snapshot");
        chosen.强化词条[0] = 8;
        Plugin.Transfers.Request("machine", 0, 1, visible);
        Check(Harness.Messages.Count == messagesBefore, "same id and upgrade count with changed affix rejected");
        chosen.强化词条[0] = 3;
        Harness.Host.Summary.当前拥有炮台.Reverse();
        Plugin.Transfers.Request("machine", 0, Harness.Host.Summary.当前拥有炮台.IndexOf(chosen), visible);
        Check(Harness.Host.Summary.当前拥有炮台.Count == 1 && Harness.Host.Summary.当前拥有炮台[0].强化词条[0] == 2, "reordered inventory transfers selected upgraded copy only");
        var last = Harness.Guest.Summary.当前拥有炮台[1];
        Check(last.炮台id == 20 && last.强化词条.Count == 2 && last.强化词条[0] == 3 && last.强化词条[1] == 3, "same-name machines remain distinguished by full affix data");
        Check(visible.Upgrades[0] == 3, "request preserves displayed snapshot");
        Plugin.Network.Disconnect();
        Check(!Plugin.Network.PeerReady && !Plugin.Network.SettingsReady, "disconnect clears peer readiness");
        using (Harness.Use(Harness.Guest)) Plugin.Network.Disconnect();
        Plugin.Network.PublishSettings();
        Check(Plugin.Network.SettingsReady, "new session revision accepted after disconnect");
        ProtocolHarness.Run(Check);
        ExpansionHarness.Run(Check);
        ModuleSaveHarness.Run(Check);
        StratagemHarness.Run(Check);
        Console.WriteLine($"PASS: {checks} production module, storage and two-peer protocol checks using code-level transport/storage doubles");
    }
}
