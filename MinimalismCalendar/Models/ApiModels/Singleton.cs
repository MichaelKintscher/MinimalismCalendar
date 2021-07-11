using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.Models.ApiModels
{
    /// <summary>
    /// Provides a base class that implements the singleton design
    /// pattern using an Instance property.
    /// </summary>
    /// <typeparam name="T">The type of the singelton, which must implement a public parameterless constructor.</typeparam>
    public abstract class Singleton<T> where T : new()
    {
        /// <summary>
        /// The static reference to the only (singleton) instance of the class.
        /// </summary>
        private static readonly Lazy<T> _mySingleton = new Lazy<T>(() => new T());

        /// <summary>
        /// Returns the single instance of the class.
        /// </summary>
        public static T Instance
        {
            get => Singleton<T>._mySingleton.Value;
        }
    }
}
