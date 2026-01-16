namespace DatabaseManager.Services.DataQuality.Models
{
    public class ResponseDto
    {
        public bool IsSuccess { get; set; } = true;
        public object? Result { get; set; }
        public List<string> ErrorMessages { get; set; } = new();
    }
}
