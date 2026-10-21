using System;
using System.Collections.Generic;
using CluckmechCoop;
using Newtonsoft.Json;

static class Program {
 private static int checks;
 private static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
 static void Main(){
        Harness.Host.Summary.当前金钱数 = 600; Harness.Guest.Summary.当前金钱数 = 90;
        Harness.Host.Summary.当前拥有炮台.Add(new 总结脚本.背包单元 { 炮台id = 12, 强化上限 = 7, 强化词条 = new List<int> { 4, 9, 9 } });
        Harness.Host.Summary.当前携带炮台.Add(Harness.Host.Summary.当前拥有炮台[0]);
        Steam联机管理器.玩家_主机.Network总结数据_联机.当前拥有炮台 = Harness.Host.Summary.当前拥有炮台;
        int messagesBefore = Harness.Messages.Count;
        Plugin.Transfers.Request("gold", 250, -1);
        Check(Harness.Host.Summary.当前金钱数 == 350 && Harness.Guest.Summary.当前金钱数 == 340, "production gold transfer commits both balances");
        Check(Harness.Host.InventoryRefreshes == 0 && Harness.Guest.InventoryRefreshes == 0 && Harness.Host.Uploads == 1 && Harness.Guest.Uploads == 1, "gold transfer refreshes native server balances once without rebuilding mechanical UI");
        Check(ReferenceEquals(Steam联机管理器.玩家_主机.Network总结数据_联机.当前拥有炮台, Harness.Host.Summary.当前拥有炮台), "gold native summary retains mechanical list reference");
        string commit = null;
        foreach (var msg in Harness.Messages.GetRange(messagesBefore, Harness.Messages.Count - messagesBefore))
        {
            var payload = Newtonsoft.Json.Linq.JObject.Parse(msg.Json);
            Check(payload["Wallet"] == null && payload["Result"] == null && !msg.Json.Contains("Machines"), "gold message contains no machine wallet");
            if ((string)payload["Type"] == "commit") commit = msg.Json;
        }
        using (Harness.Use(Harness.Guest)) Plugin.Transfers.Receive(TransferService.ReplyKey, commit);
        Check(Harness.Guest.Summary.当前金钱数 == 340, "duplicate gold commit ignored");
        using (Harness.Use(Harness.Guest)) Plugin.Transfers.Request("gold", 100, -1);
        Check(Harness.Host.Summary.当前金钱数 == 450 && Harness.Guest.Summary.当前金钱数 == 240, "production guest gold transfer commits both balances");
        Plugin.Transfers.Request("machine", 0, 0, TransferService.Snapshot(Harness.Host.Summary.当前拥有炮台[0]));
        Check(Harness.Host.Summary.当前拥有炮台.Count == 0 && Harness.Guest.Summary.当前拥有炮台.Count == 1, "production mechanical transfer conserves inventory");
        var transferred = Harness.Guest.Summary.当前拥有炮台[0];
        Check(transferred.强化上限 == 7 && transferred.强化词条.Count == 3 && transferred.强化词条[2] == 9, "production transfer preserves every upgrade");
        Check(Harness.Host.Summary.当前携带炮台[0].炮台id == 0, "removed carried mechanical selection cleared");
        Check(Harness.Host.InventoryRefreshes == 1 && Harness.Guest.InventoryRefreshes == 1, "mechanical transfer refreshes only mechanical changes");
        var first = new 总结脚本.背包单元 { 炮台id = 20, 强化上限 = 4, 强化词条 = new List<int> { 2 } };
        var chosen = new 总结脚本.背包单元 { 炮台id = 20, 强化上限 = 4, 强化词条 = new List<int> { 3, 3 } };
        Harness.Host.Summary.当前拥有炮台.Add(first);
        Harness.Host.Summary.当前拥有炮台.Add(chosen);
        var visible = TransferService.Snapshot(chosen);
        messagesBefore = Harness.Messages.Count;
        Plugin.Transfers.Request("machine", 0, 0, visible);
        Check(Harness.Messages.Count == messagesBefore && !Plugin.Transfers.Locked, "displayed machine mismatch rejected before network traffic");
        Check(Harness.Host.Summary.当前拥有炮台.Count == 2 && Harness.Guest.Summary.当前拥有炮台.Count == 1, "mismatch changes neither inventory");
        Check(Plugin.Transfers.Status.Contains("背包已变化"), "mismatch reports inventory change");
        Plugin.Transfers.Request("machine", 0, 1);
        Check(Harness.Messages.Count == messagesBefore && !Plugin.Transfers.Locked, "machine request requires displayed snapshot");
        chosen.强化词条[0] = 8;
        Plugin.Transfers.Request("machine", 0, 1, visible);
        Check(Harness.Messages.Count == messagesBefore, "same id and upgrade count with changed affix rejected");
        chosen.强化词条[0] = 3;
        Harness.Host.Summary.当前拥有炮台.Reverse();
        Plugin.Transfers.Request("machine", 0, Harness.Host.Summary.当前拥有炮台.IndexOf(chosen), visible);
        Check(Harness.Host.Summary.当前拥有炮台.Count == 1 && Harness.Host.Summary.当前拥有炮台[0].强化词条[0] == 2, "reordered inventory transfers selected upgraded copy only");
        var last = Harness.Guest.Summary.当前拥有炮台[1];
        Check(last.炮台id == 20 && last.强化词条.Count == 2 && last.强化词条[0] == 3 && last.强化词条[1] == 3, "same-name machines remain distinguished by full affix data");
        Check(visible.Upgrades[0] == 3, "request preserves displayed snapshot");

        Harness.DeliverTransfers=false;
        Plugin.Transfers.Request("gold",10,-1);
        Check(Plugin.Transfers.Locked,"preparation waits when peer delivery is suspended");
        Plugin.Transfers.Disconnect();
        Check(!Plugin.Transfers.Locked&&Harness.Host.Instance.Tasks.Count==0,"disconnect releases transfer lock and timeout");
        using(Harness.Use(Harness.Guest))Plugin.Transfers.Disconnect();
        Harness.DeliverTransfers=true;
        int before=Plugin.Summary.当前金钱数;
        Plugin.Transfers.Request("gold",10,-1);
        Check(Plugin.Summary.当前金钱数==before-10,"new transfer succeeds after disconnect cleanup");
        ProtocolHarness.Run(Check);
        Console.WriteLine($"PASS: {checks} production transfer protocol checks using synchronous and queued two-peer transport doubles");
 }
}
