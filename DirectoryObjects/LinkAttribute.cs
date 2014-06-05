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
    /// Attribute to mark links
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class LinkAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinkAttribute"/> class.
        /// </summary>
        /// <param name="name">Link name.</param>
        /// <param name="isSingleValued">Is this a single valued link?</param>
        public LinkAttribute(string name, bool isSingleValued)
        {
            this.Name = name;
            this.IsSingleValued = isSingleValued;
        }

        /// <summary>
        /// Gets or sets the name of the link.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether the link is singled valued or not.
        /// </summary>
        public bool IsSingleValued { get; set; }
    }
}
