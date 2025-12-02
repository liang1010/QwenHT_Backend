using System;

namespace QwenHT.Utilities
{
    public static class TimezoneHelper
    {
        /// <summary>
        /// Converts a DateTime to UTC, handling different DateTimeKind values appropriately
        /// </summary>
        /// <param name="dateTime">The DateTime to convert to UTC</param>
        /// <returns>A DateTime in UTC</returns>
        public static DateTime ToUtcDateTime(DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                _ => dateTime.ToUniversalTime()
            };
        }

        /// <summary>
        /// Converts a nullable DateTime to UTC
        /// </summary>
        /// <param name="dateTime">The nullable DateTime to convert to UTC</param>
        /// <returns>A nullable DateTime in UTC, or null if input is null</returns>
        public static DateTime? ToUtcDateTime(DateTime? dateTime)
        {
            return dateTime.HasValue ? ToUtcDateTime(dateTime.Value) : null;
        }

        /// <summary>
        /// Converts a DateTime to the specified timezone from UTC
        /// </summary>
        /// <param name="utcDateTime">The UTC DateTime to convert</param>
        /// <param name="timeZoneId">The target timezone ID (e.g., "America/New_York", "Asia/Kuala_Lumpur")</param>
        /// <returns>A DateTime in the specified timezone</returns>
        public static DateTime ConvertUtcToTimezone(DateTime utcDateTime, string timeZoneId)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = ToUtcDateTime(utcDateTime);
            }

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
        }

        /// <summary>
        /// Converts a nullable DateTime to the specified timezone from UTC
        /// </summary>
        /// <param name="utcDateTime">The nullable UTC DateTime to convert</param>
        /// <param name="timeZoneId">The target timezone ID</param>
        /// <returns>A nullable DateTime in the specified timezone, or null if input is null</returns>
        public static DateTime? ConvertUtcToTimezone(DateTime? utcDateTime, string timeZoneId)
        {
            return utcDateTime.HasValue ? ConvertUtcToTimezone(utcDateTime.Value, timeZoneId) : null;
        }

        /// <summary>
        /// Gets the current UTC DateTime
        /// </summary>
        /// <returns>Current UTC DateTime</returns>
        public static DateTime GetCurrentUtcTime()
        {
            return DateTime.UtcNow;
        }
    }
}