using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.EventArguments
{
    /// <summary>
    /// Contains event info for when the show at launch setting is changed.
    /// </summary>
    public class ShowAtLaunchSettingChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Whether the setting was set to resume last viewed or not.
        /// </summary>
        public bool ResumeLastViewed { get; private set; }

        public ShowAtLaunchSettingChangedEventArgs(bool resumeLastViewed)
        {
            this.ResumeLastViewed = resumeLastViewed;
        }
    }
}
