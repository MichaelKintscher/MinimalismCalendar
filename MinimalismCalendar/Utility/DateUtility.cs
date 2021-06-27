using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.Utility
{
    /// <summary>
    /// Contains methods for date calculations.
    /// </summary>
    public static class DateUtility
    {
        /// <summary>
        /// Gets the date of the Sunday preceeding the given date.
        /// </summary>
        /// <param name="date">The date to find the Sunday prior to.</param>
        /// <returns>The date of the Sunday preceding the given date.</returns>
        public static DateTime GetPreviousSunday(DateTime date)
        {
            return date.AddDays(-(int)date.DayOfWeek);
        }

        /// <summary>
        /// Gets the date of the Saturday following the given date.
        /// </summary>
        /// <param name="date">The date to find the Saturday following.</param>
        /// <returns>The date of the Saturday following the given date.</returns>
        public static DateTime GetNextSaturday(DateTime date)
        {
            return date.AddDays(6 - (int)date.DayOfWeek);
        }

        /// <summary>
        /// Gets a list of the calendar month names.
        /// </summary>
        /// <returns>A list of the calendar month names.</returns>
        public static List<string> GetMonthNames()
        {
            return new List<string>()
            {
                "January",
                "February",
                "March",
                "April",
                "May",
                "June",
                "July",
                "August",
                "September",
                "October",
                "November",
                "December"
            };
        }

        /// <summary>
        /// Gets the calendar month name of the given month number.
        /// </summary>
        /// <param name="month">The number of the month to get the name of. Must be a value between 1 and 12.</param>
        /// <returns></returns>
        public static string GetMonthName(int month)
        {
            // Throw an exception for any values outside of the acceptable range.
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException("month", "Month must be a value between 1 and 12.");
            }

            // The array is zero indexed, so one must be substracted from the momth number.
            return DateUtility.GetMonthNames()[month - 1];
        }
    }
}
