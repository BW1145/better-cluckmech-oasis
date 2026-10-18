namespace CluckmechCoop
{
    // Plugin owns this coordinator; scene data is released in one ordered path.
    // Run preferences/progress belong to ModuleSettings and survive a scene change.
    internal static class SceneLifecycle
    {
        internal static SceneLifetime Current { get; private set; }
        internal static bool IsCurrent(SceneLifetime scene) => scene != null && scene.Active && ReferenceEquals(Current,scene);

        internal static bool Enter(int handle)
        {
            if (Current != null && Current.Active && Current.Handle == handle) return false;
            Leave();
            Current = new SceneLifetime(handle);
            return true;
        }

        internal static void Leave()
        {
            if (Current == null || !Current.Close()) return;
            // Invalidate delayed work before destroying the objects it references.
            Plugin.InCombat = false;
            Plugin.Network?.ClearScene();
            UnitLifecycle.ClearScene();
            Plugin.Combat?.ClearScene();
            GrowthPanel.Close();
            MapBoonsUi.Clear();
            FeatureModal.Clear();
            Difficulty.Reset();
            ModuleSettings.ClearScene();
            CharacterBoons.Started = false;
            StratagemWorld.Clear();
            KamuiWorld.Clear();
            OtherworldWorld.Clear();
            OtherworldDamageStatistics.Clear();
            OtherworldDeployment.ClearScene();
            SwallowTargets.Clear();
        }
    }
}
