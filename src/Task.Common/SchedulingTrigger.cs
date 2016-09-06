using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.String;

namespace Task.Common
{
    /// <summary>
    /// An enum representing the choice between hours and minutes.
    /// </summary>
    public enum HoursOrMinutes
    {
        Minutes,
        Hours
    }

    /// <summary>
    /// A set of flags for the days of the week.
    /// </summary>
    [Flags]
    public enum DayOfWeekFlag
    {
        Sunday = 1 << 0,
        Monday = 1 << 1,
        Tuesday = 1 << 2,
        Wednesday = 1 << 3,
        Thursday = 1 << 4,
        Friday = 1 << 5,
        Saturday = 1 << 6
    }

    /// <summary>
    /// An enum representing the specific weeks of the month.
    /// </summary>
    public enum WeekOfMonth
    {
        First,
        Second,
        Third,
        Fourth,
        Last
    }

    /// <summary>
    /// Represents a trigger for when and how often to schedule a task.
    /// </summary>
    public class SchedulingTrigger
    {
        /// <summary>
        /// The start date of the schedule and the time of day at which it should first run.
        /// </summary>
        public DateTime StartDateTime { get; set; }
        /// <summary>
        /// An optional end date.
        /// </summary>
        public DateTime? EndDateTime { get; set; }

        /// <summary>
        /// Does the task repeat throughout the day?
        /// </summary>
        public bool RepeatTask { get; set; }
        /// <summary>
        /// How often the task repeats within the day.
        /// </summary>
        public RepeatEvery? Every { get; set; }
        /// <summary>
        /// The task will repeat itself throughout the day until this criterion.
        /// </summary>
        public RepeatUntil? Until { get; set; }

        private DailyTrigger? _daily;
        /// <summary>
        /// Gets or sets daily scheduling repetition settings. This is mutually exclusive with Weekly and Monthly.
        /// </summary>
        public DailyTrigger? Daily
        {
            get { return _daily; }
            set
            {
                if (value.HasValue)
                {
                    if (_weekly.HasValue || _monthly.HasValue)
                        throw new InvalidOperationException("Daily, Weekly, and Monthly are mutually exclusive.");

                    _daily = value;
                }
                else
                {
                    _daily = null;
                }
            }
        }

        private WeeklyTrigger? _weekly;
        /// <summary>
        /// Gets or sets weekly scheduling repetition settings. This is mutually exclusive with Daily and Monthly.
        /// </summary>
        public WeeklyTrigger? Weekly
        {
            get { return _weekly; }
            set
            {
                if (value.HasValue)
                {
                    if (_daily.HasValue || _monthly.HasValue)
                        throw new InvalidOperationException("Daily, Weekly, and Monthly are mutually exclusive.");

                    _weekly = value;
                }
                else
                {
                    _weekly = null;
                }
            }
        }

        private MonthlyTrigger? _monthly;
        /// <summary>
        /// Gets or sets monthly scheduling repetition settings. This is mutually exclusive with Daily and Weekly.
        /// </summary>
        public MonthlyTrigger? Monthly
        {
            get { return _monthly; }
            set
            {
                if (value.HasValue)
                {
                    if (_daily.HasValue || _weekly.HasValue)
                        throw new InvalidOperationException("Daily, Weekly, and Monthly are mutually exclusive.");

                    _monthly = value;
                }
                else
                {
                    _monthly = null;
                }
            }
        }

        /// <summary>
        /// A structure describing a repetition pattern within a given day.
        /// </summary>
        public struct RepeatEvery
        {
            /// <summary>
            /// Number represents the number of hours or minutes to repeat based on.
            /// </summary>
            public int Number { get; set; }
            /// <summary>
            /// Determines whether Number represents hours or minutes.
            /// </summary>
            public HoursOrMinutes HoursOrMinutes { get; set; }
        }

        /// <summary>
        /// A stopping criteria for the repetition pattern within a given day.
        /// </summary>
        public struct RepeatUntil
        {
            /// <summary>
            /// Repeats until a time of day is reached. The date component is ignored.
            /// </summary>
            public DateTime? Time { get; set; }
            /// <summary>
            /// Repeats until a duration of time has passed.
            /// </summary>
            public TimeSpan? Duration { get; set; }
        }

        /// <summary>
        /// Represents daily scheduling criteria.
        /// </summary>
        public struct DailyTrigger
        {
            /// <summary>
            /// Run the task once every N days.
            /// </summary>
            public int EveryNDays { get; set; }
        }

        /// <summary>
        /// Represents weekly scheduling criteria.
        /// </summary>
        public struct WeeklyTrigger
        {
            /// <summary>
            /// Run the task every N weeks.
            /// </summary>
            public int EveryNWeeks { get; set; }
            /// <summary>
            /// Run the task on the given days of the week.
            /// </summary>
            public DayOfWeekFlag DaysOfWeek { get; set; }
        }

        /// <summary>
        /// Represents monthly scheduling criteria.
        /// </summary>
        public struct MonthlyTrigger
        {
            private int? _dayOfMonth;
            /// <summary>
            /// Run only on the given day of the month (1-31). This is mutually exclusive with DayOfWeek.
            /// </summary>
            public int? DayOfMonth
            {
                get { return _dayOfMonth; }
                set
                {
                    if (value.HasValue)
                    {
                        if (_dayOfWeek.HasValue)
                            throw new InvalidOperationException("DayOfMonth and DayOfWeek are mutually exclusive");
                        _dayOfMonth = value;
                    }
                    else
                    {
                        _dayOfMonth = null;
                    }
                }
            }

            private NthWeeklyDayOfWeek? _dayOfWeek;
            /// <summary>
            /// Run only on a specific week of the month, on a given day of the week. This is mutually exclusive with DayOfMonth.
            /// </summary>
            public NthWeeklyDayOfWeek? DayOfWeek
            {
                get { return _dayOfWeek; }
                set
                {
                    if (value.HasValue)
                    {
                        if (_dayOfMonth.HasValue)
                            throw new InvalidOperationException("DayOfMonth and DayOfWeek are mutually exclusive");
                        _dayOfWeek = value;
                    }
                    else
                    {
                        _dayOfWeek = null;
                    }
                }
            }

            /// <summary>
            /// Represents a specific week of the month and day of that week.
            /// </summary>
            public struct NthWeeklyDayOfWeek
            {
                /// <summary>
                /// Run on the first, second, third, fourth, or last week of the month.
                /// </summary>
                public WeekOfMonth NthMonthlyWeek { get; set; }
                /// <summary>
                /// Run on the given day of the week.
                /// </summary>
                public DayOfWeek DayOfWeek { get; set; }
            }
        }

        /// <summary>
        /// Formats a schedule trigger for display.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Daily.HasValue)
            {
                sb.Append(Daily.Value.EveryNDays == 1
                    ? "Run every day"
                    : $"Run every {Daily.Value.EveryNDays} days");
            }
            else if (Weekly.HasValue)
            {
                sb.Append(Weekly.Value.EveryNWeeks == 1
                    ? "Run every week"
                    : $"Run every {Weekly.Value.EveryNWeeks} weeks");
                sb.Append(" on ");

                var days = new List<string>(7);
                if ((Weekly.Value.DaysOfWeek & DayOfWeekFlag.Monday) == DayOfWeekFlag.Monday) days.Add("Monday");
                if ((Weekly.Value.DaysOfWeek & DayOfWeekFlag.Tuesday) == DayOfWeekFlag.Tuesday) days.Add("Tuesday");
                if ((Weekly.Value.DaysOfWeek & DayOfWeekFlag.Wednesday) == DayOfWeekFlag.Wednesday) days.Add("Wednesday");
                if ((Weekly.Value.DaysOfWeek & DayOfWeekFlag.Thursday) == DayOfWeekFlag.Thursday) days.Add("Thursday");
                if ((Weekly.Value.DaysOfWeek & DayOfWeekFlag.Friday) == DayOfWeekFlag.Friday) days.Add("Friday");
                if ((Weekly.Value.DaysOfWeek & DayOfWeekFlag.Saturday) == DayOfWeekFlag.Saturday) days.Add("Saturday");
                if ((Weekly.Value.DaysOfWeek & DayOfWeekFlag.Sunday) == DayOfWeekFlag.Sunday) days.Add("Sunday");

                if (days.Count > 1)
                {
                    sb.Append(Join(", ", days.Take(days.Count - 1).ToArray()));
                    sb.Append(days.Count - 1 > 1 ? ", and " : " and ");
                    sb.Append(days[days.Count - 1]);
                }
                else
                {
                    sb.Append(days[0]);
                }
            }
            else if (Monthly.HasValue)
            {
                if (Monthly.Value.DayOfMonth.HasValue)
                {
                    sb.Append($"Run every {Monthly.Value.DayOfMonth.Value} day of the month");
                }
                else if (Monthly.Value.DayOfWeek.HasValue)
                {
                    sb.Append("Run the ");
                    switch (Monthly.Value.DayOfWeek.Value.NthMonthlyWeek)
                    {
                        case WeekOfMonth.First: sb.Append("first"); break;
                        case WeekOfMonth.Second: sb.Append("second"); break;
                        case WeekOfMonth.Third: sb.Append("third"); break;
                        case WeekOfMonth.Fourth: sb.Append("fourth"); break;
                        case WeekOfMonth.Last: sb.Append("last"); break;
                    }
                    sb.Append(" week of the month on ");
                    sb.Append(Monthly.Value.DayOfWeek.Value.DayOfWeek);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }

            sb.Append(", starting on " + StartDateTime.ToShortDateString());
            sb.Append(" at " + StartDateTime.ToShortTimeString());

            if (!RepeatTask) return sb.ToString();
            if (Every.HasValue)
            {
                sb.Append(", repeating every ");
                if (Every.Value.Number == 1)
                {
                    sb.Append(Every.Value.HoursOrMinutes == HoursOrMinutes.Minutes ? "minute" : "hour");
                }
                else
                {
                    sb.Append(Every.Value.HoursOrMinutes == HoursOrMinutes.Minutes
                        ? $"{Every.Value.Number} minutes"
                        : $"{Every.Value.Number} hours");
                }
            }

            if (!Until.HasValue) return sb.ToString();
            if (Until.Value.Duration.HasValue)
            {
                sb.Append(" for ");
                sb.Append(Until.Value.Duration.Value);
            }
            else if (Until.Value.Time.HasValue)
            {
                sb.Append(" until ");
                sb.Append(Until.Value.Time.Value.ToShortDateString());
            }
            else
            {
                throw new InvalidOperationException();
            }

            return sb.ToString();
        }
    }
}