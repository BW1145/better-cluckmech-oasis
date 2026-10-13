using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace CluckmechCoop
{
    internal sealed class Message
    {
        public string Type;
        public string Id;
        public string Version = Plugin.Version;
        public string Text;
        public int? Gold;
        public GoldResult GoldResult;
        public Transfer Transfer;
        public Wallet Wallet;
        public TransferResult Result;
    }

    internal sealed class PendingTransfer
    {
        public Transfer Transfer;
        public Wallet Host;
        public Wallet Guest;
        public int? HostGold, GuestGold;
    }

    internal sealed class TransferService
    {
        internal const string RequestKey = "CluckmechCoop.Request.v1";
        internal const string ReplyKey = "CluckmechCoop.Reply.v1";
        private PendingTransfer pending;
        private string lockId;
        private string appliedId;
        private Transfer lockedTransfer;
        private Wallet localWallet;
        private int localGold;
        private readonly HashSet<string> seen = new HashSet<string>();
        private Coroutine timeout;
        private static readonly FieldInfo[] walletFields = typeof(联机玩家控件.联机总结数据包).GetFields(BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo rpc = AccessTools.Method(typeof(联机玩家控件), "发送消息广播_Rpc", new[] { typeof(string), typeof(string) });
        private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        internal bool Locked => lockId != null;
        internal bool PeerReady => Plugin.Network.SettingsReady;
        internal string Status = "";
        internal void Disconnect()
        {
            pending = null; lockId = null; lockedTransfer = null; localWallet = null; appliedId = null; seen.Clear();
            if (timeout != null) Plugin.Instance.StopCoroutine(timeout);
            timeout = null;
        }
        private void WatchTimeout()
        {
            if (timeout != null) Plugin.Instance.StopCoroutine(timeout);
            timeout = Plugin.Instance.StartCoroutine(Timeout());
        }
        private System.Collections.IEnumerator Timeout()
        {
            yield return new WaitForSecondsRealtime(10);
            timeout = null;
            if (pending != null) Abort("转交同步超时");
            else if (lockId != null) { lockId = null; lockedTransfer = null; localWallet = null; Status = "转交同步超时"; Plugin.Panel.RefreshState(); }
        }

        private void Send(Message message)
        {
            var player = Plugin.LocalPlayer;
            if (player == null || !player.isLocalPlayer) return;
            player.发送消息广播_cmd(RequestKey, JsonConvert.SerializeObject(message, jsonSettings));
        }

        private void Broadcast(Message message)
        {
            if (Plugin.Role != 1 || Steam联机管理器.玩家_主机 == null) return;
            rpc.Invoke(Steam联机管理器.玩家_主机, new object[] { ReplyKey, JsonConvert.SerializeObject(message, jsonSettings) });
        }

        internal void Request(string kind, int amount, int index, Machine expected = null)
        {
            if ((kind != "gold" && kind != "machine") || Plugin.LocalPlayer == null || !ModuleSettings.Transfer || Locked || !Plugin.CanTrade || !PeerReady) { Status = "请在关间等待队友连接后转交"; return; }
            if (kind == "gold" && (amount <= 0 || amount > Plugin.Summary.当前金钱数)) { Status = "金币不足，请重新输入金额"; return; }
            var transfer = new Transfer { Id = Guid.NewGuid().ToString("N"), Sender = Plugin.Role, Kind = kind, Amount = amount, Index = index };
            if (kind == "machine")
            {
                var machines = Plugin.Summary.当前拥有炮台;
                if (index < 0 || index >= machines.Count || !Rules.SameMachine(machines[index].炮台id, machines[index].强化上限, machines[index].强化词条, expected))
                {
                    Status = "背包已变化，请重新选择鸡械";
                    Plugin.Panel.RefreshInventory();
                    return;
                }
                transfer.Expected = expected.Copy();
                Plugin.Log($"Transfer requested: sender {Plugin.Role}, index {index}, machine {expected.Id}, upgrades [{string.Join(",", expected.Upgrades)}]");
            }
            lockId = transfer.Id;
            lockedTransfer = transfer;
            WatchTimeout();
            Status = "正在转交…";
            Plugin.Panel.RefreshState();
            Send(new Message { Type = "give", Id = transfer.Id, Transfer = transfer });
        }

        private static bool ValidId(string id) => Guid.TryParseExact(id, "N", out var value) && value != Guid.Empty;
        private static bool ValidMessage(Message message) => ValidId(message.Id) &&
            (message.Type == "give" || message.Type == "prepare" || message.Type == "commit" ?
                Rules.ValidTransfer(message.Transfer) && message.Transfer.Id == message.Id :
                message.Type == "state" || message.Type == "decline" || message.Type == "abort");

        internal void ServerReceive(联机玩家控件 sender, string json)
        {
            if (Plugin.Role != 1 || sender == null) return;
            int role = sender == Steam联机管理器.玩家_主机 ? 1 : sender == Steam联机管理器.玩家_客户端 ? 2 : 0;
            if (role == 0 || !NetworkJson.TryRead<Message>(json, out var message) || message.Version != Plugin.Version || !ValidMessage(message)) return;
            if (message.Type == "give")
            {
                if (message.Transfer.Sender != role || seen.Contains(message.Id)) return;
                if (pending != null)
                { Broadcast(new Message { Type = "abort", Id = message.Id, Text = "另一笔转交正在完成，请稍后重试" }); return; }
                if (!ModuleSettings.Transfer || !Plugin.CanTrade || !PeerReady || Steam联机管理器.玩家_主机 == null || Steam联机管理器.玩家_客户端 == null)
                { Broadcast(new Message { Type = "abort", Id = message.Id, Text = "请在关间等待队友连接后转交" }); return; }
                seen.Add(message.Id);
                pending = new PendingTransfer { Transfer = message.Transfer };
                WatchTimeout();
                Broadcast(new Message { Type = "prepare", Id = message.Id, Transfer = message.Transfer });
                return;
            }
            if (pending == null || pending.Transfer.Id != message.Id) return;
            if (message.Type == "decline") { Abort("队友正在战斗或准备切换界面，请在关间转交"); return; }
            if (message.Type != "state") return;
            if (!ModuleSettings.Transfer || !Plugin.CanTrade || !PeerReady) { Abort("当前无法转交，请在关间重试"); return; }
            if (pending.Transfer.Kind == "gold")
            {
                int? actual = ServerGold(role);
                if (!actual.HasValue || actual < 0 || message.Gold != actual) { Abort("金币同步状态不一致，请重试"); return; }
                if (role == 1) { if (pending.HostGold.HasValue) return; pending.HostGold = actual; }
                else { if (pending.GuestGold.HasValue) return; pending.GuestGold = actual; }
                if (!pending.HostGold.HasValue || !pending.GuestGold.HasValue) return;
                if (ServerGold(1) != pending.HostGold || ServerGold(2) != pending.GuestGold) { Abort("金币已变化，请重试"); return; }
                var transaction = pending;
                var gold = Rules.GiveGold(transaction.HostGold.Value, transaction.GuestGold.Value, transaction.Transfer.Sender, transaction.Transfer.Amount);
                if (!gold.Success) { Abort(gold.Error); return; }
                UpdateNetworkGold(Steam联机管理器.玩家_主机, gold.Host);
                var guestGold = UpdateNetworkGold(Steam联机管理器.玩家_客户端, gold.Guest);
                var guestSave = UnityEngine.Object.FindObjectOfType<联机_2P存档>();
                if (guestSave != null) guestSave.联机存档_2P = guestGold;
                pending = null;
                Broadcast(new Message { Type = "commit", Id = transaction.Transfer.Id, Transfer = transaction.Transfer, GoldResult = gold });
                Plugin.Summary?.保存单局存档();
                return;
            }
            var wallet = ServerWallet(role);
            if (!Rules.SameWallet(wallet, message.Wallet)) { Abort("背包同步状态不一致，请重试"); return; }
            if (role == 1) { if (pending.Host != null) return; pending.Host = wallet; }
            else { if (pending.Guest != null) return; pending.Guest = wallet; }
            if (pending.Host == null || pending.Guest == null) return;
            if (!Rules.SameWallet(ServerWallet(1), pending.Host) || !Rules.SameWallet(ServerWallet(2), pending.Guest)) { Abort("背包已变化，请重试"); return; }
            var machineTransaction = pending;
            var result = Rules.Give(machineTransaction.Host, machineTransaction.Guest, machineTransaction.Transfer);
            if (!result.Success) { Abort(result.Error); return; }
            UpdateNetworkWallet(Steam联机管理器.玩家_主机, result.Host);
            var guest = UpdateNetworkWallet(Steam联机管理器.玩家_客户端, result.Guest);
            var save = UnityEngine.Object.FindObjectOfType<联机_2P存档>();
            if (save != null) save.联机存档_2P = guest;
            pending = null;
            Broadcast(new Message { Type = "commit", Id = machineTransaction.Transfer.Id, Transfer = machineTransaction.Transfer, Result = result });
            Plugin.Summary?.保存单局存档();
        }

        private static int? ServerGold(int role) => role == 1 ? Plugin.Summary?.当前金钱数 : Steam联机管理器.玩家_客户端?.Network总结数据_联机?.当前金钱数;
        private static Wallet ServerWallet(int role)
        {
            if (role == 1) return ReadWallet(Plugin.Summary);
            var data = Steam联机管理器.玩家_客户端?.Network总结数据_联机;
            if (data == null || data.当前拥有炮台 == null || data.当前拥有炮台.Any(x => x == null || x.强化词条 == null)) return null;
            return new Wallet { Gold = data.当前金钱数, Machines = data.当前拥有炮台.Select(Snapshot).ToList() };
        }

        private void Abort(string reason)
        {
            if (pending == null) return;
            string id = pending.Transfer.Id;
            Plugin.Log($"Transfer aborted: {reason}");
            pending = null;
            Broadcast(new Message { Type = "abort", Id = id, Text = reason });
        }

        internal void Receive(string key, string json)
        {
            if (key != ReplyKey || (Plugin.Role != 1 && Plugin.Role != 2)) return;
            if (!NetworkJson.TryRead<Message>(json, out var message) || message.Version != Plugin.Version || !ValidMessage(message)) return;
            if (message.Type == "prepare")
            {
                if (!ModuleSettings.Transfer || !Plugin.CanTrade || Plugin.LocalPlayer == null || message.Id == appliedId || (Locked && (lockId != message.Id || !Rules.SameTransfer(lockedTransfer, message.Transfer)))) { Send(new Message { Type = "decline", Id = message.Id }); return; }
                lockId = message.Id;
                lockedTransfer = message.Transfer;
                localGold = Plugin.Summary.当前金钱数;
                localWallet = message.Transfer.Kind == "machine" ? ReadWallet(Plugin.Summary) : null;
                if (message.Transfer.Kind == "machine" && !Rules.ValidWallet(localWallet)) { Send(new Message { Type = "decline", Id = message.Id }); return; }
                Status = "正在转交…";
                Plugin.Panel.Open();
                WatchTimeout();
                Plugin.LocalPlayer.上传玩家总结数据();
                if (message.Transfer.Kind == "gold")
                    Send(new Message { Type = "state", Id = message.Id, Gold = Plugin.Summary.当前金钱数 });
                else
                {
                    Send(new Message { Type = "state", Id = message.Id, Wallet = ReadWallet(Plugin.Summary) });
                }
            }
            else if (message.Type == "abort")
            {
                if (lockId == message.Id) { ClearLock(); Status = message.Text; }
                Plugin.Panel.RefreshState();
            }
            else if (message.Type == "commit" && lockId == message.Id && appliedId != message.Id)
            {
                if (Plugin.Summary == null || !Rules.SameTransfer(lockedTransfer, message.Transfer) || !ValidCommit(message)) return;
                if (message.Transfer.Kind == "gold")
                    Plugin.Summary.当前金钱数 = Plugin.Role == 1 ? message.GoldResult.Host : message.GoldResult.Guest;
                else
                {
                    ApplyLocal(Plugin.Role == 1 ? message.Result.Host : message.Result.Guest, message.Transfer);
                    Plugin.Log($"Transfer applied: sender {message.Transfer.Sender}, index {message.Transfer.Index}, machine {message.Transfer.Expected.Id}, upgrades [{string.Join(",", message.Transfer.Expected.Upgrades)}]");
                    Plugin.LocalPlayer.上传玩家总结数据();
                    Plugin.Panel.RefreshInventory();
                }
                appliedId = message.Id; ClearLock();
                Plugin.Summary.保存单局存档();
                Status = message.Transfer.Sender == Plugin.Role ? "已交给队友" : "已收到队友的转交";
                Plugin.Panel.RefreshWallet();
                Plugin.Panel.RefreshState();
            }
        }

        private void ClearLock()
        {
            if (timeout != null) Plugin.Instance.StopCoroutine(timeout);
            timeout = null; lockId = null; lockedTransfer = null; localWallet = null;
        }
        private bool ValidCommit(Message message)
        {
            var transfer = message.Transfer;
            if (transfer.Kind == "gold")
            {
                var result = message.GoldResult;
                long expected = (long)localGold + (transfer.Sender == Plugin.Role ? -transfer.Amount : transfer.Amount);
                return result != null && result.Success && result.Host >= 0 && result.Guest >= 0 && Plugin.Summary.当前金钱数 == localGold &&
                    expected == (Plugin.Role == 1 ? result.Host : result.Guest);
            }
            if (message.Result == null || !message.Result.Success || !Rules.ValidWallet(message.Result.Host) || !Rules.ValidWallet(message.Result.Guest) || !Rules.SameWallet(ReadWallet(Plugin.Summary), localWallet)) return false;
            var expectedWallet = localWallet.Copy();
            if (transfer.Sender == Plugin.Role)
            {
                if (transfer.Index >= expectedWallet.Machines.Count || !Rules.SameMachine(expectedWallet.Machines[transfer.Index], transfer.Expected)) return false;
                expectedWallet.Machines.RemoveAt(transfer.Index);
            }
            else expectedWallet.Machines.Add(transfer.Expected.Copy());
            return Rules.SameWallet(expectedWallet, Plugin.Role == 1 ? message.Result.Host : message.Result.Guest);
        }

        private static Wallet ReadWallet(总结脚本 summary) => summary == null || summary.当前拥有炮台 == null || summary.当前拥有炮台.Any(x => x == null || x.强化词条 == null) ? null : new Wallet
        {
            Gold = summary.当前金钱数,
            Machines = summary.当前拥有炮台.Select(Snapshot).ToList()
        };
        internal static Machine Snapshot(总结脚本.背包单元 machine) => new Machine
        { Id = machine.炮台id, UpgradeLimit = machine.强化上限, Upgrades = new List<int>(machine.强化词条) };

        private static List<总结脚本.背包单元> NativeMachines(Wallet wallet) => wallet.Machines.Select(x =>
            new 总结脚本.背包单元 { 炮台id = x.Id, 强化上限 = x.UpgradeLimit, 强化词条 = new List<int>(x.Upgrades) }).ToList();

        private static 联机玩家控件.联机总结数据包 UpdateNetworkGold(联机玩家控件 player, int gold)
        {
            var copy = new 联机玩家控件.联机总结数据包();
            foreach (var field in walletFields) field.SetValue(copy, field.GetValue(player.Network总结数据_联机));
            copy.当前金钱数 = gold;
            player.Network总结数据_联机 = copy;
            return copy;
        }

        private static 联机玩家控件.联机总结数据包 UpdateNetworkWallet(联机玩家控件 player, Wallet wallet)
        {
            var copy = new 联机玩家控件.联机总结数据包();
            foreach (FieldInfo field in walletFields)
                field.SetValue(copy, field.GetValue(player.Network总结数据_联机));
            copy.当前金钱数 = wallet.Gold;
            copy.当前拥有炮台 = NativeMachines(wallet);
            player.Network总结数据_联机 = copy;
            return copy;
        }

        private static void ApplyLocal(Wallet wallet, Transfer transfer)
        {
            var summary = Plugin.Summary;
            if (transfer.Kind == "gold") { summary.当前金钱数 = wallet.Gold; return; }
            var oldInventory = summary.当前拥有炮台;
            bool removing = transfer.Kind == "machine" && transfer.Sender == Plugin.Role;
            Func<int, int> remap = i => !removing ? i : i == transfer.Index ? -1 : i > transfer.Index ? i - 1 : i;
            var carried = summary.当前携带炮台.Select(x => remap(oldInventory.IndexOf(x))).ToArray();
            var selection = UnityEngine.Object.FindObjectsOfType<战斗背包>().Select(x => new { Bag = x, Indices = x.背包类.当前已选单元.Select(remap).Where(i => i >= 0).ToArray() }).ToArray();
            summary.当前金钱数 = wallet.Gold;
            summary.当前拥有炮台 = NativeMachines(wallet);
            for (int i = 0; i < carried.Length; i++)
                summary.当前携带炮台[i] = carried[i] >= 0 && carried[i] < wallet.Machines.Count ? summary.当前拥有炮台[carried[i]] : new 总结脚本.背包单元();
            foreach (var bag in UnityEngine.Object.FindObjectsOfType<玩家背包脚本>()) bag.更新背包();
            foreach (var item in selection)
            {
                var bag = item.Bag.背包类;
                bag.当前已选单元.Clear();
                bag.更新背包(wallet.Machines.Count);
                foreach (int index in item.Indices.Where(i => i < wallet.Machines.Count && i < bag.单元列表.Count))
                {
                    bag.当前已选单元.Add(index);
                    bag.单元列表[index].GetComponent<Toggle>()?.SetIsOnWithoutNotify(true);
                }
            }
        }
    }

    [HarmonyPatch(typeof(联机玩家控件), "UserCode_发送消息广播_cmd__String__String")]
    internal static class TransferRequestPatch
    {
        private static bool Prefix(联机玩家控件 __instance, string key, string 值)
        {
            if (key == TransferService.ReplyKey) return false;
            if (key != TransferService.RequestKey) return true;
            Plugin.Transfers.ServerReceive(__instance, 值);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class TransferSceneLock
    {
        private static IEnumerable<MethodBase> TargetMethods() => AccessTools.GetDeclaredMethods(typeof(场景切换按钮)).Where(x => x.Name == "切换场景");
        private static bool Prefix() => !Plugin.Transfers.Locked;
    }
}
