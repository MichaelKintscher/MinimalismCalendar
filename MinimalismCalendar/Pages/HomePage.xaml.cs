using MinimalismCalendar.EventArguments;
using MinimalismCalendar.Models;
using System;
using System.Collections.Generic;
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
    public sealed partial class HomePage : Page
    {
        #region Events
        public delegate void CalendarViewChangedHandler(object sender, CalendarViewChangedEventArgs e);
        /// <summary>
        /// Raised when the calendar view changes.
        /// </summary>
        public event CalendarViewChangedHandler CalendarViewChanged;
        private void RaiseCalendarViewChanged(DateTime newSunday)
        {
            // Create the args and call the listening event handlers, if there are any.
            CalendarViewChangedEventArgs args = new CalendarViewChangedEventArgs(newSunday);
            this.CalendarViewChanged?.Invoke(this, args);
        }
        #endregion

        public HomePage()
        {
            this.InitializeComponent();
        }

        #region Event Handlers
        /// <summary>
        /// Handles when the user taps on the splitview pane toggle button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SplitViewPaneToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle the main split view's pane.
            this.MainSplitView.IsPaneOpen = !this.MainSplitView.IsPaneOpen;
        }

        /// <summary>
        /// Handles when the calendar view changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalendarControl_ViewChanged(object sender, CalendarViewChangedEventArgs e)
        {
            // Raise the calendar view changed event.
            this.RaiseCalendarViewChanged(e.NewSunday);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Initializes the calendar control to show the given date and list of events.
        /// </summary>
        /// <param name="visibleDate">The date to show on the calendar control.</param>
        /// <param name="events">The list of events to display on the calendar.</param>
        public void InitializeCalendarControl(DateTime visibleDate, List<CalendarEvent> events)
        {
            // Add the events to the calendar control's list.
            foreach (CalendarEvent calEvent in events)
            {
                this.CalendarControl.CalendarEvents.Add(calEvent);
            }

            // Set the calendar control to show the given date.
            this.CalendarControl.SetToWeek(visibleDate);
        }
        #endregion
    }
}
