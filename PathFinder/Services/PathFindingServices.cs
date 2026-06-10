using PathFinder.Interfaces;
using PathFinder.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PathFinder.Services
{
    public class PathfindingService
    {
        private readonly IGridFactory _gridFactory;
        private readonly IPathfindingEngine _engine;
        private readonly IPathOptimizer _optimizer;
        private readonly IHeadingCalculator _headingCalculator;
        private readonly ISpeedEvaluator _speedEvaluator;

        public PathfindingService(
            IGridFactory gridFactory,
            IPathfindingEngine engine,
            IPathOptimizer optimizer,
            IHeadingCalculator headingCalculator,
            ISpeedEvaluator speedEvaluator)
        {
            _gridFactory = gridFactory;
            _engine = engine;
            _optimizer = optimizer;
            _headingCalculator = headingCalculator;
            _speedEvaluator = speedEvaluator;
        }

        public async Task<List<Pose>> CalculatePathAsync(
            Stream imageStream,
            List<Coordinate> targetCells,
            int cellSize,
            double resolution,
            double? finalHeadingRad = null)
        {
            var rawGrid = await _gridFactory.ParseImageAsync(imageStream, cellSize);
            double gridCellSizeMeters = resolution * cellSize;
            var distanceMap = _gridFactory.CreateDistanceMap(rawGrid, gridCellSizeMeters);

            var fullOptimizedPath = new List<Coordinate>();

            for (int i = 0; i < targetCells.Count - 1; i++)
            {
                var startNode = targetCells[i];
                var endNode = targetCells[i + 1];

                var segmentPath = _engine.FindPath(distanceMap, startNode, endNode, _speedEvaluator, gridCellSizeMeters);

                if (segmentPath == null || segmentPath.Count == 0)
                    return new List<Pose>(); 

                var optimizedSegment = _optimizer.Optimize(segmentPath, distanceMap, _speedEvaluator);

                if (i > 0)
                {
                    optimizedSegment.RemoveAt(0);
                }

                fullOptimizedPath.AddRange(optimizedSegment);
            }

            // ⭐ LOGICA DECELERAZIONE FINALE (1.5 metri)
            if (fullOptimizedPath.Count >= 2)
            {
                var last = fullOptimizedPath[^1];
                var penultimate = fullOptimizedPath[^2];

                double dx = last.X - penultimate.X;
                double dy = last.Y - penultimate.Y;
                double distCells = Math.Sqrt(dx * dx + dy * dy);
                double distMeters = distCells * gridCellSizeMeters;

                // Se l'ultimo segmento è più lungo di 1.5m, iniettiamo un punto a 1.5m dalla fine
                if (distMeters > 1.5)
                {
                    double ratio = 1.5 / distMeters; // Percentuale della fine rispetto all'inizio del segmento
                    // Calcoliamo la posizione a 1.5m dal fondo (quindi a 1-ratio dall'inizio)
                    double t = 1.0 - ratio;
                    int newX = (int)Math.Round(penultimate.X + t * dx);
                    int newY = (int)Math.Round(penultimate.Y + t * dy);
                    
                    fullOptimizedPath.Insert(fullOptimizedPath.Count - 1, new Coordinate(newX, newY));
                }
            }

            var finalPoses = _headingCalculator.CalculateHeadings(fullOptimizedPath, finalHeadingRad);

            // Assegnazione velocità con soglia di decelerazione finale
            double minSpeed = _speedEvaluator.GetProfiles().Last().Speed;
            var goalPose = finalPoses.Last();

            foreach (var pose in finalPoses)
            {
                double distToGoal = Math.Sqrt(Math.Pow(pose.X - goalPose.X, 2) + Math.Pow(pose.Y - goalPose.Y, 2)) * gridCellSizeMeters;
                
                if (distToGoal <= 1.51 && distToGoal > 0.001) // Siamo nella zona di decelerazione (ma non siamo già al goal)
                {
                    pose.speed = minSpeed;
                }
                else
                {
                    double distFromWall = distanceMap[(int)pose.X, (int)pose.Y];
                    pose.speed = _speedEvaluator.GetMaxSpeedForDistance(distFromWall);
                }
            }

            return finalPoses;
        }
    }
}
