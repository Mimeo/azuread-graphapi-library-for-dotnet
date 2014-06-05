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
    using System.Net;

    /// <summary>
    /// A batch request item.
    /// </summary>
    public class BatchResponseItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BatchResponseItem"/> class.
        /// </summary>
        public BatchResponseItem()
        {
            this.BatchHeaders = new WebHeaderCollection();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the batch item has failed.
        /// </summary>
        public bool Failed { get; set; }

        /// <summary>
        /// Gets or sets the paged results (applicable for queries and modifications)
        /// </summary>
        public PagedResults<GraphObject> ResultSet { get; set; }

        /// <summary>
        /// Gets or sets the exception associated with request (if there is one).
        /// </summary>
        public GraphException Exception { get; set; }

        /// <summary>
        /// Gets the batch response headers.
        /// </summary>
        public WebHeaderCollection BatchHeaders { get; private set; }
    }
}
