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

namespace Microsoft.Azure.ActiveDirectory.GraphClient.ErrorHandling
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Mapping of error codes to exception types.
    /// </summary>
    public static class ErrorCodes
    {
        /// <summary>
        /// A map from AAD error codes to exception types.
        /// </summary>
        private static Dictionary<string, Type> exceptionCodeMap = new Dictionary<string, Type>
        {
            { "Authentication_MissingOrMalformed", typeof(AuthenticationException) },
            { "Authentication_Unauthorized", typeof(AuthenticationException) },
            { "Authentication_UnsupportedTokenType", typeof(AuthenticationException) },
            { "Authentication_ExpiredToken", typeof(ExpiredTokenException) },
            { "Authorization_IdentityDisabled", typeof(AuthorizationException) },
            { "Authorization_IdentityNotFound", typeof(AuthorizationException) },
            { "Authorization_RequestDenied", typeof(AuthorizationException) },
            { "Request_ResourceNotFound", typeof(ObjectNotFoundException) },
            { "Directory_ObjectNotFound", typeof(ObjectNotFoundException) },
            { "Directory_ExpiredPageToken", typeof(PageNotAvailableException) },            
            { "Directory_ReplicaUnavailable", typeof(ServiceUnavailableException) },
            { "Request_DataContractVersionMissing", typeof(InvalidApiVersionException) },
            { "Request_InvalidDataContractVersion", typeof(InvalidApiVersionException) },
            { "Headers_HeaderNotSupported", typeof(InvalidHeaderException) },
            { "Request_BadRequest", typeof(BadRequestException) },
            { "Request_InvalidRequestUrl", typeof(BadRequestException) },
            { "Request_UnsupportedQuery", typeof(UnsupportedQueryException) },
            { "Request_MultipleObjectsWithSameKeyValue", typeof(DuplicateObjectException) },
            { "Service_InternalServerError", typeof(InternalServerErrorException) },            
            { "Directory_ConcurrencyViolation", typeof(ServiceUnavailableException) },
            { "Directory_QuotaExceeded", typeof(QuotaExceededException) },
            { "Request_ThrottledPermanently", typeof(RequestThrottledException) },
            { "Request_ThrottledTemporarily", typeof(RequestThrottledException) },
        };

        /// <summary>
        /// Gets the exception error code map.
        /// </summary>
        public static Dictionary<string, Type> ExceptionErrorCodeMap
        {
            get
            {
                return ErrorCodes.exceptionCodeMap;
            }
        }
    }
}
