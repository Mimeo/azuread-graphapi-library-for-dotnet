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
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Text;

    /// <summary>
    /// A batch request item.
    /// </summary>
    public class BatchRequestItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BatchRequestItem"/> class.
        /// </summary>
        /// <param name="httpMethod">Http method.</param>
        /// <param name="isChangesetRequired">
        /// Is this a query or a modify request. Modify requests require a changeset and queries do not require one.
        /// </param>
        /// <param name="requestUri">Request uri.</param>
        /// <param name="headers">Http headers.</param>
        /// <param name="body">Request body.</param>
        /// <remarks>
        /// Request id is the batch id, which will be set before generating the request part.
        /// </remarks>
        public BatchRequestItem(
            string httpMethod, bool isChangesetRequired, Uri requestUri, WebHeaderCollection headers, string body)
        {
            this.Method = httpMethod;
            this.IsChangesetRequired = isChangesetRequired;
            this.RequestUri = requestUri;
            this.Headers = headers;
            this.Body = body;
        }

        /// <summary>
        /// Gets or sets the http method.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the request uri.
        /// </summary>
        public Uri RequestUri { get; set; }

        /// <summary>
        /// Gets or sets the request id.
        /// </summary>
        public Guid RequestId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a changeset needs to be generated.
        /// </summary>
        public bool IsChangesetRequired { get; set; }

        /// <summary>
        /// Gets or sets the changeset id.
        /// </summary>
        public Guid ChangeSetId { get; set; }

        /// <summary>
        /// Gets or sets the headers.
        /// </summary>
        public WebHeaderCollection Headers { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Generates the strin representation.
        /// </summary>
        /// <returns>String representation of the OData batch request.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("--batch_{0}", this.RequestId.ToString());
            sb.AppendLine();

            if (this.IsChangesetRequired)
            {
                sb.AppendFormat(
                    "Content-Type: multipart/mixed; boundary=changeset_{0}", this.ChangeSetId.ToString());
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendFormat("--changeset_{0}", this.ChangeSetId.ToString());
                sb.AppendLine();
            }

            sb.Append("Content-Type: application/http");
            sb.AppendLine();
            sb.Append("Content-Transfer-Encoding: binary");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendFormat("{0} {1} HTTP/1.1", this.Method, this.RequestUri);
            sb.AppendLine();

            if (this.Headers == null)
            {
                this.Headers = new WebHeaderCollection();
            }

            this.Headers[HttpRequestHeader.ContentType] = Constants.MinimalMetadataContentType;
            this.Headers[HttpRequestHeader.Accept] = Constants.MinimalMetadataContentType;
            this.Headers[Constants.PreferHeaderName] = Constants.PreferHeaderValue;
            this.Headers[HttpRequestHeader.AcceptCharset] = "UTF-8";

            foreach (string headerName in this.Headers.Keys)
            {
                sb.AppendFormat("{0}: {1}", headerName, this.Headers[headerName]);
                sb.AppendLine();
            }

            if (!String.IsNullOrEmpty(this.Body))
            {
                sb.AppendLine();
                sb.Append(this.Body);
            }

            sb.AppendLine();

            return sb.ToString();
        }
    }
}
