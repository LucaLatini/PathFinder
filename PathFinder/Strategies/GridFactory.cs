using PathFinder.Interfaces;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PathFinder.Strategies
{
    public class GridFactory : IGridFactory
    {
        public async Task<int[,]> ParseImageAsync(Stream imageStream, int cellSize)
        {
            if (cellSize <= 0) cellSize = 1;

            using var bitmap = SKBitmap.Decode(imageStream);
            var pixels = bitmap.Pixels;

            int origWidth = bitmap.Width;
            int origHeight = bitmap.Height;

            int newWidth = (int)Math.Ceiling((double)origWidth / cellSize);
            int newHeight = (int)Math.Ceiling((double)origHeight / cellSize);

            var rawGrid = new int[newWidth, newHeight];
            int[,] obstaclePixelsCount = new int[newWidth, newHeight];
            int[,] totalPixelsCount = new int[newWidth, newHeight];

            for (int y = 0; y < origHeight; y++)
            {
                for (int x = 0; x < origWidth; x++)
                {
                    SKColor pixel = pixels[y * origWidth + x];
                    int gridX = x / cellSize;
                    int gridY = y / cellSize;

                    if (gridX >= newWidth || gridY >= newHeight) continue;

                    totalPixelsCount[gridX, gridY]++;

                    if (pixel.Red < 80 && pixel.Green < 80 && pixel.Blue < 80)
                    {
                        obstaclePixelsCount[gridX, gridY]++;
                    }
                }
            }

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    if (totalPixelsCount[x, y] > 0)
                    {
                        double ratio = (double)obstaclePixelsCount[x, y] / totalPixelsCount[x, y];
                        if (ratio > 0.30) rawGrid[x, y] = 255;
                        else rawGrid[x, y] = 0;
                    }
                }
            }

            return rawGrid;
        }

        public double[,] CreateDistanceMap(int[,] rawGrid, double resolution)
        {
            int width = rawGrid.GetLength(0);
            int height = rawGrid.GetLength(1);
            var distSq = new double[width, height];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    distSq[x, y] = (rawGrid[x, y] == 255) ? 0 : 1e9;

            // Passaggio 1: Alto-Sinistra -> Basso-Destra
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x > 0) distSq[x, y] = Math.Min(distSq[x, y], distSq[x - 1, y] + 1);
                    if (y > 0) distSq[x, y] = Math.Min(distSq[x, y], distSq[x, y - 1] + 1);
                    if (x > 0 && y > 0) distSq[x, y] = Math.Min(distSq[x, y], distSq[x - 1, y - 1] + 1.414);
                }
            }

            // Passaggio 2: Basso-Destra -> Alto-Sinistra
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = width - 1; x >= 0; x--)
                {
                    if (x < width - 1) distSq[x, y] = Math.Min(distSq[x, y], distSq[x + 1, y] + 1);
                    if (y < height - 1) distSq[x, y] = Math.Min(distSq[x, y], distSq[x, y + 1] + 1);
                    if (x < width - 1 && y < height - 1) distSq[x, y] = Math.Min(distSq[x, y], distSq[x + 1, y + 1] + 1.414);
                }
            }

            var result = new double[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    result[x, y] = distSq[x, y] * resolution;

            return result;
        }
    }
}
