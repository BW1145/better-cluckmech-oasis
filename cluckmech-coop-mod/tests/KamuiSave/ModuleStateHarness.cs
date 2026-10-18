using System;
using System.Collections.Generic;
using CluckmechCoop;
using BepInEx.Configuration;
using Newtonsoft.Json;

static class ModuleStateHarness
{
    private static int checks;
    private static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
    private static void Preferences(bool enabled)
    {
        foreach(var info in ModuleCatalog.All)ModuleSettings.Preference(info.Id,enabled);
    }
    internal static void Run()
    {
        ModuleSettings.Initialize(new ConfigFile());
        for(int role=0;role<=2;role++)
        {
            Plugin.Role=role;ModuleSettings.MainMenu();Preferences(true);
            int published=Plugin.Network.Published;
            ModuleSettings.EnterRun();
            Check(ModuleSettings.InRun&&ModuleSettings.Music,"enter captures local music for every role");
            Check(ModuleSettings.Expedition==(role!=2)&&ModuleSettings.Transfer==(role!=2),"guest awaits host expedition/transfer");
            Check(ModuleSettings.Extra.Otherworld==(role!=2)&&ModuleSettings.Extra.Stratagems==(role!=2),"guest awaits host gameplay modules");
            Check(Plugin.Network.Published==published+(role==2?0:1),"only authority publishes once on entry");
            var progress=ModuleSettings.Progress;var modules=ModuleSettings.Extra;
            progress.Player(role).Coupons=4;
            ModuleSettings.Choice=new ExpeditionChoice{Node="page:node",Token="kept",Enabled=true,Locked=true};
            Preferences(false);ModuleSettings.EnterRun();
            Check(ModuleSettings.Music&&ModuleSettings.Expedition==(role!=2)&&ReferenceEquals(modules,ModuleSettings.Extra),"preferences cannot mutate an active run snapshot");
            Check(ReferenceEquals(progress,ModuleSettings.Progress)&&ModuleSettings.LocalProgress.Coupons==4&&ModuleSettings.Choice.Token=="kept","repeated entry preserves progress and choice");
            Check(Plugin.Network.Published==published+(role==2?0:1),"repeated entry does not republish");
            int cleared=OtherworldProgress.Cleared,applied=CarryCapacity.Applied;
            ModuleSettings.MainMenu();
            Check(!ModuleSettings.InRun&&!ModuleSettings.Expedition&&!ModuleSettings.Transfer&&!ModuleSettings.Music,"menu resets current run switches");
            Check(!ReferenceEquals(progress,ModuleSettings.Progress)&&ModuleSettings.Progress.Players.Count==0&&ModuleSettings.Choice==null&&!ModuleSettings.Extra.Otherworld,"menu resets run progress and choice");
            Check(OtherworldProgress.Cleared==cleared+1&&CarryCapacity.Applied==applied+1,"menu retains dependent lifecycle hooks");
            Check(!ModuleSettings.Preference(ModuleId.Music)&&!ModuleSettings.ExpeditionPreference,"menu retains changed persistent preferences");
            ModuleSettings.EnterRun();
            Check(!ModuleSettings.Music&&!ModuleSettings.Expedition&&!ModuleSettings.Extra.Otherworld,"next run captures updated preferences");
        }
        Plugin.Role=0;ModuleSettings.MainMenu();Preferences(true);
        var save=new 存档脚本();string path="danju_m.cd";
        const string progressKey="CluckmechCoop.Progress.v4",choiceKey="CluckmechCoop.Expedition.v3",charactersKey="CluckmechCoop.Characters.v1";
        var run=new RunProgress();run.Player(0).Coupons=9;
        var choice=new ExpeditionChoice{Node="page:node",Token="resume",RewardId=2,Enabled=true,Locked=true};
        string progressJson=JsonConvert.SerializeObject(run),choiceJson=JsonConvert.SerializeObject(choice),charactersJson="{\"Players\":{},\"Rooms\":[]}";
        ES3.Data.Clear();ES3.Writes=0;
        ES3.Data[path+progressKey]=progressJson;ES3.Data[path+choiceKey]=choiceJson;ES3.Data[path+charactersKey]=charactersJson;
        ModuleSettings.Load(save);
        Check(ModuleSettings.InRun&&ModuleSettings.LocalProgress.Coupons==9&&ModuleSettings.Choice.Token=="resume","continue restores progress and choice before entering run");
        ModuleSettings.Save(save);
        Check(Equals(ES3.Data[path+progressKey],progressJson)&&Equals(ES3.Data[path+choiceKey],choiceJson)&&Equals(ES3.Data[path+charactersKey],charactersJson),"existing ES3 keys and serialized JSON remain byte compatible");
        Plugin.Role=2;ModuleSettings.MainMenu();Preferences(false);
        ModuleSettings.Preference(ModuleId.Music,true);
        var hostModules=new GameplayModules{Growth=true,Otherworld=true};
        var hostProgress=new RunProgress();hostProgress.Player(2).Coupons=7;
        int beforeHostSettings=Plugin.Network.Published;
        ModuleSettings.ApplyHostSettings(true,true,new(){Node="host",Token="same",Enabled=true},hostModules,hostProgress);
        Check(ModuleSettings.InRun&&ModuleSettings.Music&&ModuleSettings.Expedition&&ModuleSettings.Transfer,"host settings enter guest run while retaining local music");
        Check(ReferenceEquals(ModuleSettings.Extra,hostModules)&&ReferenceEquals(ModuleSettings.Progress,hostProgress)&&ModuleSettings.LocalProgress.Coupons==7,"host snapshot is applied by current-run owner");
        Check(!ModuleSettings.Preference(ModuleId.Otherworld)&&Plugin.Network.Published==beforeHostSettings,"host settings neither overwrite preferences nor publish guest settings");
        ModuleSettings.Choice.RewardApplied=true;ModuleSettings.Choice.Locked=true;
        ModuleSettings.ApplyHostSettings(false,true,new(){Node="host",Token="same"},null,null);
        Check(ModuleSettings.Choice.RewardApplied&&ModuleSettings.Choice.Locked,"same host choice retains consumed reward and locked state");
        Check(ModuleSettings.Extra!=null&&ModuleSettings.Progress!=null&&!ModuleSettings.Extra.Otherworld,"missing host collections use fresh defaults");
        ModuleSettings.ApplyHostSettings(true,false,new(){Node="host",Token="new",RewardApplied=true},new(),new());
        Check(!ModuleSettings.Choice.RewardApplied&&!ModuleSettings.Choice.Locked,"new host choice resets local one-time markers");
        var remoteProgress=new PlayerProgress{Coupons=11};ModuleSettings.SetPlayerProgress(1,remoteProgress);
        Check(ReferenceEquals(ModuleSettings.Progress.Player(1),remoteProgress),"peer progress update uses run owner");
        Console.WriteLine($"PASS: {checks} module preference/run/menu/continue behavior checks using complete production ModuleSettings");
        StorageHarness.Run();
    }
}
