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
    using System.Net;
    using System.Text;

    /// <summary>
    /// Base Graph exception.
    /// </summary>
    [Serializable]
    public class GraphException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public GraphException(string message) : base(message)
        {
            this.HttpStatusCode = HttpStatusCode.Unused;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphException"/> class.
        /// </summary>
        /// <param name="statusCode">Http status code.</param>
        /// <param name="message">Exception message.</param>
        public GraphException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            this.HttpStatusCode = statusCode;
            this.ErrorMessage = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphException"/> class.
        /// </summary>
        /// <param name="httpStatusCode">Http status code.</param>
        /// <param name="errorCode">Aad error code.</param>
        /// <param name="message">Exception message.</param>
        public GraphException(HttpStatusCode statusCode, string errorCode, string message)
            : base(message)
        {
            this.HttpStatusCode = statusCode;
            this.Code = errorCode;
            this.ErrorMessage = message;
        }

        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; set; }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the response headers.
        /// </summary>
        public WebHeaderCollection ResponseHeaders { get; set; }
        
        /// <summary>
        /// Gets or sets the extended error information.
        /// </summary>
        public Dictionary<string, string> ExtendedErrors { get; set; }

        /// <summary>
        /// Gets or sets the error object.
        /// </summary>
        public ODataError ErrorResponse { get; set; }

        /// <summary>
        /// Gets or sets the response uri.
        /// </summary>
        public string ResponseUri { get; set; }

        /// <summary>
        /// Add the parsed details to the exception details.
        /// </summary>
        /// <returns>Exception details.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString());
            sb.AppendFormat("[Url: {0}]", this.ResponseUri);

            if (this.ResponseHeaders != null)
            {
                sb.Append("[Headers: ");
                foreach (string headerName in this.ResponseHeaders.AllKeys)
                {
                    sb.AppendFormat("{0}: {1},", headerName, this.ResponseHeaders[headerName]);
                }

                sb.Append("]");
            }

            return sb.ToString();
        }
    }
}
