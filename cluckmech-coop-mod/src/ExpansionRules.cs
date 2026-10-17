using System;
using System.Collections.Generic;

namespace CluckmechCoop
{
    internal enum ModuleId { Speed, Characters, Energy, Music, Growth, Shop, Expedition, Transfer, SkipSplash, Stratagems, NativeRepairs }

    internal sealed class ModuleInfo
    {
        public ModuleId Id;
        public string Name, Description, Details;
        public ModuleInfo(ModuleId id, string name, string description, string details)
        { Id = id; Name = name; Description = description; Details = details; }
    }

    internal static class ModuleCatalog
    {
        internal static readonly ModuleInfo[] All = {
            new ModuleInfo(ModuleId.Speed, "倍速", "左Shift切换战斗2倍速。", "左Shift：按一下开启2倍速，再按一下关闭。\n联机任意玩家均可切换双方速度。移动、攻击、刷怪、冷却和持续效果同步加速。"),
            new ModuleInfo(ModuleId.Characters, "更好的角色右键强化", "增强角色右键，并改善鼠标吞噬选择。", "战斗鸡：三连霰弹，间隔0.2秒，冷却4秒。\n工程狮：集结时移速提高30%、蓄力时间×0.75；松手后集结战士伤害提高25%、攻速提高20%，持续4秒，冷却8秒。\n肌肉鼠：移动治疗；按下额外治疗20%最大生命，冷却10秒。\n阿法狗：召唤并自爆，伤害300；召唤物前5秒生命、格挡×2。\n八酱兔：向鼠标处召唤；前5秒伤害提高50%、移速提高30%。\n肚子鳄、水母骑士：优先吞噬范围内鼠标指向的合法目标；空地仍选最近目标。"),
            new ModuleInfo(ModuleId.Energy, "基地车后备隐藏能源", "按F强化基地车，可通过事件补充次数。", "F：启动后备隐藏能源\n受到伤害减少100%，攻击力增加500，持续10秒。初始整局双方共用一次；第二层事件可补充2次。"),
            new ModuleInfo(ModuleId.Music, "音乐扩展", "主菜单播放《春日影》，扩展战斗音乐。", "主菜单播放《春日影 (MyGO!!!!! ver.)》。\n矿洞：原版、《岩窟快车》《矿脉引擎》各有1/3概率。其他六个区域：原版与新增各有50%概率。\n普通Boss从原版两首和《铜管通缉令》中随机选一首，各占1/3。\n响度统一，使用原版音乐音量设置。"),
            new ModuleInfo(ModuleId.Growth, "强化成长", "扩充强化空间、成长事件与工匠强化回馈。", "区域关底完成后，每人选C级鸡械上限+3，或B级上限+1；可以跳过。\n工匠强化：C级返款50%，B级返款20%；每累计600金币强化净花费，可为已有鸡械自选1项强化，进度跨工匠保留。\n工匠刷新券：30金币增加2次刷新，每家限购一次；维修节点额外赠送4次。\n第二层问号事件：10次工匠刷新或50%获得20次；所有C级鸡械上限+3或指定任意鸡械上限+1；后备隐藏能源+2次或100金币。\n第三层问号事件：携带上限5→6或150金币；战斗动力上限20→25或150金币。\n事件效果与剩余刷新次数本局保留。第六格使用数字键6。"),
            new ModuleInfo(ModuleId.Shop, "商店扩展", "同一家商店刷新3次后提供4台鸡械。", "成功付费刷新达到3次后，每次提供4台鸡械供购买。刷新价格沿用原版，新商店重新累计。"),
            new ModuleInfo(ModuleId.Expedition, "强化远征", "追加强化敌人，并增强绿洲最终Boss。", "进关前独立选择。原版敌人全部出完并消灭后，再出现一次；生命×1.5，攻击×2。\n绿洲最终Boss只出现一次，生命×10、攻击×2。\n完成后获得显示的额外奖励。"),
            new ModuleInfo(ModuleId.Transfer, "队友转交", "向队友转交金币或指定鸡械。", "在路线地图等关间界面打开队友转交。鸡械保留强化词条与强化上限，选中后确认转交。"),
            new ModuleInfo(ModuleId.SkipSplash, "跳过开屏", "跳过启动商标，修改后下次启动生效。", "启动时跳过全部开屏商标。\n开关独立保存，修改后下次启动游戏时生效。"),
            new ModuleInfo(ModuleId.Stratagems, "地狱潜兵战备鸡械", "增加七台战备鸡械及其专属强化。", "MD-6地雷：3动力，冷却20秒；无限耐久陷阱。\n380火力网：6动力，冷却60秒；15发随机炮击。\n轨道激光：8动力，冷却60秒；持续15秒，每次300伤害；同目标照射3秒，间隔0.15秒加速至0.02秒。\n飞鹰500kg：4动力，冷却40秒；中心10000伤害，暴击率5%，暴击伤害1000%。\nEMS迫击炮：3动力，冷却25秒；远程眩晕和减速。\n护盾发生器：3动力，冷却30秒；5000生命、15格挡；半径4.5护罩，吸引仇恨并拦截弹丸。\n激光护卫犬：3动力，冷却30秒；固定基座，无限耐久无人机跟随所属角色，过热返航充能。\n\n通过商店、奖励与工匠获取和强化，攻击只伤敌方。"),
            new ModuleInfo(ModuleId.NativeRepairs, "原版运行修复", "修复随从动画、伤害通知和场景界面错误。", "补齐随从及炮塔动画引用；持续伤害、反伤正确通知受伤组件和血条。\n修复火山气泡初始化、重复天赋显示、结算箭头、突变信息与手柄滚动事件。\n主菜单演示使用适合菜单的控制与属性计算。\n修改开关后，下次启动游戏时生效。")
        };
    }

    internal sealed class GameplayModules
    {
        public bool Speed, Characters, Energy, Growth, Shop, Stratagems;
    }

    internal sealed class ShopProgress
    {
        public int Refreshes, FourthId = -1, FourthPrice;
        public bool TicketBought;
        public bool[] Sold = new bool[4];
        public bool Expanded => Refreshes >= 3;
        public void Refreshed() { Refreshes++; FourthId = -1; Sold = new bool[4]; }
    }

    internal sealed class PlayerProgress
    {
        public List<StratagemCooldown> StratagemCooldowns = new List<StratagemCooldown>();
        public int Coupons;
        public int ArtisanPaid, ArtisanSpend, ArtisanCredits;
        internal bool ValidArtisan => ArtisanPaid>=0 && ArtisanSpend>=0 && ArtisanSpend<600 && ArtisanCredits>=0;
        internal int CompleteArtisan(int tier)
        {
            int paid=ArtisanPaid;ArtisanPaid=0;
            int refund=ArtisanEconomy.Refund(paid,tier);
            int spent=ArtisanSpend+paid-refund;
            ArtisanCredits+=spent/600;ArtisanSpend=spent%600;
            return refund;
        }

        public int ExtraCarrySlots, ExtraPowerCapacity;
        public bool PendingMachineUpgrade;
        public HashSet<int> GrowthEvents = new HashSet<int>();
        public bool ValidGrowthRewards()
        {
            if(GrowthEvents==null || ExtraCarrySlots<0 || ExtraCarrySlots>1 || ExtraPowerCapacity<0 || ExtraPowerCapacity>5)return false;
            foreach(int id in GrowthEvents)if(id<88 || id>92)return false;
            return true;
        }
        public string PendingRegion;
        public HashSet<string> Regions = new HashSet<string>();
        public HashSet<string> RepairNodes = new HashSet<string>();
        public Dictionary<string, ShopProgress> Shops = new Dictionary<string, ShopProgress>();
        public ShopProgress ShopAt(string node)
        {
            if (!Shops.TryGetValue(node, out var shop)) Shops[node] = shop = new ShopProgress();
            return shop;
        }
        public bool OfferRegion(string region)
        {
            if (string.IsNullOrEmpty(region) || Regions.Contains(region) || PendingRegion == region) return false;
            PendingRegion = region; return true;
        }
        public bool ClaimRegion()
        {
            if (PendingRegion == null || !Regions.Add(PendingRegion)) return false;
            PendingRegion = null; return true;
        }
        public bool BuyCoupons(string node, int gold)
        {
            var shop = ShopAt(node);
            if (gold < 30 || shop.TicketBought) return false;
            shop.TicketBought = true; Coupons+=2; return true;
        }
        public bool ClaimRepair(string node)
        {
            if (string.IsNullOrWhiteSpace(node) || !RepairNodes.Add(node)) return false;
            Coupons+=4; return true;
        }
        public bool SpendCoupon(int freeRefreshes)
        {
            if (freeRefreshes > 0 || Coupons <= 0) return false;
            Coupons--; return true;
        }
        public bool ClaimGrowthEvent(int id, int choice=0, int gamble=0)
        {
            if (id < 88 || id > 92 || choice<0 || choice>1 || (gamble!=0 && gamble!=20) || !GrowthEvents.Add(id)) return false;
            if (id == 88) Coupons += choice==0 ? 10 : gamble;
            else if (id == 89 && choice==0) ExtraCarrySlots = 1;
            else if (id == 90 && choice==0) ExtraPowerCapacity = 5;
            else if (id == 91 && choice==1) PendingMachineUpgrade = true;
            return true;
        }
    }

    internal sealed class RunProgress
    {
        public bool EnergyUsed;
        public bool GrowthPoolsAdded;
        public int GrowthEventVersion;
        public int EnergyBonus, EnergyBonusUsed;
        internal int EnergyRemaining => (EnergyUsed ? 0 : 1)+EnergyBonus-EnergyBonusUsed;
        internal bool ValidEnergy => EnergyBonus>=0 && EnergyBonus<=2 && EnergyBonusUsed>=0 && EnergyBonusUsed<=EnergyBonus;
        public Dictionary<int, PlayerProgress> Players = new Dictionary<int, PlayerProgress>();
        public PlayerProgress Player(int role)
        {
            if (!Players.TryGetValue(role, out var player)) Players[role] = player = new PlayerProgress();
            return player;
        }
        public bool UseEnergy()
        {
            if (!EnergyUsed) { EnergyUsed = true; return true; }
            if (EnergyBonusUsed>=EnergyBonus) return false;
            EnergyBonusUsed++;return true;
        }
    }

    internal sealed class SpeedHolds
    {
        private readonly bool[] held = new bool[3];
        public bool Active => held[0] || held[1] || held[2];
        public bool Set(int role, bool value)
        {
            if (role < 0 || role > 2) return false;
            bool before = Active; held[role] = value; return before != Active;
        }
        public void Clear() { Array.Clear(held, 0, held.Length); }
    }
}
