using System;

namespace Nucs {
    public static class DateTimeUtilities {
        public static string TimeFormat = "hh:mm:ss.fff";
        public static string DateTimeFormat = "yyyyMMdd HH:mm:ss";
        public static string MinuteTimeFormat = "hh:mm";
        public static string SecondTimeFormat = "HH:mm:ss";

        public static string DateOnlyFormat = "yyyyMMdd";
        public static string TimeFormatSeconds = "hhmmss";

        public static bool IsDayPassed(DateTime oldDay, DateTime newDay) {
            return newDay.Date > oldDay.Date ? true : false;
        }

        public static bool IsTimeEnclosed(DateTime Time, DateTime StartTime, DateTime EndTime) {
            if (Time.Ticks > StartTime.Ticks && Time.Ticks < EndTime.Ticks)
                return true;

            return false;
        }

        // Function is used to round the DateTime to the nearest Interval specified using a TimeSpan
        // For example, used by the Bar Generator to generate timestamps for bars
        public static DateTime Round(DateTime dt, TimeSpan ts, bool up) {
            long totalMilliseconds = Convert.ToUInt32(dt.TimeOfDay.TotalMilliseconds);
            long period = Convert.ToUInt32(ts.TotalMilliseconds);

            totalMilliseconds -= totalMilliseconds % period;

            DateTime rounded = new DateTime(dt.Date.Ticks + totalMilliseconds * 10000); // Convert to ticks.

            if (up)
                return rounded + ts;

            return rounded;
        }

        public static DateTime GetFirstBusinessDay(int Year, int Month) {
            DateTime FirstOfMonth = default(DateTime);
            DateTime FirstBusinessDay = default(DateTime);
            FirstOfMonth = new DateTime(Year, Month, 1);
            if (FirstOfMonth.DayOfWeek == DayOfWeek.Sunday) {
                FirstBusinessDay = FirstOfMonth.AddDays(1);
            } else if (FirstOfMonth.DayOfWeek == DayOfWeek.Saturday) {
                FirstBusinessDay = FirstOfMonth.AddDays(2);
            } else {
                FirstBusinessDay = FirstOfMonth;
            }

            return FirstBusinessDay;
        }

        public static DateTime GetLastBusinessDay(int Year, int Month) {
            DateTime LastOfMonth = default(DateTime);
            DateTime LastBusinessDay = default(DateTime);
            LastOfMonth = new DateTime(Year, Month, DateTime.DaysInMonth(Year, Month));
            if (LastOfMonth.DayOfWeek == DayOfWeek.Sunday) {
                LastBusinessDay = LastOfMonth.AddDays(-2);
            } else if (LastOfMonth.DayOfWeek == DayOfWeek.Saturday) {
                LastBusinessDay = LastOfMonth.AddDays(-1);
            } else {
                LastBusinessDay = LastOfMonth;
            }

            return LastBusinessDay;
        }

        public static DateTime AddBusinessDays(DateTime date, int days) {
            if (days == 0) return date;

            if (date.DayOfWeek == DayOfWeek.Saturday) {
                date = date.AddDays(2);
                days -= 1;
            } else if (date.DayOfWeek == DayOfWeek.Sunday) {
                date = date.AddDays(1);
                days -= 1;
            }

            date = date.AddDays(days / 5 * 7);
            int extraDays = days % 5;

            if ((int) date.DayOfWeek + extraDays > 5) {
                extraDays += 2;
            }

            return date.AddDays(extraDays);
        }

        public static DateTime SubtractBusinessDays(this DateTime current, int days) {
            return AddBusinessDays(current, -days);
        }

        public static bool IsBetweenFirstOrLast_X_DaysOfTheMonth(DateTime currentDate, int NumDays) {
            DateTime firstBusinessDay = GetFirstBusinessDay(currentDate.Year, currentDate.Month);
            DateTime lastBusinessDay = GetLastBusinessDay(currentDate.Year, currentDate.Month);

            DateTime firstBusinesssDayAndNumDays = AddBusinessDays(firstBusinessDay, NumDays);
            DateTime lastBusinessDayAndNumDays = SubtractBusinessDays(lastBusinessDay, NumDays);

            if (currentDate.Day < firstBusinesssDayAndNumDays.Day || currentDate.Day > lastBusinessDayAndNumDays.Day)
                return true;

            return false;
        }
    }
}