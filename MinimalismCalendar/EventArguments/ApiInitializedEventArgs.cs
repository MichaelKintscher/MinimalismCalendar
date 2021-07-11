using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.EventArguments
{
    /// <summary>
    /// Contains event info for when an API is initialized.
    /// </summary>
    public class ApiInitializedEventArgs : EventArgs
    {
        /// <summary>
        /// The name of the Api that was initialized.
        /// </summary>
        public string ApiName { get; private set; }

        public ApiInitializedEventArgs(string apiName)
        {
            this.ApiName = apiName;
        }
    }
}
