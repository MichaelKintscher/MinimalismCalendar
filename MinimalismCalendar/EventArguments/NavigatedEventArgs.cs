using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace MinimalismCalendar.EventArguments
{
    /// <summary>
    /// Contains event info for when a navigation has completed from the MainPage.Navigationed event.
    /// </summary>
    public class NavigatedEventArgs
    {
        /// <summary>
        /// The page that was navigated from.
        /// </summary>
        public Page PageNavigatedFrom { get; private set; }

        /// <summary>
        /// The page that was navigated to.
        /// </summary>
        public Page PageNavigatedTo { get; private set; }

        public NavigatedEventArgs(Page pageNavigatedFrom, Page pageNavigatedTo)
        {
            this.PageNavigatedFrom = pageNavigatedFrom;
            this.PageNavigatedTo = pageNavigatedTo;
        }
    }
}
