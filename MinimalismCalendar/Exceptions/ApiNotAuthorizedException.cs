using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.Exceptions
{
    /// <summary>
    /// Represents an error when an API call was made to an API without proper authorization.
    /// </summary>
    public class ApiNotAuthorizedException : Exception
    {
        /// <summary>
        /// The name of the API that thre the exception.
        /// </summary>
        public string ApiName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ApiNotAuthorizedException class.
        /// </summary>
        public ApiNotAuthorizedException()
            : base()
        {

        }

        /// <summary>
        /// Initializes a new instance of the ApiNotAuthorizedException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ApiNotAuthorizedException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the ApiNotAuthorizedException class with the name of the API that generated the error and a specified error message.
        /// </summary>
        /// <param name="apiName">The name of the API that generated the error.</param>
        /// <param name="message">The message that describes the error.</param>
        public ApiNotAuthorizedException(string apiName, string message)
            : base (message)
        {
            this.ApiName = apiName;
        }

        /// <summary>
        /// Initializes a new instance of the ApiNotAuthorizedException class with the name of the API that generated the error,
        /// a specified error message, and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="apiName">The name of the API that generated the error.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ApiNotAuthorizedException(string apiName, string message, Exception innerException)
            : base(message, innerException)
        {
            this.ApiName = apiName;
        }
    }
}
