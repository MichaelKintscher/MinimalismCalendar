using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.Models.AppConfigModels
{
    /// <summary>
    /// Represents a record of a calendar the user has hidden.
    /// </summary>
    public class HiddenCalendarRecord
    {
        /// <summary>
        /// The ID of the hidden calendar.
        /// </summary>
        public string CalendarID { get; set; }
        /// <summary>
        /// The name of the hidden calendar.
        /// </summary>
        public string CalendarName { get; set; }
    }
}
