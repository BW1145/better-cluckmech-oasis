using System;
using System.Collections.Generic;
using CluckmechCoop;

static class Program
{
    static int checks;
    static void Check(bool condition, string title) { if (!condition) throw new Exception(title); checks++; }
    static void Main()
    {
        var host = new Wallet { Gold = 600, Machines = new List<Machine> { new Machine { Id = 12, UpgradeLimit = 7, Upgrades = new List<int> { 4, 9, 9 } }, new Machine { Id = 12, UpgradeLimit = 3, Upgrades = new List<int> { 2 } } } };
        var guest = new Wallet { Gold = 90 };
        Check(!Rules.GiveGold(-1,90,1,1).Success && !Rules.GiveGold(90,-1,1,1).Success,"negative server balances are rejected");
        Check(!Rules.Give(null,guest,null).Success,"null transaction state cannot crash rules");
        Check(!Rules.Give(new Wallet{Machines=null},guest,new Transfer()).Success,"null inventory cannot crash rules");
        Check(!Rules.Give(new Wallet{Machines=new List<Machine>{null}},guest,new Transfer()).Success,"null inventory entries cannot crash rules");
        Check(!Rules.SameMachine(12,2,null,new Machine{Id=12,UpgradeLimit=2})&&!Rules.SameMachine(new Machine{Id=12},new Machine{Id=12,Upgrades=null}),"null upgrades compare safely");
        Check(!Rules.GiveGold(1,int.MaxValue,1,1).Success,"receiver overflow is rejected");
        var gold = Rules.Give(host, guest, new Transfer { Sender = 1, Kind = "gold", Amount = 250 });
        Check(gold.Success && gold.Host.Gold == 350 && gold.Guest.Gold == 340, "host gives gold");
        Check(gold.Host.Gold + gold.Guest.Gold == host.Gold + guest.Gold, "gold conserved");
        Check(host.Gold == 600 && guest.Gold == 90, "input remains uncommitted");
        var returned = Rules.Give(gold.Host, gold.Guest, new Transfer { Sender = 2, Kind = "gold", Amount = 100 });
        Check(returned.Success && returned.Host.Gold == 450 && returned.Guest.Gold == 240, "guest gives gold");
        foreach (int bad in new[] { -1, 0, 601 })
        {
            var rejected = Rules.Give(host, guest, new Transfer { Sender = 1, Kind = "gold", Amount = bad });
            Check(!rejected.Success && rejected.Host.Gold == 600 && rejected.Guest.Gold == 90, "invalid amount unchanged");
        }
        var machine = Rules.Give(host, guest, new Transfer { Sender = 1, Kind = "machine", Index = 0, Expected = host.Machines[0].Copy() });
        Check(machine.Success && machine.Host.Machines.Count == 1 && machine.Guest.Machines.Count == 1, "machine conserved");
        Check(Rules.SameMachine(machine.Guest.Machines[0], host.Machines[0]), "all upgrades including duplicates preserved");
        Check(Rules.SameMachine(machine.Host.Machines[0], host.Machines[1]), "same type with different upgrades remains distinct");
        machine.Guest.Machines[0].Upgrades.Add(888);
        Check(host.Machines[0].Upgrades.Count == 3, "transferred upgrades do not alias source");
        var stale = Rules.Give(host, guest, new Transfer { Sender = 1, Kind = "machine", Index = 1, Expected = host.Machines[0].Copy() });
        Check(!stale.Success && stale.Host.Machines.Count == 2 && stale.Guest.Machines.Count == 0, "changed selection rejected");
        var plan = new EncounterPlan<string>();
        var originals = new List<string> { "crawler", "flyer" };
        plan.Record(0, 0, originals, 3, false, 2, x => x == "oasis");
        plan.Record(0, 0, new[] { "extra" }, 2, false, 2, x => x == "oasis");
        Check(!plan.TryStart(false), "clearing an early native stage never starts repeated enemies");
        plan.Record(1, 0, new[] { "ordinary-boss", "escort" }, 1, true, 4, x => x == "oasis");
        plan.Record(2, 0, new[] { "oasis", "final-escort" }, 1, true, 5, x => x == "oasis");
        Check(plan.Stages.Count == 3 && plan.Stages[0].Groups.Count == 2, "native stages and extra difficulty groups retained in order");
        Check(plan.Stages[0].Groups[0].Count == 3 && plan.Stages[0].Groups[0].Units[0] == "crawler" && plan.Stages[0].Groups[0].Units[1] == "flyer", "spawn quantities and prefab order identical");
        Check(plan.Stages[1].Groups[0].Boss && plan.Stages[1].Groups[0].Units[0] == "ordinary-boss", "ordinary boss retained");
        Check(plan.Stages[2].Groups[0].Units.Count == 1 && plan.Stages[2].Groups[0].Units[0] == "final-escort", "only oasis boss omitted, escorts retained");
        originals.Clear();
        Check(plan.Stages[0].Groups[0].Units.Count == 2, "recorded prefab list is an independent snapshot");
        plan.OriginalClosed = true;
        Check(!plan.TryStart(true), "every remaining original enemy must die before repetition");
        Check(plan.TryStart(false) && plan.Replay, "entire original encounter cleared starts repetition");
        Check(!plan.TryStart(false), "repeated spawn sequence starts only once");
        plan.Record(3, 0, new[] { "crawler" }, 99, false, 0, x => false);
        Check(plan.Stages.Count == 3, "replayed spawns cannot record themselves again");
        plan.Complete();
        Check(plan.Done && !plan.Replay && !plan.TryStart(false), "completion cannot trigger another repetition");
        var oasisOnly = new EncounterPlan<string>();
        oasisOnly.Record(0, 0, new[] { "oasis" }, 1, true, 0, x => x == "oasis");
        oasisOnly.OriginalClosed = true;
        Check(oasisOnly.Stages.Count == 0 && !oasisOnly.TryStart(false), "oasis-only encounter never duplicates its boss");
        Check(Rules.HealthMultiplier(false, false) == 1 && Rules.AttackMultiplier(false, false) == 1, "original enemies and ordinary bosses retain native stats");
        Check(Rules.HealthMultiplier(false, true) == 1.5f && Rules.AttackMultiplier(false, true) == 2, "repeated enemies and ordinary bosses HP x1.5 ATK x2");
        Check(Rules.HealthMultiplier(true, false) == 10 && Rules.AttackMultiplier(true, false) == 2, "oasis boss HP x10 ATK x2");
        Check(Rules.HealthMultiplier(true, true) == 10 && Rules.AttackMultiplier(true, true) == 2, "oasis multiplier cannot stack");
        var timedStage = new EncounterStage<string> { Limit = 20 };
        Check(!EncounterPlan<string>.StageFinished(timedStage, true, false, 19), "living native stage stays active before timeout");
        Check(EncounterPlan<string>.StageFinished(timedStage, true, false, 20), "native stage timeout still permits next scheduled enemies");
        Check(EncounterPlan<string>.StageFinished(timedStage, false, false, 1), "cleared native stage advances early");
        var bossStage = new EncounterStage<string> { BossWave = true, InfiniteBoss = true, Limit = 1 };
        Check(!EncounterPlan<string>.StageFinished(bossStage, true, true, 1000), "native infinite boss time remains unlimited");
        Check(EncounterPlan<string>.StageFinished(bossStage, true, false, 1), "native boss death advances its stage while escorts remain tracked globally");
        var waitStage = new EncounterStage<string> { Wait = 3, Limit = 20 };
        Check(EncounterPlan<string>.NextStageDelay(waitStage, false, 5) == 3, "next stage inherits completed stage waiting time");
        Check(EncounterPlan<string>.NextStageDelay(waitStage, false, 20) == 3, "native timeout resets countdown to completed stage waiting time");
        var finiteBossStage = new EncounterStage<string> { BossWave = true, Wait = 3, Limit = 20 };
        Check(EncounterPlan<string>.NextStageDelay(finiteBossStage, false, 5) == 15, "native boss death carries remaining countdown");
        Check(EncounterPlan<string>.NextStageDelay(finiteBossStage, false, 20) == 3, "timeout has precedence over boss death");
        Check(EncounterPlan<string>.NextStageDelay(bossStage, false, 1000) == 1, "untimed boss retains native countdown");
        var reward = new DeferredOnce<string>();
        reward.Capture("original-boss-talent");
        Check(reward.Take(false) == null, "original boss death cannot release encounter rewards");
        reward.Capture("repeated-boss-talent");
        Check(reward.Take(true) == "original-boss-talent", "repeated boss death shares the original single reward");
        Check(reward.Take(true) == null, "duplicate completion cannot release reward twice");
        reward.Capture("late-boss-message");
        Check(reward.Take(true) == null, "late death messages cannot reopen completed reward");
        var lateReward = new DeferredOnce<string>();
        Check(lateReward.Take(true) == null, "completion before a client death message keeps reward pending");
        lateReward.Capture("client-talent");
        Check(lateReward.Take(true) == "client-talent" && lateReward.Take(true) == null, "late client death delivers its one reward");
        var leanGold = Rules.GiveGold(600, 90, 1, 250);
        Check(leanGold.Success && leanGold.Host == 350 && leanGold.Guest == 340, "gold-only transfer matches wallet transaction");
        var leanReturn = Rules.GiveGold(leanGold.Host, leanGold.Guest, 2, 100);
        Check(leanReturn.Success && leanReturn.Host == 450 && leanReturn.Guest == 240, "gold-only guest transfer conserves balances");
        foreach (int bad in new[] { -1, 0, 601 }) Check(!Rules.GiveGold(600, 90, 1, bad).Success, "gold-only invalid amount rejected");
        Check(!Rules.GiveGold(100, int.MaxValue, 1, 1).Success, "gold-only overflow rejected");
        Check(!Rules.GiveGold(100, 100, 0, 1).Success, "gold-only invalid sender rejected");
        for (int rewardId = 0; rewardId < 3; rewardId++)
        {
            var choice = new ExpeditionChoice { Enabled = true, RewardId = rewardId };
            Check(choice.TryReward() && choice.RewardApplied, "each native reward can be added once");
            Check(!choice.TryReward(), "repeated completion does not add another reward");
        }
        Check(!new ExpeditionChoice { Enabled = false, RewardId = 0 }.TryReward(), "unselected expedition adds no reward");
        Check(!new ExpeditionChoice { Enabled = true, RewardId = -1 }.TryReward(), "oasis final level adds no ordinary reward");
        Console.WriteLine($"PASS: {checks} checks");
    }
}
