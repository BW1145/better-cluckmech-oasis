namespace CluckmechCoop
{
    // One run owns its module snapshot, expedition choice and mutable progression.
    // Configuration and failed-save protection have longer, separate lifetimes.
    internal sealed class ModuleRunState
    {
        internal bool InRun { get; private set; }
        internal bool Expedition { get; private set; }
        internal bool Transfer { get; private set; }
        internal bool Music { get; private set; }
        internal GameplayModules Extra { get; private set; } = new GameplayModules();
        internal RunProgress Progress { get; private set; } = new RunProgress();
        internal ExpeditionChoice Choice { get; private set; }

        internal bool Enter(ModulePreferences preferences, int role)
        {
            if (InRun) return false;
            InRun = true;
            Expedition = role != 2 && preferences.Get(ModuleId.Expedition);
            Transfer = role != 2 && preferences.Get(ModuleId.Transfer);
            Music = preferences.Get(ModuleId.Music);
            Extra = role == 2 ? new GameplayModules() : new GameplayModules
            {
                Speed = preferences.Get(ModuleId.Speed),
                Characters = preferences.Get(ModuleId.Characters),
                Energy = preferences.Get(ModuleId.Energy),
                Growth = preferences.Get(ModuleId.Growth),
                Shop = preferences.Get(ModuleId.Shop),
                Stratagems = preferences.Get(ModuleId.Stratagems),
                Boons = preferences.Get(ModuleId.Boons),
                Wealth = preferences.Get(ModuleId.Wealth),
                Otherworld = preferences.Get(ModuleId.Otherworld)
            };
            return true;
        }

        internal void Reset()
        {
            InRun = false;
            Expedition = false;
            Transfer = false;
            Music = false;
            Choice = null;
            Extra = new GameplayModules();
            Progress = new RunProgress();
        }

        internal void Restore(RunProgress progress, ExpeditionChoice choice)
        {
            Progress = progress;
            Choice = choice;
        }

        internal void ApplyHostSettings(bool expedition, bool transfer, ExpeditionChoice choice,
            GameplayModules modules, RunProgress progress)
        {
            if (choice != null)
            {
                bool same = Choice != null && Choice.Token == choice.Token;
                choice.RewardApplied = same && Choice.RewardApplied;
                if (same) choice.Locked |= Choice.Locked;
            }
            Expedition = expedition;
            Transfer = transfer;
            Choice = choice;
            Extra = modules ?? new GameplayModules();
            Progress = progress ?? new RunProgress();
        }

        internal void SetPlayerProgress(int role, PlayerProgress progress) => Progress.Players[role] = progress;

        // Compatibility writes are kept at the facade boundary while callers migrate.
        internal void SetInRun(bool value) => InRun = value;
        internal void SetExpedition(bool value) => Expedition = value;
        internal void SetTransfer(bool value) => Transfer = value;
        internal void SetMusic(bool value) => Music = value;
        internal void SetExtra(GameplayModules value) => Extra = value;
        internal void SetProgress(RunProgress value) => Progress = value;
        internal void SetChoice(ExpeditionChoice value) => Choice = value;
    }
}
