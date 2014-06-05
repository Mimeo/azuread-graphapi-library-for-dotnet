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
    using System.Net;

    /// <summary>
    /// Generic internal server error exception.
    /// </summary>
    [Serializable]
    public class InternalServerErrorException : GraphException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InternalServerErrorException"/> class.
        /// </summary>
        /// <param name="httpStatusCode">Http status code.</param>
        /// <param name="message">Exception message.</param>
        public InternalServerErrorException(HttpStatusCode httpStatusCode, string message)
            : base(httpStatusCode, message)
        {
        }
    }
}
