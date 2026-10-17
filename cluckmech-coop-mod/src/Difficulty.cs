using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class Difficulty
    {
        private static readonly Dictionary<刷怪器, EncounterSession> rounds = new Dictionary<刷怪器, EncounterSession>();
        private static readonly MethodInfo spawn = AccessTools.Method(typeof(刷怪器), "刷怪");
        private static readonly MethodInfo settle = AccessTools.Method(typeof(刷怪器), "结算界面切换");
        private static 事件管理器_战斗 events;
        internal static 刷怪器 Spawning;
        internal static readonly Dictionary<模块_攻击属性, Strength> Attacks = new Dictionary<模块_攻击属性, Strength>();
        private static bool clientComplete;
        private static readonly HashSet<string> scaledTypes = new HashSet<string>();
        private static readonly Dictionary<BossBloodstrip, float> barMaximums = new Dictionary<BossBloodstrip, float>();
        internal static void Reset()
        {
            if (events != null)
            {
                events.事件_敌人生成.RemoveListener(Generated);
                events.事件_Boss生成.RemoveListener(Generated);
            }
            foreach (var pair in rounds) if (pair.Key != null && pair.Value.Routine != null) pair.Key.StopCoroutine(pair.Value.Routine);
            rounds.Clear(); Attacks.Clear(); scaledTypes.Clear(); barMaximums.Clear(); events = null; Spawning = null; clientComplete = false;
        }
        internal static EncounterSession State(刷怪器 spawner)
        {
            if (!rounds.TryGetValue(spawner, out var state)) rounds.Add(spawner, state = new EncounterSession());
            return state;
        }
        internal static void BindEvents(刷怪器 spawner)
        {
            var value = (事件管理器_战斗)AccessTools.Field(typeof(刷怪器), "战斗事件").GetValue(spawner);
            if (events != value)
            {
                if (events != null)
                {
                    events.事件_敌人生成.RemoveListener(Generated);
                    events.事件_Boss生成.RemoveListener(Generated);
                }
                events = value;
                if (events != null)
                {
                    events.事件_敌人生成.AddListener(Generated);
                    events.事件_Boss生成.AddListener(Generated);
                }
            }

        }
        private static bool IsFinalBoss(GameObject unit) => unit != null && unit.GetComponent<最终boss状态机>() != null;
        internal static bool CountsForEncounter(GameObject unit)
        {
            var hp = unit.GetComponent<模块_基本属性>();
            return hp != null && hp.当前阵营 == 阵营数据库.阵营.敌方 &&
                !hp.单位定位列表.Contains(模块_基本属性.单位定位枚举.矿山) &&
                !hp.单位定位列表.Contains(模块_基本属性.单位定位枚举.场景) &&
                hp.单位类型 != 模块_基本属性.单位类型枚举.陷阱 &&
                hp.单位类型 != 模块_基本属性.单位类型枚举.物品 &&
                hp.单位类型 != 模块_基本属性.单位类型枚举.场景;
        }
        internal static void Generated(GameObject unit)
        {
            if(WealthEncounter.Spawning)return;
            var spawner = Spawning != null ? Spawning : Plugin.Spawner;
            if (Plugin.Role == 2 || spawner == null || !ModuleSettings.Expedition || unit == null) return;
            if (!CountsForEncounter(unit)) return;
            if (IsFinalBoss(unit)) { Mark(unit,true,false); return; }
            if (!ModuleSettings.Active) return;
            var state = State(spawner);
            bool finalBoss = IsFinalBoss(unit);
            if (!finalBoss)
            {
                if (!state.Living.Add(unit)) return;
                var tracked = unit.GetComponent<EncounterEnemy>() ?? unit.AddComponent<EncounterEnemy>();
                tracked.Owner = state;
                tracked.Stage = state.Replay ? state.ReplayStage : spawner.当前波次;
                tracked.Boss = Spawning != null ? state.SpawnBoss : unit.GetComponent<词条_100028_Boss胜利判断>() != null;
                state.StageCounts.TryGetValue(tracked.Stage, out var counts);
                if (counts == null) state.StageCounts.Add(tracked.Stage, counts = new EnemyCounts());
                counts.Enemies++; if (tracked.Boss) counts.Bosses++;
            }
            Mark(unit, finalBoss, state.Replay);
        }
        internal static void Mark(GameObject unit, bool finalBoss, bool replay)
        {
            if ((!finalBoss && !replay) || unit.GetComponent<Strength>() != null) return;
            var marker = unit.AddComponent<Strength>();
            marker.Health = Rules.HealthMultiplier(finalBoss, replay);
            marker.Attack = Rules.AttackMultiplier(finalBoss, replay);
            marker.Source = unit.GetComponent<模块_攻击属性>();
            if (marker.Source != null) Attacks[marker.Source] = marker;
        }
        internal static void ReportScale(string name, float health, float attack, float maximum)
        {
            if (scaledTypes.Add(name + ":" + health + ":" + attack))
                Plugin.Log($"Scaled {name}: HP x{health}, ATK x{attack}, max HP {maximum}");
        }
        internal static void Record(刷怪器 spawner, 攻击波次数据模板.出怪单元 group)
        {
            if(WealthEncounter.Spawning)return;
            if (Plugin.Role == 2 || !ModuleSettings.Active) return;
            if (State(spawner).Plan.OriginalClosed) return;
            var wave = spawner.数据.波次数据[spawner.当前波次];
            State(spawner).Plan.Record(spawner.当前波次, spawner.当前组次, group.单位, group.数量,
                group.是否为boss, group.出现延迟, IsFinalBoss, wave.等待时间, wave.上限时间,
                wave.是否为boss波, spawner.是否boss关无限时间);
        }
        internal static bool Finish(刷怪器 spawner, FinishKind kind, int jump = 0)
        {
            if(!WealthEncounter.Finish(spawner,kind,jump))return false;
            if (!ModuleSettings.Active || spawner.数据 == null) return true;
            var state = State(spawner);
            if (Plugin.Role == 2)
            {
                if (clientComplete) return true;
                if (kind == FinishKind.Advance || kind == FinishKind.Jump) { state.Kind = kind; state.Jump = jump; }
                if (kind == FinishKind.Delay) state.DelayRequested = true;
                if (kind == FinishKind.Victory) state.VictoryRequested = true;
                if (kind == FinishKind.Settle) state.SettleRequested = true;
                return false;
            }
            if (state.Plan.Done) return true;
            if (state.Plan.Stages.Count == 0)
            {
                state.Plan.Complete();
                ModuleSettings.CompleteReward();
                ReleaseBossCompletion(state);
                Plugin.Network.Complete(ModuleSettings.Choice.Token);
                return true;
            }
            if (kind == FinishKind.Delay) state.DelayRequested = true;
            if (kind == FinishKind.Victory) state.VictoryRequested = true;
            if (kind == FinishKind.Settle) state.SettleRequested = true;
            if (state.Kind != FinishKind.None) return false;
            state.Kind = kind; state.Jump = jump;
            if (spawner.关卡状态 != 刷怪器.关卡状态枚举.正在出怪)
            {
                state.Plan.OriginalClosed = true;
                spawner.关卡状态 = 刷怪器.关卡状态枚举.无;
            }

            state.Routine = spawner.StartCoroutine(Replay(spawner, state));
            return false;
        }
        private static bool HasLiving(EncounterSession state)
        {
            return state.Living.Count != 0;
        }
        internal static void Playback(int stage, float delay)
        {
            var spawner = Plugin.Spawner;
            if (spawner == null || !ModuleSettings.Active || clientComplete) return;
            var state = State(spawner);
            state.Replay = true;
            state.ReplayStage = stage;
            state.CountdownWaiting = delay > 0;
            state.CountdownUntil = Time.time + delay;
        }
        private static bool TryPlayback(刷怪器 spawner, out EncounterSession state)
        {
            state = null;
            return ModuleSettings.Active && rounds.TryGetValue(spawner, out state) && state.Replay;
        }
        internal static float PlaybackCountdown(刷怪器 spawner) => TryPlayback(spawner, out var state) && state.CountdownWaiting
            ? Mathf.Max(0, state.CountdownUntil - Time.time) : spawner.倒计时;
        internal static int PlaybackStage(刷怪器 spawner) => TryPlayback(spawner, out var state) ? state.ReplayStage : spawner.当前波次;
        internal static bool PlaybackCounting(刷怪器 spawner) => TryPlayback(spawner, out _) || spawner.是否计时;
        internal static 刷怪器.关卡状态枚举 PlaybackPhase(刷怪器 spawner) => TryPlayback(spawner, out var state)
            ? state.CountdownWaiting ? 刷怪器.关卡状态枚举.等待 : 刷怪器.关卡状态枚举.正在出怪 : spawner.关卡状态;
        private static void ShowPlayback(int stage, float delay)
        {
            Playback(stage, delay);
            Plugin.Network.Playback(ModuleSettings.Choice.Token, stage, delay);
        }
        private static IEnumerator Replay(刷怪器 spawner, EncounterSession state)
        {
            yield return null;
            var pause = new WaitForSeconds(0.2f);
            while (!state.Plan.OriginalClosed && spawner.关卡状态 == 刷怪器.关卡状态枚举.正在出怪) yield return pause;
            state.Plan.OriginalClosed = true;
            spawner.关卡状态 = 刷怪器.关卡状态枚举.无;
            while (HasLiving(state)) yield return pause;
            if (!state.Plan.TryStart(false)) yield break;
            state.Replay = true;
            Plugin.Log($"[Encounter] Original cleared; repeating {state.Plan.Stages.Count} recorded stages; HP x2, ATK x2");
            float nextDelay = 0;
            foreach (var stage in state.Plan.Stages)
            {
                ShowPlayback(stage.Index, nextDelay);
                spawner.波次存活怪物.Clear();
                spawner.存活boss.Clear();
                if (nextDelay > 0) yield return new WaitForSeconds(nextDelay);
                if (nextDelay > 0) ShowPlayback(stage.Index, 0);
                int previousSlot = -1;
                foreach (var group in stage.Groups)
                {
                    if (group.Slot != previousSlot && group.Delay > 0) yield return new WaitForSeconds(group.Delay);
                    previousSlot = group.Slot;
                    var native = new 攻击波次数据模板.出怪单元
                    { 单位 = group.Units, 数量 = group.Count, 是否为boss = group.Boss, 出现延迟 = group.Delay };
                    spawn.Invoke(spawner, new object[] { native });
                }
                state.StageCounts.TryGetValue(stage.Index, out var counts);
                float started = Time.time;
                while (!EncounterPlan<GameObject>.StageFinished(stage,
                    counts != null && counts.Enemies > 0,
                    counts != null && counts.Bosses > 0, Time.time - started)) yield return pause;
                nextDelay = EncounterPlan<GameObject>.NextStageDelay(stage,
                    counts != null && counts.Bosses > 0, Time.time - started);
            }
            while (HasLiving(state)) yield return pause;
            state.Plan.Complete(); state.Replay = false; state.Routine = null;
            Plugin.Log("[Encounter] Strengthened enemies cleared; releasing original completion");
            Plugin.Network.Complete(ModuleSettings.Choice.Token);
            Continue(spawner, state);
        }
        private static void Continue(刷怪器 spawner, EncounterSession state)
        {
            ModuleSettings.CompleteReward();
            ReleaseBossCompletion(state);
            if (state.Continued) return;
            state.Continued = true;
            if (state.Kind == FinishKind.Advance) spawner.下一波次初始化();
            else if (state.Kind == FinishKind.Jump) spawner.下一波次初始化(state.Jump);
            if (state.VictoryRequested) spawner.游戏胜利();
            if (state.SettleRequested) settle.Invoke(spawner, null);
            else if (state.DelayRequested) spawner.延迟结算();
        }
        internal static void CaptureBossReward(词条_100028_Boss胜利判断 boss, GameObject feedback = null)
        {
            var spawner = Plugin.Spawner;
            if (spawner == null || !ModuleSettings.Active) return;
            var state = State(spawner);
            state.Reward.Capture(new BossReward(boss, feedback));
            boss.是否死亡 = false;
            ReleaseBossCompletion(state);
        }
        internal static void RememberBar(BossBloodstrip bar, 模块_基本属性 hp)
        {
            if (hp != null) barMaximums[bar] = hp.当前生命值上限;
        }
        internal static void RefreshBar(BossBloodstrip bar, 模块_基本属性 hp, ref float cachedHealth)
        {
            var spawner = Plugin.Spawner;
            if (hp == null || spawner == null || !ModuleSettings.Active) return;
            if (!barMaximums.TryGetValue(bar, out var maximum) || maximum == hp.当前生命值上限) return;
            barMaximums[bar] = hp.当前生命值上限;
            bar.初始血条(hp.当前生命值上限);
            cachedHealth = hp.当前生命值上限;
        }
        internal static void BossVictory(GameObject world, string message)
        {
            var spawner = Plugin.Spawner;
            if (spawner == null || !ModuleSettings.Active) { if(!WealthEncounter.DeferBoss(world,()=>world.SendMessage(message)))world.SendMessage(message); return; }
            var state = State(spawner);
            state.BossWorld.Capture(world);
            ReleaseBossCompletion(state);
        }
        private static void ReleaseBossCompletion(EncounterSession state)
        {
            bool completed = Plugin.Role == 2 ? clientComplete : state.Plan.Done;
            var reward = state.Reward.Take(completed);
            if (reward != null) Plugin.Instance.StartCoroutine(reward.Apply());
            var world = state.BossWorld.Take(completed);
            if (world != null) world.SendMessage("游戏胜利");
        }
        internal static void ClientFinished()
        {
            if (Plugin.Role != 2) return;
            clientComplete = true;
            ModuleSettings.CompleteReward();
            var spawner = Plugin.Spawner;
            if (spawner != null && rounds.TryGetValue(spawner, out var state)) { state.Replay = false; Continue(spawner, state); }
        }
    }

    internal enum FinishKind { None, Advance, Jump, Victory, Delay, Settle }
    internal sealed class EncounterSession
    {
        internal readonly EncounterPlan<GameObject> Plan = new EncounterPlan<GameObject>();
        internal readonly HashSet<GameObject> Living = new HashSet<GameObject>();
        internal bool Replay, SpawnBoss;
        internal int ReplayStage;
        internal bool CountdownWaiting;
        internal float CountdownUntil;
        internal readonly Dictionary<int, EnemyCounts> StageCounts = new Dictionary<int, EnemyCounts>();
        internal FinishKind Kind;
        internal int Jump;
        internal bool VictoryRequested, DelayRequested, SettleRequested, Continued;
        internal readonly DeferredOnce<BossReward> Reward = new DeferredOnce<BossReward>();
        internal readonly DeferredOnce<GameObject> BossWorld = new DeferredOnce<GameObject>();
        internal Coroutine Routine;
    }
    internal sealed class BossReward
    {
        private readonly GameObject ui, baseCar, comic, feedback;
        private readonly Vector3 position;
        private readonly float delay;
        internal BossReward(词条_100028_Boss胜利判断 boss, GameObject feedback = null)
        {
            this.feedback = feedback;
            ui = boss.是否出天赋 ? boss.天赋UI : boss.是否出专属天赋 ? boss.角色天赋UI : null;
            baseCar = boss.基地车;
            comic = boss.是否最终BOSS ? boss.最终BOSS结束漫画 : null;
            position = boss.transform.position;
            delay = Mathf.Max(0, boss.延时);
        }
        internal IEnumerator Apply()
        {
            if (feedback != null) UnityEngine.Object.Instantiate(feedback, position, Quaternion.identity);
            if (delay > 0) yield return new WaitForSeconds(delay);
            if (ui != null) UnityEngine.Object.Instantiate(ui, position, Quaternion.identity);
            var car = baseCar != null ? baseCar : GameObject.Find("基地车(Clone)");
            if (car != null && car.TryGetComponent<模块_基本属性>(out var hp))
            {
                hp.是否无敌 = true;
                hp.当前是否无敌 = true;
                hp.生命值 = hp.当前生命值;
            }
            Plugin.Log("[Encounter] Boss reward released once after completion");
            if (comic != null)
            {
                if (delay < 5) yield return new WaitForSeconds(5 - delay);
                comic.SetActive(true);
            }
        }
    }

    [HarmonyPatch(typeof(词条_100028_Boss胜利判断), "死亡反馈")]
    internal static class BossRewardGate
    {
        private static bool Prefix(词条_100028_Boss胜利判断 __instance, 效果物体引用器 ___引用器)
        {
            if (!ModuleSettings.Active || Plugin.Spawner == null)
            {
                if(!WealthEncounter.Pending)return true;
                var snapshot=new BossReward(__instance,___引用器.预制体.Boss死亡反馈);
                return !WealthEncounter.DeferBoss(__instance,()=>Plugin.Instance.StartCoroutine(snapshot.Apply()));
            }
            Difficulty.CaptureBossReward(__instance, ___引用器.预制体.Boss死亡反馈);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class PlaybackCountdownUi
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (string name in new[] { "Update", "倒计时出现", "显示波次" })
                yield return AccessTools.Method(typeof(模块_波次倒计时UI), name);
        }
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var reads = new Dictionary<FieldInfo, MethodInfo>
            {
                { AccessTools.Field(typeof(刷怪器), "倒计时"), AccessTools.Method(typeof(Difficulty), nameof(Difficulty.PlaybackCountdown)) },
                { AccessTools.Field(typeof(刷怪器), "当前波次"), AccessTools.Method(typeof(Difficulty), nameof(Difficulty.PlaybackStage)) },
                { AccessTools.Field(typeof(刷怪器), "是否计时"), AccessTools.Method(typeof(Difficulty), nameof(Difficulty.PlaybackCounting)) },
                { AccessTools.Field(typeof(刷怪器), "关卡状态"), AccessTools.Method(typeof(Difficulty), nameof(Difficulty.PlaybackPhase)) }
            };
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == System.Reflection.Emit.OpCodes.Ldfld && instruction.operand is FieldInfo field && reads.TryGetValue(field, out var read))
                {
                    instruction.opcode = System.Reflection.Emit.OpCodes.Call;
                    instruction.operand = read;
                }
                yield return instruction;
            }
        }
    }

    [HarmonyPatch(typeof(BossBloodstrip), "初始化")]
    internal static class BossBarInitialized
    {
        private static void Postfix(BossBloodstrip __instance, 模块_基本属性 ___基本属性)
            => Difficulty.RememberBar(__instance, ___基本属性);
    }

    [HarmonyPatch(typeof(BossBloodstrip), "血量变化")]
    internal static class BossBarMaximum
    {
        private static void Prefix(BossBloodstrip __instance, 模块_基本属性 ___基本属性, ref float ___生命值缓存)
            => Difficulty.RefreshBar(__instance, ___基本属性, ref ___生命值缓存);
    }
    [HarmonyPatch(typeof(BossBloodstrip), "Update")]
    internal static class BossBarReadyGate
    {
        private static bool Prefix(BossBloodstrip __instance, 模块_基本属性 ___基本属性)
            => __instance.本体 != null && ___基本属性 != null;
    }

    [HarmonyPatch]
    internal static class BossVictoryGate
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(词条_100028_Boss胜利判断), "死亡触发");
            yield return AccessTools.Method(typeof(词条_100028_Boss胜利判断), "子物体死亡检测");
        }
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var send = AccessTools.Method(typeof(GameObject), "SendMessage", new[] { typeof(string) });
            var defer = AccessTools.Method(typeof(Difficulty), nameof(Difficulty.BossVictory));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(send))
                {
                    instruction.opcode = System.Reflection.Emit.OpCodes.Call;
                    instruction.operand = defer;
                }
                yield return instruction;
            }
        }
    }
    internal sealed class EnemyCounts { internal int Enemies, Bosses; }
    public sealed class EncounterEnemy : MonoBehaviour
    {
        internal EncounterSession Owner;
        internal int Stage;
        internal bool Boss;
        private void OnDestroy()
        {
            if (Owner != null && Owner.Living.Remove(gameObject) && Owner.StageCounts.TryGetValue(Stage, out var counts))
            { counts.Enemies--; if (Boss) counts.Bosses--; }
        }
    }
    [HarmonyPatch(typeof(刷怪器), "下一波次初始化", new System.Type[0])]
    internal static class FinalAdvance
    {
        private static bool Prefix(刷怪器 __instance) => __instance.当前波次 + 1 < __instance.波次 || Difficulty.Finish(__instance, FinishKind.Advance);
    }
    [HarmonyPatch(typeof(刷怪器), "下一波次初始化", new[] { typeof(int) })]
    internal static class FinalJump
    {
        private static bool Prefix(刷怪器 __instance, int 跳转波次) => 跳转波次 < __instance.波次 || Difficulty.Finish(__instance, FinishKind.Jump, 跳转波次);
    }
    [HarmonyPatch(typeof(刷怪器), "游戏胜利")]
    internal static class VictoryGate
    {
        private static bool Prefix(刷怪器 __instance) => Difficulty.Finish(__instance, FinishKind.Victory);
    }
    [HarmonyPatch(typeof(刷怪器), "延迟结算")]
    internal static class DelayGate
    {
        private static bool Prefix(刷怪器 __instance) => Difficulty.Finish(__instance, FinishKind.Delay);
    }
    [HarmonyPatch(typeof(刷怪器), "结算界面切换")]
    internal static class SettlementGate
    {
        private static bool Prefix(刷怪器 __instance) => Difficulty.Finish(__instance, FinishKind.Settle);
    }

    [HarmonyPatch(typeof(刷怪器), "刷怪")]
    internal static class SpawnContext
    {
        private static void Prefix(刷怪器 __instance, 攻击波次数据模板.出怪单元 组次出怪数据, out 刷怪器 __state)
        {
            __state = Difficulty.Spawning;
            Difficulty.Spawning = __instance;
            if (ModuleSettings.Active) Difficulty.State(__instance).SpawnBoss = 组次出怪数据.是否为boss;
            Difficulty.Record(__instance, 组次出怪数据);
        }
        private static void Finalizer(刷怪器 __state) => Difficulty.Spawning = __state;
    }

    public sealed class Strength : MonoBehaviour
    {
        internal 模块_攻击属性 Source;
        private void OnDestroy() { if (!System.Object.ReferenceEquals(Source, null)) Difficulty.Attacks.Remove(Source); }
        internal float Health = 1;
        internal float Attack = 1;
        private IEnumerator Start()
        {
            // Allow the game's difficulty and mutation components to finish initialization.
            yield return null;
            if (Plugin.Role == 2) yield break;
            var hp = GetComponent<模块_基本属性>();
            if (hp != null)
            {
                float ratio = hp.当前生命值上限 > 0 ? hp.当前生命值 / hp.当前生命值上限 : 1;
                hp.生命值 *= Health;
                hp.变化生命值 *= Health;
                hp.运算生命值上限();
                hp.当前生命值 = hp.当前生命值上限 * ratio;
                Difficulty.ReportScale(gameObject.name, Health, Attack, hp.当前生命值上限);
            }
            if (Source == null) Source = GetComponent<模块_攻击属性>();
            if (Source != null) { Difficulty.Attacks[Source] = this; Source.运算攻击力(); }
        }
    }

    [HarmonyPatch(typeof(模块_基本属性), "Awake")]
    internal static class MarkEnemy
    {
        private static void Postfix(模块_基本属性 __instance)
        {
            if (Plugin.Role == 2) return;
            if (!ModuleSettings.InRun || !ModuleSettings.Expedition) return;
            bool finalBoss = __instance.GetComponent<最终boss状态机>() != null;
            if (finalBoss) Difficulty.Mark(__instance.gameObject, true, false);
        }
    }

    [HarmonyPatch(typeof(模块_攻击属性), "运算攻击力")]
    internal static class AttackScale
    {
        private static void Postfix(模块_攻击属性 __instance)
        {
            if (ModuleSettings.Expedition && Plugin.Role != 2 && Difficulty.Attacks.TryGetValue(__instance, out var strength))
                __instance.当前攻击力 *= strength.Attack;
        }
    }

    [HarmonyPatch(typeof(最终boss状态机), "投掷实现")]
    internal static class BossThrowDamage
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int replaced = 0;
            var scaled = AccessTools.Method(typeof(BossThrowDamage), nameof(Spawn));
            foreach (var instruction in instructions)
            {
                if (instruction.operand is MethodInfo method && method.Name == "Instantiate" && method.DeclaringType == typeof(UnityEngine.Object) && method.GetParameters().Length == 3)
                {
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Ldarg_0) { labels = instruction.labels, blocks = instruction.blocks };
                    instruction.labels = new List<System.Reflection.Emit.Label>();
                    instruction.blocks = new List<ExceptionBlock>();
                    instruction.opcode = System.Reflection.Emit.OpCodes.Call; instruction.operand = scaled;
                    replaced++;
                }
                yield return instruction;
            }
            if (replaced != 1) throw new System.InvalidOperationException("Oasis throw spawn binding changed");
        }
        private static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, 最终boss状态机 boss)
        {
            var instance = UnityEngine.Object.Instantiate(prefab, position, rotation);
            if (ModuleSettings.Expedition && Plugin.Role != 2 && boss.TryGetComponent<Strength>(out var strength) && instance.TryGetComponent<模块_伤害精灵>(out var damage))
                damage.伤害数据.攻击伤害 *= strength.Attack;
            return instance;
        }
    }
}
