using System.Collections.Generic;

namespace CluckmechCoop
{
    internal static class BronzeDragonRules
    {
        internal const int Unit = 20201, Power = 3, Cap = 4;
        internal const float Health = 350, Cooldown = 35;
        internal static readonly int[] NativeAffixes = {35,72,153,222,245,28,145,196,358,362};
        internal static readonly int[] SpecialAffixes = {25101,25102,25103,25104};
        internal static readonly string[] Names = {"双孔铸模","精密传动","积少成多","开库见财"};
        internal static readonly string[] Effects = {
            "每次生产的金币增加至2枚。", "生产间隔缩短至6秒。",
            "每完成5次生产，额外生产2金币。", "放置时立即生产3金币。"
        };
        internal static bool Special(int id) => id >= 25101 && id <= 25104;
        internal static List<int> Pool()
        {var pool=new List<int>(NativeAffixes);pool.AddRange(SpecialAffixes);return pool;}
    }
    internal sealed class BronzeDragonState
    {
        public float Remaining = 10;
        public int Cycles;
        public bool Started;
        internal bool Valid => !float.IsNaN(Remaining)&&!float.IsInfinity(Remaining)&&Remaining>=0&&Remaining<=10&&Cycles>=0&&Cycles<5;
        internal int Advance(float delta,bool doubleCoins,bool fast,bool surplus)
        {
            if(delta<=0)return 0;
            Remaining-=delta;
            int coins=0;
            while(Remaining<=0)
            {
                Remaining+=fast?6:10;
                Cycles=(Cycles+1)%5;
                coins+=(doubleCoins?2:1)+(surplus&&Cycles==0?2:0);
            }
            return coins;
        }
    }
}
