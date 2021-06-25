using MinimalismCalendar.EventArguments;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
        #endregion

        public SettingsPage()
        {
            this.InitializeComponent();
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
        #endregion
    }
}
