using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.Models.ApiModels
{
    /// <summary>
    /// Represents a client app's API credentials.
    /// </summary>
    public class ApiCredential
    {
        /// <summary>
        /// The client app's ID for an API.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The client app's secret for an API.
        /// </summary>
        public string ClientSecret { get; set; }
    }
}
