using PigeonsTracker.DataModels;

namespace PigeonsTracker.Helper;

public static class PigeonsTrackingRecordTimeCalculator
{
    public static PigeonsTrackingRecord CalculateHours(PigeonsTrackingRecord trackingRecord)
    {
        foreach (var rec in trackingRecord.Records)
        {
            if (rec.EndTime.HasValue)
            {
                rec.TotalBirdFlyingTime = rec.EndTime.Value.Subtract(trackingRecord.StartTime);
                //Console.WriteLine($"Total Bird Hours:Minutes : {rec.TotalBirdFlyingTime.Value.ToString(@"hh\:mm\:ss")}");
            }
        }

        var temp = trackingRecord.Records.Where(w => w.TotalBirdFlyingTime != null).Select(s => s.TotalBirdFlyingTime.Value).ToList();

        /*trackingRecord.TotalFlyingTime =
            new TimeSpan(trackingRecord.Records.Where(w => w.TotalBirdFlyingTime != null)
                                       .Sum(r => r.TotalBirdFlyingTime.Value.Ticks));*/

        if (temp.Any())
        {
            var mins = temp.GetTotalMinutes();

            //Console.WriteLine($"Total Minutes : {mins}");
            trackingRecord.TotalFlyingTime = TimeSpan.FromMinutes(mins);

            //Console.WriteLine($"Total Tracking Hours:Minutes : {string.Format("{0}:{1}", (int) trackingRecord.TotalFlyingTime.Value.TotalHours, trackingRecord.TotalFlyingTime.Value.Minutes)}");
        }

        return trackingRecord;
    }

    public static string CalculateTimeDifferenceFormatted(DateTime startTime, DateTime endTime)
    {
        var duration = endTime - startTime;
        return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
    }

    // Calculates the total duration for multiple pigeons and returns the total time in HH:mm:ss format.
    public static string CalculateTotalTime(DateTime startTimes, List<DateTime> endTimes)
    {
        var totalDuration = endTimes.Aggregate(TimeSpan.Zero, (current, t) => current + (t - startTimes));

        return $"{(int)totalDuration.TotalHours:D2}:{totalDuration.Minutes:D2}:{totalDuration.Seconds:D2}";
    }



}