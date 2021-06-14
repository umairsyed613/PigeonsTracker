using System;
using System.Globalization;
using PigeonsTracker.DataModels;

namespace PigeonsTracker.Helper
{
    public static class MessageCreator
    {
        public static string Create(PigeonsTrackingRecord record)
        {
            string result;

            result = $"{record.RoofName}{Environment.NewLine}";
            result += $"Start Time: {record.StartTime.ToString("dd MMMM yyyy hh:mm:ss tt", CultureInfo.CurrentCulture)}{Environment.NewLine}";
            result += $"{Environment.NewLine}";

            var i = 1;

            result += $"Nr. {"BirdName".PadRight(25)}Landed\tAverage{Environment.NewLine}";
            foreach (var rec in record.Records)
            {
                result += $"{i}:\t{rec.BirdName.PadRight(25)}{rec.EndTime.ToCustomFormat()}\t{rec.TotalBirdFlyingTime?.ToCustomFormat()}{Environment.NewLine}";
                i++;
            }

            result += $"{Environment.NewLine}Total Average: \t\t{record.TotalFlyingTime?.ToCustomFormat()}{Environment.NewLine}";

            return result;
        }
    }
}
