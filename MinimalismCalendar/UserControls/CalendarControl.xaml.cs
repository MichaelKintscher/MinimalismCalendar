using MinimalismCalendar.Utility;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MinimalismCalendar.UserControls
{
    /// <summary>
    /// Control for displaying detailed calendar data.
    /// </summary>
    public sealed partial class CalendarControl : UserControl, INotifyPropertyChanged
    {
        #region Constants
        private List<TextBlock> DayOfMonthTextBlocks;
        #endregion

        #region Properties
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
                this.RaisePropertyChanged("TimeUnitHeight");
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
        #endregion

        public CalendarControl()
        {
            this.InitializeComponent();

            // Initialize the height of the agenda time units.
            this.AgendaTimeUnitHeight = 50;

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

            this.SetToThisWeek();
        }

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
            DateTime sunday = DateUtility.GetPreviousSunday(date);

            // For each day in the week...
            for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
            {
                // Set the day of the month blocks.
                this.DayOfMonthTextBlocks[dayOfWeek].Text = sunday.AddDays(dayOfWeek).Day.ToString();
            }
        }
        #endregion
    }
}
