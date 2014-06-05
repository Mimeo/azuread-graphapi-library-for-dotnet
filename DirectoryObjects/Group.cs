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
    using System.Linq;
    using System.Web;
    using Microsoft.Azure.ActiveDirectory.GraphClient.ErrorHandling;
    using Newtonsoft.Json;

    /// <summary>
    /// Aad group object.
    /// </summary>
    public partial class Group : DirectoryObject
    {
        /// <summary>
        /// Validate the group properties for create / update.
        /// </summary>
        /// <param name="isCreate">Is this a create operation?</param>
        public override void ValidateProperties(bool isCreate)
        {
            base.ValidateProperties(isCreate);

            if (this.MailEnabled.HasValue && this.MailEnabled.Value)
            {
                throw new PropertyValidationException("MailEnabled groups cannot be created through Graph Api.");
            }
        }
    }
}