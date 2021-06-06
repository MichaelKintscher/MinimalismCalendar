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
    public sealed partial class CalendarControl : UserControl, INotifyPropertyChanged
    {
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
        }
    }
}
