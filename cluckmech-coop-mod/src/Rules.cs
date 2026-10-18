using System;
using System.Collections.Generic;

namespace CluckmechCoop
{
    public sealed class ExpeditionChoice
    {
        public string Node, Token;
        public int RewardId = -1;
        public bool Enabled, Locked, RewardApplied;
        public bool TryReward()
        {
            if (!Enabled || RewardApplied || RewardId < 0 || RewardId > 2) return false;
            RewardApplied = true;
            return true;
        }
    }
    public sealed class GoldResult
    {
        public int Host, Guest;
        public string Error;
        public bool Success => Error == null;
    }
    public sealed class Machine
    {
        public int Id;
        public int UpgradeLimit;
        public List<int> Upgrades = new List<int>();
        public Machine Copy() => new Machine { Id = Id, UpgradeLimit = UpgradeLimit, Upgrades = new List<int>(Upgrades) };
    }

    public sealed class Wallet
    {
        public int Gold;
        public List<Machine> Machines = new List<Machine>();
        public Wallet Copy() => new Wallet { Gold = Gold, Machines = Machines.ConvertAll(x => x.Copy()) };
    }

    public sealed class Transfer
    {
        public string Id;
        public int Sender;
        public string Kind;
        public int Amount;
        public int Index;
        public Machine Expected;
    }

    public sealed class TransferResult
    {
        public Wallet Host;
        public Wallet Guest;
        public string Error;
        public bool Success => Error == null;
    }

    public static class Rules
    {
        public static GoldResult GiveGold(int host, int guest, int sender, int amount)
        {
            var result = new GoldResult { Host = host, Guest = guest };
            int source = sender == 1 ? host : guest, target = sender == 1 ? guest : host;
            if (host < 0 || guest < 0) result.Error = "金币状态无效";
            else if (sender != 1 && sender != 2) result.Error = "玩家已离开房间";
            else if (amount <= 0 || amount > source) result.Error = "金币不足，请重新输入金额";
            else if ((long)target + amount > int.MaxValue) result.Error = "队友金币已达到上限";
            else if (sender == 1) { result.Host -= amount; result.Guest += amount; }
            else { result.Guest -= amount; result.Host += amount; }
            return result;
        }
        public static float HealthMultiplier(bool finalBoss, bool replay) => finalBoss ? 10 : replay ? 2 : 1;
        public static float AttackMultiplier(bool finalBoss, bool replay) => finalBoss || replay ? 2 : 1;
        public static bool SameMachine(Machine a, Machine b)
        {
            return a != null && SameMachine(a.Id, a.UpgradeLimit, a.Upgrades, b);
        }
        public static bool SameMachine(int id, int limit, IList<int> upgrades, Machine expected)
        {
            if (expected == null || upgrades == null || expected.Upgrades == null || id != expected.Id || limit != expected.UpgradeLimit || upgrades.Count != expected.Upgrades.Count) return false;
            for (int i = 0; i < upgrades.Count; i++) if (upgrades[i] != expected.Upgrades[i]) return false;
            return true;
        }

        public static bool ValidMachine(Machine machine) => machine != null && machine.Id > 0 && machine.UpgradeLimit >= 0 && machine.Upgrades != null;
        public static bool ValidWallet(Wallet wallet) => wallet != null && wallet.Gold >= 0 && wallet.Machines != null && wallet.Machines.TrueForAll(ValidMachine);
        public static bool SameWallet(Wallet a, Wallet b)
        {
            if (!ValidWallet(a) || !ValidWallet(b) || a.Gold != b.Gold || a.Machines.Count != b.Machines.Count) return false;
            for (int i = 0; i < a.Machines.Count; i++) if (!SameMachine(a.Machines[i], b.Machines[i])) return false;
            return true;
        }
        public static bool ValidTransfer(Transfer transfer) => transfer != null && (transfer.Sender == 1 || transfer.Sender == 2) &&
            (transfer.Kind == "gold" ? transfer.Amount > 0 : transfer.Kind == "machine" && transfer.Index >= 0 && ValidMachine(transfer.Expected));
        public static bool SameTransfer(Transfer a, Transfer b) => ValidTransfer(a) && ValidTransfer(b) && a.Id == b.Id && a.Sender == b.Sender &&
            a.Kind == b.Kind && (a.Kind == "gold" ? a.Amount == b.Amount : a.Index == b.Index && SameMachine(a.Expected, b.Expected));

        public static TransferResult Give(Wallet host, Wallet guest, Transfer transfer)
        {
            if (!ValidWallet(host) || !ValidWallet(guest) || transfer == null) return new TransferResult { Error = "转交状态无效" };
            var result = new TransferResult { Host = host.Copy(), Guest = guest.Copy() };
            var source = transfer.Sender == 1 ? result.Host : result.Guest;
            var target = transfer.Sender == 1 ? result.Guest : result.Host;
            if (transfer.Sender != 1 && transfer.Sender != 2) result.Error = "玩家已离开房间";
            else if (transfer.Kind == "gold")
            {
                if (transfer.Amount <= 0 || transfer.Amount > source.Gold) result.Error = "金币不足，请重新输入金额";
                else if ((long)target.Gold + transfer.Amount > int.MaxValue) result.Error = "队友金币已达到上限";
                else { source.Gold -= transfer.Amount; target.Gold += transfer.Amount; }
            }
            else if (transfer.Kind == "machine")
            {
                if (transfer.Index < 0 || transfer.Index >= source.Machines.Count || !SameMachine(source.Machines[transfer.Index], transfer.Expected))
                    result.Error = "背包已变化，请重新选择鸡械";
                else
                {
                    target.Machines.Add(source.Machines[transfer.Index].Copy());
                    source.Machines.RemoveAt(transfer.Index);
                }
            }
            else result.Error = "请选择金币或鸡械";
            return result;
        }
    }

    public sealed class DeferredOnce<T> where T : class
    {
        private T pending;
        private bool released;
        public void Capture(T value) { if (!released && pending == null) pending = value; }
        public T Take(bool completed)
        {
            if (!completed || released || pending == null) return null;
            released = true;
            var value = pending;
            pending = null;
            return value;
        }
    }

    public sealed class EncounterGroup<T>
    {
        public int Slot;
        public float Delay;
        public int Count;
        public bool Boss;
        public List<T> Units;
    }

    public sealed class EncounterStage<T>
    {
        public int Index;
        public float Wait, Limit;
        public bool BossWave, InfiniteBoss;
        public readonly List<EncounterGroup<T>> Groups = new List<EncounterGroup<T>>();
    }

    public sealed class EncounterPlan<T>
    {
        public readonly List<EncounterStage<T>> Stages = new List<EncounterStage<T>>();
        public bool OriginalClosed;
        public bool Replay;
        public bool Done;

        public void Record(int stage, int slot, IList<T> units, int count, bool boss, float delay, Predicate<T> finalBoss,
            float wait = 0, float limit = 0, bool bossWave = false, bool infiniteBoss = false)
        {
            if (OriginalClosed) return;
            var repeated = new List<T>();
            foreach (var unit in units) if (!finalBoss(unit)) repeated.Add(unit);
            if (repeated.Count == 0 || count <= 0) return;
            var item = Stages.Count == 0 ? null : Stages[Stages.Count - 1];
            if (item == null || item.Index != stage)
            {
                item = new EncounterStage<T> { Index = stage, Wait = wait, Limit = limit, BossWave = bossWave, InfiniteBoss = infiniteBoss };
                Stages.Add(item);
            }
            item.Groups.Add(new EncounterGroup<T> { Slot = slot, Delay = delay, Count = count, Boss = boss, Units = repeated });
        }

        public bool TryStart(bool hasLivingOriginalEnemies)
        {
            if (!OriginalClosed || hasLivingOriginalEnemies || Replay || Done || Stages.Count == 0) return false;
            Replay = true;
            return true;
        }

        public void Complete()
        {
            Done = true;
            Replay = false;
        }
        public static bool StageFinished(EncounterStage<T> stage, bool livingEnemies, bool livingBoss, float elapsed)
            => !livingEnemies || (stage.BossWave && !livingBoss) || (!(stage.BossWave && stage.InfiniteBoss) && elapsed >= stage.Limit);
        public static float NextStageDelay(EncounterStage<T> stage, bool livingBoss, float elapsed)
        {
            float remaining = stage.BossWave && stage.InfiniteBoss ? stage.Limit : Math.Max(0, stage.Limit - elapsed);
            if (remaining > 0 && stage.BossWave && !livingBoss) return remaining;
            return stage.Wait;
        }
    }
}
