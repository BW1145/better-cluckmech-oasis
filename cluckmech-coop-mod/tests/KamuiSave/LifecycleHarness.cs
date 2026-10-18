using System;
using CluckmechCoop;
using BepInEx.Configuration;

static class LifecycleHarness
{
    private static int checks;
    private static void Check(bool value,string name)
    {
        checks++;
        if(!value)throw new Exception(name);
    }
    private static void Preferences(int mask)
    {
        foreach(var info in ModuleCatalog.All)ModuleSettings.Preference(info.Id,false);
        ModuleSettings.Preference(ModuleId.Stratagems,(mask&1)!=0);
        ModuleSettings.Preference(ModuleId.Otherworld,(mask&2)!=0);
        ModuleSettings.Preference(ModuleId.Music,true);
    }
    private static void Modules(int mask,string name)
    {
        Check(ModuleSettings.Extra.Stratagems==((mask&1)!=0)&&ModuleSettings.Extra.Otherworld==((mask&2)!=0),name);
    }
    private static void ClearScenePreserves(string name)
    {
        var progress=ModuleSettings.Progress;
        var choice=ModuleSettings.Choice;
        var modules=ModuleSettings.Extra;
        int published=Plugin.Network.Published;
        ModuleSettings.ClearScene();
        ModuleSettings.ClearScene();
        Check(ModuleSettings.InRun&&ReferenceEquals(progress,ModuleSettings.Progress)&&ReferenceEquals(choice,ModuleSettings.Choice)&&ReferenceEquals(modules,ModuleSettings.Extra),name+" keeps run objects across scenes");
        Check(Plugin.Network.Published==published,name+" scene clearing does not publish");
    }
    internal static void Run()
    {
        ModuleSettings.Initialize(new ConfigFile());
        for(int role=0;role<=2;role++)
        for(int firstMask=0;firstMask<4;firstMask++)
        {
            Plugin.Role=role;
            string label=$"role {role} mask {firstMask}";
            ES3.Data.Clear();ES3.Writes=0;
            int published=Plugin.Network.Published;
            ModuleSettings.MainMenu();
            ModuleSettings.MainMenu();
            Preferences(firstMask);
            Check(!ModuleSettings.InRun&&ModuleSettings.Progress.Players.Count==0&&ModuleSettings.Choice==null,label+" repeated menu starts empty");
            Check(Plugin.Network.Published==published,label+" menu does not publish");

            for(int runNumber=0;runNumber<2;runNumber++)
            {
                int mask=runNumber==0?firstMask:firstMask^3;
                if(runNumber==1)
                {
                    ModuleSettings.MainMenu();
                    Check(ModuleSettings.Progress.Players.Count==0&&ModuleSettings.Choice==null,label+" second run drops previous progression");
                }
                ModuleSettings.EnterRun();
                int expectedPublished=published+(role==2?0:runNumber+1);
                Check(Plugin.Network.Published==expectedPublished,label+" authority publishes exactly once for each new run");
                Check(ModuleSettings.InRun&&ModuleSettings.Music,label+" local music snapshot survives gameplay combinations");
                if(role==2)
                {
                    Modules(0,label+" guest waits for authoritative modules");
                    ModuleSettings.ApplyHostSettings(false,false,null,new GameplayModules{Stratagems=(mask&1)!=0,Otherworld=(mask&2)!=0},new RunProgress());
                }
                Modules(mask,label+" current run snapshot");
                var progress=ModuleSettings.Progress;
                var modules=ModuleSettings.Extra;
                progress.Player(role).Coupons=10+mask;
                var choice=new ExpeditionChoice{Node="page:node",Token=label+"/"+runNumber,RewardId=1,Enabled=true,Locked=true};
                ModuleSettings.Choice=choice;
                Preferences(mask^3);
                ModuleSettings.EnterRun();
                ModuleSettings.EnterRun();
                Check(ReferenceEquals(progress,ModuleSettings.Progress)&&ReferenceEquals(choice,ModuleSettings.Choice)&&ReferenceEquals(modules,ModuleSettings.Extra),label+" repeated entry retains progressed run identity");
                Check(ModuleSettings.LocalProgress.Coupons==10+mask&&Plugin.Network.Published==expectedPublished,label+" repeated entry neither resets progress nor publishes");
                Modules(mask,label+" mid-run preference changes wait for next run");
                ClearScenePreserves(label+" run "+runNumber);
            }

            var save=new 存档脚本{文件名=role==0?"danju":"lianji"};
            var savedProgress=ModuleSettings.Progress;
            string token=ModuleSettings.Choice.Token;
            int coupons=ModuleSettings.LocalProgress.Coupons;
            if(role!=2)
            {
                ModuleSettings.Save(save);
                Check(ES3.Writes==3,label+" authority writes existing three addon keys");
                ModuleSettings.MainMenu();
                int beforeContinue=Plugin.Network.Published;
                ModuleSettings.Load(save);
                Check(ModuleSettings.InRun&&!ReferenceEquals(savedProgress,ModuleSettings.Progress)&&ModuleSettings.LocalProgress.Coupons==coupons&&ModuleSettings.Choice.Token==token,label+" continue restores second run progress and choice");
                Check(Plugin.Network.Published==beforeContinue+1&&ES3.Writes==3,label+" continue enters once without writing disk");
                ClearScenePreserves(label+" continued run");
                var resumedProgress=ModuleSettings.Progress;
                var resumedChoice=ModuleSettings.Choice;
                ModuleSettings.EnterRun();
                Check(ReferenceEquals(resumedProgress,ModuleSettings.Progress)&&ReferenceEquals(resumedChoice,ModuleSettings.Choice)&&Plugin.Network.Published==beforeContinue+1,label+" repeated continued entry retains progress and choice");
            }
            else
            {
                ModuleSettings.Save(save);
                ModuleSettings.Load(save);
                Check(ES3.Writes==0&&ReferenceEquals(savedProgress,ModuleSettings.Progress)&&ModuleSettings.Choice.Token==token,label+" guest keeps authoritative run and never reads or writes local slots");
                ClearScenePreserves(label+" guest continuation");
            }
        }
        Console.WriteLine($"PASS: {checks} lifecycle combination checks covering two runs, continuation, scenes and all Stratagems/Otherworld combinations for solo/host/guest");
    }
}
