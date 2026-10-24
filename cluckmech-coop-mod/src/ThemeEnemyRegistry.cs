using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace CluckmechCoop
{
    internal static class ThemeEnemyRegistry
    {
        private static GameObject templates, bossTemplate;
        private static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<int, ThemeEnemyStats> definitions = new Dictionary<int, ThemeEnemyStats>();
        private static readonly FieldInfo assetId = AccessTools.Field(typeof(NetworkIdentity), "_assetId");
        private static readonly HashSet<Type> retained = new HashSet<Type> {
            typeof(模块_基本属性), typeof(模块_攻击属性), typeof(模块_攻击详细属性), typeof(模块_攻击),
            typeof(模块_受伤), typeof(模块_死亡), typeof(模块_状态属性), typeof(模块_状态持续效果),
            typeof(模块_抗性属性), typeof(模块_移动属性), typeof(模块_移动), typeof(模块_仇恨),
            typeof(模块_词条_词缀管理器), typeof(模块_伤害数据统计), typeof(模块_生命显示),
            typeof(模块_加成管理器), typeof(模块_难度等级), typeof(模块_Boss多难度), typeof(模块_BOSS生命和狂暴),
            typeof(PolyNav.PolyNavAgent), typeof(词条_6_死亡掉落)
        };

        internal static GameObject Prefab(string id) => prefabs.TryGetValue(id, out var prefab) ? prefab : null;
        internal static ThemeEnemyStats Definition(int id) => definitions.TryGetValue(id, out var value) ? value : null;
        internal static ThemeEnemyStats Find(string id)
        {
            foreach (var values in Catalogs) foreach (var value in values) if (value.Id == id) return value;
            return null;
        }
        private static IEnumerable<ThemeEnemyStats[]> Catalogs
        {
            get { yield return ThemeCastleData.Enemies; yield return ThemeEldritchData.Enemies; yield return ThemeDeepSeaData.Enemies; }
        }
        internal static bool IsBoss(string id) => id == ThemeCastleData.Count || id == ThemeEldritchData.BanquetEater ||
            id == ThemeEldritchData.HangingCount || id == ThemeEldritchData.BloodMother || id == ThemeEldritchData.WhiteVisitor ||
            id == ThemeDeepSeaData.CrownLily || id == ThemeDeepSeaData.ValveAnemone || id == ThemeDeepSeaData.Octopus;
        internal static bool IsLeader(string id) => id == ThemeCastleData.Leader || id == ThemeEldritchData.ShadowLeader;
        internal static bool IsCrossbow(string id) => id == ThemeCastleData.Crossbow || id == ThemeEldritchData.ShadowCrossbow;
        internal static bool IsRanged(string id) => IsCrossbow(id) || id == ThemeDeepSeaData.Anemone ||
            id == ThemeDeepSeaData.Lily || id == ThemeDeepSeaData.CrownLily || id == ThemeDeepSeaData.ValveAnemone;
        internal static int VolleyCount(string id) => IsCrossbow(id) ? ThemeCastleData.CrossbowArrows :
            id == ThemeDeepSeaData.Anemone ? ThemeDeepSeaData.AnemoneProjectiles : 1;

        internal static void Install(dw单位库 database)
        {
            if (templates == null)
            {
                templates = new GameObject("ThemeEnemyTemplates");
                templates.SetActive(false);
                Object.DontDestroyOnLoad(templates);
            }
            int index = 0;
            foreach (var catalog in Catalogs)
            foreach (var definition in catalog)
            {
                int id = 27001 + index++;
                if (prefabs.ContainsKey(definition.Id)) continue;
                var original = IsBoss(definition.Id) || IsLeader(definition.Id) ? BossTemplate(database) :
                    database.获取单位(IsRanged(definition.Id) ? 1005 :
                        definition.Id == ThemeCastleData.WineBat || definition.Id == ThemeCastleData.BossWineBat ? 1008 : 1007);
                if (original == null) continue;
                var projectileTemplate = database.获取单位(IsRanged(definition.Id) ? 1005 : 1007)
                    ?.GetComponent<模块_攻击属性>()?.子弹;
                var prefab = Make(original, definition, id, projectileTemplate);
                prefabs.Add(definition.Id, prefab);
                definitions.Add(id, definition);
            }
            foreach (var pair in prefabs)
            {
                var stats = pair.Value.GetComponent<模块_基本属性>();
                if (database.单位是否存在(stats.ID)) continue;
                database.单位列表.Add(new dw单位库.单位单元 {
                    id = stats.ID, 单位预制体 = pair.Value, 单位阵营 = dw单位库.单位单元.阵营枚举.敌方,
                    是否游戏内出现 = false, 是否商店内出现 = false, 是否工匠内出现 = false, 默认解锁 = true
                });
            }
            RegisterNetwork();
        }

        private static GameObject BossTemplate(dw单位库 database)
        {
            if (bossTemplate != null) return bossTemplate;
            // Native stone-cactus boss: a fixed identity keeps Mirror behaviour ordering identical on both peers.
            var registered = database.获取单位(1006);
            if (ValidBossTemplate(registered)) return bossTemplate = registered;
            foreach (var boss in Resources.FindObjectsOfTypeAll<模块_BOSS生命和狂暴>())
            {
                var health = boss.GetComponent<模块_基本属性>();
                if (health == null || health.ID != 1006 || !ValidBossTemplate(boss.gameObject)) continue;
                if (bossTemplate == null || string.CompareOrdinal(boss.gameObject.name, bossTemplate.name) < 0)
                    bossTemplate = boss.gameObject;
            }
            return bossTemplate;
        }

        private static bool ValidBossTemplate(GameObject candidate)
        {
            if (candidate == null) return false;
            var health = candidate.GetComponent<模块_基本属性>();
            var boss = candidate.GetComponent<模块_BOSS生命和狂暴>();
            return health != null && !health.是否为实例() && boss != null && boss.BOSS狂暴条 != null &&
                boss.BOSS血条UI != null && candidate.GetComponent<模块_移动>() != null &&
                candidate.GetComponent<模块_攻击>() != null && candidate.GetComponent<模块_受伤>() != null &&
                candidate.GetComponent<模块_死亡>() != null && candidate.GetComponent<NetworkIdentity>() != null &&
                candidate.GetComponent<词条_100028_Boss胜利判断>() != null;
        }

        private static GameObject Make(GameObject original, ThemeEnemyStats definition, int id, GameObject projectile)
        {
            bool boss = IsBoss(definition.Id);
            var prefab = Object.Instantiate(original, templates.transform);
            prefab.name = "ThemeEnemy_" + definition.Id;
            prefab.transform.localScale = Vector3.one;
            foreach (var component in prefab.GetComponents<MonoBehaviour>())
                if (!(component is NetworkBehaviour) && !retained.Contains(component.GetType()) &&
                    !(component is 词条_100028_Boss胜利判断 && (boss || IsLeader(definition.Id))))
                    Object.DestroyImmediate(component);
            foreach (var animator in prefab.GetComponentsInChildren<Animator>(true))
                if (animator.GetComponentInParent<Canvas>(true) == null) animator.enabled = false;
            foreach (var renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;
            foreach (var audio in prefab.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
            foreach (var sound in prefab.GetComponentsInChildren<音效_播放音效>(true)) Object.DestroyImmediate(sound);
            foreach (var collider in prefab.GetComponentsInChildren<Collider2D>(true))
                if (collider.gameObject != prefab) collider.enabled = false;
            float radius = IsBoss(definition.Id) ? 1.1f : IsLeader(definition.Id) ? 0.7f : 0.4f;
            foreach (var collider in prefab.GetComponents<Collider2D>())
            {
                if (collider is CircleCollider2D circle) { circle.radius = radius; circle.offset = Vector2.zero; }
                else if (collider is BoxCollider2D box) { box.size = Vector2.one * radius * 2; box.offset = Vector2.zero; }
                else if (collider is CapsuleCollider2D capsule) { capsule.size = Vector2.one * radius * 2; capsule.offset = Vector2.zero; }
            }
            ConfigureHurtShape(prefab, definition.Id);
            var agent = prefab.GetComponent<PolyNav.PolyNavAgent>();
            if (agent != null) { agent.avoidRadius = radius * 0.65f; agent.rotateTransform = false; agent.stoppingDistance = 0.1f; }

            var health = prefab.GetComponent<模块_基本属性>();
            health.ID = id; health.母体id = health.背包id位置 = -1;
            health.强化词条列表 = new List<int>();
            health.单位定位列表 = new List<模块_基本属性.单位定位枚举> { 模块_基本属性.单位定位枚举.单位 };
            health.初始阵营 = health.当前阵营 = 阵营数据库.阵营.敌方;
            health.单位类型 = definition.Id == ThemeCastleData.WineBat || definition.Id == ThemeCastleData.BossWineBat ?
                模块_基本属性.单位类型枚举.飞行单位 : 模块_基本属性.单位类型枚举.地面单位;
            health.生命值 = boss ? definition.FinalHealth - 10000 : definition.FinalHealth / 9;
            health.生命值倍率 = 1; health.变化生命值 = 0;
            health.当前生命值 = health.当前生命值上限 = 0;
            health.格挡值 = boss ? definition.FinalBlock : definition.FinalBlock - 5;
            health.抗性 = definition.FinalResistance;
            health.变化格挡值 = health.变化抗性 = health.格挡值加成 = health.抗性加成 = 0;
            health.生命再生 = health.变化生命再生 = health.反伤值 = health.变化反伤值 = 0;
            health.受到治疗加成 = 0;
            health.是否无敌 = health.变化是否无敌 = health.当前是否无敌 = false;
            health.是否受到全局加成影响 = false; health.是否受到治疗 = true;
            health.免疫击退 = false; health.启用建筑体积 = false;
            health.图标 = health.名片 = ThemeAssets.Named(definition.Id + ".idle.0");
            var bonuses = prefab.GetComponent<模块_加成管理器>();
            if (bonuses != null) bonuses.状态加成列表 = new List<模块_加成管理器.状态加成类>();
            var attack = prefab.GetComponent<模块_攻击属性>();
            attack.攻击力 = definition.FinalDamage / (boss ? 1.35f : 9);
            attack.攻击速度 = 1 / definition.AttackIntervalSeconds; attack.攻击范围 = definition.AttackRange;
            attack.攻击前置时间 = definition.AttackWindupSeconds; attack.攻击延迟 = 0;
            attack.当前攻击倒计时 = attack.变化攻击力 = attack.变化百分比攻击力 = 0;
            attack.变化攻击速度 = attack.变化百分比攻击速度 = attack.攻击力加成 = attack.百分比攻击力加成 = 0;
            attack.攻击速度加成 = attack.百分比攻击速度加成 = attack.变化攻击范围 = attack.变化百分比攻击范围 = 0;
            attack.当前造成状态 = new List<攻击类型与状态数据库.状态枚举>();
            attack.子弹 = projectile;
            attack.是否制导 = attack.是否爆炸 = attack.是否击退 = false;
            attack.穿透数量 = 0;
            attack.是否被动攻击 = false; attack.是否能攻击 = attack.变化是否能攻击 = attack.当前是否能攻击 = true;
            attack.额外弹丸数量 = 0; attack.攻击偏转角度 = 0;
            var detailed = prefab.GetComponent<模块_攻击详细属性>();
            detailed.暴击率 = detailed.变化暴击率 = 0; detailed.暴击倍率 = 1;
            var movement = prefab.GetComponent<模块_移动属性>();
            movement.移动速度 = definition.MoveSpeed;
            movement.变化移动速度 = movement.变化百分比移动速度 = 0;
            movement.是否跟随 = false; movement.跟随目标 = movement.前进目标 = movement.前进副目标 = null;
            prefab.GetComponent<模块_词条_词缀管理器>().当前词条列表 = new List<世界类.词条数据包>();
            var death = prefab.GetComponent<模块_死亡>();
            death.死亡召唤列表 = new List<GameObject>(); death.死亡事件 = new UnityEvent(); death.延迟死亡时间 = 1;
            var difficulty = prefab.GetComponent<模块_难度等级>();
            if (boss) { if (difficulty != null) Object.DestroyImmediate(difficulty); }
            else
            {
                if (difficulty == null) difficulty = prefab.AddComponent<模块_难度等级>();
                difficulty.设定等级 = 24; difficulty.难度等级加成 = 0;
                difficulty.每级生命值等级 = difficulty.每级攻击力加成 = 0.25f;
            }
            if (!boss)
            {
                var scaling = prefab.GetComponent<模块_Boss多难度>();
                if (scaling != null) Object.DestroyImmediate(scaling);
                var rage = prefab.GetComponent<模块_BOSS生命和狂暴>();
                if (rage != null && !IsLeader(definition.Id)) Object.DestroyImmediate(rage);
            }
            var victory = prefab.GetComponent<词条_100028_Boss胜利判断>();
            if (victory != null)
            {
                victory.是否死亡 = victory.是否复数生物 = victory.是否最终BOSS = false;
                victory.子物体 = new List<GameObject>();
                victory.是否出天赋 = true; victory.是否出专属天赋 = false;
            }
            var nativeAttack = prefab.GetComponent<模块_攻击>();
            nativeAttack.enabled = false;
            nativeAttack.炮口位置 = nativeAttack.炮筒位置 = prefab.transform;
            var actor = prefab.AddComponent<ThemeEnemyActor>();
            actor.UnitId = definition.Id;
            prefab.AddComponent<ThemeEnemyVisual>();
            if (definition.Id == ThemeCastleData.Count || definition.Id == ThemeEldritchData.HangingCount) prefab.AddComponent<ThemeCountActor>();
            if (definition.Id == ThemeDeepSeaData.Lily || definition.Id == ThemeDeepSeaData.CrownLily)
                prefab.AddComponent<ThemeLilyAura>();
            foreach (var protectedId in ThemeDeepSeaData.LilyProtectedUnits)
                if (definition.Id == protectedId) { prefab.AddComponent<ThemeLilyProtection>(); break; }
            if (definition.Id == ThemeDeepSeaData.ValveAnemone) prefab.AddComponent<ThemeValveAnemoneActor>();
            if (definition.Id == ThemeDeepSeaData.Octopus) prefab.AddComponent<ThemeOctopusActor>();
            if (definition.Id == ThemeDeepSeaData.Octopus)
            {
                foreach (var collider in prefab.GetComponents<Collider2D>()) Object.DestroyImmediate(collider);
                var mouth = prefab.AddComponent<BoxCollider2D>(); mouth.isTrigger = true;
                mouth.size = new Vector2(8, 3); mouth.offset = new Vector2(0, -.5f);
            }
            if (definition.Id == ThemeDeepSeaData.SlamArm || definition.Id == ThemeDeepSeaData.BindArm)
            {
                prefab.AddComponent<ThemeOctopusArm>();
                foreach (var collider in prefab.GetComponents<Collider2D>()) Object.DestroyImmediate(collider);
                var bodyShape = prefab.AddComponent<BoxCollider2D>(); bodyShape.isTrigger = true;
                bodyShape.size = new Vector2(3, 4); bodyShape.offset = new Vector2(0, 2);
            }
            var identity = prefab.GetComponent<NetworkIdentity>();
            assetId.SetValue(identity, (uint)(0xBC080000 + id)); identity.sceneId = 0;
            return prefab;
        }

        private static void ConfigureHurtShape(GameObject prefab, string id)
        {
            Vector2 size;
            switch (id)
            {
                case ThemeCastleData.Count: size = new Vector2(1.45f, 2.75f); break;
                case ThemeEldritchData.HangingCount: size = new Vector2(1.45f, 2.9f); break;
                case ThemeCastleData.Leader: case ThemeEldritchData.ShadowLeader: size = new Vector2(1.7f, 2.3f); break;
                case ThemeCastleData.Coffin: size = new Vector2(1.65f, 2.1f); break;
                case ThemeCastleData.WineBat: case ThemeCastleData.BossWineBat: size = new Vector2(1, 1.4f); break;
                case ThemeEldritchData.BanquetEater: size = new Vector2(2.8f, 1.8f); break;
                case ThemeEldritchData.FleshKnot: size = new Vector2(1.3f, 1.6f); break;
                case ThemeDeepSeaData.CrownLily: case ThemeDeepSeaData.ValveAnemone: size = new Vector2(2.5f, 2.7f); break;
                case ThemeEldritchData.BloodMother: size = new Vector2(4.3f, 3.4f); break;
                case ThemeEldritchData.BloodSac: size = new Vector2(.9f, .95f); break;
                case ThemeCastleData.Crossbow: case ThemeEldritchData.ShadowCrossbow: size = new Vector2(.95f, 1.65f); break;
                default: size = new Vector2(1.35f, 1.65f); break;
            }
            BoxCollider2D body = null;
            foreach (var collider in prefab.GetComponents<Collider2D>())
            {
                if (!collider.isTrigger) continue;
                if (body == null && collider is BoxCollider2D box) body = box;
                else collider.enabled = false;
            }
            if (body == null) body = prefab.AddComponent<BoxCollider2D>();
            body.isTrigger = true; body.size = size; body.offset = new Vector2(0, size.y * .5f); body.enabled = true;
        }

        internal static void RegisterNetwork()
        { foreach (var prefab in prefabs.Values) NetworkClient.RegisterPrefab(prefab); }
    }

    [HarmonyPatch(typeof(NetworkClient), "PrepareToSpawnSceneObjects")]
    internal static class ThemeEnemyNetworkRegistration
    { private static void Postfix() => ThemeEnemyRegistry.RegisterNetwork(); }

    [HarmonyPatch(typeof(模块_基本属性), "名称")]
    internal static class ThemeEnemyName
    {
        private static bool Prefix(模块_基本属性 __instance, ref string __result)
        {
            var definition = ThemeEnemyRegistry.Definition(__instance.ID);
            if (definition == null) return true;
            __result = definition.Name; return false;
        }
    }
}
