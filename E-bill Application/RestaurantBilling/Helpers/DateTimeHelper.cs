using System;

namespace RestaurantBilling.Helpers
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo IndianZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

        public static DateTime GetIndianTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndianZone);
        }

        public static DateTime ToIndianTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Unspecified)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, IndianZone);
        }

        public static DateTime ToUtcTime(DateTime indianDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(indianDateTime, IndianZone);
        }
    }
}
