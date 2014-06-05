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
    using Newtonsoft.Json;

    /// <summary>
    /// Connection and query manager for the Aad graph api.
    /// </summary>
    public partial class GraphConnection
    {
        /// <summary>
        /// Get the groups that the user is a member of.
        /// </summary>
        /// <param name="user">User whose group membership should be queried.</param>
        /// <param name="securityEnabledOnly">Get the security enabled groups only.</param>
        /// <returns>List of group object ids that user is a part of.</returns>
        public virtual IList<string> GetMemberGroups(User user, bool securityEnabledOnly)
        {
            Utils.ValidateGraphObject(user, "user");

            List<string> memberGroups = new List<string>();
            Uri requestUri = Utils.GetRequestUri<User>(
                this, user.ObjectId, Constants.ActionGetMemberGroups);
            Logger.Instance.Info("POSTing to {0}", requestUri);

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["securityEnabledOnly"] = securityEnabledOnly.ToString();

            string requestJson = JsonConvert.SerializeObject(parameters);
            string responseJson = this.ClientConnection.UploadString(
                requestUri, HttpVerb.POST, requestJson, null, null);

            PagedResults<GraphObject> pagedResults =
                SerializationHelper.DeserializeJsonResponse<GraphObject>(responseJson, requestUri);

            memberGroups.AddRange(pagedResults.MixedResults);

            return memberGroups;
        }

        /// <summary>
        /// Returns the groups that the <see cref="DirectoryObject"/> is a member of, among the groups
        /// requested.
        /// </summary>
        /// <param name="directoryObject">Object whose membership needs to be checked.</param>
        /// <param name="groupIds">Group ids.</param>
        /// <returns>
        /// List of groups that the object is a part of (from the list of groupIds provided)
        /// </returns>
        public virtual IList<string> CheckMemberGroups(GraphObject graphObject, IList<string> groupIds)
        {
            Utils.ValidateGraphObject(graphObject, "graphObject");

            Utils.ThrowIfNullOrEmpty(groupIds, "groupIds");

            List<string> memberGroups = new List<string>();
            Uri requestUri = Utils.GetRequestUri<DirectoryObject>(
                this, graphObject.ObjectId, Constants.ActionCheckMemberGroups);
            Logger.Instance.Info("POSTing to {0}", requestUri);

            Dictionary<string, IList<string>> parameters = new Dictionary<string, IList<string>>();
            parameters["groupIds"] = groupIds;

            string requestJson = JsonConvert.SerializeObject(parameters);
            string responseJson = this.ClientConnection.UploadString(
                requestUri, HttpVerb.POST, requestJson, null, null);
            PagedResults<GraphObject> pagedResults = 
                SerializationHelper.DeserializeJsonResponse<GraphObject>(responseJson, requestUri);

            memberGroups.AddRange(pagedResults.MixedResults);

            return memberGroups;
        }

        /// <summary>
        /// Restore an application.
        /// </summary>
        /// <param name="application">Application to be restored.</param>
        /// <param name="identifierUris">Identifier uris to be assigned after restore.</param>
        /// <returns>Restored application.</returns>
        public virtual Application Restore(Application application, IList<string> identifierUris)
        {
            Utils.ValidateGraphObject(application, "application");

            if (identifierUris == null)
            {
                identifierUris = new List<string>();
            }

            Uri requestUri = Utils.GetRequestUri<Application>(
                this, application.ObjectId, Constants.ActionRestoreApplication);

            Dictionary<string, IList<string>> parameters = new Dictionary<string, IList<string>>();
            parameters["identifierUris"] = identifierUris;

            string requestJson = JsonConvert.SerializeObject(parameters);
            string responseJson = this.ClientConnection.UploadString(
                requestUri, HttpVerb.POST, requestJson, null, null);

            PagedResults<Application> pagedResults =
                SerializationHelper.DeserializeJsonResponse<Application>(responseJson, requestUri);

            if (pagedResults != null && pagedResults.Results.Count > 0)
            {
                return pagedResults.Results[0];
            }

            // TODO: Should we throw an exception here?
            return null;
        }

        /// <summary>
        /// Assign license to a user.
        /// </summary>
        /// <param name="user">User whose licenses need to be manipulated.</param>
        /// <param name="addLicenses">List of licenses to be assigned.</param>
        /// <param name="removeLicenses">Licenses to be disabled.</param>
        /// <returns>Updated user object.</returns>
        public virtual User AssignLicense(User user, IList<AssignedLicense> addLicenses, IList<Guid> removeLicenses)
        {
            Utils.ValidateGraphObject(user, "user");

            if (addLicenses == null)
            {
                throw new ArgumentNullException("addLicenses");
            }

            if (removeLicenses == null)
            {
                throw new ArgumentNullException("removeLicenses");
            }

            Uri requestUri = Utils.GetRequestUri<User>(
                this, user.ObjectId, Constants.ActionAssignLicense);

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters["addLicenses"] = addLicenses;
            parameters["removeLicenses"] = removeLicenses;

            string requestJson = JsonConvert.SerializeObject(parameters);
            string responseJson = this.ClientConnection.UploadString(
                requestUri, HttpVerb.POST, requestJson, null, null);

            PagedResults<User> pagedResults = SerializationHelper.DeserializeJsonResponse<User>(responseJson, requestUri);

            if (pagedResults != null && pagedResults.Results.Count > 0)
            {
                return pagedResults.Results[0];
            }

            // TODO: Should we throw an exception?
            return null;
        }

        /// <summary>
        /// Is the user member of the specified group? (transitive lookup)
        /// </summary>
        /// <param name="groupId">List of app ids.</param>
        /// <param name="memberId">Member object id.</param>
        /// <returns>
        /// <see langword="true"/> if the member is part of the group.
        /// <see langword="false"/> otherwise.
        /// </returns>
        public virtual bool IsMemberOf(string groupId, string memberId)
        {
            if (String.IsNullOrEmpty(groupId))
            {
                throw new ArgumentNullException("groupId");
            }

            if (String.IsNullOrEmpty(memberId))
            {
                throw new ArgumentNullException("memberId");
            }

            bool isMemberOf = false;

            Uri requestUri = Utils.GetRequestUri(this, null, null, Constants.ActionIsMemberOf);

            Logger.Instance.Info("POSTing to {0}", requestUri);

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["groupId"] = groupId;
            parameters["memberId"] = memberId;

            string requestJson = JsonConvert.SerializeObject(parameters);
            string responseJson = this.ClientConnection.UploadString(
                requestUri, HttpVerb.POST, requestJson, null, null);

            PagedResults<GraphObject> pagedResults =
                SerializationHelper.DeserializeJsonResponse<GraphObject>(responseJson, requestUri);

            if (pagedResults.MixedResults.Count > 0)
            {
                bool.TryParse(pagedResults.MixedResults[0].ToString(), out isMemberOf);
            }

            return isMemberOf;
        }
    }
}
