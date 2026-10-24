using System;
using System.Collections.Generic;
using System.Linq;

namespace CluckmechCoop
{
    internal static class ThemeBattleGeometry
    {
        internal static bool Contains(ThemeRect[] floors, float x, float y)
        {
            foreach (var floor in floors)
                if (x >= floor.MinX && x <= floor.MaxX && y >= floor.MinY && y <= floor.MaxY) return true;
            return false;
        }

        internal static bool ContainsFootprint(ThemeRect[] floors, float x, float y, float width, float height)
        {
            float minX = x - width * .5f, maxX = x + width * .5f;
            float minY = y - height * .5f, maxY = y + height * .5f;
            if (!Contains(floors, minX, minY) || !Contains(floors, maxX, minY) ||
                !Contains(floors, minX, maxY) || !Contains(floors, maxX, maxY)) return false;
            var xs = new List<float> { minX, maxX }; var ys = new List<float> { minY, maxY };
            foreach (var floor in floors)
            {
                if (floor.MinX > minX && floor.MinX < maxX) xs.Add(floor.MinX);
                if (floor.MaxX > minX && floor.MaxX < maxX) xs.Add(floor.MaxX);
                if (floor.MinY > minY && floor.MinY < maxY) ys.Add(floor.MinY);
                if (floor.MaxY > minY && floor.MaxY < maxY) ys.Add(floor.MaxY);
            }
            xs.Sort(); ys.Sort();
            for (int i = 0; i + 1 < xs.Count; i++)
                for (int j = 0; j + 1 < ys.Count; j++)
                    if (!Contains(floors, (xs[i] + xs[i + 1]) * .5f, (ys[j] + ys[j + 1]) * .5f)) return false;
            return true;
        }

        // PolyNav treats its first contour as land and subsequent contours as holes.
        // Merge adjoining floor rectangles into their actual perimeter before binding it.
        internal static ThemePoint[] Perimeter(ThemeRect[] floors)
        {
            var xs = floors.SelectMany(f => new[] { f.MinX, f.MaxX }).Distinct().OrderBy(x => x).ToArray();
            var ys = floors.SelectMany(f => new[] { f.MinY, f.MaxY }).Distinct().OrderBy(y => y).ToArray();
            var land = new bool[xs.Length - 1, ys.Length - 1];
            for (int x = 0; x + 1 < xs.Length; x++)
                for (int y = 0; y + 1 < ys.Length; y++)
                    land[x, y] = Contains(floors, (xs[x] + xs[x + 1]) * .5f, (ys[y] + ys[y + 1]) * .5f);
            var edges = new Dictionary<(int, int), (int, int)>();
            for (int x = 0; x + 1 < xs.Length; x++)
                for (int y = 0; y + 1 < ys.Length; y++)
                {
                    if (!land[x, y]) continue;
                    if (y == 0 || !land[x, y - 1]) edges.Add((x, y), (x + 1, y));
                    if (x + 2 == xs.Length || !land[x + 1, y]) edges.Add((x + 1, y), (x + 1, y + 1));
                    if (y + 2 == ys.Length || !land[x, y + 1]) edges.Add((x + 1, y + 1), (x, y + 1));
                    if (x == 0 || !land[x - 1, y]) edges.Add((x, y + 1), (x, y));
                }
            if (edges.Count == 0) throw new InvalidOperationException("Theme battlefield needs a floor contour");
            var first = edges.Keys.OrderBy(k => k.Item1).ThenBy(k => k.Item2).First();
            var cursor = first; var points = new List<ThemePoint>();
            do
            {
                points.Add(new ThemePoint(xs[cursor.Item1], ys[cursor.Item2]));
                var next = edges[cursor]; edges.Remove(cursor); cursor = next;
            } while (cursor != first);
            if (edges.Count != 0) throw new InvalidOperationException("Theme battlefield floor contour must be connected");
            for (int i = points.Count - 1; i >= 0 && points.Count > 3; i--)
            {
                var a = points[(i + points.Count - 1) % points.Count]; var b = points[i];
                var c = points[(i + 1) % points.Count];
                if ((a.X == b.X && b.X == c.X) || (a.Y == b.Y && b.Y == c.Y)) points.RemoveAt(i);
            }
            return points.ToArray();
        }
    }
}
