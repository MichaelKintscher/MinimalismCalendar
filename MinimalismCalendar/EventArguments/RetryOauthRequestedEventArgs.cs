using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.EventArguments
{
    /// <summary>
    /// Contains event info for request to retry connecting to a service.
    /// </summary>
    public class RetryOauthRequestedEventArgs : ConnectServiceRequestedEventArgs
    {
        public RetryOauthRequestedEventArgs(string serviceName) : base(serviceName)
        {
            // Simply passes the service name to the inhereted constructor.
        }
    }
}
