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
    /// <summary>
    /// Constant literals.
    /// </summary>
    public partial class Constants
    {
        #region Connection Settings
        /// <summary>
        /// Graph api endpoint
        /// </summary>
        public const string AadGraphEndpointFormat = "https://{0}/{1}";

        /// <summary>
        /// Query argument for api-version.
        /// </summary>
        public const string QueryParameterNameApiVersion = "api-version";

        /// <summary>
        /// Default graph api domain name.
        /// </summary>
        public const string DefaultGraphApiDomainName = "graph.windows.net";

        /// <summary>
        /// Maximum number of automatic retries.
        /// </summary>
        public const int MaxRetryAttempts = 10;
        #endregion

        #region AAD Header names

        /// <summary>
        /// PropertyName of the client request id header.
        /// </summary>
        public const string HeaderAadClientRequestId = "client-request-id";

        /// <summary>
        /// Header name for specifying the return content preference.
        /// </summary>
        public const string PreferHeaderName = "Prefer";

        /// <summary>
        /// Preference indicating the service to return the content in the response of a modify operation.
        /// </summary>
        public const string PreferHeaderValue = "return-content";

        /// <summary>
        /// PropertyName of the request id header.
        /// </summary>
        public const string HeaderAadRequestId = "request-id";

        /// <summary>
        /// PropertyName of the diagnostics server name header.
        /// </summary>
        public const string HeaderAadServerName = "ocp-aad-diagnostics-server-name";

        /// <summary>
        /// User agent string.
        /// </summary>
        public const string UserAgentString = "Microsoft Azure Graph Client Library 1.0";

        #endregion

        #region Token claim names

        /// <summary>
        /// Tenant Id claim name.
        /// </summary>
        public const string ClaimNameTenantId = "tid";

        #endregion

        #region OData keys

        /// <summary>
        /// In the JSON response, the OData type of the object being returned is this key.
        /// </summary>
        public const string OdataTypeKey = "odata.type";

        /// <summary>
        /// OData key name for next link.
        /// </summary>
        public const string ODataNextLink = "odata.nextLink";

        /// <summary>
        /// OData metadata key.
        /// </summary>
        public const string ODataMetadataKey = "odata.metadata";

        /// <summary>
        /// OData value key.
        /// </summary>
        public const string ODataValues = "value";

        /// <summary>
        /// OData error key.
        /// </summary>
        public const string ODataErrorKey = "odata.error";

        /// <summary>
        /// Key name for error code in an OData error message.
        /// </summary>
        public const string ODataCodeKey = "code";

        /// <summary>
        /// Key name for error message in an OData error message.
        /// </summary>
        public const string ODataMessageKey = "message";

        /// <summary>
        /// JSON MinimalMetadata content type.
        /// </summary>
        public const string MinimalMetadataContentType = "application/json;odata=minimalmetadata";

        /// <summary>
        /// FilterGenerator operator
        /// </summary>
        public const string FilterOperator = "$filter";

        /// <summary>
        /// Top operator.
        /// </summary>
        public const string TopOperator = "$top";

        /// <summary>
        /// Expand operator.
        /// </summary>
        public const string ExpandOperator = "$expand";

        /// <summary>
        /// OrderBy operator.
        /// </summary>
        public const string OrderByOperator = "$orderby";

        /// <summary>
        /// Select operator.
        /// </summary>
        public const string SelectOperator = "$select";

        /// <summary>
        /// Url segment for links.
        /// </summary>
        public const string LinksFragment = "$links";

        /// <summary>
        /// Common tenant name.
        /// </summary>
        public const string CommonTenantName = "myorganization";

        #endregion

        #region Action names

        /// <summary>
        /// Name of the action - getMemberGroups
        /// </summary>
        public const string ActionGetMemberGroups = "getMemberGroups";

        /// <summary>
        /// Name of the action - checkMemberGroups
        /// </summary>
        public const string ActionCheckMemberGroups = "checkMemberGroups";

        /// <summary>
        /// Action name of assignLicense
        /// </summary>
        public const string ActionAssignLicense = "assignLicense";

        /// <summary>
        /// Action name of isMemberOf
        /// </summary>
        public const string ActionIsMemberOf = "isMemberOf";

        /// <summary>
        /// Action name of restore application.
        /// </summary>
        public const string ActionRestoreApplication = "restore";

        #endregion

        #region Api Version
        /// <summary>
        /// Default api version.
        /// </summary>
        public static string DefaultApiVersion = "2013-11-08";
        #endregion
    }
}
