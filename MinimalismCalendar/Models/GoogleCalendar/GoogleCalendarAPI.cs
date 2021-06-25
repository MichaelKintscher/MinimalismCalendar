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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        #endregion

        #region Properties
        /// <summary>
        /// A cache of the authorized user credential for the Google Calendar API.
        /// </summary>
        private static UserCredential credential = null;

        /// <summary>
        /// Returns whether the user has authorized the app with the API.
        /// </summary>
        public static bool IsAuthorized
        {
            // The credential property contains a value if the app has previously been authorized.
            get => GoogleCalendarAPI.credential != null;
        }

        public static readonly string baseUri = "https://accounts.google.com/o/oauth2/v2/auth";

        public static readonly string redirectUri = "urn:ietf:wg:oauth:2.0:oob";
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
        /// <returns>The authorization token to use in future API calls.</returns>
        public async Task<string> GetOauthTokenAsync(string authorizationCode)
        {
            System.Diagnostics.Debug.WriteLine("Getting token using code: " + authorizationCode);

            return "";
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
                string startUrl = GoogleCalendarAPI.baseUri +
                                    "?client_id=" + secrets.ClientId +
                                    "&redirect_uri=" + GoogleCalendarAPI.redirectUri +
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
