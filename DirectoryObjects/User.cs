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
    using Microsoft.Azure.ActiveDirectory.GraphClient.ErrorHandling;

    /// <summary>
    /// Aad user object.
    /// </summary>
    public partial class User : DirectoryObject
    {
        /// <summary>
        /// Validate properties for create / update.
        /// </summary>
        /// <param name="isCreate">Is this a create or an update operation?</param>
        /// <exception cref="PropertyValidationException">Property validation exception.</exception>
        public override void ValidateProperties(bool isCreate)
        {
            base.ValidateProperties(isCreate);

            if (isCreate)
            {
                if (String.IsNullOrEmpty(this.MailNickname))
                {
                    throw new PropertyValidationException("Missing MailNickname");
                }

                if (String.IsNullOrEmpty(this.DisplayName))
                {
                    throw new PropertyValidationException("Missing DisplayName property.");
                }

                if (String.IsNullOrEmpty(this.UserPrincipalName))
                {
                    throw new PropertyValidationException("Missing UserPrincipalName property.");
                }

                if (this.PasswordProfile == null)
                {
                    throw new PropertyValidationException("Missing PasswordProfile property.");
                }

                if (!this.ChangedProperties.Contains("AccountEnabled"))
                {
                    throw new PropertyValidationException("AccountEnabled should be specified.");
                }

                if (this.ChangedProperties.Contains("AssignedLicenses"))
                {
                    throw new PropertyValidationException("AssignedLicense should not specified.");
                }
            }
        }
    }
}