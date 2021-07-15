using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.EventArguments
{
    /// <summary>
    /// Contains event info for when an API event is raised.
    /// </summary>
    public class ApiEventArgs : EventArgs
    {
        /// <summary>
        /// The name of the Api the event was raised for.
        /// </summary>
        public string ApiName { get; private set; }

        public ApiEventArgs(string apiName)
        {
            this.ApiName = apiName;
        }
    }
}
