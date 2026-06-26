using System;
using YueXinMiaoPet.Models;

namespace YueXinMiaoPet.Services
{
    public class TimeStateService
    {
        public string GetTimeTag(AppConfig config, DateTime now)
        {
            TimeSpan current = now.TimeOfDay;
            TimeSpan seven = Parse("07:00", "07:00");
            TimeSpan workStart = Parse(config.WorkStartTime, "09:00");
            TimeSpan workStartEnd = workStart.Add(TimeSpan.FromHours(1));
            TimeSpan lunchStart = Parse(config.LunchStartTime, "12:00");
            TimeSpan lunchEnd = Parse(config.LunchEndTime, "13:30");
            TimeSpan workEnd = Parse(config.WorkEndTime, "18:00");
            TimeSpan eveningStart = Parse(config.EveningStartTime, "20:00");
            TimeSpan sleep = Parse(config.SleepTime, "23:30");

            if (IsInRange(current, sleep, seven))
            {
                return "night";
            }

            if (IsInRange(current, seven, workStart))
            {
                return "morning";
            }

            if (IsInRange(current, workStart, workStartEnd))
            {
                return "work_start";
            }

            if (IsInRange(current, workStartEnd, lunchStart))
            {
                return "morning";
            }

            if (IsInRange(current, lunchStart, lunchEnd))
            {
                return "noon";
            }

            if (IsInRange(current, lunchEnd, workEnd))
            {
                return "afternoon";
            }

            if (IsInRange(current, workEnd, eveningStart))
            {
                return "work_end";
            }

            if (IsInRange(current, eveningStart, sleep))
            {
                return "evening";
            }

            return "night";
        }

        public bool IsWorkingTime(AppConfig config, DateTime now)
        {
            TimeSpan current = now.TimeOfDay;
            return IsInRange(current, Parse(config.WorkStartTime, "09:00"), Parse(config.WorkEndTime, "18:00"));
        }

        public bool IsAfterWork(AppConfig config, DateTime now)
        {
            TimeSpan current = now.TimeOfDay;
            return IsInRange(current, Parse(config.WorkEndTime, "18:00"), Parse(config.SleepTime, "23:30"));
        }

        private TimeSpan Parse(string value, string fallback)
        {
            TimeSpan result;
            if (TimeSpan.TryParse(value, out result))
            {
                return result;
            }

            TimeSpan.TryParse(fallback, out result);
            return result;
        }

        private bool IsInRange(TimeSpan current, TimeSpan start, TimeSpan end)
        {
            if (start <= end)
            {
                return current >= start && current < end;
            }

            return current >= start || current < end;
        }
    }
}
