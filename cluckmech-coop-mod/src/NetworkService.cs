using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class SessionMessage
    {
        public string Type, Version = Plugin.Version;
        public int Role, Revision;
        public int Stage;
        public int EnergyBonusUsed;
        public float Delay;
        public bool Expedition, Transfer;
        public ExpeditionChoice Choice;
        public string Token;
        public GameplayModules Modules;
        public RunProgress Progress;
        public PlayerProgress PlayerProgress;
        public bool Held, Triple;
        public uint Unit;
        public uint[] Targets;
        public float X, Y;
        public StratagemEffectMessage Effect;
    }
    internal sealed class NetworkService
    {
        internal const string RequestKey = "CluckmechCoop.Session.Request.v3", ReplyKey = "CluckmechCoop.Session.Reply.v3";
        private static readonly MethodInfo rpc = AccessTools.Method(typeof(联机玩家控件), "发送消息广播_Rpc", new[] { typeof(string), typeof(string) });
        private Coroutine handshake;
        private readonly NetworkSessionState session = new NetworkSessionState();
        internal string Status = "等待队友同步模组设置";
        internal bool PeerReady => Plugin.Role == 0 || session.Peer;
        internal bool SettingsReady => Plugin.Role == 0 || session.Ready;
        internal void PlayerCreated(联机玩家控件 player) => BeginHandshake();
        internal void BeginHandshake()
        {
            OtherworldNetwork.Register();
            if (Plugin.Role == 0 || handshake != null) return;
            session.Open();
            handshake = Plugin.Instance.StartCoroutine(Handshake(session.Capture(),SceneLifecycle.Current));
        }
        private IEnumerator Handshake(ulong lease,SceneLifetime scene)
        {
            yield return null;
            for (int i = 0; i < 12 && !SettingsReady && session.Current(lease) && SceneLifecycle.IsCurrent(scene); i++)
            {
                Send(new SessionMessage { Type = "hello", Role = Plugin.Role });
                yield return new WaitForSecondsRealtime(0.5f);
            }
            if (session.Current(lease)) handshake = null;
        }
        internal void ClearScene()
        {
            session.CancelPending();
            if (handshake != null) Plugin.Instance.StopCoroutine(handshake);
            handshake = null;
        }
        internal void Disconnect() => Disconnect(true);
        internal void Shutdown() => Disconnect(false);
        private void Disconnect(bool refreshUi)
        {
            ClearScene();
            session.Close();
            OtherworldNetwork.Disconnect();
            Status = "等待队友同步模组设置";
            Plugin.Combat?.Disconnected();
            Plugin.Transfers.Disconnect();
            if (refreshUi) Plugin.Panel.RefreshState();
        }
        internal void Send(SessionMessage message)
        {
            var player = Plugin.LocalPlayer;
            if (player != null && player.isLocalPlayer) player.发送消息广播_cmd(RequestKey, JsonConvert.SerializeObject(message));
        }
        internal void PublishSettings()
        {
            if (Plugin.Role != 1) return;
            session.Published(); Status = "等待队友同步模组设置";
            BroadcastSettings();
        }
        private void BroadcastSettings() => Broadcast(new SessionMessage
        {
            Type = "settings", Revision = session.Revision, Expedition = ModuleSettings.Expedition,
            Transfer = ModuleSettings.Transfer, Choice = ModuleSettings.Choice,
            Modules = ModuleSettings.Extra, Progress = ModuleSettings.Progress, Token = Plugin.Combat?.Token, Held = Plugin.Combat?.Holds.Active ?? false, Triple = Plugin.Combat?.TripleSpeed ?? false, Delay = Plugin.Combat?.RemainingEnergy ?? 0
        });
        internal void SendProgress(PlayerProgress progress) => Send(new SessionMessage { Type = "progress", PlayerProgress = progress });
        internal void Complete(string token) => Broadcast(new SessionMessage { Type = "complete", Token = token });
        internal void Playback(string token, int stage, float delay) => Broadcast(new SessionMessage { Type = "playback", Token = token, Stage = stage, Delay = delay });
        internal void Broadcast(SessionMessage message)
        {
            if (Plugin.Role == 1 && Steam联机管理器.玩家_主机 != null)
                rpc.Invoke(Steam联机管理器.玩家_主机, new object[] { ReplyKey, JsonConvert.SerializeObject(message) });
        }
        private static bool Finite(float value) => NetworkValues.Finite(value);
        private static bool ValidProgress(PlayerProgress progress)
        {
            if (progress == null || progress.Coupons < 0 || !progress.ValidGrowthRewards() || !progress.ValidArtisan || !progress.ValidShop || progress.Regions == null || progress.RepairNodes == null || progress.Shops == null || progress.StratagemCooldowns == null) return false;
            foreach (string node in progress.RepairNodes) if (string.IsNullOrWhiteSpace(node)) return false;
            foreach (var cooldown in progress.StratagemCooldowns)
                if (cooldown == null || string.IsNullOrWhiteSpace(cooldown.Node) || cooldown.Slot < 0 || cooldown.Slot >= 5+progress.ExtraCarrySlots ||
                    !Rules.ValidMachine(cooldown.Machine) || !StratagemCatalog.IsUnit(cooldown.Machine.Id) || !Finite(cooldown.Remaining) || cooldown.Remaining < 0) return false;
            foreach (var shop in progress.Shops.Values)
                if (shop == null || shop.Sold == null || shop.Sold.Length != 4 || shop.Refreshes < 0 || shop.FourthPrice < 0 || shop.FourthId < -1) return false;
            return true;
        }
        private static bool ValidMessage(SessionMessage message)
        {
            if (!Finite(message.Delay) || message.Delay < 0 || !Finite(message.X) || !Finite(message.Y)) return false;
            switch (message.Type)
            {
                case "bronze-coins": return !string.IsNullOrWhiteSpace(message.Token) && message.Stage>0 && message.Stage<=128;
                case "swallow": case "swallow-request":
                    return !string.IsNullOrWhiteSpace(message.Token) && message.Unit != 0 && message.Revision > 0 && message.Targets != null && message.Targets.Length <= 32;
                case "stratagem-effect": return !string.IsNullOrWhiteSpace(message.Token) && message.Effect != null && message.Effect.Valid;
                case "settings":
                    if (message.Revision < 0 || message.Delay > 10 || (message.Choice != null && (string.IsNullOrWhiteSpace(message.Choice.Token) || message.Choice.RewardId < -1 || message.Choice.RewardId > 2))) return false;
                    if (message.Progress == null) return true;
                    if (message.Progress.Players == null || !message.Progress.ValidEnergy || !message.Progress.ValidBoons) return false;
                    foreach (var player in message.Progress.Players)
                        if (player.Key < 0 || player.Key > 2 || !ValidProgress(player.Value)) return false;
                    return true;
                case "progress": return ValidProgress(message.PlayerProgress);
                case "hello": case "ack": return message.Revision >= 0;
                case "playback": return !string.IsNullOrWhiteSpace(message.Token) && message.Stage >= 0;
                case "energy": return !string.IsNullOrWhiteSpace(message.Token) && message.Delay <= 10 && message.EnergyBonusUsed >= 0 && message.EnergyBonusUsed <= 2;
                case "growth-event": return !string.IsNullOrWhiteSpace(message.Token) && message.Stage>=88 && message.Stage<=92 && message.Revision>=0 && message.Revision<=1 && (message.Role==0 || message.Role==20);
                case "complete": case "speed": case "growth": case "speed-request": case "triple-request": case "energy-request": case "rabbit-aim":
                    return !string.IsNullOrWhiteSpace(message.Token);
                default: return false;
            }
        }
        internal void ServerReceive(联机玩家控件 sender, string json)
        {
            if (Plugin.Role != 1 || sender == null) return;
            bool guest = sender == Steam联机管理器.玩家_客户端;
            if (!guest && sender != Steam联机管理器.玩家_主机) return;
            if (!NetworkJson.TryRead<SessionMessage>(json, out var message) || !ValidMessage(message)) return;
            if (message.Version != Plugin.Version) { session.Mismatch(); Status = "双方模组版本不一致"; PreparationPanel.ShowStatus(Status); return; }
            if (message.Type == "swallow-request") { SwallowTargets.Handle(message,guest ? 2 : 1,true); return; }
            if (message.Type == "progress" && message.PlayerProgress != null)
            {
                ModuleSettings.SetPlayerProgress(guest ? 2 : 1,message.PlayerProgress);
                Plugin.Summary?.保存单局存档();
            }
            else if (FeatureNetwork.ServerReceive(message, guest ? 2 : 1)) return;
            else if (message.Type == "hello")
            {
                if (guest) session.PeerSeen();
                BroadcastSettings();
            }
            else if (message.Type == "ack" && guest && message.Revision == session.Revision)
            {
                session.Acknowledge(); Status = "";
                PreparationPanel.RefreshAll(); Plugin.Panel.RefreshState();
            }
        }
        internal void Receive(string key, string json)
        {
            if (key != ReplyKey || Plugin.Role != 2) return;
            if (!NetworkJson.TryRead<SessionMessage>(json, out var message) || !ValidMessage(message)) return;
            if (message.Version != Plugin.Version) { session.Mismatch(); Status = "双方模组版本不一致"; PreparationPanel.ShowStatus(Status); return; }
            if (message.Type == "swallow") { SwallowTargets.Handle(message,2,false); return; }
            if (message.Type == "settings")
            {
                if (message.Revision < session.Revision) return;
                ModuleSettings.ApplyHostSettings(message.Expedition,message.Transfer,message.Choice,message.Modules,message.Progress);
                CarryCapacity.RefreshPreparation();
                if (Plugin.Combat != null) { Plugin.Combat.Token = message.Token; Plugin.Combat.Synchronize(message.Held, message.Delay, message.Triple); }
                session.AcceptSettings(message.Revision); Status = "";
                Send(new SessionMessage { Type = "ack", Revision = session.Revision });
                CharacterBoons.Synchronize();MapBoonsUi.RefreshCurrent();
                PreparationPanel.RefreshAll();
                Plugin.Panel.RefreshState();
            }
            else if (message.Token == Plugin.Combat?.Token && ModuleSettings.Extra.Stratagems && message.Type == "stratagem-effect")
                StratagemEffects.Play(message.Effect);
            else if (message.Type=="bronze-coins") BronzeDragon.Receive(message);
            else if (message.Type=="growth-event") GrowthEventPanel.Receive(message);
            else if (FeatureNetwork.Receive(message)) return;
            else if (message.Type == "complete" && ModuleSettings.Active && ModuleSettings.Choice?.Token != null && ModuleSettings.Choice.Token == message.Token)
                Difficulty.ClientFinished();
            else if (message.Type == "playback" && ModuleSettings.Active && ModuleSettings.Choice?.Token != null && ModuleSettings.Choice.Token == message.Token)
                Difficulty.Playback(message.Stage, message.Delay);
        }
    }
    [HarmonyPatch(typeof(联机玩家控件), "UserCode_发送消息广播_cmd__String__String")]
    internal static class SessionRequestPatch
    {
        private static bool Prefix(联机玩家控件 __instance, string key, string 值)
        {
            if (key == NetworkService.ReplyKey) return false;
            if (key != NetworkService.RequestKey) return true;
            Plugin.Network.ServerReceive(__instance, 值); return false;
        }
    }
}
