using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PolyNav;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CluckmechCoop
{
    public sealed class ThemeBattlePillar : MonoBehaviour
    {
        internal Vector2 Center => transform.position;
        internal float Radius;
        internal Vector2 HalfSize => Vector2.one * Radius;
        internal bool Broken { get; set; }
    }

    public sealed class ThemeBattleScene : MonoBehaviour
    {
        internal static ThemeBattleScene Current { get; private set; }
        internal ThemeMapDefinition Definition { get; private set; }
        internal int Theme { get; private set; }
        internal bool Eldritch { get; private set; }
        internal int BattleIndex { get; private set; }
        internal Vector2 Origin => Vector2.zero;
        internal readonly List<ThemeBattlePillar> Pillars = new List<ThemeBattlePillar>();
        internal readonly Dictionary<string, Transform> Entrances = new Dictionary<string, Transform>();
        private readonly List<Collider2D> shotBlockers = new List<Collider2D>();
        private readonly Dictionary<string, GameObject> facilities = new Dictionary<string, GameObject>();
        private 导航管理器 navigation;
        private int handle;

        internal static void Install(Scene scene)
        {
            if (Current != null && Current.handle == scene.handle) return;
            if (!ThemeBranchRuntime.InBranch) return;
            int step = ThemeBranchRuntime.CurrentStep;
            int battle = step == 1 ? 0 : step == 3 ? 1 : step == 5 ? 2 : -1;
            if (battle < 0) return;
            var spawner = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<刷怪器>(true)).FirstOrDefault();
            if (spawner == null) return;
            bool eldritch = ThemeBranchRuntime.State.Eldritch[battle];
            int theme = ThemeBranchRuntime.State.Theme;
            var definition = theme == 1 ? eldritch ? battle == 0 ? ThemeEldritchData.ShadowCourtyard : battle == 1 ? ThemeEldritchData.Banquet : ThemeEldritchData.HangingHall :
                battle == 0 ? ThemeCastleData.Courtyard : battle == 1 ? ThemeCastleData.Banquet : ThemeCastleData.CountHall :
                eldritch ? battle == 0 ? ThemeEldritchData.BloodGallery : battle == 1 ? ThemeEldritchData.PumpLung : ThemeEldritchData.WhiteDock :
                battle == 0 ? ThemeDeepSeaData.Gallery : battle == 1 ? ThemeDeepSeaData.PumpRoom : ThemeDeepSeaData.OctopusDock;
            ThemeAssets.CaptureSorting(Plugin.Units?.获取单位(1007)?.GetComponent<SpriteRenderer>() ??
                scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>(true)).FirstOrDefault());
            HideNativeTerrain(scene);
            var root = new GameObject("ThemeBattlefield_" + definition.Id);
            SceneManager.MoveGameObjectToScene(root, scene);
            var current = root.AddComponent<ThemeBattleScene>(); Current = current;
            current.Definition = definition; current.Theme = theme; current.BattleIndex = battle; current.handle = scene.handle;
            current.Eldritch = eldritch;
            if (NativeUi.Font == null)
            {
                var canvas = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).FirstOrDefault();
                if (canvas != null) NativeUi.Capture(canvas.transform);
            }
            current.Build(spawner);
        }

        private static void HideNativeTerrain(Scene scene)
        {
            var keep = new HashSet<string> { "世界", "数据库", "总结", "奖励列表", "导航管理器", "出生点", "UI", "EventSystem", "预放置炮台显示" };
            foreach (var root in scene.GetRootGameObjects())
            {
                if (keep.Contains(root.name) || root.GetComponentInChildren<世界对齐>(true) != null) continue;
                root.SetActive(false);
            }
            foreach (var detail in Object.FindObjectsOfType<模块_随机场景细节>()) detail.enabled = false;
            foreach (var fixedBase in Object.FindObjectsOfType<基地车出生点>()) fixedBase.enabled = false;
        }

        private void Build(刷怪器 spawner)
        {
            ThemeEnemyRegistry.Install(Plugin.Units ?? Object.FindObjectOfType<dw单位库>());
            gameObject.AddComponent<ThemeNativeEffects>();
            navigation = Object.FindObjectOfType<导航管理器>();
            if (navigation == null) throw new InvalidOperationException("Theme battlefield needs native navigation");
            navigation.地面导航地图.obstaclesMask = (1 << 10) | (1 << 13) | (1 << 14) | (1 << 16);
            navigation.空中导航地图.obstaclesMask = 1 << 10;
            var perimeter = ThemeBattleGeometry.Perimeter(Definition.Floors);
            if (Theme == 2)
            {
                var background = ThemeAssets.Renderer(transform, "deepsea.background", 80, new Vector2(0, 18), true);
                background.sortingOrder = -200;
            }
            BuildFloors();
            for (int i = 0; i < perimeter.Length; i++)
                Boundary(World(perimeter[i]), World(perimeter[(i + 1) % perimeter.Length]));
            foreach (var obstacle in Definition.Obstacles) Obstacle(obstacle);
            BuildFacilities();
            ConfigureMap(navigation.地面导航地图, perimeter, (1 << 10) | (1 << 13) | (1 << 14) | (1 << 16));
            ConfigureMap(navigation.空中导航地图, perimeter, 1 << 10);
            foreach (var entrance in Definition.Entrances)
            {
                var marker = new GameObject("ThemeEntrance_" + entrance.Id);
                marker.transform.SetParent(transform, false); marker.transform.position = World(entrance.Center);
                Entrances.Add(entrance.Id, marker.transform);
            }
            var birth = Object.FindObjectOfType<出生点>();
            if (birth != null) birth.transform.position = World(Definition.BaseHints[0].Example) + Vector2.down;
            ThemeBattleWaves.Attach(this, spawner);
            if (Theme == 1 && BattleIndex == 1) ThemeCastleChandelier.Install(this);
            if (Theme == 2 && BattleIndex == 1 && !Eldritch) ThemeDeepSeaValve.Install(this);
            Plugin.Log("[Theme] Installed " + Definition.Id + (Eldritch ? " (eldritch)" : ""));
        }

        private static void ConfigureMap(PolyNavMap map, ThemePoint[] perimeter, int obstacleMask)
        {
            var points = perimeter.Select(p => new Vector2(p.X, p.Y)).ToArray();
            points = PolyNavMap.InflatePolygon(ref points, Mathf.Max(.01f, map.radiusOffset));
            map.obstaclesMask = obstacleMask;
            map.SetCustomMap(new PolyNavMap.PolyMap(new PolyNavMap.Polygon(points)));
            map.GenerateMap(false);
        }

        private void BuildFloors()
        {
            foreach (var floor in Definition.Floors)
                for (float x = floor.MinX; x < floor.MaxX; x += 4)
                    for (float y = floor.MinY; y < floor.MaxY; y += 4)
                        ArtRect(Theme == 1 ? "castle.floor" : "deepsea.floor", new ThemeRect(x, y, Mathf.Min(x + 4, floor.MaxX), Mathf.Min(y + 4, floor.MaxY)), true);
            if (Theme == 2) { BuildDeepSeaDetails(); return; }
            if (BattleIndex == 2)
            {
                for (float y = 2; y < 27; y += 5)
                    ArtRect("castle.carpet", new ThemeRect(-3, y, 3, y + 5), true);
                for (int side = -1; side <= 1; side += 2)
                    for (int y = 5; y <= 25; y += 5)
                    {
                        ThemeAssets.Renderer(transform, "castle.window", 2.8f, new Vector2(side * 15.6f, y));
                        ThemeAssets.Renderer(transform, "castle.lamp", .7f, new Vector2(side * 14.3f, y - 1));
                    }
            }
            if (BattleIndex == 0)
            {
                ThemeAssets.Renderer(transform, "castle.gate", 8, new Vector2(0, 27.5f));
                foreach (var side in new[] { -1, 1 })
                {
                    ThemeAssets.Renderer(transform, "castle.rubble", 2, new Vector2(side * 10, 6));
                    ThemeAssets.Renderer(transform, "castle.banner", 1.5f, new Vector2(side * 11, 24));
                }
            }
        }

        private void ArtRect(string key, ThemeRect rect, bool ground, int? sorting = null)
        {
            var renderer = ThemeAssets.Renderer(transform, key, rect.Width, new Vector2(rect.Center.X, rect.Center.Y), ground);
            if (renderer == null || renderer.sprite == null || renderer.sprite.bounds.size.y <= 0) return;
            var scale = renderer.transform.localScale;
            scale.y = rect.Height / renderer.sprite.bounds.size.y; renderer.transform.localScale = scale;
            var pivot = renderer.sprite.pivot;
            renderer.transform.localPosition = new Vector3(rect.MinX + rect.Width * pivot.x / renderer.sprite.rect.width,
                rect.MinY + rect.Height * pivot.y / renderer.sprite.rect.height,
                StratagemAssets.Z(rect.MinY) + (ground ? .6f : 0));
            if (key.EndsWith(".floor", StringComparison.Ordinal)) renderer.sortingOrder = -100;
            else if (key == "castle.carpet") renderer.sortingOrder = -9;
            else if (key == "castle.flowerbed") renderer.sortingOrder = -1;
            else if (key == "deepsea.well") renderer.sortingOrder = -9;
            if (sorting.HasValue) renderer.sortingOrder = sorting.Value;
        }

        private void Boundary(Vector2 a, Vector2 b)
        {
            var direction = b - a; var outward = new Vector2(direction.y, -direction.x).normalized;
            var wall = new GameObject("ThemeBoundary"); wall.layer = Definition.BoundaryBlocksShots ? 10 : 16;
            wall.transform.SetParent(transform, false);
            var body = new GameObject("WallCollision"); body.layer = wall.layer; body.transform.SetParent(wall.transform, false);
            body.transform.position = (a + b) * .5f + outward * .25f;
            body.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            var collider = body.AddComponent<BoxCollider2D>(); collider.size = new Vector2(direction.magnitude + .25f, .5f);
            if (Definition.BoundaryBlocksShots) { shotBlockers.Add(collider); body.AddComponent<ThemeProjectileWall>(); }
            string key = Theme == 1 ? "castle.wall" : outward.y < -.5f ? "deepsea.rail" : "deepsea.glass";
            if (Mathf.Abs(direction.x) > .01f)
            {
                int count = Mathf.Max(1, Mathf.CeilToInt(direction.magnitude / 2.5f));
                for (int i = 0; i < count; i++)
                    ThemeAssets.Renderer(wall.transform, key, direction.magnitude / count + .2f,
                        Vector2.Lerp(a, b, (i + .5f) / count) + outward * .1f);
            }
            else
            {
                const float width = 2.5f, overlap = .15f, endMargin = .05f;
                var sprite = ThemeAssets.Named(key);
                float height = width * sprite.bounds.size.y / sprite.bounds.size.x;
                float baselineSpan = Mathf.Max(0, direction.magnitude + endMargin * 2 - height);
                int count = Mathf.Max(1, Mathf.CeilToInt(baselineSpan / (height - overlap)) + 1);
                float step = count > 1 ? baselineSpan / (count - 1) : 0;
                float x = a.x + outward.x * (width * .5f - overlap);
                float bottom = Mathf.Min(a.y, b.y) - endMargin;
                for (int i = 0; i < count; i++)
                    ThemeAssets.Renderer(wall.transform, key, width, new Vector2(x, bottom + step * i));
            }
        }

        private void Obstacle(ThemeObstacle obstacle)
        {
            var root = new GameObject("ThemeObstacle_" + obstacle.Id);
            root.SetActive(false); root.layer = obstacle.BlocksShots ? 10 : 16;
            root.transform.SetParent(transform, false);
            var collider = root.AddComponent<PolygonCollider2D>();
            Vector2[] points;
            if (obstacle.IsCircle)
            {
                root.transform.position = World(obstacle.Center);
                points = Enumerable.Range(0, 16).Select(i => new Vector2(Mathf.Cos(i * Mathf.PI / 8), Mathf.Sin(i * Mathf.PI / 8)) * obstacle.Radius).ToArray();
                var pillar = root.AddComponent<ThemeBattlePillar>(); pillar.Radius = obstacle.Radius; Pillars.Add(pillar);
                ThemeAssets.Renderer(root.transform, Theme == 1 ? "castle.pillar" : "deepsea.tank", obstacle.Radius * 2.3f, Vector2.up * .7f);
            }
            else
            {
                points = obstacle.Outline.Select(World).ToArray();
                float minX = points.Min(p => p.x), minY = points.Min(p => p.y), maxX = points.Max(p => p.x), maxY = points.Max(p => p.y);
                string key = Theme == 2 ? obstacle.Id.Contains("well") ? "deepsea.well" : obstacle.Id.Contains("pump") ? "deepsea.pump" :
                    obstacle.Id.Contains("tank") || obstacle.Id.Contains("pillar") ? "deepsea.tank" : "deepsea.rack" :
                    obstacle.Id.Contains("table") ? "castle.table" : obstacle.Id == "flowerbed" ? "castle.flowerbed" : "castle.coffin";
                if (obstacle.Id == "flowerbed")
                {
                    int rows = Mathf.Max(1, Mathf.RoundToInt((maxY - minY) / ((maxX - minX) * .7f)));
                    float length = (maxY - minY) / rows;
                    for (int i = 0; i < rows; i++)
                        ArtRect(key, new ThemeRect(minX, minY + i * length, maxX, minY + (i + 1) * length), true);
                }
                else if (!(Theme == 2 && BattleIndex == 0))
                    ArtRect(key, new ThemeRect(minX, minY, maxX, maxY), obstacle.Id == "flowerbed" || obstacle.Id.Contains("well"));
                if (obstacle.Id.Contains("table"))
                    for (float y = minY + .6f; y < maxY; y += 1.5f)
                        foreach (int side in new[] { -1, 1 })
                            ThemeAssets.Renderer(transform, "castle.chair", .9f, new Vector2((minX + maxX) * .5f + side * ((maxX - minX) * .5f + .6f), y));
            }
            collider.points = points;
            collider.isTrigger = !obstacle.BlocksMovement;
            if (obstacle.BlocksMovement)
            {
                var nav = root.AddComponent<PolyNavObstacle>(); nav.shapeType = PolyNavObstacle.ShapeType.Polygon;
            }
            if (obstacle.BlocksShots) { shotBlockers.Add(collider); root.AddComponent<ThemeProjectileWall>(); }
            root.SetActive(true);
        }

        private void BuildDeepSeaDetails()
        {
            foreach (var floor in Definition.Floors)
                for (float x = floor.MinX + 2; x < floor.MaxX; x += 5)
                    ThemeAssets.Renderer(transform, "deepsea.lamp", .65f, new Vector2(x, floor.MaxY - .8f));
            if (BattleIndex == 0)
            {
                ArtRect("deepsea.well", new ThemeRect(-4, 7, 4, 21), true);
                for (int side = -1; side <= 1; side += 2)
                    for (float y = 8; y < 21; y += 2.5f)
                        ThemeAssets.Renderer(transform, "deepsea.rail", .8f, new Vector2(side * 4, y));
                if (Eldritch)
                {
                    ArtRect("deepsea.floor", new ThemeRect(-2, 12, 2, 16), true, -5);
                    ArtRect("deepsea.floor", new ThemeRect(2, 13, 9, 15), true, -5);
                }
            }
            else if (BattleIndex == 1)
            {
                for (int side = -1; side <= 1; side += 2)
                    ThemeAssets.Renderer(transform, "deepsea.pipe", 2.2f, new Vector2(side * 8, Eldritch ? 25 : 25.5f));
                if (Eldritch)
                {
                    foreach (var link in ThemeEldritchData.PipeLinks)
                        ThemeAssets.Renderer(transform, "deepsea.pipe", 2, World(link.Exit));
                }
            }
            else
            {
                for (int side = -1; side <= 1; side += 2)
                    for (int y = 5; y < 24; y += 5)
                        ThemeAssets.Renderer(transform, "deepsea.lamp", .8f, new Vector2(side * (Eldritch ? 15 : 17), y));
            }
        }

        private void BuildFacilities()
        {
            if (Theme != 2) return;
            if (BattleIndex == 2 && !Eldritch)
                foreach (var arm in ThemeDeepSeaData.OctopusArms) ReserveFacility(arm.Id, arm.Footprint);
            if (BattleIndex == 0 && Eldritch)
                for (int i = 0; i < ThemeEldritchData.BloodMotherFacilities.Length; i++)
                    ReserveFacility("blood-mother:" + i, ThemeEldritchData.BloodMotherFacilities[i]);
            if (BattleIndex == 1 && Eldritch)
                for (int i = 0; i < ThemeEldritchData.PumpFacilities.Length; i++)
                    ReserveFacility("pump:" + i, ThemeEldritchData.PumpFacilities[i]);
        }

        internal void ReserveFacility(string id, ThemeRect area)
        {
            var root = new GameObject("ThemeFacility_" + id); root.layer = 16;
            root.transform.SetParent(transform, false); root.transform.position = World(area.Center);
            var collider = root.AddComponent<BoxCollider2D>(); collider.isTrigger = true;
            collider.size = new Vector2(area.Width, area.Height); facilities.Add(id, root);
        }

        internal void ReleaseFacility(string id)
        {
            if (!facilities.TryGetValue(id, out var item)) return;
            item.SetActive(false); facilities.Remove(id); Object.Destroy(item);
        }

        internal Vector2 World(ThemePoint point) => Origin + new Vector2(point.X, point.Y);
        internal Vector2 World(Vector2 point) => Origin + point;

        internal bool ValidFootprint(Vector2 point, Vector2 volume) =>
            ThemeBattleGeometry.ContainsFootprint(Definition.Floors, point.x - Origin.x, point.y - Origin.y,
                Mathf.Max(.1f, volume.x), Mathf.Max(.1f, volume.y)) && StratagemDeployment.ValidPoint(point, volume);

        internal bool ValidMovement(Vector2 point, Vector2 volume, GameObject owner = null)
        {
            if (!ThemeBattleGeometry.ContainsFootprint(Definition.Floors, point.x - Origin.x, point.y - Origin.y, volume.x, volume.y)) return false;
            foreach (var collider in Physics2D.OverlapBoxAll(point, new Vector2(Mathf.Max(.05f, volume.x - .05f), Mathf.Max(.05f, volume.y - .05f)), 0,
                (1 << 10) | (1 << 13) | (1 << 16)))
                if (!collider.isTrigger && (owner == null || collider.GetComponentInParent<模块_基本属性>()?.gameObject != owner)) return false;
            return true;
        }

        internal Vector2 EntrancePoint(Vector2 desired)
        {
            if (ValidMovement(desired, Vector2.one * .6f)) return desired;
            for (float ring = .25f; ring <= 2; ring += .25f)
                for (int angle = 0; angle < 16; angle++)
                {
                    var point = desired + new Vector2(Mathf.Cos(angle * Mathf.PI / 8), Mathf.Sin(angle * Mathf.PI / 8)) * ring;
                    if (ValidMovement(point, Vector2.one * .6f)) return point;
                }
            throw new InvalidOperationException("Theme entrance needs a full legal movement footprint");
        }

        internal Vector2 ValidLanding(Vector2 desired, Vector2 volume)
        {
            if (ValidFootprint(desired, volume)) return desired;
            for (float radius = .5f; radius <= 5; radius += .5f)
                for (int angle = 0; angle < 24; angle++)
                {
                    var point = desired + new Vector2(Mathf.Cos(angle * Mathf.PI / 12), Mathf.Sin(angle * Mathf.PI / 12)) * radius;
                    if (ValidFootprint(point, volume)) return point;
                }
            Vector2 closest = World(Definition.BaseHints[0].Example); float best = float.PositiveInfinity;
            foreach (var floor in Definition.Floors)
                for (float x = floor.MinX + volume.x * .5f; x <= floor.MaxX - volume.x * .5f; x += .5f)
                    for (float y = floor.MinY + volume.y * .5f; y <= floor.MaxY - volume.y * .5f; y += .5f)
                    {
                        Vector2 point = World(new Vector2(x, y)); float distance = (point - desired).sqrMagnitude;
                        if (distance < best && ValidFootprint(point, volume)) { best = distance; closest = point; }
                    }
            return closest;
        }

        internal bool BlocksShot(Vector2 from, Vector2 to)
        {
            foreach (var hit in Physics2D.LinecastAll(from, to, 1 << 10))
                if (shotBlockers.Contains(hit.collider)) return true;
            return false;
        }

        private void OnDestroy() { if (Current == this) Current = null; }
    }

    public sealed class ThemeProjectileWall : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var flight = other.GetComponent<模块_伤害精灵_飞行>();
            if (flight != null && flight.飞行速度 > 0) other.GetComponent<模块_伤害精灵>()?.死亡();
        }
    }

    [HarmonyPatch(typeof(预放置炮台碰撞监测), "射线检测")]
    internal static class ThemeFloorPlacement
    {
        private static void Postfix(预放置炮台碰撞监测 __instance)
        {
            var current = ThemeBattleScene.Current;
            if (current == null || !__instance.是否开始检测 || !建造系统.是否能建造) return;
            if (current.ValidFootprint(__instance.transform.position, __instance.体积)) return;
            __instance.是否允许放置 = 建造系统.是否能建造 = false;
        }
    }
}
