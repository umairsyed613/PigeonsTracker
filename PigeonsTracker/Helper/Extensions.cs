using System;
using System.Collections.Generic;
using System.Globalization;

namespace PigeonsTracker.Helper
{
    public static class Extensions
    {
        public static TimeSpan Sum(this IEnumerable<TimeSpan> timeSpans)
        {
            TimeSpan sumTillNowTimeSpan = TimeSpan.Zero;

            foreach (TimeSpan timeSpan in timeSpans) { sumTillNowTimeSpan = sumTillNowTimeSpan.Add(timeSpan); }

            return sumTillNowTimeSpan;
        }

        public static double GetTotalMinutes(this IEnumerable<TimeSpan> timeSpans)
        {
            double min = 0;

            foreach (TimeSpan timeSpan in timeSpans) { min += timeSpan.TotalMinutes; }

            return min;
        }

        public static string ToCustomFormat(this TimeSpan t)
        {
            return $"{((int) t.TotalHours).ToString("D2", CultureInfo.InvariantCulture)}:{t.Minutes.ToString("D2", CultureInfo.InvariantCulture)}:{t.Seconds.ToString("D2", CultureInfo.InvariantCulture)}";
        }

        public static string FromMinutesToCustomFormat(this double minutes)
        {
            var t = TimeSpan.FromMinutes(minutes);
            return $"{((int) t.TotalHours).ToString("D2", CultureInfo.InvariantCulture)}:{t.Minutes.ToString("D2", CultureInfo.InvariantCulture)}:{t.Seconds.ToString("D2", CultureInfo.InvariantCulture)}";
        }

        public static string ToCustomFormat(this DateTime t)
        {
            return $"{((int) t.Hour).ToString("D2", CultureInfo.InvariantCulture)}:{t.Minute.ToString("D2", CultureInfo.InvariantCulture)}";
        }
        public static string ToCustomFormat(this DateTime? temp)
        {
            if (!temp.HasValue)
            {
                return string.Empty;
            }

            return temp.Value.ToString("hh:mm:ss tt");
        }

        public static string KelvinToCelsius(this double temp)
        {
            var c = temp - 273.15;

            return Convert.ToInt32(c).ToString();
        }

        public static string ToOneDecimal(this double temp) => $"{temp:0.#}";

        public static (int year, int month, int day) GetSeparatedDate(this DateTime date)
        {
            var year = DateTime.Now.Year;

            if (date.Year >= 1 && date.Year <= 9999) { year = date.Year; }

            var month = DateTime.Now.Month;

            if (date.Month >= 1 && date.Month <= 12) { month = date.Month; }

            var totalDaysInMonth = DateTime.DaysInMonth(year, month);

            var day = 1;

            if (date.Day >= 1 && date.Day <= totalDaysInMonth) { day = date.Day; }

            return (year, month, day);
        }

        public static (int hour, int minute, int second) GetSeparatedTime(this DateTime date)
        {
            /*
             hour is less than 0 or greater than 23.
             minute is less than 0 or greater than 59.
             second is less than 0 or greater than 59.
             */
            var hour = DateTime.Now.Hour;

            if (date.Hour is >= 0 and <= 23) { hour = date.Hour; }

            var minute = DateTime.Now.Minute;

            if (date.Minute is >= 0 and <= 59) { minute = date.Minute; }

            var second = 0;

            return (hour, minute, second);
        }
    }
}
