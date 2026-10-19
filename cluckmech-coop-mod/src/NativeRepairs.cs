using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using 函数库;

namespace CluckmechCoop
{
    internal static class NativeRepairs
    {
        internal static bool Enabled;
        internal static Animator Model(Component owner)
        {
            var root = owner.GetComponent<Animator>();
            if (root != null) return root;
            var model = owner.transform.Find("本体") ?? owner.transform.Find("炮筒");
            return model == null ? null : model.GetComponent<Animator>();
        }
    }

    [HarmonyPatch(typeof(模块_随从状态机), "Start")]
    internal static class RepairFollowerAnimator
    {
        private static void Postfix(模块_随从状态机 __instance, ref Animator ___敌人身体)
        {
            if (NativeRepairs.Enabled && ___敌人身体 == null) ___敌人身体 = NativeRepairs.Model(__instance);
        }
    }

    [HarmonyPatch(typeof(词条_342_狐朋狗友), "Start")]
    internal static class RepairFoxFriendsInitialization
    {
        private static void Prefix(词条_342_狐朋狗友 __instance,ref 模块_攻击属性 ___攻击属性,ref 模块_光环 ___光环)
        {
            if(!NativeRepairs.Enabled)return;
            ___攻击属性=__instance.GetComponent<模块_攻击属性>();
            ___光环=__instance.GetComponent<模块_光环>();
            ___光环.初始化();
            ___光环.光环优化(___攻击属性.攻击范围);
            ___光环.检索光环内友方();
        }
    }

    [HarmonyPatch(typeof(模块_玩家_死亡), "玩家死亡")]
    internal static class RepairPlayerKillNotification
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var send=AccessTools.Method(typeof(GameObject),"SendMessage",new[]{typeof(string),typeof(object)});
            var code=TranspilerContract.Exact(instructions,"Player kill notification",1,c=>c.Calls(send));
            foreach(var instruction in code)
                if(instruction.Calls(send))
                {instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(RepairPlayerKillNotification),nameof(Send));}
            return code;
        }
        private static void Send(GameObject killer,string message,object victim)
            =>killer.SendMessage(message,NativeRepairs.Enabled?(object)((GameObject)victim).transform.position:victim);
    }

    [HarmonyPatch(typeof(模块_炮塔状态机), "Start")]
    internal static class RepairTurretAnimator
    {
        private static void Prefix(模块_炮塔状态机 __instance)
        {
            if (NativeRepairs.Enabled && __instance.炮筒 == null) __instance.炮筒 = NativeRepairs.Model(__instance);
        }
    }

    [HarmonyPatch]
    internal static class StaticTurretAnimation
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(模块_炮塔状态机), "攻击准备");
            yield return AccessTools.Method(typeof(模块_炮塔状态机), "攻击取消");
        }
        private static bool Prefix(模块_炮塔状态机 __instance) => !NativeRepairs.Enabled || __instance.炮筒 != null;
    }

    [HarmonyPatch(typeof(词条_100067_定时出现火山岩浆气泡), "Start")]
    internal static class RepairLavaInitialization
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            var register = AccessTools.Method(typeof(模块_词条_词缀管理器), "收到词条");
            foreach (var instruction in code)
            {
                if (instruction.Calls(register))
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RepairLavaInitialization), nameof(Register))).MoveLabelsFrom(instruction);
                else yield return instruction;
            }
        }
        private static void Register(模块_词条_词缀管理器 manager, 世界类.词条数据包 data)
        {
            // Terrain emitters have no unit affix manager; their native timer still initializes.
            if (!NativeRepairs.Enabled || manager != null) manager.收到词条(data);
        }
    }

    // Cache bound receivers, while collecting current components to include affixes acquired later.
    public sealed class NativeDamageNotifications : MonoBehaviour
    {
        private readonly List<MonoBehaviour> components = new List<MonoBehaviour>();
        private readonly Dictionary<MonoBehaviour, Action<模块_状态持续效果.状态伤害类>> status = new Dictionary<MonoBehaviour, Action<模块_状态持续效果.状态伤害类>>();
        private readonly Dictionary<MonoBehaviour, Action<float>> scalar = new Dictionary<MonoBehaviour, Action<float>>();
        private Action<T> Receiver<T>(MonoBehaviour component, Dictionary<MonoBehaviour, Action<T>> cache)
        {
            if (cache.TryGetValue(component, out var action)) return action;
            var method = component.GetType().GetMethod("受到伤害",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new[] { typeof(T) },null);
            if (method != null) action = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), component, method);
            else
            {
                method = component.GetType().GetMethod("受到伤害",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,Type.EmptyTypes,null);
                if (method != null)
                {
                    var notify = (Action)Delegate.CreateDelegate(typeof(Action), component, method);
                    action = value => notify();
                }
            }
            cache.Add(component, action);
            return action;
        }
        private void Dispatch<T>(T damage, Dictionary<MonoBehaviour, Action<T>> cache)
        {
            GetComponents(components);
            foreach (var component in components)
            {
                if (component == null) continue;
                if (component is 模块_角色界面UI ui) ui.血条显示倒计时 = 3;
                else Receiver(component, cache)?.Invoke(damage);
            }
        }
        internal static void Send(GameObject target, string name, object damage, SendMessageOptions options)
        {
            if (!NativeRepairs.Enabled) { target.SendMessage(name, damage, options); return; }
            var relay = target.GetComponent<NativeDamageNotifications>() ?? target.AddComponent<NativeDamageNotifications>();
            if (damage is 模块_状态持续效果.状态伤害类 state) relay.Dispatch(state, relay.status);
            else relay.Dispatch((float)damage, relay.scalar);
        }
        internal static void Send(GameObject target, string name, object damage) => Send(target, name, damage, SendMessageOptions.RequireReceiver);
    }

    [HarmonyPatch]
    internal static class RepairDamageNotifications
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(模块_状态持续效果), "状态持续");
            yield return AccessTools.Method(typeof(模块_受伤), "受到伤害", new[] { typeof(世界类.伤害数据包) });
        }
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool damageMessage = false;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr) damageMessage = (string)instruction.operand == "受到伤害";
                if (damageMessage && instruction.operand is MethodInfo method && method.DeclaringType == typeof(GameObject) && method.Name == "SendMessage")
                {
                    var args = new List<Type> { typeof(GameObject) };
                    foreach (var parameter in method.GetParameters()) args.Add(parameter.ParameterType);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NativeDamageNotifications), "Send", args.ToArray())).MoveLabelsFrom(instruction);
                    damageMessage = false;
                }
                else yield return instruction;
            }
        }
    }

    [HarmonyPatch(typeof(UI手柄滚动视图单元格适配), "适配实现")]
    internal static class RepairScrollSelection
    {
        private static bool Prefix(UI手柄滚动视图单元格适配 __instance, GameObject __0)
            => !NativeRepairs.Enabled || __instance != null && __instance.isActiveAndEnabled && __0 != null;
    }

    public sealed class NativeScrollSubscription : MonoBehaviour
    {
        internal Action<GameObject> Listener;
        private void OnDisable() { if (Listener != null) UI焦点控制管理器.事件_选择物体改变 -= Listener; }
        private void OnDestroy() => OnDisable();
    }
    [HarmonyPatch(typeof(UI手柄滚动视图单元格适配), "OnEnable")]
    internal static class RepairScrollSubscription
    {
        private static void Prefix(UI手柄滚动视图单元格适配 __instance)
        {
            if (!NativeRepairs.Enabled) return;
            var scope = __instance.GetComponent<NativeScrollSubscription>() ?? __instance.gameObject.AddComponent<NativeScrollSubscription>();
            if (scope.Listener == null) scope.Listener = (Action<GameObject>)Delegate.CreateDelegate(typeof(Action<GameObject>), __instance, AccessTools.Method(typeof(UI手柄滚动视图单元格适配), "适配实现"));
            UI焦点控制管理器.事件_选择物体改变 -= scope.Listener;
        }
    }

    [HarmonyPatch(typeof(UI_结算界面_天赋界面管理器), "Start")]
    internal static class RepairDuplicateTalentDisplay
    {
        private static bool Prefix(UI_结算界面_天赋界面管理器 __instance)
        {
            if (!NativeRepairs.Enabled || __instance.天赋格子 != null) return true;
            foreach (var sibling in __instance.GetComponents<UI_结算界面_天赋界面管理器>())
                if (sibling != __instance && sibling.天赋格子 != null && sibling.天赋容器位置 == __instance.天赋容器位置)
                { __instance.enabled = false; return false; }
            return true;
        }
    }

    [HarmonyPatch(typeof(UI不时停), "Start")]
    internal static class RepairStaticUiAnimation
    {
        private static bool Prefix(UI不时停 __instance) => !NativeRepairs.Enabled || __instance.GetComponent<Animator>() != null;
    }

    [HarmonyPatch]
    internal static class RewardSceneContext
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(结算界面脚本), "Awake");
            yield return AccessTools.Method(typeof(奖励结算脚本), "初始化");
        }
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
                if (instruction.operand is MethodInfo method && method.Name == "GetComponent" && method.IsGenericMethod && method.GetGenericArguments()[0] == typeof(奖励列表脚本))
                    yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(RewardSceneContext),nameof(RewardList))).MoveLabelsFrom(instruction);
                else yield return instruction;
        }
        private static 奖励列表脚本 RewardList(GameObject target)
            => NativeRepairs.Enabled && target == null ? null : target.GetComponent<奖励列表脚本>();
    }
    [HarmonyPatch(typeof(奖励结算脚本),"随机炮台结果运算")]
    internal static class OptionalRewardDraw
    {
        private static bool Prefix(奖励列表脚本 ___奖励列表) => !NativeRepairs.Enabled || !ReferenceEquals(___奖励列表,null);
    }
    [HarmonyPatch(typeof(结算界面脚本),"总结流程更新")]
    internal static class OptionalRewardSummary
    {
        private static bool Prefix(奖励列表脚本 ___奖励列表) => !NativeRepairs.Enabled || !ReferenceEquals(___奖励列表,null);
    }
    [HarmonyPatch(typeof(奖励流程脚本),"Start")]
    internal static class OptionalRewardFlow
    {
        private static bool Prefix(奖励流程脚本 __instance,ref 奖励结算脚本 ___奖励结算)
        {
            if (!NativeRepairs.Enabled || GameObject.Find("奖励列表") != null) return true;
            ___奖励结算=__instance.GetComponent<奖励结算脚本>();
            return false;
        }
    }

    [HarmonyPatch]
    internal static class RepairRewardArrows
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(结算界面脚本), "结算金币动画");
            yield return AccessTools.Method(typeof(结算界面脚本), "结算动力动画");
        }
        private static bool Prefix(结算界面脚本 __instance, MethodBase __originalMethod)
        {
            if (!NativeRepairs.Enabled) return true;
            bool gold = __originalMethod.Name == "结算金币动画";
            if ((gold ? __instance.结算箭头动画_金币 : __instance.结算箭头动画_动力) != null) return true;
            if (gold) __instance.Invoke("结算动力动画", .2f);
            return false;
        }
    }

    public sealed class NativeMutationSubscription : MonoBehaviour
    {
        internal 突变自选管理器 Manager;
        internal UnityAction<突变模式数据模版> Listener;
        private void OnDestroy() { if (Manager != null) Manager.事件_购买突变自选.RemoveListener(Listener); }
    }
    [HarmonyPatch(typeof(UI_突变模式信息显示), "Start")]
    internal static class RepairMutationDisplay
    {
        private static bool Prefix(UI_突变模式信息显示 __instance, ref 数据库_突变模式 ___数据库, ref UI引用器 ___引用器, ref 全局存档 ___全局存档)
        {
            if (!NativeRepairs.Enabled) return true;
            ___数据库 = GameObject.Find(常数类.命名_数据库).GetComponent<数据库_突变模式>();
            ___引用器 = GameObject.Find(常数类.命名_UI引用器)?.GetComponent<UI引用器>();
            ___全局存档 = GameObject.Find(常数类.命名_全局存档).GetComponent<全局存档>();
            AccessTools.Method(typeof(UI_突变模式信息显示), "载入突变模式").Invoke(__instance, null);
            AccessTools.Method(typeof(UI_突变模式信息显示), "检测是否开启突变模式").Invoke(__instance, null);
            var manager = GameObject.Find(常数类.命名_突变模式管理器)?.GetComponent<突变自选管理器>();
            if (manager != null)
            {
                var scope = __instance.gameObject.AddComponent<NativeMutationSubscription>();
                scope.Manager = manager;
                scope.Listener = (UnityAction<突变模式数据模版>)Delegate.CreateDelegate(typeof(UnityAction<突变模式数据模版>), __instance, AccessTools.Method(typeof(UI_突变模式信息显示), "更新累计星星数"));
                manager.事件_购买突变自选.AddListener(scope.Listener);
            }
            return false;
        }
    }

    [HarmonyPatch]
    internal static class MenuBattleBonuses
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(词条_100211_币塔), "放置效果");
            yield return AccessTools.Method(typeof(词条_100214金钱就是力量_攻速), "增加属性");
        }
        private static bool Prefix() => !NativeRepairs.Enabled || !RunSaveRepair.AtMainMenu;
    }
    [HarmonyPatch]
    internal static class MenuBattleControls
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(建造系统), "Start");
            yield return AccessTools.Method(typeof(基地车放置和爆炸条UI), "Start");
        }
        private static bool Prefix(MonoBehaviour __instance)
        {
            if (!NativeRepairs.Enabled) return true;
            bool missing = __instance is 建造系统 ? GameObject.Find("预放置炮台显示") == null : __instance.GetComponent<模块_阶段判断器>() == null;
            if (!RunSaveRepair.AtMainMenu && !missing) return true;
            if (__instance is 建造系统)
            {
                建造系统.是否建造模式 = 建造系统.是否放置基地车 = 建造系统.是否能建造 = 建造系统.按键建造模式 = 建造系统.联机_是否虚拟放置基地车 = false;
            }
            __instance.enabled = false;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class RepairHitFeedback
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(模块_玩家_受击反馈),"Start");
            yield return AccessTools.Method(typeof(模块_玩家_受击反馈),"受到伤害");
            yield return AccessTools.Method(typeof(模块_玩家_受击反馈),"受伤结束");
        }
        private static bool Prefix(模块_玩家_受击反馈 __instance)
        {
            if (!NativeRepairs.Enabled) return true;
            if (__instance.玩家身体 != null && __instance.玩家身体.GetComponent<SpriteRenderer>() != null) return true;
            var model=__instance.GetComponent<SpriteRenderer>();
            if(model==null)
            {
                var child=__instance.transform.Find("本体")??__instance.transform.Find("炮筒");
                if(child!=null)model=child.GetComponent<SpriteRenderer>();
            }
            if(model==null)return false;
            __instance.玩家身体=model.gameObject;return true;
        }
    }
    [HarmonyPatch(typeof(模块_攻击),"初始化")]
    internal static class RepairAttackOrigin
    {
        private static void Postfix(模块_攻击 __instance)
        {if(NativeRepairs.Enabled&&__instance.炮口位置==null)__instance.炮口位置=__instance.transform;}
    }
    [HarmonyPatch(typeof(词条_6_死亡掉落),"死亡触发")]
    internal static class RepairEmptyDrop
    {
        private static bool Prefix(词条_6_死亡掉落 __instance)
        {
            if(!NativeRepairs.Enabled)return true;
            if(!(UnityEngine.Random.Range(0f,1f)<=__instance.掉落概率)||__instance.随机掉落物==null||__instance.随机掉落物.Length==0)return false;
            var drop=__instance.随机掉落物[UnityEngine.Random.Range(0,__instance.随机掉落物.Length)];
            // Empty entries retain their native probability of awarding nothing.
            if(drop==null)return false;
            bool network=drop.GetComponent<NetworkIdentity>()!=null;
            if(network&&Plugin.Role==2)return false;
            var copy=UnityEngine.Object.Instantiate(drop,__instance.transform.position,__instance.transform.rotation);
            if(network&&Plugin.Role==1)NetworkServer.Spawn(copy);
            return false;
        }
    }
    [HarmonyPatch(typeof(词条_100034_定时销毁),"Start")]
    internal static class RepairEffectLifetime
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var register=AccessTools.Method(typeof(模块_词条_词缀管理器),"收到词条");
            foreach(var instruction in instructions)
                if(instruction.Calls(register))yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(RepairEffectLifetime),nameof(Register))).MoveLabelsFrom(instruction);
                else if(instruction.operand is MethodInfo method&&method.Name=="GetComponent"&&method.IsGenericMethod&&method.GetGenericArguments()[0]==typeof(效果物体引用器))
                    yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(RepairEffectLifetime),nameof(Effects))).MoveLabelsFrom(instruction);
                else yield return instruction;
        }
        private static void Register(模块_词条_词缀管理器 manager,世界类.词条数据包 packet)
        {if(!NativeRepairs.Enabled||manager!=null)manager.收到词条(packet);}
        private static 效果物体引用器 Effects(GameObject target)
            =>NativeRepairs.Enabled&&target==null?null:target.GetComponent<效果物体引用器>();
    }
}
