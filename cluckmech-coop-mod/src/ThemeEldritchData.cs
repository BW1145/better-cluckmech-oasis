using System.Collections.Generic;

namespace CluckmechCoop
{
    // SPEC 5.1–5.6. Variants share the regular maps' existing data where their rules agree.
    internal static class ThemeEldritchData
    {
        internal const string ShadowKnight = "castle.shadow-knight", ShadowCrossbow = "castle.shadow-crossbow";
        internal const string ShadowLeader = "castle.shadow-leader", BanquetEater = "castle.banquet-eater";
        internal const string HangingCount = "castle.hanging-count", FleshKnot = "castle.flesh-knot";
        internal const string BloodMother = "deepsea.blood-mother", BloodSac = "deepsea.blood-sac";
        internal const string WhiteVisitor = "deepsea.white-visitor", WhiteHand = "deepsea.white-hand", Drowned = "deepsea.drowned";
        internal const string NorthWest = "north-west", NorthEast = "north-east", West = "west", East = "east", Mouth = "mouth";
        internal const string TableWestToEast = "table-west-to-east", TableEastToWest = "table-east-to-west";
        internal const string PipeWestToEast = "pipe-west-to-east", PipeEastToWest = "pipe-east-to-west";

        internal static readonly ThemeEnemyStats[] Enemies = {
            new ThemeEnemyStats(ShadowKnight, "附影重甲骑士", 4000, 320, 1.8f, 1.5f, 0.9f, frontalDamageMultiplier: 0.5f),
            new ThemeEnemyStats(ShadowCrossbow, "附影弩手", 4000, 160, 4, 7, 1.2f, attackWindupSeconds: 0.8f),
            new ThemeEnemyStats(ShadowLeader, "影旗领队", 80000, 400, 1.8f, 1.5f, 1, frontalDamageMultiplier: 0.5f),
            new ThemeEnemyStats(BanquetEater, "席外食客", 160000, 400, 8, 24, 0, finalBlock: 0, attackWindupSeconds: 2),
            new ThemeEnemyStats(HangingCount, "倒悬伯爵", 400000, 400, 1.5f, 1.8f, 1, finalBlock: 0, attackWindupSeconds: 0.45f),
            new ThemeEnemyStats(FleshKnot, "牵线肉结", 12000, 600, 10, 22, 0, finalBlock: 0),
            new ThemeEnemyStats(BloodMother, "蓄血囊母", 200000, 400, 2, 2, 0.9f,
                finalBlock: 0, attackWindupSeconds: 0.6f),
            new ThemeEnemyStats(BloodSac, "血囊", 6000, 500, 4, 2, 0, finalBlock: 0),
            new ThemeEnemyStats(WhiteVisitor, "白渊来客", 800000, 450, 3, 3.5f, 0, finalBlock: 0, attackWindupSeconds: 1),
            new ThemeEnemyStats(WhiteHand, "白渊巨掌", 800000, 400, 5, 2.2f, 0.65f, finalBlock: 0),
            new ThemeEnemyStats(Drowned, "溺行者", 2000, 150, 1.5f, 1.2f, 1.3f)
        };

        internal static readonly ThemeUnitReplacement[] ShadowAssignments = {
            Shadow(1, 0, NorthWest, ThemeCastleData.Knight, ShadowKnight, 1),
            Shadow(1, 1, NorthEast, ThemeCastleData.Knight, ShadowKnight, 1),
            Shadow(2, 0, NorthWest, ThemeCastleData.Knight, ShadowKnight, 1),
            Shadow(2, 0, NorthWest, ThemeCastleData.Crossbow, ShadowCrossbow, 1),
            Shadow(2, 1, NorthEast, ThemeCastleData.Knight, ShadowKnight, 1),
            Shadow(2, 2, East, ThemeCastleData.Crossbow, ShadowCrossbow, 1),
            Shadow(3, 0, NorthEast, ThemeCastleData.Knight, ShadowKnight, 1),
            Shadow(3, 0, NorthEast, ThemeCastleData.Crossbow, ShadowCrossbow, 1),
            Shadow(3, 1, West, ThemeCastleData.Knight, ShadowKnight, 1),
            Shadow(3, 1, West, ThemeCastleData.Crossbow, ShadowCrossbow, 1),
            Shadow(3, 2, NorthWest, ThemeCastleData.Knight, ShadowKnight, 1),
            Shadow(3, 2, NorthWest, ThemeCastleData.Crossbow, ShadowCrossbow, 1),
            Shadow(4, 0, West, ThemeCastleData.Knight, ShadowKnight, 2),
            Shadow(4, 0, West, ThemeCastleData.Crossbow, ShadowCrossbow, 2),
            Shadow(4, 1, East, ThemeCastleData.Knight, ShadowKnight, 1),
            Shadow(4, 1, East, ThemeCastleData.Crossbow, ShadowCrossbow, 1),
            Shadow(4, 2, NorthWest, ThemeCastleData.Knight, ShadowKnight, 1),
            Shadow(4, 2, NorthWest, ThemeCastleData.Crossbow, ShadowCrossbow, 1),
            Shadow(5, 0, NorthWest, ThemeCastleData.Knight, ShadowKnight, 1),
            Shadow(5, 0, NorthWest, ThemeCastleData.Crossbow, ShadowCrossbow, 1),
            Shadow(5, 0, NorthEast, ThemeCastleData.Knight, ShadowKnight, 1),
            Shadow(5, 1, East, ThemeCastleData.Knight, ShadowKnight, 2),
            Shadow(5, 1, East, ThemeCastleData.Crossbow, ShadowCrossbow, 3),
            Shadow(5, 1, East, ThemeCastleData.Leader, ShadowLeader, 1),
            Shadow(5, 2, West, ThemeCastleData.Knight, ShadowKnight, 1),
            Shadow(5, 2, West, ThemeCastleData.Crossbow, ShadowCrossbow, 1)
        };

        internal static readonly ThemeMapDefinition ShadowCourtyard = CopyMap(
            ThemeCastleData.Courtyard, "castle.shadow-courtyard", "无主之影",
            ReplaceUnits(ThemeCastleData.Courtyard.Waves, ShadowAssignments), ShadowLeader);

        internal static readonly EldritchTransportLink[] TableLinks = {
            new EldritchTransportLink(TableWestToEast, P(-4.5f, 22), P(4.5f, 6)),
            new EldritchTransportLink(TableEastToWest, P(4.5f, 22), P(-4.5f, 6))
        };

        // BatchIndex is zero based. Members are selected from the named original spawn group.
        internal static readonly EldritchTransportAssignment[] TableAssignments = {
            T(1, 0, NorthWest, ThemeCastleData.Knight, 1, TableWestToEast),
            T(1, 1, NorthEast, ThemeCastleData.Knight, 1, TableEastToWest),
            T(2, 0, NorthWest, ThemeCastleData.Knight, 1, TableWestToEast),
            T(2, 1, NorthEast, ThemeCastleData.Knight, 1, TableEastToWest),
            T(2, 2, NorthWest, ThemeCastleData.Knight, 1, TableWestToEast),
            T(2, 2, NorthEast, ThemeCastleData.Knight, 1, TableEastToWest),
            T(3, 0, NorthWest, ThemeCastleData.Knight, 2, TableWestToEast),
            T(3, 1, NorthEast, ThemeCastleData.Knight, 2, TableEastToWest),
            T(3, 2, NorthWest, ThemeCastleData.Knight, 1, TableWestToEast),
            T(3, 2, NorthEast, ThemeCastleData.Knight, 1, TableEastToWest),
            T(3, 2, West, ThemeCastleData.Coffin, 1, TableWestToEast),
            T(4, 0, NorthWest, ThemeCastleData.Knight, 3, TableWestToEast),
            T(4, 0, West, ThemeCastleData.Coffin, 1, TableWestToEast),
            T(4, 1, NorthEast, ThemeCastleData.Knight, 3, TableEastToWest),
            T(4, 1, East, ThemeCastleData.Coffin, 1, TableEastToWest),
            T(4, 2, NorthWest, ThemeCastleData.Knight, 1, TableWestToEast),
            T(4, 2, NorthEast, ThemeCastleData.Knight, 1, TableEastToWest),
            T(5, 0, NorthEast, ThemeCastleData.Knight, 2, TableEastToWest),
            T(5, 1, NorthWest, ThemeCastleData.Knight, 3, TableWestToEast),
            T(5, 1, West, ThemeCastleData.Coffin, 1, TableWestToEast),
            T(5, 2, NorthWest, ThemeCastleData.Knight, 1, TableWestToEast),
            T(5, 2, NorthEast, ThemeCastleData.Knight, 2, TableEastToWest),
            T(5, 2, East, ThemeCastleData.Coffin, 1, TableEastToWest)
        };

        internal static readonly ThemeMapDefinition Banquet = CopyMap(
            ThemeCastleData.Banquet, "castle.outside-banquet", "席外之客",
            ReplaceAll(ThemeCastleData.Banquet.Waves, ThemeCastleData.Leader, BanquetEater), BanquetEater);

        internal static readonly CastleChandelierDefinition[] TableChandeliers = {
            new CastleChandelierDefinition("west-chandelier", P(-4.5f, 6), P(-13, 11)),
            new CastleChandelierDefinition("east-chandelier", P(4.5f, 6), P(11, 11))
        };
        internal static readonly ThemePoint[] BanquetMouths = { P(-4.5f, 22), P(4.5f, 22), P(4.5f, 6), P(-4.5f, 6) };

        internal static readonly ThemeMapDefinition HangingHall = CopyMap(
            ThemeCastleData.CountHall, "castle.hanging-hall", "倒悬王庭",
            ReplaceAll(ThemeCastleData.CountHall.Waves, ThemeCastleData.Count, HangingCount), HangingCount);

        internal static readonly ThemeMapDefinition BloodGallery = new ThemeMapDefinition(
            "deepsea.blood-gallery", "回望之井", ThemeDeepSeaData.Gallery.Floors,
            new[] {
                ThemeObstacle.Rectangle("well-south", R(-4, 7, 4, 12), false),
                ThemeObstacle.Rectangle("well-north", R(-4, 16, 4, 21), false),
                ThemeObstacle.Rectangle("well-west", R(-4, 12, -2, 16), false),
                ThemeObstacle.Rectangle("well-east-south", R(2, 12, 4, 13), false),
                ThemeObstacle.Rectangle("well-east-north", R(2, 15, 4, 16), false)
            }, ThemeDeepSeaData.Gallery.Entrances, ThemeDeepSeaData.Gallery.BaseHints,
            ThemeDeepSeaData.Gallery.Waves, ThemeDeepSeaData.CrownLily);

        internal static readonly ThemeRect[] BloodMotherFacilities = { R(-2, 12, 2, 16), R(2, 13, 9, 15) };
        internal static readonly ThemePoint BloodMotherInitialPosition = P(0, 14);
        internal static readonly ThemePoint[] BloodMotherWakePath = { P(0, 14), P(9, 14) };

        internal static readonly ThemeMapDefinition PumpLung = new ThemeMapDefinition(
            "deepsea.pump-lung", "深海之肺",
            new[] { R(-15, 0, 15, 25), R(-10, 25, -6, 28), R(6, 25, 10, 28) },
            new[] {
                ThemeObstacle.Rectangle("lung-pump", R(-3, 6, 3, 22)),
                ThemeObstacle.Rectangle("west-low-rack", R(-13, 11, -10, 13), false),
                ThemeObstacle.Rectangle("east-storage-tank", R(9, 12, 12, 15))
            },
            new[] {
                Horizontal(NorthWest, -8, 28, 4), Horizontal(NorthEast, 8, 28, 4),
                Vertical(West, -15, 16, 2), Vertical(East, 15, 18, 2)
            },
            new[] {
                new ThemeBaseHint("south-service", R(-5, 0, 5, 5), P(0, 2.5f)),
                new ThemeBaseHint("west-service", R(-14, 4, -10, 9), P(-12, 6.5f))
            }, BuildLungWaves(), ThemeDeepSeaData.ValveAnemone);

        internal static readonly EldritchTransportLink[] PipeLinks = {
            new EldritchTransportLink(PipeWestToEast, P(-8, 25), P(8, 9)),
            new EldritchTransportLink(PipeEastToWest, P(8, 25), P(-8, 9))
        };
        internal static readonly ThemeRect[] PumpFacilities = {
            R(-10, 25, -6, 28), R(6, 25, 10, 28),
            R(-9.5f, 7.5f, -6.5f, 10.5f), R(6.5f, 7.5f, 9.5f, 10.5f)
        };
        internal static readonly EldritchPumpBatchRule[] PumpRouting = {
            PR(1, 0, EldritchPumpRoutePolicy.Both), PR(1, 1, EldritchPumpRoutePolicy.OneSideRandom),
            PR(1, 2, EldritchPumpRoutePolicy.ThreeWayRandom),
            PR(2, 0, EldritchPumpRoutePolicy.ThreeWayRandom), PR(2, 1, EldritchPumpRoutePolicy.ThreeWayRandom),
            PR(2, 2, EldritchPumpRoutePolicy.ThreeWayRandom),
            PR(3, 0, EldritchPumpRoutePolicy.ThreeWayRandom), PR(3, 1, EldritchPumpRoutePolicy.ThreeWayRandom),
            PR(3, 2, EldritchPumpRoutePolicy.ThreeWayRandom, randomSideDoor: true),
            PR(4, 0, EldritchPumpRoutePolicy.ThreeWayRandom), PR(4, 1, EldritchPumpRoutePolicy.ThreeWayRandom),
            PR(4, 2, EldritchPumpRoutePolicy.ThreeWayRandom, randomSideDoor: true),
            PR(5, 0, EldritchPumpRoutePolicy.Both), PR(5, 1, EldritchPumpRoutePolicy.Both),
            PR(5, 2, EldritchPumpRoutePolicy.BossSideToOther, waitForBoss: true)
        };

        internal static readonly ThemeMapDefinition WhiteDock = new ThemeMapDefinition(
            "deepsea.white-dock", "白渊观测坞", new[] { R(-16, 0, 16, 26) },
            new[] {
                ThemeObstacle.Rectangle("pillar-south-west", R(-7, 7, -5, 9)),
                ThemeObstacle.Rectangle("pillar-south-east", R(5, 7, 7, 9)),
                ThemeObstacle.Rectangle("pillar-north-west", R(-7, 17, -5, 19)),
                ThemeObstacle.Rectangle("pillar-north-east", R(5, 17, 7, 19))
            },
            new[] { Horizontal(Mouth, 0, 24, 2), Vertical(West, -14, 13, 2), Vertical(East, 14, 13, 2) },
            new[] {
                new ThemeBaseHint("south-service", R(-6, 2, 6, 6), P(0, 4)),
                new ThemeBaseHint("west-service", R(-13, 10, -9, 16), P(-11, 13)),
                new ThemeBaseHint("east-service", R(9, 10, 13, 16), P(11, 13))
            }, new[] { W(1, 1, B(0, One(WhiteVisitor, Mouth))) }, WhiteVisitor);

        internal static readonly EldritchContactDefinition[] WhiteContacts = {
            new EldritchContactDefinition(EldritchWindowSide.North, P(0, 24), P(0, -1)),
            new EldritchContactDefinition(EldritchWindowSide.East, P(14, 13), P(-1, 0)),
            new EldritchContactDefinition(EldritchWindowSide.West, P(-14, 13), P(1, 0))
        };
        internal static readonly EldritchWindowSide[] WhiteContactCycle = {
            EldritchWindowSide.North, EldritchWindowSide.East, EldritchWindowSide.North, EldritchWindowSide.West
        };
        internal static readonly EldritchFaceRoute[] WhiteRoutes = {
            new EldritchFaceRoute(EldritchWindowSide.North, EldritchWindowSide.East,
                new[] { P(0, 24), P(12, 24), P(14, 22), P(14, 13) }),
            new EldritchFaceRoute(EldritchWindowSide.East, EldritchWindowSide.North,
                new[] { P(14, 13), P(14, 22), P(12, 24), P(0, 24) }),
            new EldritchFaceRoute(EldritchWindowSide.North, EldritchWindowSide.West,
                new[] { P(0, 24), P(-12, 24), P(-14, 22), P(-14, 13) }),
            new EldritchFaceRoute(EldritchWindowSide.West, EldritchWindowSide.North,
                new[] { P(-14, 13), P(-14, 22), P(-12, 24), P(0, 24) })
        };

        internal static readonly EldritchShadowParameters ShadowParameters = new EldritchShadowParameters();
        internal static readonly EldritchBanquetParameters BanquetParameters = new EldritchBanquetParameters();
        internal static readonly EldritchHangingParameters HangingParameters = new EldritchHangingParameters();
        internal static readonly EldritchBloodMotherParameters BloodMotherParameters = new EldritchBloodMotherParameters();
        internal static readonly EldritchPumpParameters PumpParameters = new EldritchPumpParameters();
        internal static readonly EldritchWhiteVisitorParameters WhiteParameters = new EldritchWhiteVisitorParameters();
        internal static readonly EldritchWhiteRevealParameters WhiteReveal = new EldritchWhiteRevealParameters();

        internal static readonly ThemeMapDefinition[] Maps = { ShadowCourtyard, Banquet, HangingHall, BloodGallery, PumpLung, WhiteDock };

        internal static ThemeEnemyStats FindEnemy(string id)
        {
            foreach (var enemy in Enemies) if (enemy.Id == id) return enemy;
            return null;
        }
        internal static ThemeMapDefinition FindMap(string id)
        {
            foreach (var map in Maps) if (map.Id == id) return map;
            return null;
        }
        internal static float HangingWindowSeconds(int eventChoice) => eventChoice == 1 ? 9 : 6;
        internal static float WhiteHandSpeed(int eventChoice) => eventChoice == 0 ? 0.4f : 0.65f;
        internal static string MirroredEntrance(string entrance, bool mirror)
        {
            if (!mirror) return entrance;
            if (entrance == NorthWest) return NorthEast;
            if (entrance == NorthEast) return NorthWest;
            if (entrance == West) return East;
            return entrance == East ? West : entrance;
        }

        private static ThemeMapDefinition CopyMap(ThemeMapDefinition source, string id, string name, ThemeWave[] waves, string boss) =>
            new ThemeMapDefinition(id, name, source.Floors, source.Obstacles, source.Entrances,
                source.BaseHints, waves, boss, source.InitialDeploymentSeconds, source.BetweenWavesSeconds,
                source.WaitForClear, source.BoundaryBlocksShots);

        private static ThemeWave[] ReplaceAll(ThemeWave[] waves, string sourceId, string replacementId)
        {
            var replacements = new List<ThemeUnitReplacement>();
            foreach (var wave in waves)
                for (int batch = 0; batch < wave.Batches.Length; batch++)
                    foreach (var group in wave.Batches[batch].Groups)
                        if (group.UnitId == sourceId)
                            replacements.Add(Shadow(wave.Number, batch, group.EntranceId, sourceId, replacementId, group.Count));
            return ReplaceUnits(waves, replacements.ToArray());
        }

        private static ThemeWave[] ReplaceUnits(ThemeWave[] waves, ThemeUnitReplacement[] replacements)
        {
            var result = new ThemeWave[waves.Length];
            for (int w = 0; w < waves.Length; w++)
            {
                var sourceWave = waves[w]; var batches = new ThemeBatch[sourceWave.Batches.Length];
                for (int b = 0; b < batches.Length; b++)
                {
                    var groups = new List<ThemeSpawnGroup>();
                    foreach (var group in sourceWave.Batches[b].Groups)
                    {
                        int replaced = 0;
                        foreach (var item in replacements)
                        {
                            if (item.WaveNumber != sourceWave.Number || item.BatchIndex != b ||
                                item.EntranceId != group.EntranceId || item.UnitId != group.UnitId) continue;
                            groups.Add(new ThemeSpawnGroup(item.ReplacementId, group.EntranceId, item.Count,
                                group.DelaySeconds + replaced * group.IntervalSeconds, group.IntervalSeconds));
                            replaced += item.Count;
                        }
                        if (replaced < group.Count)
                            groups.Add(new ThemeSpawnGroup(group.UnitId, group.EntranceId, group.Count - replaced,
                                group.DelaySeconds + replaced * group.IntervalSeconds, group.IntervalSeconds));
                    }
                    batches[b] = new ThemeBatch(sourceWave.Batches[b].StartSeconds, groups.ToArray());
                }
                result[w] = new ThemeWave(sourceWave.Number, sourceWave.ExpectedDirectCount, batches);
            }
            return result;
        }

        private static ThemeWave[] BuildLungWaves() => new[] {
            W(1, 35,
                B(0, D(NorthWest, 4, 1), D(NorthEast, 4, 1)),
                B(8, D(NorthWest, 4, 2), D(NorthEast, 4, 1, 1)),
                B(16, D(NorthWest, 4, 1, 1), D(NorthEast, 4, 2, 1))),
            W(2, 50,
                B(0, D(NorthWest, 5, 2, 1), D(NorthEast, 5, 2, 1)),
                B(8, D(NorthWest, 4, 1, 2, 1), D(NorthEast, 4, 1, 2, 1)),
                B(16, D(NorthWest, 5, 2, 1, 1), D(NorthEast, 5, 2, 1, 1))),
            W(3, 65,
                B(0, D(NorthWest, 6, 2, 1, 1), D(NorthEast, 6, 2, 1, 1)),
                B(8, D(NorthWest, 6, 2, 2, 1), D(NorthEast, 6, 2, 2, 1)),
                B(16, D(NorthWest, 3, 2, 1, 1), D(NorthEast, 3, 2, 2), D(West, 6, 2, 1))),
            W(4, 80,
                B(0, D(NorthWest, 7, 3, 2, 1), D(NorthEast, 7, 3, 2, 1)),
                B(8, D(NorthWest, 7, 3, 2, 1), D(NorthEast, 7, 3, 2, 1)),
                B(16, D(NorthWest, 3, 2, 1, 1), D(NorthEast, 3, 2, 1, 1), D(West, 8, 2, 2, 2))),
            W(5, 95,
                B(0, D(NorthWest, 8, 4, 2, 1), D(NorthEast, 8, 4, 2, 1)),
                B(8, D(NorthWest, 7, 4, 2, 2), D(NorthEast, 7, 4, 2, 2), One(ThemeDeepSeaData.ValveAnemone, NorthWest)),
                B(16, D(NorthWest, 8, 3, 4, 1), D(NorthEast, 4, 1, 2, 1), D(East, 4, 2, 2, 2)))
        };

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
        private static ThemeSpawnGroup[] One(string id, string entrance) => new[] { new ThemeSpawnGroup(id, entrance, 1) };
        private static ThemeSpawnGroup[] D(string gate, int grapes, int shells, int anemones = 0, int lilies = 0) =>
            ThemeDeepSeaData.Group(gate, grapes, shells, anemones, lilies);
        private static ThemeUnitReplacement Shadow(int wave, int batch, string gate, string unit, string replacement, int count) =>
            new ThemeUnitReplacement(wave, batch, gate, unit, replacement, count);
        private static EldritchTransportAssignment T(int wave, int batch, string gate, string unit, int count, string link) =>
            new EldritchTransportAssignment(wave, batch, gate, unit, count, link);
        private static EldritchPumpBatchRule PR(int wave, int batch, EldritchPumpRoutePolicy route,
            bool randomSideDoor = false, bool waitForBoss = false) =>
            new EldritchPumpBatchRule(wave, batch, route, randomSideDoor, waitForBoss);
    }

    internal sealed class ThemeUnitReplacement
    {
        internal readonly int WaveNumber, BatchIndex, Count;
        internal readonly string EntranceId, UnitId, ReplacementId;
        internal ThemeUnitReplacement(int wave, int batch, string entrance, string unit, string replacement, int count)
        { WaveNumber = wave; BatchIndex = batch; EntranceId = entrance; UnitId = unit; ReplacementId = replacement; Count = count; }
    }

    internal sealed class EldritchTransportLink
    {
        internal readonly string Id;
        internal readonly ThemePoint Entry, Exit;
        internal EldritchTransportLink(string id, ThemePoint entry, ThemePoint exit)
        { Id = id; Entry = entry; Exit = exit; }
    }

    internal sealed class EldritchTransportAssignment
    {
        internal readonly int WaveNumber, BatchIndex, Count;
        internal readonly string EntranceId, UnitId, LinkId;
        internal EldritchTransportAssignment(int wave, int batch, string entrance, string unit, int count, string link)
        { WaveNumber = wave; BatchIndex = batch; EntranceId = entrance; UnitId = unit; Count = count; LinkId = link; }
    }

    internal enum EldritchPumpRoutePolicy { Both, OneSideRandom, ThreeWayRandom, BossSideToOther }

    internal sealed class EldritchPumpBatchRule
    {
        internal readonly int WaveNumber, BatchIndex;
        internal readonly EldritchPumpRoutePolicy Route;
        internal readonly bool RandomSideDoor, WaitForBossEngagementOrDeath;
        internal EldritchPumpBatchRule(int wave, int batch, EldritchPumpRoutePolicy route, bool randomSideDoor, bool waitForBoss)
        { WaveNumber = wave; BatchIndex = batch; Route = route; RandomSideDoor = randomSideDoor; WaitForBossEngagementOrDeath = waitForBoss; }
    }

    internal enum EldritchWindowSide { North, East, West }

    internal sealed class EldritchContactDefinition
    {
        internal readonly EldritchWindowSide Side;
        internal readonly ThemePoint Position, GroundInward;
        internal EldritchContactDefinition(EldritchWindowSide side, ThemePoint position, ThemePoint groundInward)
        { Side = side; Position = position; GroundInward = groundInward; }
    }

    internal sealed class EldritchFaceRoute
    {
        internal readonly EldritchWindowSide From, To;
        internal readonly ThemePoint[] Points;
        internal EldritchFaceRoute(EldritchWindowSide from, EldritchWindowSide to, ThemePoint[] points)
        { From = from; To = to; Points = points; }
    }

    internal sealed class EldritchShadowParameters
    {
        internal readonly float DelaySeconds = 1.2f, DamageMultiplier = 0.5f, RecedeSeconds = 0.3f;
    }

    internal sealed class EldritchBanquetParameters
    {
        internal readonly float EnterSeconds = 1.5f, TransitSeconds = 2, ExitSeconds = 0.6f, EntrySpacingSeconds = 1;
        internal readonly float ChandelierClosureSeconds = 8;
        internal readonly float CrackCooldownSeconds = 8, CrackWarningSeconds = 2, CrackWidth = 2, CrackMaximumLength = 24;
        internal readonly float CrackFinalDamage = 400, BossWineHealing = 24000;
    }

    internal sealed class EldritchHangingParameters
    {
        internal readonly int KnotCount = 2;
        internal readonly float KnotOffsetFromPillarCenter = 2;
        internal readonly float SweepIntervalSeconds = 10, BetweenKnotSweepsSeconds = 2;
        internal readonly float SweepRange = 22, SweepAngleDegrees = 50, SweepWarningSeconds = 1.5f;
        internal readonly float SweepDurationSeconds = 3, SweepWidth = 0.8f, SweepFinalDamage = 600;
        internal readonly float FallenDamageMultiplier = 1.5f, RecoverySeconds = 8, RethreadSeconds = 2;
    }

    internal sealed class EldritchBloodMotherParameters
    {
        internal readonly float InitialHealth = 200000, HealthPerRegisteredDeath = 2000;
        internal readonly float HealthDrainPerSecond = 2000, DormantMinimumHealth = 180000, AwakeningSeconds = 2;
        internal readonly float SacThrowRange = 10, SacThrowIntervalSeconds = 10, SacWindupSeconds = 1, SacFlightSeconds = 1;
        internal readonly int NormalSacCount = 2, HighHealthSacCount = 3;
        internal readonly float HighHealthSacThreshold = 400000, MinimumSacSeparation = 4;
        internal readonly float SacLifetimeSeconds = 4, SacRadius = 2, SacFinalDamage = 500, SacBacklashDamage = 8000;
        internal readonly float SweepIntervalSeconds = 12, SweepWarningSeconds = 1.2f, SweepAngleDegrees = 120;
        internal readonly float SweepRadius = 3.5f, SweepFinalDamage = 550, SweepRecoverySeconds = 1.5f;
    }

    internal sealed class EldritchPumpParameters
    {
        internal readonly float RouteWarningSeconds = 3, TransitSeconds = 2;
        internal readonly float LastBatchEarliestSeconds = 16, BossEngagementWarningSeconds = 3, BossEngagementRange = 8;
        internal readonly int LastBatchCount = 34;
    }

    internal sealed class EldritchWhiteVisitorParameters
    {
        internal readonly float ContactRadius = 1.8f, ResidenceSeconds = 20, NextSideWarningSeconds = 4;
        internal readonly float LastNewActionSecond = 18, RouteSpeed = 4, FirstBiteDelaySeconds = 2;
        internal readonly int DrownedPerContact = 12, DrownedPerBatch = 6;
        internal readonly float DrownedBatchIntervalSeconds = 4;
        internal readonly float BiteRange = 3.5f, BiteRadius = 1.5f, BiteWarningSeconds = 1, BiteFinalDamage = 450, BiteIntervalSeconds = 3;
        internal readonly float CrushAtResidenceSecond = 12, CrushWidth = 8, CrushDepth = 3;
        internal readonly float CrushWarningSeconds = 1.5f, CrushDurationSeconds = 0.8f, CrushFinalDamage = 700;
        internal readonly float[] SpitAtResidenceSeconds = { 6, 15 };
        internal readonly float SpitRadius = 2.5f, SpitWarningSeconds = 2, SpitFinalDamage = 600;
        internal readonly float PhaseTwoHealthFraction = 0.5f, PhaseTransitionSeconds = 3;
        internal readonly float PhaseTwoMouthDamageMultiplier = 1.25f, PhaseTwoSpitIntervalSeconds = 9;
        internal readonly float HandInitialBaseDistance = 7, HandRadius = 2.2f, SecondHandStartDelaySeconds = 1.5f;
        internal readonly float HandDamagePerPush = 10000, HandPushDistance = 1, HandMaximumBaseDistance = 7, HandRecoilSeconds = 0.15f;
        internal readonly float HandSlamIntervalSeconds = 5, HandSlamWarningSeconds = 1, HandSlamRadius = 2.2f, HandSlamFinalDamage = 400;
        internal readonly float GraspBaseDistance = 3.5f, GraspWarningSeconds = 2, GraspRadius = 3, GraspFinalDamage = 900, GraspIntervalSeconds = 5;
        internal readonly float SiegeIntervalSeconds = 3, SiegeWarningSeconds = 1.5f, SiegeFinalDamage = 600;
    }

    internal sealed class EldritchWhiteRevealParameters
    {
        internal readonly float DurationSeconds = 4, FaceWidthToWindowRatio = 1.2f;
        internal readonly float InitialDarkOpacity = 0.85f, LitDarkOpacity = 0.35f, LampFadeSeconds = 0.25f;
        internal readonly float[] LampStartSeconds = { 0.8f, 1.25f, 1.7f };
        internal readonly float EyelidStartSeconds = 2.2f, EyelidDurationSeconds = 0.6f;
        internal readonly int EyelidFrameCount = 4;
        internal readonly float PupilMoveSeconds = 0.5f, PupilMaximumEyeWidthFraction = 0.05f, PupilMaximumEyeHeightFraction = 0.03f;
        internal readonly float FaceMovementStartSeconds = 2.2f, FaceMovementEndSeconds = 3.6f;
        internal readonly float FaceDeckShift = 0.35f, FinalFaceScale = 1.03f, NeckDeckShift = 0.08f;
    }
}
