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
    using Newtonsoft.Json;

    /// <summary>
    /// This class will be used to deserialize all the OData JSON responses.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ODataError
    {
        /// <summary>
        /// Gets or sets the OData error code and message object.
        /// </summary>
        [JsonProperty("odata.error")]
        public ODataErrorCodeMessage Error { get; set; }
    }
}
