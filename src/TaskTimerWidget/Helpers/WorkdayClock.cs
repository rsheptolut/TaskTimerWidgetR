using System.Globalization;

namespace TaskTimerWidget.Helpers
{
    internal static class WorkdayClock
    {
        private const int DayBoundaryHour = 4;
        private const string DayKeyFormat = "yyyy-MM-dd";

        public static string GetDayKey(DateTime localNow)
        {
            var workday = localNow.TimeOfDay < TimeSpan.FromHours(DayBoundaryHour)
                ? localNow.Date.AddDays(-1)
                : localNow.Date;

            return workday.ToString(DayKeyFormat, CultureInfo.InvariantCulture);
        }

        public static DateTime ParseDayKey(string dayKey)
        {
            return DateTime.ParseExact(dayKey, DayKeyFormat, CultureInfo.InvariantCulture);
        }

        public static string FormatDisplay(string dayKey)
        {
            return ParseDayKey(dayKey).ToString("ddd, dd MMM yyyy", CultureInfo.CurrentCulture);
        }
    }
}
