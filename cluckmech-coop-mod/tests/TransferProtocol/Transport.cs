using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CluckmechCoop;
using Newtonsoft.Json;

namespace UnityEngine {
 public sealed class Coroutine {public IEnumerator Body;}
 public sealed class WaitForSecondsRealtime {public WaitForSecondsRealtime(float seconds){}}
 public static class Object {public static T FindObjectOfType<T>()=>default;public static T[] FindObjectsOfType<T>()=>Array.Empty<T>();}
 public sealed class GameObject {public T GetComponent<T>()=>default;}
}
namespace UnityEngine.UI {public sealed class Toggle {public void SetIsOnWithoutNotify(bool value){}}}
namespace HarmonyLib {
 [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch:Attribute {public HarmonyPatch(){} public HarmonyPatch(Type type,string name){}}
 public static class AccessTools {
  public static MethodInfo Method(Type type,string name,Type[] parameters)=>type.GetMethod(name,parameters);
  public static IEnumerable<MethodInfo> GetDeclaredMethods(Type type)=>type.GetMethods();
 }
}
public sealed class 总结脚本 {
 public sealed class 背包单元 {public int 炮台id,强化上限;public List<int> 强化词条=new();}
 public int 当前金钱数;
 public List<背包单元> 当前拥有炮台=new(),当前携带炮台=new();
 public void 保存单局存档(){Harness.Current.Saves++;}
}
public sealed class 战斗背包 {public 背包类 背包类=new();}
public sealed class 背包类 {
 public List<int> 当前已选单元=new();public List<UnityEngine.GameObject> 单元列表=new();
 public void 更新背包(int count){}
}
public sealed class 玩家背包脚本 {public void 更新背包(){}}
public sealed class 场景切换按钮 {public void 切换场景(int value){}}
public sealed class 联机_2P存档 {public 联机玩家控件.联机总结数据包 联机存档_2P;}
public sealed class 联机玩家控件 {
 public sealed class 联机总结数据包 {public int 当前金钱数;public List<总结脚本.背包单元> 当前拥有炮台=new();}
 public 联机总结数据包 Network总结数据_联机=new();
 public bool isLocalPlayer=true;
 public void 上传玩家总结数据(){
  var snapshot=new 联机总结数据包 {当前金钱数=Harness.Current.Summary.当前金钱数,当前拥有炮台=Harness.QueueTransport?Harness.Current.Summary.当前拥有炮台.ConvertAll(x=>new 总结脚本.背包单元{炮台id=x.炮台id,强化上限=x.强化上限,强化词条=new(x.强化词条)}):Harness.Current.Summary.当前拥有炮台};
  Harness.Current.Uploads++;Harness.Deliver(()=>Network总结数据_联机=snapshot);
 }
 public void 发送消息广播_cmd(string key,string value){
  Harness.Messages.Add((key,value));
  Harness.Deliver(()=>{using(Harness.Use(Harness.Host))Plugin.Transfers.ServerReceive(this,value);});
 }
 public void 发送消息广播_Rpc(string key,string value){
  Harness.Messages.Add((key,value));if(!Harness.DeliverTransfers)return;
  Harness.Deliver(()=>{using(Harness.Use(Harness.Guest))Plugin.Transfers.Receive(key,value);});
  Harness.Deliver(()=>{using(Harness.Use(Harness.Host))Plugin.Transfers.Receive(key,value);});
 }
}
public static class Steam联机管理器 {public static 联机玩家控件 玩家_主机=new(),玩家_客户端=new();}
namespace CluckmechCoop {
 internal sealed class FakeInstance {
  internal readonly List<UnityEngine.Coroutine> Tasks=new();
  internal UnityEngine.Coroutine StartCoroutine(IEnumerator body){var task=new UnityEngine.Coroutine{Body=body};Tasks.Add(task);return task;}
  internal void StopCoroutine(UnityEngine.Coroutine task)=>Tasks.Remove(task);
 }
 internal sealed class CoopPanel {
  internal void RefreshState(){} internal void RefreshInventory(){Harness.Current.InventoryRefreshes++;}
  internal void RefreshWallet(){} internal void Open(){}
 }
 // Session readiness is an input; transfer prepare/state/commit code is production code.
 internal sealed class SessionInput {internal bool SettingsReady=true;}
 internal static class ModuleSettings {internal static bool Transfer {get=>Harness.Current.Enabled;set=>Harness.Current.Enabled=value;}}
 internal static class Plugin {
  internal const string Version="transfer-fixture";
  internal static int Role=>Harness.Current.Role;
  internal static SessionInput Network=>Harness.Current.Session;
  internal static 联机玩家控件 LocalPlayer=>Role==1?Steam联机管理器.玩家_主机:Steam联机管理器.玩家_客户端;
  internal static 总结脚本 Summary=>Harness.Current.Summary;
  internal static FakeInstance Instance=>Harness.Current.Instance;
  internal static TransferService Transfers=>Harness.Current.Transfers;
  internal static bool CanTrade=>ModuleSettings.Transfer;
  internal static CoopPanel Panel=new();internal static void Log(string text){}
 }
}
sealed class Context {
 internal int Role,InventoryRefreshes,Uploads,Saves;internal bool Enabled=true;
 internal TransferService Transfers=new();internal SessionInput Session=new();
 internal FakeInstance Instance=new();internal 总结脚本 Summary=new();
 internal Context(int role){Role=role;}
}
static class Harness {
 internal static bool DeliverTransfers=true,QueueTransport;
 internal static readonly Queue<Action> Transport=new();
 internal static readonly List<(string Key,string Json)> Messages=new();
 internal static Context Host=new(1),Guest=new(2),Current=Host;
 internal static void Deliver(Action action){if(QueueTransport)Transport.Enqueue(action);else action();}
 internal static void Drain(){int count=0;while(Transport.Count>0){if(++count>100)throw new Exception("Transport did not settle");Transport.Dequeue()();}}
 private sealed class Scope:IDisposable {
  private readonly Context old;internal Scope(Context context){old=Current;Current=context;}
  public void Dispose(){Current=old;}
 }
 internal static IDisposable Use(Context context)=>new Scope(context);
}
