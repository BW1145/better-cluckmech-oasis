using System.Collections.Generic;
using BepInEx.Configuration;

namespace CluckmechCoop
{
    // Plugin-lifetime configuration. A run reads these values only on first entry.
    internal sealed class ModulePreferences
    {
        private readonly Dictionary<ModuleId, ConfigEntry<bool>> entries = new Dictionary<ModuleId, ConfigEntry<bool>>();

        internal void Bind(ConfigFile config)
        {
            foreach (var info in ModuleCatalog.All)
            {
                string description = info.Id == ModuleId.Expedition ? "强化远征" :
                    info.Id == ModuleId.Transfer ? "队友转交" : info.Description;
                entries[info.Id] = config.Bind("Modules", info.Id.ToString(), true, description);
            }
        }

        internal bool Get(ModuleId id) => entries[id].Value;
        internal void Set(ModuleId id, bool value) => entries[id].Value = value;
    }
}
