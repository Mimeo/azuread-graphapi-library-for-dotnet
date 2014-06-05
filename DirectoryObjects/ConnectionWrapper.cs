// Copyright Â© Microsoft Open Technologies, Inc.
//
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION
// ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A
// PARTICULAR PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache License, Version 2.0 for the specific language
// governing permissions and limitations under the License.

namespace Microsoft.Azure.ActiveDirectory.GraphClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Threading;
    using Microsoft.Azure.ActiveDirectory.GraphClient.ErrorHandling;

    /// <summary>
    /// A wrapper to the http connection.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConnectionWrapper
    {
        /// <summary>
        /// Graph connection settings.
        /// </summary>
        private readonly GraphSettings graphSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionWrapper"/> class.
        /// </summary>
        public ConnectionWrapper() : this(new GraphSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionWrapper"/> class.
        /// </summary>
        public ConnectionWrapper(GraphSettings graphSettings)
        {
            this.graphSettings = graphSettings;
        }

        /// <summary>
        /// Gets or sets the client request id.
        /// </summary>
        internal Guid ClientRequestId { get; set; }
        
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        internal string AccessToken { get; set; }

        /// <summary>
        /// Gets the graph api domain name.
        /// </summary>
        internal string GraphApiDomainName
        {
            get { return this.graphSettings.GraphDomainName; }
        }

        /// <summary>
        /// Gets the graph api version.
        /// </summary>
        internal string GraphApiVersion
        {
            get { return this.graphSettings.ApiVersion; }
        }

        /// <summary>
        /// Gets whether the client library should retry the requests for the configured exceptions.
        /// </summary>
        internal bool IsRetryEnabled
        {
            get { return this.graphSettings.IsRetryEnabled; }
        }

        /// <summary>
        /// Gets the exceptions for which the request should be retried.
        /// </summary>
        internal HashSet<string> RetryOnExceptions
        {
            get { return this.graphSettings.RetryOnExceptions; }
        }

        /// <summary>
        /// Gets or sets the wait time before retrying exceptions.
        /// </summary>
        internal TimeSpan WaitBeforeRetry
        {
            get { return this.graphSettings.WaitBeforeRetry; }
        }

        /// <summary>
        /// Gets the total number of tries after which retry should be stopped.
        /// </summary>
        internal int TotalAttempts
        {
            get { return this.graphSettings.TotalAttempts; }
        }

        /// <summary>
        /// Attach all the required headers.
        /// Add the additional headers.
        /// Headers are replaced and not added, thereby avoiding duplicates.
        /// </summary>
        /// <param name="headers">Request headers.</param>
        /// <param name="includeContentType">Should content type header be included?</param>
        /// <param name="additionalHeaders">Additional http headers.</param>
        public virtual void AttachRequiredHeaders(
            WebHeaderCollection headers, bool includeContentType, WebHeaderCollection additionalHeaders)
        {
            headers[HttpRequestHeader.Authorization] = this.AccessToken;
            headers[Constants.HeaderAadClientRequestId] = this.ClientRequestId.ToString();
            headers[Constants.PreferHeaderName] = Constants.PreferHeaderValue;

            if (includeContentType)
            {
                headers[HttpRequestHeader.ContentType] = Constants.MinimalMetadataContentType;
            }

            try
            {
                headers[HttpRequestHeader.UserAgent] = Constants.UserAgentString;

                if (additionalHeaders != null)
                {
                    foreach (string additionalHeaderName in additionalHeaders.AllKeys)
                    {
                        headers[additionalHeaderName] = additionalHeaders[additionalHeaderName];
                    }
                }
            }
            catch (ArgumentException)
            {
                // When using HttpWebRequest, useragent should be set using the special member and not through headers.
                // When using WebClient, user agent has to be set using headers.
            }
        }

        /// <summary>
        /// Invoke a network operation using the WebClient or the HttpWebRequest classes.
        /// </summary>
        /// <typeparam name="T">Type of the return value.</typeparam>
        /// <param name="action">Action to be performed.</param>
        /// <returns>Web response.</returns>
        public virtual T InvokeNetworkOperation<T>(Func<T> action) where T : class
        {
            int attempt = 1;

            while (true)
            {
                try
                {
                    return action();
                }
                catch (WebException webExc)
                {
                    GraphException graphException = ErrorResolver.ParseWebException(webExc);

                    if (!this.graphSettings.IsRetryEnabled ||
                        this.graphSettings.TotalAttempts <= 0 ||
                        attempt == this.graphSettings.TotalAttempts ||
                        attempt == Constants.MaxRetryAttempts ||
                        !this.graphSettings.RetryOnExceptions.Contains(graphException.GetType().FullName))
                    {
                        throw graphException;
                    }

                    attempt++;
                    // TODO: Validate that we are not going to sleep for too long :-)
                    Thread.Sleep(this.graphSettings.WaitBeforeRetry);
                }
            }
        }

        /// <summary>
        /// A wrapper to the DownloadData method.
        /// </summary>
        /// <param name="address">Web address.</param>
        /// <param name="additionalHeaders">Optional, additional request headers.</param>
        /// <returns>Http response/</returns>
        public virtual byte[] DownloadData(string address, WebHeaderCollection additionalHeaders)
        {
            return this.InvokeNetworkOperation<byte[]>(
                () =>
                    {
                        using (WebClient webClient = new WebClient())
                        {
                            this.AttachRequiredHeaders(webClient.Headers, true, additionalHeaders);
                            byte[] responseData = webClient.DownloadData(address);
                            Utils.LogResponseHeaders(webClient.ResponseHeaders);
                            return responseData;
                        }
                    });
        }

        /// <summary>
        /// A wrapper to the DownloadData.
        /// </summary>
        /// <param name="address">Web address.</param>
        /// <param name="additionalHeaders">Optional, additional request headers.</param>
        /// <returns>Http response/</returns>
        public virtual byte[] DownloadData(Uri address, WebHeaderCollection additionalHeaders)
        {
            return this.DownloadData(address.ToString(), additionalHeaders);
        }

        /// <summary>
        /// A wrapper to the UploadString method.
        /// </summary>
        /// <param name="requestUri">Request uri.</param>
        /// <param name="method">Http method.</param>
        /// <param name="requestBody">Request body.</param>
        /// <param name="additionalHeaders">Additional request headers.</param>
        /// <param name="responseHeaders">Set to the response headers.</param>
        /// <returns>Http response.</returns>
        public virtual string UploadString(
            string requestUri, 
            string method, 
            string requestBody, 
            WebHeaderCollection additionalHeaders, 
            WebHeaderCollection responseHeaders)
        {
            return this.InvokeNetworkOperation<string>(
                () =>
                    {
                        using (WebClient webClient = new WebClient())
                        {
                            this.AttachRequiredHeaders(webClient.Headers, true, additionalHeaders);
                            string responseData = webClient.UploadString(requestUri, method, requestBody);

                            if (responseHeaders != null)
                            {
                                responseHeaders.Add(webClient.ResponseHeaders);
                            }

                            Utils.LogResponseHeaders(webClient.ResponseHeaders);
                            return responseData;
                        }
                    });
        }

        /// <summary>
        /// A wrapper to the UploadString method.
        /// </summary>
        /// <param name="requestUri">Request uri.</param>
        /// <param name="method">Http method.</param>
        /// <param name="requestBody">Request body.</param>
        /// <param name="additionalHeaders">Additional request headers.</param>
        /// <param name="responseHeaders">Set to the response headers.</param>
        /// <returns>Http response.</returns>
        public virtual string UploadString(
            Uri requestUri, 
            string method, 
            string requestBody, 
            WebHeaderCollection additionalHeaders, 
            WebHeaderCollection responseHeaders)
        {
            return this.UploadString(requestUri.ToString(), method, requestBody, additionalHeaders, responseHeaders);
        }

        /// <summary>
        /// A wrapper to the UploadData method.
        /// </summary>
        /// <param name="requestUri">Request uri.</param>
        /// <param name="method">Http method.</param>
        /// <param name="data">Data to be uploaded.</param>
        /// <param name="additionalHeaders">Additional request headers.</param>
        /// <returns>Http response.</returns>
        public virtual byte[] UploadData(
            Uri requestUri, string method, byte[] data, WebHeaderCollection additionalHeaders)
        {
            return this.InvokeNetworkOperation<byte[]>(
                () =>
                    {
                        using (WebClient webClient = new WebClient())
                        {
                            this.AttachRequiredHeaders(webClient.Headers, true, additionalHeaders);
                            byte[] responseData = webClient.UploadData(requestUri, method, data);
                            Utils.LogResponseHeaders(webClient.ResponseHeaders);
                            return responseData;
                        }
                    });
        }

        /// <summary>
        /// A wrapper to the DownloadData method.
        /// </summary>
        /// <param name="requestUri">Request uri.</param>
        public virtual void DeleteRequest(Uri requestUri)
        {
            HttpWebRequest webRequest = this.CreateWebRequest(requestUri);
            webRequest.Method = HttpVerb.DELETE;
            WebResponse webResponse = this.InvokeNetworkOperation<WebResponse>(webRequest.GetResponse);

            if (webResponse != null)
            {
                webResponse.Close();
            }
        }

        /// <summary>
        /// Add a http web request with the required headers attached.
        /// </summary>
        /// <param name="requestUri">Request uri.</param>
        /// <returns>Http web request object.</returns>
        public virtual HttpWebRequest CreateWebRequest(Uri requestUri)
        {
            HttpWebRequest webRequest = WebRequest.Create(requestUri) as HttpWebRequest;
            webRequest.UserAgent = Constants.UserAgentString;
            this.AttachRequiredHeaders(webRequest.Headers, false, null);
            return webRequest;
        }

        /// <summary>
        /// Get the content type of the most recent response.
        /// </summary>
        /// <param name="responseHeaders">Response headers.</param>
        /// <returns>Content type of the most recent response.</returns>
        public virtual string GetContentTypeOfLastResponse(WebHeaderCollection responseHeaders)
        {
            if (responseHeaders != null)
            {
                return responseHeaders[HttpResponseHeader.ContentType];
            }

            return String.Empty;
        }
    }
}
