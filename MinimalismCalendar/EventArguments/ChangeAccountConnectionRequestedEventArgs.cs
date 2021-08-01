using MinimalismCalendar.Models.AppConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.EventArguments
{
    /// <summary>
    /// Contains event info for a request to change a service's connection.
    /// </summary>
    public class ChangeAccountConnectionRequestedEventArgs : EventArgs
    {
        /// <summary>
        /// The ID of the account associated with the connection change request. Null for new account connections.
        /// </summary>
        public string AccoutId { get; private set; }
        /// <summary>
        /// The calendar service provider for the account.
        /// </summary>
        public CalendarProvider CalendarProvider { get; set; }
        /// <summary>
        /// The connection change requested.
        /// </summary>
        public ConnectionAction Action { get; private set; }

        public ChangeAccountConnectionRequestedEventArgs(string accountId, CalendarProvider calendarProvider, ConnectionAction action)
        {
            this.AccoutId = accountId;
            this.CalendarProvider = calendarProvider;
            this.Action = action;
        }
    }

    public enum ConnectionAction
    {
        Connect,
        RetryConnect,
        Disconnect
    }
}
