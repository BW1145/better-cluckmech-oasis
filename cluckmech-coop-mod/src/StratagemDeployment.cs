using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class DeploymentState
    {
        internal Machine[] Cards;
        internal float[] Ready;
        internal StratagemSequence Sequence = new StratagemSequence();
    }
    internal static class StratagemDeployment
    {
        private static 建造系统 build;
        private static GameObject previews;
        private static readonly Dictionary<int,GameObject> previewCards = new Dictionary<int,GameObject>();
        private static readonly Dictionary<int,DeploymentState> roles = new Dictionary<int,DeploymentState>();
        private static int sequence,pending;
        private static string node;
        private static readonly System.Reflection.FieldInfo displayField = AccessTools.Field(typeof(建造系统),"虚拟炮台显示");
        private static readonly System.Reflection.MethodInfo updateMouse = AccessTools.Method(typeof(虚拟炮台显示),"鼠标位置");
        private static readonly System.Reflection.MethodInfo snapPoint = AccessTools.Method(typeof(虚拟炮台显示),"吸附位置");
        internal static bool ValidPoint(Vector2 point,Vector2 volume)
        {
            if (volume.x <= 0 || volume.y <= 0) return true;
            var hit = Physics2D.BoxCast(point,volume-new Vector2(.1f,.1f),0,Vector2.zero,100,91136);
            if (hit.collider == null) return true;
            int layer = hit.collider.gameObject.layer;
            return layer != 10 && layer != 13 && layer != 14 && layer != 16;
        }
        internal static void Clear()
        {
            build = null; roles.Clear(); sequence = pending = 0; node = null;
            if (previews != null) UnityEngine.Object.Destroy(previews);
            previews = null; previewCards.Clear();
        }
        private static Machine Snapshot(总结脚本.背包单元 card) => card == null || card.炮台id <= 0 ? null :
            new Machine { Id = card.炮台id, UpgradeLimit = card.强化上限, Upgrades = new List<int>(card.强化词条) };
        internal static Machine Selected()
        {
            int slot = 建造系统.当前选中背包id;
            var cards = Plugin.Summary?.当前携带炮台;
            return cards == null || slot < 0 || slot >= cards.Count ? null : Snapshot(cards[slot]);
        }
        internal static bool CustomSelected
        {
            get
            {
                int slot = 建造系统.当前选中背包id;
                var cards = Plugin.Summary?.当前携带炮台;
                return ModuleSettings.Extra.Stratagems && cards != null && slot >= 0 && slot < cards.Count && StratagemCatalog.IsUnit(cards[slot].炮台id);
            }
        }
        internal static void Initialize(建造系统 value)
        {
            build = value; node = ModuleSettings.NodeKey;
            if (!ModuleSettings.Extra.Stratagems || Plugin.Summary == null) return;
            var cards = new Machine[CarryCapacity.Slots];
            for (int i = 0; i < cards.Length; i++) cards[i] = Snapshot(Plugin.Summary.当前携带炮台[i]);
            var state = new DeploymentState { Cards = cards, Ready = new float[cards.Length] };
            roles.TryGetValue(Plugin.Role,out var prior);
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null || !StratagemCatalog.IsUnit(cards[i].Id)) continue;
                var p = new StratagemParameters(cards[i].Id,cards[i].Upgrades);
                var item = build.建造单位信息列表[i];
                var saved = ModuleSettings.LocalProgress.StratagemCooldowns.FirstOrDefault(x => x.Node == node && x.Slot == i && Rules.SameMachine(cards[i],x.Machine));
                float remaining = prior != null && i < prior.Cards.Length && Rules.SameMachine(prior.Cards[i],cards[i]) ? Mathf.Max(0,prior.Ready[i]-Time.time) : saved == null ? item.剩余cd时间 : saved.Remaining;
                item.cd时间 = p.Cooldown; item.花费动力 = item.当前花费动力 = p.Power;
                item.剩余cd时间 = remaining; item.是否cd完毕 = remaining <= 0; item.百分比 = remaining/p.Cooldown;
                state.Ready[i] = Time.time+remaining;
            }
            roles[Plugin.Role] = state;
            SendCarry();
        }
        internal static void Begin() => SendCarry();
        private static void SendCarry()
        {
            if (Plugin.Role == 2 && Plugin.Combat?.Token != null && roles.TryGetValue(2,out var state))
                Plugin.Network.Send(new SessionMessage { Type = "stratagem-carry", Token = Plugin.Combat.Token, Cards = state.Cards });
        }
        internal static void Carry(int role,Machine[] cards)
        {
            if (role != 2 || cards == null || cards.Length != CarryCapacity.SlotsFor(role) || roles.ContainsKey(role)) return;
            var owned = Steam联机管理器.玩家_客户端?.Network总结数据_联机?.当前拥有炮台;
            if (owned == null) return;
            if (!StratagemCatalog.OwnsDeck(cards,owned.ConvertAll(Snapshot))) return;
            var state = new DeploymentState { Cards = cards, Ready = new float[cards.Length] };
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null || !StratagemCatalog.IsUnit(cards[i].Id)) continue;
                var saved = ModuleSettings.Progress.Player(role).StratagemCooldowns.FirstOrDefault(x => x.Node == node && x.Slot == i && Rules.SameMachine(cards[i],x.Machine));
                state.Ready[i] = Time.time+(saved?.Remaining ?? StratagemCatalog.Unit(cards[i].Id).InitialCooldown);
            }
            roles.Add(role,state);
        }
        internal static bool CanPlace()
        {
            var card = Selected();
            if (card == null || !StratagemCatalog.IsUnit(card.Id) || build == null || !ModuleSettings.Extra.Stratagems || pending != 0) return false;
            int slot = 建造系统.当前选中背包id;
            var p = new StratagemParameters(card.Id,card.Upgrades);
            return build.建造单位信息列表[slot].是否cd完毕 && 模块_玩家_动力.当前动力 >= p.Power && 建造系统.是否放置基地车;
        }
        internal static bool FinishDrag()
        {
            var card = Selected();
            if (card == null || !StratagemCatalog.IsUnit(card.Id)) return true;
            if (!建造系统.是否建造模式 || !建造系统.是否放置基地车) return false;
            if (CanPlace() && Plugin.InCombat)
            {
                var display = (虚拟炮台显示)displayField.GetValue(build);
                if (输入管理器.当前操作设备 == 输入管理器.操作设备枚举.键鼠) updateMouse.Invoke(display,null);
                snapPoint.Invoke(display,null);
                display.transform.position = display.虚拟炮台位置;
                Vector2 point = display.虚拟炮台位置;
                建造系统.是否能建造 = ValidPoint(point,display.炮台碰撞体.size);
                if (建造系统.是否能建造)
                {
                    var request = new StratagemPlacement { Slot = 建造系统.当前选中背包id, Machine = card, Sequence = ++sequence, X = point.x, Y = point.y };
                    pending = request.Sequence;
                    if (Plugin.Role == 0) Place(0,request);
                    else Plugin.Network.Send(new SessionMessage { Type = "stratagem-place", Token = Plugin.Combat.Token, Placement = request });
                }
            }
            if (建造系统.是否建造模式) 建造系统.按键建造模式 = true;
            return false;
        }
        internal static void Place(int role,StratagemPlacement request)
        {
            if (!Plugin.InCombat || !ModuleSettings.Extra.Stratagems) return;
            if (!roles.TryGetValue(role,out var state) || request.Slot >= state.Cards.Length || !Rules.SameMachine(state.Cards[request.Slot],request.Machine))
            {
                float currentPower = role == 2 ? Steam联机管理器.玩家_客户端.当前动力 : 模块_玩家_动力.当前动力;
                if (role <= 1) Applied(role,request.Sequence,request.Slot,0,currentPower,false);
                else Plugin.Network.Broadcast(new SessionMessage { Type = "stratagem-placed", Token = Plugin.Combat.Token, Role = role,
                    Revision = request.Sequence, Stage = request.Slot, X = currentPower });
                return;
            }
            if (!state.Sequence.Accept(request.Sequence)) return;
            var player = role == 1 ? Steam联机管理器.玩家_主机 : role == 2 ? Steam联机管理器.玩家_客户端 : null;
            float power = role == 2 ? player.当前动力 : 模块_玩家_动力.当前动力;
            var p = new StratagemParameters(request.Machine.Id,request.Machine.Upgrades);
            Vector2 point = new Vector2(request.X,request.Y);
            bool valid = state.Ready[request.Slot] <= Time.time && power >= p.Power &&
                ValidPoint(point,StratagemRegistry.Prefab(request.Machine.Id).GetComponent<BoxCollider2D>().size);
            if (valid)
            {
                power -= p.Power; state.Ready[request.Slot] = Time.time+p.Cooldown;
                var obj = UnityEngine.Object.Instantiate(StratagemRegistry.Prefab(request.Machine.Id),point,Quaternion.identity);
                var stats = obj.GetComponent<模块_基本属性>();
                stats.所属玩家 = (Steam联机管理器.联机状态)role; stats.母体id = stats.ID; stats.背包id位置 = request.Slot;
                if (role == 0)
                {
                    stats.强化词条列表.AddRange(request.Machine.Upgrades);
                    var events = AccessTools.Field(typeof(刷怪器),"战斗事件").GetValue(Plugin.Spawner) as 事件管理器_战斗;
                    if (events != null) events.触发_单位放置(obj,stats.所属玩家);
                }
                else
                {
                    var sync = obj.GetComponent<联机_单位属性同步>();
                    sync.添加待同步词条(request.Machine.Upgrades,true); sync.放置单位初始化(stats.所属玩家);
                    NetworkServer.Spawn(obj,player.gameObject);
                    player.Network当前动力 = power;
                }
            }
            if (role <= 1) Applied(role,request.Sequence,request.Slot,valid ? p.Cooldown : 0,power,valid);
            if (role != 0) Plugin.Network.Broadcast(new SessionMessage { Type = "stratagem-placed", Token = Plugin.Combat.Token, Role = role,
                Revision = request.Sequence, Stage = request.Slot, Delay = valid ? p.Cooldown : 0, X = power, Held = valid });
            Capture();
        }
        internal static void Applied(int role,int revision,int slot,float cooldown,float power,bool accepted)
        {
            if (role != Plugin.Role || revision != pending || build == null) return;
            pending = 0; 模块_玩家_动力.当前动力 = power;
            if (!accepted) return;
            build.状态切换(false); 建造系统.按键建造模式 = false;
            var item = build.建造单位信息列表[slot]; item.剩余cd时间 = cooldown; item.cd时间 = cooldown; item.是否cd完毕 = false;
            if (roles.TryGetValue(role,out var state)) state.Ready[slot] = Time.time+cooldown;
        }
        internal static void Capture()
        {
            if (node == null) return;
            if (build != null && roles.TryGetValue(Plugin.Role,out var local))
                for (int i = 0; i < local.Cards.Length; i++)
                    if (local.Cards[i] != null && StratagemCatalog.IsUnit(local.Cards[i].Id)) local.Ready[i] = Time.time+Mathf.Max(0,build.建造单位信息列表[i].剩余cd时间);
            foreach (var role in roles)
            {
                var saved = ModuleSettings.Progress.Player(role.Key).StratagemCooldowns;
                saved.Clear();
                for (int i = 0; i < role.Value.Cards.Length; i++)
                    if (role.Value.Cards[i] != null && StratagemCatalog.IsUnit(role.Value.Cards[i].Id))
                        saved.Add(new StratagemCooldown { Node = node, Slot = i, Machine = role.Value.Cards[i].Copy(), Remaining = Mathf.Max(0,role.Value.Ready[i]-Time.time) });
            }
        }
        internal static GameObject Preview(int slot,GameObject original)
        {
            if (!ModuleSettings.Extra.Stratagems || Plugin.Summary == null || slot < 0 || slot >= Plugin.Summary.当前携带炮台.Count) return original;
            var machine = Snapshot(Plugin.Summary.当前携带炮台[slot]);
            if (machine == null || !StratagemCatalog.IsUnit(machine.Id)) return original;
            if (previewCards.TryGetValue(slot,out var cached)) return cached;
            if (previews == null) { previews = new GameObject("StratagemPreviews"); previews.SetActive(false); }
            var obj = UnityEngine.Object.Instantiate(original,previews.transform);
            var p = new StratagemParameters(machine.Id,machine.Upgrades);
            var stats = obj.GetComponent<模块_基本属性>(); stats.花费 = p.Power; stats.放置cd = p.Cooldown;
            stats.强化词条列表 = machine.Upgrades;
            stats.显示范围首选项 = 模块_基本属性.显示范围类型枚举.攻击范围;
            var attack = obj.GetComponent<模块_攻击属性>();
            attack.攻击范围 = p.Definition.Kind == StratagemKind.Barrage ? p.ScatterRadius :
                p.Definition.Kind == StratagemKind.Shield || p.Definition.Kind == StratagemKind.Eagle ? p.BlastRadius : p.Definition.Range;
            previewCards.Add(slot,obj); return obj;
        }
    }
    [HarmonyPatch(typeof(预放置炮台碰撞监测),"射线检测")]
    internal static class StratagemPlacementCollision
    {
        private static bool Prefix(预放置炮台碰撞监测 __instance)
        {
            if (!StratagemDeployment.CustomSelected) return true;
            __instance.体积 = __instance.虚拟炮台.炮台碰撞体.size;
            __instance.是否开始检测 = true;
            __instance.是否允许放置 = StratagemDeployment.ValidPoint(__instance.transform.position,__instance.体积);
            建造系统.是否能建造 = __instance.是否允许放置;
            return false;
        }
    }
    [HarmonyPatch(typeof(建造系统),"背包cd初始化",new Type[0])]
    internal static class StratagemCardCooldowns { private static void Postfix(建造系统 __instance) => StratagemDeployment.Initialize(__instance); }
    [HarmonyPatch(typeof(建造系统),"结束拖动延迟")]
    internal static class PlaceStratagemCard { private static bool Prefix() => StratagemDeployment.FinishDrag(); }
    [HarmonyPatch(typeof(建造系统),"炮台运算")]
    internal static class PreviewStratagemCard { private static void Postfix(int 背包id号,ref GameObject __result) => __result = StratagemDeployment.Preview(背包id号,__result); }
    [HarmonyPatch(typeof(建造系统),"开始拖动")]
    internal static class StartStratagemDrag
    {
        private static bool Prefix(建造系统 __instance)
        {
            var card = StratagemDeployment.Selected();
            if (card == null || !StratagemCatalog.IsUnit(card.Id)) return true;
            if (StratagemDeployment.CanPlace()) { __instance.状态切换(true); __instance.移动端_背包选择(建造系统.当前选中背包id); }
            return false;
        }
    }
    [HarmonyPatch(typeof(联机玩家控件),"UserCode_放置单位_cmd__放置单位数据")]
    internal static class StratagemNativeCommand
    {
        private static bool Prefix(联机玩家控件.放置单位数据 数据) => !StratagemCatalog.IsUnit(数据.id);
    }
}
