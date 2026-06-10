using System.Collections.Generic;
using System.Linq;
using PathFinder.Interfaces;
using PathFinder.Models;

namespace PathFinder.Strategies
{
    public class RaycastingPathOptimizer : IPathOptimizer
    {
        private readonly ILineOfSightChecker _lineOfSightChecker;

        public RaycastingPathOptimizer(ILineOfSightChecker lineOfSightChecker)
        {
            _lineOfSightChecker = lineOfSightChecker;
        }

        public List<Coordinate> Optimize(List<Coordinate> path, double[,] distanceMap, ISpeedEvaluator speedEvaluator)
        {
            if (path == null)
                return new List<Coordinate>();

            if (path.Count < 3)
                return path;

            var waypoints = new List<Coordinate>();
            waypoints.Add(path[0]);
            int lastWaypointIndex = 0;

            for (int i = 1; i < path.Count; i++)
            {
                // Determiniamo la velocità nel punto di partenza del potenziale segmento
                double currentDist = distanceMap[path[lastWaypointIndex].X, path[lastWaypointIndex].Y];
                double currentSpeed = speedEvaluator.GetMaxSpeedForDistance(currentDist);
                
                var profile = speedEvaluator.GetProfiles().FirstOrDefault(p => p.Speed <= currentSpeed);
                double requiredClearance = profile?.RequiredClearanceMeters ?? 0.5;

                // ⭐ ISTERESI E VELOCITÀ: Controlliamo se la velocità cambierebbe significativamente nel punto 'i'
                double futureDist = distanceMap[path[i].X, path[i].Y];
                double futureSpeed = speedEvaluator.GetMaxSpeedForDistance(futureDist);

                bool isVisible = _lineOfSightChecker.HasLineOfSight(distanceMap, path[lastWaypointIndex], path[i], requiredClearance);

                // Spezziamo se:
                // 1. Non c'è più linea di vista con l'ingombro attuale
                // 2. La velocità cambierebbe (perché dobbiamo emettere un nuovo edge VDA5050 con velocità diversa)
                if (!isVisible || Math.Abs(futureSpeed - currentSpeed) > 0.001)
                {
                    // Aggiungiamo il punto precedente come waypoint
                    waypoints.Add(path[i - 1]);
                    lastWaypointIndex = i - 1;
                }
            }

            waypoints.Add(path[path.Count - 1]);
            return waypoints;
        }
    }
}
