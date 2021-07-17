using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MinimalismCalendar.Controllers;
using MinimalismCalendar.EventArguments;
using MinimalismCalendar.Exceptions;
using MinimalismCalendar.Models.ApiModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Web.Http;
using Xamarin.Essentials;

namespace MinimalismCalendar.Models.GoogleCalendar
{
    /// <summary>
    /// Wrapper class for interfacing with the Google Calendar API. This class
    /// encapsulates all external dependencies on the Google Calendar v3 API.
    /// </summary>
    public class GoogleCalendarAPI : OAuthApi<GoogleCalendarAPI>
    {
        #region Constants
        /// <summary>
        /// The file path the API credentials are stored in.
        /// </summary>
        private static readonly string credentialsFilePath = "Assets/Config/credentials.json";
        /// <summary>
        /// The scopes within the API the app is accessing.
        /// </summary>
        private static readonly string[] scopes = { CalendarService.Scope.CalendarReadonly };
        /// <summary>
        /// The name of the application to present to the API.
        /// </summary>
        private static readonly string applicationName = "Minimalism Calendar";
        /// <summary>
        /// The base uri for Google OAuth 2.0
        /// </summary>
        public static readonly string oauthBaseUri = "https://accounts.google.com/o/oauth2/v2/auth";
        /// <summary>
        /// The redirect uri for the OAuth 2.0 flow. This is the special value Google uses to indicate a manual redirect.
        /// See: https://developers.google.com/identity/protocols/oauth2/native-app
        /// </summary>
        public static readonly string oauthRedirectUri = "urn:ietf:wg:oauth:2.0:oob";
        /// <summary>
        /// The endpoint uri for exchanging the authorization code for an access token.
        /// </summary>
        private static readonly string oauthTokenEndpoint = "https://oauth2.googleapis.com/token";
        #endregion

        #region Properties
        /// <summary>
        /// The file name of the file stonring the Google Calendar API OAuth token.
        /// </summary>
        private string TokenFileName
        {
            get => this.Name + "_token.json";
        }
        #endregion

        #region Constructors
        public GoogleCalendarAPI()
            : base(GoogleCalendarAPI.oauthBaseUri, GoogleCalendarAPI.oauthTokenEndpoint, GoogleCalendarAPI.oauthRedirectUri, GoogleCalendarAPI.credentialsFilePath,
                        GoogleCalendarAPI.LoadApiCredentials, GoogleCalendarAPI.GetOAuthQueryString, GoogleCalendarAPI.GetTokenExchangeParams,
                        GoogleCalendarAPI.ConvertResponseToToken, GoogleCalendarAPI.GetTokenRefreshParams, GoogleCalendarAPI.ConvertTokenToJsonString)
        {
            // Initalize API properties.
            this.Name = "Google Calendar";

            this.InitializeTokenDataAsync(this.TokenFileName);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Saves the authorization data for the current connection to the Google Calendar API to a token file
        ///     at the authTokenFilePath location.
        /// </summary>
        /// <returns></returns>
        public async Task SaveConnectionDataAsync()
        {
            await this.SaveConnectionDataAsync(this.TokenFileName);
        }

        /// <summary>
        /// Gets a list of calendars for the authorized user.
        /// See: https://developers.google.com/calendar/api/v3/reference/calendars
        /// </summary>
        /// <returns>A list of calendars</returns>
        public async Task<List<Calendar>> GetCalendarsAsync()
        {
            string uri = "https://www.googleapis.com/calendar/v3/users/me/calendarList";

            // Make a GET request to the calendar list endpoint.
            string calendarListResponseContent = await this.GetAsync(uri);

            // Convert the response content to a list of calendars.
            return this.GetCalendarsFromCalendarListResponse(calendarListResponseContent);
        }

        /// <summary>
        /// Gets a list of events on the authorized user's primary calendar.
        /// See: https://developers.google.com/calendar/api/v3/reference/events/list
        /// </summary>
        /// <returns>A list of calendar events</returns>
        public async Task<List<CalendarEvent>> GetCalendarEventsAsync()
        {
            string uri = "https://www.googleapis.com/calendar/v3/calendars/primary/events";

            // No exceptions were thrown, so parse the response message.
            string responseContent = await this.GetAsync(uri);

            // Convert the response content to a list of events.
            return this.GetEventsFromResponse(responseContent);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Loads the client app's credentials for the Google API from the file they are stored in.
        /// </summary>
        /// <returns>The client app's credentials for the Google API.</returns>
        private static ApiCredential LoadApiCredentials()
        {
            using (var stream = new FileStream(GoogleCalendarAPI.credentialsFilePath, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string credentials = reader.ReadToEnd();

                    JsonObject credentialsJson = JsonObject.Parse(credentials);
                    credentialsJson = credentialsJson["installed"].GetObject();
                    string clientId = credentialsJson["client_id"].GetString();
                    string clientSecret = credentialsJson["client_secret"].GetString();

                    return new ApiCredential()
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    };
                }
            }
        }
        
        /// <summary>
        /// Gets the query string for a request to the Google OAuth endpoint.
        /// </summary>
        /// <param name="credentials">The client app's credentials for the Google API.</param>
        /// <returns>The query string for a request to the Google OAuth endpoint. DOES NOT include the base url for the endpoint.</returns>
        private static string GetOAuthQueryString(ApiCredential credentials)
        {
            // Build the scopes string as a space deliminated list, per the requirement
            //      specified here: https://developers.google.com/identity/protocols/oauth2/native-app#uwp
            StringBuilder scopesString = new StringBuilder();
            foreach( string scope in GoogleCalendarAPI.scopes)
            {
                scopesString.Append(scope);
                scopesString.Append(" ");
            }

            // Create the start URI
            string startUrl = "?client_id=" + credentials.ClientId +
                              "&redirect_uri=" + GoogleCalendarAPI.oauthRedirectUri +
                              "&response_type=code" +
                              "&scope=" + scopesString.ToString();
            return startUrl;
        }

        /// <summary>
        /// Gets the parameters for Google's OAuth 2.0 token exchange point.
        /// </summary>
        /// <param name="authorizationCode">The authorization code returned by the OAuth endpoint.</param>
        /// <param name="redirectUri">The redirect URI for the OAuth flow.</param>
        /// <param name="credentials">The client app's credentials for the Google API.</param>
        /// <returns>An IList of key value pairs representing the HTTP POST parameters.</returns>
        private static IList<KeyValuePair<string, string>> GetTokenExchangeParams(string authorizationCode, string redirectUri, ApiCredential credentials)
        {
            // Parameter names are documented at: https://developers.google.com/identity/protocols/oauth2/native-app#step-2:-send-a-request-to-googles-oauth-2.0-server
            return new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("client_id", credentials.ClientId),
                new KeyValuePair<string, string>("client_secret", credentials.ClientSecret),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            };
        }

        /// <summary>
        /// Converts the token response from the Google OAuth API to its corresponding OAuthToken object.
        /// </summary>
        /// <param name="responseContent">The string of the Json object to convert into a token response object.</param>
        /// <returns>The token response object containing the data in the given content string.</returns>
        protected static OAuthToken ConvertResponseToToken(string responseContent)
        {
            // Parse the response json content.
            // Response format is documented at: https://developers.google.com/identity/protocols/oauth2/native-app#handlingresponse
            JsonObject responseJson = JsonObject.Parse(responseContent);
            string token = responseJson.ContainsKey("access_token") ? responseJson["access_token"].GetString() : "";
            long expiresInSeconds = responseJson.ContainsKey("expires_in") ? (long)responseJson["expires_in"].GetNumber() : 0;
            string tokenType = responseJson.ContainsKey("token_type") ? responseJson["token_type"].GetString() : "";
            string scope = responseJson.ContainsKey("scope") ? responseJson["scope"].GetString() : "";
            string refreshToken = responseJson.ContainsKey("refresh_token") ? responseJson["refresh_token"].GetString() : "";

            // If the response content contains an issued time, use that. Otherwise, default to Utc now.
            //      This value does not exist from the actual response JSON from Google's API, so this
            //      is a new API response if the value does not exist, and thus the issued time should be
            //      set to now. Otherwise, the respose JSON is a saved copy, and the saved value should
            //      be used.
            DateTime issuedTime = responseJson.ContainsKey("issued_time") ? DateTime.Parse(responseJson["issued_time"].GetString()) : DateTime.UtcNow;

            // Create and return a new instance of the OAuthToken class.
            return new OAuthToken()
            {
                AccessToken = token,
                TokenType = tokenType,
                ExpiresInSeconds = expiresInSeconds,
                RefreshToken = refreshToken,
                Scope = scope,
                IdToken = "",
                IssuedUtc = issuedTime
            };
        }

        /// <summary>
        /// Gets the parameters for Google's OAuth 2.0 token refresh.
        /// </summary>
        /// <param name="refreshToken">The refresh token to be exchanged at the API endpoint for a new authorization token.</param>
        /// <param name="credentials">The client app's credentials for the Google API.</param>
        /// <returns>An IList of key value pairs representing the HTTP POST parameters.</returns>
        private static IList<KeyValuePair<string, string>> GetTokenRefreshParams(string refreshToken, ApiCredential credentials)
        {
            // Parameter names are documented at: https://developers.google.com/identity/protocols/oauth2/native-app#exchange-authorization-code
            return new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("client_id", credentials.ClientId),
                new KeyValuePair<string, string>("client_secret", credentials.ClientSecret),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token")
            };
        }

        /// <summary>
        /// Converts the token response object into a string containing a serialized Json representation of that object.
        /// </summary>
        /// <param name="tokenData">The response to serialize into a string.</param>
        /// <returns>The string containing a serialized Json representation of the given Token Response object.</returns>
        private static string ConvertTokenToJsonString(OAuthToken tokenData)
        {
            JsonObject jsonObject = new JsonObject();
            jsonObject.Add("access_token", JsonValue.CreateStringValue(tokenData.AccessToken));

            // Set the expiration time (lifespan of the token) to zero seconds if no value exists.
            double expirationTime = tokenData.ExpiresInSeconds.HasValue ? (double)tokenData.ExpiresInSeconds.Value : 0.0;
            jsonObject.Add("expires_in", JsonValue.CreateNumberValue(expirationTime));

            jsonObject.Add("token_type", JsonValue.CreateStringValue(tokenData.TokenType));
            jsonObject.Add("scope", JsonValue.CreateStringValue(tokenData.Scope));
            jsonObject.Add("refresh_token", JsonValue.CreateStringValue(tokenData.RefreshToken));
            jsonObject.Add("issued_time", JsonValue.CreateStringValue(tokenData.IssuedUtc.ToString()));

            return jsonObject.Stringify();
        }

        /// <summary>
        /// Gets a list of calendar events from the given response content.
        /// For formatting, see: https://developers.google.com/calendar/api/v3/reference/events/list
        /// </summary>
        /// <param name="responseContent">The response content formatted in an Events:list response from the Google Calendar API.</param>
        /// <returns>A list of calendar events on the user's primary calendar.</returns>
        private List<CalendarEvent> GetEventsFromResponse(string responseContent)
        {
            // Parse the list of events from the response content.
            JsonObject responseJson = JsonObject.Parse(responseContent);
            IJsonValue v = responseJson["items"];
            JsonArray itemsArray = responseJson["items"].GetArray();

            // Create an empty list of events, and parse and add each event.
            List<CalendarEvent> events = new List<CalendarEvent>();
            foreach (var eventJsonValue in itemsArray)
            {
                // This is necessary because of the type iterated over in the JsonArray.
                JsonObject eventJson = eventJsonValue.GetObject();

                // Parse the event's title (summary).
                string summary = eventJson["summary"].GetString();

                // Parse the event's start date and time.
                JsonObject startJson = eventJson["start"].GetObject();
                DateTime start = DateTime.Parse(startJson["dateTime"].GetString());

                // Parse the event's end date and time.
                JsonObject endJson = eventJson["end"].GetObject();
                DateTime end = DateTime.Parse(endJson["dateTime"].GetString());

                // Add a new calendar event model to the list.
                events.Add(new CalendarEvent()
                {
                    Name = summary,
                    Start = start,
                    End = end
                });
            }

            return events;
        }

        /// <summary>
        /// Gets a list of calendar events from the given response content.
        /// For formatting, see: https://developers.google.com/calendar/api/v3/reference/calendarList/list
        /// </summary>
        /// <param name="responseContent">The response content formatted in a CalendarList:list response from the Google Calendar API.</param>
        /// <returns>A list of calendars from the user's calendar list.</returns>
        private List<Calendar> GetCalendarsFromCalendarListResponse(string responseContent)
        {
            // Parse the response json content.
            // Response format is documented at: https://developers.google.com/calendar/api/v3/reference/calendarList/list
            JsonObject responseJson = JsonObject.Parse(responseContent);
            JsonArray itemsArray = responseJson["items"].GetArray();

            List<Calendar> calendars = new List<Calendar>();
            foreach(var calendarJsonValue in itemsArray)
            {
                // This is necessary because of the type iterated over in the JsonArray.
                JsonObject calendarJson = calendarJsonValue.GetObject();
                string id = calendarJson["id"].GetString();
                string summary = calendarJson["summary"].GetString();

                // Add a new calendar model to the list.
                calendars.Add(new Calendar()
                {
                    ID = id,
                    Name = summary
                });
            }

            return calendars;
        }
        #endregion
    }
}
