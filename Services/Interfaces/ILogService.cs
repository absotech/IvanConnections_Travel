namespace IvanConnections_Travel.Services.Interfaces
{
    public interface ILogService
    {
        void LogException(Exception exception);

        void LogMessage(string message);
    }
}
