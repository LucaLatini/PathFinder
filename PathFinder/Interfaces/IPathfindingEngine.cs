using System.Collections.Generic;
using PathFinder.Models;

namespace PathFinder.Interfaces
{
    public interface IPathfindingEngine
    {
        List<Coordinate> FindPath(double[,] distanceMap, Coordinate start, Coordinate end, ISpeedEvaluator speedEvaluator, double gridCellSizeMeters);
    }
}
