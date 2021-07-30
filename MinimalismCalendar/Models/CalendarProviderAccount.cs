using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.Models
{
    /// <summary>
    /// Represents an account with a calendar service provider.
    /// </summary>
    public class CalendarProviderAccount
    {
        /// <summary>
        /// The calnedar service providers.
        /// </summary>
        public CalendarProvider Provider { get; set; }

        /// <summary>
        /// The name for the account given by the user. Must be unique.
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// The username for the account with the calendar service provider.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Whether the account is connected.
        /// </summary>
        public bool Connected { get; set; }

        /// <summary>
        /// When data from this account was last synced.
        /// </summary>
        public DateTime LastSynced { get; set; }
    }

    /// <summary>
    /// Represents calendar providers supported by the app.
    /// </summary>
    public enum CalendarProvider
    {
        Google
    }
}
