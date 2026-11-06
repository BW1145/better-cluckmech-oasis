using System;
using System.Collections.Generic;
using System.Linq;

namespace CluckmechCoop
{
    internal enum ShopPrizeKind { Gold, Machines, Coupons, Energy, Capacity, Pick }
    internal sealed class ShopPrize
    {
        internal readonly ShopPrizeKind Kind;
        internal readonly int Amount, Weight;
        internal readonly string Name;
        internal ShopPrize(ShopPrizeKind kind,int amount,int weight,string name)
        {Kind=kind;Amount=amount;Weight=weight;Name=name;}
    }
    internal static class ShopRewardRules
    {
        internal const int SpinRefreshes=5, PickRefreshes=15;
        internal static readonly string[] Groups={"金币","随机鸡械","工匠刷新","后备能源","稀有大奖"};
        internal static readonly ShopPrize[] Prizes={
            new ShopPrize(ShopPrizeKind.Gold,80,25,"80金币"),
            new ShopPrize(ShopPrizeKind.Machines,1,14,"随机ABC鸡械1台"),
            new ShopPrize(ShopPrizeKind.Machines,2,5,"随机ABC鸡械2台"),
            new ShopPrize(ShopPrizeKind.Machines,4,1,"随机ABC鸡械4台"),
            new ShopPrize(ShopPrizeKind.Coupons,1,15,"工匠刷新1次"),
            new ShopPrize(ShopPrizeKind.Coupons,3,12,"工匠刷新3次"),
            new ShopPrize(ShopPrizeKind.Coupons,10,3,"工匠刷新10次"),
            new ShopPrize(ShopPrizeKind.Gold,200,8,"200金币"),
            new ShopPrize(ShopPrizeKind.Gold,500,2,"500金币"),
            new ShopPrize(ShopPrizeKind.Energy,1,8,"后备隐藏能源＋1次"),
            new ShopPrize(ShopPrizeKind.Energy,2,5,"后备隐藏能源＋2次"),
            new ShopPrize(ShopPrizeKind.Capacity,1,1,"自选鸡械强化上限＋1"),
            new ShopPrize(ShopPrizeKind.Pick,1,1,"自选鸡械1次")
        };
        internal static bool Available(ShopPrize prize,bool growth,bool energy)=>
            (growth||prize.Kind!=ShopPrizeKind.Coupons&&prize.Kind!=ShopPrizeKind.Capacity)&&
            (energy||prize.Kind!=ShopPrizeKind.Energy);
        internal static int Total(bool growth,bool energy)=>Prizes.Where(p=>Available(p,growth,energy)).Sum(p=>p.Weight);
        internal static int Roll(int ticket,bool growth,bool energy)
        {
            if(ticket<0||ticket>=Total(growth,energy))throw new ArgumentOutOfRangeException(nameof(ticket));
            for(int i=0;i<Prizes.Length;i++)
                if(Available(Prizes[i],growth,energy)){ticket-=Prizes[i].Weight;if(ticket<0)return i;}
            throw new InvalidOperationException();
        }
        internal static int GroupWeight(ShopPrizeKind kind,bool growth,bool energy)=>
            Prizes.Where(p=>p.Kind==kind&&Available(p,growth,energy)).Sum(p=>p.Weight);
        internal static int Sector(ShopPrizeKind kind)=>Math.Min((int)kind,4);
        internal static float SectorStart(int sector,bool growth,bool energy)=>
            360f*Prizes.Where(p=>Sector(p.Kind)<sector&&Available(p,growth,energy)).Sum(p=>p.Weight)/Total(growth,energy);
        internal static float SectorEnd(int sector,bool growth,bool energy)=>SectorStart(sector+1,growth,energy);
    }
    internal sealed class ShopDrawResult
    {
        public string Token;
        public int Prize, Sequence;
        public int[] Machines=new int[0];
        internal bool Valid=>Guid.TryParseExact(Token,"N",out _)&&Sequence>0&&Prize>=0&&Prize<ShopRewardRules.Prizes.Length&&Machines!=null&&
            Machines.Length==(ShopRewardRules.Prizes[Prize].Kind==ShopPrizeKind.Machines?ShopRewardRules.Prizes[Prize].Amount:0);
    }
}
