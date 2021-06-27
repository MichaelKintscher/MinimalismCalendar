using MinimalismCalendar.Models;
using MinimalismCalendar.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MinimalismCalendar.UserControls
{
    /// <summary>
    /// Control for displaying detailed calendar data.
    /// </summary>
    public sealed partial class CalendarControl : UserControl, INotifyPropertyChanged
    {
        #region Constants
        /// <summary>
        /// List of the Day of Month textblocks for easy iteration access.
        /// </summary>
        private List<TextBlock> DayOfMonthTextBlocks;
        /// <summary>
        /// List of the Day Agenda controls for easy iteration access.
        /// </summary>
        private List<DayAgendaControl> DayAgendaControls;
        #endregion

        #region Properties
        private DateTime sunday { get; set; }

        private double agendaTimeUnitHeight;
        /// <summary>
        /// Height of each vertical unit of time on the agenda view.
        /// </summary>
        public double AgendaTimeUnitHeight
        {
            get => this.agendaTimeUnitHeight;
            set
            {
                this.agendaTimeUnitHeight = value;
                this.RaisePropertyChanged("AgendaTimeUnitHeight");
            }
        }

        private string monthYearText;
        private string MonthYearText
        {
            get => this.monthYearText;
            set
            {
                this.monthYearText = value;
                this.RaisePropertyChanged("MonthYearText");
            }
        }

        /// <summary>
        /// The list of calendar events the control CAN display. (The control is not
        /// necessarily actively displaying all at once - the control manages the
        /// current view and displays events from this list contained within the scope
        /// of that view, such as a week view).
        /// </summary>
        public ObservableCollection<CalendarEvent> CalendarEvents { get; private set; }
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
        #endregion

        public CalendarControl()
        {
            this.InitializeComponent();

            // Initialize the properties.
            this.AgendaTimeUnitHeight = 50;
            this.CalendarEvents = new ObservableCollection<CalendarEvent>();

            // Subscribe to the changed event for the Calendar Events collection.
            this.CalendarEvents.CollectionChanged += this.CalendarEventsChanged;

            // Initialize the day of month textblocks.
            this.DayOfMonthTextBlocks = new List<TextBlock>()
            {
                this.DayOfMonth0TextBlock,
                this.DayOfMonth1TextBlock,
                this.DayOfMonth2TextBlock,
                this.DayOfMonth3TextBlock,
                this.DayOfMonth4TextBlock,
                this.DayOfMonth5TextBlock,
                this.DayOfMonth6TextBlock
            };

            // Initialize the day agena controls list.
            this.DayAgendaControls = new List<DayAgendaControl>()
            {
                this.Day0AgendaControl,
                this.Day1AgendaControl,
                this.Day2AgendaControl,
                this.Day3AgendaControl,
                this.Day4AgendaControl,
                this.Day5AgendaControl,
                this.Day6AgendaControl
            };

            this.SetToThisWeek();
        }

        #region Event Handlers
        /// <summary>
        /// Handles the loaded event for the Day Agenda controls to complete setting up the data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DayAgendaControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DayAgendaControl control)
            {
                this.RefreshDayAgendaControl(control);
            }
        }

        /// <summary>
        /// Handles the Collection Changed event for the calendar events collection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalendarEventsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Refresh all of the day adenga views.
            //      NOTE: This can be optimized to only refresh the necessary views - those
            //      displaying the affected calendar events.
            this.RefreshDayAgendaControls(this.DayAgendaControls);
        }

        /// <summary>
        /// Handles when the user taps the button to go back a time step.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackTimeStepButton_Click(object sender, RoutedEventArgs e)
        {
            // Set the control to the previous week.
            this.SetToWeek(this.sunday.AddDays(-7));
        }

        /// <summary>
        /// Handles when the user taps the button to go forward a time step.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ForwardTimeStepButton_Click(object sender, RoutedEventArgs e)
        {
            // Set the control to the next week.
            this.SetToWeek(this.sunday.AddDays(7));
        }

        /// <summary>
        /// Handles when the user taps the button to return the view to today.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GoToTodayButton_Click(object sender, RoutedEventArgs e)
        {
            // Set the control to today.
            this.SetToWeek(DateTime.Now);
        }

        /// <summary>
        /// Handles when the selected date in the calendar view flyout is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CalendarView_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            // Get the newly selected date.
            DateTimeOffset selectedDateTime = sender.SelectedDates.FirstOrDefault();
            if (selectedDateTime != null)
            {
                // Set the control to the selected date.
                this.SetToWeek(selectedDateTime.Date);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sets the calendar control to display the current week.
        /// </summary>
        public void SetToThisWeek()
        {
            // Set the control to the current week.
            this.SetToWeek(DateTime.Now);
        }

        /// <summary>
        /// Sets the calendar control to display the week containing the given date.
        /// </summary>
        /// <param name="date">The date contained in the week to set the control to.</param>
        public void SetToWeek(DateTime date)
        {
            this.sunday = DateUtility.GetPreviousSunday(date);

            // For each day in the week...
            for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
            {
                // Set the day of the month blocks.
                this.DayOfMonthTextBlocks[dayOfWeek].Text = this.sunday.AddDays(dayOfWeek).Day.ToString();
            }

            // Update the month year text.
            this.MonthYearText = DateUtility.GetMonthName(this.sunday.Month) + " " + sunday.Year;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Refreshes the given Day Agenda controls.
        /// </summary>
        /// <param name="controls">A list of Day Agenda controls to refresh.</param>
        private void RefreshDayAgendaControls(List<DayAgendaControl> controls)
        {
            foreach (DayAgendaControl control in controls)
            {
                this.RefreshDayAgendaControl(control);
            }
        }

        /// <summary>
        /// Refreshes the given Day Agenda control.
        /// </summary>
        /// <param name="control">The day agenda control to refresh.</param>
        private void RefreshDayAgendaControl(DayAgendaControl control)
        {
            // Clear the existing events.
            control.CalendarEvents.Clear();

            // Add the updated list of events to the control.
            //      NOTE: this can be optimized in two ways:
            //          1) only refresh the changed events
            //          2) only provide a list of events for the day the control is displaying
            foreach (CalendarEvent calEvent in this.CalendarEvents.ToList())
            {
                control.CalendarEvents.Add(calEvent);
            }
        }
        #endregion
    }
}
