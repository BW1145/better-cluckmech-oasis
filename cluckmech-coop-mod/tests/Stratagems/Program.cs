using System;
using System.Collections.Generic;
using CluckmechCoop;

internal static class Program
{
    private static int assertions;
    private static void Check(bool valid, string message)
    { assertions++; if (!valid) throw new Exception(message); }
    private static void Near(float actual, float expected, string message)
    { Check(Math.Abs(actual - expected) < .001f, message + $": {actual} != {expected}"); }
    private static StratagemParameters Params(int id, params int[] upgrades) => new StratagemParameters(id, upgrades);
    private static void Main()
    {
        var ids = new HashSet<int>();
        var ledger = new StratagemSequence();
        Check(ledger.Accept(1) && !ledger.Accept(1) && !ledger.Accept(0) && ledger.Accept(3) && !ledger.Accept(2), "Placement sequence executes each operation once");
        var one = new Machine { Id = 20001, UpgradeLimit = 4, Upgrades = new List<int> {25011} };
        var two = new Machine { Id = 20001, UpgradeLimit = 5, Upgrades = new List<int> {25012} };
        Check(StratagemCatalog.OwnsDeck(new[] {two,one,null,null,null},new[] {one,two}), "Reordered same-name cards keep exact upgrades and cap");
        Check(!StratagemCatalog.OwnsDeck(new[] {one,one,null,null,null},new[] {one,two}), "Deck cannot duplicate one inventory copy");
        Check(!StratagemCatalog.OwnsDeck(new[] {one.Copy()},new[] {two}), "Same identifier with different upgrades is rejected");
        foreach (var rows in StratagemCatalog.AtlasRows)
        {
            Check(rows.Length == 7 && rows[0] == 0 && rows[6] == 1086, "Atlas rows cover the complete source height");
            for (int row = 0; row < 6; row++) Check(rows[row+1] > rows[row], "Atlas frames have positive area");
        }
        Check(StratagemCatalog.Units.Length == 7 && StratagemCatalog.Affixes.Length == 42, "Seven machines and 42 upgrades");
        foreach (var unit in StratagemCatalog.Units)
        {
            Check(ids.Add(unit.Id), "Unique unit identifiers");
            var p = Params(unit.Id);
            Check(p.Power == unit.Power && p.Cooldown == unit.Cooldown, "Unmodified deployment uses catalog parameters");
            for (int n = 1; n <= 6; n++)
            {
                int id = 25010 + (unit.Id - 20001) * 10 + n;
                var upgrade = StratagemCatalog.Affix(id);
                Check(upgrade != null && ids.Add(id), "Each machine has six unique upgrade identifiers");
                Check(!StratagemCatalog.CanChoose(id, new[] { id }), "Owned unique upgrade excluded");
                if (upgrade.Excludes != 0)
                {
                    Check(StratagemCatalog.Affix(upgrade.Excludes).Excludes == id, "Exclusion is symmetric");
                    Check(!StratagemCatalog.CanChoose(id, new[] { upgrade.Excludes }), "Incompatible upgrade excluded");
                }
            }
        }
        var mines = Params(20001, 25011, 25012, 25014, 25016);
        Check(mines.MineCap == 8 && mines.MineInitial == 6 && mines.MineRefill == 4 && mines.Power == 5, "Mine ammunition upgrades combine");
        Near(mines.MineInterval(.12f), 8 / 1.2f, "Attack speed accelerates refill");
        Check(float.IsPositiveInfinity(mines.MineInterval(0)), "Zero attack rate pauses refill");
        var barrage = Params(20002, 25022, 25025);
        Near(barrage.BarrageEmission(17, 1), 13.5f, "Sixth salvo has three rounds");
        Near(barrage.BarrageEmission(17, 2), 6.75f, "Attack speed compresses salvo and intrabatch timing");
        Check(barrage.Salvos * 3 == 18, "Attack speed preserves ammunition");
        Check(float.IsPositiveInfinity(barrage.BarrageEmission(1, 0)), "Zero rate pauses barrage");
        var laser = Params(20003, 25031, 25032, 25035, 25036);
        foreach (float rate in new[] { 4f,4.8f,6f,8f })
        {
            var clock = new StratagemAttackClock(); int ticks = 0;
            for (int frame = 0; frame < 900; frame++) ticks += clock.Advance(1f/60,rate);
            Check(ticks == Math.Round(15*rate), "Exact fifteen-second tick budget at sixty frames per second");
        }
        var pausedClock = new StratagemAttackClock();
        Check(pausedClock.Advance(5,0) == 0 && pausedClock.Advance(.25f,4) == 1, "Paused attack clock does not queue attacks");
        Near(laser.Duration / laser.Interval(4), 72, "Longer laser duration has 72 base ticks");
        Near(laser.Duration / laser.Interval(8), 144, "Attack speed doubles laser ticks within same duration");
        Near(laser.BlastRadius, .55f, "Focused laser radius");
        var eagle = Params(20004);
        Near(eagle.EagleDamage(0), 3000, "Blast center");
        Near(eagle.EagleDamage(2.5f), 3000, "Inner radius edge");
        Near(eagle.EagleDamage(3.25f), 1900, "Linear falloff midpoint");
        Near(eagle.EagleDamage(4), 800, "Outer radius edge");
        Near(eagle.EagleDamage(4.01f), 0, "Outside blast");
        eagle = Params(20004, 25041, 25043, 25044, 25045);
        Check(eagle.Power == 6, "Efficient Eagle payload costs six power");
        Near(eagle.EagleDamage(4.8f), 1000, "Heavy expanded blast outer edge");
        Near(eagle.Cooldown, 41.4f, "Heavy payload and fast return cooldown");
        var mortar = Params(20005, 25051, 25052, 25053, 25055);
        Check(mortar.Gas && mortar.Stun == 0 && mortar.Slow == .5f, "Gas replaces stun while retaining slow");
        var rover = Params(20007, 25071, 25072, 25073, 25074);
        Check(Math.Ceiling(rover.HeatCap / rover.HeatPerShot) == 38, "Heat capacity and redline output affect burst length");
        Near(rover.DockTime + 2 * rover.ReturnTime, 3.5f, "Upgraded return-dock-launch duration");

        Check(ShieldGeometry.Entry(-5,0,0,0,0,0,3,out var t), "Outside projectile enters shield"); Near(t,.4f,"Entry position");
        Check(ShieldGeometry.Entry(-5,0,5,0,0,0,3,out t), "Fast projectile crosses entire shield"); Near(t,.2f,"Fast entry position");
        Check(!ShieldGeometry.Entry(0,0,5,0,0,0,3,out t), "Projectile fired inside passes outward");
        Check(!ShieldGeometry.Entry(-5,3,5,3,0,0,3,out t), "Tangent does not enter interior");
        Check(!ShieldGeometry.Entry(-5,0,-5,0,0,0,3,out t), "Stationary projectile does not enter");
        Check(ShieldGeometry.Entry(5,1,-5,1,0,0,2,out var far), "Far shield intersects");
        Check(ShieldGeometry.Entry(5,1,-5,1,3,0,2,out var near) && near < far, "Overlapping shields can be processed by trajectory order");
        float capacity = 100;
        Near(ShieldGeometry.Residual(50,1,ref capacity),0,"Fully blocked projectile"); Near(capacity,50,"Remaining shield capacity");
        Near(ShieldGeometry.Residual(80,1,ref capacity),30,"Partially blocked projectile passes residual"); Near(capacity,0,"Shield depleted");
        capacity = 100;
        Near(ShieldGeometry.Residual(200,.75f,ref capacity), 200 - 100 / .75f, "Fortress absorbs raw damage at reduced capacity cost");
        Console.WriteLine($"Stratagem rules: {assertions} assertions passed.");
    }
}
