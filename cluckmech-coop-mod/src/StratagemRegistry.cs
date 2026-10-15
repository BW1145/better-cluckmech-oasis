using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace CluckmechCoop
{
    internal static class StratagemRegistry
    {
        private static GameObject templates;
        private static readonly Dictionary<int, GameObject> prefabs = new Dictionary<int, GameObject>();
        private static readonly Dictionary<int, 模块_基本属性> rankSamples = new Dictionary<int, 模块_基本属性>();
        private static readonly HashSet<Type> retained = new HashSet<Type> {
            typeof(模块_基本属性), typeof(模块_攻击属性), typeof(模块_攻击详细属性), typeof(模块_受伤),
            typeof(模块_死亡), typeof(模块_状态属性), typeof(模块_状态持续效果), typeof(模块_抗性属性),
            typeof(模块_移动属性), typeof(模块_移动), typeof(模块_词条_词缀管理器), typeof(模块_伤害数据统计),
            typeof(模块_生命显示), typeof(模块_加成管理器)
        };
        private static readonly FieldInfo assetId = AccessTools.Field(typeof(NetworkIdentity), "_assetId");
        internal static GameObject Prefab(int id) => prefabs.TryGetValue(id, out var prefab) ? prefab : null;
        internal static bool Enabled => ModuleSettings.InRun ? ModuleSettings.Extra.Stratagems : ModuleSettings.Preference(ModuleId.Stratagems);

        internal static void Install(dw单位库 database)
        {
            if (templates == null)
            {
                foreach (var entry in database.单位列表)
                    if (entry.id < 20000 && entry.单位阵营 == dw单位库.单位单元.阵营枚举.友方 && entry.是否商店内出现 && !rankSamples.ContainsKey(entry.单位等级))
                        rankSamples.Add(entry.单位等级, entry.单位预制体.GetComponent<模块_基本属性>());
                templates = new GameObject("StratagemTemplates"); templates.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(templates);
                foreach (var unit in StratagemCatalog.Units)
                    prefabs.Add(unit.Id, Make(database.获取单位(1), unit.Id, unit));
                prefabs.Add(20101, Make(database.获取单位(1), 20101, StratagemCatalog.Unit(20001)));
                prefabs.Add(20102, Make(database.获取单位(165).GetComponent<模块_基本属性>().衍生单位, 20102, StratagemCatalog.Unit(20007)));
            }
            foreach (var unit in StratagemCatalog.Units)
                Add(database, unit.Id, unit);
            Add(database, 20101, StratagemCatalog.Unit(20001));
            Add(database, 20102, StratagemCatalog.Unit(20007));
            RegisterNetworkPrefabs();
        }
        private static void Add(dw单位库 database, int id, StratagemDefinition unit)
        {
            if (database.单位是否存在(id)) return;
            database.单位列表.Add(new dw单位库.单位单元 {
                id = id, 单位阵营 = dw单位库.单位单元.阵营枚举.友方, 单位预制体 = prefabs[id],
                稀有度 = unit.Tier, 单位等级 = unit.Tier, 升级词缀上限 = unit.Cap, 默认解锁 = true,
                是否游戏内出现 = StratagemCatalog.IsUnit(id), 是否商店内出现 = StratagemCatalog.IsUnit(id),
                是否工匠内出现 = StratagemCatalog.IsUnit(id)
            });
        }
        private static GameObject Make(GameObject original, int id, StratagemDefinition unit)
        {
            if (original == null) throw new InvalidOperationException("Native stratagem template missing: " + id);
            // The inactive parent keeps native Awake/Start and network spawning out of template construction.
            var prefab = UnityEngine.Object.Instantiate(original, templates.transform);
            prefab.name = "Stratagem_" + id;
            foreach (var component in prefab.GetComponents<MonoBehaviour>())
                if (!(component is NetworkBehaviour) && !(component is NetworkIdentity) && !retained.Contains(component.GetType()))
                    UnityEngine.Object.DestroyImmediate(component);
            foreach (var animator in prefab.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach(var sound in prefab.GetComponentsInChildren<音效_播放音效>(true))UnityEngine.Object.DestroyImmediate(sound);
            foreach(var source in prefab.GetComponentsInChildren<AudioSource>(true))source.enabled=false;
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var collider in prefab.GetComponentsInChildren<Collider2D>(true))
                if (collider.gameObject != prefab) collider.enabled = false;
            var stats = prefab.GetComponent<模块_基本属性>();
            stats.ID = id; stats.母体id = -1; stats.背包id位置 = -1;
            stats.强化词条列表 = new List<int>();
            stats.单位定位列表 = new List<模块_基本属性.单位定位枚举> { 模块_基本属性.单位定位枚举.单位 };
            stats.单位分类 = (模块_基本属性.单位分类枚举)unit.Category;
            stats.单位类型 = id == 20102 ? 模块_基本属性.单位类型枚举.飞行单位 : 模块_基本属性.单位类型枚举.建筑;
            stats.初始阵营 = stats.当前阵营 = 阵营数据库.阵营.友方;
            stats.稀有度 = rankSamples[unit.Tier].稀有度;
            stats.花费 = unit.Power; stats.放置cd = unit.Cooldown; stats.初始放置cd = unit.InitialCooldown;
            stats.是否初始放置有cd = true;
            stats.生命值 = id == 20102 ? 500 : unit.Health; stats.当前生命值 = stats.当前生命值上限 = 0;
            stats.生命值倍率 = 1; stats.变化生命值 = stats.变化抗性 = stats.变化格挡值 = 0;
            stats.格挡值 = unit.Block; stats.抗性 = 0; stats.是否无敌 = stats.变化是否无敌 = false;
            stats.是否受到全局加成影响 = id != 20101;
            var movement = prefab.GetComponent<模块_移动属性>() ?? prefab.AddComponent<模块_移动属性>();
            movement.移动速度 = id == 20102 ? 5 : 0;
            movement.变化移动速度 = movement.变化百分比移动速度 = 0;
            if (prefab.GetComponent<模块_移动>() == null) prefab.AddComponent<模块_移动>();
            if (unit.Kind == StratagemKind.Rover)
            {
                var body = prefab.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
                foreach (var collider in prefab.GetComponents<Collider2D>()) collider.isTrigger = true;
            }
            stats.启用建筑体积 = id < 20100 && unit.Kind != StratagemKind.Rover && unit.Health > 1;
            stats.建筑体积 = new Vector2(1,1);
            prefab.GetComponent<BoxCollider2D>().size = stats.启用建筑体积 ? stats.建筑体积-new Vector2(.35f,.35f) : Vector2.zero;
            stats.图标 = stats.名片 = StratagemAssets.Icon(unit.Kind, id);
            stats.预放置图片 = id < 20100 ? StratagemAssets.Preview(id) : stats.图标;
            var attack = prefab.GetComponent<模块_攻击属性>();
            attack.攻击力 = unit.Damage; attack.攻击速度 = unit.Rate; attack.攻击范围 = unit.Range;
            attack.变化攻击力 = attack.变化百分比攻击力 = attack.变化攻击速度 = attack.变化百分比攻击速度 = 0;
            attack.攻击力加成 = attack.攻击速度加成 = attack.百分比攻击力加成 = attack.百分比攻击速度加成 = 0;
            attack.当前造成状态 = new List<攻击类型与状态数据库.状态枚举>();
            var detailed = prefab.GetComponent<模块_攻击详细属性>();
            detailed.暴击率 = .1f; detailed.暴击倍率 = 1;
            detailed.变化暴击率 = detailed.变化暴击倍率 = 0;
            detailed.状态加成列表 = new 模块_攻击详细属性.状态属性单元[18];
            for (int i = 0; i < detailed.状态加成列表.Length; i++) detailed.状态加成列表[i] = new 模块_攻击详细属性.状态属性单元();
            var death = prefab.GetComponent<模块_死亡>();
            death.死亡召唤列表 = new List<GameObject>(); death.死亡事件 = new UnityEvent();
            var manager = prefab.GetComponent<模块_词条_词缀管理器>();
            manager.当前词条列表 = new List<世界类.词条数据包>();
            if (id >= 20100)
            {
                stats.单位定位列表.Add(模块_基本属性.单位定位枚举.召唤物);
                stats.母体id = unit.Id;
                if (id == 20101) stats.单位类型 = 模块_基本属性.单位类型枚举.陷阱;
            }
            if (unit.Health == 1 || id == 20101)
            {
                stats.是否无敌 = true;
                foreach (var collider in prefab.GetComponents<Collider2D>()) collider.enabled = false;
            }
            prefab.AddComponent<StratagemUnit>();
            var identity = prefab.GetComponent<NetworkIdentity>();
            if (identity != null) { assetId.SetValue(identity, (uint)(0xBC050000 + id)); identity.sceneId = 0; }
            return prefab;
        }
        internal static void RegisterNetworkPrefabs()
        {
            foreach (var prefab in prefabs.Values)
                if (prefab.GetComponent<NetworkIdentity>() != null) NetworkClient.RegisterPrefab(prefab);
        }
        internal static string Name(StratagemDefinition definition)
        {
            // Native localized names carry the rank's rich-text color, including language changes.
            string sample = rankSamples[definition.Tier].名称();
            int begin = sample.IndexOf("<color=", StringComparison.OrdinalIgnoreCase);
            int end = begin < 0 ? -1 : sample.IndexOf('>',begin);
            return end < 0 ? definition.Name : sample.Substring(begin,end-begin+1)+definition.Name+"</color>";
        }
        internal static void Install(cz词缀库 database)
        {
            foreach (var affix in StratagemCatalog.Affixes)
            {
                if (database.词缀列表.Exists(x => x.id == affix.Id)) continue;
                var item = ScriptableObject.CreateInstance<词条数据模板>();
                item.name = affix.Name; item.id = affix.Id; item.是否唯一 = true; item.禁止继承 = false;
                item.图标 = StratagemAssets.AffixIcon(affix.Id);
                item.稀有度 = (常量.枚举.词条稀有度)affix.Quality;
                database.词缀列表.Add(item);
            }
        }
        internal static void Install(cz升级词缀库 database)
        {
            foreach (var unit in StratagemCatalog.Units)
            {
                if (database.升级方案列表.Exists(x => x.炮台id == unit.Id)) continue;
                var pool = new List<int>(unit.NativeAffixes);
                pool.AddRange(StratagemCatalog.UpgradePool(unit.Id));
                database.升级方案列表.Add(new cz升级词缀库.升级方案单元 { 炮台id = unit.Id, 升级词缀id = pool });
            }
        }
    }
    [HarmonyPatch(typeof(dw单位库), "OnEnable")]
    internal static class RegisterStratagemUnits { private static void Prefix(dw单位库 __instance) => StratagemRegistry.Install(__instance); }
    [HarmonyPatch(typeof(NetworkClient),"PrepareToSpawnSceneObjects")]
    internal static class RegisterStratagemNetworkPrefabs { private static void Postfix() => StratagemRegistry.RegisterNetworkPrefabs(); }
    [HarmonyPatch(typeof(模块_移动), "Start")]
    internal static class StratagemMovement
    {
        private static bool Prefix(模块_移动 __instance)
        {
            int id = __instance.GetComponent<模块_基本属性>()?.ID ?? 0;
            if (!StratagemCatalog.IsUnit(id) && id != 20101 && id != 20102) return true;
            AccessTools.Method(typeof(模块_移动),"初始化").Invoke(__instance,null);
            var agent = __instance.GetComponent("PolyNavAgent") as Behaviour;
            if (agent != null) agent.enabled = false;
            var body = __instance.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            return false;
        }
    }
    [HarmonyPatch(typeof(cz词缀库), "Start")]
    internal static class RegisterStratagemAffixes { private static void Prefix(cz词缀库 __instance) => StratagemRegistry.Install(__instance); }
    [HarmonyPatch(typeof(cz升级词缀库), "Start")]
    internal static class RegisterStratagemPools { private static void Prefix(cz升级词缀库 __instance) => StratagemRegistry.Install(__instance); }
    [HarmonyPatch(typeof(dw单位库), "是否抽取")]
    internal static class StratagemAvailability
    {
        private static bool Prefix(int id, ref bool __result)
        {
            if (id == 20101 || id == 20102 || StratagemCatalog.IsUnit(id) && !StratagemRegistry.Enabled)
            { __result = false; return false; }
            return true;
        }
    }
    [HarmonyPatch(typeof(模块_基本属性), "名称")]
    internal static class StratagemName
    {
        private static bool Prefix(模块_基本属性 __instance, ref string __result)
        {
            var definition = StratagemCatalog.Unit(__instance.ID);
            if (__instance.ID == 20101) { __result = "MD-6地雷"; return false; }
            if (__instance.ID == 20102) { __result = "激光护卫犬无人机"; return false; }
            if (definition == null) return true;
            __result = StratagemRegistry.Name(definition); return false;
        }
    }
    [HarmonyPatch(typeof(cz词缀库), "获取词缀名称")]
    internal static class StratagemAffixName
    {
        private static bool Prefix(int 词条id, ref string __result)
        { var a = StratagemCatalog.Affix(词条id); if (a == null) return true; __result = a.Name; return false; }
    }
    [HarmonyPatch(typeof(cz词缀库), "获取词缀描述")]
    internal static class StratagemAffixText
    {
        private static bool Prefix(int 词条id, ref string __result)
        { var a = StratagemCatalog.Affix(词条id); if (a == null) return true; __result = a.Description; return false; }
    }
    [HarmonyPatch(typeof(词条数据模板), "获取词缀名称")]
    internal static class StratagemAffixDataName
    {
        private static bool Prefix(词条数据模板 __instance, ref string __result)
        { var a = StratagemCatalog.Affix(__instance.id); if (a == null) return true; __result = a.Name; return false; }
    }
    [HarmonyPatch(typeof(词条数据模板), "获取词缀描述")]
    internal static class StratagemAffixDataText
    {
        private static bool Prefix(词条数据模板 __instance, ref string __result)
        { var a = StratagemCatalog.Affix(__instance.id); if (a == null) return true; __result = a.Description; return false; }
    }
    [HarmonyPatch(typeof(奖励结算脚本), "随机词条结果运算")]
    internal static class FilterStratagemUpgrades
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = AccessTools.Method(typeof(cz升级词缀库),"获取单位词缀列表");
            var replacement = AccessTools.Method(typeof(FilterStratagemUpgrades),nameof(Candidates));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(original)) { instruction.opcode = System.Reflection.Emit.OpCodes.Call; instruction.operand = replacement; }
                yield return instruction;
            }
        }
        internal static bool Candidates(cz升级词缀库 library,int unit,out List<int> pool)
        {
            bool found = library.获取单位词缀列表(unit,out pool);
            if (!found || !StratagemCatalog.IsUnit(unit) || Plugin.Summary == null) return found;
            int selected = Plugin.Summary.当前选中的背包id;
            var bag = Plugin.Summary.当前拥有炮台;
            if (selected < 0 || selected >= bag.Count || bag[selected].炮台id != unit) return found;
            var owned = bag[selected].强化词条;
            pool.RemoveAll(id => !StratagemCatalog.CanChoose(id, owned));
            return found;
        }
    }
}
