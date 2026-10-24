using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    public sealed class ThemePumpLung : MonoBehaviour
    {
        private ThemeBattleScene scene;
        private ThemeBattleWaves waves;
        private 刷怪器 spawner;
        private 事件管理器_战斗 events;
        private ThemePumpRoutingDisplay display;
        private readonly Dictionary<string, ThemeTransportRoute> routes = new Dictionary<string, ThemeTransportRoute>();
        private readonly int[,] choices = new int[5, 3];
        private readonly float[,] previewAt = new float[5, 3];
        private readonly float[] waveStarted = { -1, -1, -1, -1, -1 };
        private readonly bool[] prepared = new bool[5], sideDoorEast = new bool[5];
        private bool bossRight;
        private ThemeEnemyActor boss;
        private float bossEngaged = -1;

        internal void Initialize(ThemeBattleScene battlefield, ThemeBattleWaves owner)
        {
            scene = battlefield; waves = owner;
            for (int w = 0; w < 5; w++) for (int b = 0; b < 3; b++) previewAt[w, b] = -1;
            foreach (var link in ThemeEldritchData.PipeLinks)
                routes.Add(link.Id, new ThemeTransportRoute(scene, link, 0, ThemeEldritchData.PumpParameters.TransitSeconds, .3f, .15f));
            var marker = new GameObject("LungRoutingDisplay"); marker.transform.SetParent(transform, false);
            display = marker.AddComponent<ThemePumpRoutingDisplay>(); display.Initialize(scene);
            waves.ConfigureBatch = ConfigureBatch; waves.BeforeBatch = BeforeBatch;
            waves.Spawned += Spawned; waves.WaveStarting += WaveStarting;
            spawner = Object.FindObjectOfType<刷怪器>();
            spawner.波次等待开始广播.AddListener(Preparing);
            events = Object.FindObjectOfType<事件管理器_战斗>(); events.事件_单位死亡.AddListener(Died);
        }

        private void Preparing(int wave)
        {
            if (Plugin.Role == 2 || wave < 0 || wave >= 5) return;
            Plan(wave);
            float delay = spawner.数据.波次数据[wave].等待时间 - ThemeEldritchData.PumpParameters.RouteWarningSeconds;
            StartCoroutine(PreviewAfter(wave, 0, Mathf.Max(0, delay)));
        }
        private void Plan(int wave)
        {
            if (prepared[wave]) return;
            prepared[wave] = true; sideDoorEast[wave] = Random.value < .5f;
            if (wave == 4) bossRight = Random.value < .5f;
            foreach (var rule in ThemeEldritchData.PumpRouting)
            {
                if (rule.WaveNumber != wave + 1) continue;
                int value = 0;
                if (rule.Route == EldritchPumpRoutePolicy.OneSideRandom) value = Random.Range(1, 3);
                else if (rule.Route == EldritchPumpRoutePolicy.ThreeWayRandom) value = Random.Range(0, 3);
                else if (rule.Route == EldritchPumpRoutePolicy.BossSideToOther) value = bossRight ? 2 : 1;
                choices[wave, rule.BatchIndex] = value;
            }
        }
        private void WaveStarting(int wave)
        {
            if (Plugin.Role == 2) return;
            Plan(wave); waveStarted[wave] = Time.time;
            StartCoroutine(PreviewAfter(wave, 1, 8 - ThemeEldritchData.PumpParameters.RouteWarningSeconds));
            if (wave != 4) StartCoroutine(PreviewAfter(wave, 2, 16 - ThemeEldritchData.PumpParameters.RouteWarningSeconds));
        }
        private IEnumerator PreviewAfter(int wave, int batch, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            Preview(wave, batch);
        }
        private void Preview(int wave, int batch)
        {
            if (previewAt[wave, batch] >= 0) return;
            previewAt[wave, batch] = Time.time;
            display.Broadcast(new ThemeMechanismMessage {
                Sequence = wave * 3 + batch + 1, Value = wave * 100 + batch * 10 + choices[wave, batch],
                Source = (uint)((sideDoorEast[wave] ? 1 : 0) | (bossRight ? 2 : 0)),
                Seconds = ThemeEldritchData.PumpParameters.RouteWarningSeconds
            });
        }
        internal ThemeSpawnGroup[] ConfigureBatch(int wave, int batch, ThemeSpawnGroup[] groups)
        {
            Plan(wave);
            var result = new ThemeSpawnGroup[groups.Length];
            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i]; string gate = group.EntranceId;
                if (wave == 4) gate = ThemeEldritchData.MirroredEntrance(gate, bossRight);
                else if ((wave == 2 || wave == 3) && sideDoorEast[wave] && gate == ThemeEldritchData.West) gate = ThemeEldritchData.East;
                result[i] = new ThemeSpawnGroup(group.UnitId, gate, group.Count, group.DelaySeconds, group.IntervalSeconds);
            }
            return result;
        }
        internal IEnumerator BeforeBatch(int wave, int batch)
        {
            if (Plugin.Role == 2) yield break;
            Plan(wave);
            if (wave == 4 && batch == 2)
            {
                while (bossEngaged < 0) yield return null;
                float release = Mathf.Max(waveStarted[wave] + ThemeEldritchData.PumpParameters.LastBatchEarliestSeconds,
                    bossEngaged + ThemeEldritchData.PumpParameters.BossEngagementWarningSeconds);
                float warnAt = release - ThemeEldritchData.PumpParameters.RouteWarningSeconds;
                while (Time.time < warnAt) yield return null;
                Preview(wave, batch);
                while (Time.time < release) yield return null;
            }
            else
            {
                if (previewAt[wave, batch] < 0) Preview(wave, batch);
                while (Time.time < previewAt[wave, batch] + ThemeEldritchData.PumpParameters.RouteWarningSeconds) yield return null;
            }
        }
        private void Spawned(GameObject unit, ThemeSpawnContext context)
        {
            if (context == null || context.BatchIndex < 0 || Plugin.Role == 2) return;
            if (context.UnitId == ThemeDeepSeaData.ValveAnemone)
            { boss = unit.GetComponent<ThemeEnemyActor>(); return; }
            int choice = choices[context.WaveIndex, context.BatchIndex];
            if (choice == 1 && context.EntranceId == ThemeEldritchData.NorthWest)
                routes[ThemeEldritchData.PipeWestToEast].Assign(unit);
            else if (choice == 2 && context.EntranceId == ThemeEldritchData.NorthEast)
                routes[ThemeEldritchData.PipeEastToWest].Assign(unit);
        }
        private void Update()
        {
            if (Plugin.Role == 2 || bossEngaged >= 0 || boss == null || !boss.Alive) return;
            var target = boss.SelectTarget(ThemeEldritchData.PumpParameters.BossEngagementRange);
            if (target != null && !scene.BlocksShot(boss.transform.position, target.transform.position)) bossEngaged = Time.time;
        }
        private void Died(GameObject unit)
        { if (Plugin.Role != 2 && bossEngaged < 0 && boss != null && unit == boss.gameObject) bossEngaged = Time.time; }
        private void OnDestroy()
        {
            if (waves != null)
            { waves.Spawned -= Spawned; waves.WaveStarting -= WaveStarting; waves.ConfigureBatch = null; waves.BeforeBatch = null; }
            if (spawner != null) spawner.波次等待开始广播.RemoveListener(Preparing);
            if (events != null) events.事件_单位死亡.RemoveListener(Died);
            foreach (var route in routes.Values) route.Stop();
        }
    }

    public sealed class ThemePumpRoutingDisplay : ThemeInteractive
    {
        private ThemeBattleScene scene;
        private int sequence;
        internal void Initialize(ThemeBattleScene battlefield)
        { scene = battlefield; Bind("lung-routing", 0, ""); Available = false; }
        internal override void TryUse(模块_基本属性 player) { }
        internal override void Apply(ThemeMechanismMessage update)
        {
            if (update.Sequence <= sequence) return;
            sequence = update.Sequence; int choice = update.Value % 10;
            int wave = update.Value / 100, batch = update.Value % 100 / 10;
            Vector2 left = scene.World(new Vector2(-8, 25)), right = scene.World(new Vector2(8, 25));
            Vector2 leftExit = scene.World(new Vector2(-8, 9)), rightExit = scene.World(new Vector2(8, 9));
            if (choice == 1)
            { ThemeVisual.Point(left, 1.8f, update.Seconds); ThemeVisual.Point(rightExit, 1.8f, update.Seconds); ThemeVisual.Line(left, rightExit, .35f, update.Seconds); }
            else if (choice == 2)
            { ThemeVisual.Point(right, 1.8f, update.Seconds); ThemeVisual.Point(leftExit, 1.8f, update.Seconds); ThemeVisual.Line(right, leftExit, .35f, update.Seconds); }
            else
            { ThemeVisual.Line(left, scene.World(new Vector2(-8, 22)), .35f, update.Seconds); ThemeVisual.Line(right, scene.World(new Vector2(8, 22)), .35f, update.Seconds); }
            if ((wave == 2 || wave == 3) && batch == 2)
                ThemeVisual.Point(scene.World((update.Source & 1) != 0 ? new Vector2(15, 18) : new Vector2(-15, 16)), 1.6f, update.Seconds);
            if (wave == 4 && batch == 1)
                ThemeVisual.Point((update.Source & 2) != 0 ? right : left, 2.2f, update.Seconds);
        }
    }
}
