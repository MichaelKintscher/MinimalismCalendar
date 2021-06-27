using MinimalismCalendar.Controllers.Navigation;
using MinimalismCalendar.Enums;
using MinimalismCalendar.EventArguments;
using MinimalismCalendar.Models;
using MinimalismCalendar.Models.GoogleCalendar;
using MinimalismCalendar.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml.Controls;

namespace MinimalismCalendar.Controllers
{
    /// <summary>
    /// The main controller for the app - handles navigation between app-level processes.
    /// </summary>
    public class AppController : SingletonController<AppController>
    {
        #region Properties
        /// <summary>
        /// The root page for app navigation.
        /// </summary>
        public MainPage RootPage { get; private set; }

        /// <summary>
        /// The current navigation state of the app.
        /// </summary>
        public AppNavigationState NavState { get; set; }

        /// <summary>
        /// Reference to currently displayed page.
        /// </summary>
        public Page CurrentPage { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an instance of the app controller class, with default values for properties.
        /// </summary>
        public AppController()
        {
            // Register for the network status changed event.
            NetworkInformation.NetworkStatusChanged += this.NetworkStatusChanged;

            // Initialize the controller with a main page as the root and the start state for the app navigation.
            this.RootPage = new MainPage();
            this.NavState = new StartState(this);
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles changes in the network connection status.
        /// </summary>
        /// <param name="sender"></param>
        public void NetworkStatusChanged(object sender)
        {
            // This event is NOT running on the UI thread, so any UI-related
            //      work needs to be dispatched on the UI thread.
            this.RootPage.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                // Check the network connection status, and respond accordingly.
                this.RefreshInternetConnectionStatus();
            });
        }

        /// <summary>
        /// Handles a navigation request between app pages.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNavigationRequested(object sender, NavigationRequestedEventArgs e)
        {
            // If this is a back request, go back and exit this method.
            if (e.IsBackRequest)
            {
                this.NavState.GoBack();
                return;
            }

            switch (e.ToPage)
            {
                case PageTypes.Home:
                    // Navigate to the home page.
                    this.NavState.GotoHome();
                    break;
                case PageTypes.Settings:
                    // Navigate to the settings page.
                    this.NavState.GotoSettings();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handles when a navigation between pages is complete. Subscribes to the new page's
        /// events and unsubscribes from the old page's events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNavigated(object sender, NavigatedEventArgs e)
        {
            // Store a reference to the page as the new current page.
            this.CurrentPage = e.PageNavigatedTo;

            // Initialize the new page.
            if (e.PageNavigatedTo is HomePage homePage)
            {
                this.InitializeHomePage(homePage);
            }
            else if (e.PageNavigatedTo is SettingsPage settingsPage)
            {
                this.InitializeSettingsPage(settingsPage);
            }

            // Unsubscribe from the old page's events. This is to clear any handles on the page
            //      so that garbage collection deletes it and prevents a memory leak.
            if (e.PageNavigatedFrom is null)
            {
                // There is no from page (this is the app initialization), so update the internet connection status.
                // Check the network connection status, and respond accordingly.
                this.RefreshInternetConnectionStatus();
                return;
            }
            else if (e.PageNavigatedFrom is HomePage homePageFrom)
            {

            }
            else if (e.PageNavigatedFrom is SettingsPage settingsPageFrom)
            {
                settingsPageFrom.ConnectServiceRequested -= this.SettingsPage_ConnectServiceRequested;
                settingsPageFrom.OauthCodeAcquired -= this.SettingsPage_OauthCodeAcquired;
                settingsPageFrom.RetryOauthRequested -= this.SettingsPage_ConnectServiceRequested;
            }
        }

        /// <summary>
        /// Handles when the user requests to connect a service.S
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsPage_ConnectServiceRequested(object sender, ConnectServiceRequestedEventArgs e)
        {
            this.ConnectGoogleCalendarAsync();

            if (sender is SettingsPage settingsPage)
            {
                settingsPage.ShowServiceOAuthCodeUIAsync(e.ServiceName);
            }
        }

        /// <summary>
        /// Handles when the user has entered an OAuth code to connect a service.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsPage_OauthCodeAcquired(object sender, OauthCodeAcquiredEventArgs e)
        {
            // Validate the code.
            if (String.IsNullOrWhiteSpace(e.Code))
            {
                // The code is definitely invalid; no point in reaching out to the server.
                if (sender is SettingsPage settingsPage)
                {
                    string errorMessage = "No code was entered!";
                    settingsPage.ShowOAuthErrorUIAsync(e.ServiceName, errorMessage);
                }
            }
            else
            {
                // Complete the OAuth flow.
                GoogleCalendarAPI.Instance.GetOauthTokenAsync(e.Code);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Intended to be called upon app launch. Initializes app navigation.
        /// </summary>
        /// <param name="rootPage">The root page for this initialization of the app.</param>
        public void StartApp(MainPage rootPage)
        {
            // Subscribe to the root page's events.
            rootPage.NavigationRequested += this.OnNavigationRequested;
            rootPage.Navigated += this.OnNavigated;

            // Set the given page as the root page.
            this.RootPage = rootPage;

            // Navigate to the home page.
            this.NavState.GotoHome();
        }

        public void ResumeAppFromAPIAuth(MainPage rootPage)
        {
            // Subscribe to the root page's events.
            rootPage.NavigationRequested += this.OnNavigationRequested;
            rootPage.Navigated += this.OnNavigated;

            // Set the given page as the root page.
            this.RootPage = rootPage;

            System.Diagnostics.Debug.WriteLine("annnnnd we back!");

            // Finish the Authorization.
            //ClickUpAPIWrapper.Instance.GetOauthTokenAsync();
            // Navigate to the home page.
            //this.NavState.GotoHome();
        }
        #endregion

        #region Helper Methods
        /*
         * This design pattern is necessary because the NavigationView.Navigate method
         * takes in a Type, and not a page instance. It creates the page instance and
         * places that instance into the event args for a "Navigated" event. Handling
         * this event is the way to get access to the instance of the new page, and
         * thus initialize its data.
         */

        /// <summary>
        /// Initializes the given instance of the home page.
        /// </summary>
        /// <param name="homePage">The instance of the home page to initialize.</param>
        private void InitializeHomePage(HomePage homePage)
        {
            // Subscribe to the new page's events.
            //    - None at the moment.

            // Initialize the calendar control.
            List<CalendarEvent> eventList = TestDataGenerator.GetTestEvents();
            homePage.InitializeCalendarControl(DateTime.Now, eventList);
        }

        /// <summary>
        /// Initializes the given instance of the settings page.
        /// </summary>
        /// <param name="settingsPage"></param>
        private void InitializeSettingsPage(SettingsPage settingsPage)
        {
            // Subscribe to the new page's events.
            settingsPage.ConnectServiceRequested += this.SettingsPage_ConnectServiceRequested;
            settingsPage.OauthCodeAcquired += this.SettingsPage_OauthCodeAcquired;
            settingsPage.RetryOauthRequested += this.SettingsPage_ConnectServiceRequested;

            // Update the Google API status.
            settingsPage.GoogleAuthStatus = GoogleCalendarAPI.IsAuthorized ? "Good to go!" : "Please reconnect.";
        }

        /// <summary>
        /// Updates the app depending on the availability of an internet connection.
        /// </summary>
        private void RefreshInternetConnectionStatus()
        {
            ConnectionProfile internetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();

            // Change to offline mode if no profile was found, then return.
            if (internetConnectionProfile == null)
            {
                this.ChangeToOfflineMode();
                return;
            }

            // A profile was found. Check the network connectivity level.
            NetworkConnectivityLevel networkConnectivityLevel = internetConnectionProfile.GetNetworkConnectivityLevel();
            switch (networkConnectivityLevel)
            {
                // No connection detected... change to offline mode.
                case NetworkConnectivityLevel.None:
                    this.ChangeToOfflineMode();
                    break;
                case NetworkConnectivityLevel.LocalAccess:
                    break;
                case NetworkConnectivityLevel.ConstrainedInternetAccess:
                    break;
                case NetworkConnectivityLevel.InternetAccess:
                    break;
                default:
                    break;
            }

            System.Diagnostics.Debug.WriteLine("Internet connection state: " + networkConnectivityLevel);
        }

        /// <summary>
        /// Updates the app to offline mode.
        /// </summary>
        private void ChangeToOfflineMode()
        {
            // Update the relevant property of the main page.
            this.RootPage.InternetConnectionAvailable = false;
            System.Diagnostics.Debug.WriteLine("You went offline!");
        }

        /// <summary>
        /// Starts an OAuth flow to connect to the Google Calendar API.
        /// </summary>
        /// <returns></returns>
        private async Task ConnectGoogleCalendarAsync()
        {
            await GoogleCalendarAPI.Instance.AuthorizeAsync();
        }
        #endregion
    }
}
