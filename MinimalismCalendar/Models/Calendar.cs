using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.Models
{
    /// <summary>
    /// Represents a calendar.
    /// </summary>
    public class Calendar
    {
        /// <summary>
        /// The assigned ID of the calendar.
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// The user-given name of the calendar.
        /// </summary>
        public string Name { get; set; }
    }
}
