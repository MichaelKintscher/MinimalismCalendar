﻿using MinimalismCalendar.Managers;
using MinimalismCalendar.Models.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;

namespace MinimalismCalendar.Models.AppConfigModels
{
    /// <summary>
    /// Represents configuration data about the app.
    /// </summary>
    public class AppConfig : Singleton<AppConfig>
    {
        #region Constants
        private static readonly string ConfigFileName = "CalendarConfig.json";
        #endregion

        #region Properties
        /// <summary>
        /// The list of calendars the user has set to be hidden.
        /// </summary>
        private List<HiddenCalendarRecord> HiddenCalendars { get; set; }
        #endregion

        #region Constructors
        public AppConfig()
        {
            this.HiddenCalendars = null;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the list of calendars the user has set to be hidden.
        /// </summary>
        /// <returns>A list of records containing info about the calendars the user has set to be hidden.</returns>
        public async Task<List<HiddenCalendarRecord>> GetHiddenCalendarsAsync()
        {
            // Initialize the list if it is uninitialized.
            if (this.HiddenCalendars == null)
            {
                this.HiddenCalendars = await this.InitializeHiddenCalendarsAsync();
            }

            // Return the list of hidden calendars.
            return new List<HiddenCalendarRecord>(this.HiddenCalendars);
        }

        /// <summary>
        /// Saves to a config file the list of calendars the user has set to be hidden.
        /// </summary>
        /// <returns></returns>
        public async Task SaveHiddenCalendarsAsync()
        {
            // For each record in the list of hidden calendars...
            JsonArray calsArray = new JsonArray();
            foreach(HiddenCalendarRecord record in this.HiddenCalendars)
            {
                // Serialize the record and add it to the array.
                JsonObject recordJson = HiddenCalendarRecordManager.Serialize(record);
                calsArray.Add(recordJson);
            }

            // Store the array in a json object.
            JsonObject jsonObject = new JsonObject();
            jsonObject.Add("hidden_calenders", calsArray);

            // Get a reference to the file, and create it if it does not exist.
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(AppConfig.ConfigFileName, CreationCollisionOption.ReplaceExisting);

            // Save the json object to the file.
            await FileIO.WriteTextAsync(file, jsonObject.Stringify());
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Initializes the list of hidden calendars from the stored data.
        /// </summary>
        /// <returns>The list of hidden calendar records, or an empty list if there is a file error.</returns>
        private async Task<List<HiddenCalendarRecord>> InitializeHiddenCalendarsAsync()
        {
            // Initialize the list.
            List<HiddenCalendarRecord> hiddenCalendars = new List<HiddenCalendarRecord>();

            // Try to read the list from the file.
            IStorageItem storageItem = await ApplicationData.Current.LocalFolder.TryGetItemAsync(AppConfig.ConfigFileName);
            if (storageItem is StorageFile file)
            {
                // Read the data from the file.
                string fileContent = await FileIO.ReadTextAsync(file);

                // Parse the data from the file.
                JsonObject jsonObject = JsonObject.Parse(fileContent);
                JsonArray calsArray = jsonObject["hidden_calenders"].GetArray();

                // For each hidden calendar record in the array...
                foreach(var hiddenCalJsonValue in calsArray)
                {
                    // This is necessary because of the type iterated over in the JsonArray.
                    JsonObject hiddenCalJson = hiddenCalJsonValue.GetObject();

                    // Parse the record data.
                    HiddenCalendarRecord record = HiddenCalendarRecordManager.Deserialize(hiddenCalJson);

                    // Add a new hidden calendar record to the list.
                    hiddenCalendars.Add(record);
                }
            }

            return hiddenCalendars;
        }
        #endregion
    }
}
