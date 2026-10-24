using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace CluckmechCoop
{
    // Uses the game's fixed-position and zoom transitions for this scene's four-second reveal.
    public sealed class ThemeWhiteIntroCamera : MonoBehaviour
    {
        internal static readonly Vector3 Focus = new Vector3(0, 30, 0);
        internal const float ViewHeight = 12;
        private 摄像机固定 native;
        private 摄像机固定.相机移动模式枚举 previousMode;
        private Vector3 previousFixed;
        private Vector3 previousPosition;
        private float previousHeight;
        private ThemeWhiteVisitorVisual face;
        private 事件管理器_战斗 events;
        private bool captured, restored;

        internal void Initialize(ThemeWhiteVisitorVisual visual)
        {
            face = visual;
            events = Object.FindObjectOfType<事件管理器_战斗>();
            events?.事件_基地车生成.AddListener(BasePlaced);
            events?.事件_触发建造模式.AddListener(BuildMode);
        }

        private IEnumerator Start()
        {
            // Birth.Start creates the native camera; its own Start resolves Camera.main and follow target.
            yield return null; yield return null;
            while (face != null && native == null && !restored)
            {
                native = Object.FindObjectOfType<摄像机固定>();
                if (native == null) yield return new WaitForSeconds(.1f);
            }
            if (face == null || restored || native == null) yield break;
            while (native != null && native.摄像机 == null && !restored) yield return null;
            if (native == null || restored) yield break;
            if (建造系统.是否建造模式 || 建造系统.是否放置基地车 ||
                Plugin.Spawner != null && Plugin.Spawner.关卡状态 != 刷怪器.关卡状态枚举.放置等待)
            { face.FinishPreparation(); restored = true; yield break; }
            previousMode = native.相机移动模式; previousFixed = native.固定位置; previousPosition = native.transform.position;
            previousHeight = native.相机高度 > 0 ? native.相机高度 : native.摄像机.orthographicSize;
            captured = true;
            native.固定位置 = Focus;
            native.设置相机移动模式(摄像机固定.相机移动模式枚举.固定位置);
            native.设置相机高度(ViewHeight);
            while (native != null && !restored &&
                (Vector2.Distance(native.transform.position, Focus) > .15f || native.摄像机.orthographicSize < ViewHeight - .2f))
                yield return null;
            if (native == null || restored || face == null) yield break;
            face.BeginPreparation(); ThemeBossPresentation.SyncPreparation();
            while (!restored && face != null && face.PreparationElapsed < ThemeEldritchData.WhiteReveal.DurationSeconds)
                yield return null;
            Restore();
        }

        private void BasePlaced(GameObject ignored)
        {
            // Native placement has already assigned its own follow mode and zoom before this event.
            face?.FinishPreparation(); restored = true; captured = false;
        }
        private void BuildMode(bool enabled)
        {
            if (!enabled) return;
            face?.FinishPreparation();
            // Native building owns its stationary mode. Return the view we borrowed before handing that mode back.
            if (!restored && captured && native != null && native.相机移动模式 == 摄像机固定.相机移动模式枚举.无 &&
                (native.固定位置 - Focus).sqrMagnitude < .001f)
            {
                native.transform.position = previousPosition;
                AccessTools.Field(typeof(摄像机固定), "实际位置").SetValue(native, previousPosition);
                AccessTools.Field(typeof(摄像机固定), "过渡位置").SetValue(native, Vector3.zero);
                native.固定位置 = previousFixed;
                if (Mathf.Approximately(native.相机高度, ViewHeight)) native.设置相机高度(previousHeight);
                restored = true; captured = false;
            }
            else Restore();
        }
        private void Restore()
        {
            if (restored) return; restored = true;
            if (!captured || native == null || native.相机移动模式 != 摄像机固定.相机移动模式枚举.固定位置 ||
                (native.固定位置 - Focus).sqrMagnitude > .001f || !Mathf.Approximately(native.相机高度, ViewHeight)) return;
            native.固定位置 = previousFixed;
            native.设置相机移动模式(previousMode);
            if (native.isActiveAndEnabled) native.设置相机高度(previousHeight);
        }
        private void OnDisable() { Restore(); }
        private void OnDestroy()
        {
            if (events != null) events.事件_基地车生成.RemoveListener(BasePlaced);
            if (events != null) events.事件_触发建造模式.RemoveListener(BuildMode);
            Restore();
        }
    }
}
