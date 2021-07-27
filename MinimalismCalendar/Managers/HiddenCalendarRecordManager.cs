using MinimalismCalendar.Models.AppConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace MinimalismCalendar.Managers
{
    /// <summary>
    /// Contains methods for manipulating HiddenCalendarRecord models.
    /// </summary>
    public static class HiddenCalendarRecordManager
    {
        /// <summary>
        /// Deserializes a record json object into the corresponding model.
        /// </summary>
        /// <param name="recordJson">The Json object representing the serialized record.</param>
        /// <returns>The record represented by the Json object.</returns>
        public static HiddenCalendarRecord Deserialize(JsonObject recordJson)
        {
            // Parse the record data.
            string name = recordJson["name"].GetString();

            // Add a new hidden calendar record to the list.
            return new HiddenCalendarRecord()
            {
                CalendarName = name
            };
        }

        /// <summary>
        /// Serializes a HiddenCalendarRecord to JSON.
        /// </summary>
        /// <param name="record">The record to serlialize.</param>
        /// <returns>a Json object representing the serialized record.</returns>
        public static JsonObject Serialize(HiddenCalendarRecord record)
        {
            JsonObject recordJson = new JsonObject();
            recordJson.Add("name", JsonValue.CreateStringValue(record.CalendarName));

            return recordJson;
        }
    }
}
