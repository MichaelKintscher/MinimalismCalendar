using MinimalismCalendar.EventArguments;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    }
}
