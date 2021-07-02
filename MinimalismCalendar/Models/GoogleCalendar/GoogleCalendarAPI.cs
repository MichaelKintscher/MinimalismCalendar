using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MinimalismCalendar.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Xamarin.Essentials;

namespace MinimalismCalendar.Models.GoogleCalendar
{
    /// <summary>
    /// Wrapper class for interfacing with the Google Calendar API. This class
    /// encapsulates all external dependencies on the Google Calendar v3 API.
    /// </summary>
    public class GoogleCalendarAPI : SingletonController<GoogleCalendarAPI>
    {
        #region Constants
        /// <summary>
        /// The file path the API credentials are stored in.
        /// </summary>
        private static readonly string credentialsFilePath = "Assets/Config/credentials.json";
        /// <summary>
        /// The file path the authenticated token for API access is stored in.
        /// </summary>
        private static readonly string authTokenFilePath = ApplicationData.Current.LocalFolder.Path + "\\token.json";
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
        private static UserCredential credential = null;

        private HttpClient WebClient { get; set; }

        /// <summary>
        /// Returns whether the user has authorized the app with the API.
        /// </summary>
        public static bool IsAuthorized
        {
            // The credential property contains a value if the app has previously been authorized.
            get => GoogleCalendarAPI.credential != null;
        }
        #endregion

        #region Constructors
        public GoogleCalendarAPI()
        {
            this.WebClient = new HttpClient();
        }
        #endregion

        #region Methods
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
                                                "&code=" + authorizationCode;

                    // Use http GET to get a response from the token endpoint.
                    HttpResponseMessage response = new HttpResponseMessage();
                    try
                    {
                        response = await tokenClient.GetAsync(new Uri(tokenExchangeUri));
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

                    // Set the token as part of the authorization header for the web client used to make API calls.
                    this.WebClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    System.Diagnostics.Debug.WriteLine("Token obtained: " + token);
                }
            }
        }

        /// <summary>
        /// Gets a list of 10 events from the authorized user's primary calendar.
        /// </summary>
        /// <returns>A list of calendar events.</returns>
        public async Task<List<CalendarEvent>> GetCalendarEventsAsync()
        {
            // Authorize if not already done so.
            if (!GoogleCalendarAPI.IsAuthorized)
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
