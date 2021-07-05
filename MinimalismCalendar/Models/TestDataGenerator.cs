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
                    Start = new DateTime(2021,7,4,10,0,0),
                    End = new DateTime(2021,7,4,12,0,0)
                },
                new CalendarEvent()
                {
                    Name = "Second Event",
                    Start = new DateTime(2021,7,6,13,30,0),
                    End = new DateTime(2021,7,6,14,0,0)
                },
                new CalendarEvent()
                {
                    Name = "Third Event",
                    Start = new DateTime(2021,7,9,18,0,0),
                    End = new DateTime(2021,7,9,22,0,0)
                }
            };
        }
    }
}
