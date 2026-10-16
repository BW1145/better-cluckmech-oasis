using System;
using System.Collections.Generic;

namespace CluckmechCoop
{
    internal enum StratagemKind { Mines, Barrage, Laser, Eagle, Mortar, Shield, Rover }
    internal sealed class StratagemSequence
    {
        private int last;
        internal bool Accept(int sequence)
        { if (sequence <= last) return false; last = sequence; return true; }
    }
    internal struct StratagemAttackClock
    {
        private double progress;
        internal int Advance(float delta,float rate)
        {
            if (delta <= 0 || rate <= 0) return 0;
            progress += delta*rate;
            int count = (int)Math.Floor(progress+0.00001);
            progress -= count;
            return count;
        }
        internal void Reset() => progress = 0;
    }
    internal sealed class StratagemEffectMessage
    {
        public int Kind, Action;
        public uint Root, Target;
        public float X,Y,EndX,EndY,Radius,Duration;
        internal bool Valid => Kind >= 0 && Kind < 7 && Action >= 0 && Action <= 16 &&
            Finite(X) && Finite(Y) && Finite(EndX) && Finite(EndY) && Finite(Radius) && Radius >= 0 && Radius <= 20 &&
            Finite(Duration) && Duration >= 0 && Duration <= 60;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
    internal sealed class StratagemCooldown
    {
        public string Node;
        public int Slot;
        public Machine Machine;
        public float Remaining;
    }
    internal sealed class StratagemPlacement
    {
        public int Slot;
        public int Sequence;
        public Machine Machine;
        public float X,Y;
    }

    internal sealed class StratagemDefinition
    {
        internal readonly int Id, Category, Tier, Cap, Power;
        internal readonly string Name;
        internal readonly float Health, Block, Damage, Rate, Range, Cooldown, InitialCooldown;
        internal readonly int[] NativeAffixes;
        internal StratagemKind Kind => (StratagemKind)(Id - 20001);
        internal StratagemDefinition(int id, string name, int category, int tier, int cap, int power,
            float health, float block, float damage, float rate, float range, float cooldown, float initial, int[] pool)
        {
            Id = id; Name = name; Category = category; Tier = tier; Cap = cap; Power = power;
            Health = health; Block = block; Damage = damage; Rate = rate; Range = range;
            Cooldown = cooldown; InitialCooldown = initial; NativeAffixes = pool;
        }
    }

    internal sealed class StratagemAffix
    {
        internal readonly int Id, Quality, Excludes;
        internal readonly string Name, Effect;
        internal string Description
        {
            get
            {
                string text = Effect;
                foreach (int other in StratagemCatalog.Exclusions(Id))
                    if(StratagemCatalog.Active(other))text += "\n与「" + StratagemCatalog.Affix(other).Name + "」互斥。";
                return text;
            }
        }
        internal StratagemAffix(int id, int quality, string name, string description, int excludes = 0)
        { Id = id; Quality = quality; Name = name; Effect = description; Excludes = excludes; }
    }

    internal static class StratagemCatalog
    {
        internal static readonly StratagemDefinition[] Units = {
            new StratagemDefinition(20001, "MD-6地雷播撒器", 4, 2, 4, 3, 650, 0, 100, .1f, 3.2f, 30, 2, new[] {42,109,157,23,25,66,82,72}),
            new StratagemDefinition(20002, "380mm轨道火力网", 5, 4, 6, 9, 1, 0, 1200, 1, 4.5f, 60, 12, new[] {42,109,157,23,25,66,82,72}),
            new StratagemDefinition(20003, "轨道激光", 5, 4, 6, 8, 1, 0, 250, 4, 6, 60, 12, new[] {109,157,23,66,82,181,182,183,72}),
            new StratagemDefinition(20004, "飞鹰500kg", 5, 4, 6, 8, 1, 0, 3000, 1, 4, 45, 8, new[] {42,109,157,23,25,66,72}),
            new StratagemDefinition(20005, "EMS迫击炮", 0, 2, 4, 3, 450, 0, 0, .25f, 9, 25, 2, new[] {82,81,167,28,71,72}),
            new StratagemDefinition(20006, "FX-12护盾发生器", 1, 3, 5, 3, 5000, 15, 0, 0, 4.5f, 30, 3, new[] {27,28,29,39,71,112,72}),
            new StratagemDefinition(20007, "AX/LAS-5激光护卫犬", 2, 3, 5, 3, 450, 0, 40, 4, 4.5f, 30, 3, new[] {143,105,146,255,462,23,72})
        };
        internal static readonly StratagemAffix[] Affixes = {
            new StratagemAffix(25011,0,"扩充弹链","地雷上限+4，初次布雷+2；布防0.4秒，触发等待0.08秒。"),
            new StratagemAffix(25012,0,"快速投送","补雷间隔缩短至8秒。"),
            new StratagemAffix(25013,0,"灵敏引信","布防0.4秒，触发延迟0.08秒。"),
            new StratagemAffix(25014,1,"反坦克地雷","只由Boss或基础生命至少1000的敌人触发，伤害提高80%，地雷上限减少4。",25015),
            new StratagemAffix(25015,1,"燃烧地雷","爆炸伤害减少25%，附加持续3秒、每0.5秒4点灼烧。",25014),
            new StratagemAffix(25016,2,"连续补给","每次补充4颗地雷，放置动力+1。"),
            new StratagemAffix(25021,0,"定点校准","散布半径缩小至3.8。"),
            new StratagemAffix(25022,0,"扩充弹舱","增加一批3发炮弹。"),
            new StratagemAffix(25023,0,"高密度装药","爆炸半径增加至2，伤害减少10%。"),
            new StratagemAffix(25024,1,"同步装填","放置冷却缩短20%。"),
            new StratagemAffix(25025,1,"快速落弹","呼叫等待1.2秒，批内间隔0.5秒，批次间隔2.5秒。"),
            new StratagemAffix(25026,2,"焦土火网","留下持续3秒的燃烧区域，半径1.2，每0.5秒施加2点灼烧。"),
            new StratagemAffix(25031,0,"持续供能","照射时间增加3秒。"),
            new StratagemAffix(25032,0,"光束聚焦","伤害提高25%，光束半径缩小至0.55，追踪速度提高至6。",25033),
            new StratagemAffix(25033,0,"宽幅照射","光束半径增加至1.15，伤害减少10%。",25032),
            new StratagemAffix(25034,1,"快速调焦","追踪速度增加至6。"),
            new StratagemAffix(25035,1,"冷却储备","放置冷却缩短20%。"),
            new StratagemAffix(25036,2,"高温熔穿","连续照射同一敌人3秒后，光束伤害提高30%，灼烧提高至6点。"),
            new StratagemAffix(25041,0,"扩大杀伤","内爆炸半径增加至3，外半径增加至4.8。"),
            new StratagemAffix(25042,0,"高频引信","炸弹入土后等待缩短至0.5秒。"),
            new StratagemAffix(25043,0,"优化挂载","放置动力减少至6。"),
            new StratagemAffix(25044,1,"重型弹头","伤害提高25%，放置冷却增加15%。"),
            new StratagemAffix(25045,1,"快速返航","放置冷却缩短20%。"),
            new StratagemAffix(25046,2,"震荡冲击","爆炸外围增加1.5格冲击环，眩晕0.8秒并击退敌人。"),
            new StratagemAffix(25051,0,"静电扩散","区域半径增加至2.3。"),
            new StratagemAffix(25052,0,"延时电容","区域持续时间增加至6秒。"),
            new StratagemAffix(25053,0,"高压制动","移动减速提高至50%，首次眩晕提高至1秒。",25055),
            new StratagemAffix(25054,1,"稳定脉冲","首次眩晕提高至1秒。",25055),
            new StratagemAffix(25055,1,"瓦斯弹头","保留减速，施加持续2秒、每0.5秒5点中毒，替代首次眩晕。",25053),
            new StratagemAffix(25056,2,"紧急拦阻","每第三次发射，额外向另一名靠近基地的敌人发射一弹。"),
            new StratagemAffix(25061,0,"扩域线圈","护罩半径增加至5.5。"),
            new StratagemAffix(25062,0,"备用电容","护罩容量提高至本体最大生命的55%。"),
            new StratagemAffix(25063,0,"快速复位","破盾后重启时间缩短至7秒。"),
            new StratagemAffix(25064,1,"稳态充电","未破盾再生等待2.5秒，每秒恢复150容量。"),
            new StratagemAffix(25065,1,"紧急泄压","破盾时眩晕并击退4格内敌人，眩晕0.6秒。"),
            new StratagemAffix(25066,2,"堡垒模式","护罩受到的弹丸伤害减少25%。"),
            new StratagemAffix(25071,0,"大容量散热器","无人机热量容量提高至150。"),
            new StratagemAffix(25072,0,"高效风冷","停靠充能缩短至3秒，返航速度翻倍；近距离返航与起飞最短0.25秒。"),
            new StratagemAffix(25073,0,"返航推进","返航和起飞各0.25秒，飞行速度提高至6。"),
            new StratagemAffix(25074,1,"红线输出","激光伤害提高100%，每次攻击热量增加50%。"),
            new StratagemAffix(25075,1,"自动维修","停靠充能时自动维修已纳入无人机基础能力。"),
            new StratagemAffix(25076,2,"焦灼射线","激光附加持续2秒、每0.5秒4点灼烧。"),
            new StratagemAffix(25077,1,"贯穿光学","激光延伸至当前射程终点，贯穿沿线敌人；首个敌人承受全额伤害，后续各承受50%。"),
            new StratagemAffix(25078,1,"余热回收","无人机每击杀一个敌人，降低10点热量。")
        };
        private static readonly HashSet<int> retired = new HashSet<int> {25012,25013,25025,25034,25042,25054,25056,25066,25073,25075,25076};
        private static readonly Dictionary<int,StratagemAffix> affixesById = IndexAffixes();
        private static Dictionary<int,StratagemAffix> IndexAffixes()
        {
            var result = new Dictionary<int,StratagemAffix>();
            foreach (var affix in Affixes) result.Add(affix.Id,affix);
            return result;
        }
        internal static IEnumerable<StratagemAffix> ActiveAffixes
        { get { foreach (var affix in Affixes) if (!retired.Contains(affix.Id)) yield return affix; } }
        internal static bool Active(int id)=>affixesById.ContainsKey(id)&&!retired.Contains(id);
        internal static IEnumerable<int> UpgradePool(int unit)
        { foreach (var affix in ActiveAffixes) if (affix.Id/10-2501 == unit-20001) yield return affix.Id; }
        internal static IEnumerable<int> Exclusions(int id)
        {
            var affix = Affix(id);
            if (affix != null && affix.Excludes != 0) yield return affix.Excludes;
            if (id == 25055) yield return 25054;
        }
        internal static bool IsUnit(int id) => id >= 20001 && id <= 20007;
        internal static bool OwnsDeck(Machine[] cards,IEnumerable<Machine> inventory)
        {
            var available = new List<Machine>(inventory);
            foreach (var card in cards)
            {
                if (card == null) continue;
                int match = available.FindIndex(x => Rules.SameMachine(x,card));
                if (match < 0) return false;
                available.RemoveAt(match);
            }
            return true;
        }
        internal static StratagemDefinition Unit(int id) => IsUnit(id) ? Units[id - 20001] : null;
        internal static StratagemAffix Affix(int id)
        {
            return affixesById.TryGetValue(id,out var affix) ? affix : null;
        }
        internal static bool CanChoose(int id, ICollection<int> owned)
        {
            var affix = Affix(id);
            if (affix == null) return true;
            if(retired.Contains(id))return false;
            if (owned.Contains(id)) return false;
            foreach (int other in Exclusions(id)) if (owned.Contains(other)) return false;
            return true;
        }
    }

    internal sealed class StratagemParameters
    {
        internal readonly StratagemDefinition Definition;
        internal int Power, MineInitial = 4, MineCap = 8, MineRefill = 2, Salvos = 5;
        internal float Cooldown, DamageMultiplier = 1, BlastRadius, InnerRadius;
        internal float RefillInterval = 10, ArmTime = .8f, TriggerTime = .15f, ScatterRadius = 4.5f;
        internal float CallDelay, SalvoInterval = 3, ShellInterval = .6f, Duration = 15, TrackSpeed = 4;
        internal float FieldDuration = 4, Slow = .35f, Stun = .6f, CapacityRatio = .4f;
        internal float Reboot = 10, RegenDelay = 4, Regen = 100, ShieldDamageFactor = 1;
        internal float HeatCap = 100, HeatPerShot = 3.125f, DockTime = 4.5f, ReturnTime = .5f, FlightSpeed = 5;
        internal bool AntiTank, BurningMines, Scorch, Melt, Shock, Gas, DoubleMortar, Discharge, BurningRover, Piercing;
        internal float HeatRecovery;

        internal StratagemParameters(int id, ICollection<int> owned)
        {
            Definition = StratagemCatalog.Unit(id) ?? throw new ArgumentOutOfRangeException(nameof(id));
            Power = Definition.Power; Cooldown = Definition.Cooldown;
            switch (Definition.Kind)
            {
                case StratagemKind.Mines: BlastRadius = 1.1f; break;
                case StratagemKind.Barrage: BlastRadius = 1.6f; CallDelay = 2; break;
                case StratagemKind.Laser: BlastRadius = .85f; CallDelay = 1.5f; break;
                case StratagemKind.Eagle: InnerRadius = 2.5f; BlastRadius = 4; CallDelay = 1; break;
                case StratagemKind.Mortar: BlastRadius = 1.8f; break;
                case StratagemKind.Shield: BlastRadius = 4.5f; break;
            }
            if (owned == null) return;
            foreach (int affix in owned) Apply(affix);
            if (Gas) Stun = 0;
            MineInitial = Math.Min(MineInitial, MineCap);
        }
        private void Apply(int id)
        {
            if ((id / 10 - 2501) != (int)Definition.Kind) return;
            switch (id)
            {
                case 25011: MineCap += 4; MineInitial += 2; ArmTime = .4f; TriggerTime = .08f; break;
                case 25012: RefillInterval = 8; break;
                case 25013: ArmTime = .4f; TriggerTime = .08f; break;
                case 25014: AntiTank = true; DamageMultiplier *= 1.8f; MineCap -= 4; break;
                case 25015: BurningMines = true; DamageMultiplier *= .75f; break;
                case 25016: MineRefill = 4; Power++; break;
                case 25021: ScatterRadius = 3.8f; break;
                case 25022: Salvos = 6; break;
                case 25023: BlastRadius = 2; DamageMultiplier *= .9f; break;
                case 25024: Cooldown *= .8f; break;
                case 25025: CallDelay = 1.2f; ShellInterval = .5f; SalvoInterval = 2.5f; break;
                case 25026: Scorch = true; break;
                case 25031: Duration += 3; break;
                case 25032: DamageMultiplier *= 1.25f; BlastRadius = .55f; TrackSpeed = 6; break;
                case 25033: DamageMultiplier *= .9f; BlastRadius = 1.15f; break;
                case 25034: TrackSpeed = 6; break;
                case 25035: Cooldown *= .8f; break;
                case 25036: Melt = true; break;
                case 25041: InnerRadius = 3; BlastRadius = 4.8f; break;
                case 25042: CallDelay = .5f; break;
                case 25043: Power = 6; break;
                case 25044: DamageMultiplier *= 1.25f; Cooldown *= 1.15f; break;
                case 25045: Cooldown *= .8f; break;
                case 25046: Shock = true; break;
                case 25051: BlastRadius = 2.3f; break;
                case 25052: FieldDuration = 6; break;
                case 25053: Slow = .5f; Stun = 1; break;
                case 25054: Stun = 1; break;
                case 25055: Gas = true; Stun = 0; break;
                case 25056: DoubleMortar = true; break;
                case 25061: BlastRadius = 5.5f; break;
                case 25062: CapacityRatio = .55f; break;
                case 25063: Reboot = 7; break;
                case 25064: RegenDelay = 2.5f; Regen = 150; break;
                case 25065: Discharge = true; break;
                case 25066: ShieldDamageFactor = .75f; break;
                case 25071: HeatCap = 150; break;
                case 25072: DockTime = 3; ReturnTime = .25f; break;
                case 25073: ReturnTime = .25f; FlightSpeed = 6; break;
                case 25074: DamageMultiplier *= 2; HeatPerShot *= 1.5f; break;
                case 25076: BurningRover = true; break;
                case 25077: Piercing = true; break;
                case 25078: HeatRecovery = 10; break;
            }
        }
        internal float EagleDamage(float distance)
        {
            if (distance > BlastRadius) return 0;
            float damage = distance <= InnerRadius ? 3000 : 3000 - 2200 * (distance - InnerRadius) / (BlastRadius - InnerRadius);
            return damage * DamageMultiplier;
        }
        internal float BarrageEmission(int shell, float rateFactor) =>
            rateFactor > 0 ? (shell / 3 * SalvoInterval + shell % 3 * ShellInterval) / rateFactor : float.PositiveInfinity;
        internal float Interval(float calculatedRate) => calculatedRate > 0 ? 1 / calculatedRate : float.PositiveInfinity;
        internal float MineInterval(float calculatedRate) => calculatedRate > 0 ? RefillInterval * Definition.Rate / calculatedRate : float.PositiveInfinity;
    }

    internal static class ShieldGeometry
    {
        // Outside-to-inside intersection; catches travel through the entire circle in one frame.
        internal static bool Entry(float ax, float ay, float bx, float by, float cx, float cy, float radius, out float t)
        {
            t = 0;
            double x = ax - cx, y = ay - cy, dx = bx - ax, dy = by - ay;
            double c = x * x + y * y - radius * radius;
            if (c <= 0) return false;
            double a = dx * dx + dy * dy;
            if (a <= 0) return false;
            double b = 2 * (x * dx + y * dy), discriminant = b * b - 4 * a * c;
            if (discriminant <= 0) return false;
            double entry = (-b - Math.Sqrt(discriminant)) / (2 * a);
            if (entry < 0 || entry > 1) return false;
            t = (float)entry; return true;
        }
        internal static float Residual(float rawDamage, float shieldFactor, ref float capacity)
        {
            float absorbed = Math.Min(capacity, rawDamage * shieldFactor);
            capacity -= absorbed;
            return Math.Max(0, rawDamage - absorbed / shieldFactor);
        }
    }
}
