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
    /// Control for displaying calendar status data.
    /// </summary>
    public sealed partial class CalendarStatusViewControl : UserControl, INotifyPropertyChanged
    {
        #region Properties
        private string calendarName;
        /// <summary>
        /// The name of the calendar being represented by this control.
        /// </summary>
        public string CalendarName
        {
            get => this.calendarName;
            set
            {
                this.calendarName = value;
                this.RaisePropertyChanged("CalendarName");
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

        public CalendarStatusViewControl()
        {
            this.InitializeComponent();
        }
    }
}
