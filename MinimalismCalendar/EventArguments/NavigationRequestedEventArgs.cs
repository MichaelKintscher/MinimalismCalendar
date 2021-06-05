using MinimalismCalendar.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.EventArguments
{
    /// <summary>
    /// Contains event info for a navigation request from the MainPage.NavigationRequested event.
    /// </summary>
    public class NavigationRequestedEventArgs : EventArgs
    {
        /// <summary>
        /// The enum value of the page being reqested as the target of the navigation.
        /// </summary>
        public PageTypes ToPage { get; private set; }

        /// <summary>
        /// A flag indicating whether this navigation request is a back navigation request or not.
        /// </summary>
        public bool IsBackRequest { get; private set; }

        public NavigationRequestedEventArgs(PageTypes toPage, bool isBackRequest)
        {
            this.ToPage = toPage;
            this.IsBackRequest = isBackRequest;
        }
    }
}
