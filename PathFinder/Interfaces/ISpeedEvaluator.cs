using System.Collections.Generic;
using PathFinder.Models;

namespace PathFinder.Interfaces
{
    public interface ISpeedEvaluator
    {
        double GetMaxSpeedForDistance(double distanceMeters);
        bool IsDistanceSafe(double distanceMeters);
        List<SpeedProfile> GetProfiles();
    }
}
