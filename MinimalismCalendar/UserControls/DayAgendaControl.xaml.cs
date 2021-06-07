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
    /// A control for displaying an agenda view of a single day.
    /// </summary>
    public sealed partial class DayAgendaControl : UserControl, INotifyPropertyChanged
    {
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
        }
    }
}
