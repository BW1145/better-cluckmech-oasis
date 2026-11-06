using System;
using System.Linq;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class ShopRewardNetwork
    {
        internal static string Pending;
        internal static ShopDrawResult Completed;
        private static string node;
        internal static bool Request()
        {
            var source=ShopRewards.Current;
            if(source==null||!ModuleSettings.Extra.Shop||ModuleSettings.LocalProgress.ShopSpins<=0||source.Pool(true).Count==0)return false;
            if(node!=ModuleSettings.NodeKey){node=ModuleSettings.NodeKey;Pending=null;}
            if(Pending==null)Pending=Guid.NewGuid().ToString("N");
            Completed=null;ShopRewards.Save();
            var request=new SessionMessage{Type="shop-spin",Token=Pending,ShopPool=source.Pool(true).ToArray(),
                Stage=Mathf.Min(3,source.Shop.商品单位等级+Mathf.Max(0,source.Shop.第三栏超模额外等级))};
            if(Plugin.Role==2)Plugin.Network.Send(request);else Draw(request,Plugin.Role);
            return true;
        }
        internal static bool ServerReceive(SessionMessage request,int role)
        {
            if(request.Type!="shop-spin")return false;
            Draw(request,role);return true;
        }
        private static void Draw(SessionMessage request,int role)
        {
            if(!ModuleSettings.Extra.Shop||Plugin.InCombat||!ModuleSettings.InRun)return;
            var p=ModuleSettings.Progress.Player(role);
            if(p.ShopDraw!=null&&(p.ShopDraw.Token==request.Token||p.ShopAppliedSequence<p.ShopDraw.Sequence))
            {Reply(role,p);return;}
            if(p.ShopSpins<=0)return;
            int maximum=request.Stage;
            var allowed=Plugin.Units.单位列表.Where(x=>x.单位等级>=1&&x.单位等级<=maximum&&x.是否商店内出现&&x.是否游戏内出现).Select(x=>x.id).ToArray();
            var pool=request.ShopPool.Where(id=>allowed.Contains(id)).Distinct().ToArray();
            if(pool.Length==0)return;
            bool growth=ModuleSettings.Extra.Growth,energy=ModuleSettings.Extra.Energy;
            int prize=ShopRewardRules.Roll(UnityEngine.Random.Range(0,ShopRewardRules.Total(growth,energy)),growth,energy);
            var item=ShopRewardRules.Prizes[prize];
            var result=new ShopDrawResult{Token=request.Token,Prize=prize,Sequence=++p.ShopDrawSequence};
            if(item.Kind==ShopPrizeKind.Machines)
                result.Machines=Enumerable.Range(0,item.Amount).Select(_=>pool[UnityEngine.Random.Range(0,pool.Length)]).ToArray();
            p.ShopSpins--;p.ShopDraw=result;
            if(item.Kind==ShopPrizeKind.Coupons)p.Coupons+=item.Amount;
            else if(item.Kind==ShopPrizeKind.Capacity)p.MachineUpgradeCredits+=item.Amount;
            else if(item.Kind==ShopPrizeKind.Pick)p.ShopPicks+=item.Amount;
            else if(item.Kind==ShopPrizeKind.Energy)ModuleSettings.Progress.EnergyBonus+=item.Amount;
            p.ShopReceipt="获得："+item.Name;
            ModuleSettings.SaveProgress();Reply(role,p);
        }
        private static void Reply(int role,PlayerProgress progress)
        {
            if(role==Plugin.Role)Apply(progress.ShopDraw);
            if(Plugin.Role==1)Plugin.Network.Broadcast(new SessionMessage{Type="shop-spin-result",Role=role,Token=progress.ShopDraw.Token,
                ShopDraw=progress.ShopDraw,PlayerProgress=progress,Revision=ModuleSettings.Progress.EnergyBonus});
        }
        internal static bool Receive(SessionMessage message)
        {
            if(message.Type!="shop-spin-result")return false;
            ModuleSettings.Progress.EnergyBonus=Mathf.Max(ModuleSettings.Progress.EnergyBonus,message.Revision);
            if(message.Role!=Plugin.Role)return true;
            if(message.ShopDraw.Sequence<=ModuleSettings.LocalProgress.ShopAppliedSequence)return true;
            ModuleSettings.SetPlayerProgress(Plugin.Role,message.PlayerProgress);
            Apply(message.ShopDraw);return true;
        }
        internal static void ApplyPending()
        {
            var draw=ModuleSettings.LocalProgress.ShopDraw;
            if(draw!=null&&draw.Sequence>ModuleSettings.LocalProgress.ShopAppliedSequence)Apply(draw);
        }
        private static void Apply(ShopDrawResult result)
        {
            var p=ModuleSettings.LocalProgress;
            if(p.ShopAppliedSequence<result.Sequence)
            {
                var prize=ShopRewardRules.Prizes[result.Prize];
                if(prize.Kind==ShopPrizeKind.Gold)Plugin.Summary.获得金钱(prize.Amount);
                foreach(int id in result.Machines)Plugin.Summary.获得炮台(id);
                p.ShopAppliedSequence=result.Sequence;
                ShopRewards.Save();
            }
            Completed=result;Pending=null;ShopRewards.Refresh();
        }
    }
}
