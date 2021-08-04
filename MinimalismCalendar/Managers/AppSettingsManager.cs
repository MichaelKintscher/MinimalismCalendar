using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace MinimalismCalendar.Managers
{
    /// <summary>
    /// Contains methods for manipulating the app settings.
    /// </summary>
    public static class AppSettingsManager
    {
        /// <summary>
        /// Whether the app should resume the last viewed position on the calendar upon app launch.
        /// </summary>
        public static bool ResumeLastViewedOnLaunch
        {
            get => (ApplicationData.Current.LocalSettings.Values["ResumeLastViewedOnLaunch"] as bool?).HasValue ?
                        (ApplicationData.Current.LocalSettings.Values["ResumeLastViewedOnLaunch"] as bool?).Value :
                        false;
            set
            {
                ApplicationData.Current.LocalSettings.Values["ResumeLastViewedOnLaunch"] = value;
            }
        }
    }
}
