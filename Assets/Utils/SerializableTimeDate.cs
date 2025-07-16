using System;
using UnityEngine;

namespace Utils
{
    [System.Serializable]
    public struct SerializableTime
    {
        [Range(0, 23)] public int hours;
        [Range(0, 59)] public int minutes;
        public TimeSpan ToTimeSpan() => new TimeSpan(hours, minutes, 0);
    }

    [System.Serializable]
    public struct SerializableDate
    {
        [Range(1, 12)] public int month;
        [Range(1, 31)] public int day;
        public DateTime ToDateTime(int year = 2000)
        {
            // Clamp the day to the max days in the month to prevent exceptions
            Debug.Log($"monthx: {month}");
            int daysInMonth = DateTime.DaysInMonth(year, month);
            int safeDay = Mathf.Clamp(day, 1, daysInMonth);
            return new DateTime(year, month, safeDay);
        }
    }

    [System.Serializable]
    public class TimeRange
    {
        public SerializableTime BeginTime;
        public SerializableTime EndTime;
        public bool TimeRangeContainsTime(int hour, int minute)
        {
            return TimeRangeContainsTime(new SerializableTime() { minutes = minute, hours = hour });
        }
        public bool TimeRangeContainsTime(SerializableTime time)
        {
            var start = BeginTime.ToTimeSpan();
            var end = EndTime.ToTimeSpan();
            var check = time.ToTimeSpan();
            if (start <= end)
            {
                return check >= start && check <= end;
            }
            return check >= start || check <= end;
        }
    }

    [System.Serializable]
    public class DateRange
    {
        public SerializableDate BeginDate;
        public SerializableDate EndDate;
        public bool DateRangeContainsDate(int month, int day)
        {
            return DateRangeContainsDate(new SerializableDate { month = month, day = day });
        }
        public bool DateRangeContainsDate(SerializableDate date)
        {
            // Dummy year should be a leap year
            var dummyYear = 2000;
            DateTime start = BeginDate.ToDateTime(dummyYear);
            DateTime end = EndDate.ToDateTime(dummyYear);
            DateTime check = date.ToDateTime(dummyYear);
            if (start <= end)
            {
                // Normal range: e.g. Mar 15 - Jun 01
                return check >= start && check <= end;
            }
            else
            {
                // Wrap-around range: e.g. Nov 15 - Feb 10
                return check >= start || check <= end;
            }
        }
    }
} 