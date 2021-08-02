using MinimalismCalendar.Managers;
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
        /// The list of accounts the user has connected with the app.
        /// </summary>
        private List<CalendarProviderAccount> Accounts { get; set; }
        /// <summary>
        /// The list of calendars the user has set to be hidden.
        /// </summary>
        private Dictionary<string, List<HiddenCalendarRecord>> HiddenCalendars { get; set; }
        #endregion

        #region Constructors
        public AppConfig()
        {
            this.Accounts = null;
            this.HiddenCalendars = null;
        }
        #endregion

        #region Methods - Accounts
        /// <summary>
        /// Gets a list of accounts the user has connected with the app.
        /// </summary>
        /// <returns></returns>
        public async Task<List<CalendarProviderAccount>> GetAccountsAsync()
        {
            // Initialize the list if it is uninitialized.
            if (this.Accounts == null)
            {
                this.Accounts = await this.InitializeAccountsAsync();
            }

            return new List<CalendarProviderAccount>(this.Accounts);
        }

        /// <summary>
        /// Adds the given account to the list of accounts.
        /// </summary>
        /// <param name="account">The account to add to the list of accounts.</param>
        /// <returns></returns>
        public async Task AddAccountAsync(CalendarProviderAccount account)
        {
            // Initialize the list if it is uninitialized.
            if (this.Accounts == null)
            {
                this.Accounts = await this.InitializeAccountsAsync();
            }

            this.Accounts.Add(account);
        }

        /// <summary>
        /// Removes the account with the given ID from the list of accounts and clears any associated data.
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task RemoveAccountAsync(string accountId)
        {
            // Initialize the list if it is uninitialized.
            if (this.Accounts == null)
            {
                this.Accounts = await this.InitializeAccountsAsync();
            }

            CalendarProviderAccount account = this.Accounts.Where(a => a.ID == accountId).FirstOrDefault();
            if (account != null)
            {
                this.Accounts.Remove(account);
            }

            // Clear any calendars associated with this account.
            await this.RemoveCalendarsAsync(accountId);
        }

        /// <summary>
        /// Gets the provider-assigned ID for a account, given the app-assigned account ID.
        /// </summary>
        /// <param name="accountId">The unique ID of the account given locally by the app.</param>
        /// <returns></returns>
        public async Task<string> GetProviderAccountId(string accountId)
        {
            // Initialize the list if it is uninitialized.
            if (this.Accounts == null)
            {
                this.Accounts = await this.InitializeAccountsAsync();
            }

            CalendarProviderAccount account = this.Accounts.Where(a => a.ID == accountId).FirstOrDefault();

            return account != null ? account.ProviderGivenID : "";
        }
        #endregion

        #region Methods - Hidden Calendars
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

            // Construct the list of all hidden calendars.
            List<HiddenCalendarRecord> allRecords = new List<HiddenCalendarRecord>();
            foreach (List<HiddenCalendarRecord> recordsForAccount in this.HiddenCalendars.Values)
            {
                allRecords.AddRange(recordsForAccount);
            }

            // Return the list of hidden calendars.
            return allRecords;
        }

        public async Task<bool> IsCalendarHiddenAsync(string accountId, string calendarId)
        {
            // Initialize the list if it is uninitialized.
            if (this.HiddenCalendars == null)
            {
                this.HiddenCalendars = await this.InitializeHiddenCalendarsAsync();
            }

            bool isHidden = false;
            if (this.HiddenCalendars.ContainsKey(accountId) && this.HiddenCalendars[accountId] != null)
            {
                isHidden = this.HiddenCalendars[accountId].Where(c => c.CalendarID == calendarId).FirstOrDefault() != null;
            }

            return isHidden;
        }

        /// <summary>
        /// Adds the given calendar to a list of hidden calendars.
        /// </summary>
        /// <param name="calendar">The calendar to add to the list of hidden calendars.</param>
        public void AddHiddenCalendar(Calendar calendar)
        {
            if (!this.HiddenCalendars.ContainsKey(calendar.AccountID))
            {
                this.HiddenCalendars.Add(calendar.AccountID, new List<HiddenCalendarRecord>());
            }

            // Create a new calendar record and add it to the list.
            this.HiddenCalendars[calendar.AccountID].Add(new HiddenCalendarRecord()
            {
                CalendarID = calendar.ID,
                CalendarName = calendar.Name
            });
        }

        /// <summary>
        /// Removes the given calendar from the list of hidden calendars.
        /// </summary>
        /// <param name="calendar">The calendar to remove from the list of hidden calendars.</param>
        /// <returns>True if the given calendar was removed, false otherwise.</returns>
        public bool RemoveHiddenCalendar(Calendar calendar)
        {
            bool removed = false;

            // Try to get a reference to the record of this calendar.
            HiddenCalendarRecord record = this.HiddenCalendars[calendar.AccountID].Where(r => r.CalendarName == calendar.Name).FirstOrDefault();

            // If the calendar is in the list of hidden calendars, remove it.
            if (record != null)
            {
                this.HiddenCalendars[calendar.AccountID].Remove(record);
                removed = true;
            }

            return removed;
        }

        /// <summary>
        /// Removes all calendars for the given account.
        /// <paramref name="accountId">The account ID to remove all calendars for.</paramref>
        /// </summary>
        public async Task RemoveCalendarsAsync(string accountId)
        {
            // Initialize the list if it is uninitialized.
            if (this.HiddenCalendars == null)
            {
                this.HiddenCalendars = await this.InitializeHiddenCalendarsAsync();
            }

            this.HiddenCalendars.Remove(accountId);
        }

        /// <summary>
        /// Saves all config contents to a config file.
        /// </summary>
        /// <returns></returns>
        public async Task SaveAsync()
        {
            // There are no accounts to save.
            if (this.Accounts == null)
            {
                return;
            }

            // For each account in the list of accounts...
            JsonArray accountsArray = new JsonArray();
            foreach (CalendarProviderAccount account in this.Accounts)
            {
                // Serialize the account data.
                JsonObject accountJson = CalendarProviderAccountManager.Serialize(account);

                // Add the array of calendar data to the serialized account data.
                JsonArray calsArray = this.SerializeHiddenCalendarsForAccount(account.ID);
                accountJson.Add("hidden_calendars", calsArray);

                // Add the account to array.
                accountsArray.Add(accountJson);
            }

            // Store the array in a json object.
            JsonObject jsonObject = new JsonObject();
            jsonObject.Add("accounts", accountsArray);

            // Get a reference to the file, and create it if it does not exist.
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(AppConfig.ConfigFileName, CreationCollisionOption.ReplaceExisting);

            // Save the json object to the file.
            await FileIO.WriteTextAsync(file, jsonObject.Stringify());
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Initializes the list of accounts the user has connected with the app from the stored data.
        /// </summary>
        /// <returns>The list of hidden calendar records, or an empty list if there is a file error.</returns>
        private async Task<List<CalendarProviderAccount>> InitializeAccountsAsync()
        {
            // Initialize the list.
            List<CalendarProviderAccount> accounts = new List<CalendarProviderAccount>();

            // Try to read the list from the file.
            IStorageItem storageItem = await ApplicationData.Current.LocalFolder.TryGetItemAsync(AppConfig.ConfigFileName);
            if (storageItem is StorageFile file)
            {
                // Read the data from the file.
                string fileContent = await FileIO.ReadTextAsync(file);

                // Parse the data from the file.
                JsonObject jsonObject = JsonObject.Parse(fileContent);
                JsonArray accountsArray = jsonObject["accounts"].GetArray();

                // For each account in the array...
                foreach (var accountJsonValue in accountsArray)
                {
                    // This is necessary because of the type iterated over in the JsonArray.
                    JsonObject accountJson = accountJsonValue.GetObject();

                    // Parse the account data.
                    CalendarProviderAccount account = CalendarProviderAccountManager.Deserialize(accountJson);

                    // Add a new hidden calendar record to the list.
                    accounts.Add(account);
                }
            }

            return accounts;
        }

        /// <summary>
        /// Initializes the list of hidden calendars from the stored data.
        /// </summary>
        /// <returns>The list of hidden calendar records, or an empty list if there is a file error.</returns>
        private async Task<Dictionary<string, List<HiddenCalendarRecord>>> InitializeHiddenCalendarsAsync()
        {
            // Initialize the collection.
            Dictionary<string, List<HiddenCalendarRecord>> recordsCollection = new Dictionary<string, List<HiddenCalendarRecord>>();

            // Try to read the list from the file.
            IStorageItem storageItem = await ApplicationData.Current.LocalFolder.TryGetItemAsync(AppConfig.ConfigFileName);
            if (storageItem is StorageFile file)
            {
                // Read the data from the file.
                string fileContent = await FileIO.ReadTextAsync(file);

                // Parse the data from the file.
                JsonObject jsonObject = JsonObject.Parse(fileContent);
                JsonArray accountsArray = jsonObject["accounts"].GetArray();

                // For each account in the list of accounts...
                foreach (var accountJsonValue in accountsArray)
                {
                    // Parse the account data.
                    JsonObject accountJson = accountJsonValue.GetObject();
                    CalendarProviderAccount account = CalendarProviderAccountManager.Deserialize(accountJson);

                    // Initialize the list for this account.
                    recordsCollection.Add(account.ID, new List<HiddenCalendarRecord>());

                    // If there are no hidden calendars, move on.
                    if (accountJson.ContainsKey("hidden_calendars") == false)
                    {
                        continue;
                    }

                    JsonArray calsArray = accountJson["hidden_calendars"].GetArray();

                    // For each hidden calendar record in the array...
                    foreach (var hiddenCalJsonValue in calsArray)
                    {
                        // This is necessary because of the type iterated over in the JsonArray.
                        JsonObject hiddenCalJson = hiddenCalJsonValue.GetObject();

                        // Parse the record data.
                        HiddenCalendarRecord record = HiddenCalendarRecordManager.Deserialize(hiddenCalJson);

                        // Add a new hidden calendar record to the list.
                        recordsCollection[account.ID].Add(record);
                    }
                }
            }

            return recordsCollection;
        }

        /// <summary>
        /// Serializes the hidden calendars for the given account into a JSON array.
        /// </summary>
        /// <param name="accountId">The unique ID of the account given locally by the app.</param>
        /// <returns></returns>
        private JsonArray SerializeHiddenCalendarsForAccount(string accountId)
        {
            // Return an empty array if there are no calendar records for the given account.
            if (this.HiddenCalendars == null ||
                this.HiddenCalendars.ContainsKey(accountId) == false ||
                this.HiddenCalendars[accountId] == null)
            {
                return new JsonArray();
            }

            // For each record in the list of hidden calendars...
            JsonArray calsArray = new JsonArray();
            foreach (HiddenCalendarRecord record in this.HiddenCalendars[accountId])
            {
                // Serialize the record and add it to the array.
                JsonObject recordJson = HiddenCalendarRecordManager.Serialize(record);
                calsArray.Add(recordJson);
            }

            return calsArray;
        }
        #endregion
    }
}
