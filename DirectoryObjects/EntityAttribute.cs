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
    /// Attribute to mark entities.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EntityAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityAttribute"/> class.
        /// </summary>
        /// <param name="setName">Entity set name.</param>
        /// <param name="odataType">OData type.</param>
        public EntityAttribute(string setName, string odataType)
        {
            this.SetName = setName;
            this.ODataType = odataType;
        }

        /// <summary>
        /// Gets or sets the name of the entity set.
        /// </summary>
        public string SetName { get; set; }

        /// <summary>
        /// Gets or sets the OData type.
        /// </summary>
        public string ODataType { get; set; }
    }
}
