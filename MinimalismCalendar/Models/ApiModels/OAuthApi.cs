using MinimalismCalendar.EventArguments;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Web.Http;
using Xamarin.Essentials;

namespace MinimalismCalendar.Models.ApiModels
{
    /// <summary>
    /// Implements OAuth 2.0 flows for an external API, using the Singleton design pattern.
    /// Use the Instance property to access.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class OAuthApi<T> : ApiBase<T> where T : new()
    {
        /// <summary>
        /// The authorization endpoint.
        /// </summary>
        private string OAuthEndPoint { get; set; }

        private string OAuthTokenEndpoint { get; set; }

        private string OAuthRedirectUri { get; set; }

        private string CredentialsFilePath { get; set; }

        /// <summary>
        /// A cache of the token data for the API.
        /// </summary>
        private OAuthToken tokenData { get; set; }

        /// <summary>
        /// Returns whether the token exists and has expired, or exists but is missing an expiration limit.
        /// </summary>
        private bool TokenExpired
        {
            // There has to be a token data, AND EITHER
            //      there is no expiration time OR
            //      there is an expiration time and it has passed.
            get => this.IsAuthorized &&
                (this.tokenData.ExpiresInSeconds.HasValue == false ||
                    DateTime.Compare(DateTime.UtcNow, this.tokenData.IssuedUtc.AddSeconds(this.tokenData.ExpiresInSeconds.Value)) >= 0);
        }

        private bool isInitialized;
        /// <summary>
        /// Returns whether the API is initialized.
        /// </summary>
        public bool IsInitialized
        {
            get => this.isInitialized;
            set
            {
                this.isInitialized = value;

                // Only raise the initialized event if this is set to true.
                if (value)
                {
                    this.RaiseInitialized(this.Name);
                    System.Diagnostics.Debug.WriteLine("API Initialized!");
                }
            }
        }

        /// <summary>
        /// Returns whether the user has authorized the app with the API.
        /// </summary>
        public bool IsAuthorized
        {
            // The token data property contains a value if the app has previously been authorized.
            get => this.tokenData != null;
        }

        #region Delegate Functions
        private Func<ApiCredential> LoadCredentialsFromFile;
        private Func<ApiCredential, string> GetOAuthQueryString;
        private Func<string, string, ApiCredential, IList<KeyValuePair<string, string>>> GetTokenExchangeParams;
        private Func<string, OAuthToken> ConvertResponseToToken;
        private Func<string, ApiCredential, IList<KeyValuePair<string, string>>> GetTokenRefreshParams;
        private Func<OAuthToken, string> ConvertTokenToJsonString;
        #endregion

        #region Events
        public delegate void InitializedHandler(object sender, ApiEventArgs e);
        /// <summary>
        /// Raised when the API is initialized.
        /// </summary>
        public event InitializedHandler Initialized;
        private void RaiseInitialized(string apiName)
        {
            // Create the args and call the listening event handlers, if there are any.
            ApiEventArgs args = new ApiEventArgs(apiName);
            this.Initialized?.Invoke(this, args);
        }

        public delegate void AuthorizedHandler(object sender, ApiAuthorizedEventArgs e);
        /// <summary>
        /// Raised when the API is initialized.
        /// </summary>
        public event AuthorizedHandler Authorized;
        private void RaiseAuthorized(string apiName, bool success)
        {
            // Create the args and call the listening event handlers, if there are any.
            ApiAuthorizedEventArgs args = new ApiAuthorizedEventArgs(apiName, success);
            this.Authorized?.Invoke(this, args);
        }
        #endregion

        public OAuthApi(string oauthEndPoint, string oauthTokenEndpoint, string oauthRedirectUri, string credentialsFilePath,
                        Func<ApiCredential> loadCredentialsFromFile, Func<ApiCredential, string> getOAuthQueryString,
                        Func<string, string, ApiCredential, IList<KeyValuePair<string, string>>> getTokenExchangeParams,
                        Func<string, OAuthToken> convertResponseToToken,
                        Func<string, ApiCredential, IList<KeyValuePair<string, string>>> getTokenRefreshParams,
                        Func<OAuthToken, string> convertTokenToJsonString)
        {
            this.IsInitialized = false;

            // Initialize the OAuth endpoints and redirect uri.
            this.OAuthEndPoint = oauthEndPoint;
            this.OAuthTokenEndpoint = oauthTokenEndpoint;
            this.OAuthRedirectUri = oauthRedirectUri;
            this.CredentialsFilePath = credentialsFilePath;

            // Initialize the delegate functions.
            this.LoadCredentialsFromFile = loadCredentialsFromFile;
            this.GetOAuthQueryString = getOAuthQueryString;
            this.GetTokenExchangeParams = getTokenExchangeParams;
            this.ConvertResponseToToken = convertResponseToToken;
            this.GetTokenRefreshParams = getTokenRefreshParams;
            this.ConvertTokenToJsonString = convertTokenToJsonString;
        }

        #region Methods
        #endregion

        #region Methods - OAuth Flow
        /// <summary>
        /// Starts an OAuth 2.0 authoization flow by redirecting the user to a browser to authorize the application.
        /// </summary>
        /// <returns></returns>
        public async Task StartOAuthAsync()
        {
            ApiCredential credentials = this.LoadCredentialsFromFile();
            string oauthUri = this.OAuthEndPoint + this.GetOAuthQueryString(credentials);
            await Browser.OpenAsync(new Uri(oauthUri));
        }

        /// <summary>
        /// Completes the OAuth flow by exchanging the given authorization code for a token.
        /// </summary>
        /// <param name="authorizationCode">The authorization code to exchange for the token.</param>
        public async Task GetOauthTokenAsync(string authorizationCode)
        {
            System.Diagnostics.Debug.WriteLine("Getting token using code: " + authorizationCode);

            ApiCredential credentials = this.LoadCredentialsFromFile();

            IList<KeyValuePair<string, string>> content = this.GetTokenExchangeParams(authorizationCode, this.OAuthRedirectUri, credentials);

            string responseContent = "";
            bool success = true;
            try
            {
                responseContent = await this.PostAsync(this.OAuthTokenEndpoint, content);

                // No exceptions were thrown, so parse the response message.
                this.tokenData = this.ConvertResponseToToken(responseContent);
            }
            catch(Exception ex)
            {
                // An exception was thrown. The authorization was not a success.
                success = false;
            }

            // Raise the authorized event to signal the completion of the get token.
            this.RaiseAuthorized(this.Name, success);
        }
        #endregion

        #region Methods - HTTP Requests
        /// <summary>
        /// Executes an HTTP POST to the given URI with the given parameters, adding the stored authorization headers to the request.
        /// </summary>
        /// <param name="uri">The uri to make an HTTP POST to.</param>
        /// <param name="parameters">The HTTP content parameters for the POST.</param>
        /// <returns>A string containing the HTTP Response received.</returns>
        public override async Task<string> PostAsync(string uri, IList<KeyValuePair<string, string>> parameters)
        {
            if (this.IsAuthorized)
            {
                // Set the authorization header.
                this.Client.DefaultRequestHeaders.Authorization =
                    new Windows.Web.Http.Headers.HttpCredentialsHeaderValue(this.tokenData.TokenType, this.tokenData.AccessToken);
            }

            return await base.PostAsync(uri, parameters);
        }

        /// <summary>
        /// Executes an HTTP GET to the given URI, adding the stored authorization headers to the request.
        /// </summary>
        /// <param name="uri">The uri to request an HTTP GET from.</param>
        /// <returns>A string containing the HTTP Response received.</returns>
        public override async Task<string> GetAsync(string uri)
        {
            // Refresh the token if needed.
            await this.RefreshTokenIfNeededAsync();

            if (this.IsAuthorized)
            {
                // Set the authorization header.
                this.Client.DefaultRequestHeaders.Authorization =
                    new Windows.Web.Http.Headers.HttpCredentialsHeaderValue(this.tokenData.TokenType, this.tokenData.AccessToken);
            }

            return await base.GetAsync(uri);
        }
        #endregion

        #region Helper Methods - Load/Save Token
        /// <summary>
        /// Initializes the token data from the saved token data if it can, otherwise sets
        /// the token data to null.
        /// </summary>
        /// <param name="tokenFileName">The name to give the token save file.</param>
        protected async Task InitializeTokenDataAsync(string tokenFileName)
        {
            // Try to load the token data.
            bool loaded = await this.TryLoadTokenDataAsync(tokenFileName);
            if (loaded == false)
            {
                // The load was unsuccessful, so initialize the value to false.
                this.tokenData = null;
            }
            else
            {
                // Refresh the token if needed.
                await this.RefreshTokenIfNeededAsync();
            }

            // Mark initialization as complete.
            this.IsInitialized = true;
        }

        /// <summary>
        /// Trys to load the OAuth token data from the token file.
        /// </summary>
        /// <returns>Whether the token data was successfully loaded.</returns>
        private async Task<bool> TryLoadTokenDataAsync(string tokenFileName)
        {
            System.Diagnostics.Debug.WriteLine("Trying to load API token file...");
            // Read the text from the file.
            string lines = "";
            try
            {
                StorageFile tokenFile = await ApplicationData.Current.LocalFolder.GetFileAsync(tokenFileName);
                lines = await FileIO.ReadTextAsync(tokenFile);
            }
            catch (Exception ex)
            {
                // An IO exception occured, so return false.
                System.Diagnostics.Debug.WriteLine("Error accessing: " + ApplicationData.Current.LocalFolder.Path);
                return false;
            }

            // Return false if the read data is empty or whitespace.
            if (String.IsNullOrWhiteSpace(lines))
            {
                System.Diagnostics.Debug.WriteLine("API token file was empty!");
                return false;
            }

            // Try to convert the token response to token data.
            try
            {
                // NOTE: An exception is thrown if the JSON contained in the string
                //      is ill-formed.
                this.tokenData = this.ConvertResponseToToken(lines);
            }
            catch (Exception ex)
            {
                return false;
            }

            // The data was successfully loaded if this point is reached.
            return true;
        }

        /// <summary>
        /// Saves the authorization data for the current API connection to a token file
        ///     with the given name.
        /// </summary>
        /// <param name="tokenFileName">The name to give the token save file.</param>
        protected async Task SaveConnectionDataAsync(string tokenFileName)
        {
            // Save the token data, if there is any.
            if (this.tokenData != null)
            {
                string tokenJsonString = this.ConvertTokenToJsonString(this.tokenData);

                StorageFile tokenFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(tokenFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(tokenFile, tokenJsonString);
            }
        }
        #endregion

        #region Helper Methods - Token Refresh
        /// <summary>
        /// Refreshes the token, if the token needs to be refreshed.
        /// </summary>
        /// <returns>Whether the token was refreshed.</returns>
        private async Task<bool> RefreshTokenIfNeededAsync()
        {
            System.Diagnostics.Debug.WriteLine("Checking if token expired...");
            // Refresh the token if needed.
            if (this.TokenExpired)
            {
                System.Diagnostics.Debug.WriteLine("Token expired!");
                await this.RefreshTokenAsync();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Refreshes the access token using the refresh token.
        /// </summary>
        /// <returns></returns>
        private async Task RefreshTokenAsync()
        {
            System.Diagnostics.Debug.WriteLine("Refreshing token.");

            ApiCredential credentials = this.LoadCredentialsFromFile();

            IList<KeyValuePair<string, string>> content = this.GetTokenRefreshParams(this.tokenData.RefreshToken, credentials);

            string responseContent = await this.PostAsync(this.OAuthTokenEndpoint, content);

            // No exceptions were thrown, so parse the response message.
            OAuthToken updatedToken = this.ConvertResponseToToken(responseContent);

            // Update the access token and expiration.
            this.tokenData.AccessToken = updatedToken.AccessToken;
            this.tokenData.ExpiresInSeconds = updatedToken.ExpiresInSeconds;
            this.tokenData.IssuedUtc = DateTime.UtcNow;

            System.Diagnostics.Debug.WriteLine("Done refreshing token!");
        }
        #endregion
    }
}
