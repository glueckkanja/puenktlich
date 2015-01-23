using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Puenktlich
{
    /// <summary>
    ///     Provides a trigger defined by a cron expression.
    /// </summary>
    public class CronTrigger : Trigger
    {
        private static readonly DateTime OddEvenWeekRefDate = new DateTime(2001, 1, 1);

        private readonly List<int> _days = new List<int>();
        private readonly List<int> _hours = new List<int>();
        private readonly List<int> _minutes = new List<int>();
        private readonly List<int> _months = new List<int>();
        private readonly List<int> _seconds = new List<int>();
        private readonly List<int> _weekdays = new List<int>();

        private string _expression;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CronTrigger" /> class.
        /// </summary>
        /// <param name="expression">The cron expression.</param>
        /// <param name="targetTimeZone">The target timezone -or- <c>null</c> if no conversion is required.</param>
        public CronTrigger(string expression, TimeZoneInfo targetTimeZone = null)
        {
            _expression = expression;
            TargetTimeZone = targetTimeZone;

            ParseParts();
        }

        /// <summary>
        ///     Gets the cron expression.
        /// </summary>
        /// <value>The cron expression.</value>
        public override string Expression
        {
            get { return _expression; }
        }

        public TimeZoneInfo TargetTimeZone { get; private set; }

        /// <summary>
        ///     Gets the (possibly infinite) upcoming or previous occurrences of this trigger relative to
        ///     <paramref name="baseTime" />.
        /// </summary>
        /// <param name="baseTime">The base time at which to start the calculation.</param>
        /// <remarks>This methods supports years between 1 and 9999. :-)</remarks>
        /// <returns>
        ///     An (possibly infinite) enumerable of occurrences.
        /// </returns>
        public override IEnumerable<DateTimeOffset> GetUpcomingOccurrences(DateTimeOffset baseTime)
        {
            List<int> months = _months;
            List<int> days = _days;
            List<int> hours = _hours;
            List<int> minutes = _minutes;
            List<int> seconds = _seconds;

            Func<int, int, bool> isOutOfRange = (currentValue, baseTimeValue) => currentValue < baseTimeValue;

            for (int year = baseTime.Year; year <= 9999; year++)
            {
                foreach (int month in months)
                {
                    if (isOutOfRange(month, baseTime.Month) &&
                        year == baseTime.Year)
                        continue;

                    foreach (int day in days)
                    {
                        if (isOutOfRange(day, baseTime.Day) &&
                            year == baseTime.Year &&
                            month == baseTime.Month)
                            continue;

                        if (!IsValidDate(year, month, day))
                            continue;

                        foreach (int hour in hours)
                        {
                            if (isOutOfRange(hour, baseTime.Hour) &&
                                year == baseTime.Year &&
                                month == baseTime.Month &&
                                day == baseTime.Day)
                                continue;

                            foreach (int minute in minutes)
                            {
                                if (isOutOfRange(minute, baseTime.Minute) &&
                                    year == baseTime.Year &&
                                    month == baseTime.Month &&
                                    day == baseTime.Day &&
                                    hour == baseTime.Hour)
                                    continue;

                                foreach (int second in seconds)
                                {
                                    if (isOutOfRange(second, baseTime.Second) &&
                                        year == baseTime.Year &&
                                        month == baseTime.Month &&
                                        day == baseTime.Day &&
                                        hour == baseTime.Hour &&
                                        minute == baseTime.Minute)
                                        continue;

                                    var result = new DateTimeOffset(year, month, day, hour, minute, second,
                                        baseTime.Offset);

                                    if (result < baseTime)
                                    {
                                        continue;
                                    }

                                    DayOfWeek wd = result.DayOfWeek;
                                    int wdn = (int) wd + 1;

                                    if (_weekdays.Contains(wdn) ||
                                        (_weekdays.Contains(10 + wdn) && IsFirstDateInMonth(result, wd)) ||
                                        (_weekdays.Contains(20 + wdn) && IsLastDateInMonth(result, wd)) ||
                                        (_weekdays.Contains(30 + wdn) && IsOddWeek(result)) ||
                                        (_weekdays.Contains(40 + wdn) && !IsOddWeek(result)))
                                    {
                                        if (TargetTimeZone != null)
                                        {
                                            result = TimeZoneInfo.ConvertTime(result, TargetTimeZone);
                                        }

                                        yield return result;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Parses the parts of the cron expression and adds them to the internal collections.
        /// </summary>
        private void ParseParts()
        {
            const StringSplitOptions nonEmpty = StringSplitOptions.RemoveEmptyEntries;

            string[] parts = Expression.Split(new[] {' '}, nonEmpty);

            string secondsPart = parts[0].Trim();
            string minutesPart = parts[1].Trim();
            string hoursPart = parts[2].Trim();
            string daysPart = parts[3].Trim();
            string monthPart = parts[4].Trim();
            string weekdaysPart = parts[5].Trim();

            if (daysPart == "?") daysPart = "*";
            if (weekdaysPart == "?") weekdaysPart = "*";

            if (secondsPart.StartsWith("/")) secondsPart = "*" + secondsPart;
            if (minutesPart.StartsWith("/")) minutesPart = "*" + minutesPart;
            if (hoursPart.StartsWith("/")) hoursPart = "*" + hoursPart;
            if (daysPart.StartsWith("/")) daysPart = "*" + daysPart;
            if (weekdaysPart.StartsWith("/")) weekdaysPart = "*" + weekdaysPart;

            secondsPart = secondsPart.Replace("*", "0-59");
            minutesPart = minutesPart.Replace("*", "0-59");
            hoursPart = hoursPart.Replace("*", "0-23");
            daysPart = daysPart.Replace("*", "1-31");
            monthPart = monthPart.Replace("*", "1-12");
            weekdaysPart = weekdaysPart.Replace("*", "1-7");

            monthPart = monthPart.Replace("JAN", "1");
            monthPart = monthPart.Replace("FEB", "2");
            monthPart = monthPart.Replace("MAR", "3");
            monthPart = monthPart.Replace("APR", "4");
            monthPart = monthPart.Replace("MAY", "5");
            monthPart = monthPart.Replace("JUN", "6");
            monthPart = monthPart.Replace("JUL", "7");
            monthPart = monthPart.Replace("AUG", "8");
            monthPart = monthPart.Replace("SEP", "9");
            monthPart = monthPart.Replace("OCT", "10");
            monthPart = monthPart.Replace("NOV", "11");
            monthPart = monthPart.Replace("DEC", "12");

            weekdaysPart = weekdaysPart.Replace("SUN", "1");
            weekdaysPart = weekdaysPart.Replace("MON", "2");
            weekdaysPart = weekdaysPart.Replace("TUE", "3");
            weekdaysPart = weekdaysPart.Replace("WED", "4");
            weekdaysPart = weekdaysPart.Replace("THU", "5");
            weekdaysPart = weekdaysPart.Replace("FRI", "6");
            weekdaysPart = weekdaysPart.Replace("SAT", "7");

            string[] secondsRanges = secondsPart.Split(new[] {','}, nonEmpty);
            string[] minutesRanges = minutesPart.Split(new[] {','}, nonEmpty);
            string[] hoursRanges = hoursPart.Split(new[] {','}, nonEmpty);
            string[] daysRanges = daysPart.Split(new[] {','}, nonEmpty);
            string[] monthsRanges = monthPart.Split(new[] {','}, nonEmpty);
            string[] weekdaysRanges = weekdaysPart.Split(new[] {','}, nonEmpty);

            foreach (string range in secondsRanges)
                _seconds.AddRange(ParseNumericalRange(range).Where(x => x >= 0 && x <= 59));

            foreach (string range in minutesRanges)
                _minutes.AddRange(ParseNumericalRange(range).Where(x => x >= 0 && x <= 59));

            foreach (string range in hoursRanges)
                _hours.AddRange(ParseNumericalRange(range).Where(x => x >= 0 && x <= 23));

            foreach (string range in daysRanges)
                _days.AddRange(ParseNumericalRange(range).Where(x => x >= 1 && x <= 31));

            foreach (string range in monthsRanges)
                _months.AddRange(ParseNumericalRange(range).Where(x => x >= 1 && x <= 12));

            foreach (string range in weekdaysRanges)
                _weekdays.AddRange(ParseNumericalRange(range).Where(
                    x => (x >= 1 && x <= 7) ||
                         (x >= 11 && x <= 17) ||
                         (x >= 21 && x <= 27) ||
                         (x >= 31 && x <= 37) ||
                         (x >= 41 && x <= 47)));
        }

        /// <summary>
        ///     Parses a numerical cron range.
        /// </summary>
        /// <param name="cronRangeExpression">The partial cron range expression.</param>
        /// <returns>
        ///     The contents of this range, as an enumerable of <see cref="int" />.
        /// </returns>
        private static IEnumerable<int> ParseNumericalRange(string cronRangeExpression)
        {
            const StringSplitOptions nonEmpty = StringSplitOptions.RemoveEmptyEntries;

            string expr = cronRangeExpression;

            if (expr.Contains("-"))
            {
                Match match = Regex.Match(expr, @"(\d+)-(\d+)");
                int start = Convert.ToInt32(match.Groups[1].Value);
                int end = Convert.ToInt32(match.Groups[2].Value);

                string newExpr = string.Join(",", Enumerable.Range(start, end - start + 1));

                expr = Regex.Replace(expr, @"(\d+)-(\d+)", newExpr);
            }

            if (expr.Contains("/"))
            {
                Match match = Regex.Match(expr, @"(.+)/(\d+)");
                int[] values = match
                    .Groups[1]
                    .Value
                    .Split(new[] {','}, nonEmpty)
                    .Select(x => Convert.ToInt32(x))
                    .ToArray();
                int interval = Convert.ToInt32(match.Groups[2].Value);
                int min = values.Min();
                int max = values.Max();

                if (values.Length == 1)
                    max = 59;

                var v = new List<int>();
                int n = min;

                do
                {
                    v.Add(n);
                    n += interval;
                } while (n <= max);

                expr = string.Join(",", v);
            }

            return expr
                .Split(new[] {','}, nonEmpty)
                .Select(x =>
                {
                    int add = 0;

                    // first (weekday name) of the month
                    if (x.EndsWith("F"))
                    {
                        add = 10;
                        x = x.TrimEnd('F');
                    }
                        // last (weekday name) of the month
                    else if (x.EndsWith("L"))
                    {
                        add = 20;
                        x = x.TrimEnd('L');
                    }
                        // only in odd weeks (see IsOddWeek below)
                    else if (x.EndsWith("O"))
                    {
                        add = 30;
                        x = x.TrimEnd('O');
                    }
                        // only in even weeks (see IsOddWeek below)
                    else if (x.EndsWith("E"))
                    {
                        add = 40;
                        x = x.TrimEnd('E');
                    }

                    return add + Convert.ToInt32(x);
                })
                .OrderBy(x => x);
        }

        private static bool IsCronExpression(string expression)
        {
            return Regex.IsMatch(expression, @"^([\*\?\-,/0-9A-Za-z]+( +|$)){6}$");
        }

        public static bool TryParse(string expression, out CronTrigger trigger)
        {
            if (!IsCronExpression(expression))
            {
                trigger = null;
                return false;
            }

            try
            {
                trigger = new CronTrigger(expression);
                return true;
            }
            catch
            {
                trigger = null;
                return false;
            }
        }

        public override string ToString()
        {
            return Expression;
        }

        private static bool IsValidDate(int year, int month, int day)
        {
            return year >= 1 && year <= 9999 && month >= 1 && month <= 12 && day >= 1 &&
                   day <= DateTime.DaysInMonth(year, month);
        }

        private static DateTimeOffset GetFirstDateInMonth(DateTimeOffset time)
        {
            return time.AddDays(1 - time.Day);
        }

        private static DateTimeOffset GetFirstDateInMonth(DateTimeOffset time, DayOfWeek wd)
        {
            DateTimeOffset first = GetFirstDateInMonth(time);

            while (first.DayOfWeek != wd)
            {
                first = first.AddDays(+1);
            }

            return first;
        }

        private static DateTimeOffset GetLastDateInMonth(DateTimeOffset time)
        {
            DateTimeOffset lastDayInMonth = time.AddMonths(1);
            return lastDayInMonth.AddDays(-lastDayInMonth.Day);
        }

        private static DateTimeOffset GetLastDateInMonth(DateTimeOffset time, DayOfWeek wd)
        {
            DateTimeOffset last = GetLastDateInMonth(time);

            while (last.DayOfWeek != wd)
            {
                last = last.AddDays(-1);
            }

            return last;
        }

        private static bool IsFirstDateInMonth(DateTimeOffset time, DayOfWeek wd)
        {
            DateTimeOffset first = GetFirstDateInMonth(time, wd);
            return time.Year == first.Year && time.Month == first.Month && time.Day == first.Day;
        }

        private static bool IsLastDateInMonth(DateTimeOffset time, DayOfWeek wd)
        {
            DateTimeOffset last = GetLastDateInMonth(time, wd);
            return time.Year == last.Year && time.Month == last.Month && time.Day == last.Day;
        }

        // week starting 2001-01-01 (Monday) is referenced as first week (odd)

        private static bool IsOddWeek(DateTimeOffset time)
        {
            double numDays = (time.Date - OddEvenWeekRefDate).TotalDays%14;

            return (numDays >= 0 && numDays < 7) || numDays < -7;
        }
    }
}