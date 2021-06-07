﻿using MinimalismCalendar.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private int dayOfMonth;
        /// <summary>
        /// The day of the month the agenda control is set to.
        /// </summary>
        public int DayOfMonth
        {
            get => this.dayOfMonth;
            set
            {
                this.dayOfMonth = value;
                this.RaisePropertyChanged("DayOfMonth");
            }
        }

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

        private List<CalendarEvent> events { get; set; }
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
            this.events = new List<CalendarEvent>();
        }

        #region Event Handlers
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // FOR TESTING PURPOSES ONLY!!!
            this.AddEvents(TestDataGenerator.GetTestEvents());
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds the given list of events to the agenda view.
        /// </summary>
        /// <param name="events">The list of calendar events to add to the agenda view.</param>
        public void AddEvents(List<CalendarEvent> events)
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
        public void AddEvent(CalendarEvent calEvent)
        {
            // Create the UI for the event.
            TextBlock eventTextBlock = new TextBlock()
            {
                Text = calEvent.Name
            };

            // Wrap the UI in a border for color.
            Border border = new Border();
            border.Child = eventTextBlock;
            border.Background = new SolidColorBrush(Colors.Pink);

            // Calculate the height of the event UI.
            border.Height = this.AgendaTimeUnitHeight * (calEvent.End - calEvent.Start).TotalHours;

            // Calculate the start position of the UI.
            double verticalOffset = this.AgendaTimeUnitHeight * calEvent.Start.TimeOfDay.TotalHours;
            Canvas.SetTop(border, verticalOffset);

            // Add the UI to the canvas.
            this.AgendaCanvas.Children.Add(border);

            this.eventBlocks.Add(border);
        }
        #endregion
    }
}
