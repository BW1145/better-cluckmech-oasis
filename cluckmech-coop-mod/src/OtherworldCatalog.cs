using System;
using System.Collections.Generic;

namespace CluckmechCoop
{
    internal sealed class OtherworldHero
    {
        internal readonly int Id, UnitId, StartingMachine;
        internal readonly string Name;
        internal readonly float Health, Attack, Rate, Range, Speed;
        internal OtherworldHero(int id, string name, int machine, float health, float attack, float rate, float range, float speed)
        { Id=id; UnitId=22000+id; Name=name; StartingMachine=machine; Health=health; Attack=attack; Rate=rate; Range=range; Speed=speed; }
    }
    internal sealed class OtherworldTalent
    {
        internal readonly int Id, Hero, Branch;
        internal readonly string Name, Description;
        internal bool Initial => Id<26100;
        internal OtherworldTalent(int id,int hero,int branch,string name,string description)
        { Id=id; Hero=hero; Branch=branch; Name=name; Description=description; }
    }
    internal static class OtherworldCatalog
    {
        internal const string PerlicaSkillDescription="雷击半径1.8格内的敌人，造成攻击力×12伤害并麻痹5秒；冷却8秒。";
        internal const string ObitoSkillDescription="短按虚化1秒并加速，冷却5秒。\n长按最多冲刺3段，逐段收容敌人5秒；松手后完成当前段进入空间，3段自动进入，冷却10／12／14秒。\n空间内右键返回；空间内鸡械本局跨关保留。";
        internal static readonly OtherworldHero[] Heroes={
            new OtherworldHero(12,"佩丽卡",16,600,50,4,6,4),
            new OtherworldHero(13,"宇智波带土",78,750,80,2.5f,5,4.5f)
        };
        internal static readonly OtherworldTalent[] Talents={
            new OtherworldTalent(26001,12,0,"长效导流","本人及所属鸡械的麻痹效果提高100%，麻痹附带25%基础脆弱，并随全局元素影响增强。\n研究每级还需10、20、30……名受本人或所属鸡械麻痹的敌人死亡；每级全局元素影响提高10%。"),
            new OtherworldTalent(26002,12,1,"即时协议·闪链","射程增加2格。每第四次普攻追加闪链，命中自身4格内最多6名敌人，每名受到单发普攻300%的伤害。\n闪链每命中一名敌人，右键冷却减少0.5秒，每条最多减少2秒；本局每获得一次鸡械强化，普攻成长倍率增加5%。"),
            new OtherworldTalent(26003,12,2,"协议调度","右键冷却×2。施放时在落点附近额外部署1台最后成功手动放置的鸡械，保留该鸡械当前的强化。"),
            new OtherworldTalent(26004,12,3,"过载授权","右键本体伤害×2，可储存3次使用并逐次恢复。额外伤害为场上攻击力最高的5台所属鸡械攻击力合计×2。\n落点标定8秒，再次轰击标定区时范围扩大50%。"),
            new OtherworldTalent(26005,13,0,"火遁·爆风乱舞","普攻附加灼烧，基础每0.5秒造成10点伤害、持续2秒。本人及所属鸡械的灼烧伤害提高70%、持续时间增加2秒。\n敌人死亡时，直接施加的灼烧可向3格内最多3名敌人传播一次，继承剩余伤害的50%；本局每获得一次鸡械强化，灼烧伤害加成增加6%。"),
            new OtherworldTalent(26006,13,1,"神威工坊","神威空间内部署鸡械消耗0动力，放置冷却缩短50%。空间内所属鸡械最大生命×2、攻击力×1.5。"),
            new OtherworldTalent(26007,13,2,"异界狩猎","所属鸡械攻击力、最大生命各提高50%。神威空间内每死亡10名敌人，本局两项加成各再提高5%，作用于空间内外。"),
            new OtherworldTalent(26008,13,3,"神威武库","虚化时长翻倍。每次被虚化躲过的敌方攻击，将其伤害的50%转为储能。\n虚化结束时向瞄准方向释放螺旋火焰，造成攻击力×30＋储能的伤害；随后3秒普攻伤害×3。"),
            new OtherworldTalent(26101,12,-1,"协议ε·70.41K","右键升级为轨道残骸轰击，半径3.5格，造成角色攻击力×80伤害并麻痹5秒；基础冷却8秒。"),
            new OtherworldTalent(26102,12,-1,"协议加速","右键冷却缩短25%，逐次充能同步加快。"),
            new OtherworldTalent(26103,12,-1,"战地回收","主动右键每命中一名敌人获得5金币。"),
            new OtherworldTalent(26104,12,-1,"电磁汇聚","轰击将敌人向落点牵引最多2格，遵循敌人位移抗性。"),
            new OtherworldTalent(26105,12,0,"电网扩散","受本人或所属鸡械麻痹的敌人死亡，将3秒麻痹及对应脆弱传播至3格内最多5名敌人。"),
            new OtherworldTalent(26106,12,0,"高阶解析","每级研究的全局元素影响提高至15%，追溯已有等级；附加脆弱持续时间增加2秒。"),
            new OtherworldTalent(26107,12,1,"瞬发回路","每第二次普攻追加闪链，最多命中8名敌人。"),
            new OtherworldTalent(26108,12,1,"实战校准","本局每次鸡械强化额外增加1点角色攻击力，追溯已获得的强化次数。"),
            new OtherworldTalent(26109,12,2,"批量交付","每次右键额外部署2台鸡械。"),
            new OtherworldTalent(26110,12,2,"战备预热","自动部署的鸡械获得10秒攻击力提高50%、攻速提高50%；即时战备获得对应攻击力收益。"),
            new OtherworldTalent(26111,12,3,"复合协议","右键的额外伤害提高至场上攻击力最高的5台所属鸡械攻击力合计×4。"),
            new OtherworldTalent(26112,12,3,"交叉轰击","再次轰击标定区时，1秒后再落下一次轰击。"),
            new OtherworldTalent(26113,13,-1,"永久收容","普通敌人在本局内永久留在神威空间；精英及Boss维持临时囚禁。"),
            new OtherworldTalent(26114,13,-1,"时空间延展","神威冲刺距离提高至5格，临时囚禁延长至10秒。"),
            new OtherworldTalent(26115,13,-1,"虚实转移","返回主战场时，右键冷却减少4秒，并获得一次虚化加速。"),
            new OtherworldTalent(26116,13,-1,"空间压迫","神威空间内的敌人受到的伤害提高25%。"),
            new OtherworldTalent(26117,13,0,"豪火灭却","虚化结束或返回动作完成时喷火2秒，每秒命中5次，每次造成攻击力×2伤害并附加自身灼烧。每名敌人在本次喷火中额外结算一次剩余灼烧总伤害。"),
            new OtherworldTalent(26118,13,0,"不熄余烬","本局每次鸡械强化的灼烧成长提高至10%，追溯已有次数；灼烧持续时间再增加2秒。"),
            new OtherworldTalent(26119,13,1,"超额供能","神威空间内放置冷却缩短75%，所属鸡械攻击力倍率提高至×2.5。"),
            new OtherworldTalent(26120,13,1,"神威军阵","神威空间内每有一种所属鸡械类别，空间内全部所属鸡械攻速提高25%、格挡增加10。"),
            new OtherworldTalent(26121,13,2,"灵魂刻印","神威空间内每死亡10名敌人，所属鸡械攻击力、最大生命的成长提高至各10%，追溯已有计数。"),
            new OtherworldTalent(26122,13,2,"狩猎战利","神威空间内敌人的金币掉落增加50%；每死亡1名敌人，带土恢复最大生命的3%。"),
            new OtherworldTalent(26123,13,3,"焰团扇反击","虚化储能转化提高至100%，反击附带击退与2秒眩晕。"),
            new OtherworldTalent(26124,13,3,"虚实协同","虚化后的普攻增益延长至6秒，并使所属鸡械获得6秒攻速提高50%。反击命中敌人时，右键冷却减少2秒，每次反击触发一次。")
        };
        internal static OtherworldHero Hero(int id) { foreach(var h in Heroes)if(h.Id==id || h.UnitId==id)return h; return null; }
        internal static OtherworldTalent Talent(int id) { foreach(var t in Talents)if(t.Id==id)return t; return null; }
        internal static int Branch(int initial) => initial>=26001 && initial<=26008 ? (initial-26001)%4 : -1;
        internal static int ResearchLevel(int kills)
        { int level=0; long required=10; while(kills>=required) { kills-=(int)required; level++; required+=10; } return level; }
        internal static bool Eligible(OtherworldTalent talent,int hero,int initial,ICollection<int> owned)
            => talent.Hero==hero && !talent.Initial && (talent.Branch<0 || talent.Branch==Branch(initial)) && !owned.Contains(talent.Id);
    }
    internal sealed class OtherworldRun
    {
        public int Hero, Initial, Upgrades, ShockKills, SpaceKills;
        public HashSet<int> Talents=new HashSet<int>();
        [Newtonsoft.Json.JsonProperty(NullValueHandling=Newtonsoft.Json.NullValueHandling.Ignore)]
        public List<int> NativeTalents;
        public string LastMachine;
        public Machine LastDeployment;
        public List<string> CardIds=new List<string>();
        public float BaseInfluence;
        internal bool Has(int id) => Talents.Contains(id);
        internal float Influence => OtherworldCatalog.ResearchLevel(ShockKills)*(Has(26106)?.15f:.1f);
        internal float Hunting => .5f+(SpaceKills/10)*(Has(26121)?.1f:.05f);
        internal float BurnBonus => .7f+Upgrades*(Has(26118)?.1f:.06f);
    }
}
