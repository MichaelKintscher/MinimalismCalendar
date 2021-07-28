using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.EventArguments
{
    /// <summary>
    /// Contains event info for when a calendar's visibility changes.
    /// </summary>
    public class CalendarVisibilityChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The name of the calendar that changed visibility.
        /// </summary>
        public string CalendarName { get; private set; }
        /// <summary>
        /// The new visibility of the calendar.
        /// </summary>
        public CalendarVisibility Visibility { get; private set; }

        public CalendarVisibilityChangedEventArgs(string calendarName, CalendarVisibility newVisibility)
        {
            this.CalendarName = calendarName;
            this.Visibility = newVisibility;
        }
    }

    public enum CalendarVisibility
    {
        Visible,
        Hidden
    }
}
