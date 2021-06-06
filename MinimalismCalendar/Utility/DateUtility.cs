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
    }
}
