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
    public class GoogleCalendarAPI : ApiBase<GoogleCalendarAPI>
    {
        #region Constants
        /// <summary>
        /// The file path the API credentials are stored in.
        /// </summary>
        private static readonly string credentialsFilePath = "Assets/Config/credentials.json";
        /// <summary>
        /// The file name of the file stonring the Google Calendar API OAuth token.
        /// </summary>
        private static readonly string tokenFileName = "token.json";
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
        /// A cache of the authorized user credential for the Google Calendar API.
        /// </summary>
        private UserCredential credential = null;

        /// <summary>
        /// A cache of the token data for the Google Calendar API.
        /// </summary>
        private TokenResponse tokenData { get; set; }

        private HttpClient WebClient { get; set; }

        /// <summary>
        /// Returns whether the token exists and has expired, or exists but is missing an expiration limit.
        /// </summary>
        private bool TokenExpired
        {
            // There has to be a token data, AND EITHER
            //      there is no expiration time OR
            //      there is an expiration time and it has passed.
            get => this.IsAuthorized &&
                (this.tokenData.ExpiresInSeconds.HasValue == false ||
                    DateTime.Compare(DateTime.UtcNow, this.tokenData.IssuedUtc.AddSeconds(this.tokenData.ExpiresInSeconds.Value)) >= 0);
        }

        private bool isInitialized;
        /// <summary>
        /// Returns whether the API is initialized.
        /// </summary>
        public bool IsInitialized
        {
            get => this.isInitialized;
            set
            {
                this.isInitialized = value;

                // Only raise the initialized event if this is set to true.
                if (value)
                {
                    this.RaiseInitialized(this.Name);
                    System.Diagnostics.Debug.WriteLine("API Initialized!");
                }
            }
        }

        /// <summary>
        /// Returns whether the user has authorized the app with the API.
        /// </summary>
        public bool IsAuthorized
        {
            // The token data property contains a value if the app has previously been authorized.
            get => this.tokenData != null;
        }
        #endregion

        #region Events
        public delegate void InitializedHandler(object sender, ApiInitializedEventArgs e);
        /// <summary>
        /// Raised when the API is initialized.
        /// </summary>
        public event InitializedHandler Initialized;
        private void RaiseInitialized(string apiName)
        {
            // Create the args and call the listening event handlers, if there are any.
            ApiInitializedEventArgs args = new ApiInitializedEventArgs(apiName);
            this.Initialized?.Invoke(this, args);
        }
        #endregion

        #region Constructors
        public GoogleCalendarAPI()
        {
            this.Name = "Google Calendar";
            this.IsInitialized = false;
            this.WebClient = new HttpClient();

            this.InitializedTokenDataAsync();
        }
        #endregion

        #region Methods - OAuth
        public async Task AuthorizeAsync()
        {
            await this.StartOAuthAsync();
        }

        /// <summary>
        /// Prompts the user to authorize the app with Google's services.
        /// </summary>
        public async Task<Uri> StartOAuthAsync()
        {
            Uri startUri = this.GetOauthEndpoint();

            await Browser.OpenAsync(startUri);
            return startUri;
        }

        /// <summary>
        /// Completes the OAuth flow by exchanging the given authorization code for a token.
        /// </summary>
        /// <param name="authorizationCode">The authorization code to exchange for the token.</param>
        public async Task GetOauthTokenAsync(string authorizationCode)
        {
            System.Diagnostics.Debug.WriteLine("Getting token using code: " + authorizationCode);

            // Open the secrets file to read the client ID.
            using (var stream = new FileStream(GoogleCalendarAPI.credentialsFilePath, FileMode.Open, FileAccess.Read))
            {
                // Make a client specifically for exchanging tokens.
                using (HttpClient tokenClient = new HttpClient())
                {
                    // Read the client ID and secret from their file.
                    ClientSecrets secrets = GoogleClientSecrets.Load(stream).Secrets;

                    // Add the parameters to the OAuth token exchange endpoint.
                    string tokenExchangeUri = GoogleCalendarAPI.oauthTokenEndpoint +
                                                "?client_id=" + secrets.ClientId +
                                                "&client_secret=" + secrets.ClientSecret +
                                                "&code=" + authorizationCode +
                                                "&redirect_uri=" + GoogleCalendarAPI.oauthRedirectUri +
                                                "&grant_type=" + "authorization_code";

                    HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("client_id", secrets.ClientId),
                        new KeyValuePair<string, string>("client_secret", secrets.ClientSecret),
                        new KeyValuePair<string, string>("code", authorizationCode),
                        new KeyValuePair<string, string>("redirect_uri", GoogleCalendarAPI.oauthRedirectUri),
                        new KeyValuePair<string, string>("grant_type", "authorization_code")
                    });

                    // Use http GET to get a response from the token endpoint.
                    HttpResponseMessage response = new HttpResponseMessage();
                    try
                    {
                        response = await tokenClient.PostAsync(new Uri(GoogleCalendarAPI.oauthTokenEndpoint), content);
                        response.EnsureSuccessStatusCode();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error in HTTP response: " + ex.Message);
                    }

                    // No exceptions were thrown, so parse the response message.
                    string responseContent = await response.Content.ReadAsStringAsync();


                    // Set the token as part of the authorization header for the web client used to make API calls.
                    //JsonObject responseJson = JsonObject.Parse(responseContent);
                    //string token = responseJson["access_token"].GetString();
                    //this.WebClient.DefaultRequestHeaders.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Bearer", token);
                    //System.Diagnostics.Debug.WriteLine("Token obtained: " + token);

                    //this.credential = this.ConvertToCredential(responseContent);

                    this.tokenData = this.ConvertToTokenResponse(responseContent);
                }
            }
        }

        /// <summary>
        /// Saves the authorization data for the current connection to the Google Calendar API to a token file
        ///     at the authTokenFilePath location.
        /// </summary>
        public async Task SaveConnectionDataAsync()
        {
            // Save the token data, if there is any.
            if (this.tokenData != null)
            {
                string tokenJsonString = this.ConvertToJsonString(this.tokenData);

                StorageFile tokenFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(GoogleCalendarAPI.tokenFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(tokenFile, tokenJsonString);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets a list of events on the authorized user's primary calendar.
        /// See: https://developers.google.com/calendar/api/v3/reference/events/list
        /// </summary>
        /// <returns>A list of calendar events</returns>
        public async Task<List<CalendarEvent>> GetCalendarEventsAsync()
        {
            // Throw an API Not Authorized exception if the api is not authorized.
            if (this.IsAuthorized == false)
            {
                throw new ApiNotAuthorizedException();
            }

            // Refresh the token if needed.
            await this.RefreshTokenIfNeededAsync();

            using (HttpClient client = new HttpClient())
            {
                // Set the authorization header.
                client.DefaultRequestHeaders.Authorization =
                    new Windows.Web.Http.Headers.HttpCredentialsHeaderValue(this.tokenData.TokenType, this.tokenData.AccessToken);

                string uri = "https://www.googleapis.com/calendar/v3/calendars/primary/events";

                // Use http GET to get a response from the calendar endpoint.
                HttpResponseMessage response = new HttpResponseMessage();
                try
                {
                    response = await client.GetAsync(new Uri(uri));
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error in HTTP response: " + ex.Message);
                }

                // No exceptions were thrown, so parse the response message.
                string responseContent = await response.Content.ReadAsStringAsync();

                // Convert the response content to a list of events.
                return this.GetEventsFromResponse(responseContent);
            }
        }

        /// <summary>
        /// Gets a list of 10 events from the authorized user's primary calendar.
        /// </summary>
        /// <returns>A list of calendar events.</returns>
        public async Task<List<CalendarEvent>> GetCalendarEventsAsyncOld()
        {
            // Authorize if not already done so.
            if (!this.IsAuthorized)
            {
                await this.AuthorizeAsync();
            }

            // Create the Google Calendar API service.
            CalendarService service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName
            });

            // Define the parameters of the request.
            EventsResource.ListRequest request = service.Events.List("primary");
            request.TimeMin = DateTime.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // Execute the request to actually get the events from the API.
            Events events = request.Execute();

            // Return the list of events, converted to the local app's calendar event model.
            return GoogleCalendarAPI.ConvertCalendarEventsToLocal(events);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Initializes the token data from the saved token data if it can, otherwise sets
        /// the token data to null.
        /// </summary>
        private async Task InitializedTokenDataAsync()
        {
            // Try to load the token data.
            bool loaded = await this.TryLoadTokenDataAsync();
            if (loaded == false)
            {
                // The load was unsuccessful, so initialize the value to false.
                this.tokenData = null;
            }
            else
            {
                // Refresh the token if needed.
                await this.RefreshTokenIfNeededAsync();
            }

            // Mark initialization as complete.
            this.IsInitialized = true;
        }

        /// <summary>
        /// Trys to load the OAuth token data from the token file.
        /// </summary>
        /// <returns>Whether the token data was successfully loaded.</returns>
        private async Task<bool> TryLoadTokenDataAsync()
        {
            System.Diagnostics.Debug.WriteLine("Trying to load API token file...");
            // Read the text from the file.
            string lines = "";
            try
            {
                //lines = File.ReadAllText(GoogleCalendarAPI.authTokenFilePath);
                StorageFile tokenFile = await ApplicationData.Current.LocalFolder.GetFileAsync(GoogleCalendarAPI.tokenFileName);
                lines = await FileIO.ReadTextAsync(tokenFile);
            }
            catch (Exception ex)
            {
                // An IO exception occured, so return false.
                System.Diagnostics.Debug.WriteLine("Error accessing: " + ApplicationData.Current.LocalFolder.Path);
                return false;
            }

            // Return false if the read data is empty or whitespace.
            if (String.IsNullOrWhiteSpace(lines))
            {
                System.Diagnostics.Debug.WriteLine("API token file was empty!");
                return false;
            }

            // Try to convert the token response to token data.
            try
            {
                // NOTE: An exception is thrown if the JSON contained in the string
                //      is ill-formed.
                this.tokenData = this.ConvertToTokenResponse(lines);
            }
            catch(Exception ex)
            {
                return false;
            }

            // The data was successfully loaded if this point is reached.
            return true;
        }

        /// <summary>
        /// Refreshes the token, if the token needs to be refreshed.
        /// </summary>
        /// <returns>Whether the token was refreshed.</returns>
        private async Task<bool> RefreshTokenIfNeededAsync()
        {
            System.Diagnostics.Debug.WriteLine("Checking if token expired...");
            // Refresh the token if needed.
            if (this.TokenExpired)
            {
                System.Diagnostics.Debug.WriteLine("Token expired!");
                await this.RefreshTokenAsync();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Refreshes the access token using the refresh token.
        /// </summary>
        /// <returns></returns>
        private async Task RefreshTokenAsync()
        {
            System.Diagnostics.Debug.WriteLine("Refreshing token.");

            // Open the secrets file to read the client ID.
            using (var stream = new FileStream(GoogleCalendarAPI.credentialsFilePath, FileMode.Open, FileAccess.Read))
            {
                // Make a client specifically for exchanging tokens.
                using (HttpClient tokenClient = new HttpClient())
                {
                    // Read the client ID and secret from their file.
                    ClientSecrets secrets = GoogleClientSecrets.Load(stream).Secrets;

                    HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("client_id", secrets.ClientId),
                        new KeyValuePair<string, string>("client_secret", secrets.ClientSecret),
                        new KeyValuePair<string, string>("refresh_token", this.tokenData.RefreshToken),
                        new KeyValuePair<string, string>("grant_type", "refresh_token")
                    });

                    // Use http GET to get a response from the token endpoint.
                    HttpResponseMessage response = new HttpResponseMessage();
                    try
                    {
                        response = await tokenClient.PostAsync(new Uri(GoogleCalendarAPI.oauthTokenEndpoint), content);
                        response.EnsureSuccessStatusCode();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error in HTTP response: " + ex.Message);
                    }

                    // No exceptions were thrown, so parse the response message.
                    string responseContent = await response.Content.ReadAsStringAsync();
                    JsonObject responseJson = JsonObject.Parse(responseContent);
                    string token = responseJson["access_token"].GetString();
                    long expiresInSeconds = (long)responseJson["expires_in"].GetNumber();

                    // Update the access token and expiration.
                    this.tokenData.AccessToken = token;
                    this.tokenData.ExpiresInSeconds = expiresInSeconds;
                    this.tokenData.IssuedUtc = DateTime.UtcNow;
                }
            }
            System.Diagnostics.Debug.WriteLine("Done refreshing token!");
        }
        
        private Uri GetOauthEndpoint()
        {
            // Build the scopes string as a space deliminated list, per the requirement
            //      specified here: https://developers.google.com/identity/protocols/oauth2/native-app#uwp
            StringBuilder scopesString = new StringBuilder();
            foreach( string scope in GoogleCalendarAPI.scopes)
            {
                scopesString.Append(scope);
                scopesString.Append(" ");
            }

            // Open the secrets file to read the client ID.
            using (var stream = new FileStream(GoogleCalendarAPI.credentialsFilePath, FileMode.Open, FileAccess.Read))
            {
                ClientSecrets secrets = GoogleClientSecrets.Load(stream).Secrets;

                // Create the start URI
                string startUrl = GoogleCalendarAPI.oauthBaseUri +
                                    "?client_id=" + secrets.ClientId +
                                    "&redirect_uri=" + GoogleCalendarAPI.oauthRedirectUri +
                                    "&response_type=code" +
                                    "&scope=" + scopesString.ToString();
                return new Uri(startUrl);
            }
        }

        private UserCredential ConvertToCredential(string responseContent)
        {
            JsonObject responseJson = JsonObject.Parse(responseContent);
            string token = responseJson["access_token"].GetString();
            long expiresInSeconds = (long)responseJson["expires_in"].GetNumber();
            string tokenType = responseJson["token_type"].GetString();
            string scope = responseJson["scope"].GetString();
            string refreshToken = responseJson["refresh_token"].GetString();

            AuthorizationCodeFlow flow = new AuthorizationCodeFlow(new AuthorizationCodeFlow.Initializer(GoogleCalendarAPI.oauthBaseUri, GoogleCalendarAPI.oauthTokenEndpoint));
            return new UserCredential(
                        flow,
                        "",
                        new Google.Apis.Auth.OAuth2.Responses.TokenResponse()
                        {
                            AccessToken = token,
                            TokenType = tokenType,
                            ExpiresInSeconds = expiresInSeconds,
                            RefreshToken = refreshToken,
                            Scope = scope,
                            IdToken = "",
                            IssuedUtc = DateTime.UtcNow
                        });
        }

        /// <summary>
        /// Converts the token response from the Google Calendar API V3 to its corresponding TokenResponse object.
        /// </summary>
        /// <param name="responseContent">The string of the Json object to convert into a token response object.</param>
        /// <returns>The token response object containing the data in the given content string.</returns>
        private TokenResponse ConvertToTokenResponse(string responseContent)
        {
            JsonObject responseJson = JsonObject.Parse(responseContent);
            string token = responseJson["access_token"].GetString();
            long expiresInSeconds = (long)responseJson["expires_in"].GetNumber();
            string tokenType = responseJson["token_type"].GetString();
            string scope = responseJson["scope"].GetString();
            string refreshToken = responseJson["refresh_token"].GetString();

            // If the response content contains an issued time, use that. Otherwise, default to Utc now.
            //      This value does not exist from the actual response JSON from Google's API, so this
            //      is a new API response if the value does not exist, and thus the issued time should be
            //      set to now. Otherwise, the respose JSON is a saved copy, and the saved value should
            //      be used.
            DateTime issuedTime = responseJson.ContainsKey("issued_time") ? DateTime.Parse(responseJson["issued_time"].GetString()) : DateTime.UtcNow;

            return new TokenResponse()
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
        /// Converts the token response object into a string containing a serialized Json representation of that object.
        /// </summary>
        /// <param name="tokenResponse">The response to serialize into a string.</param>
        /// <returns>The string containing a serialized Json representation of the given Token Response object.</returns>
        private string ConvertToJsonString(TokenResponse tokenResponse)
        {
            JsonObject jsonObject = new JsonObject();
            jsonObject.Add("access_token", JsonValue.CreateStringValue(tokenResponse.AccessToken));

            // Set the expiration time (lifespan of the token) to zero seconds if no value exists.
            double expirationTime = tokenResponse.ExpiresInSeconds.HasValue ? (double)tokenResponse.ExpiresInSeconds.Value : 0.0;
            jsonObject.Add("expires_in", JsonValue.CreateNumberValue(expirationTime));

            jsonObject.Add("token_type", JsonValue.CreateStringValue(tokenResponse.TokenType));
            jsonObject.Add("scope", JsonValue.CreateStringValue(tokenResponse.Scope));
            jsonObject.Add("refresh_token", JsonValue.CreateStringValue(tokenResponse.RefreshToken));
            jsonObject.Add("issued_time", JsonValue.CreateStringValue(tokenResponse.IssuedUtc.ToString()));

            return jsonObject.Stringify();
        }

        /// <summary>
        /// Gets a list of calendar events from the given response content.
        /// For formatting, see: https://developers.google.com/calendar/api/v3/reference/events/list
        /// </summary>
        /// <param name="responseContent">The response content formatted in an Events:list response from the Google Calendar API.</param>
        /// <returns></returns>
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
        /// Converts a list of events in the Google Calendar API v3 format to a list of
        /// events in the local app's format.
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        private static List<CalendarEvent> ConvertCalendarEventsToLocal(Events events)
        {
            // Reference: https://developers.google.com/calendar/concepts/events-calendars
            List<CalendarEvent> calEvents = new List<CalendarEvent>();

            // Events are stored in the Items collection.
            foreach (Event eventItem in events.Items)
            {
                // Create a new Calendar Event model and add it to the list, initializing
                //      its properties based on the calendar event from the API.
                calEvents.Add(new CalendarEvent()
                {
                    Name = eventItem.Summary,
                    Start = eventItem.Start.DateTime.HasValue ? eventItem.Start.DateTime.Value : DateTime.Now,
                    End = eventItem.End.DateTime.HasValue ? eventItem.End.DateTime.Value : DateTime.Now
                });
            }

            return calEvents;
        }
        #endregion
    }
}
