using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CluckmechCoop
{
    internal sealed class ThemeSpawnContext
    {
        internal int WaveIndex, BatchIndex, GroupIndex, UnitIndex;
        internal string UnitId, EntranceId;
    }

    public sealed class ThemeBattleWaves : MonoBehaviour
    {
        private static readonly MethodInfo nativeSpawn = AccessTools.Method(typeof(刷怪器), "刷怪");
        internal static ThemeBattleWaves Current { get; private set; }
        internal ThemeBattleScene Scene { get; private set; }
        internal ThemeMapDefinition Definition => Scene.Definition;
        internal IReadOnlyCollection<GameObject> Living => living;
        internal event Action<GameObject, ThemeSpawnContext> Spawned;
        internal event Action<int> WaveStarting;
        internal event Action<int, int> BatchStarting;
        internal event Action AllScheduledEnemiesCleared;
        internal Func<int, int, ThemeSpawnGroup[], ThemeSpawnGroup[]> ConfigureBatch;
        internal Func<int, int, IEnumerator> BeforeBatch;
        internal string FinalRewardUnitId;
        private readonly Dictionary<攻击波次数据模板.出怪单元, (int Wave, int Batch)> batches = new Dictionary<攻击波次数据模板.出怪单元, (int, int)>();
        private readonly HashSet<GameObject> living = new HashSet<GameObject>();
        private readonly HashSet<GameObject> waveIgnored = new HashSet<GameObject>();
        private readonly HashSet<string> holds = new HashSet<string>();
        private 刷怪器 spawner;
        private 事件管理器_战斗 events;
        private 攻击波次数据模板 data;
        private ThemeSpawnContext spawning;
        private List<GameObject> generated;
        private int pendingGroups, pendingBatches, remainingBatches;
        private BossReward reward;
        private bool rewardReleased, victorious, scheduledClearNotified;
        private int previousFlowerPlayer = -1;
        private readonly HashSet<int> capturedBosses = new HashSet<int>();

        internal static void Attach(ThemeBattleScene scene, 刷怪器 owner)
        {
            var current = scene.gameObject.AddComponent<ThemeBattleWaves>(); Current = current;
            current.Scene = scene; current.spawner = owner;
            current.FinalRewardUnitId = scene.Definition.BossUnitId;
            current.events = Object.FindObjectOfType<事件管理器_战斗>();
            current.events.事件_敌人生成.AddListener(current.Generated);
            current.events.事件_Boss生成.AddListener(current.Generated);
            current.events.事件_单位死亡.AddListener(current.Died);
            owner.波次开始广播.AddListener(current.Started);
            current.Configure();
        }

        private void Configure()
        {
            data = ScriptableObject.CreateInstance<攻击波次数据模板>();
            data.name = "ThemeWaves_" + Definition.Id;
            data.是否最终关卡 = false; data.是否被动刷怪 = false; data.是否替换怪物难度等级 = true;
            data.怪物难度等级 = 24; data.boss关无限时间 = true;
            data.额外难度波次列表 = new List<攻击波次数据模板.额外难度波次数据类>();
            data.波次数据 = new List<攻击波次数据模板.波次数据数据包>();
            for (int w = 0; w < Definition.Waves.Length; w++)
            {
                var wave = Definition.Waves[w];
                var native = new 攻击波次数据模板.波次数据数据包 {
                    等待时间 = w == 0 ? Definition.InitialDeploymentSeconds : Definition.BetweenWavesSeconds,
                    上限时间 = float.MaxValue, 是否为boss波 = w == Definition.Waves.Length - 1,
                    是否含有飞行敌人 = wave.Batches.SelectMany(b => b.Groups).Any(g => g.UnitId == ThemeCastleData.WineBat || g.UnitId == ThemeCastleData.BossWineBat),
                    组次数据 = new List<攻击波次数据模板.出怪单元>()
                };
                float previous = 0;
                for (int b = 0; b < wave.Batches.Length; b++)
                {
                    var batch = wave.Batches[b];
                    var marker = new 攻击波次数据模板.出怪单元 {
                        单位 = new List<GameObject>(), 数量 = 0, 是否为boss = false,
                        出现延迟 = batch.StartSeconds - previous
                    };
                    native.组次数据.Add(marker); batches.Add(marker, (w, b)); previous = batch.StartSeconds;
                }
                data.波次数据.Add(native);
            }
            spawner.数据 = data;
            spawner.出怪口 = Scene.Entrances.Values.Select(t => t.gameObject).ToList();
            spawner.boss出怪口 = new List<GameObject>(spawner.出怪口);
            spawner.波次 = data.波次数据.Count; spawner.当前波次 = spawner.当前组次 = 0;
            spawner.关卡状态 = 刷怪器.关卡状态枚举.放置等待;
            spawner.是否计时 = false;
        }

        private void Started(int wave)
        {
            if (wave < 0 || wave >= Definition.Waves.Length) return;
            remainingBatches = Definition.Waves[wave].Batches.Length;
            WaveStarting?.Invoke(wave);
        }

        internal static void NativeGroup(刷怪器 owner, 攻击波次数据模板.出怪单元 group)
        {
            var current = Current;
            if (current == null || current.spawner != owner || !current.batches.TryGetValue(group, out var location) || Plugin.Role == 2) return;
            current.remainingBatches--;
            current.pendingBatches++;
            current.StartCoroutine(current.DispatchBatch(location.Wave, location.Batch));
        }

        private IEnumerator DispatchBatch(int wave, int batch)
        {
            try
            {
                var delay = BeforeBatch?.Invoke(wave, batch);
                if (delay != null) yield return delay;
                BatchStarting?.Invoke(wave, batch);
                var groups = Definition.Waves[wave].Batches[batch].Groups;
                if (ConfigureBatch != null) groups = ConfigureBatch(wave, batch, groups);
            for (int i = 0; i < groups.Length; i++)
            {
                    pendingGroups++;
                    StartCoroutine(SpawnGroup(groups[i], wave, batch, i));
            }
            }
            finally { pendingBatches--; }
        }

        private IEnumerator SpawnGroup(ThemeSpawnGroup group, int wave, int batch, int groupIndex)
        {
            try
            {
                if (group.DelaySeconds > 0) yield return new WaitForSeconds(group.DelaySeconds);
                for (int i = 0; i < group.Count; i++)
                {
                    if (i > 0 && group.IntervalSeconds > 0) yield return new WaitForSeconds(group.IntervalSeconds);
                    var entrance = Definition.Entrances.First(e => e.Id == group.EntranceId);
                    float fraction = group.Count <= 1 ? .5f : (i % 7 + .5f) / 7;
                    var point = Scene.EntrancePoint(Vector2.Lerp(Scene.World(entrance.LineStart), Scene.World(entrance.LineEnd), fraction));
                    SpawnOne(group.UnitId, point, group.UnitId == Definition.BossUnitId,
                        new ThemeSpawnContext { WaveIndex = wave, BatchIndex = batch, GroupIndex = groupIndex,
                            UnitIndex = i, UnitId = group.UnitId, EntranceId = group.EntranceId });
                }
            }
            finally { pendingGroups--; }
        }

        internal static List<GameObject> Spawn(string unitId, int count, IEnumerable<string> entranceKeys, bool boss = false)
        {
            var current = Current; var result = new List<GameObject>();
            if (current == null || Plugin.Role == 2 || current.victorious) return result;
            var keys = entranceKeys.ToArray();
            if (keys.Length == 0) return result;
            for (int i = 0; i < count; i++)
            {
                string key = keys[i % keys.Length];
                if (!current.Scene.Entrances.TryGetValue(key, out var entrance)) throw new InvalidOperationException("Unknown theme entrance " + key);
                var point = current.Scene.EntrancePoint((Vector2)entrance.position + Vector2.right * ((i % 3) - 1) * .45f);
                result.AddRange(current.SpawnOne(unitId, point, boss,
                    new ThemeSpawnContext { WaveIndex = current.spawner.当前波次, BatchIndex = -1, GroupIndex = -1,
                        UnitIndex = i, UnitId = unitId, EntranceId = key }));
            }
            return result;
        }

        internal static List<GameObject> SpawnAt(string unitId, int count, Vector2 point, bool boss = false)
        {
            var current = Current; var result = new List<GameObject>();
            if (current == null || Plugin.Role == 2 || current.victorious) return result;
            for (int i = 0; i < count; i++)
            {
                var desired = count == 1 ? point : point + new Vector2(Mathf.Cos(i * 2.4f), Mathf.Sin(i * 2.4f)) * (.4f + .15f * (i % 3));
                result.AddRange(current.SpawnOne(unitId, current.Scene.EntrancePoint(desired), boss,
                    new ThemeSpawnContext { WaveIndex = current.spawner.当前波次, BatchIndex = -1, GroupIndex = -1,
                        UnitIndex = i, UnitId = unitId, EntranceId = null }));
            }
            return result;
        }

        private List<GameObject> SpawnOne(string unitId, Vector2 point, bool boss, ThemeSpawnContext context)
        {
            var prefab = ThemeEnemyRegistry.Prefab(unitId);
            if (prefab == null) throw new InvalidOperationException("Theme enemy prefab is missing: " + unitId);
            var marker = new GameObject("ThemeSpawnPoint"); marker.transform.position = point;
            var oldEntrances = spawner.出怪口; var oldBossEntrances = spawner.boss出怪口;
            var oldContext = spawning; var oldGenerated = generated; var result = new List<GameObject>();
            spawning = context; generated = result;
            spawner.出怪口 = spawner.boss出怪口 = new List<GameObject> { marker };
            try
            {
                nativeSpawn.Invoke(spawner, new object[] { new 攻击波次数据模板.出怪单元 {
                    单位 = new List<GameObject> { prefab }, 数量 = 1, 是否为boss = boss, 出现延迟 = 0
                } });
            }
            finally
            {
                spawner.出怪口 = oldEntrances; spawner.boss出怪口 = oldBossEntrances;
                spawning = oldContext; generated = oldGenerated; Object.Destroy(marker);
            }
            foreach (var unit in result) Spawned?.Invoke(unit, context);
            return result;
        }

        private void Generated(GameObject unit)
        {
            if (unit == null || Plugin.Role == 2) return;
            if (Plugin.Role == 1 && unit.GetComponent<ThemeEnemyActor>() != null &&
                unit.TryGetComponent<模块_难度等级>(out var difficulty) && unit.TryGetComponent<联机_单位属性同步>(out var sync))
                sync.Network待同步难度等级 = difficulty.设定等级;
            Register(unit);
            if (spawning != null) generated.Add(unit);
        }

        internal GameObject NextFlowerTarget()
        {
            模块_基本属性 next = null, first = null;
            foreach (var unit in 世界事件系统.场上所有单位)
            {
                if (!ThemeDeepSeaCombat.IsPlayer(unit)) continue;
                int role = (int)unit.所属玩家;
                if (first == null || role < (int)first.所属玩家) first = unit;
                if (role > previousFlowerPlayer && (next == null || role < (int)next.所属玩家)) next = unit;
            }
            next = next ?? first;
            if (next == null) return ThemeDeepSeaCombat.BaseUnit()?.gameObject;
            previousFlowerPlayer = (int)next.所属玩家;
            return next.gameObject;
        }

        internal void Register(GameObject unit)
        {
            if (!living.Add(unit)) return;
            var tracked = unit.GetComponent<ThemeBattleEnemy>();
            if (tracked == null) tracked = unit.AddComponent<ThemeBattleEnemy>();
            tracked.Owner = this;
        }
        internal void Died(GameObject unit) { living.Remove(unit); waveIgnored.Remove(unit); }
        internal void IgnoreForWaveClear(GameObject unit, bool ignored = true)
        { if (ignored) waveIgnored.Add(unit); else waveIgnored.Remove(unit); }
        internal static void Hold(string key) { Current?.holds.Add(key); }
        internal static void Release(string key) { Current?.holds.Remove(key); }
        private bool ScheduledClear => remainingBatches <= 0 && pendingGroups == 0 && pendingBatches == 0 && living.All(waveIgnored.Contains);
        private bool Clear => ScheduledClear && holds.Count == 0;

        internal static void NativeLiving(刷怪器 owner, List<GameObject> units, ref bool result)
        {
            var current = Current;
            if (result && current != null && current.spawner == owner && current.waveIgnored.Count > 0)
                result = units.Any(unit => unit != null && !current.waveIgnored.Contains(unit));
        }

        internal static bool Finish(刷怪器 owner, FinishKind kind, int jump = 0)
        {
            var current = Current;
            if (current == null || current.spawner != owner) return true;
            if (Plugin.Role == 2)
            {
                if (kind == FinishKind.Advance || kind == FinishKind.Jump) return true;
                return owner.当前波次 >= owner.波次;
            }
            if (!current.scheduledClearNotified && owner.当前波次 == owner.波次 - 1 && current.ScheduledClear)
            {
                current.scheduledClearNotified = true;
                current.AllScheduledEnemiesCleared?.Invoke();
            }
            if (kind == FinishKind.Advance || kind == FinishKind.Jump)
                return current.Clear;
            return owner.当前波次 >= owner.波次 && current.Clear;
        }

        internal static void Victory(刷怪器 owner)
        {
            var current = Current;
            if (current == null || current.spawner != owner || owner.当前波次 < owner.波次) return;
            current.victorious = true; current.ReleaseReward();
        }

        internal static void Advanced(刷怪器 owner)
        {
            var current = Current;
            if (current != null && current.spawner == owner && owner.当前波次 < owner.波次)
                owner.倒计时 = current.Definition.BetweenWavesSeconds;
        }

        internal static bool CaptureReward(词条_100028_Boss胜利判断 boss, 效果物体引用器 effects)
        {
            var current = Current;
            if (current == null || boss.GetComponent<ThemeEnemyActor>() == null) return true;
            var actor = boss.GetComponent<ThemeEnemyActor>();
            if (actor.UnitId != current.FinalRewardUnitId) return false;
            if (current.capturedBosses.Add(boss.GetInstanceID()))
            {
                current.reward = new BossReward(boss, effects?.预制体.Boss死亡反馈);
                current.ReleaseReward();
            }
            return false;
        }

        private void ReleaseReward()
        {
            if (!victorious || reward == null || rewardReleased) return;
            rewardReleased = true; StartCoroutine(reward.Apply());
        }

        private void OnDestroy()
        {
            if (events != null)
            {
                events.事件_敌人生成.RemoveListener(Generated); events.事件_Boss生成.RemoveListener(Generated);
                events.事件_单位死亡.RemoveListener(Died);
            }
            if (spawner != null) spawner.波次开始广播.RemoveListener(Started);
            if (data != null) Object.Destroy(data);
            if (Current == this) Current = null;
        }
    }

    public sealed class ThemeBattleEnemy : MonoBehaviour
    {
        internal ThemeBattleWaves Owner;
        private void OnDestroy() { if (Owner != null) Owner.Died(gameObject); }
    }

    [HarmonyPatch(typeof(刷怪器), "刷怪")]
    internal static class ThemeNativeBatch
    {
        private static void Prefix(刷怪器 __instance, 攻击波次数据模板.出怪单元 组次出怪数据) => ThemeBattleWaves.NativeGroup(__instance, 组次出怪数据);
    }

    [HarmonyPatch(typeof(刷怪器), "存活单位检测")]
    internal static class ThemeNativeLiving
    {
        private static void Postfix(刷怪器 __instance, List<GameObject> 存活单位, ref bool __result) =>
            ThemeBattleWaves.NativeLiving(__instance, 存活单位, ref __result);
    }

    [HarmonyPatch(typeof(刷怪器), "下一波次初始化", new Type[0])]
    internal static class ThemeWaveClear
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(刷怪器 __instance) => ThemeBattleWaves.Finish(__instance, FinishKind.Advance);
        private static void Postfix(刷怪器 __instance, bool __runOriginal) { if (__runOriginal) ThemeBattleWaves.Advanced(__instance); }
    }
    [HarmonyPatch(typeof(刷怪器), "下一波次初始化", new[] { typeof(int) })]
    internal static class ThemeWaveJump
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(刷怪器 __instance, int 跳转波次) => ThemeBattleWaves.Finish(__instance, FinishKind.Jump, 跳转波次);
        private static void Postfix(刷怪器 __instance, bool __runOriginal) { if (__runOriginal) ThemeBattleWaves.Advanced(__instance); }
    }
    [HarmonyPatch(typeof(刷怪器), "游戏胜利")]
    internal static class ThemeVictoryCompletion
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(刷怪器 __instance) => ThemeBattleWaves.Finish(__instance, FinishKind.Victory);
        private static void Postfix(刷怪器 __instance, bool __runOriginal) { if (__runOriginal) ThemeBattleWaves.Victory(__instance); }
    }
    [HarmonyPatch(typeof(刷怪器), "延迟结算")]
    internal static class ThemeRewardDelay
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(刷怪器 __instance) => ThemeBattleWaves.Finish(__instance, FinishKind.Delay);
    }
    [HarmonyPatch(typeof(刷怪器), "结算界面切换")]
    internal static class ThemeRewardExit
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(刷怪器 __instance) => ThemeBattleWaves.Finish(__instance, FinishKind.Settle);
    }
    [HarmonyPatch(typeof(词条_100028_Boss胜利判断), "死亡反馈")]
    internal static class ThemeBossReward
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(词条_100028_Boss胜利判断 __instance, 效果物体引用器 ___引用器) => ThemeBattleWaves.CaptureReward(__instance, ___引用器);
    }
}
