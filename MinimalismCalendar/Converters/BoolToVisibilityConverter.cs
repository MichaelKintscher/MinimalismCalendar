using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace MinimalismCalendar.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public Visibility TrueVisibility { get; set; }
        public Visibility FalseVisibility { get; set; }

        public BoolToVisibilityConverter()
        {
            this.TrueVisibility = Visibility.Visible;
            this.TrueVisibility = Visibility.Collapsed;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool boolValue = (bool)value;
            return boolValue ? this.TrueVisibility : this.FalseVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool convertedValue;

            if (value is Visibility == false)
            {
                return DependencyProperty.UnsetValue;
            }

            if ((Visibility)value == this.TrueVisibility)
            {
                convertedValue = true;
            }
            else
            {
                convertedValue = false;
            }

            return convertedValue;
        }
    }
}
