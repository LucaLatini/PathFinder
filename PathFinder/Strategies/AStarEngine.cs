using PathFinder.Interfaces;
using PathFinder.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PathFinder.Strategies
{
    public class AStarEngine : IPathfindingEngine
    {
        public List<Coordinate> FindPath(double[,] distanceMap, Coordinate start, Coordinate end, ISpeedEvaluator speedEvaluator, double gridCellSizeMeters)
        {
            int width = distanceMap.GetLength(0);
            int height = distanceMap.GetLength(1);

            double maxPossibleSpeed = speedEvaluator.GetProfiles().Max(p => p.Speed);

            if (!IsValid(start, width, height) || !IsValid(end, width, height) ||
                !speedEvaluator.IsDistanceSafe(distanceMap[start.X, start.Y]) || 
                !speedEvaluator.IsDistanceSafe(distanceMap[end.X, end.Y]))
            {
                return new List<Coordinate>();
            }

            var openQueue = new PriorityQueue<PathNode, int>();
            var allNodes = new Dictionary<Coordinate, PathNode>();
            var closedSet = new HashSet<Coordinate>();

            var startNode = new PathNode(start) { 
                GCost = 0, 
                HCost = (int)(GetOctileDistance(start, end) * gridCellSizeMeters / maxPossibleSpeed * 1000) 
            };
            allNodes[start] = startNode;
            openQueue.Enqueue(startNode, startNode.FCost);

            while (openQueue.Count > 0)
            {
                var current = openQueue.Dequeue();

                if (!closedSet.Add(current.Position)) continue;

                if (current.Position == end)
                {
                    return RetracePath(current);
                }

                foreach (var neighborPos in GetNeighbors(current.Position, width, height))
                {
                    double distanceToObstacle = distanceMap[neighborPos.X, neighborPos.Y];
                    if (closedSet.Contains(neighborPos) || !speedEvaluator.IsDistanceSafe(distanceToObstacle))
                        continue;

                    double speed = speedEvaluator.GetMaxSpeedForDistance(distanceToObstacle);
                    if (speed <= 0) continue;

                    double stepDistanceCells = IsDiagonal(current.Position, neighborPos) ? 1.414 : 1.0;
                    double stepDistanceMeters = stepDistanceCells * gridCellSizeMeters;

                    int timeCost = (int)(stepDistanceMeters / speed * 1000);

                    int turnPenalty = 0;
                    if (current.Parent != null)
                    {
                        int dx1 = current.Position.X - current.Parent.Position.X;
                        int dy1 = current.Position.Y - current.Parent.Position.Y;
                        int dx2 = neighborPos.X - current.Position.X;
                        int dy2 = neighborPos.Y - current.Position.Y;

                        if (dx1 != dx2 || dy1 != dy2)
                        {
                            turnPenalty = 50; 
                        }
                    }

                    int newGCost = current.GCost + timeCost + turnPenalty;

                    if (!allNodes.TryGetValue(neighborPos, out var neighborNode))
                    {
                        neighborNode = new PathNode(neighborPos);
                        allNodes[neighborPos] = neighborNode;
                    }

                    if (newGCost < neighborNode.GCost || !neighborNode.InOpenList)
                    {
                        neighborNode.GCost = newGCost;
                        neighborNode.HCost = (int)(GetOctileDistance(neighborPos, end) * gridCellSizeMeters / maxPossibleSpeed * 1000);
                        neighborNode.Parent = current;

                        openQueue.Enqueue(neighborNode, neighborNode.FCost);
                        neighborNode.InOpenList = true;
                    }
                }
            }

            return new List<Coordinate>();
        }

        private double GetOctileDistance(Coordinate a, Coordinate b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            return (dx + dy) + (1.414 - 2) * Math.Min(dx, dy);
        }

        private IEnumerable<Coordinate> GetNeighbors(Coordinate current, int width, int height)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    int newX = current.X + x;
                    int newY = current.Y + y;

                    if (IsValid(new Coordinate(newX, newY), width, height))
                    {
                        yield return new Coordinate(newX, newY);
                    }
                }
            }
        }

        private bool IsValid(Coordinate c, int width, int height)
        {
            return c.X >= 0 && c.X < width && c.Y >= 0 && c.Y < height;
        }

        private bool IsDiagonal(Coordinate a, Coordinate b)
        {
            return Math.Abs(a.X - b.X) == 1 && Math.Abs(a.Y - b.Y) == 1;
        }

        private List<Coordinate> RetracePath(PathNode endNode)
        {
            var path = new List<Coordinate>();
            var current = endNode;

            while (current != null)
            {
                path.Add(current.Position);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }
    }
}
