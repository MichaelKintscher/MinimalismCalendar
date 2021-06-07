using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.Models
{
    /// <summary>
    /// Represents an event on a calendar.
    /// </summary>
    public class CalendarEvent
    {
        /// <summary>
        /// The friendly name of the event.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The start time and date of the event.
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// The end time and date of the event.
        /// </summary>
        public DateTime End { get; set; }
    }
}
