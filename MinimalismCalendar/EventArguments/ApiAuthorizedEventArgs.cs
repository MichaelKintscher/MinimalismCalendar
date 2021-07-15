﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.EventArguments
{
    /// <summary>
    /// Contains event info for when an API is authorized.
    /// </summary>
    public class ApiAuthorizedEventArgs : ApiEventArgs
    {
        /// <summary>
        /// Whether the Api was successfully authorized.
        /// </summary>
        public bool AuthorizationSuccess { get; private set; }

        public ApiAuthorizedEventArgs(string apiName, bool authorizationSuccess)
            : base(apiName)
        {
            this.AuthorizationSuccess = authorizationSuccess;
        }
    }
}
