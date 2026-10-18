using System;
using System.Collections.Generic;

namespace CluckmechCoop
{
    internal enum ModuleId { Speed, Characters, Energy, Music, Growth, Shop, Expedition, Transfer, SkipSplash, Stratagems, NativeRepairs, Boons, Wealth, Otherworld }

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
            new ModuleInfo(ModuleId.Otherworld, "异界角色", "佩丽卡与宇智波带土，包含专属成长及神威空间。", "佩丽卡：电磁轰击、闪链、战备调度与过载授权。\n宇智波带土：虚化冲刺、神威空间、火遁与空间阵地。\n各有四项初始天赋及十二项局内成长；神威空间本局跨关保留。"),
            new ModuleInfo(ModuleId.Speed, "倍速", "左Shift切换2倍速，按住Ctrl开启3倍速。", "左Shift：按一下开启2倍速，再按一下关闭。\nCtrl：按住开启3倍速，松开回到1倍速。\n联机任意玩家均可控制双方速度。移动、攻击、刷怪、冷却和持续效果同步加速。"),
            new ModuleInfo(ModuleId.Characters, "更好的角色右键强化", "增强角色右键，并改善鼠标吞噬选择。", "战斗鸡：三连霰弹，间隔0.2秒，冷却4秒。\n工程狮：集结时移速×2、蓄力时间×0.75；松手后集结战士伤害提高25%、攻速提高20%，持续4秒，冷却8秒。\n肌肉鼠：移动治疗；按下额外治疗20%最大生命，冷却10秒。\n阿法狗：召唤并自爆，伤害300；召唤物前5秒生命、格挡×2。\n八酱兔：向鼠标处召唤；前5秒伤害提高50%、移速提高30%。\n肚子鳄、水母骑士：优先吞噬范围内鼠标指向的合法目标；空地仍选最近目标。"),
            new ModuleInfo(ModuleId.Energy, "基地车后备隐藏能源", "按F强化基地车，可通过事件补充次数。", "F：启动后备隐藏能源\n受到伤害减少100%，攻击力增加500，持续10秒。初始整局双方共用一次；第二层事件可补充2次。"),
            new ModuleInfo(ModuleId.Music, "音乐扩展", "主菜单播放《春日影》，扩展战斗音乐。", "主菜单播放《春日影 (MyGO!!!!! ver.)》。\n矿洞：原版、《岩窟快车》《矿脉引擎》各有1/3概率。其他六个区域：原版与新增各有50%概率。\n普通Boss从原版两首和《铜管通缉令》中随机选一首，各占1/3。\n响度统一，使用原版音乐音量设置。"),
            new ModuleInfo(ModuleId.Growth, "强化成长", "扩充强化空间、成长事件与工匠强化回馈。", "区域关底完成后，每人选C级鸡械上限+3，或B级上限+1；可以跳过。\n工匠强化：C级返款50%，B级返款20%；每累计800金币强化净花费，可为已有鸡械自选1项强化，进度跨工匠保留。\n工匠刷新券：30金币增加2次刷新，每家限购一次；维修节点额外赠送4次。\n第二层问号事件：10次工匠刷新或50%获得20次；所有C级鸡械上限+3或指定任意鸡械上限+1；后备隐藏能源+2次或100金币。\n第三层问号事件：携带上限5→6或150金币；战斗动力上限20→25或150金币。\n事件效果与剩余刷新次数本局保留。第六格使用数字键6。"),
            new ModuleInfo(ModuleId.Shop, "商店扩展", "扩充商品，并提供消费轮盘与刷新回馈。", "同店成功付费刷新3次后提供4台鸡械。\n本局累计30次成功付费刷新，获得1次自选鸡械机会。\n商店消费每800金币可抽轮盘：随机4台鸡械40%、工匠刷新10次30%、500金币25%、自选鸡械1次5%。\n消费、刷新余数与奖励机会跨店保留；鸡械奖励使用领取商店的合法商品范围。"),
            new ModuleInfo(ModuleId.Expedition, "强化远征", "追加强化敌人，并增强绿洲最终Boss。", "进关前独立选择。原版敌人全部出完并消灭后，再出现一次；生命×2，攻击×2。\n绿洲最终Boss只出现一次，生命×10、攻击×2。\n完成后获得显示的额外奖励。"),
            new ModuleInfo(ModuleId.Transfer, "队友转交", "向队友转交金币或指定鸡械。", "在路线地图等关间界面打开队友转交。鸡械保留强化词条与强化上限，选中后确认转交。"),
            new ModuleInfo(ModuleId.SkipSplash, "跳过开屏", "跳过启动商标，修改后下次启动生效。", "启动时跳过全部开屏商标。\n开关独立保存，修改后下次启动游戏时生效。"),
            new ModuleInfo(ModuleId.Stratagems, "地狱潜兵战备鸡械", "增加七台战备鸡械及其专属强化。", "MD-6地雷：3动力，冷却20秒；无限耐久陷阱。\n380火力网：6动力，冷却60秒；15发随机炮击。\n轨道激光：8动力，冷却60秒；持续15秒，每次300伤害；同目标照射3秒，间隔0.15秒加速至0.02秒。\n飞鹰500kg：4动力，冷却40秒；中心10000伤害，暴击率5%，暴击伤害1000%。\nEMS迫击炮：3动力，冷却25秒；远程眩晕和减速。\n护盾发生器：3动力，冷却30秒；5000生命、15格挡；半径4.5护罩，吸引仇恨并拦截弹丸。\n激光护卫犬：3动力，冷却30秒；固定基座，无限耐久无人机跟随所属角色，过热返航充能。\n\n通过商店、奖励与工匠获取和强化，攻击只伤敌方。"),
            new ModuleInfo(ModuleId.Boons, "战备协同", "收集不同评级鸡械，获得本场角色增益。", "每满足一项提升一档：剩余后备能源至少1次；背包持有2、3、4种不同评级鸡械。\n四档累计生命+100%／300%／500%／1000%。第二档攻速+5%；第三档攻击+10%，每场一次致命伤害保留1生命、无敌2秒；第四档累计攻速+40%、攻击+100%。\n仅作用于玩家角色。地图显示当前条件与收益，开战锁定本场效果。"),
            new ModuleInfo(ModuleId.Wealth, "财不露白", "携带超过2000金币进入战斗，会招来额外强敌。", "开战时每位持有超过2000金币的玩家各招来12只本区域强敌，分3批出现，生命×3、攻击×1.5。\n准备界面显示预计强敌数量。持续持有超过2000金币则每场触发。"),
            new ModuleInfo(ModuleId.NativeRepairs, "原版运行修复", "修复随从动画、伤害通知和场景界面错误。", "补齐随从及炮塔动画引用；持续伤害、反伤正确通知受伤组件和血条。\n修复火山气泡初始化、重复天赋显示、结算箭头、突变信息与手柄滚动事件。\n主菜单演示使用适合菜单的控制与属性计算。\n修改开关后，下次启动游戏时生效。")
        };
    }

    internal sealed class GameplayModules
    {
        public bool Speed, Characters, Energy, Growth, Shop, Stratagems, Boons, Wealth, Otherworld;
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
        public int ShopSpend, ShopRefreshCount, ShopPicks, ShopSpins;
        public string ShopReceipt;
        internal bool ValidShop => ShopSpend>=0 && ShopSpend<800 && ShopRefreshCount>=0 && ShopRefreshCount<30 && ShopPicks>=0 && ShopSpins>=0;
        internal void ShopSpent(int gold, bool refreshed=false)
        {
            if(gold<=0)return;
            long spend=(long)ShopSpend+gold;
            ShopSpins+=(int)(spend/800);ShopSpend=(int)(spend%800);
            if(refreshed && ++ShopRefreshCount==30){ShopRefreshCount=0;ShopPicks++;}
        }
        internal const int ArtisanThreshold = 800;
        internal bool ValidArtisan => ArtisanPaid>=0 && ArtisanSpend>=0 && ArtisanSpend<ArtisanThreshold && ArtisanCredits>=0;
        internal int CompleteArtisan(int tier)
        {
            int paid=ArtisanPaid;ArtisanPaid=0;
            int refund=ArtisanEconomy.Refund(paid,tier);
            int spent=ArtisanSpend+paid-refund;
            ArtisanCredits+=spent/ArtisanThreshold;ArtisanSpend=spent%ArtisanThreshold;
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
        public Dictionary<int,int> BattleBoons = new Dictionary<int,int>();
        internal bool ValidBoons
        {
            get{if(BattleBoons==null)return false;foreach(var pair in BattleBoons)if(pair.Key<0 || pair.Key>2 || pair.Value<0 || pair.Value>4)return false;return true;}
        }
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
