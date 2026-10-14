using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CluckmechCoop;
using Newtonsoft.Json;

static class ModuleSaveHarness
{
    private const string ProgressKey = "CluckmechCoop.Progress.v4", ChoiceKey = "CluckmechCoop.Expedition.v3";
    private static string Path(存档脚本 save) => save.文件名 + "_m" + save.后缀;
    private static string HealthyProgress() => JsonConvert.SerializeObject(new RunProgress
    {
        EnergyUsed = true,
        Players = new Dictionary<int, PlayerProgress> { [0] = new PlayerProgress
        {
            Coupons = 3, Regions = new HashSet<string> { "desert" }, PendingRegion = "ice",RepairNodes=new HashSet<string>{"page:repair"},
            Shops = new Dictionary<string, ShopProgress> { ["page:shop"] = new ShopProgress
            { Refreshes = 3, FourthId = 24, FourthPrice = 80, Sold = new[] { false, false, false, true } } }
        } }
    });
    private static string HealthyChoice() => JsonConvert.SerializeObject(new ExpeditionChoice
    { Node = "page:node", Token = "saved-token", RewardId = 2, Enabled = true, Locked = true });
    private static void Seed(存档脚本 save, object progress, object choice)
    {
        ModuleSettings.MainMenu(); ES3.ReadFailure = null; ES3.Writes = 0; Plugin.Warnings.Clear();
        ES3.Data.Clear();
        ES3.Data[Path(save) + "native-summary"] = "original run data";
        ES3.Data[Path(save) + ProgressKey] = progress; ES3.Data[Path(save) + ChoiceKey] = choice;
    }
    private static void Load(存档脚本 save) => typeof(LoadExpedition).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { save });
    private static bool SavePrefix(存档脚本 save) => (bool)typeof(SaveExpedition).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { save });
    private static void Preserved(存档脚本 save, Dictionary<string, object> before, Action<bool, string> check)
    {
        check(ES3.Writes == 0 && ES3.Data.Count == before.Count, "loading addons does not write storage");
        check(SavePrefix(save), "native save continues after addon fallback");
        bool same = ES3.Writes == 0 && ES3.Data.Count == before.Count;
        foreach (var pair in before) same &= ES3.Data.TryGetValue(pair.Key, out var value) && Equals(value, pair.Value);
        check(same, "autosave preserves original native and addon values");
        ModuleSettings.MainMenu(); ModuleSettings.EnterRun(); ModuleSettings.Save(save);
        check(ES3.Writes == 0, "menu and new-run initialization retain addon write protection");
    }
    internal static void Run(Action<bool, string> check)
    {
        int checks = 0;
        Action<bool, string> verify = (ok, name) => { check(ok, name); checks++; };
        using (Harness.Use(new Context(0)))
        {
            var save = Harness.Current.Save;
            foreach (string slot in new[] { "danju", "lianji" })
            {
                save.文件名 = slot; Harness.Current.Role = slot == "danju" ? 0 : 1;
                Seed(save, HealthyProgress(), HealthyChoice()); Load(save);
                var player = ModuleSettings.Progress.Player(0);
                verify(ModuleSettings.Progress.EnergyUsed && player.Coupons == 3 && player.Regions.Contains("desert") && player.Shops["page:shop"].Sold[3], "valid nested progress restored");
                verify(ModuleSettings.Choice.Token == "saved-token" && ModuleSettings.Choice.RewardId == 2 && ModuleSettings.Choice.Locked, "valid expedition restored");
                verify(Plugin.Warnings.Count == 0 && ES3.Writes == 0, "valid load is read-only and quiet");
                ModuleSettings.Save(save); verify(ES3.Writes == 2, "valid addons remain writable");
                verify(player.RepairNodes.Contains("page:repair")&&!player.ClaimRepair("page:repair"),"repair claim survives save and cannot grant twice");
                Seed(save,HealthyProgress().Replace("\"RepairNodes\":[\"page:repair\"],",""),HealthyChoice());Load(save);
                verify(ModuleSettings.Progress.Player(0).RepairNodes.Count==0&&ModuleSettings.Progress.Player(0).Coupons==3,"older progress keeps coupon balance and initializes repair records");
                Seed(save, HealthyProgress(), "null"); ES3.Data.Remove(Path(save) + ProgressKey); Load(save);
                verify(ModuleSettings.Progress.Players.Count == 0 && ModuleSettings.Choice == null && Plugin.Warnings.Count == 0, "native-only save and explicit null choice use safe defaults");
                ModuleSettings.Save(save); verify(ES3.Writes == 2, "absent addon data can be initialized normally");

                string[] badProgress = {
                    "{", "", "null", "[]", "{}", "{\"Players\":null}", "{\"Players\":[]}",
                    "{\"Players\":{\"0\":null}}", "{\"Players\":{\"0\":{\"Coupons\":-1}}}",
                    "{\"Players\":{\"0\":{\"Coupons\":\"bad\"}}}", "{\"Players\":{\"0\":{\"Coupons\":2147483648}}}",
                    "{\"Players\":{\"3\":{}}}", "{\"Players\":{\"0\":{\"Regions\":null,\"Shops\":{}}}}",
                    "{\"Players\":{\"0\":{\"Regions\":[],\"Shops\":null}}}",
                    "{\"Players\":{\"0\":{\"Regions\":[],\"Shops\":{\"shop\":null}}}}",
                    HealthyProgress().Replace("\"Sold\":[false,false,false,true]", "\"Sold\":null"),
                    HealthyProgress().Replace("[false,false,false,true]", "[true]"),
                    HealthyProgress().Replace("\"Refreshes\":3", "\"Refreshes\":-1"),
                    HealthyProgress().Replace("\"FourthPrice\":80", "\"FourthPrice\":-1"),
                    HealthyProgress().Replace("\"FourthId\":24", "\"FourthId\":-2"),
                    HealthyProgress().Replace("\"Regions\":[\"desert\"],", ""),
                    HealthyProgress().Replace("\"Shops\":", "\"FutureShops\":"),
                    HealthyProgress().Replace("\"Sold\":[false,false,false,true],", ""),
                    HealthyProgress().Replace("\"EnergyUsed\":true,", ""),
                    HealthyProgress().Replace("\"Coupons\":3,", ""),
                    HealthyProgress().Replace("\"PendingRegion\":\"ice\",", ""),
                    HealthyProgress().Replace("\"FourthId\":24,", ""),
                    HealthyProgress().Replace("\"FourthPrice\":80,", ""),
                    HealthyProgress().Replace("\"TicketBought\":false,", ""),
                    HealthyProgress().Replace("\"EnergyUsed\":true", "\"EnergyUsed\":\"true\""),
                    HealthyProgress().Replace("\"Coupons\":3", "\"Coupons\":\"3\""),
                    HealthyProgress().Replace("\"Refreshes\":3", "\"Refreshes\":3.5"),
                    HealthyProgress() + " {}", "{\"Players\":{},\"FutureState\":true}"
                };
                foreach (string json in badProgress)
                {
                    Seed(save, json, HealthyChoice()); var before = new Dictionary<string, object>(ES3.Data); Load(save);
                    verify(ModuleSettings.InRun && ModuleSettings.Progress.Players.Count == 0 && ModuleSettings.Choice.Token == "saved-token", "bad progress falls back independently and continuation returns");
                    verify(Plugin.Warnings.Count == 1 && Plugin.Warnings[0].Contains(ProgressKey), "bad progress warning identifies key");
                    Preserved(save, before, verify);
                }
                string[] badChoice = { "{", "", "[]", "{}", "{\"Node\":\"page:node\"}",
                    HealthyChoice().Replace("\"RewardId\":2", "\"RewardId\":9"),
                    HealthyChoice().Replace("saved-token", ""), HealthyChoice().Replace("page:node", ""),
                    HealthyChoice().Replace("\"Enabled\":true", "\"Enabled\":{}"),
                    HealthyChoice().Replace("\"RewardId\":2,", ""),
                    HealthyChoice().Replace("\"Enabled\":true,", ""),
                    HealthyChoice().Replace("\"Locked\":true,", ""),
                    HealthyChoice().Replace(",\"RewardApplied\":false", ""),
                    HealthyChoice().Replace("\"Enabled\":true", "\"Enabled\":\"true\""),
                    HealthyChoice() + " false" };
                foreach (string json in badChoice)
                {
                    Seed(save, HealthyProgress(), json); var before = new Dictionary<string, object>(ES3.Data); Load(save);
                    verify(ModuleSettings.InRun && ModuleSettings.Progress.EnergyUsed && ModuleSettings.Choice == null, "bad expedition falls back independently");
                    verify(Plugin.Warnings.Count == 1 && Plugin.Warnings[0].Contains(ChoiceKey), "bad expedition warning identifies key");
                    Preserved(save, before, verify);
                }
                foreach (string operation in new[] { "exists", "load" })
                foreach (string key in new[] { ProgressKey, ChoiceKey })
                {
                    Seed(save, HealthyProgress(), HealthyChoice()); var before = new Dictionary<string, object>(ES3.Data);
                    ES3.ReadFailure = (op, name) => op == operation && name == key ? new IOException("storage fixture") : null;
                    Load(save); ES3.ReadFailure = null;
                    verify(ModuleSettings.InRun && Plugin.Warnings.Count == 1, "ES3 read and key lookup exceptions contained");
                    Preserved(save, before, verify);
                }
                Seed(save, 123, HealthyChoice()); var wrongType = new Dictionary<string, object>(ES3.Data); Load(save);
                verify(Plugin.Warnings.Count == 1, "non-string stored addon contained"); Preserved(save, wrongType, verify);
                Seed(save, "{", "{"); Load(save);
                verify(Plugin.Warnings.Count == 2 && ModuleSettings.Choice == null && ModuleSettings.Progress.Players.Count == 0, "both invalid keys fall back independently");
                Seed(save, HealthyProgress(), HealthyChoice()); Load(save); ModuleSettings.Save(save);
                verify(ES3.Writes == 2, "successful reread of repaired data restores addon writes");

                Seed(save, "{", HealthyChoice()); Load(save);
                var other = new 存档脚本 { 文件名 = slot, 后缀 = ".other" }; ModuleSettings.Save(other);
                verify(ES3.Writes == 2, "write protection belongs to exact slot and suffix");
                ModuleSettings.Save(save); verify(ES3.Writes == 2, "saving another slot retains original protection");
            }
            Seed(save, "{", "{"); Harness.Current.Role = 2; Load(save); ModuleSettings.Save(save);
            verify(Plugin.Warnings.Count == 0 && ES3.Writes == 0 && !ModuleSettings.InRun, "guest keeps native path");
            Harness.Current.Role = 0; save.文件名 = "quanju"; Load(save); ModuleSettings.Save(save);
            verify(Plugin.Warnings.Count == 0 && ES3.Writes == 0 && !ModuleSettings.InRun, "global slot stays isolated");
            save.文件名 = "danju"; RunSaveRepair.Blocked = true; Load(save); RunSaveRepair.Blocked = false;
            verify(!ModuleSettings.InRun && Plugin.Warnings.Count == 0, "failed core continuation gate retained");
        }
        Console.WriteLine($"PASS: {checks} addon-save regression checks using production load/save patches and ES3 storage doubles");
    }
}
