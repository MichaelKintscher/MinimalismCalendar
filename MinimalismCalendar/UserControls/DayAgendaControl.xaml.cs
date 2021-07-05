using MinimalismCalendar.Models;
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
using Windows.UI;
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
    /// A control for displaying an agenda view of a single day.
    /// </summary>
    public sealed partial class DayAgendaControl : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// A list to track all of the event TextBlocks on the agenda.
        /// </summary>
        private List<Border> eventBlocks;

        #region Properties
        public DateTime Date { get; set; }

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

        /// <summary>
        /// The list of events the control can display from.
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

        public DayAgendaControl()
        {
            this.InitializeComponent();

            // Initialize the collections.
            this.eventBlocks = new List<Border>();
            this.CalendarEvents = new ObservableCollection<CalendarEvent>();

            // Subscribe to the changed event for the Calendar Events collection.
            this.CalendarEvents.CollectionChanged += this.CalendarEventsChanged;
        }

        #region Event Handlers
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
            this.AddEvents(this.CalendarEvents.ToList());
        }
        #endregion

        #region Methods
        /// <summary>
        /// Clears the events from the control.
        /// </summary>
        public void Clear()
        {
            // Remove all of the event blocks from the canvas.
            this.eventBlocks.ForEach(b => this.AgendaCanvas.Children.Remove(b));

            // Now clear the event block references.
            this.eventBlocks.Clear();

            // Finally, clear the calendar events list.
            this.CalendarEvents.Clear();
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Adds the given list of events to the agenda view. ONLY CALL THIS AFTER THE LOADED EVENT HAS FIRED.
        /// </summary>
        /// <param name="events">The list of calendar events to add to the agenda view.</param>
        private void AddEvents(List<CalendarEvent> events)
        {
            foreach (CalendarEvent calEvent in events)
            {
                this.AddEvent(calEvent);
            }
        }

        /// <summary>
        /// Adds the given event to the agenda view.
        /// </summary>
        /// <param name="calEvent">The calendar event to add to the agenda view.</param>
        private void AddEvent(CalendarEvent calEvent)
        {
            // Create the UI for the event.
            TextBlock eventTextBlock = new TextBlock()
            {
                Text = calEvent.Name,
                Margin = new Thickness(10, 5, 0, 0)
            };

            // Wrap the UI in a border for color.
            Border border = new Border();
            border.Child = eventTextBlock;
            border.Background = new SolidColorBrush(Colors.DarkGray);

            // Calculate the height of the event UI.
            border.Height = this.AgendaTimeUnitHeight * (calEvent.End - calEvent.Start).TotalHours;

            // Calculate the start position of the UI.
            //double verticalOffset = this.AgendaTimeUnitHeight * calEvent.Start.TimeOfDay.TotalHours;
            //Canvas.SetTop(border, verticalOffset);

            int row = (int)Math.Floor(calEvent.Start.TimeOfDay.TotalHours);
            double verticalOffset = this.AgendaTimeUnitHeight * (calEvent.Start.TimeOfDay.TotalHours - row);
            Grid.SetRow(border, row);
            border.Margin = new Thickness(0, verticalOffset, 0, 0);

            // Add the UI to the canvas.
            this.AgendaCanvas.Children.Add(border);

            this.eventBlocks.Add(border);

            // Use this line to observe unecessary repetitions of this event call.
            System.Diagnostics.Debug.WriteLine("AddEvent executed from " + this.Name +" control for event name: " + calEvent.Name + " at ROW " + row + " with vertical offset " + verticalOffset);
        }
        #endregion
    }
}
