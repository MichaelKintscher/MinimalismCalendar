using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.EventArguments
{
    /// <summary>
    /// Contains event info for when an OAuth code is acquired for a service to connect.
    /// </summary>
    public class OauthCodeAcquiredEventArgs : EventArgs
    {
        /// <summary>
        /// The name of the service requesting to be connected.
        /// </summary>
        public string ServiceName { get; private set; }

        /// <summary>
        /// The OAuth code acquired.
        /// </summary>
        public string Code { get; private set; }

        public OauthCodeAcquiredEventArgs(string serviceName, string code)
        {
            this.ServiceName = serviceName;
            this.Code = code;
        }
    }
}
