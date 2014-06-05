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

    /// <summary>
    /// Paged set of results.
    /// </summary>
    public class PagedResults<T> where T : GraphObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PagedResults"/> class.
        /// </summary>
        public PagedResults()
        {
            this.Results = new List<T>();
            this.MixedResults = new List<string>();
        }

        /// <summary>
        /// Gets the results of directory objects.
        /// </summary>
        public IList<T> Results { get; private set; }

        /// <summary>
        /// Gets the mixed collection of objects (objects that are not directoryObjects)
        /// </summary>
        public IList<string> MixedResults { get; private set; }

        /// <summary>
        /// Gets or sets the request uri that returned the results.
        /// </summary>
        public Uri RequestUri { get; internal set; }

        /// <summary>
        /// Gets or sets the link to the next page of results.
        /// </summary>
        public string PageToken { get; set; }

        /// <summary>
        /// Gets or sets the odata metadata type.
        /// </summary>
        public string ODataMetadataType { get; set; }

        /// <summary>
        /// Gets whether this is the last page of results.
        /// </summary>
        public bool IsLastPage
        {
            get { return string.IsNullOrEmpty(this.PageToken); }
        }
    }
}