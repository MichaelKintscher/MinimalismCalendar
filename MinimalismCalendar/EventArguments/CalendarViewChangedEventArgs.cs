using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.EventArguments
{
    /// <summary>
    /// Contains event info for when the calendar view changes.
    /// </summary>
    public class CalendarViewChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The date of the Sunday now visible in the calendar control.
        /// </summary>
        public DateTime NewSunday { get; private set; }

        public CalendarViewChangedEventArgs(DateTime newSunday)
        {
            this.NewSunday = newSunday;
        }
    }
}
