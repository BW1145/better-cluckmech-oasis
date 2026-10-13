using System;
using System.Collections.Generic;
using System.Reflection;
using CluckmechCoop;
using UnityEngine;
namespace UnityEngine
{
 public class MonoBehaviour { public GameObject gameObject;public T GetComponent<T>()=>gameObject.GetComponent<T>(); }
 public class GameObject
 {
  readonly Dictionary<Type,MonoBehaviour> components=new();
  public T GetComponent<T>()=>components.TryGetValue(typeof(T),out var value)?(T)(object)value:default;
  public T AddComponent<T>() where T:MonoBehaviour,new(){var c=new T{gameObject=this};components[typeof(T)]=c;return c;}
 }
}
public class 模块_基本属性:MonoBehaviour
{
 public 阵营数据库.阵营 当前阵营=阵营数据库.阵营.敌方;
 public List<单位定位枚举> 单位定位列表=new();
 public 单位类型枚举 单位类型;
 public enum 单位定位枚举{矿山,场景}
 public enum 单位类型枚举{怪物,建筑,陷阱,物品,场景}
}
public class 模块_攻击属性:MonoBehaviour{}
public class 最终boss状态机:MonoBehaviour{}
public class 词条_100028_Boss胜利判断:MonoBehaviour{}
public class 刷怪器 { public int 当前波次; }
public static class 阵营数据库 { public enum 阵营{敌方,友方} }
namespace CluckmechCoop
{
 internal static class Plugin { internal static int Role;internal static 刷怪器 Spawner=new();internal static bool Challenge=>ModuleSettings.Active; }
 internal static class ModuleSettings {internal static bool Active=true;}
 internal sealed class EncounterSession
 {
  internal readonly HashSet<GameObject> Living=new();
  internal readonly Dictionary<int,EnemyCounts> StageCounts=new();
  internal bool Replay,SpawnBoss;
  internal int ReplayStage;
 }
 internal sealed class EnemyCounts {internal int Enemies,Bosses;}
 public class Strength:MonoBehaviour {internal float Health,Attack;internal 模块_攻击属性 Source;}
}
static class Program
{
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 static GameObject Unit(模块_基本属性.单位类型枚举 type=模块_基本属性.单位类型枚举.怪物)
 {var unit=new GameObject();unit.AddComponent<模块_基本属性>().单位类型=type;unit.AddComponent<模块_攻击属性>();return unit;}
 static void Awake(GameObject unit)=>typeof(MarkEnemy).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{unit.GetComponent<模块_基本属性>()});
 static void Die(GameObject unit)=>typeof(EncounterEnemy).GetMethod("OnDestroy",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(unit.GetComponent<EncounterEnemy>(),null);
 static void Main()
 {
  var state=Difficulty.State(Plugin.Spawner);state.Replay=true;state.ReplayStage=2;
  var mineral=Unit();mineral.GetComponent<模块_基本属性>().单位定位列表.Add(模块_基本属性.单位定位枚举.矿山);
  Awake(mineral);Difficulty.Generated(mineral);
  Check(state.Living.Count==0&&mineral.GetComponent<Strength>()==null,"mineral neither blocks settlement nor gains replay scaling");
  foreach(var type in new[]{模块_基本属性.单位类型枚举.陷阱,模块_基本属性.单位类型枚举.物品,模块_基本属性.单位类型枚举.场景})
  {var unit=Unit(type);Difficulty.Generated(unit);Check(unit.GetComponent<EncounterEnemy>()==null&&unit.GetComponent<Strength>()==null,"noncombat object excluded: "+type);}
  var scene=Unit();scene.GetComponent<模块_基本属性>().单位定位列表.Add(模块_基本属性.单位定位枚举.场景);Difficulty.Generated(scene);
  Check(scene.GetComponent<EncounterEnemy>()==null,"scene-tag object excluded");
  foreach(var name in new[]{"poison","missile","bee","boss arm"})
  {var auxiliary=Unit();Awake(auxiliary);Check(auxiliary.GetComponent<EncounterEnemy>()==null&&auxiliary.GetComponent<Strength>()==null,"non-wave Awake never enrolls "+name);}
  var normal=Unit();Difficulty.Generated(normal);Difficulty.Generated(normal);
  Check(state.Living.Count==1&&state.StageCounts[2].Enemies==1,"native generated event enrolled exactly once");
  Check(normal.GetComponent<Strength>().Health==1.5f&&normal.GetComponent<Strength>().Attack==2,"wave root receives intended replay modifiers");
  var building=Unit(模块_基本属性.单位类型枚举.建筑);Difficulty.Generated(building);
  Check(state.Living.Contains(building),"wave enemy building kept");
  var boss=Unit();boss.AddComponent<词条_100028_Boss胜利判断>();Difficulty.Generated(boss);
  Check(state.StageCounts[2].Bosses==1&&state.Living.Contains(boss),"real boss kept without relying on unit ID");
  var final=Unit();final.AddComponent<最终boss状态机>();Awake(final);Difficulty.Generated(final);
  Check(final.GetComponent<Strength>().Health==10&&final.GetComponent<Strength>().Attack==2,"oasis receives dedicated modifiers");
  Check(!state.Living.Contains(final),"oasis is not enrolled for additional spawning");
  var friend=Unit();friend.GetComponent<模块_基本属性>().当前阵营=阵营数据库.阵营.友方;Difficulty.Generated(friend);
  Check(friend.GetComponent<EncounterEnemy>()==null,"friendly excluded");
  Plugin.Role=2;var client=Unit();Difficulty.Generated(client);Check(client.GetComponent<EncounterEnemy>()==null,"client cannot duplicate host count");Plugin.Role=0;
  ModuleSettings.Active=false;var disabled=Unit();Difficulty.Generated(disabled);Check(disabled.GetComponent<EncounterEnemy>()==null,"disabled module retains native spawning");ModuleSettings.Active=true;
  Die(normal);Die(normal);Check(state.StageCounts[2].Enemies==2,"duplicate destruction cannot decrement twice");
  Die(boss);Check(state.StageCounts[2].Bosses==0,"boss death removes boss count");Die(building);
  var living=typeof(Difficulty).GetMethod("HasLiving",BindingFlags.NonPublic|BindingFlags.Static);
  Check(!(bool)living.Invoke(null,new object[]{state}),"clearing wave roots completes while mineral and auxiliaries remain");
  Console.WriteLine($"PASS: {checks} production encounter enrollment and destruction checks using Unity event/object doubles");
 }
}
