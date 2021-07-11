using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.Models.ApiModels
{
    /// <summary>
    /// Represents an external API, using the Singleton design pattern.
    /// Use the Instance property to access.
    /// </summary>
    /// <typeparam name="T">The type of ApiBase, which must implement a public parameterless constructor.</typeparam>
    public class ApiBase<T> : Singleton<T> where T : new()
    {
        /// <summary>
        /// The name of the API.
        /// </summary>
        public string Name { get; set; }
    }
}
