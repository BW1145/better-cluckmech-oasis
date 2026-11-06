using System;
using System.Collections.Generic;
using CluckmechCoop;
using Newtonsoft.Json;

namespace CluckmechCoop
{
    internal sealed class SessionMessage
    {
        public string Type,Token;
        public int Role,Stage,Revision;
        public int[] ShopPool;
        public ShopDrawResult ShopDraw;
        public PlayerProgress PlayerProgress;
    }
    internal sealed class ShopTestUnits
    {
        internal sealed class Entry {public int id,单位等级;public bool 是否商店内出现=true,是否游戏内出现=true;}
        internal readonly List<Entry> 单位列表=new();
    }
    internal sealed class ShopTestSource {internal int 商品单位等级=3,第三栏超模额外等级;}
    internal sealed class ShopRewards
    {
        internal static ShopRewards Current=new();
        internal readonly ShopTestSource Shop=new();
        internal List<int> Pool(bool abc)=>new(){1,2,3};
        internal static void Save()=>ModuleSettings.SaveProgress();
        internal static void Refresh(){}
    }
}

internal static class ShopRewardsHarness
{
    internal static readonly List<SessionMessage> Messages=new();
    internal static void Run(Action<bool,string> check)
    {
        ModuleSettings.Initialize(new BepInEx.Configuration.ConfigFile());
        Plugin.Role=2;ModuleSettings.MainMenu();ModuleSettings.EnterRun();
        ModuleSettings.ApplyHostSettings(false,true,null,new GameplayModules{Shop=true,Growth=true,Energy=true},new RunProgress());
        var p=ModuleSettings.LocalProgress;p.ShopDrawSequence=1;
        var draw=new ShopDrawResult{Token=Guid.NewGuid().ToString("N"),Sequence=1,Prize=0};
        p.ShopDraw=draw;
        var response=new SessionMessage{Type="shop-spin-result",Role=2,Token=draw.Token,ShopDraw=draw,PlayerProgress=p,Revision=5};
        string wire=JsonConvert.SerializeObject(response);
        int gold=Plugin.Summary.当前金钱数;
        check(ShopRewardNetwork.Receive(JsonConvert.DeserializeObject<SessionMessage>(wire))&&Plugin.Summary.当前金钱数==gold+80,"guest applies the host gold result once");
        ShopRewardNetwork.Receive(JsonConvert.DeserializeObject<SessionMessage>(wire));
        check(Plugin.Summary.当前金钱数==gold+80&&ModuleSettings.LocalProgress.ShopAppliedSequence==1,"a duplicate response with an older progress copy does not reissue gold");
        var second=JsonConvert.DeserializeObject<PlayerProgress>(JsonConvert.SerializeObject(ModuleSettings.LocalProgress));
        second.ShopDrawSequence=2;second.ShopDraw=new ShopDrawResult{Token=Guid.NewGuid().ToString("N"),Sequence=2,Prize=3,Machines=new[]{1,2,1,3}};
        int machines=Plugin.Summary.Granted.Count;
        ShopRewardNetwork.Receive(new SessionMessage{Type="shop-spin-result",Role=2,ShopDraw=second.ShopDraw,PlayerProgress=second,Revision=7});
        ShopRewardNetwork.Receive(JsonConvert.DeserializeObject<SessionMessage>(wire));
        check(Plugin.Summary.Granted.Count==machines+4&&Plugin.Summary.当前金钱数==gold+80&&ModuleSettings.LocalProgress.ShopAppliedSequence==2,"four-machine reward and delayed previous result preserve exactly one grant per draw");
        check(ModuleSettings.Progress.EnergyBonus==7,"older result messages preserve the shared energy total");
        var unclaimed=ModuleSettings.LocalProgress;unclaimed.ShopDrawSequence=3;
        unclaimed.ShopDraw=new ShopDrawResult{Token=Guid.NewGuid().ToString("N"),Sequence=3,Prize=8};
        ShopRewardNetwork.ApplyPending();ShopRewardNetwork.ApplyPending();
        check(Plugin.Summary.当前金钱数==gold+580&&unclaimed.ShopAppliedSequence==3,"a saved unsettled result grants once when the shop opens again");
        var other=JsonConvert.DeserializeObject<SessionMessage>(wire);other.Role=1;other.Revision=9;
        ShopRewardNetwork.Receive(other);
        check(Plugin.Summary.当前金钱数==gold+580&&ModuleSettings.Progress.EnergyBonus==9,"teammate reward updates shared energy without awarding the teammate's coins locally");
        Plugin.Role=1;
        var guest=ModuleSettings.Progress.Player(2);guest.ShopSpins=2;guest.ShopDrawSequence=4;
        guest.ShopDraw=new ShopDrawResult{Token=Guid.NewGuid().ToString("N"),Sequence=4,Prize=0};
        var request=new SessionMessage{Type="shop-spin",Token=guest.ShopDraw.Token,Stage=3,ShopPool=new[]{1,2,3}};
        int messages=Messages.Count;
        ShopRewardNetwork.ServerReceive(request,2);ShopRewardNetwork.ServerReceive(request,2);
        check(guest.ShopSpins==2&&guest.ShopDrawSequence==4&&Messages.Count==messages+2,"duplicate requests resend the stored result without spending another spin or rolling again");
        guest.ShopAppliedSequence=4;ShopRewardNetwork.ServerReceive(request,2);
        check(guest.ShopSpins==2&&guest.ShopDrawSequence==4,"replaying a completed request retains its original result");
    }
}
