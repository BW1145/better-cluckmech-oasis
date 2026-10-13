using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using 函数库;

namespace CluckmechCoopDiagnostics
{
    [BepInPlugin("billy.cluckmech.coop.diagnostics", "联机模组运行诊断", "1.0.3")]
    [BepInDependency("billy.cluckmech.coop")]
    public sealed class DiagnosticPlugin : BaseUnityPlugin
    {
        private Probe probe;
        private Harmony trace;
        private bool quitting;
        private void Awake()
        {
            var info = Chainloader.PluginInfos["billy.cluckmech.coop"];
            var methods = Harmony.GetAllPatchedMethods().Where(m => Harmony.GetPatchInfo(m).Owners.Contains(info.Metadata.GUID)).ToArray();
            Logger.LogInfo($"[DIAG] Original version={info.Metadata.Version}; active patches={methods.Length}; managerFlags={gameObject.hideFlags}");
            Logger.LogInfo($"[DIAG] BOOT frame={Time.frameCount}; sceneCount={SceneManager.sceneCount}; managerScene={gameObject.scene.name}; managerSceneValid={gameObject.scene.IsValid()}; managerSceneLoaded={gameObject.scene.isLoaded}");
            foreach (var method in methods) Logger.LogInfo($"[DIAG] PATCH {method.DeclaringType.FullName}.{method.Name}");
            DestructionTrace.Log = Logger;
            bool detailedScan = Config.Bind("Diagnostics", "DetailedScan", false,
                "Enable detailed scene scans and destruction tracing for troubleshooting.").Value;
            if (detailedScan)
            {
                trace = new Harmony("billy.cluckmech.coop.diagnostics.trace");
                var prefix = new HarmonyMethod(AccessTools.Method(typeof(DestructionTrace), "BeforeDestroy"));
                foreach (string name in new[] { "Destroy", "DestroyImmediate" })
                    trace.Patch(AccessTools.Method(typeof(UnityEngine.Object), name, new[] { typeof(UnityEngine.Object) }), prefix: prefix);
            }
            Logger.LogInfo($"[DIAG] DetailedScan={detailedScan}; destruction observers={trace?.GetPatchedMethods().Count() ?? 0}");
            Application.quitting += Quitting;
            probe = gameObject.AddComponent<Probe>();
            probe.Original = info.Instance;
            probe.Log = Logger;
            probe.DetailedScan = detailedScan;
        }
        private void OnDestroy()
        {
            int remaining = Harmony.GetAllPatchedMethods().Count(m => Harmony.GetPatchInfo(m).Owners.Contains("billy.cluckmech.coop"));
            Logger.LogInfo($"[DIAG] Diagnostic plugin destroyed; quitting={quitting}; frame={Time.frameCount}; scene={gameObject.scene.name}; originalPatchesRemaining={remaining}");
            if (!quitting) Logger.LogInfo($"[DIAG] Destruction callback stack:\n{new StackTrace(1, true)}");
            Application.quitting -= Quitting;
            trace?.UnpatchSelf();
            if (probe != null) Destroy(probe);
        }
        private void Quitting() => quitting = true;
    }

    internal static class DestructionTrace
    {
        internal static ManualLogSource Log;
        private static void BeforeDestroy(UnityEngine.Object __0, MethodBase __originalMethod)
        {
            if (__0 == null || Chainloader.ManagerObject == null) return;
            var target = __0 as GameObject;
            if (target == null && __0 is Component component) target = component.gameObject;
            if (target != Chainloader.ManagerObject) return;
            Log.LogInfo($"[DIAG] MANAGED DESTRUCTION method={__originalMethod.Name}; target={__0.name}; frame={Time.frameCount}; caller:\n{new StackTrace(1, true)}");
        }
    }

    public sealed class Probe : MonoBehaviour
    {
        internal BaseUnityPlugin Original;
        internal ManualLogSource Log;
        internal bool DetailedScan;
        private float nextSnapshot;
        private float nextEnemySample;
        private string lastSnapshot;
        private bool sawHello;

        private void Start()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            Steam联机管理器.事件_联机消息广播_string.AddListener(StringMessage);
            Steam联机管理器.事件_联机消息广播_int.AddListener(IntMessage);
            Log.LogInfo("[DIAG] Observer ticking");
            Snapshot();
        }
        private void Update()
        {
            if (!DetailedScan) return;
            if (Time.realtimeSinceStartup < nextSnapshot) return;
            nextSnapshot = Time.realtimeSinceStartup + 1;
            Snapshot();
        }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogInfo($"[DIAG] SCENE {scene.name}");
            Snapshot();
        }
        private void StringMessage(string key, string value)
        {
            if (key != "CluckmechCoop.Reply.v1") return;
            string type = Regex.Match(value, "\"Type\"\\s*:\\s*\"([^\"]+)\"").Groups[1].Value;
            if (type == "hello" && sawHello) return;
            if (type == "hello") sawHello = true;
            Log.LogInfo($"[DIAG] MOD MESSAGE type={type}");
        }
        private void IntMessage(string key, int value)
        {
            if (key == 常数类.联机广播Key_挑战开启) Log.LogInfo($"[DIAG] CHALLENGE MESSAGE {value}");
        }
        private void Snapshot()
        {
            if (!DetailedScan) return;
            if (Original == null) { Log.LogInfo("[DIAG] Original plugin destroyed"); enabled = false; return; }
            var summary = Object.FindObjectOfType<总结脚本>();
            var spawners = Object.FindObjectsOfType<刷怪器>();
            var panel = Original.GetComponents<MonoBehaviour>().FirstOrDefault(c => c.GetType().FullName == "CluckmechCoop.CoopPanel");
            var canvases = Resources.FindObjectsOfTypeAll<Canvas>().Where(c => c.name == "Coop expedition").ToArray();
            var rounds = (IDictionary)AccessTools.Field(Original.GetType().Assembly.GetType("CluckmechCoop.Difficulty"), "rounds").GetValue(null);
            string waves = string.Join(" | ", spawners.Select(s =>
            {
                var round = rounds.Contains(s) ? rounds[s] : null;
                bool replay = round != null && (bool)AccessTools.Field(round.GetType(), "Replay").GetValue(round);
                bool boss = s.数据 != null && s.当前波次 < s.数据.波次数据.Count &&
                    (s.数据.波次数据[s.当前波次].是否为boss波 || s.数据.波次数据[s.当前波次].组次数据.Exists(g => g.是否为boss));
                return $"{s.name}: state={s.关卡状态}, wave={s.当前波次}, group={s.当前组次}, challenge={s.是否开启挑战}, boss={boss}, replay={replay}";
            }));
            string snapshot = $"scene={SceneManager.GetActiveScene().name}; role={Steam联机管理器.当前联机状态}; originalEnabled={Original.enabled}; originalActive={Original.gameObject.activeInHierarchy}; summary={summary != null}; ownsMap={summary?.是否拥有地图存档}; selectedChallenge={summary?.地图存档?.是否开启挑战}; CanTrade={AccessTools.Property(Original.GetType(), "CanTrade").GetValue(null)}; prepBags={Object.FindObjectsOfType<战斗背包>().Length}; panelEnabled={panel?.enabled}; canvases={canvases.Length}; canvasActive={string.Join(",", canvases.Select(c => c.gameObject.activeInHierarchy))}; waves=[{waves}]";
            if (snapshot != lastSnapshot) { lastSnapshot = snapshot; Log.LogInfo($"[DIAG] STATE {snapshot}"); }
            if (spawners.Length == 0 || Time.realtimeSinceStartup < nextEnemySample) return;
            nextEnemySample = Time.realtimeSinceStartup + 10;
            var enemies = Object.FindObjectsOfType<模块_基本属性>().Where(e => e.当前阵营 == 阵营数据库.阵营.敌方).ToArray();
            var sample = enemies.FirstOrDefault();
            int strengthened = enemies.Count(e => e.GetComponents<MonoBehaviour>().Any(c => c != null && c.GetType().FullName == "CluckmechCoop.Strength"));
            Log.LogInfo($"[DIAG] ENEMIES count={enemies.Length}; strengthened={strengthened}; sample={sample?.name}; maxHP={sample?.当前生命值上限}; attack={sample?.GetComponent<模块_攻击属性>()?.当前攻击力}");
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            Steam联机管理器.事件_联机消息广播_string.RemoveListener(StringMessage);
            Steam联机管理器.事件_联机消息广播_int.RemoveListener(IntMessage);
        }
    }
}
