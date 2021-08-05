using MinimalismCalendar.Controllers.Navigation;
using MinimalismCalendar.Enums;
using MinimalismCalendar.EventArguments;
using MinimalismCalendar.Managers;
using MinimalismCalendar.Models;
using MinimalismCalendar.Models.AppConfigModels;
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

        private Guid accountIdPendingAuthorization = Guid.Empty;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an instance of the app controller class, with default values for properties.
        /// </summary>
        public AppController()
        {
            // Register for the network status changed event.
            NetworkInformation.NetworkStatusChanged += this.NetworkStatusChanged;

            // Register for the API initialization events.
            GoogleCalendarAPI.Instance.Initialized += this.GoogleApi_Initialized;
            GoogleCalendarAPI.Instance.Authorized += this.GoogleApi_Authorized;

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
        /// Handles when the Google Calendar API is initialized and read to use.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GoogleApi_Initialized(object sender, ApiEventArgs e)
        {
            // If the home page is currently displayed, refresh the calendar.
            if (this.RootPage.CurrentPage is HomePage homePage)
            {
                this.RefreshHomePageCalendarControlAsync(homePage);
            }
        }

        /// <summary>
        /// Handles when the Google Calendar API has changed authorized status.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GoogleApi_Authorized(object sender, ApiAuthorizedEventArgs e)
        {
            this.AddNewAccountDataAsync(e.AccountID);
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
            string toPageName = "";
            if (e.PageNavigatedTo is HomePage homePage)
            {
                this.InitializeHomePageAsync(homePage);
                toPageName = "Home Page";
            }
            else if (e.PageNavigatedTo is SettingsPage settingsPage)
            {
                this.InitializeSettingsPageAsync(settingsPage);
                toPageName = "Settings Page";
            }

            // Unsubscribe from the old page's events. This is to clear any handles on the page
            //      so that garbage collection deletes it and prevents a memory leak.
            string fromPageName = "";
            if (e.PageNavigatedFrom is null)
            {
                // There is no from page (this is the app initialization).
                fromPageName = "App Launch";
            }
            else if (e.PageNavigatedFrom is HomePage homePageFrom)
            {
                fromPageName = "Home Page";
                homePageFrom.CalendarViewChanged -= this.HomePage_CalendarViewChanged;
            }
            else if (e.PageNavigatedFrom is SettingsPage settingsPageFrom)
            {
                settingsPageFrom.ChangeAccountConnectionRequested -= this.SettingsPage_ChangeAccountConnectionRequested;
                settingsPageFrom.OauthCodeAcquired -= this.SettingsPage_OauthCodeAcquired;
                settingsPageFrom.ShowAtLaunchSettingChanged -= this.SettingsPage_ShowAtLaunchSettingChanged;
                settingsPageFrom.CalendarVisibilityChanged -= this.SettingsPage_CalendarVisibilityChanged;
                settingsPageFrom.ConnectionRequestCancelled -= this.SettingsPage_ConnectionRequestCancelled;
                fromPageName = "Settings Page";
            }

            // Check the network connection status, and respond accordingly.
            this.RefreshInternetConnectionStatus();

            System.Diagnostics.Debug.WriteLine("Navigated to " + toPageName + " from " + fromPageName);
        }
        #endregion

        #region Event Handlers - Home Page
        /// <summary>
        /// Handles when the calendar view changes on the home page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HomePage_CalendarViewChanged(object sender, CalendarViewChangedEventArgs e)
        {
            // Update the stored last viewed date.
            AppConfig.Instance.SetLastViewedDate(e.NewSunday);
        }
        #endregion

        #region Event Handlers - Settings Page
        /// <summary>
        /// Handles when the user requests to change a connection to a service.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsPage_ChangeAccountConnectionRequested(object sender, ChangeAccountConnectionRequestedEventArgs e)
        {
            if (sender is SettingsPage settingsPage)
            {
                switch (e.Action)
                {
                    // A request was issued to connect to the service.
                    case ConnectionAction.Connect:
                        this.StartOAuthAsync();
                        settingsPage.ShowServiceOAuthCodeUIAsync(e.AccoutId);
                        break;

                    // A request was issued to retry connecting to the service.
                    case ConnectionAction.RetryConnect:
                        this.StartOAuthAsync();
                        settingsPage.ShowServiceOAuthCodeUIAsync(e.AccoutId);
                        break;

                    // A request was issued to disconnect the service.
                    case ConnectionAction.Disconnect:
                        this.DisconnectAccountAsync(e.AccoutId, settingsPage);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Handles when the user has entered an OAuth code to connect a service.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsPage_OauthCodeAcquired(object sender, OAuthFlowContinueEventArgs e)
        {
            // Validate the code.
            if (String.IsNullOrWhiteSpace(e.Code))
            {
                // The code is definitely invalid; no point in reaching out to the server.
                if (sender is SettingsPage settingsPage)
                {
                    string errorMessage = "No code was entered!";
                    settingsPage.ShowOAuthErrorUIAsync(errorMessage);
                }
            }
            else
            {
                // Complete the OAuth flow and clear the state data for the pending authorization.
                GoogleCalendarAPI.Instance.GetOauthTokenAsync(this.accountIdPendingAuthorization.ToString(), e.Code);
                this.accountIdPendingAuthorization = Guid.Empty;
            }
        }

        /// <summary>
        /// Handles when the user cancells the pending connect account request from the settings page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsPage_ConnectionRequestCancelled(object sender, OAuthFlowContinueEventArgs e)
        {
            // Clear the associated state value.
            this.accountIdPendingAuthorization = Guid.Empty;
        }

        /// <summary>
        /// Handles when the user changes the show at launch app setting from the settings page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsPage_ShowAtLaunchSettingChanged(object sender, ShowAtLaunchSettingChangedEventArgs e)
        {
            // Set the related app setting.
            AppSettingsManager.ResumeLastViewedOnLaunch = e.ResumeLastViewed;
        }

        /// <summary>
        /// Handles when the user changes the visibility of a calendar from the settings page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsPage_CalendarVisibilityChanged(object sender, CalendarVisibilityChangedEventArgs e)
        {
            // Based on the visibility the calendar was set to...
            switch (e.Visibility)
            {
                case CalendarVisibility.Visible:
                    // The calendar was set to visible, so remove it from the hidden calendars list.
                    AppConfig.Instance.RemoveHiddenCalendar(e.Calendar);
                    break;
                case CalendarVisibility.Hidden:
                    // The calendar was set to hidden, so add it to the hidden calendars list.
                    AppConfig.Instance.AddHiddenCalendar(e.Calendar);
                    break;
                default:
                    break;
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

        /// <summary>
        /// Saves the state of the application.
        /// </summary>
        public async Task SaveAppStateAsync()
        {
            // Save the connection state for the Google Calendar API Connection.
            await GoogleCalendarAPI.Instance.SaveConnectionDataAsync();

            // Save the user's app config settings.
            await AppConfig.Instance.SaveAsync();
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
        private async Task InitializeHomePageAsync(HomePage homePage)
        {
            // Subscribe to the new page's events.
            homePage.CalendarViewChanged += this.HomePage_CalendarViewChanged;

            // Initialize the calendar control.
            //List<CalendarEvent> eventList = TestDataGenerator.GetTestEvents();
            await this.RefreshHomePageCalendarControlAsync(homePage);
            System.Diagnostics.Debug.WriteLine("Home page initialized!");
        }

        /// <summary>
        /// Initializes the given instance of the settings page.
        /// </summary>
        /// <param name="settingsPage">The instance of the settings page to initialize.</param>
        private async Task InitializeSettingsPageAsync(SettingsPage settingsPage)
        {
            // Subscribe to the new page's events.
            settingsPage.ChangeAccountConnectionRequested += this.SettingsPage_ChangeAccountConnectionRequested;
            settingsPage.OauthCodeAcquired += this.SettingsPage_OauthCodeAcquired;
            settingsPage.ConnectionRequestCancelled += this.SettingsPage_ConnectionRequestCancelled;
            settingsPage.ShowAtLaunchSettingChanged += this.SettingsPage_ShowAtLaunchSettingChanged;
            settingsPage.CalendarVisibilityChanged += this.SettingsPage_CalendarVisibilityChanged;

            // Add the accounts logged into with this app to the page.
            List<CalendarProviderAccount> accounts = await AppConfig.Instance.GetAccountsAsync();
            foreach (CalendarProviderAccount account in accounts)
            {
                // Get the account and add it to the list on the page.
                settingsPage.Accounts.Add(account);
            }

            // Initialize the ResumeLastViewedOnLaunch setting.
            settingsPage.ResumeLastViewedOnLaunch = AppSettingsManager.ResumeLastViewedOnLaunch;

            // Refresh the calendars list.
            await this.RefreshSettinsPageCalendarList(settingsPage);
        }

        /// <summary>
        /// Refreshes the home page calendar control.
        /// </summary>
        /// <param name="homePage">The home page instance to refresh the calendar control on.</param>
        /// <returns></returns>
        private async Task RefreshHomePageCalendarControlAsync(HomePage homePage)
        {
            // Get a list of the accounts.
            List<CalendarProviderAccount> accounts = await AppConfig.Instance.GetAccountsAsync();

            // For each account...
            List<CalendarEvent> eventList = new List<CalendarEvent>();
            foreach (CalendarProviderAccount account in accounts)
            {
                // For each calendar on the account...
                List<Calendar> calendars = await GoogleCalendarAPI.Instance.GetCalendarsAsync(account.ID);
                foreach (Calendar calendar in calendars)
                {
                    // Skip any hidden calendars.
                    if (await AppConfig.Instance.IsCalendarHiddenAsync(account.ID, calendar.ID))
                    {
                        continue;
                    }

                    // Get all events on the calendar and add them to the list.
                    try
                    {
                        List<CalendarEvent> eventsForAccount = await GoogleCalendarAPI.Instance.GetCalendarEventsAsync(account.ID, calendar.ID);
                        eventList.AddRange(eventsForAccount);
                    }
                    catch(Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Problem reading calendar with ID: " + calendar.ID + ". Calendar skipped.");
                        continue;
                    }
                }
            }

            // Get the date to set the calendar view to.
            DateTime visibleDate = DateTime.Now;

            // If the resume setting is enabled then use the last viewed date.
            if (AppSettingsManager.ResumeLastViewedOnLaunch)
            {
                // Still default to today if a saved value is not found.
                DateTime? lastViewedDate = await AppConfig.Instance.GetLastViewedDateAsync();
                visibleDate = lastViewedDate.HasValue ? lastViewedDate.Value : DateTime.Now;
            }

            // Initialize the calendar control on the homepage.
            homePage.InitializeCalendarControl(visibleDate, eventList);
            System.Diagnostics.Debug.WriteLine("Home page calendar initialized!");
        }

        /// <summary>
        /// Refreshes the settings page calendar list.
        /// </summary>
        /// <param name="settingsPage">The settings page instance to refresh the calendar list on.</param>
        /// <returns></returns>
        private async Task RefreshSettinsPageCalendarList(SettingsPage settingsPage)
        {
            // Clear the available calendars list.
            settingsPage.ClearCalenderLists();

            // Get a list of the accounts.
            List<CalendarProviderAccount> accounts = await AppConfig.Instance.GetAccountsAsync();

            // For each account...
            List<Calendar> calendars = new List<Calendar>();
            foreach (CalendarProviderAccount account in accounts)
            {
                // Get an updated list of available calendars from the Google API.
                List<Calendar> calendarsForAccount = await GoogleCalendarAPI.Instance.GetCalendarsAsync(account.ID);
                calendars.AddRange(calendarsForAccount);
            }

            // Get a list of the hidden calendars from the app's config settings.
            List<HiddenCalendarRecord> records = await AppConfig.Instance.GetHiddenCalendarsAsync();

            // Split the list of calendars from the Google API into visible vs hidden calendars, based on
            //      whether there exists a record for that calendar in the hidden calendar records.
            List<Calendar> visibleCalendars = calendars.Where(c =>
                    records.Where(r =>
                            r.CalendarName == c.Name
                            ).FirstOrDefault() == null
                    ).ToList();
            List<Calendar> hiddenCalendars = calendars.Where(c =>
                    records.Where(r =>
                            r.CalendarName == c.Name
                            ).FirstOrDefault() != null
                    ).ToList();

            // Add the calendars to their appropriate lists.
            settingsPage.AddCalendars(visibleCalendars, CalendarVisibility.Visible);
            settingsPage.AddCalendars(hiddenCalendars, CalendarVisibility.Hidden);
        }

        /// <summary>
        /// Wrapps the call to the Google Calendar API so that await can be used.
        /// </summary>
        /// <returns></returns>
        private async Task StartOAuthAsync()
        {
            this.accountIdPendingAuthorization = await GoogleCalendarAPI.Instance.StartOAuthAsync();
        }

        /// <summary>
        /// Populates the account info for the given account ID and adds that account to the app's data.
        /// </summary>
        /// <param name="accountId">The app-assigned ID of the account to get data for and add.</param>
        /// <returns></returns>
        private async Task AddNewAccountDataAsync(string accountId)
        {
            // Create the account model and add it to the app config.
            CalendarProviderAccount account = await GoogleCalendarAPI.Instance.GetAccountAsync(accountId);
            await AppConfig.Instance.AddAccountAsync(account);

            // If the home page is currently displayed, refresh the calendar.
            if (this.RootPage.CurrentPage is HomePage homePage)
            {
                await this.RefreshHomePageCalendarControlAsync(homePage);
            }
            // If the settings page is currently displayed, refresh the API status message.
            else if (this.RootPage.CurrentPage is SettingsPage settingsPage)
            {
                // Add the account to the page.
                settingsPage.Accounts.Add(account);

                // Refresh the calendars list.
                await this.RefreshSettinsPageCalendarList(settingsPage);
            }
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
                this.ChangeOnlineMode(false);
                return;
            }

            // A profile was found. Check the network connectivity level.
            NetworkConnectivityLevel networkConnectivityLevel = internetConnectionProfile.GetNetworkConnectivityLevel();
            switch (networkConnectivityLevel)
            {
                // No connection detected... change to offline mode.
                case NetworkConnectivityLevel.None:
                    this.ChangeOnlineMode(false);
                    break;
                case NetworkConnectivityLevel.LocalAccess:
                    break;
                case NetworkConnectivityLevel.ConstrainedInternetAccess:
                    break;
                case NetworkConnectivityLevel.InternetAccess:
                    this.ChangeOnlineMode(true);
                    break;
                default:
                    break;
            }

            System.Diagnostics.Debug.WriteLine("Internet connection state: " + networkConnectivityLevel);
        }

        /// <summary>
        /// Updates the app to the given online state.
        /// </summary>
        /// <param name="online">Whether internet access is available or not.</param>
        private void ChangeOnlineMode(bool online)
        {
            // Update the relevant property of the main page.
            this.RootPage.InternetConnectionAvailable = online;
            System.Diagnostics.Debug.WriteLine("You went " + (online? "online" : "offline") + " !");
        }

        /// <summary>
        /// Disconnects an account.
        /// </summary>
        /// <param name="accountId">The ID of the account to remove.</param>
        /// <param name="settingsPage">The settings page instance to update with the disconnected account.</param>
        /// <returns></returns>
        private async Task DisconnectAccountAsync(string accountId, SettingsPage settingsPage)
        {
            // Remove the account's connection with Google.
            await GoogleCalendarAPI.Instance.RemoveAccount(accountId);

            // Remove cahced account data.
            await AppConfig.Instance.RemoveAccountAsync(accountId);

            // Refresh the calendar lists on the settings page.
            await this.RefreshSettinsPageCalendarList(settingsPage);

            // Remove the account from the settings page.
            CalendarProviderAccount accountToRemove = settingsPage.Accounts.Where(a => a.ID == accountId).FirstOrDefault();
            if (accountToRemove != null)
            {
                settingsPage.Accounts.Remove(accountToRemove);
            }
        }
        #endregion
    }
}
