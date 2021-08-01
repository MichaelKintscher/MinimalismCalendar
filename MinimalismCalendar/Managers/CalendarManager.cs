using MinimalismCalendar.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace MinimalismCalendar.Managers
{
    /// <summary>
    /// Contains methods for manipulating Calendar models.
    /// </summary>
    public static class CalendarManager
    {
        /// <summary>
        /// Converts a calendar to it's string representation.
        /// </summary>
        /// <param name="calendar"></param>
        /// <returns></returns>
        public static JsonObject ConvertToJson(Calendar calendar)
        {
            JsonObject jsonObject = new JsonObject();
            jsonObject.Add("id", JsonValue.CreateStringValue(calendar.ID));
            jsonObject.Add("account_id", JsonValue.CreateStringValue(calendar.AccountID));
            jsonObject.Add("name", JsonValue.CreateStringValue(calendar.Name));

            return jsonObject;
        }

        /// <summary>
        /// Converts the string representation of a calendar to a calendar.
        /// </summary>
        /// <param name="calendarString"></param>
        public static Calendar ParseCalendarJson(string calendarJsonString)
        {
            JsonObject jsonObject = JsonObject.Parse(calendarJsonString);
            string id = jsonObject["id"].GetString();
            string accountId = jsonObject["account_id"].GetString();
            string name = jsonObject["name"].GetString();

            return new Calendar()
            {
                ID = id,
                AccountID = accountId,
                Name = name
            };
        }

        /// <summary>
        /// Returns whether the given calendars are the same by value.
        /// </summary>
        /// <param name="a">The first calendar to compare.</param>
        /// <param name="b">The second calendar to compare.</param>
        /// <returns>Whether the two given calendars are the same by value.</returns>
        public static bool Equals(Calendar a, Calendar b)
        {
            return a.ID == b.ID &&
                    a.AccountID == b.AccountID;
        }
    }
}
