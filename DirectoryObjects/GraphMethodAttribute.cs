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

    /// <summary>
    /// Attribute to mark methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class GraphMethodAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphMethodAttribute"/> class.
        /// </summary>
        /// <param name="supportsBatching">Indicates if the method supports batching.</param>
        public GraphMethodAttribute(bool supportsBatching)
        {
            this.SupportsBatching = supportsBatching;
        }

        /// <summary>
        /// Gets or sets a value indicating if a given method supports batching.
        /// </summary>
        public bool SupportsBatching { get; set; }
    }
}
