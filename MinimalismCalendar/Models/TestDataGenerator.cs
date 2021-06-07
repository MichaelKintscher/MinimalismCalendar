using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.Models
{
    /// <summary>
    /// A class with methods for generating test data for testing app features.
    /// </summary>
    public static class TestDataGenerator
    {
        /// <summary>
        /// Returns a list of calendar events, each occuring within a single day.
        /// </summary>
        /// <returns>A list of calendar events.</returns>
        public static List<CalendarEvent> GetTestEvents()
        {
            return new List<CalendarEvent>()
            {
                new CalendarEvent()
                {
                    Name = "First Event",
                    Start = new DateTime(2021,6,6,10,0,0),
                    End = new DateTime(2021,6,6,12,0,0)
                },
                new CalendarEvent()
                {
                    Name = "First Event",
                    Start = new DateTime(2021,6,8,1,30,0),
                    End = new DateTime(2021,6,8,14,0,0)
                },
                new CalendarEvent()
                {
                    Name = "First Event",
                    Start = new DateTime(2021,6,11,10,0,0),
                    End = new DateTime(2021,6,11,12,0,0)
                }
            };
        }
    }
}
