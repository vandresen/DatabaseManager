namespace DatabaseManager.ServerLessClient.Services
{
    public interface IPopupMessage
    {
        ValueTask DisplayErrorMessage(string message);
        ValueTask DisplaySuccessMessage(string message);
    }
}
