using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CluckmechCoop;
using Newtonsoft.Json;

static class ProtocolHarness
{
    private static string Json(object message) => JsonConvert.SerializeObject(message);
    private static Message Give(string kind = "gold", int sender = 1) => new Message
    {
        Type = "give", Id = Guid.NewGuid().ToString("N"),
        Transfer = new Transfer { Sender = sender, Kind = kind, Amount = 10, Index = 0,
            Expected = new Machine { Id = 12, UpgradeLimit = 2, Upgrades = new List<int> { 4 } } }
    };
    private static void Start(Message message) { message.Transfer.Id = message.Id; Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_主机, Json(message)); }
    private static object Pending() => typeof(TransferService).GetField("pending", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Plugin.Transfers);
    internal static void Run(Action<bool, string> check)
    {
        var oldHost = Harness.Host; var oldGuest = Harness.Guest;
        Harness.Host = new Context(1); Harness.Guest = new Context(2);
        try
        {
            using (Harness.Use(Harness.Host))
            {
                ModuleSettings.MainMenu(); ModuleSettings.EnterRun(); ModuleSettings.Transfer = true; Plugin.Network.PublishSettings();
                Harness.Host.Summary.当前金钱数 = 100; Harness.Guest.Summary.当前金钱数 = 40;
                Steam联机管理器.玩家_客户端.Network总结数据_联机 = new 联机玩家控件.联机总结数据包 { 当前金钱数 = 40 };
                Harness.DeliverTransfers = false;
                var malformed = new[] { null, "", "null", "[]", "{", "{}", "{\"Type\":\"give\",\"Version\":3}", "{\"Type\":\"give\",\"Version\":\"0.4.7\",\"Gold\":2147483648}",
                    new string('x', NetworkJson.MaxLength + 1), "{\"Type\":\"give\",\"Version\":\"0.4.7\",\"Wallet\":" + new string('[', 40) + "0" + new string(']', 40) + "}" };
                foreach (var json in malformed)
                {
                    Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_主机, json);
                    Plugin.Transfers.Receive(TransferService.ReplyKey, json);
                    Plugin.Network.ServerReceive(Steam联机管理器.玩家_主机, json);
                    using (Harness.Use(Harness.Guest)) Plugin.Network.Receive(NetworkService.ReplyKey, json);
                    check(Pending() == null && !Plugin.Transfers.Locked, "malformed JSON is ignored across all four production receivers");
                }
                var invalid = Give(); invalid.Transfer.Id = invalid.Id;
                var cases = new List<Message> {
                    new Message { Type = "give", Id = invalid.Id },
                    new Message { Type = "give", Id = "bad", Transfer = invalid.Transfer },
                    new Message { Type = "other", Id = invalid.Id },
                    new Message { Type = "give", Id = Guid.NewGuid().ToString("N"), Transfer = invalid.Transfer }
                };
                foreach (var message in cases) { Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_主机, Json(message)); check(Pending() == null, "invalid transfer envelope creates no transaction"); }
                var spoof = Give(sender:2); Start(spoof); check(Pending() == null, "authenticated player identity rejects spoofed Sender");
                var zero = Give(); zero.Transfer.Amount = 0; Start(zero); check(Pending() == null, "zero amount creates no transaction");
                var negative = Give(); negative.Transfer.Amount = -1; Start(negative); check(Pending() == null, "negative amount creates no transaction");
                var nullUpgrades = Give("machine"); nullUpgrades.Transfer.Expected.Upgrades = null; Start(nullUpgrades); check(Pending() == null, "null machine upgrades create no transaction");
                var missingVersion = Json(invalid).Replace("\"Version\":\"0.4.7\",", "");
                Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_主机, missingVersion); check(Pending() == null, "missing explicit protocol version is rejected");
                Plugin.Transfers.ServerReceive(null, Json(invalid)); Plugin.Transfers.ServerReceive(new 联机玩家控件(), Json(invalid));
                check(Pending() == null, "null and unrelated senders cannot create transfers");

                var gold = Give(); Start(gold);
                Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_主机, Json(new Message { Type = "state", Id = gold.Id, Gold = 100 }));
                Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_客户端, Json(new Message { Type = "state", Id = gold.Id, Gold = 999999 }));
                check(Pending() == null && Steam联机管理器.玩家_客户端.Network总结数据_联机.当前金钱数 == 40 && Harness.Host.Summary.当前金钱数 == 100, "inflated self-reported balance aborts without minting gold");
                Start(gold); check(Pending() == null, "replayed accepted request is ignored even after abort");
                var wrongId = Give(); Start(wrongId);
                Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_客户端, Json(new Message { Type = "state", Id = Guid.NewGuid().ToString("N"), Gold = 40 }));
                check(Pending() != null, "unrelated transaction state cannot affect pending transfer");
                Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_主机, Json(new Message { Type = "state", Id = wrongId.Id, Gold = 100 }));
                Harness.Host.Summary.当前金钱数 = 101;
                Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_客户端, Json(new Message { Type = "state", Id = wrongId.Id, Gold = 40 }));
                check(Pending() == null && Steam联机管理器.玩家_客户端.Network总结数据_联机.当前金钱数 == 40, "balance changed between acknowledgements aborts transaction");
                Harness.Host.Summary.当前金钱数 = 100;
                var machine = Give("machine"); Start(machine);
                var forged = new Wallet { Gold = 100, Machines = new List<Machine> { machine.Transfer.Expected } };
                Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_主机, Json(new Message { Type = "state", Id = machine.Id, Wallet = forged }));
                check(Pending() == null && Harness.Host.Summary.当前拥有炮台.Count == 0, "forged self-reported inventory cannot create ownership");
                var badWallet = Give("machine"); Start(badWallet);
                Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_客户端, Json(new Message { Type = "state", Id = badWallet.Id, Wallet = new Wallet { Machines = new List<Machine> { null } } }));
                check(Pending() == null, "null nested inventory entries abort safely");
                var nonOwned = Give("machine"); Start(nonOwned);
                Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_主机, Json(new Message { Type = "state", Id = nonOwned.Id, Wallet = new Wallet { Gold = 100 } }));
                Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_客户端, Json(new Message { Type = "state", Id = nonOwned.Id, Wallet = new Wallet { Gold = 40 } }));
                check(Pending() == null && Steam联机管理器.玩家_客户端.Network总结数据_联机.当前拥有炮台.Count == 0, "server ownership check rejects nonexistent machine index");

                var prefix = typeof(TransferRequestPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
                check(!(bool)prefix.Invoke(null, new object[] { Steam联机管理器.玩家_客户端, TransferService.ReplyKey, Json(invalid) }), "client command cannot relay forged transfer replies");
                prefix = typeof(SessionRequestPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
                check(!(bool)prefix.Invoke(null, new object[] { Steam联机管理器.玩家_客户端, NetworkService.ReplyKey, "{}" }), "client command cannot relay forged session replies");

                using (Harness.Use(Harness.Guest))
                {
                    ModuleSettings.Choice = null; ModuleSettings.InRun = ModuleSettings.Expedition = true;
                    Plugin.Network.Receive(NetworkService.ReplyKey, Json(new SessionMessage { Type = "complete", Token = "test" }));
                    Plugin.Network.Receive(NetworkService.ReplyKey, Json(new SessionMessage { Type = "playback", Token = "test" }));
                    check(Harness.Guest.Completed == 0 && Harness.Guest.PlaybackMessages == 0, "null choice safely ignores completion and playback");
                    var prepare = Give(); prepare.Transfer.Id = prepare.Id; prepare.Type = "prepare";
                    Plugin.Transfers.Receive(TransferService.ReplyKey, Json(prepare));
                    check(Plugin.Transfers.Locked, "valid preparation captures local balance before commit");
                    var commit = new Message { Type = "commit", Id = prepare.Id, Transfer = prepare.Transfer };
                    Plugin.Transfers.Receive(TransferService.ReplyKey, Json(commit));
                    check(Plugin.Transfers.Locked && Plugin.Summary.当前金钱数 == 40, "missing gold result is ignored safely");
                    commit.GoldResult = new GoldResult { Host = 90, Guest = 999 };
                    Plugin.Transfers.Receive(TransferService.ReplyKey, Json(commit));
                    check(Plugin.Summary.当前金钱数 == 40, "commit cannot change local balance beyond prepared amount");
                    commit.Transfer = new Transfer { Id = prepare.Id, Sender = 1, Kind = "gold", Amount = 999 };
                    Plugin.Transfers.Receive(TransferService.ReplyKey, Json(commit));
                    check(Plugin.Summary.当前金钱数 == 40, "commit cannot substitute a different transfer amount");
                    Plugin.Transfers.Disconnect();
                    var prepareMachine = Give("machine"); prepareMachine.Transfer.Id = prepareMachine.Id; prepareMachine.Type = "prepare";
                    Plugin.Transfers.Receive(TransferService.ReplyKey, Json(prepareMachine));
                    Plugin.Transfers.Receive(TransferService.ReplyKey, Json(new Message { Type = "commit", Id = prepareMachine.Id, Transfer = prepareMachine.Transfer, Result = new TransferResult() }));
                    check(Plugin.Summary.当前拥有炮台.Count == 0, "null commit wallets are ignored safely");
                    Plugin.Transfers.Disconnect();
                    var settings = new SessionMessage { Type = "settings", Revision = 100, Progress = new RunProgress { Players = null } };
                    Plugin.Network.Receive(NetworkService.ReplyKey, Json(settings));
                    check(ModuleSettings.Progress.Players != null, "null nested synchronized progress does not replace active state");
                    settings.Progress = new RunProgress { Players = new Dictionary<int, PlayerProgress> { [2] = new PlayerProgress { Shops = new Dictionary<string, ShopProgress> { ["shop"] = new ShopProgress { Sold = new bool[1] } } } } };
                    Plugin.Network.Receive(NetworkService.ReplyKey, Json(settings));
                    check(ModuleSettings.Progress.Player(2).Shops.Count == 0, "invalid sold marker dimensions are rejected before state installation");
                    ModuleSettings.Choice = new ExpeditionChoice { Enabled = true, Token = "test" };
                    Plugin.Network.Receive(NetworkService.ReplyKey, Json(new SessionMessage { Type = "playback", Token = "test", Delay = float.NaN }));
                    Plugin.Network.Receive(NetworkService.ReplyKey, Json(new SessionMessage { Type = "playback", Token = "test", Stage = -1 }));
                    check(Harness.Guest.PlaybackMessages == 0, "invalid playback delay and stage cannot enter runtime");
                }
                Harness.DeliverTransfers = true; Harness.QueueTransport = true;
                int hostSaves = Harness.Host.Saves, guestSaves = Harness.Guest.Saves;
                Plugin.Transfers.Request("gold", 10, -1);
                check(Plugin.Transfers.Locked && Harness.Guest.Summary.当前金钱数 == 40, "queued transfer waits for peer state before application");
                Harness.Drain();
                check(Harness.Host.Summary.当前金钱数 == 90 && Harness.Guest.Summary.当前金钱数 == 50, "queued reliable transport preserves both gold balances");
                check(Harness.Host.Saves > hostSaves && Harness.Guest.Saves > guestSaves, "both local wallets are saved after commit delivery");
                var successfulGive = Harness.Messages.Last(x => x.Key == TransferService.RequestKey && JsonConvert.DeserializeObject<Message>(x.Json).Type == "give").Json;
                Plugin.Transfers.ServerReceive(Steam联机管理器.玩家_主机, successfulGive);
                Harness.Drain();
                check(Harness.Host.Summary.当前金钱数 == 90 && Harness.Guest.Summary.当前金钱数 == 50, "completed request replay cannot transfer twice");
                Harness.Host.Summary.当前拥有炮台.Add(new 总结脚本.背包单元 { 炮台id = 12, 强化上限 = 2, 强化词条 = new List<int> { 4 } });
                Plugin.Transfers.Request("machine", 0, 0, TransferService.Snapshot(Harness.Host.Summary.当前拥有炮台[0]));
                Harness.Drain();
                check(Harness.Host.Summary.当前拥有炮台.Count == 0 && Harness.Guest.Summary.当前拥有炮台.Count == 1, "queued machine transfer conserves ownership through native uploads");
                using (Harness.Use(Harness.Guest)) Plugin.Transfers.Request("machine", 0, 0, TransferService.Snapshot(Harness.Guest.Summary.当前拥有炮台[0]));
                Harness.Drain();
                check(Harness.Host.Summary.当前拥有炮台.Count == 1 && Harness.Guest.Summary.当前拥有炮台.Count == 0, "queued guest transfer preserves upgraded machine in reverse direction");
                check(!Harness.Host.Transfers.Locked && !Harness.Guest.Transfers.Locked, "queued commits release both locks");
            }
        }
        finally { Harness.QueueTransport = false; Harness.Transport.Clear(); Harness.DeliverTransfers = true; Harness.Host = oldHost; Harness.Guest = oldGuest; }
    }
}
