using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CluckmechCoop
{
    internal sealed class ThemeMechanismMessage
    {
        public string Token, Id, Action;
        public int Sequence, Value;
        public float Seconds, X, Y;
        public uint Source;
        internal bool Valid => !string.IsNullOrEmpty(Token) && Token.Length <= 512 &&
            !string.IsNullOrEmpty(Id) && Id.Length <= 80 && (Action == "interact" || Action == "trigger" || Action == "push") &&
            Sequence >= 0 && Value >= 0 && Value <= 1000 && NetworkValues.Finite(Seconds) && Seconds >= 0 && Seconds <= 60 &&
            NetworkValues.Finite(X) && NetworkValues.Finite(Y) && (Action != "push" || Source != 0);
    }

    internal static class ThemeMechanisms
    {
        private static readonly Dictionary<string, ThemeInteractive> items = new Dictionary<string, ThemeInteractive>();
        internal static void Register(ThemeInteractive item) => items.Add(item.Id, item);
        internal static void Remove(ThemeInteractive item)
        { if (items.TryGetValue(item.Id, out var existing) && existing == item) items.Remove(item.Id); }

        internal static 模块_基本属性 Player(int role)
        {
            foreach (var unit in 世界事件系统.场上所有单位)
                if (unit != null && unit.gameObject.activeInHierarchy && unit.当前生命值 > 0 &&
                    unit.单位定位列表.Contains(模块_基本属性.单位定位枚举.玩家) && (int)unit.所属玩家 == role)
                    return unit;
            return null;
        }

        internal static bool ServerReceive(SessionMessage message, int role)
        {
            if (message.Type != "theme-interact") return false;
            var request = message.Mechanism;
            if (Plugin.Role == 1 && request != null && request.Valid && request.Action == "interact" &&
                request.Token == ModuleSettings.NodeKey && items.TryGetValue(request.Id, out var item))
                item.TryUse(Player(role));
            return true;
        }

        internal static void Receive(SessionMessage message)
        {
            var update = message.Mechanism;
            if (Plugin.Role == 2 && message.Type == "theme-mechanism" && update != null && update.Valid &&
                (update.Action == "trigger" || update.Action == "push") && update.Token == ModuleSettings.NodeKey && items.TryGetValue(update.Id, out var item))
                item.Apply(update);
        }
    }

    public abstract class ThemeInteractive : MonoBehaviour
    {
        internal string Id { get; private set; }
        internal float InteractionRadius { get; private set; }
        protected bool Available { get; set; } = true;
        protected bool ShowWhenUnavailable { get; set; }
        private TextMesh prompt;
        private float nextPrompt;

        protected void Bind(string id, float interactionRadius, string text)
        {
            Id = id; InteractionRadius = interactionRadius; ThemeMechanisms.Register(this);
            var input = gameObject.AddComponent<监听_输入触发>();
            var pressed = new UnityEvent(); pressed.AddListener(Pressed);
            input.输入事件列表.Add(new 监听_输入触发.输入触发类 { 按键 = KeyCode.E, 触发事件 = pressed });
            if (NativeUi.Font == null) return;
            var label = new GameObject("InteractionPrompt"); label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0, 1,
                StratagemAssets.Z(transform.position.y) - transform.position.z - .2f);
            prompt = label.AddComponent<TextMesh>(); prompt.text = text; prompt.font = NativeUi.Font;
            prompt.fontSize = 32; prompt.characterSize = .075f; prompt.anchor = TextAnchor.MiddleCenter;
            prompt.color = new Color(1, .9f, .5f, 1);
            var renderer = label.GetComponent<MeshRenderer>(); renderer.sharedMaterial = NativeUi.Font.material;
            label.SetActive(false);
        }

        protected bool InRange(模块_基本属性 player) => Plugin.InCombat && Time.timeScale > 0 &&
            !建造系统.是否建造模式 && Plugin.Summary?.是否光标位于UI界面 != true &&
            player != null && player.当前生命值 > 0 && ((Vector2)player.transform.position - (Vector2)transform.position).sqrMagnitude <= InteractionRadius * InteractionRadius;
        protected bool CanUse(模块_基本属性 player) => Available && InRange(player);
        protected void SetPrompt(string text) { if (prompt != null) prompt.text = text; }

        private void Pressed()
        {
            if (!CanUse(ThemeMechanisms.Player(Plugin.Role))) return;
            if (Plugin.Role == 2)
                Plugin.Network.Send(new SessionMessage { Type = "theme-interact", Mechanism =
                    new ThemeMechanismMessage { Token = ModuleSettings.NodeKey, Id = Id, Action = "interact" } });
            else TryUse(ThemeMechanisms.Player(Plugin.Role));
        }

        internal void Broadcast(ThemeMechanismMessage update)
        {
            update.Token = ModuleSettings.NodeKey; update.Id = Id; update.Action = "trigger";
            Apply(update);
            if (Plugin.Role == 1) Plugin.Network.Broadcast(new SessionMessage { Type = "theme-mechanism", Mechanism = update });
        }
        internal abstract void TryUse(模块_基本属性 player);
        internal abstract void Apply(ThemeMechanismMessage update);
        private void Update()
        {
            if (prompt == null || Time.unscaledTime < nextPrompt) return;
            nextPrompt = Time.unscaledTime + .15f;
            prompt.gameObject.SetActive((Available || ShowWhenUnavailable) && InRange(ThemeMechanisms.Player(Plugin.Role)));
        }
        protected virtual void OnDestroy() { if (Id != null) ThemeMechanisms.Remove(this); }
    }
}
