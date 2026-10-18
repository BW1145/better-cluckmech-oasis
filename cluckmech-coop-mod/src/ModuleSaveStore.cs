using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using 函数库;

namespace CluckmechCoop
{
    internal sealed class ModuleSaveSnapshot
    {
        internal readonly RunProgress Progress;
        internal readonly ExpeditionChoice Choice;
        internal readonly OtherworldSave Characters;
        internal ModuleSaveSnapshot(RunProgress progress, ExpeditionChoice choice, OtherworldSave characters)
        {
            Progress = progress;
            Choice = choice;
            Characters = characters;
        }
    }

    // This owner lives for the plugin lifetime; resetting a run cannot unlock a damaged slot.
    internal sealed class ModuleSaveStore
    {
        private const string SaveKey = "CluckmechCoop.Expedition.v3";
        private const string ProgressKey = "CluckmechCoop.Progress.v4";
        private const string CharactersKey = "CluckmechCoop.Characters.v1";
        private readonly HashSet<string> protectedSaves = new HashSet<string>(StringComparer.Ordinal);

        private static ES3Settings SaveSettings() => new ES3Settings(ES3.Location.Cache)
        {
            encryptionType = ES3.EncryptionType.AES,
            encryptionPassword = 常数类.存档_存档加密密码
        };

        internal void Save(存档脚本 save, ModuleRunState run, int role)
        {
            if (role == 2 || !run.InRun || !RunSaveRepair.IsRunSlot(save)) return;
            string path = save.文件名 + "_m" + save.后缀;
            if (protectedSaves.Contains(path)) return;
            StratagemDeployment.Capture();
            ES3.Save(ProgressKey, JsonConvert.SerializeObject(run.Progress), path, SaveSettings());
            ES3.Save(SaveKey, JsonConvert.SerializeObject(run.Choice), path, SaveSettings());
            ES3.Save(CharactersKey, JsonConvert.SerializeObject(KamuiWorld.Save()), path, SaveSettings());
        }

        internal bool TryLoad(存档脚本 save, int role, out ModuleSaveSnapshot snapshot)
        {
            snapshot = null;
            if (role == 2 || !RunSaveRepair.IsRunSlot(save) || RunSaveRepair.Blocked) return false;
            string path = save.文件名 + "_m" + save.后缀;
            bool failed = false;
            var progress = ReadAddon(path, ProgressKey, new RunProgress(), ValidProgress, false, ref failed);
            var choice = ReadAddon<ExpeditionChoice>(path, SaveKey, null, ValidChoice, true, ref failed);
            var characters = ReadAddon(path, CharactersKey, new OtherworldSave(),
                (_, value) => KamuiWorld.Valid(value), false, ref failed);
            if (failed) protectedSaves.Add(path);
            else protectedSaves.Remove(path);
            snapshot = new ModuleSaveSnapshot(progress, choice, characters);
            return true;
        }

        private static T ReadAddon<T>(string path, string key, T fallback,
            Func<JObject, T, bool> valid, bool allowNull, ref bool failed) where T : class
        {
            try
            {
                var settings = SaveSettings();
                if (!ES3.KeyExists(key, path, settings)) return fallback;
                var token = JToken.Parse(ES3.Load<string>(key, path, settings));
                if (allowNull && token.Type == JTokenType.Null) return fallback;
                if (!(token is JObject data)) throw new JsonSerializationException("Expected an addon object");
                var serializer = JsonSerializer.Create(new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error,
                    TypeNameHandling = TypeNameHandling.None,
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                });
                var value = data.ToObject<T>(serializer);
                if (value == null || !valid(data, value)) throw new JsonSerializationException("Invalid addon structure or fields");
                return value;
            }
            catch (Exception error)
            {
                failed = true;
                Plugin.Warn($"[Save] Could not read {key} ({error.GetType().Name}); using defaults and preserving addon data. Native saving remains available.");
                return fallback;
            }
        }
        private static bool ValidProgress(JObject data, RunProgress progress)
        {
            if (!HasFields(data, JTokenType.Boolean, "EnergyUsed") ||
                !progress.ValidEnergy || !progress.ValidBoons ||
                !(data["Players"] is JObject players) || progress.Players == null) return false;
            foreach (var entry in progress.Players)
            {
                var player = entry.Value;
                if (entry.Key < 0 || entry.Key > 2 || player == null || player.Coupons < 0 ||
                    !player.ValidGrowthRewards() || !player.ValidArtisan || !player.ValidShop ||
                    player.Regions == null || player.RepairNodes == null ||
                    player.Shops == null || player.StratagemCooldowns == null) return false;
                foreach (var cooldown in player.StratagemCooldowns)
                {
                    if (cooldown == null || string.IsNullOrWhiteSpace(cooldown.Node) ||
                        cooldown.Slot < 0 || cooldown.Slot >= 5 + player.ExtraCarrySlots ||
                        !Rules.ValidMachine(cooldown.Machine) || !StratagemCatalog.IsUnit(cooldown.Machine.Id) ||
                        cooldown.Remaining < 0 ||
                        float.IsNaN(cooldown.Remaining) || float.IsInfinity(cooldown.Remaining)) return false;
                }
                var fields = players[entry.Key.ToString(System.Globalization.CultureInfo.InvariantCulture)] as JObject;
                if (!(fields?["Regions"] is JArray regions) || !(fields["Shops"] is JObject shops)) return false;
                if (!HasFields(fields, JTokenType.Integer, "Coupons") ||
                    (fields["PendingRegion"]?.Type != JTokenType.String && fields["PendingRegion"]?.Type != JTokenType.Null)) return false;
                foreach (var region in regions)
                {
                    if (region.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)region)) return false;
                }
                if (fields["RepairNodes"] != null)
                {
                    if (!(fields["RepairNodes"] is JArray repairs)) return false;
                    foreach (var repair in repairs)
                    {
                        if (repair.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)repair)) return false;
                    }
                }
                foreach (var shopEntry in player.Shops)
                {
                    var shop = shopEntry.Value;
                    if (string.IsNullOrWhiteSpace(shopEntry.Key) || shop == null || shop.Refreshes < 0 ||
                        shop.FourthPrice < 0 || shop.FourthId < -1 ||
                        shop.Sold == null || shop.Sold.Length != 4) return false;
                    var shopFields = shops[shopEntry.Key] as JObject;
                    if (!(shopFields?["Sold"] is JArray sold) || sold.Count != 4 ||
                        !HasFields(shopFields, JTokenType.Integer, "Refreshes", "FourthId", "FourthPrice") ||
                        !HasFields(shopFields, JTokenType.Boolean, "TicketBought")) return false;
                    foreach (var marker in sold)
                    {
                        if (marker.Type != JTokenType.Boolean) return false;
                    }
                }
            }
            return true;
        }
        private static bool ValidChoice(JObject data, ExpeditionChoice choice) =>
            data["Node"]?.Type == JTokenType.String && !string.IsNullOrWhiteSpace(choice.Node) &&
            data["Token"]?.Type == JTokenType.String && !string.IsNullOrWhiteSpace(choice.Token) &&
            data["RewardId"]?.Type == JTokenType.Integer && choice.RewardId >= -1 && choice.RewardId <= 2 &&
            HasFields(data, JTokenType.Boolean, "Enabled", "Locked", "RewardApplied");
        private static bool HasFields(JObject data, JTokenType type, params string[] names)
        {
            foreach (string name in names)
            {
                if (data[name]?.Type != type) return false;
            }
            return true;
        }
    }
}
