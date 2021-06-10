using System;
using System.Linq;
using PigeonsTracker.DataModels;

namespace PigeonsTracker.Helper
{
    public class PigeonsTrackingRecordTimeCalculator
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
    }
}
