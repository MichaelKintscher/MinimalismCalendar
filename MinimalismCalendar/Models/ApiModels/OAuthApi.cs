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

        /// <summary>
        /// A cache of the token data for the API.
        /// </summary>
        private Dictionary <string, OAuthToken> tokenDataCollection { get; set; }

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

        #region Delegate Functions
        private Func<ApiCredential> LoadCredentialsFromFile;
        private Func<ApiCredential, string> GetOAuthQueryString;
        private Func<string, string, ApiCredential, IList<KeyValuePair<string, string>>> GetTokenExchangeParams;
        private Func<string, OAuthToken> ConvertResponseToToken;
        private Func<string, ApiCredential, IList<KeyValuePair<string, string>>> GetTokenRefreshParams;
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
        /// Raised when the API is authorized.
        /// </summary>
        public event AuthorizedHandler Authorized;
        private void RaiseAuthorized(string apiName, string accountId, bool success)
        {
            // Create the args and call the listening event handlers, if there are any.
            ApiAuthorizedEventArgs args = new ApiAuthorizedEventArgs(apiName, accountId, success);
            this.Authorized?.Invoke(this, args);
        }
        #endregion

        /// <summary>
        /// Constructs a new instance of the OAuthApi class.
        /// </summary>
        /// <param name="oauthEndPoint">The url for the OAuth 2.0 endpoint.</param>
        /// <param name="oauthTokenEndpoint">The url for the OAuth 2.0 endpoint to exchange a code for a token.</param>
        /// <param name="oauthRedirectUri">The redirect url for the OAuth 2.0 to redirect to on completion.</param>
        /// <param name="loadCredentialsFromFile">The function that loads the app's API credentials.</param>
        /// <param name="getOAuthQueryString">The function that provides the query string for the request to the OAuth 2.0 authorization endpoint.</param>
        /// <param name="getTokenExchangeParams">The function that gets the parameters for the request to the OAuth 2.0 token exchange endpoint.</param>
        /// <param name="convertResponseToToken">The function that parses the response from the OAuth 2.0 token exchange endpoint.</param>
        /// <param name="getTokenRefreshParams">The function that gets the parameters for the request to the OAuth 2.0 token exchange endpoint to refresh the token.</param>
        public OAuthApi(string oauthEndPoint, string oauthTokenEndpoint, string oauthRedirectUri,
                        Func<ApiCredential> loadCredentialsFromFile, Func<ApiCredential, string> getOAuthQueryString,
                        Func<string, string, ApiCredential, IList<KeyValuePair<string, string>>> getTokenExchangeParams,
                        Func<string, OAuthToken> convertResponseToToken,
                        Func<string, ApiCredential, IList<KeyValuePair<string, string>>> getTokenRefreshParams)
        {
            this.IsInitialized = false;

            // Initialize the OAuth endpoints and redirect uri.
            this.OAuthEndPoint = oauthEndPoint;
            this.OAuthTokenEndpoint = oauthTokenEndpoint;
            this.OAuthRedirectUri = oauthRedirectUri;

            // Initialize the delegate functions.
            this.LoadCredentialsFromFile = loadCredentialsFromFile;
            this.GetOAuthQueryString = getOAuthQueryString;
            this.GetTokenExchangeParams = getTokenExchangeParams;
            this.ConvertResponseToToken = convertResponseToToken;
            this.GetTokenRefreshParams = getTokenRefreshParams;
        }

        #region Methods

        /// <summary>
        /// Gets the access token associated with the given account.
        /// </summary>
        /// <param name="accountId">The account ID to get the access token of.</param>
        /// <exception cref="Exception">The given accountId has no data associated with it.</exception>
        /// <returns></returns>
        public string GetToken(string accountId)
        {
            // Verify that the account has token data associated with it.
            if (!this.IsAuthorized(accountId))
            {
                throw new Exception("No data for the given account ID");
            }

            return this.tokenDataCollection[accountId].AccessToken;
        }

        /// <summary>
        /// Returns whether the user has authorized the app with the API on the given account.
        /// </summary>
        /// <param name="accountId">The account ID to check the authorization on.</param>
        /// <returns></returns>
        public bool IsAuthorized(string accountId)
        {
            // The token data for the given account contains a value if the app has previously been authorized on that account.
            return this.tokenDataCollection != null && this.tokenDataCollection.ContainsKey(accountId);
        }

        /// <summary>
        /// Returns whether the token exists and has expired, or exists but is missing an expiration limit.
        /// </summary>
        /// <param name="accountId">The account ID to check the access token of.</param>
        /// <exception cref="Exception">The given accountId has no data associated with it.</exception>
        /// <returns></returns>
        public bool IsTokenExpired(string accountId)
        {
            // Verify that the account has token data associated with it.
            if (!this.IsAuthorized(accountId))
            {
                throw new Exception("No data for the given account ID");
            }

            // There has to be a token data, AND EITHER
            //      there is no expiration time OR
            //      there is an expiration time and it has passed.
            return this.IsAuthorized(accountId) &&
                (this.tokenDataCollection[accountId].ExpiresInSeconds.HasValue == false ||
                    DateTime.Compare(DateTime.UtcNow, this.tokenDataCollection[accountId].IssuedUtc.AddSeconds(this.tokenDataCollection[accountId].ExpiresInSeconds.Value)) >= 0);
        }
        #endregion

        #region Methods - OAuth Flow
        /// <summary>
        /// Starts an OAuth 2.0 authoization flow by redirecting the user to a browser to authorize the application.
        /// </summary>
        /// <returns>A unique ID to use to identify the authorization request.</returns>
        public async Task<Guid> StartOAuthAsync()
        {
            ApiCredential credentials = this.LoadCredentialsFromFile();
            string oauthUri = this.OAuthEndPoint + this.GetOAuthQueryString(credentials);
            await Browser.OpenAsync(new Uri(oauthUri));

            return Guid.NewGuid();
        }

        /// <summary>
        /// Completes the OAuth flow by exchanging the given authorization code for a token.
        /// </summary>
        /// <param name="accountId">The account ID to check the access token of.</param>
        /// <param name="authorizationCode">The authorization code to exchange for the token.</param>
        public async Task GetOauthTokenAsync(string accountId, string authorizationCode)
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
                this.tokenDataCollection.Add(accountId, this.ConvertResponseToToken(responseContent));
            }
            catch(Exception ex)
            {
                // An exception was thrown. The authorization was not a success.
                success = false;
            }

            // Raise the authorized event to signal the completion of the get token.
            this.RaiseAuthorized(this.Name, accountId, success);
        }
        #endregion

        #region Methods - HTTP Requests
        /// <summary>
        /// Executes an HTTP POST to the given URI with the given parameters, adding the stored authorization headers to the request.
        /// </summary>
        /// <param name="accountId">The account ID to check the access token of.</param>
        /// <param name="uri">The uri to make an HTTP POST to.</param>
        /// <param name="parameters">The HTTP content parameters for the POST.</param>
        /// <exception cref="Exception">The given accountId has no data associated with it.</exception>
        /// <returns>A string containing the HTTP Response received.</returns>
        public async Task<string> PostAsync(string accountId, string uri, IList<KeyValuePair<string, string>> parameters)
        {
            // Verify that the account has token data associated with it.
            if (!this.IsAuthorized(accountId))
            {
                throw new Exception("No data for the given account ID");
            }

            // Refresh the token if needed.
            await this.RefreshTokenIfNeededAsync(accountId);

            // Set the authorization header.
            this.Client.DefaultRequestHeaders.Authorization =
                new Windows.Web.Http.Headers.HttpCredentialsHeaderValue(this.tokenDataCollection[accountId].TokenType, this.tokenDataCollection[accountId].AccessToken);

            return await base.PostAsync(uri, parameters);
        }

        /// <summary>
        /// Executes an HTTP GET to the given URI, adding the stored authorization headers to the request.
        /// </summary>
        /// <param name="accountId">The account ID to check the access token of.</param>
        /// <param name="uri">The uri to request an HTTP GET from.</param>
        /// <exception cref="Exception">The given accountId has no data associated with it.</exception>
        /// <returns>A string containing the HTTP Response received.</returns>
        public async Task<string> GetAsync(string accountId, string uri)
        {
            // Verify that the account has token data associated with it.
            if (!this.IsAuthorized(accountId))
            {
                throw new Exception("No data for the given account ID");
            }

            // Refresh the token if needed.
            await this.RefreshTokenIfNeededAsync(accountId);

            // Set the authorization header.
            this.Client.DefaultRequestHeaders.Authorization =
                new Windows.Web.Http.Headers.HttpCredentialsHeaderValue(this.tokenDataCollection[accountId].TokenType, this.tokenDataCollection[accountId].AccessToken);

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
                this.tokenDataCollection = new Dictionary<string, OAuthToken>();
            }
            else
            {
                // Refresh the tokens if needed.
                foreach (string accountId in this.tokenDataCollection.Keys)
                {
                    await this.RefreshTokenIfNeededAsync(accountId);
                }
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
                this.tokenDataCollection = this.DeserializeTokenData(lines);
            }
            catch (Exception ex)
            {
                return false;
            }

            // The data was successfully loaded if this point is reached.
            return true;
        }

        /// <summary>
        /// Saves the authorization data for all acounts with the current API
        /// connection to a token file with the given name.
        /// </summary>
        /// <param name="tokenFileName">The name to give the token save file.</param>
        protected async Task SaveConnectionDataAsync(string tokenFileName)
        {
            // Save the token data, if there is any.
            if (this.tokenDataCollection != null)
            {
                string tokenString = this.SerializeTokenData(this.tokenDataCollection);

                StorageFile tokenFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(tokenFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(tokenFile, tokenString);
            }
        }

        /// <summary>
        /// Serializes the current token data into a JSON string.
        /// </summary>
        /// <param name="data">The token data collection to serialize.</param>
        /// <returns>A serialized JSON representation of the given token data.</returns>
        private string SerializeTokenData(Dictionary<string, OAuthToken> data)
        {
            // Create a JSON array for all of the accounts' token data.
            JsonArray accountsTokenDataArray = new JsonArray();

            // For each account...
            foreach (string key in data.Keys)
            {
                // Create a JSON object for the account's token. and add the key.
                JsonObject accountTokenJson = new JsonObject();
                accountTokenJson.Add("key", JsonValue.CreateStringValue(key));

                accountTokenJson.Add("access_token", JsonValue.CreateStringValue(data[key].AccessToken));

                // Set the expiration time (lifespan of the token) to zero seconds if no value exists.
                double expirationTime = data[key].ExpiresInSeconds.HasValue ? (double)data[key].ExpiresInSeconds.Value : 0.0;
                accountTokenJson.Add("expires_in", JsonValue.CreateNumberValue(expirationTime));

                accountTokenJson.Add("token_type", JsonValue.CreateStringValue(data[key].TokenType));
                accountTokenJson.Add("scope", JsonValue.CreateStringValue(data[key].Scope));
                accountTokenJson.Add("refresh_token", JsonValue.CreateStringValue(data[key].RefreshToken));
                accountTokenJson.Add("issued_time", JsonValue.CreateStringValue(data[key].IssuedUtc.ToString()));

                // Add the JSON object for the account's token to the JSON array.
                accountsTokenDataArray.Add(accountTokenJson);
            }

            JsonObject jsonObject = new JsonObject();
            jsonObject.Add("items", accountsTokenDataArray);

            return jsonObject.Stringify();
        }

        /// <summary>
        /// Deserializes the token data from a JSON string.
        /// </summary>
        /// <param name="lines">The JSON string containing token data to deserialize.</param>
        /// <returns>The token data collection deserialized from the given JSON string.</returns>
        private Dictionary<string, OAuthToken> DeserializeTokenData(string lines)
        {
            // Parse the list of accounts' token data.
            JsonObject jsonData = JsonObject.Parse(lines);
            JsonArray itemsArray = jsonData["items"].GetArray();

            // Create an empty list of token data, and parse and add each account's token data.
            Dictionary<string, OAuthToken> accountsTokenData = new Dictionary<string, OAuthToken>();
            foreach (var tokenDataJsonValue in itemsArray)
            {
                // Parse the response json content.
                JsonObject tokenDataJson = tokenDataJsonValue.GetObject();
                string token = tokenDataJson.ContainsKey("access_token") ? tokenDataJson["access_token"].GetString() : "";
                long expiresInSeconds = tokenDataJson.ContainsKey("expires_in") ? (long)tokenDataJson["expires_in"].GetNumber() : 0;
                string tokenType = tokenDataJson.ContainsKey("token_type") ? tokenDataJson["token_type"].GetString() : "";
                string scope = tokenDataJson.ContainsKey("scope") ? tokenDataJson["scope"].GetString() : "";
                string refreshToken = tokenDataJson.ContainsKey("refresh_token") ? tokenDataJson["refresh_token"].GetString() : "";

                // If the response content contains an issued time, use that. Otherwise, default to Utc now.
                DateTime issuedTime = tokenDataJson.ContainsKey("issued_time") ? DateTime.Parse(tokenDataJson["issued_time"].GetString()) : DateTime.UtcNow;

                // Create and return a new instance of the OAuthToken class.
                OAuthToken tokenData = new OAuthToken()
                {
                    AccessToken = token,
                    TokenType = tokenType,
                    ExpiresInSeconds = expiresInSeconds,
                    RefreshToken = refreshToken,
                    Scope = scope,
                    IdToken = "",
                    IssuedUtc = issuedTime
                };

                // Parse the key for this token data and add both to the collection.
                string key = tokenDataJson["key"].GetString();
                accountsTokenData.Add(key, tokenData);
            }

            return accountsTokenData;
        }

        /// <summary>
        /// Removes the authorization data for the current API connection.
        /// </summary>
        /// <param name="accountId">The account ID to check the access token of.</param>
        /// <param name="tokenFileName">The name of the token file to delete.</param>
        /// <returns></returns>
        protected async Task RemoveConnectionDataAsync(string accountId, string tokenFileName)
        {
            // Clear the cached token data in memory.
            this.tokenDataCollection.Remove(accountId);

            // Save the updated collection.
            await this.SaveConnectionDataAsync(tokenFileName);
        }
        #endregion

        #region Helper Methods - Token Refresh
        /// <summary>
        /// Refreshes the token, if the token needs to be refreshed.
        /// </summary>
        /// <param name="accountId">The account ID to check the access token of.</param>
        /// <exception cref="Exception">The given accountId has no data associated with it.</exception>
        /// <returns>Whether the token was refreshed.</returns>
        private async Task<bool> RefreshTokenIfNeededAsync(string accountId)
        {
            // Verify that the account has token data associated with it.
            if (this.tokenDataCollection == null || this.tokenDataCollection.ContainsKey(accountId) == false)
            {
                throw new Exception("No data for the given account ID");
            }

            // Refresh the token if needed.
            if (this.IsTokenExpired(accountId))
            {
                System.Diagnostics.Debug.WriteLine("Token expired!");
                await this.RefreshTokenAsync(accountId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Refreshes the access token using the refresh token.
        /// </summary>
        /// <param name="accountId">The account ID to check the access token of.</param>
        /// <exception cref="Exception">The given accountId has no data associated with it.</exception>
        /// <returns></returns>
        private async Task RefreshTokenAsync(string accountId)
        {
            // Verify that the account has token data associated with it.
            if (this.tokenDataCollection == null || this.tokenDataCollection.ContainsKey(accountId) == false)
            {
                throw new Exception("No data for the given account ID");
            }

            ApiCredential credentials = this.LoadCredentialsFromFile();

            IList<KeyValuePair<string, string>> content = this.GetTokenRefreshParams(this.tokenDataCollection[accountId].RefreshToken, credentials);

            string responseContent = await this.PostAsync(this.OAuthTokenEndpoint, content);

            // No exceptions were thrown, so parse the response message.
            OAuthToken updatedToken = this.ConvertResponseToToken(responseContent);

            // Update the access token and expiration.
            this.tokenDataCollection[accountId].AccessToken = updatedToken.AccessToken;
            this.tokenDataCollection[accountId].ExpiresInSeconds = updatedToken.ExpiresInSeconds;
            this.tokenDataCollection[accountId].IssuedUtc = DateTime.UtcNow;

            System.Diagnostics.Debug.WriteLine("Done refreshing token!");
        }
        #endregion
    }
}
