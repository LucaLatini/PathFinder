using System.Collections.Generic;
using PathFinder.Models;

namespace PathFinder.Interfaces
{
    public interface IHeadingCalculator
    {
        List<Pose> CalculateHeadings(List<Coordinate> waypoints, double? finalHeadingRad = null);
    }
}
