using PathFinder.Interfaces;
using PathFinder.Models;
using System;

namespace PathFinder.Strategies
{
    public class BresenhamLineOfSightChecker : ILineOfSightChecker
    {
        public bool HasLineOfSight(double[,] distanceMap, Coordinate start, Coordinate end, double requiredClearance)
        {
            int x0 = start.X, y0 = start.Y;
            int x1 = end.X, y1 = end.Y;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                // Verifica se la distanza dall'ostacolo in questo punto è sufficiente
                if (distanceMap[x0, y0] < requiredClearance)
                    return false;

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }

            return true;
        }
    }
}
