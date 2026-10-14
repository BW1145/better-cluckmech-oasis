using System;
using System.Collections.Generic;

namespace CluckmechCoop
{
    internal enum ModuleId { Speed, Characters, Energy, Music, Growth, Shop, Expedition, Transfer, SkipSplash, Stratagems }

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
            new ModuleInfo(ModuleId.Speed, "倍速", "按住左Ctrl加快战斗，松开恢复。", "左Ctrl：2倍速\n联机时任意一人按住，双方一起加速；全部松开后恢复。移动、攻击、刷怪、冷却和持续效果同步加速。"),
            new ModuleInfo(ModuleId.Characters, "更好的角色右键强化", "增强五个角色的右键攻击与动作。", "战斗鸡：三连霰弹，间隔0.2秒，冷却4秒。\n工程狮：集结时移速提高30%、蓄力时间×0.75；松手后集结战士伤害提高25%、攻速提高20%，持续4秒，冷却8秒。\n肌肉鼠：移动治疗；按下额外治疗20%最大生命，冷却10秒。\n阿法狗：召唤并自爆，伤害300，冷却30秒；召唤物前5秒生命、格挡×2。\n八酱兔：向鼠标处召唤；前5秒伤害提高50%、移速提高30%。"),
            new ModuleInfo(ModuleId.Energy, "基地车后备隐藏能源", "按F强化基地车，整局共用一次。", "F：启动后备隐藏能源\n受到伤害减少100%，攻击力增加500，持续10秒。整局双方共用一次。"),
            new ModuleInfo(ModuleId.Music, "音乐扩展", "主菜单播放《春日影》，扩展战斗音乐。", "主菜单播放《春日影 (MyGO!!!!! ver.)》。\n七个区域各增加一首音乐，原版与新增各有50%概率。\n普通Boss从原版两首和《铜管通缉令》中随机选一首，各占1/3。\n响度统一，使用原版音乐音量设置。"),
            new ModuleInfo(ModuleId.Growth, "强化成长", "区域通关增加强化空间，商店出售刷新券。", "区域关底完成后，每人选一台鸡械，强化上限+1。\n工匠刷新券：30金币，增加3次刷新，每家商店限购一次；剩余次数本局保留。"),
            new ModuleInfo(ModuleId.Shop, "商店扩展", "同一家商店刷新3次后提供4台鸡械。", "成功付费刷新达到3次后，每次提供4台鸡械供购买。刷新价格沿用原版，新商店重新累计。"),
            new ModuleInfo(ModuleId.Expedition, "强化远征", "追加强化敌人，并增强绿洲最终Boss。", "进关前独立选择。原版敌人全部出完并消灭后，再出现一次；生命×1.5，攻击×2。\n绿洲最终Boss只出现一次，生命×10、攻击×2。\n完成后获得显示的额外奖励。"),
            new ModuleInfo(ModuleId.Transfer, "队友转交", "向队友转交金币或指定鸡械。", "在路线地图等关间界面打开队友转交。鸡械保留强化词条与强化上限，选中后确认转交。"),
            new ModuleInfo(ModuleId.SkipSplash, "跳过开屏", "跳过启动商标，修改后下次启动生效。", "启动时跳过全部开屏商标。\n开关独立保存，修改后下次启动游戏时生效。"),
            new ModuleInfo(ModuleId.Stratagems, "地狱潜兵战备鸡械", "增加七台战备鸡械及其专属强化。", "MD-6地雷：4动力，冷却30秒。\n380火力网：9动力，冷却60秒；15发随机炮击。\n轨道激光：8动力，冷却60秒；持续15秒追踪照射。\n飞鹰500kg：8动力，冷却45秒；中心3000伤害。\nEMS迫击炮：3动力，冷却25秒；远程眩晕和减速。\n护盾发生器：5动力，冷却30秒；吸引仇恨并拦截弹丸。\n激光护卫犬：5动力，冷却30秒；随身无人机，过热返航。\n\n通过商店、奖励与工匠获取和强化，攻击只伤敌方。")
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
        public string PendingRegion;
        public HashSet<string> Regions = new HashSet<string>();
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
            shop.TicketBought = true; Coupons += 3; return true;
        }
        public bool SpendCoupon(int freeRefreshes)
        {
            if (freeRefreshes > 0 || Coupons <= 0) return false;
            Coupons--; return true;
        }
    }

    internal sealed class RunProgress
    {
        public bool EnergyUsed;
        public Dictionary<int, PlayerProgress> Players = new Dictionary<int, PlayerProgress>();
        public PlayerProgress Player(int role)
        {
            if (!Players.TryGetValue(role, out var player)) Players[role] = player = new PlayerProgress();
            return player;
        }
        public bool UseEnergy()
        {
            if (EnergyUsed) return false;
            EnergyUsed = true; return true;
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
