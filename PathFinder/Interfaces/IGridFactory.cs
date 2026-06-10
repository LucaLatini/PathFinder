using System.IO;
using System.Threading.Tasks;

namespace PathFinder.Interfaces
{
    public interface IGridFactory
    {
        Task<int[,]> ParseImageAsync(Stream imageStream, int cellSize);
        double[,] CreateDistanceMap(int[,] rawGrid, double resolution);
    }
}
