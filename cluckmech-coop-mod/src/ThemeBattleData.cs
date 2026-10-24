using System;

namespace CluckmechCoop
{
    internal readonly struct ThemePoint
    {
        internal readonly float X, Y;
        internal ThemePoint(float x, float y) { X = x; Y = y; }
    }

    internal readonly struct ThemeRect
    {
        internal readonly float MinX, MinY, MaxX, MaxY;
        internal float Width => MaxX - MinX;
        internal float Height => MaxY - MinY;
        internal ThemePoint Center => new ThemePoint((MinX + MaxX) * 0.5f, (MinY + MaxY) * 0.5f);
        internal ThemeRect(float minX, float minY, float maxX, float maxY)
        { MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY; }
    }

    internal sealed class ThemeObstacle
    {
        internal readonly string Id;
        internal readonly ThemePoint[] Outline;
        internal readonly ThemePoint Center;
        internal readonly float Radius;
        internal readonly bool BlocksMovement, BlocksPlacement, BlocksShots;
        internal bool IsCircle => Radius > 0;

        internal ThemeObstacle(string id, ThemePoint[] outline, bool blocksShots)
        {
            Id = id; Outline = outline; Center = default; Radius = 0;
            BlocksMovement = BlocksPlacement = true; BlocksShots = blocksShots;
        }

        internal ThemeObstacle(string id, ThemePoint center, float radius)
        {
            Id = id; Outline = Array.Empty<ThemePoint>(); Center = center; Radius = radius;
            BlocksMovement = BlocksPlacement = BlocksShots = true;
        }

        internal static ThemeObstacle Rectangle(string id, ThemeRect rect, bool blocksShots = true) =>
            new ThemeObstacle(id, new[] {
                new ThemePoint(rect.MinX, rect.MinY), new ThemePoint(rect.MaxX, rect.MinY),
                new ThemePoint(rect.MaxX, rect.MaxY), new ThemePoint(rect.MinX, rect.MaxY)
            }, blocksShots);
    }

    internal sealed class ThemeEntrance
    {
        internal readonly string Id;
        internal readonly ThemePoint Center, LineStart, LineEnd;
        internal ThemeEntrance(string id, ThemePoint center, ThemePoint lineStart, ThemePoint lineEnd)
        { Id = id; Center = center; LineStart = lineStart; LineEnd = lineEnd; }
    }

    internal sealed class ThemeBaseHint
    {
        internal readonly string Id;
        internal readonly ThemeRect Region;
        internal readonly ThemePoint Example;
        internal ThemeBaseHint(string id, ThemeRect region, ThemePoint example)
        { Id = id; Region = region; Example = example; }
    }

    // Delays are relative to the batch start. Each group keeps its designated entrance.
    internal sealed class ThemeSpawnGroup
    {
        internal readonly string UnitId, EntranceId;
        internal readonly int Count;
        internal readonly float DelaySeconds, IntervalSeconds;
        internal ThemeSpawnGroup(string unitId, string entranceId, int count,
            float delaySeconds = 0, float intervalSeconds = 0.15f)
        {
            UnitId = unitId; EntranceId = entranceId; Count = count;
            DelaySeconds = delaySeconds; IntervalSeconds = intervalSeconds;
        }
    }

    internal sealed class ThemeBatch
    {
        internal readonly float StartSeconds;
        internal readonly ThemeSpawnGroup[] Groups;
        internal int DirectCount
        {
            get { int total = 0; foreach (var group in Groups) total += group.Count; return total; }
        }
        internal ThemeBatch(float startSeconds, ThemeSpawnGroup[] groups)
        { StartSeconds = startSeconds; Groups = groups; }
    }

    internal sealed class ThemeWave
    {
        internal readonly int Number, ExpectedDirectCount;
        internal readonly ThemeBatch[] Batches;
        internal int DirectCount
        {
            get { int total = 0; foreach (var batch in Batches) total += batch.DirectCount; return total; }
        }
        internal ThemeWave(int number, int expectedDirectCount, params ThemeBatch[] batches)
        { Number = number; ExpectedDirectCount = expectedDirectCount; Batches = batches; }
    }

    internal sealed class ThemeMapDefinition
    {
        internal readonly string Id, Name, BossUnitId;
        internal readonly ThemeRect[] Floors;
        internal readonly ThemeObstacle[] Obstacles;
        internal readonly ThemeEntrance[] Entrances;
        internal readonly ThemeBaseHint[] BaseHints;
        internal readonly ThemeWave[] Waves;
        internal readonly float InitialDeploymentSeconds, BetweenWavesSeconds;
        internal readonly bool WaitForClear, BoundaryBlocksShots;

        internal ThemeMapDefinition(string id, string name, ThemeRect[] floors,
            ThemeObstacle[] obstacles, ThemeEntrance[] entrances, ThemeBaseHint[] baseHints,
            ThemeWave[] waves, string bossUnitId, float initialDeploymentSeconds = 10,
            float betweenWavesSeconds = 8, bool waitForClear = true, bool boundaryBlocksShots = true)
        {
            Id = id; Name = name; Floors = floors; Obstacles = obstacles;
            Entrances = entrances; BaseHints = baseHints; Waves = waves; BossUnitId = bossUnitId;
            InitialDeploymentSeconds = initialDeploymentSeconds; BetweenWavesSeconds = betweenWavesSeconds;
            WaitForClear = waitForClear; BoundaryBlocksShots = boundaryBlocksShots;
        }
    }

    // These values describe the completed battlefield at the named reference difficulty.
    // Native difficulty conversion belongs to the production prefab/spawn adapter.
    internal sealed class ThemeEnemyStats
    {
        internal readonly string Id, Name;
        internal readonly float FinalHealth, FinalDamage, AttackIntervalSeconds, AttackRange;
        internal readonly float MoveSpeed, FinalBlock, FinalResistance, FrontalDamageMultiplier, AttackWindupSeconds;
        internal ThemeEnemyStats(string id, string name, float finalHealth, float finalDamage,
            float attackIntervalSeconds, float attackRange, float moveSpeed, float finalBlock = 5,
            float finalResistance = 0, float frontalDamageMultiplier = 1, float attackWindupSeconds = 0)
        {
            Id = id; Name = name; FinalHealth = finalHealth; FinalDamage = finalDamage;
            AttackIntervalSeconds = attackIntervalSeconds; AttackRange = attackRange; MoveSpeed = moveSpeed;
            FinalBlock = finalBlock; FinalResistance = finalResistance;
            FrontalDamageMultiplier = frontalDamageMultiplier; AttackWindupSeconds = attackWindupSeconds;
        }
    }
}
