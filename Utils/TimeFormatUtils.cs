namespace IvanConnections_Travel.Utils
{
    public static class TimeFormatUtils
    {
        /// <summary>
        /// Formats a time difference in Romanian with the appropriate noun form
        /// </summary>
        /// <param name="timestamp">The timestamp to compare with current time</param>
        /// <returns>Formatted string with time difference in Romanian</returns>
        public static string FormatTimeDifferenceInRomanian(DateTime timestamp)
        {
            TimeSpan timeDiff = DateTime.Now - timestamp;

            if (timeDiff.TotalHours >= 1)
            {
                int hours = (int)timeDiff.TotalHours;
                return hours == 1
                    ? "o oră"
                    : $"{hours} ore";
            }
            else if (timeDiff.TotalMinutes >= 1)
            {
                int minutes = (int)timeDiff.TotalMinutes;
                return minutes == 1
                    ? "un minut"
                    : $"{minutes} minute";
            }
            else
            {
                int seconds = Math.Max(1, (int)timeDiff.TotalSeconds); // At least 1 second
                return seconds == 1
                    ? "o secundă"
                    : $"{seconds} secunde";
            }
        }
    }
}
