namespace DatabaseManager.BlazorComponents.Models
{
    public class InterpolationResult
    {
        public enum ResultOptions
        {
            Hit,
            NearestNeighbor,
            Interpolated,
            Extrapolated,
            OutOfBounds
        }

        public ResultOptions Result { get; set; }

        public double Value { get; set; }

        public SurfacePoint? Point { get; set; }
    }
}
