using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace CluckmechCoop
{
    public sealed class SwallowTargets : MonoBehaviour
    {
        private static readonly List<模块_基本属性> friendly = new List<模块_基本属性>();
        private static readonly HashSet<模块_基本属性> members = new HashSet<模块_基本属性>();
        private readonly Collider2D[] hits = new Collider2D[32];
        private readonly List<模块_基本属性> selected = new List<模块_基本属性>();
        private readonly List<模块_基本属性> execution = new List<模块_基本属性>();
        private 词条_100159_肚子鳄 crocodile;
        private 词条_100176_水母骑士 jelly;
        private NetworkBehaviour actor;
        private Camera camera;
        private float nextPreview;
        private int sent,received;
        private float nextUse;
        private Predicate<模块_基本属性> validate;
        private Func<模块_基本属性,float> distance;
        private float Range => crocodile != null ? crocodile.吞噬范围 : jelly.吞噬范围;
        private int Count => crocodile != null ? crocodile.吞噬数量 : jelly.吞噬数量;
        private GameObject Marker => crocodile != null ? crocodile.即将吞噬目标 : jelly.即将吞噬目标;
        private bool Fake => crocodile != null && crocodile.是否假吞;
        private static bool Enabled => ModuleSettings.Extra.Characters && Plugin.InCombat;
        internal static IEnumerable<MethodBase> Methods(string crocodileName,string jellyName)
        {
            yield return AccessTools.Method(typeof(词条_100159_肚子鳄),crocodileName);
            yield return AccessTools.Method(typeof(词条_100176_水母骑士),jellyName);
        }
        internal static SwallowTargets For(NetworkBehaviour actor)
        {
            var value = actor.GetComponent<SwallowTargets>() ?? actor.gameObject.AddComponent<SwallowTargets>();
            if (value.actor == null)
            {
                value.actor = actor;
                value.crocodile = actor as 词条_100159_肚子鳄;
                value.jelly = actor as 词条_100176_水母骑士;
                value.camera = Camera.main;
                value.validate=value.Valid;
                value.distance=value.Distance;
            }
            return value;
        }
        private static bool Eligible(模块_基本属性 value)
        {
            if (value == null || value.当前生命值 <= 0 || value.当前阵营 != 阵营数据库.阵营.友方) return false;
            var flags = value.单位定位列表;
            return !flags.Contains(模块_基本属性.单位定位枚举.基地) && !flags.Contains(模块_基本属性.单位定位枚举.玩家) && !flags.Contains(模块_基本属性.单位定位枚举.不可吞噬单位);
        }
        internal static void Begin()
        { Clear(); if (Enabled) foreach (var unit in 世界事件系统.场上所有单位) Add(unit); }
        internal static void Add(模块_基本属性 value) { if (Enabled && value!=null && members.Add(value)) friendly.Add(value); }
        internal static void Remove(模块_基本属性 value) { if (members.Remove(value)) friendly.Remove(value); }
        internal static void Clear() { members.Clear(); friendly.Clear(); }
        private bool Valid(模块_基本属性 value) => Eligible(value) && members.Contains(value) &&
            ((Vector2)value.transform.position-(Vector2)transform.position).sqrMagnitude <= Range*Range;
        private float Distance(模块_基本属性 value)=>((Vector2)value.transform.position-(Vector2)transform.position).sqrMagnitude;
        private void Select()
        {
            if (camera == null) camera = Camera.main;
            模块_基本属性 pointed = null;
            if (camera != null && 输入管理器.当前操作设备 == 输入管理器.操作设备枚举.键鼠)
            {
                Vector2 mouse = camera.ScreenToWorldPoint(Input.mousePosition);
                int total = Physics2D.OverlapPointNonAlloc(mouse,hits);
                float nearest = float.PositiveInfinity;
                for (int i = 0; i < total; i++)
                {
                    var candidate = hits[i].GetComponentInParent<模块_基本属性>();
                    if (!Eligible(candidate)) continue;
                    float distance = ((Vector2)candidate.transform.position-mouse).sqrMagnitude;
                    if (distance < nearest) { pointed = candidate; nearest = distance; }
                }
            }
            SwallowSelection.Choose(friendly,pointed,Count,validate,distance,selected);
        }
        internal void Preview(bool ready)
        {
            var marker = Marker;
            if (marker == null) return;
            if (!Enabled || !ready || Plugin.Role != 0 && !actor.isOwned) { marker.SetActive(false); return; }
            if (Time.time >= nextPreview) { nextPreview = Time.time+.08f; Select(); }
            bool visible = selected.Count > 0 && Valid(selected[0]);
            marker.SetActive(visible);
            if (visible) marker.transform.position = selected[0].transform.position;
        }
        internal void Request()
        {
            Select();
            if (Plugin.Role == 0) { Execute(selected); return; }
            var ids = new uint[selected.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = StratagemWorld.Id(selected[i].gameObject);
            Plugin.Network.Send(new SessionMessage {Type="swallow-request",Token=Plugin.Combat.Token,Unit=StratagemWorld.Id(gameObject),Targets=ids,Revision=++sent});
        }
        private void Execute(List<模块_基本属性> targets)
        {
            execution.Clear(); execution.AddRange(targets);
            if (!Fake) foreach (var target in execution) Remove(target);
            AccessTools.Method(actor.GetType(),"吞噬触发").Invoke(actor,null);
            execution.Clear();
        }
        internal static List<模块_基本属性> Execution(NetworkBehaviour actor) => For(actor).execution;
        internal static bool Handle(SessionMessage message,int role,bool server)
        {
            if (!Enabled || message.Token != Plugin.Combat?.Token) return false;
            var spawned = server ? NetworkServer.spawned : NetworkClient.spawned;
            if (!spawned.TryGetValue(message.Unit,out var identity)) return true;
            NetworkBehaviour actor = identity.GetComponent<词条_100159_肚子鳄>();
            if (actor == null) actor = identity.GetComponent<词条_100176_水母骑士>();
            if (actor == null) return true;
            var value = For(actor);
            if (message.Revision <= value.received) return true;
            if (server)
            {
                var owner = role == 1 ? Steam联机管理器.玩家_主机 : Steam联机管理器.玩家_客户端;
                var health = identity.GetComponent<模块_基本属性>();
                if (owner == null || identity.connectionToClient != owner.connectionToClient || health == null || health.当前生命值 <= 0 || Time.time < value.nextUse || message.Targets.Length > value.Count) return true;
            }
            var targets = new List<模块_基本属性>();
            foreach (uint id in message.Targets)
                if (spawned.TryGetValue(id,out var target))
                {
                    var unit = target.GetComponent<模块_基本属性>();
                    if (value.Valid(unit) && !targets.Contains(unit)) targets.Add(unit);
                }
            value.received = message.Revision;
            if (server)
            {
                var accepted = new uint[targets.Count];
                for (int i = 0; i < accepted.Length; i++) accepted[i] = StratagemWorld.Id(targets[i].gameObject);
                message.Type = "swallow"; message.Targets = accepted;
                if (targets.Count > 0) value.nextUse = Time.time+(value.crocodile != null ? value.crocodile.技能cd : value.jelly.技能cd);
                Plugin.Network.Broadcast(message);
            }
            value.Execute(targets); return true;
        }
    }
    [HarmonyPatch]
    internal static class SwallowPreview
    {
        private static IEnumerable<MethodBase> TargetMethods() => SwallowTargets.Methods("吞噬目标","吞噬目标");
        private static bool Prefix(NetworkBehaviour __instance,bool ___是否技能可用)
        { if (!ModuleSettings.Extra.Characters || !Plugin.InCombat) return true; SwallowTargets.For(__instance).Preview(___是否技能可用); return false; }
    }
    [HarmonyPatch]
    internal static class SwallowInput
    {
        private static IEnumerable<MethodBase> TargetMethods() => SwallowTargets.Methods("技能实现实现","吞噬触发实现");
        private static bool Prefix(NetworkBehaviour __instance)
        { if (!ModuleSettings.Extra.Characters || !Plugin.InCombat) return true; SwallowTargets.For(__instance).Request(); return false; }
    }
    [HarmonyPatch]
    internal static class SwallowExecution
    {
        private static IEnumerable<MethodBase> TargetMethods() => SwallowTargets.Methods("吞噬触发","吞噬触发");
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
        {
            var code = new List<CodeInstruction>(source);
            int selection = code.FindIndex(x => x.operand is MethodInfo method && method.Name == "FindAll" && method.DeclaringType == typeof(List<模块_基本属性>));
            if (selection < 0) throw new InvalidOperationException("Native swallow selection entry missing");
            var begin = new CodeInstruction(OpCodes.Ldarg_0);
            begin.labels.AddRange(code[0].labels);
            yield return begin;
            yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(SwallowExecution),nameof(Targets)));
            for (int i = selection+1; i < code.Count; i++) yield return code[i];
        }
        private static List<模块_基本属性> Targets(NetworkBehaviour actor)
        {
            if (ModuleSettings.Extra.Characters && Plugin.InCombat) return SwallowTargets.Execution(actor);
            var values = new List<模块_基本属性>();
            float range = actor is 词条_100159_肚子鳄 crocodile ? crocodile.吞噬范围 : ((词条_100176_水母骑士)actor).吞噬范围;
            foreach (var unit in 世界事件系统.场上所有单位)
                if (unit != null && unit.gameObject != actor.gameObject && unit.当前阵营 == 阵营数据库.阵营.友方 && unit.获取与此单位距离(actor.transform.position) <= range &&
                    !unit.单位定位列表.Contains(模块_基本属性.单位定位枚举.基地) && !unit.单位定位列表.Contains(模块_基本属性.单位定位枚举.玩家) && !unit.单位定位列表.Contains(模块_基本属性.单位定位枚举.不可吞噬单位)) values.Add(unit);
            values.Sort((a,b)=>a.获取与此单位距离(actor.transform.position).CompareTo(b.获取与此单位距离(actor.transform.position)));
            return values;
        }
    }
    [HarmonyPatch(typeof(模块_基本属性),"Start")]
    internal static class SwallowUnitAdded { private static void Postfix(模块_基本属性 __instance) => SwallowTargets.Add(__instance); }
    [HarmonyPatch(typeof(模块_基本属性),"OnDisable")]
    internal static class SwallowUnitRemoved { private static void Prefix(模块_基本属性 __instance) => SwallowTargets.Remove(__instance); }
}
