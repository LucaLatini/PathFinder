using PathFinder.Models;

namespace PathFinder.Interfaces
{
    public interface ILineOfSightChecker
    {
        bool HasLineOfSight(double[,] distanceMap, Coordinate start, Coordinate end, double requiredClearance);
    }
}
