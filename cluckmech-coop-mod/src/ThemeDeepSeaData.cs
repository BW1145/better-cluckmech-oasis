using System.Collections.Generic;

namespace CluckmechCoop
{
    // SPEC 3.3–3.6 and 6.1–6.2. Final values use the same reference as ThemeCastleData.
    internal static class ThemeDeepSeaData
    {
        internal const string Grape = "deepsea.grape", ShellGuard = "deepsea.shell-guard";
        internal const string Anemone = "deepsea.anemone", Lily = "deepsea.lily";
        internal const string CrownLily = "deepsea.crown-lily", ValveAnemone = "deepsea.valve-anemone";
        internal const string Octopus = "deepsea.octopus", SlamArm = "deepsea.slam-arm", BindArm = "deepsea.bind-arm";
        internal const string NorthWest = "north-west", NorthEast = "north-east", West = "west", East = "east", Mouth = "mouth";

        internal static readonly ThemeEnemyStats[] Enemies = {
            new ThemeEnemyStats(Grape, "海葡萄群", 2000, 120, 1, 1.2f, 1.5f),
            new ThemeEnemyStats(ShellGuard, "壳藻卫士", 4500, 320, 1.8f, 1.5f, 0.9f),
            new ThemeEnemyStats(Anemone, "海葵喷射者", 4000, 160, 4, 7, 1.2f, attackWindupSeconds: 0.8f),
            new ThemeEnemyStats(Lily, "囊泡睡莲", 3000, 80, 2, 4.5f, 0.9f),
            new ThemeEnemyStats(CrownLily, "冠囊睡莲", 120000, 400, 2, 4.5f, 0.8f,
                finalBlock: 0, attackWindupSeconds: 0.8f),
            new ThemeEnemyStats(ValveAnemone, "阀冠海葵", 120000, 400, 2, 1.8f, 0.9f, finalBlock: 0),
            new ThemeEnemyStats(Octopus, "沉渊巨章", 400000, 500, 3, 3, 0, finalBlock: 0, attackWindupSeconds: 1),
            new ThemeEnemyStats(SlamArm, "压阵腕", 60000, 600, 8, 12, 0, finalBlock: 0, attackWindupSeconds: 1.5f),
            new ThemeEnemyStats(BindArm, "束缚腕", 60000, 600, 12, 14, 0, finalBlock: 0, attackWindupSeconds: 2)
        };

        internal static readonly ThemeMapDefinition Gallery = new ThemeMapDefinition(
            "deepsea.gallery", "裂谷观景廊", new[] { R(-14, 0, 14, 28) },
            new[] { ThemeObstacle.Rectangle("viewing-well", R(-4, 7, 4, 21), false) },
            new[] {
                Horizontal(NorthWest, -7, 27, 3), Horizontal(NorthEast, 7, 27, 3),
                Vertical(East, 13.5f, 15, 2)
            },
            new[] {
                new ThemeBaseHint("south-service", R(-5, 0, 5, 5), P(0, 2.5f)),
                new ThemeBaseHint("west-service", R(-14, 9, -9, 17), P(-11.5f, 13))
            },
            new[] {
                W(1, 32,
                    B(0, D(NorthWest, 9, 2)), B(8, D(NorthEast, 9, 2)),
                    B(16, D(NorthWest, 4, 1), D(NorthEast, 4, 1))),
                W(2, 45,
                    B(0, D(NorthWest, 10, 3, 1, 1)), B(8, D(NorthEast, 10, 3, 1, 1)),
                    B(16, D(NorthWest, 5, 1, 1), D(NorthEast, 5, 1, 1, 1))),
                W(3, 60,
                    B(0, D(NorthWest, 12, 4, 3, 2)), B(8, D(NorthEast, 12, 4, 3, 2)),
                    B(16, D(East, 10, 4, 2, 2))),
                W(4, 75,
                    B(0, D(NorthWest, 14, 5, 4, 2)), B(8, D(NorthEast, 14, 5, 4, 2)),
                    B(16, D(East, 14, 4, 4, 3))),
                W(5, 90,
                    B(0, D(NorthWest, 10, 4, 2, 1), D(NorthEast, 10, 4, 2, 1)),
                    B(8, D(NorthWest, 13, 5, 6, 4), One(CrownLily, NorthWest)),
                    B(16, D(East, 13, 5, 6, 3)))
            }, CrownLily);

        internal static readonly ThemeMapDefinition PumpRoom = new ThemeMapDefinition(
            "deepsea.pump-room", "环流泵房", new[] { R(-15, 0, 15, 28) },
            new[] {
                ThemeObstacle.Rectangle("pressure-pump", R(-3, 10, 3, 19)),
                ThemeObstacle.Rectangle("west-cable-rack", R(-14, 18, -12, 20), false),
                ThemeObstacle.Rectangle("east-service-box", R(12, 7, 14, 9), false)
            },
            new[] {
                Horizontal(NorthWest, -8, 27, 3), Horizontal(NorthEast, 8, 27, 3),
                Vertical(West, -14.5f, 19, 2), Vertical(East, 14.5f, 19, 2)
            },
            new[] {
                new ThemeBaseHint("south-service", R(-6, 0, 6, 5), P(0, 2.5f)),
                new ThemeBaseHint("west-service", R(-14, 6, -10, 12), P(-12, 9))
            },
            new[] {
                W(1, 35,
                    B(0, D(NorthWest, 8, 2)), B(8, D(NorthEast, 8, 3, 1)), B(16, D(NorthWest, 8, 3, 2))),
                W(2, 50,
                    B(0, D(NorthWest, 10, 4, 2, 1)), B(8, D(NorthEast, 10, 4, 3, 1)),
                    B(16, D(NorthWest, 4, 1, 1, 1), D(NorthEast, 4, 1, 2, 1))),
                W(3, 65,
                    B(0, D(NorthWest, 12, 5, 3, 2)), B(8, D(NorthEast, 12, 5, 3, 2)),
                    B(16, D(NorthWest, 6, 2, 2), D(NorthEast, 6, 2, 2, 1))),
                W(4, 80,
                    B(0, D(NorthWest, 14, 6, 4, 2)), B(8, D(NorthEast, 14, 6, 4, 2)),
                    B(16, D(West, 7, 3, 2, 2), D(East, 7, 3, 2, 2))),
                W(5, 95,
                    B(0, D(NorthWest, 8, 4, 2, 1), D(NorthEast, 8, 4, 2, 1)),
                    B(8, D(NorthWest, 14, 7, 6, 4), One(ValveAnemone, NorthWest)),
                    B(16, D(East, 16, 7, 6, 4)))
            }, ValveAnemone);

        internal static readonly ThemeMapDefinition OctopusDock = new ThemeMapDefinition(
            "deepsea.octopus-dock", "深渊观测坞", new[] { R(-18, 0, 18, 24) },
            new[] {
                ThemeObstacle.Rectangle("west-cable-rack", R(-17.5f, 17, -15.5f, 19), false),
                ThemeObstacle.Rectangle("east-service-box", R(15.5f, 4, 17.5f, 6), false)
            },
            new[] { Horizontal(Mouth, 0, 23, 3) },
            new[] {
                new ThemeBaseHint("south-service", R(-8, 0, 8, 5), P(0, 2.5f)),
                new ThemeBaseHint("west-service", R(-17, 7, -12, 14), P(-14.5f, 10.5f))
            },
            new[] { W(1, 1, B(0, One(Octopus, Mouth))) }, Octopus);

        internal static readonly DeepSeaArmDefinition[] OctopusArms = {
            new DeepSeaArmDefinition("south-west-slam", SlamArm, P(-7, 7), DeepSeaArmKind.Slam, 3),
            new DeepSeaArmDefinition("south-west-bind", BindArm, P(-10, 12), DeepSeaArmKind.Bind, 4.5f),
            new DeepSeaArmDefinition("south-east-slam", SlamArm, P(7, 7), DeepSeaArmKind.Slam, 6),
            new DeepSeaArmDefinition("south-east-bind", BindArm, P(10, 12), DeepSeaArmKind.Bind, 7.5f),
            new DeepSeaArmDefinition("north-west-slam", SlamArm, P(-7, 17), DeepSeaArmKind.Slam, 9),
            new DeepSeaArmDefinition("north-west-bind", BindArm, P(-10, 22), DeepSeaArmKind.Bind, 10.5f),
            new DeepSeaArmDefinition("north-east-slam", SlamArm, P(7, 17), DeepSeaArmKind.Slam, 12),
            new DeepSeaArmDefinition("north-east-bind", BindArm, P(10, 22), DeepSeaArmKind.Bind, 13.5f)
        };

        internal static readonly DeepSeaValveDefinition[] Valves = {
            new DeepSeaValveDefinition("west-valve", P(-4.5f, 11), P(-3, 14), P(-12, 14)),
            new DeepSeaValveDefinition("east-valve", P(4.5f, 11), P(3, 14), P(12, 14))
        };
        internal static readonly DeepSeaOctopusParameters OctopusParameters = new DeepSeaOctopusParameters();
        internal static readonly DeepSeaValveAnemoneParameters ValveAnemoneParameters = new DeepSeaValveAnemoneParameters();

        internal const float LilyAuraRadius = 3.5f, CrownLilyAuraRadius = 5, LilyResistance = 0.4f;
        internal static readonly string[] LilyProtectedUnits = { Grape, ShellGuard, Anemone, ValveAnemone };
        internal const int AnemoneProjectiles = 3, MaximumHitsPerVolley = 1;
        internal const float ValveSharedRechargeSeconds = 20, ValveWarningSeconds = 1.5f, ValveFlowSeconds = 0.8f;
        internal const float ValveMaximumPushDistance = 4, ValveFlowWidth = 3, ValveInteractionRadius = 1.5f;

        internal static float OctopusArmPeriod(DeepSeaArmKind kind, int eventChoice) =>
            (kind == DeepSeaArmKind.Slam ? 8 : 12) * (eventChoice == 0 ? 1.5f : 1);
        internal static float OctopusArmFirstDelay(DeepSeaArmDefinition arm, int eventChoice) =>
            arm.FirstAttackSeconds * (eventChoice == 0 ? 1.5f : 1);
        internal static float RageDurationMultiplier(bool whiteVisitor, int eventChoice) =>
            (whiteVisitor ? 1.5f : 2) / (eventChoice == 1 ? 0.6f : 1);

        internal static ThemeEnemyStats FindEnemy(string id)
        {
            foreach (var enemy in Enemies) if (enemy.Id == id) return enemy;
            return null;
        }
        internal static ThemeMapDefinition FindMap(string id)
        {
            if (id == Gallery.Id) return Gallery;
            if (id == PumpRoom.Id) return PumpRoom;
            return id == OctopusDock.Id ? OctopusDock : null;
        }

        internal static ThemeSpawnGroup[] Group(string gate, int grapes, int shells, int anemones = 0, int lilies = 0) =>
            D(gate, grapes, shells, anemones, lilies);
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
        private static ThemeSpawnGroup[] D(string gate, int grapes, int shells, int anemones = 0, int lilies = 0)
        {
            var groups = new List<ThemeSpawnGroup>(4);
            if (shells > 0) groups.Add(new ThemeSpawnGroup(ShellGuard, gate, shells));
            if (grapes > 0) groups.Add(new ThemeSpawnGroup(Grape, gate, grapes, 1));
            if (anemones > 0) groups.Add(new ThemeSpawnGroup(Anemone, gate, anemones, 2));
            if (lilies > 0) groups.Add(new ThemeSpawnGroup(Lily, gate, lilies, 3));
            return groups.ToArray();
        }
    }

    internal enum DeepSeaArmKind { Slam, Bind }

    internal sealed class DeepSeaArmDefinition
    {
        internal readonly string Id, UnitId;
        internal readonly ThemePoint Position;
        internal readonly ThemeRect Footprint;
        internal readonly DeepSeaArmKind Kind;
        internal readonly float FirstAttackSeconds;
        internal DeepSeaArmDefinition(string id, string unitId, ThemePoint position, DeepSeaArmKind kind, float firstAttackSeconds)
        {
            Id = id; UnitId = unitId; Position = position; Kind = kind; FirstAttackSeconds = firstAttackSeconds;
            Footprint = new ThemeRect(position.X - 1.5f, position.Y - 2, position.X + 1.5f, position.Y + 2);
        }
    }

    internal sealed class DeepSeaValveDefinition
    {
        internal readonly string Id;
        internal readonly ThemePoint Control, FlowStart, FlowEnd;
        internal DeepSeaValveDefinition(string id, ThemePoint control, ThemePoint flowStart, ThemePoint flowEnd)
        { Id = id; Control = control; FlowStart = flowStart; FlowEnd = flowEnd; }
    }

    internal sealed class DeepSeaValveAnemoneParameters
    {
        internal readonly float Range = 8, CooldownSeconds = 8, WindupSeconds = 1.5f;
        internal readonly int ProjectileCount = 4;
        internal readonly float ProjectileIntervalSeconds = 0.4f, FinalProjectileDamage = 200;
    }

    internal sealed class DeepSeaOctopusParameters
    {
        internal readonly float CoreTransitionSeconds = 2;
        internal readonly float SlamRange = 12, SlamWarningSeconds = 1.5f, SlamWidth = 2, SlamLength = 6, SlamFinalDamage = 600;
        internal readonly float BindPlayerRange = 4, BindTowerRange = 14, BindWarningSeconds = 2, BindDurationSeconds = 4;
        internal readonly float BiteRange = 3, BiteIntervalSeconds = 3, BiteWarningSeconds = 1, BiteRadius = 2, BiteFinalDamage = 500;
        internal readonly float BreathRange = 14, BreathInitialDelaySeconds = 8, BreathIntervalSeconds = 10;
        internal readonly float BreathWindupSeconds = 2, BreathAngleDegrees = 50, BreathDurationSeconds = 1.5f;
        internal readonly float BreathFinalDamagePerTick = 180;
        internal readonly float[] BreathTickSeconds = { 0, 0.5f, 1 };
        internal readonly float BombInitialDelaySeconds = 3, BombIntervalSeconds = 10, BombWarningSeconds = 2;
        internal readonly float BombRadius = 2, BombFinalDamage = 600;
        internal readonly int BombClosestTowerCount = 2;
        internal readonly float SiegeIntervalSeconds = 3, SiegeWarningSeconds = 1.5f, SiegeFinalDamage = 600;
    }
}
