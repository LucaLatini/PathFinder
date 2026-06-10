namespace PathFinder.Models
{
    // Rendo Pose una classe per poter modificare la velocità dopo la creazione
    public class Pose
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Theta { get; set; }
        public double speed { get; set; }

        public Pose(double x, double y, double theta, double speed)
        {
            X = x;
            Y = y;
            Theta = theta;
            this.speed = speed;
        }
    }
}
