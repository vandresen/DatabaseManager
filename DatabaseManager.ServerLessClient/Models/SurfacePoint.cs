namespace DatabaseManager.ServerLessClient.Models
{
    public class SurfacePoint
    {
        public double Value { get; }

        public double[] Coordinates { get; }

        public SurfacePoint(double value, params double[] coordinates)
        {
            Value = value;
            Coordinates = coordinates;
        }

        public override string ToString()
        {
            return $"{string.Join(";", Coordinates)} -> {Value}";
        }
    }
}
