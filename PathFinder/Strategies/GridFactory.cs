using PathFinder.Interfaces;
using SkiaSharp;

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

                    // Filtro anti-rumore: consideriamo ostacolo solo se è molto scuro
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
                        // Solo se almeno il 30% della cella logica è muro, marchiamo la cella come ostacolo.
                        // Altrimenti è polvere o rumore dello SLAM.
                        if (ratio > 0.30) rawGrid[x, y] = 255;
                        else rawGrid[x, y] = 0;
                    }
                }
            }

            return rawGrid;
        }

        public int[,] InflateGrid(int[,] rawGrid, int robotRadiusCells)
        {
            int width = rawGrid.GetLength(0);
            int height = rawGrid.GetLength(1);
            var inflatedGrid = new int[width, height];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    inflatedGrid[x, y] = rawGrid[x, y];

            if (robotRadiusCells <= 0) return inflatedGrid;

            int radiusSquared = robotRadiusCells * robotRadiusCells;

            // Creiamo un cuscinetto dinamico basato sul raggio del robot
            double inflationRadiusCells = robotRadiusCells + 2;
            int inflationRadiusSquared = (int)(inflationRadiusCells * inflationRadiusCells);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Espandiamo solo a partire dai muri fisici (255)
                    if (rawGrid[x, y] == 255)
                    {
                        int maxRadiusCheck = (int)Math.Ceiling(inflationRadiusCells);

                        for (int dy = -maxRadiusCheck; dy <= maxRadiusCheck; dy++)
                        {
                            for (int dx = -maxRadiusCheck; dx <= maxRadiusCheck; dx++)
                            {
                                int distSquared = dx * dx + dy * dy;

                                if (distSquared <= inflationRadiusSquared)
                                {
                                    int nx = x + dx;
                                    int ny = y + dy;

                                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                    {
                                        if (distSquared <= radiusSquared)
                                        {
                                            inflatedGrid[nx, ny] = 255; // Zona impatto letale
                                        }
                                        else if (inflatedGrid[nx, ny] != 255)
                                        {
                                            // Zona sfumata (il costo cala man mano che ci allontaniamo dal muro)
                                            double dist = Math.Sqrt(distSquared);
                                            double denominator = inflationRadiusCells - robotRadiusCells;
                                            if (denominator <= 0) denominator = 1.0;

                                            double costFactor = 1.0 - ((dist - robotRadiusCells) / denominator);
                                            int newCost = (int)Math.Max(1, Math.Min(200, costFactor * 150));

                                            if (newCost > inflatedGrid[nx, ny])
                                            {
                                                inflatedGrid[nx, ny] = newCost;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return inflatedGrid;
        }
    }
}