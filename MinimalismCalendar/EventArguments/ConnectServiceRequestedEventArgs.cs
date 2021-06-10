using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.EventArguments
{
    /// <summary>
    /// Contains event info for a request to connect a service.
    /// </summary>
    public class ConnectServiceRequestedEventArgs : EventArgs
    {
        /// <summary>
        /// The name of the service requesting to be connected.
        /// </summary>
        public string ServiceName { get; private set; }

        public ConnectServiceRequestedEventArgs(string serviceName)
        {
            this.ServiceName = serviceName;
        }
    }
}
