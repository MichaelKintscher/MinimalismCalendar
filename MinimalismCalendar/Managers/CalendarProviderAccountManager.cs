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
    /// Contains methods for manipulating CalendarProviderAccount models.
    /// </summary>
    public static class CalendarProviderAccountManager
    {
        /// <summary>
        /// Deserializes an account json object into the corresponding model.
        /// </summary>
        /// <param name="accountJson">The json object representing the serialized account data.</param>
        /// <returns>The account represented by the json object.</returns>
        public static CalendarProviderAccount Deserialize(JsonObject accountJson)
        {
            string id = accountJson["id"].GetString();
            CalendarProvider provider = Enum.Parse<CalendarProvider>(accountJson["provider"].GetString());
            string providerGivenId = accountJson["provider_given_id"].GetString();
            string friendlyName = accountJson["friendly_name"].GetString();
            string username = accountJson["username"].GetString();
            string pictureUri = accountJson["picture_uri"].GetString();
            string pictureLocalUri = accountJson["picture_local_uri"].GetString();
            DateTime lastSynced = DateTime.Parse(accountJson["last_synced"].GetString());

            // Create a new instance of the object.
            return new CalendarProviderAccount()
            {
                ID = id,
                Provider = provider,
                ProviderGivenID = providerGivenId,
                FriendlyName = friendlyName,
                Username = username,
                PictureUri = pictureUri,
                PictureLocalUri = pictureLocalUri,
                Connected = false,                  // This is initialized to false.
                LastSynced = lastSynced
            };
        }

        /// <summary>
        /// Serializes a CalendarProviderAccount to JSON.
        /// </summary>
        /// <param name="account">The account to serialize.</param>
        /// <returns>A json object representing the serialized account data.</returns>
        public static JsonObject Serialize(CalendarProviderAccount account)
        {
            JsonObject jsonObject = new JsonObject();
            jsonObject.Add("id", JsonValue.CreateStringValue(account.ID));
            jsonObject.Add("provider", JsonValue.CreateStringValue(account.Provider.ToString()));
            jsonObject.Add("provider_given_id", JsonValue.CreateStringValue(account.ProviderGivenID));
            jsonObject.Add("friendly_name", JsonValue.CreateStringValue(account.FriendlyName));
            jsonObject.Add("username", JsonValue.CreateStringValue(account.Username));
            jsonObject.Add("picture_uri", JsonValue.CreateStringValue(account.PictureUri));
            jsonObject.Add("picture_local_uri", JsonValue.CreateStringValue(account.PictureLocalUri));
            jsonObject.Add("last_synced", JsonValue.CreateStringValue(account.LastSynced.ToString()));

            return jsonObject;
        }
    }
}
