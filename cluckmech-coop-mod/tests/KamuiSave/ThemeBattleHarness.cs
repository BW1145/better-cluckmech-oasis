using System;
using System.Collections.Generic;
using System.Linq;
using CluckmechCoop;

static class ThemeBattleHarness
{
    private static int checks;
    // Installed native base prefab (sharedassets0, GameObject 26538), read from its full 建筑体积.
    private const float BaseWidth = 4, BaseHeight = 3;
    private static readonly ThemeMapDefinition[] maps = new[] {
        ThemeCastleData.Courtyard, ThemeCastleData.Banquet, ThemeCastleData.CountHall,
        ThemeDeepSeaData.Gallery, ThemeDeepSeaData.PumpRoom, ThemeDeepSeaData.OctopusDock
    }.Concat(ThemeEldritchData.Maps).ToArray();
    private static readonly Dictionary<string, ThemeEnemyStats> units = ThemeCastleData.Enemies
        .Concat(ThemeDeepSeaData.Enemies).Concat(ThemeEldritchData.Enemies).ToDictionary(u => u.Id);
    private static void Check(bool value, string name)
    { checks++; if (!value) throw new Exception("ThemeBattles: " + name); }
    private static bool Near(float a, float b) => Math.Abs(a - b) < .001f;

    internal static void Run(string section = null)
    {
        if (section == null || section == "waves") Waves();
        if (section == null || section == "geometry") Geometry();
        if (section == null || section == "balance") Balance();
        if (section != null && section != "waves" && section != "geometry" && section != "balance")
            throw new ArgumentException("Unknown ThemeBattles section: " + section);
        Console.WriteLine($"PASS: {checks} production theme wave, footprint, variant and encounter checks ({section ?? "all"})");
    }

    private static void Waves()
    {
        Check(maps.Select(m => m.Id).Distinct().Count() == 12, "twelve independently addressable battle maps");
        foreach (var unit in units.Values)
            Check(unit.FinalHealth > 0 && unit.FinalDamage > 0 && unit.AttackIntervalSeconds > 0 &&
                unit.AttackRange > 0 && unit.MoveSpeed >= 0 && unit.FinalBlock >= 0 &&
                unit.FinalResistance >= 0 && unit.FinalResistance < 1 && unit.FrontalDamageMultiplier > 0 &&
                unit.FrontalDamageMultiplier <= 1, unit.Id + " has usable reference combat values");
        foreach (var map in maps)
        {
            var gates = map.Entrances.Select(e => e.Id).ToHashSet();
            Check(gates.Count == map.Entrances.Length && gates.Count > 0 && units.ContainsKey(map.BossUnitId), map.Id + " identities resolve");
            Check(map.Waves.Length == 1 || map.Waves.Length == 5, map.Id + " is a full ordinary encounter or layer boss");
            Check(map.InitialDeploymentSeconds > 0 && map.WaitForClear && map.BetweenWavesSeconds > 0,
                map.Id + " grants preparation and waits for the existing encounter to clear");
            int bosses = 0;
            foreach (var wave in map.Waves)
            {
                Check(wave.Number >= 1 && wave.Number <= map.Waves.Length && wave.DirectCount == wave.ExpectedDirectCount,
                    map.Id + " wave " + wave.Number + " preserves its declared direct total");
                Check(wave.Batches.Length == (map.Waves.Length == 1 ? 1 : 3), map.Id + " wave " + wave.Number + " batch structure");
                float previous = -1;
                foreach (var batch in wave.Batches)
                {
                    Check(batch.StartSeconds >= 0 && batch.StartSeconds > previous && batch.DirectCount > 0,
                        map.Id + " wave " + wave.Number + " ordered populated batches");
                    previous = batch.StartSeconds;
                    foreach (var group in batch.Groups)
                    {
                        Check(units.ContainsKey(group.UnitId) && gates.Contains(group.EntranceId) && group.Count > 0 &&
                            group.DelaySeconds >= 0 && group.IntervalSeconds >= 0,
                            map.Id + "/" + wave.Number + "/" + group.UnitId + " has a real unit and entrance");
                        if (group.UnitId == map.BossUnitId)
                        { bosses += group.Count; Check(wave.Number == map.Waves.Length, map.Id + " end boss belongs to the final wave"); }
                    }
                }
            }
            Check(bosses == 1, map.Id + " contains one native-reward end boss");
        }
        SameSchedule(ThemeCastleData.Courtyard, ThemeEldritchData.ShadowCourtyard);
        SameSchedule(ThemeCastleData.Banquet, ThemeEldritchData.Banquet);
        SameSchedule(ThemeCastleData.CountHall, ThemeEldritchData.HangingHall);
        SameSchedule(ThemeDeepSeaData.Gallery, ThemeEldritchData.BloodGallery);
        foreach (var replacement in ThemeEldritchData.ShadowAssignments)
        {
            var original = ThemeCastleData.Courtyard.Waves[replacement.WaveNumber - 1].Batches[replacement.BatchIndex];
            int available = original.Groups.Where(g => g.UnitId == replacement.UnitId && g.EntranceId == replacement.EntranceId).Sum(g => g.Count);
            int requested = ThemeEldritchData.ShadowAssignments.Where(r => r.WaveNumber == replacement.WaveNumber &&
                r.BatchIndex == replacement.BatchIndex && r.UnitId == replacement.UnitId && r.EntranceId == replacement.EntranceId).Sum(r => r.Count);
            Check(available >= requested && units.ContainsKey(replacement.ReplacementId), "shadow selection consumes an existing original member");
        }
        foreach (var assignment in ThemeEldritchData.TableAssignments)
        {
            var original = ThemeCastleData.Banquet.Waves[assignment.WaveNumber - 1].Batches[assignment.BatchIndex];
            Check(original.Groups.Where(g => g.UnitId == assignment.UnitId && g.EntranceId == assignment.EntranceId).Sum(g => g.Count) >= assignment.Count &&
                ThemeEldritchData.TableLinks.Any(l => l.Id == assignment.LinkId), "table transport selects existing identities and a defined outlet");
        }
        Check(ThemeEldritchData.PumpRouting.Length == ThemeEldritchData.PumpLung.Waves.Sum(w => w.Batches.Length) &&
            ThemeEldritchData.PumpRouting.Select(r => (r.WaveNumber, r.BatchIndex)).Distinct().Count() == ThemeEldritchData.PumpRouting.Length,
            "every lung batch receives exactly one routing decision");
        var last = ThemeEldritchData.PumpLung.Waves.Last().Batches.Last();
        Check(last.DirectCount == ThemeEldritchData.PumpParameters.LastBatchCount &&
            ThemeEldritchData.PumpRouting.Last().WaitForBossEngagementOrDeath &&
            ThemeEldritchData.PumpRouting.Last().Route == EldritchPumpRoutePolicy.BossSideToOther,
            "lung final reinforcement waits for boss contact and preserves the entire redirected team");
        Console.WriteLine("PASS: ThemeBattles waves and identity conservation");
    }

    private static void SameSchedule(ThemeMapDefinition original, ThemeMapDefinition variant)
    {
        Check(original.Waves.Length == variant.Waves.Length, variant.Id + " retains ordinary wave count");
        for (int w = 0; w < original.Waves.Length; w++)
        {
            Check(original.Waves[w].DirectCount == variant.Waves[w].DirectCount &&
                original.Waves[w].Batches.Length == variant.Waves[w].Batches.Length, variant.Id + " wave replacement conserves members");
            for (int b = 0; b < original.Waves[w].Batches.Length; b++)
            {
                var before = original.Waves[w].Batches[b]; var after = variant.Waves[w].Batches[b];
                Check(before.DirectCount == after.DirectCount && before.StartSeconds == after.StartSeconds,
                    variant.Id + " replacements keep batch time and population");
                foreach (var gate in original.Entrances)
                    Check(before.Groups.Where(g => g.EntranceId == gate.Id).Sum(g => g.Count) ==
                        after.Groups.Where(g => g.EntranceId == gate.Id).Sum(g => g.Count), variant.Id + " replacements retain their source side");
            }
        }
    }

    private static void Geometry()
    {
        foreach (var map in maps)
        {
            var outline = ThemeBattleGeometry.Perimeter(map.Floors);
            Check(outline.Length >= 4 && Near(PolygonArea(outline), UnionArea(map.Floors)), map.Id + " perimeter covers its connected floor union");
            Check(map.BaseHints.Length >= 2, map.Id + " offers different base locations");
            foreach (var hint in map.BaseHints)
            {
                Check(LegalBase(map, hint.Example.X, hint.Example.Y), map.Id + "/" + hint.Id + " fits the full native 4x3 base");
                Check(new[] { (-.25f, 0f), (.25f, 0f), (0f, -.25f), (0f, .25f) }
                    .Any(d => LegalBase(map, hint.Example.X + d.Item1, hint.Example.Y + d.Item2)), map.Id + "/" + hint.Id + " remains a parking area");
            }
            foreach (var obstacle in map.Obstacles)
            {
                var center = obstacle.IsCircle ? obstacle.Center : new ThemePoint(obstacle.Outline.Average(p => p.X), obstacle.Outline.Average(p => p.Y));
                Check(!LegalBase(map, center.X, center.Y), map.Id + "/" + obstacle.Id + " occupies actual base area");
            }
        }
        Check(ThemeBattleGeometry.ContainsFootprint(ThemeCastleData.Banquet.Floors, -12, 12, BaseWidth, BaseHeight),
            "base can cross the real banquet platform seam");
        Check(ThemeBattleGeometry.ContainsFootprint(ThemeEldritchData.PumpLung.Floors, -8, 25, BaseWidth, BaseHeight),
            "base-size footprint can cross a connected upper landing seam before facility exclusion");
        Check(ThemeBattleGeometry.Contains(ThemeEldritchData.PumpLung.Floors, -7, 25) &&
            !ThemeBattleGeometry.ContainsFootprint(ThemeEldritchData.PumpLung.Floors, -7, 25, BaseWidth, BaseHeight),
            "legal centre at concave upper landing does not admit a base over the void");
        var ring = new[] { new ThemeRect(0,0,6,2), new ThemeRect(0,4,6,6), new ThemeRect(0,2,2,4), new ThemeRect(4,2,6,4) };
        Check(!ThemeBattleGeometry.ContainsFootprint(ring, 3, 3, 6, 6), "four supported corners do not permit an unsupported interior");
        var separated = new[] { new ThemeRect(0,0,2,2), new ThemeRect(3,0,5,2) };
        Check(!ThemeBattleGeometry.ContainsFootprint(separated, 2.5f, 1, 4, 1), "separated floor rectangles preserve their gap");
        bool rejected = false;
        try { ThemeBattleGeometry.Perimeter(separated); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "native navigation receives a connected land contour");
        Check(!LegalBase(ThemeCastleData.Courtyard, -4.5f, 15) && !LegalBase(ThemeDeepSeaData.Gallery, -4.5f, 14) &&
            !LegalBase(ThemeDeepSeaData.PumpRoom, -4, 14), "full vehicle edges detect flowerbed, low well and high pump overlap");
        var well = ThemeDeepSeaData.Gallery.Obstacles.Single(o => o.Id == "viewing-well");
        var pump = ThemeDeepSeaData.PumpRoom.Obstacles.Single(o => o.Id == "pressure-pump");
        Check(well.BlocksMovement && well.BlocksPlacement && !well.BlocksShots &&
            pump.BlocksMovement && pump.BlocksPlacement && pump.BlocksShots, "well permits cross-fire while the pump covers the opposite lane");
        foreach (var arm in ThemeDeepSeaData.OctopusArms)
        {
            var f = arm.Footprint;
            Check(ThemeBattleGeometry.ContainsFootprint(ThemeDeepSeaData.OctopusDock.Floors, f.Center.X, f.Center.Y, f.Width, f.Height),
                arm.Id + " full target lies on the attackable deck");
            Check(!LegalBase(ThemeDeepSeaData.OctopusDock, arm.Position.X, arm.Position.Y, new[] { f }) &&
                LegalBase(ThemeDeepSeaData.OctopusDock, arm.Position.X, arm.Position.Y), arm.Id + " destruction releases deployable ground");
        }
        foreach (var facility in ThemeEldritchData.PumpFacilities)
            Check(!LegalBase(ThemeEldritchData.PumpLung, facility.Center.X, facility.Center.Y, new[] { facility }),
                "lung transport mouth reserves its own deployment area");
        Check(ThemeBattleGeometry.ContainsFootprint(ThemeEldritchData.BloodGallery.Floors, 3, 14, 1, 1) &&
            !ThemeEldritchData.BloodGallery.Obstacles.Any(o => Overlaps(o, new ThemeRect(2.5f,13.5f,3.5f,14.5f))) &&
            ThemeEldritchData.BloodMotherFacilities.Any(f => RectOverlap(f, new ThemeRect(2.5f,13.5f,3.5f,14.5f))),
            "blood mother exit is traversable floor with a separate deployment reservation");
        Console.WriteLine("PASS: ThemeBattles whole-base geometry, floor unions and facility rules");
    }

    private static void Balance()
    {
        var arms = ThemeDeepSeaData.OctopusArms;
        Check(arms.Length == 8 && arms.Count(a => a.Kind == DeepSeaArmKind.Slam) == 4 &&
            arms.Count(a => a.Kind == DeepSeaArmKind.Bind) == 4 && arms.Select(a => a.Id).Distinct().Count() == 8,
            "octopus provides eight separate attackable limb identities");
        Check(Near(arms.Sum(a => units[a.UnitId].FinalHealth) + units[ThemeDeepSeaData.Octopus].FinalHealth, 880000),
            "all limbs plus core preserve the confirmed complete damage budget");
        Check(arms.Select(a => a.FirstAttackSeconds).SequenceEqual(arms.Select((a,i) => 3 + 1.5f*i)),
            "limb opening is staggered across both attack types");
        foreach (var arm in arms)
        {
            Check(Near(ThemeDeepSeaData.OctopusArmFirstDelay(arm, 0), arm.FirstAttackSeconds * 1.5f) &&
                Near(ThemeDeepSeaData.OctopusArmPeriod(arm.Kind, 0), ThemeDeepSeaData.OctopusArmPeriod(arm.Kind, -1) * 1.5f),
                "interference slows opening and repeated arm attacks consistently");
        }
        foreach (bool whiteVariant in new[] { false, true })
        {
            float normal = ThemeDeepSeaData.RageDurationMultiplier(whiteVariant, -1);
            Check(Near(ThemeDeepSeaData.RageDurationMultiplier(whiteVariant, 1) * .6f, normal) &&
                Near(ThemeDeepSeaData.RageDurationMultiplier(whiteVariant, 0), normal),
                "rage-rate event extends native remaining and total time by the reciprocal rate");
        }
        var octopus = ThemeDeepSeaData.OctopusParameters;
        Check(octopus.BindWarningSeconds > 0 && octopus.BindDurationSeconds == 4 && octopus.BindPlayerRange < octopus.BindTowerRange &&
            octopus.SlamWarningSeconds < ThemeDeepSeaData.OctopusArmPeriod(DeepSeaArmKind.Slam, -1),
            "limbs provide a player escape warning and four-second tower rescue window");
        Check(octopus.BombClosestTowerCount == 2 && octopus.BombWarningSeconds < octopus.BombIntervalSeconds &&
            octopus.BreathTickSeconds.All(t => t >= 0 && t < octopus.BreathDurationSeconds) &&
            octopus.BreathTickSeconds.SequenceEqual(octopus.BreathTickSeconds.OrderBy(t => t)),
            "core attacks schedule distinct rear towers and contained ordered breath ticks");
        foreach (bool first in new[] { false, true })
        {
            var normal = ThemeCastleData.CountSupply(-1, first); var sealedCellar = ThemeCastleData.CountSupply(0, first);
            Check(normal.Sum(g => g.Count) == sealedCellar.Sum(g => g.Count) &&
                normal.Where(g => g.UnitId == ThemeCastleData.BossWineBat).Sum(g => g.Count) == 4 &&
                sealedCellar.Where(g => g.UnitId == ThemeCastleData.BossWineBat).Sum(g => g.Count) == 2,
                "sealed cellar trades half the healing deliveries for additional combatants");
        }
        Check(Near(4 * units[ThemeCastleData.Count].FinalHealth * ThemeCastleData.CountParameters.WineHealFraction /
            ThemeCastleData.CountParameters.WineBatchIntervalSeconds, 16000), "ignored full wine batches create the agreed sustained recovery pressure");
        Check(ThemeCastleData.CountPillarStunSeconds(1) == ThemeCastleData.CountPillarStunSeconds(-1) * 2 &&
            ThemeEldritchData.HangingWindowSeconds(1) > ThemeEldritchData.HangingWindowSeconds(-1),
            "pillar preparation improves both regular and hanging boss damage windows");
        Check(ThemeDeepSeaData.LilyProtectedUnits.Distinct().Count() == ThemeDeepSeaData.LilyProtectedUnits.Length &&
            !ThemeDeepSeaData.LilyProtectedUnits.Contains(ThemeDeepSeaData.Lily) &&
            !ThemeDeepSeaData.LilyProtectedUnits.Contains(ThemeDeepSeaData.CrownLily) &&
            ThemeDeepSeaData.LilyProtectedUnits.Contains(ThemeDeepSeaData.ValveAnemone),
            "protective sources support the sea front line and anemone boss without protecting one another");
        var white = ThemeEldritchData.WhiteParameters;
        float perHand = white.HandDamagePerPush * ThemeEldritchData.WhiteHandSpeed(-1) / white.HandPushDistance;
        float slowed = white.HandDamagePerPush * ThemeEldritchData.WhiteHandSpeed(0) / white.HandPushDistance;
        Check(Near(perHand * 2, 13000) && Near(slowed * 2, 8000), "two approaching hands require the designed combined recoil damage budget");
        Check(white.HandMaximumBaseDistance >= white.HandInitialBaseDistance && white.HandInitialBaseDistance > white.GraspBaseDistance &&
            (white.HandInitialBaseDistance - white.GraspBaseDistance) / ThemeEldritchData.WhiteHandSpeed(-1) > white.GraspWarningSeconds,
            "hand approach creates a real response interval before base grasp");
        Check(white.SpitAtResidenceSeconds.All(t => t + white.SpitWarningSeconds <= white.LastNewActionSecond) &&
            white.CrushAtResidenceSecond + white.CrushWarningSeconds + white.CrushDurationSeconds < white.ResidenceSeconds &&
            white.NextSideWarningSeconds < white.ResidenceSeconds && white.LastNewActionSecond < white.ResidenceSeconds,
            "visible first-stage attacks finish inside their side residence and preserve the departure warning");
        Check(ThemeEldritchData.WhiteContactCycle.Zip(ThemeEldritchData.WhiteContactCycle.Skip(1)
            .Concat(ThemeEldritchData.WhiteContactCycle.Take(1))).All(pair =>
                ThemeEldritchData.WhiteRoutes.Any(r => r.From == pair.First && r.To == pair.Second)),
            "every face relocation has a defined continuous contact route");
        var reveal = ThemeEldritchData.WhiteReveal;
        Check(reveal.LampStartSeconds.Last() + reveal.LampFadeSeconds < reveal.EyelidStartSeconds &&
            reveal.EyelidStartSeconds + reveal.EyelidDurationSeconds <= reveal.DurationSeconds &&
            reveal.FaceMovementEndSeconds <= reveal.DurationSeconds,
            "reveal lighting, opening eye and approach fit inside the existing preparation presentation");
        Console.WriteLine("PASS: ThemeBattles staged boss, event and presentation timing contracts");
    }

    private static bool LegalBase(ThemeMapDefinition map, float x, float y, ThemeRect[] facilities = null)
    {
        var rect = new ThemeRect(x-BaseWidth/2, y-BaseHeight/2, x+BaseWidth/2, y+BaseHeight/2);
        return ThemeBattleGeometry.ContainsFootprint(map.Floors, x, y, BaseWidth, BaseHeight) &&
            !map.Obstacles.Any(o => o.BlocksPlacement && Overlaps(o, rect)) &&
            (facilities == null || !facilities.Any(f => RectOverlap(f, rect)));
    }
    private static bool RectOverlap(ThemeRect a, ThemeRect b) => a.MinX < b.MaxX && a.MaxX > b.MinX && a.MinY < b.MaxY && a.MaxY > b.MinY;
    private static bool Overlaps(ThemeObstacle obstacle, ThemeRect rect)
    {
        if (obstacle.IsCircle)
        {
            float x = Math.Clamp(obstacle.Center.X, rect.MinX, rect.MaxX), y = Math.Clamp(obstacle.Center.Y, rect.MinY, rect.MaxY);
            return (x-obstacle.Center.X)*(x-obstacle.Center.X)+(y-obstacle.Center.Y)*(y-obstacle.Center.Y) < obstacle.Radius*obstacle.Radius;
        }
        var box = new[] { new ThemePoint(rect.MinX,rect.MinY), new ThemePoint(rect.MaxX,rect.MinY),
            new ThemePoint(rect.MaxX,rect.MaxY), new ThemePoint(rect.MinX,rect.MaxY) };
        foreach (var polygon in new[] { box, obstacle.Outline })
            for (int i = 0; i < polygon.Length; i++)
            {
                var a = polygon[i]; var b = polygon[(i+1)%polygon.Length]; float ax = a.Y-b.Y, ay = b.X-a.X;
                float lowA = box.Min(p => p.X*ax+p.Y*ay), highA = box.Max(p => p.X*ax+p.Y*ay);
                float lowB = obstacle.Outline.Min(p => p.X*ax+p.Y*ay), highB = obstacle.Outline.Max(p => p.X*ax+p.Y*ay);
                if (highA <= lowB || highB <= lowA) return false;
            }
        return true;
    }
    private static float PolygonArea(ThemePoint[] points)
    {
        float twice = 0;
        for (int i = 0; i < points.Length; i++) twice += points[i].X*points[(i+1)%points.Length].Y-points[(i+1)%points.Length].X*points[i].Y;
        return Math.Abs(twice)*.5f;
    }
    private static float UnionArea(ThemeRect[] floors)
    {
        float area = 0;
        for (int mask = 1; mask < 1 << floors.Length; mask++)
        {
            float left = float.NegativeInfinity, bottom = left, right = float.PositiveInfinity, top = right; int count = 0;
            for (int i = 0; i < floors.Length; i++) if ((mask & (1 << i)) != 0)
            { count++; left=Math.Max(left,floors[i].MinX); bottom=Math.Max(bottom,floors[i].MinY); right=Math.Min(right,floors[i].MaxX); top=Math.Min(top,floors[i].MaxY); }
            area += (count%2 == 1 ? 1 : -1)*Math.Max(0,right-left)*Math.Max(0,top-bottom);
        }
        return area;
    }
}
