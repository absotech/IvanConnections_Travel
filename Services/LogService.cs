using IvanConnections_Travel.Services.Interfaces;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace IvanConnections_Travel.Services
{
    public class LogService : ILogService
    {
        private readonly string logPath = Path.Combine(FileSystem.AppDataDirectory, $"log_{DateTime.UtcNow.Date.Ticks}.log");

        public void LogMessage(string message)
        {
            try
            {
                this.WriteMessage(message);
            }
            catch
            {
                // The log must never fail.
            }
        }

        public void LogException(Exception exception)
        {
            try
            {
                this.WriteException(exception);
            }
            catch
            {
                // The log must never fail.
            }
        }

        private static string GetCurrentPageName()
        {
            try
            {
                return Shell.Current.CurrentPage.GetType().Name;
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string GetEntryAssembly()
        {
            try
            {
                return Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string EnrichWithProperties()
        {
            var properties = new
            {
                EntryAssembly = GetEntryAssembly(),
                Version = VersionTracking.CurrentVersion,
                Page = GetCurrentPageName(),
                Platform = DeviceInfo.Platform.ToString()
            };

            return JsonSerializer.Serialize(properties);
        }

        private void WriteException(Exception exception)
        {
            try
            {
                var exceptionText = exception.ToString();

                this.WriteMessage(exceptionText);
            }
            catch
            {
                // The log must never fail.
            }
        }

        private void WriteMessage(string message)
        {
            try
            {
                File.AppendAllText(this.logPath, message);
                File.AppendAllText(this.logPath, EnrichWithProperties());

                Debug.Write(message);
                Console.WriteLine(message);
            }
            catch
            {
                // The log must never fail.
            }
        }
    }
}
