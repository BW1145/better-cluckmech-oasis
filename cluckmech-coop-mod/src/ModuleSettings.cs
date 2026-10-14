using System;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using 函数库;

namespace CluckmechCoop
{
    internal static class ModuleSettings
    {
        private const string SaveKey = "CluckmechCoop.Expedition.v3";
        private const string ProgressKey = "CluckmechCoop.Progress.v4";
        // Keep failed addon snapshots read-only across menu transitions and autosaves.
        private static readonly System.Collections.Generic.HashSet<string> protectedSaves = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        private static ConfigEntry<bool> expeditionPreference, transferPreference;
        private static readonly System.Collections.Generic.Dictionary<ModuleId, ConfigEntry<bool>> preferences = new System.Collections.Generic.Dictionary<ModuleId, ConfigEntry<bool>>();
        internal static GameplayModules Extra = new GameplayModules();
        internal static RunProgress Progress = new RunProgress();
        internal static bool Music;
        internal static PlayerProgress LocalProgress => Progress.Player(Plugin.Role);
        internal static bool Preference(ModuleId id) => preferences[id].Value;
        internal static void Preference(ModuleId id, bool value) => preferences[id].Value = value;
        internal static bool InRun, Expedition, Transfer;
        internal static ExpeditionChoice Choice;
        private static 数据库_关卡奖励 rewards;
        private static 奖励列表脚本 rewardList;
        internal static void Initialize(ConfigFile config)
        {
            expeditionPreference = config.Bind("Modules", "Expedition", true, "强化远征");
            transferPreference = config.Bind("Modules", "Transfer", true, "队友转交");
            foreach (var info in ModuleCatalog.All)
                preferences[info.Id] = info.Id == ModuleId.Expedition ? expeditionPreference : info.Id == ModuleId.Transfer ? transferPreference :
                    config.Bind("Modules", info.Id.ToString(), true, info.Description);
        }
        internal static bool ExpeditionPreference { get => expeditionPreference.Value; set => expeditionPreference.Value = value; }
        internal static bool TransferPreference { get => transferPreference.Value; set => transferPreference.Value = value; }
        internal static void EnterRun()
        {
            if (InRun) return;
            InRun = true;
            Expedition = ExpeditionPreference;
            Transfer = TransferPreference;
            Music = Preference(ModuleId.Music);
            Extra = new GameplayModules { Speed = Preference(ModuleId.Speed), Characters = Preference(ModuleId.Characters), Energy = Preference(ModuleId.Energy), Growth = Preference(ModuleId.Growth), Shop = Preference(ModuleId.Shop), Stratagems = Preference(ModuleId.Stratagems) };
            if (Plugin.Role == 2) { Expedition = Transfer = false; Extra = new GameplayModules(); }
            else Plugin.Network.PublishSettings();
        }
        internal static void MainMenu()
        {
            InRun = Expedition = Transfer = false;
            Choice = null; Extra = new GameplayModules(); Progress = new RunProgress(); Music = false;
            ClearScene();
        }
        internal static void ClearScene() { rewards = null; rewardList = null; }
        internal static bool Active => InRun && Expedition && Choice != null && Choice.Enabled;
        internal static bool FinalLevel => Plugin.Spawner?.数据?.是否最终关卡 == true || Plugin.Summary?.地图存档?.当前场景名 == "绿洲";
        internal static string NodeKey
        {
            get
            {
                var map = Plugin.Summary?.地图存档;
                return map == null ? null : map.当前地图页 + ":" + map.当前节点名;
            }
        }
        internal static void Prepare()
        {
            EnterRun();
            if (!Expedition || Plugin.Role == 2 || NodeKey == null) return;
            string node = NodeKey;
            if (Choice != null && (Choice.Node == node || Choice.Node?.StartsWith(node + ":", StringComparison.Ordinal) == true))
            {
                Choice.Node = node;
                return;
            }
            Choice = new ExpeditionChoice { Node = node, Token = Guid.NewGuid().ToString("N"), RewardId = FinalLevel ? -1 : UnityEngine.Random.Range(0, 3) };
            Plugin.Summary.保存单局存档();
            Plugin.Network.PublishSettings();
        }
        internal static void Select(bool enabled)
        {
            if (Plugin.Role == 2 || Choice == null || Choice.Locked) return;
            Choice.Enabled = enabled;
            Plugin.Summary?.保存单局存档();
            Plugin.Network.PublishSettings();
        }
        internal static void Freeze()
        {
            Prepare();
            if (Choice != null) Choice.Locked = true;
        }
        internal static void BindRewards()
        {
            if (!Active) return;
            rewardList = UnityEngine.Object.FindObjectOfType<奖励列表脚本>();
            rewards = UnityEngine.Object.FindObjectOfType<数据库_关卡奖励>();
        }
        internal static void CompleteReward()
        {
            if (!Active || FinalLevel || !Choice.TryReward()) return;
            if (rewards == null || rewardList == null) { Choice.RewardApplied = false; Plugin.Log("Reward list missing at completion"); return; }
            rewardList.添加奖励数据(rewards.查询关卡奖励(Choice.RewardId));
        }
        internal static string RewardText => Choice == null || Choice.RewardId < 0 ? "" :
            Choice.RewardId == 0 ? "额外奖励：50金币" : Choice.RewardId == 1 ? "额外奖励：1动力" : "额外奖励：1台鸡械";
        internal static void SaveProgress()
        {
            if (Plugin.Role == 2) Plugin.Network.SendProgress(LocalProgress);
            else Plugin.Summary?.保存单局存档();
        }
        private static ES3Settings SaveSettings() => new ES3Settings(ES3.Location.Cache)
        {
            encryptionType = ES3.EncryptionType.AES, encryptionPassword = 常数类.存档_存档加密密码
        };
        internal static void Save(存档脚本 save)
        {
            if (Plugin.Role == 2 || !InRun || !RunSaveRepair.IsRunSlot(save)) return;
            string path = save.文件名 + "_m" + save.后缀;
            if (protectedSaves.Contains(path)) return;
            StratagemDeployment.Capture();
            ES3.Save(ProgressKey, JsonConvert.SerializeObject(Progress), path, SaveSettings());
            ES3.Save(SaveKey, JsonConvert.SerializeObject(Choice), path, SaveSettings());
        }
        internal static void Load(存档脚本 save)
        {
            if (Plugin.Role == 2 || !RunSaveRepair.IsRunSlot(save) || RunSaveRepair.Blocked) return;
            string path = save.文件名 + "_m" + save.后缀;
            bool failed = false;
            Progress = ReadAddon(path, ProgressKey, new RunProgress(), ValidProgress, false, ref failed);
            Choice = ReadAddon<ExpeditionChoice>(path, SaveKey, null, ValidChoice, true, ref failed);
            if (failed) protectedSaves.Add(path);
            else protectedSaves.Remove(path);
            EnterRun();
        }
        private static T ReadAddon<T>(string path, string key, T fallback, Func<JObject, T, bool> valid, bool allowNull, ref bool failed) where T : class
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
            if (!HasFields(data, JTokenType.Boolean, "EnergyUsed") || !(data["Players"] is JObject players) || progress.Players == null) return false;
            foreach (var entry in progress.Players)
            {
                var player = entry.Value;
                if (entry.Key < 0 || entry.Key > 2 || player == null || player.Coupons < 0 || player.Regions == null || player.RepairNodes == null || player.Shops == null || player.StratagemCooldowns == null) return false;
                foreach (var cooldown in player.StratagemCooldowns)
                    if (cooldown == null || string.IsNullOrWhiteSpace(cooldown.Node) || cooldown.Slot < 0 || cooldown.Slot >= 5 ||
                        !Rules.ValidMachine(cooldown.Machine) || !StratagemCatalog.IsUnit(cooldown.Machine.Id) || cooldown.Remaining < 0 ||
                        float.IsNaN(cooldown.Remaining) || float.IsInfinity(cooldown.Remaining)) return false;
                var fields = players[entry.Key.ToString(System.Globalization.CultureInfo.InvariantCulture)] as JObject;
                if (!(fields?["Regions"] is JArray regions) || !(fields["Shops"] is JObject shops)) return false;
                if (!HasFields(fields, JTokenType.Integer, "Coupons") ||
                    (fields["PendingRegion"]?.Type != JTokenType.String && fields["PendingRegion"]?.Type != JTokenType.Null)) return false;
                foreach (var region in regions)
                    if (region.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)region)) return false;
                if (fields["RepairNodes"] != null)
                {
                    if (!(fields["RepairNodes"] is JArray repairs)) return false;
                    foreach (var repair in repairs) if (repair.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)repair)) return false;
                }
                foreach (var shopEntry in player.Shops)
                {
                    var shop = shopEntry.Value;
                    if (string.IsNullOrWhiteSpace(shopEntry.Key) || shop == null || shop.Refreshes < 0 || shop.FourthPrice < 0 || shop.FourthId < -1 || shop.Sold == null || shop.Sold.Length != 4) return false;
                    var shopFields = shops[shopEntry.Key] as JObject;
                    if (!(shopFields?["Sold"] is JArray sold) || sold.Count != 4 ||
                        !HasFields(shopFields, JTokenType.Integer, "Refreshes", "FourthId", "FourthPrice") ||
                        !HasFields(shopFields, JTokenType.Boolean, "TicketBought")) return false;
                    foreach (var marker in sold) if (marker.Type != JTokenType.Boolean) return false;
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
            foreach (string name in names) if (data[name]?.Type != type) return false;
            return true;
        }
    }
    [HarmonyPatch(typeof(存档脚本), "保存")]
    internal static class SaveExpedition
    {
        private static bool Prefix(存档脚本 __instance)
        {
            if (!RunSaveRepair.CanSave(__instance)) return false;
            ModuleSettings.Save(__instance); return true;
        }
    }
    [HarmonyPatch(typeof(存档脚本), "读取")]
    internal static class LoadExpedition { private static void Postfix(存档脚本 __instance) => ModuleSettings.Load(__instance); }
}
