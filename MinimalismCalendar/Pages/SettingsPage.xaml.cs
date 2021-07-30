using MinimalismCalendar.EventArguments;
using MinimalismCalendar.Managers;
using MinimalismCalendar.Models;
using MinimalismCalendar.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MinimalismCalendar.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page, INotifyPropertyChanged
    {
        #region Properties
        private string googleAuthStatus;
        /// <summary>
        /// The connection status for the Google calendar API.
        /// </summary>
        public string GoogleAuthStatus
        {
            get => this.googleAuthStatus;
            set
            {
                this.googleAuthStatus = value;
                this.RaisePropertyChanged("GoogleAuthStatus");
            }
        }

        private bool internetConnectionAvailable;
        /// <summary>
        /// Whether an internet connection is currently available.
        /// </summary>
        public bool InternetConnectionAvailable
        {
            get => this.internetConnectionAvailable;
            set
            {
                this.internetConnectionAvailable = value;
                this.RaisePropertyChanged("InternetConnectionAvailable");
            }
        }

        public ObservableCollection<CalendarProviderAccount> Accounts { get; set; }

        /// <summary>
        /// A list of calendars that are not displayed, but are available to display.
        /// </summary>
        public ObservableCollection<Calendar> HiddenCalendars { get; set; }

        /// <summary>
        /// A list of calendars that are displayed.
        /// </summary>
        public ObservableCollection<Calendar> VisibleCalendars { get; set; }
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Raise the PropertChanged event for the given property name.
        /// </summary>
        /// <param name="name">Name of the property changed.</param>
        public void RaisePropertyChanged(string name)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public delegate void ConnectServiceRequestedHandler(object sender, ConnectServiceRequestedEventArgs e);
        /// <summary>
        /// Raised when the a request is issued to connect a service.
        /// </summary>
        public event ConnectServiceRequestedHandler ConnectServiceRequested;
        private void RaiseConnectServiceRequested(string serviceName)
        {
            // Create the args and call the listening event handlers, if there are any.
            ConnectServiceRequestedEventArgs args = new ConnectServiceRequestedEventArgs(serviceName);
            this.ConnectServiceRequested?.Invoke(this, args);
        }

        public delegate void OauthCodeAcquiredHandler(object sender, OauthCodeAcquiredEventArgs e);
        /// <summary>
        /// Raised when the a request is issued to connect a service.
        /// </summary>
        public event OauthCodeAcquiredHandler OauthCodeAcquired;
        private void RaiseOauthCodeAcquired(string serviceName, string code)
        {
            // Create the args and call the listening event handlers, if there are any.
            OauthCodeAcquiredEventArgs args = new OauthCodeAcquiredEventArgs(serviceName, code);
            this.OauthCodeAcquired?.Invoke(this, args);
        }

        public delegate void RetryOauthRequestedHandler(object sender, RetryOauthRequestedEventArgs e);
        /// <summary>
        /// Raised when the a request is issued to connect a service.
        /// </summary>
        public event RetryOauthRequestedHandler RetryOauthRequested;
        private void RaiseRetryOauthRequested(string serviceName)
        {
            // Create the args and call the listening event handlers, if there are any.
            RetryOauthRequestedEventArgs args = new RetryOauthRequestedEventArgs(serviceName);
            this.RetryOauthRequested?.Invoke(this, args);
        }

        public delegate void CalendarVisibilityChangedHandler(object sender, CalendarVisibilityChangedEventArgs e);
        /// <summary>
        /// Raised when a calendar's visibility changed.
        /// </summary>
        public event CalendarVisibilityChangedHandler CalendarVisibilityChanged;
        private void RaiseCalendarVisibilityChanged(string calendarName, CalendarVisibility newVisibility)
        {
            // Create the args and call the listening event handlers, if there are any.
            CalendarVisibilityChangedEventArgs args = new CalendarVisibilityChangedEventArgs(calendarName, newVisibility);
            this.CalendarVisibilityChanged?.Invoke(this, args);
        }
        #endregion

        public SettingsPage()
        {
            this.InitializeComponent();

            // Initialize the collections.
            this.HiddenCalendars = new ObservableCollection<Calendar>();
            this.VisibleCalendars = new ObservableCollection<Calendar>();
        }

        #region Event Handlers
        /// <summary>
        /// Handles the button click to connect Google's services.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AuthenticateGoogleButton_Click(object sender, RoutedEventArgs e)
        {
            this.RaiseConnectServiceRequested("Google");
        }

        /// <summary>
        /// Handles when the user starts to drag items on a listview.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            // For each item in the items being dragged...
            StringBuilder builder = new StringBuilder();
            foreach (Calendar calendar in e.Items)
            {
                // Put each item on a new line (new line not necessary for the first item).
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                // Append the item.
                builder.Append(CalendarManager.ConvertToJson(calendar).ToString());
            }

            // Set the content and requested operation of the data package.
            e.Data.SetText(builder.ToString());
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }

        /// <summary>
        /// Handles when a user drags items over a list view. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        /// <summary>
        /// Handles when a user drops items on a list view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ListView_Drop(object sender, DragEventArgs e)
        {
            // Get a reference to the target list view (the one the items
            //      are being dropped onto).
            ListView target = (ListView)sender;

            // Get a reference to the source and destination data collections.
            ListView source;
            ObservableCollection<Calendar> sourceCollection;
            ObservableCollection<Calendar> destinationCollection;
            if (target.Name == "HiddenCalendarsListView")
            {
                source = this.VisibleCalendarsListView;
                sourceCollection = this.VisibleCalendars;
                destinationCollection = this.HiddenCalendars;
            }
            else
            {
                source = this.HiddenCalendarsListView;
                sourceCollection = this.HiddenCalendars;
                destinationCollection = this.VisibleCalendars;
            }

            // If the drag and drop contains text...
            if (e.DataView.Contains(StandardDataFormats.Text))
            {
                // Get a deferral, to support async drag and drop.
                DragOperationDeferral def = e.GetDeferral();

                // Get the string from the dropped data package, and split
                //      the lines out.
                string dataString = await e.DataView.GetTextAsync();
                string[] items = dataString.Split('\n');

                // For each dropped item...
                foreach (string item in items)
                {
                    // Create a calendar object from the item string.
                    Calendar calendar = CalendarManager.ParseCalendarJson(item);

                    // Find the insertion index
                    int index = DragDropHelper.GetDropInsertionIndex(e, target);

                    // Insert the dropped item at the insertion index.
                    destinationCollection.Insert(index, calendar);

                    // Remove the dropped items from the source list.
                    foreach (Calendar calItem in source.Items)
                    {
                        if (CalendarManager.Equals(calItem, calendar))
                        {
                            sourceCollection.Remove(calItem);
                            break;
                        }
                    }

                    // Raise the calendar visibility changed event.
                    CalendarVisibility newVisibility = (target.Name == "HiddenCalendarsListView") ? CalendarVisibility.Hidden : CalendarVisibility.Visible;
                    this.RaiseCalendarVisibilityChanged(calendar.Name, newVisibility);
                }

                // Complete the deferral.
                e.AcceptedOperation = DataPackageOperation.Move;
                def.Complete();
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Shows the service OAuth Code dialog.
        /// </summary>
        /// <param name="serviceName">The name of the service to show the dialog box for.</param>
        /// <returns></returns>
        public async Task ShowServiceOAuthCodeUIAsync(string serviceName)
        {
            // Show the OAuth code dialog box, and get a response from the user.
            var result = await this.FinishAddingServiceDialog.ShowAsync();

            switch (result)
            {
                case ContentDialogResult.None:
                    // Nothing to do... just close the dialog.
                    break;
                case ContentDialogResult.Primary:
                    // Raise the code acquired event.
                    this.RaiseOauthCodeAcquired(serviceName, this.ServiceOauthCodeTextBox.Text);
                    break;
                case ContentDialogResult.Secondary:
                    // Nothing to do... just close the dialog.
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Shows the error UI.
        /// </summary>
        /// <param name="serviceName">The name of the service attempting to be authenticated.</param>
        /// <param name="errorMessage">The error message to display.</param>
        /// <returns></returns>
        public async Task ShowOAuthErrorUIAsync(string serviceName, string errorMessage)
        {
            // Set the error message.
            this.OAuthErrorTextBlock.Text = errorMessage;

            var result = await this.OauthErrorDialog.ShowAsync();

            switch (result)
            {
                case ContentDialogResult.None:
                    // Nothing to do... just close the dialog.
                    break;
                case ContentDialogResult.Primary:
                    // Raise the retry OAuth request event.
                    this.RaiseRetryOauthRequested(serviceName);
                    break;
                case ContentDialogResult.Secondary:
                    // Nothing to do... just close the dialog.
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Adds the given calendars to the list of calendars corresponding with the given visibility.
        /// </summary>
        /// <param name="calendars">The list of calendars to add.</param>
        /// <param name="visibility">The visibility to add the given list of calendars to.</param>
        public void AddCalendars(List<Calendar> calendars, CalendarVisibility visibility)
        {
            // Depending on the visibility given...
            switch (visibility)
            {
                case CalendarVisibility.Visible:
                    // Add each calendar to the displayed calendars.
                    calendars.ForEach(c => this.VisibleCalendars.Add(c));
                    break;
                case CalendarVisibility.Hidden:
                    // Add each calendar to the available calendars.
                    calendars.ForEach(c => this.HiddenCalendars.Add(c));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Clears the calendars lists.
        /// </summary>
        public void ClearCalenderLists()
        {
            this.HiddenCalendars.Clear();
            this.VisibleCalendars.Clear();
        }
        #endregion
    }
}
