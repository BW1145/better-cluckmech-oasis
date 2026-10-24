using System.Collections.Generic;

namespace CluckmechCoop
{
    // SPEC 2.2–2.8: single player, difficulty 15, late third-layer reference (SPEC 4.6).
    internal static class ThemeCastleData
    {
        internal const int FinalReferenceDifficulty = 15, FinalReferencePlayers = 1;
        internal const int FinalReferencePageMin = 7, FinalReferencePageMax = 8, FinalReferenceOrdinaryLevel = 32;
        internal const string Footman = "castle.footman", Knight = "castle.knight", Crossbow = "castle.crossbow";
        internal const string FlowerGhost = "castle.flower-ghost", Coffin = "castle.coffin", WineBat = "castle.wine-bat";
        internal const string Leader = "castle.knight-leader", Count = "castle.count", BossWineBat = "castle.boss-wine-bat";
        internal const string NorthWest = "north-west", NorthEast = "north-east", West = "west", East = "east";
        internal const string WestWall = "west-wall", EastWall = "east-wall", Throne = "throne";

        internal static readonly ThemeEnemyStats[] Enemies = {
            new ThemeEnemyStats(Footman, "枯藤步卒", 2000, 120, 1, 1.2f, 1.5f),
            new ThemeEnemyStats(Knight, "南瓜重甲骑士", 4000, 320, 1.8f, 1.5f, 0.9f, frontalDamageMultiplier: 0.5f),
            new ThemeEnemyStats(Crossbow, "荆棘弩手", 4000, 160, 4, 7, 1.2f, attackWindupSeconds: 0.8f),
            new ThemeEnemyStats(FlowerGhost, "石像花鬼", 3000, 200, 1.5f, 1.2f, 2.2f),
            new ThemeEnemyStats(Coffin, "棺木搬运者", 15000, 100, 2, 1.2f, 0.8f),
            new ThemeEnemyStats(WineBat, "葡萄侍酒蝠", 3000, 80, 1.2f, 1.2f, 2.5f),
            new ThemeEnemyStats(Leader, "骑士领队", 80000, 400, 1.8f, 1.5f, 1, frontalDamageMultiplier: 0.5f),
            new ThemeEnemyStats(Count, "猩红伯爵", 400000, 400, 1.5f, 1.8f, 1,
                finalBlock: 0, attackWindupSeconds: 0.45f),
            new ThemeEnemyStats(BossWineBat, "伯爵侍酒蝠", 5000, 80, 1.2f, 1.2f, 2.5f)
        };

        internal static readonly ThemeMapDefinition Courtyard = new ThemeMapDefinition(
            "castle.courtyard", "城门外庭", new[] { R(-14, 0, 14, 26) },
            new[] { new ThemeObstacle("flowerbed", new[] {
                P(-2.5f, 21), P(2.5f, 21), P(4, 18), P(4, 12),
                P(2.5f, 9), P(-2.5f, 9), P(-4, 12), P(-4, 18)
            }, false) },
            new[] {
                Horizontal(NorthWest, -5, 25, 3), Horizontal(NorthEast, 5, 25, 3),
                Vertical(West, -13, 18, 1.5f), Vertical(East, 13, 18, 1.5f),
                Vertical(WestWall, -13, 20.5f, 1.5f), Vertical(EastWall, 13, 20.5f, 1.5f)
            },
            new[] {
                new ThemeBaseHint("south-court", R(-6, 0, 6, 5), P(0, 2)),
                new ThemeBaseHint("stable-yard", R(-14, 6, -6, 12), P(-10, 9))
            },
            new[] {
                W(1, 30,
                    B(0, G(NorthWest, 8, 2)), B(8, G(NorthEast, 8, 2)),
                    B(16, G(NorthWest, 4, 1), G(NorthEast, 4, 1))),
                W(2, 45,
                    B(0, G(NorthWest, 10, 4, 2)), B(8, G(NorthEast, 10, 4, 2)),
                    B(16, G(East, 8, 2, 3))),
                W(3, 65,
                    B(0, G(NorthEast, 14, 4, 3), F(EastWall, 1)),
                    B(8, G(West, 12, 4, 3), F(WestWall, 2)),
                    B(16, G(NorthWest, 6, 2, 2), G(NorthEast, 6, 2, 2),
                        F(WestWall, 1), F(EastWall, 1, 2))),
                W(4, 75,
                    B(0, G(West, 14, 6, 4), F(WestWall, 2)),
                    B(8, G(East, 14, 6, 4), F(EastWall, 2)),
                    B(16, G(NorthWest, 6, 2, 2), G(NorthEast, 6, 2, 2),
                        F(WestWall, 2), F(EastWall, 1, 4))),
                W(5, 90,
                    B(0, G(NorthWest, 8, 3, 3), G(NorthEast, 8, 3, 2),
                        F(WestWall, 1), F(EastWall, 1, 2)),
                    B(8, G(East, 14, 8, 5, 1), F(EastWall, 2)),
                    B(16, G(West, 8, 3, 3), G(NorthWest, 8, 3, 2),
                        F(WestWall, 2, 0, 4), F(EastWall, 2, 2, 4)))
            }, Leader);

        internal static readonly ThemeMapDefinition Banquet = new ThemeMapDefinition(
            "castle.banquet", "破败宴会厅", new[] { R(-12, 0, 12, 26), R(-20, 8, -12, 16) },
            new[] {
                ThemeObstacle.Rectangle("west-table-south", R(-6, 7, -3, 12)),
                ThemeObstacle.Rectangle("west-table-north", R(-6, 16, -3, 21)),
                ThemeObstacle.Rectangle("east-table-south", R(3, 7, 6, 12)),
                ThemeObstacle.Rectangle("east-table-north", R(3, 16, 6, 21))
            },
            new[] {
                Horizontal(NorthWest, -4, 25, 3), Horizontal(NorthEast, 4, 25, 3),
                Vertical(West, -11.5f, 22.5f, 2), Vertical(East, 11.5f, 22.5f, 2)
            },
            new[] {
                new ThemeBaseHint("south-vestibule", R(-5, 0, 5, 6), P(0, 3)),
                new ThemeBaseHint("musician-platform", R(-19, 9, -13, 15), P(-16, 12))
            },
            new[] {
                W(1, 35,
                    B(0, H(NorthWest, 9, 2), S(West, 1)),
                    B(8, H(NorthEast, 8, 2), S(East, 1)),
                    B(16, H(NorthWest, 4, 1), H(NorthEast, 4, 1), S(West, 1), S(East, 1))),
                W(2, 50,
                    B(0, H(NorthWest, 10, 4, 2), S(West, 1)),
                    B(8, H(NorthEast, 10, 4, 2), S(East, 1)),
                    B(16, H(NorthWest, 5, 1, 1), H(NorthEast, 5, 1, 1), S(West, 1), S(East, 1))),
                W(3, 65,
                    B(0, H(NorthWest, 12, 4, 2), S(West, 2, 1)),
                    B(8, H(NorthEast, 12, 4, 3), S(East, 2, 1)),
                    B(16, H(NorthWest, 6, 2, 1), H(NorthEast, 6, 2, 2), S(West, 1, 1), S(East, 1))),
                W(4, 80,
                    B(0, H(NorthWest, 8, 4), H(West, 6, 2, 3), S(West, 2, 1)),
                    B(8, H(NorthEast, 8, 4), H(East, 6, 2, 3), S(East, 3, 1)),
                    B(16, H(NorthWest, 7, 2, 2), H(NorthEast, 7, 2, 2), S(West, 1, 1), S(East, 2, 1))),
                W(5, 95,
                    B(0, H(NorthEast, 12, 4, 2), H(East, 6, 2, 2), S(East, 3, 1)),
                    B(8, H(NorthWest, 16, 6, 4, 1), S(West, 3, 1)),
                    B(16, H(NorthWest, 8, 3, 2), H(NorthEast, 8, 3, 2), S(West, 2, 1), S(East, 2, 1)))
            }, Leader);

        internal static readonly ThemeMapDefinition CountHall = new ThemeMapDefinition(
            "castle.count-hall", "伯爵大厅", new[] { R(-16, 0, 16, 28) },
            new[] {
                new ThemeObstacle("pillar-north-west", P(-6, 19), 1.25f),
                new ThemeObstacle("pillar-north-east", P(6, 19), 1.25f),
                new ThemeObstacle("pillar-south-west", P(-6, 9), 1.25f),
                new ThemeObstacle("pillar-south-east", P(6, 9), 1.25f),
                ThemeObstacle.Rectangle("wine-altar-west", R(-14, 10.5f, -12, 12.5f)),
                ThemeObstacle.Rectangle("wine-altar-east", R(12, 10.5f, 14, 12.5f))
            },
            new[] {
                Horizontal(Throne, 0, 25, 2),
                Horizontal(West, -13, 27, 2), Horizontal(East, 13, 27, 2)
            },
            new[] {
                new ThemeBaseHint("south-hall", R(-5, 0, 5, 5), P(0, 3)),
                new ThemeBaseHint("west-hall", R(-15, 3, -9, 9), P(-12, 6))
            },
            new[] { W(1, 1, B(0, new[] { new ThemeSpawnGroup(Count, Throne, 1) })) }, Count);

        internal static readonly CastleChandelierDefinition[] Chandeliers = {
            new CastleChandelierDefinition("west-chandelier", P(-9, 14), P(-13, 11)),
            new CastleChandelierDefinition("east-chandelier", P(9, 14), P(11, 11))
        };

        internal static readonly CastleCountParameters CountParameters = new CastleCountParameters();

        internal const int CrossbowArrows = 3;
        internal const int CrossbowMaximumHitsPerVolley = 1;
        internal const float FlowerWarningSeconds = 1.2f, FlowerRadius = 1.5f, FlowerFinalDamage = 400;
        internal const float FlowerLandingPauseSeconds = 1, FlowerLockSpacingSeconds = 2;
        internal const int CoffinReleaseCount = 6, CoffinReleaseBatches = 2;
        internal const float CoffinRemainingPathDistance = 6, CoffinOpeningSeconds = 2, CoffinSecondReleaseDelaySeconds = 6;
        internal const float WinePourSeconds = 1, OrdinaryWineHealFraction = 0.3f;
        internal const float WineFollowDistance = 2, WineTargetMissingGraceSeconds = 3;

        internal static ThemeEnemyStats FindEnemy(string id)
        {
            foreach (var enemy in Enemies) if (enemy.Id == id) return enemy;
            return null;
        }

        internal static ThemeMapDefinition FindMap(string id)
        {
            if (id == Courtyard.Id) return Courtyard;
            if (id == Banquet.Id) return Banquet;
            return id == CountHall.Id ? CountHall : null;
        }

        // event choice 0 seals the east cellar, choice 1 equips all four pillars.
        internal static float CountPillarStunSeconds(int eventChoice) => eventChoice == 1 ? 6 : 3;

        internal static ThemeSpawnGroup[] CountSupply(int eventChoice, bool firstBatch)
        {
            var groups = new List<ThemeSpawnGroup> {
                new ThemeSpawnGroup(BossWineBat, West, 2)
            };
            if (eventChoice == 0) groups.Add(new ThemeSpawnGroup(Knight, East, 2));
            else groups.Add(new ThemeSpawnGroup(BossWineBat, East, 2));
            if (firstBatch)
            {
                groups.Add(new ThemeSpawnGroup(Knight, West, 1));
                groups.Add(new ThemeSpawnGroup(Knight, East, 1));
            }
            return groups.ToArray();
        }

        private static ThemePoint P(float x, float y) => new ThemePoint(x, y);
        private static ThemeRect R(float minX, float minY, float maxX, float maxY) => new ThemeRect(minX, minY, maxX, maxY);
        private static ThemeEntrance Horizontal(string id, float x, float y, float width) =>
            new ThemeEntrance(id, P(x, y), P(x - width * 0.5f, y), P(x + width * 0.5f, y));
        private static ThemeEntrance Vertical(string id, float x, float y, float height) =>
            new ThemeEntrance(id, P(x, y), P(x, y - height * 0.5f), P(x, y + height * 0.5f));
        private static ThemeWave W(int number, int expected, params ThemeBatch[] batches) => new ThemeWave(number, expected, batches);
        private static ThemeBatch B(float at, params ThemeSpawnGroup[][] sections)
        {
            var groups = new List<ThemeSpawnGroup>();
            foreach (var section in sections) groups.AddRange(section);
            return new ThemeBatch(at, groups.ToArray());
        }
        private static ThemeSpawnGroup[] G(string gate, int foot, int knight, int bow = 0, int leader = 0) =>
            Ground(gate, foot, knight, bow, leader, 3);
        private static ThemeSpawnGroup[] H(string gate, int foot, int knight, int bow = 0, int leader = 0) =>
            Ground(gate, foot, knight, bow, leader, 2);
        private static ThemeSpawnGroup[] Ground(string gate, int foot, int knight, int bow, int leader, float bowDelay)
        {
            var groups = new List<ThemeSpawnGroup>(4);
            if (leader > 0) groups.Add(new ThemeSpawnGroup(Leader, gate, leader));
            if (knight > 0) groups.Add(new ThemeSpawnGroup(Knight, gate, knight));
            if (foot > 0) groups.Add(new ThemeSpawnGroup(Footman, gate, foot, 1));
            if (bow > 0) groups.Add(new ThemeSpawnGroup(Crossbow, gate, bow, bowDelay));
            return groups.ToArray();
        }
        private static ThemeSpawnGroup[] F(string gate, int count, float delay = 0, float interval = 2) =>
            new[] { new ThemeSpawnGroup(FlowerGhost, gate, count, delay, interval) };
        private static ThemeSpawnGroup[] S(string gate, int bats, int coffins = 0)
        {
            var groups = new List<ThemeSpawnGroup>(2);
            if (coffins > 0) groups.Add(new ThemeSpawnGroup(Coffin, gate, coffins));
            if (bats > 0) groups.Add(new ThemeSpawnGroup(WineBat, gate, bats, 3));
            return groups.ToArray();
        }
    }

    internal sealed class CastleChandelierDefinition
    {
        internal readonly string Id;
        internal readonly ThemePoint Impact, Winch;
        internal readonly float InteractionRadius = 1.5f, WarningSeconds = 1, Radius = 2.5f;
        internal readonly float FinalDamage = 24000, StunSeconds = 3;
        internal readonly int Uses = 1;
        internal CastleChandelierDefinition(string id, ThemePoint impact, ThemePoint winch)
        { Id = id; Impact = impact; Winch = winch; }
    }

    internal sealed class CastleCountParameters
    {
        internal readonly float EntrySeconds = 2, PhaseTwoHealthFraction = 0.5f, PhaseTransitionSeconds = 0.8f;
        internal readonly float ChargeWarningSeconds = 1.5f, ChargeCooldownSeconds = 12, ChargeSpeed = 16;
        internal readonly float ChargeWidth = 2.4f, ChargeFinalDamage = 800, BoundaryPauseSeconds = 1;
        internal readonly int PhaseOneCharges = 1, PhaseTwoCharges = 2;
        internal readonly float BetweenChargesSeconds = 0.6f;
        internal readonly float VineInitialDelaySeconds = 6, VineCooldownSeconds = 6, VineWarningSeconds = 0.9f;
        internal readonly float VineActionSeconds = 1.4f, VineLength = 8, VineWidth = 0.8f;
        internal readonly float VineAngleDegrees = 30, VineFinalDamage = 250;
        internal readonly int VineCount = 3;
        internal readonly float WineBatchIntervalSeconds = 10, WineHealFraction = 0.1f, WineArrivalTolerance = 0.5f;
        internal readonly ThemePoint[] WestWinePath = {
            new ThemePoint(-13, 27), new ThemePoint(-13, 20), new ThemePoint(-13, 13.5f)
        };
        internal readonly ThemePoint[] EastWinePath = {
            new ThemePoint(13, 27), new ThemePoint(13, 20), new ThemePoint(13, 13.5f)
        };
        internal readonly float[] WineStationOffsetsX = { -0.5f, 0.5f };
    }
}
