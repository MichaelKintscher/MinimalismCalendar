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
        /// The name of the service requesting to be connected.
        /// </summary>
        public string ServiceName { get; private set; }

        /// <summary>
        /// The connection change requested.
        /// </summary>
        public ConnectionAction Action { get; private set; }

        public ChangeAccountConnectionRequestedEventArgs(string serviceName, ConnectionAction action)
        {
            this.ServiceName = serviceName;
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
