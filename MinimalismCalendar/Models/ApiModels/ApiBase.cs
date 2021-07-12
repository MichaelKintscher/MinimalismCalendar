using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

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

        protected HttpClient Client { get; set; }

        #region Constructors
        public ApiBase()
        {
            this.Client = new HttpClient();
        }
        #endregion

        /// <summary>
        /// Makes a POST to the given URI with the given parameters.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual async Task<string> PostAsync(string uri, IList<KeyValuePair<string, string>> parameters)
        {
            HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(parameters);

            // Use http POST to get a response from the token endpoint.
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await this.Client.PostAsync(new Uri(uri), content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in HTTP response: " + ex.Message);
            }

            // No exceptions were thrown, so parse the response message.
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Makes a GET to the given URI with the given parameters.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual async Task<string> GetAsync(string uri)
        {
            // Use http GET to get a response from the token endpoint.
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await this.Client.GetAsync(new Uri(uri));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in HTTP response: " + ex.Message);
            }

            // No exceptions were thrown, so parse the response message.
            return await response.Content.ReadAsStringAsync();
        }
    }
}
