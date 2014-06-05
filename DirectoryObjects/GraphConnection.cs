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
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Text;
    using System.Threading;
    using Newtonsoft.Json;

    /// <summary>
    /// Connection and query manager for the Azure graph endpoint.
    /// </summary>
    public partial class GraphConnection
    {
        /// <summary>
        /// Indicates if the current request executing is a batch request.
        /// </summary>
        private readonly ThreadLocal<bool> returnBatchItem = new ThreadLocal<bool>(() => false);

        /// <summary>
        /// List of batch request items set by various graph methods if the current request is a batch request.
        /// </summary>
        private readonly ThreadLocal<IList<BatchRequestItem>> batchRequestItems =
            new ThreadLocal<IList<BatchRequestItem>>(() => new List<BatchRequestItem>());

        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphConnection"/> class.
        /// </summary>
        /// <remarks>
        /// Unit tests that want to stub out all graph connection methods will use this constructor.
        /// Graph client library unit tests do not use them.
        /// </remarks>
        protected GraphConnection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphConnection"/> class.
        /// </summary>
        /// <param name="connectionWrapper">Mock <see cref="ConnectionWrapper"/> used for unit test.</param>
        /// <param name="accessToken">Access token.</param>
        /// <remarks>
        /// Unit tests that want to use special mock connection will use this constructor.
        /// </remarks>
        protected GraphConnection(ConnectionWrapper connectionWrapper, string accessToken)
        {
            this.ClientConnection = connectionWrapper;
            this.AccessToken = accessToken;
        }
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphConnection"/> class.
        /// </summary>
        /// <param name="accessToken">Access token.</param>
        public GraphConnection(string accessToken)
            : this (accessToken, new Guid(), null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphConnection"/> class.
        /// </summary>
        /// <param name="accessToken">Access token.</param>
        /// <param name="clientRequestId">Client request id.</param>
        public GraphConnection(string accessToken, Guid clientRequestId)
            : this(accessToken, clientRequestId, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphConnection"/> class.
        /// </summary>
        /// <param name="accessToken">Access token.</param>
        /// <param name="graphSettings">Various graph settings to control <see cref="GraphConnection"/>.</param>
        public GraphConnection(string accessToken, GraphSettings graphSettings)
            : this(accessToken, Guid.NewGuid(), graphSettings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphConnection"/> class.
        /// </summary>
        /// <param name="accessToken">Access token.</param>
        /// <param name="clientRequestId">Client request id.</param>
        /// <param name="graphSettings">Various graph settings to control <see cref="GraphConnection"/>.</param>
        public GraphConnection(string accessToken, Guid clientRequestId, GraphSettings graphSettings)
        {
            graphSettings = graphSettings ?? new GraphSettings();
            this.ClientConnection = new ConnectionWrapper(graphSettings);
            this.AccessToken = accessToken;
            this.ClientRequestId = clientRequestId;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the graph api domain name.
        /// </summary>
        public string GraphApiDomainName
        {
            get { return this.ClientConnection.GraphApiDomainName; }
        }

        /// <summary>
        /// Gets the graph api version.
        /// </summary>
        public string GraphApiVersion
        {
            get { return this.ClientConnection.GraphApiVersion; }
        }

        /// <summary>
        /// Gets whether the client library should retry the requests for the configured exceptions.
        /// </summary>
        public bool IsRetryEnabled
        {
            get { return this.ClientConnection.IsRetryEnabled; }
        }

        /// <summary>
        /// Gets the exceptions for which the request should be retried.
        /// </summary>
        public HashSet<string> RetryOnExceptions
        {
            get { return this.ClientConnection.RetryOnExceptions; }
        }

        /// <summary>
        /// Gets or sets the wait time before retrying exceptions.
        /// </summary>
        public TimeSpan WaitBeforeRetry
        {
            get { return this.ClientConnection.WaitBeforeRetry; }
        }

        /// <summary>
        /// Gets the total number of tries after which retry should be stopped.
        /// </summary>
        public int TotalAttempts
        {
            get { return this.ClientConnection.TotalAttempts; }
        }

        /// <summary>
        /// Gets or sets the web client object.
        /// </summary>
        public ConnectionWrapper ClientConnection { get; private set; }

        /// <summary>
        /// Gets or sets the client request id.
        /// </summary>
        public Guid ClientRequestId
        {
            get
            {
                return this.ClientConnection.ClientRequestId;
            }
            set
            {
                this.ClientConnection.ClientRequestId = value;
            }
        }

        /// <summary>
        /// Gets or sets the tenant id.
        /// </summary>
        public string TenantId { get; private set; }

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string AccessToken
        {
            get
            {
                return this.ClientConnection.AccessToken;
            }
            set
            {
                this.ClientConnection.AccessToken = value;
                this.TenantId = Utils.GetTenantId(value);
            }
        }
        
        /// <summary>
        /// Gets the aad graph endpoint (fully formatted)
        /// </summary>
        public string AadGraphEndpoint
        {
            get
            {
                return String.Format(
                    CultureInfo.InvariantCulture,
                    Constants.AadGraphEndpointFormat,
                    this.GraphApiDomainName,
                    this.TenantId);
            }
        }

        #endregion

        #region Graph Object Manipulations.
        /// <summary>
        /// List the directory objects of the given type
        /// </summary>
        /// <param name="objectType">Directory object type.</param>
        /// <param name="pageToken">Page token.</param>
        /// <param name="filter">OData filter.</param>
        /// <returns>Paged collection of results.</returns>
        [GraphMethod(true)]
        public virtual PagedResults<GraphObject> List(
            Type objectType, string pageToken, FilterGenerator filter)
        {
            Uri requestUri;
            return SerializationHelper.DeserializeJsonResponse<GraphObject>(
                this.ListCore(objectType, pageToken, filter, out requestUri), requestUri);
        }

        /// <summary>
        /// List the directory objects of the given type
        /// </summary>
        /// <typeparam name="T">Type of directory object.</typeparam>
        /// <param name="pageToken">Page token.</param>
        /// <param name="filter">OData filter.</param>
        /// <returns>Paged collection of results.</returns>
        [GraphMethod(true)]
        public virtual PagedResults<T> List<T>(string pageToken, FilterGenerator filter) where T : GraphObject
        {
            Uri requestUri;
            return SerializationHelper.DeserializeJsonResponse<T>(
                this.ListCore(typeof(T), pageToken, filter, out requestUri), requestUri);
        }

        /// <summary>
        /// Get current tenant details.
        /// </summary>
        /// <returns>Tenant details.</returns>
        public virtual TenantDetail GetTenantDetails()
        {
            PagedResults<TenantDetail> pagedResults = this.List<TenantDetail>(null, new FilterGenerator());

            if (pagedResults.Results == null || pagedResults.Results.Count != 1)
            {
                throw new ObjectNotFoundException(HttpStatusCode.NotFound, "Tenant details not found.");
            }

            return pagedResults.Results[0];
        }

        /// <summary>
        /// Get a single object
        /// </summary>
        /// <typeparam name="T">Type of object to be searched for.</typeparam>
        /// <param name="objectId">Object id of the directory object.</param>
        /// <returns>Directory object with the specified object id.</returns>
        [GraphMethod(true)]
        public virtual T Get<T>(string objectId) where T : GraphObject
        {
            return this.Get(typeof(T), objectId) as T;
        }

        /// <summary>
        /// Get a single graph object. You can optionally expand properties.
        /// </summary>
        /// <typeparam name="T">Type of object to be searched for.</typeparam>
        /// <param name="objectId">Object id of the directory object.</param>
        /// <returns>Entity with the specified object id.</returns>
        /// <remarks>Currently, the server supports expand of only one property at a time.</remarks>
        [GraphMethod(true)]
        public virtual GraphObject Get(Type objectType, string objectId)
        {
            return this.Get(objectType, objectId, LinkProperty.None);
        }

        /// <summary>
        /// Get a single graph object. You can optionally expand properties.
        /// </summary>
        /// <typeparam name="T">Type of object to be searched for.</typeparam>
        /// <param name="objectId">Object id of the directory object.</param>
        /// <param name="expandProperty">Property to be expanded.</param>
        /// <returns>Entity with the specified object id.</returns>
        /// <remarks>Currently, the server supports expand of only one property at a time.</remarks>
        [GraphMethod(true)]
        public virtual GraphObject Get(Type objectType, string objectId, LinkProperty expandProperty)
        {
            FilterGenerator filterGenerator = new FilterGenerator();
            filterGenerator.ExpandProperty = expandProperty;

            Uri requestUri;
            PagedResults<GraphObject> pagedResults = SerializationHelper.DeserializeJsonResponse<GraphObject>(
                this.GetCore(objectType, objectId, filterGenerator, out requestUri), requestUri);

            return pagedResults.Results.FirstOrDefault();
        }

        /// <summary>
        /// Add a new entity.
        /// </summary>
        /// <param name="entity">Entity object.</param>
        /// <returns>Updated directory object.</returns>
        [GraphMethod(true)]
        public virtual GraphObject Add(GraphObject entity)
        {
            return this.AddOrUpdate(entity, true);
        }

        /// <summary>
        /// Add a new entity.
        /// </summary>
        /// <typeparam name="T">Type of object to be added.</typeparam>
        /// <param name="entity">Entity object.</param>
        /// <returns>Updated directory object.</returns>
        [GraphMethod(true)]
        public virtual T Add<T>(T entity) where T : GraphObject
        {
            return this.AddOrUpdate(entity, true) as T;
        }

        /// <summary>
        /// Update the directory object on the cloud.
        /// </summary>
        /// <param name="entity">Object to be updated.</param>
        /// <returns>Updated directory object.</returns>
        [GraphMethod(true)]
        public virtual GraphObject Update(GraphObject entity)
        {
            return this.AddOrUpdate(entity, false);
        }

        /// <summary>
        /// Update the directory object on the cloud.
        /// </summary>
        /// <typeparam name="T">Type of object to be updated.</typeparam>
        /// <param name="entity">Object to be updated.</param>
        /// <returns>Updated directory object.</returns>
        [GraphMethod(true)]
        public virtual T Update<T>(T entity) where T : GraphObject
        {
            return this.AddOrUpdate(entity, false) as T;
        }

        /// <summary>
        /// Delete the directory object on the cloud.
        /// </summary>
        /// <param name="graphObject">Graph object.</param>
        [GraphMethod(true)]
        public virtual void Delete(GraphObject graphObject)
        {
            Utils.ValidateGraphObject(graphObject, "graphObject");

            Uri deleteUri = Utils.GetRequestUri(
                this, graphObject.GetType(), graphObject.ObjectId);

            if (this.returnBatchItem.Value)
            {
                this.batchRequestItems.Value.Add(new BatchRequestItem(HttpVerb.DELETE, true, deleteUri, null, null));
                return;
            }

            this.ClientConnection.DeleteRequest(deleteUri);
        }

        #region Containment Operations
        /// <summary>
        /// Add or update the directory object on the cloud.
        /// </summary>
        /// <param name="parent">Parent object type for containment type.</param>
        /// <param name="containment">Containment object.</param>
        /// <param name="isCreate">Is this create or update?</param>
        /// <returns>Created base entity object.</returns>
        [GraphMethod(true)]
        public GraphObject AddContainment(GraphObject parent, GraphObject containment)
        {
            return this.AddOrUpdateContainment(parent, containment, true);
        }

        /// <summary>
        /// Add or update the directory object on the cloud.
        /// </summary>
        /// <typeparam name="T">Type of containment to be added.</typeparam>
        /// <param name="parent">Parent object type for containment type.</param>
        /// <param name="containment">Containment object.</param>
        /// <param name="isCreate">Is this create or update?</param>
        /// <returns>Created base entity object.</returns>
        [GraphMethod(true)]
        public T AddContainment<T>(GraphObject parent, T containment) where T : GraphObject
        {
            return this.AddOrUpdateContainment(parent, containment, true) as T;
        }

        /// <summary>
        /// Add or update the directory object on the cloud.
        /// </summary>
        /// <typeparam name="T">Type of containment to be added.</typeparam>
        /// <param name="parent">Parent object type for containment type.</param>
        /// <param name="containment">Containment object.</param>
        /// <param name="isCreate">Is this create or update?</param>
        /// <returns>Created base entity object.</returns>
        [GraphMethod(true)]
        public T UpdateContainment<T>(GraphObject parent, T containment) where T : GraphObject
        {
            return this.AddOrUpdateContainment(parent, containment, false) as T;
        }
        
        /// <summary>
        /// List the containment objects of the given type
        /// </summary>
        /// <param name="parent">Parent object type for containment type.</param>
        /// <param name="containmentType">Containment object type.</param>
        /// <param name="linkToNextPage">Directory object type.</param>
        /// <param name="filter">OData filter.</param>
        /// <returns>Paged collection of results.</returns>
        [GraphMethod(true)]
        public virtual PagedResults<GraphObject> ListContainments(
            GraphObject parent, Type containmentType, string linkToNextPage, FilterGenerator filter)
        {
            Uri requestUri;
            return SerializationHelper.DeserializeJsonResponse<GraphObject>(
                this.ListContainmentsCore(
                    parent, containmentType, linkToNextPage, filter, out requestUri), requestUri);
        }

        /// <summary>
        /// List the containment objects of the given type
        /// </summary>
        /// <param name="parent">Parent object type for containment type.</param>
        /// <param name="linkToNextPage">Directory object type.</param>
        /// <param name="filter">OData filter.</param>
        /// <returns>Paged collection of results.</returns>
        [GraphMethod(true)]
        public virtual PagedResults<T> ListContainments<T>(
            GraphObject parent, string linkToNextPage, FilterGenerator filter) where T : GraphObject
        {
            Uri requestUri;
            return SerializationHelper.DeserializeJsonResponse<T>(
                this.ListContainmentsCore(parent, typeof(T), linkToNextPage, filter, out requestUri), requestUri);
        }

        /// <summary>
        /// Get a single containment object
        /// </summary>
        /// <param name="parent">Parent object type for containment type.</param>
        /// <param name="containmentType">Type of containment object.</param>
        /// <param name="objectId">Object id of the containment object.</param>
        /// <returns>Entity with the specified object id.</returns>
        [GraphMethod(true)]
        public virtual T GetContainment<T>(GraphObject parent, string containmentObjectId) where T : GraphObject
        {
            return this.GetContainment(parent, typeof(T), containmentObjectId) as T;
        }

        /// <summary>
        /// Get a single containment object
        /// </summary>
        /// <param name="parent">Parent object type for containment type.</param>
        /// <param name="containmentType">Type of containment object.</typeparam>
        /// <param name="objectId">Object id of the containment object.</param>
        /// <returns>Entity with the specified object id.</returns>
        [GraphMethod(true)]
        public virtual GraphObject GetContainment(GraphObject parent, Type containmentType, string containmentObjectId)
        {
            Uri listUri = Utils.GetRequestUri(this, parent.GetType(), parent.ObjectId, containmentType, containmentObjectId);
            if (this.returnBatchItem.Value)
            {
                this.batchRequestItems.Value.Add(new BatchRequestItem(HttpVerb.GET, false, listUri, null, null));
                return null;
            }

            Logger.Instance.Info("Retrieving {0}", listUri);

            byte[] rawResponse = this.ClientConnection.DownloadData(listUri, null);
            PagedResults<GraphObject> pagedResults = SerializationHelper.DeserializeJsonResponse<GraphObject>(
                Encoding.UTF8.GetString(rawResponse), listUri);

            return pagedResults.Results.FirstOrDefault();
        }

        /// <summary>
        /// Delete the containment object on the cloud.
        /// </summary>
        /// <param name="parent">Parent object of the containment.</param>
        /// <param name="containment">Containment object.</param>
        [GraphMethod(true)]
        public virtual void DeleteContainment(GraphObject parent, GraphObject containment)
        {
            Utils.ThrowIfNullOrEmpty(parent, "parent");
            Utils.ThrowIfNullOrEmpty(containment, "containment");
            Utils.ThrowArgumentExceptionIfNullOrEmpty(parent.ObjectId, "parent.ObjectId");
            Utils.ThrowArgumentExceptionIfNullOrEmpty(containment.ObjectId, "containment.ObjectId");

            Uri deleteUri = Utils.GetRequestUri(
                this, parent.GetType(), parent.ObjectId, containment.GetType(), containment.ObjectId);

            if (this.returnBatchItem.Value)
            {
                this.batchRequestItems.Value.Add(new BatchRequestItem(HttpVerb.DELETE, true, deleteUri, null, null));
                return;
            }

            this.ClientConnection.DeleteRequest(deleteUri);
        }

        #endregion

        #endregion

        #region Link manipulations

        /// <summary>
        /// Get the specified links (single or multi valued)
        /// </summary>
        /// <param name="graphObject">Graph object.</param>
        /// <param name="linkProperty">Link property.</param>
        /// <param name="nextPageToken">Token for next page.</param>
        /// <returns>Paged collection of results.</returns>
        [GraphMethod(true)]
        public virtual PagedResults<GraphObject> GetLinkedObjects(
            GraphObject graphObject, LinkProperty linkProperty, string nextPageToken)
        {
            return this.GetLinkedObjects(graphObject, linkProperty, nextPageToken, -1);
        }

        /// <summary>
        /// Get the specified links (single or multi valued)
        /// </summary>
        /// <param name="graphObject">Directory object.</param>
        /// <param name="linkProperty">Link name.</param>
        /// <param name="nextPageToken">Token for next page.</param>
        /// <param name="top">Max number of results per page.</param>
        /// <returns>Paged collection of results.</returns>
        [GraphMethod(true)]
        public virtual PagedResults<GraphObject> GetLinkedObjects(
            GraphObject graphObject, LinkProperty linkProperty, string nextPageToken, int top)
        {
            Utils.ValidateGraphObject(graphObject, "graphObject");

            Uri objectUri = Utils.GetRequestUri(
                this,
                graphObject.GetType(),
                graphObject.ObjectId,
                nextPageToken,
                top,
                Utils.GetLinkName(linkProperty));
            if (this.returnBatchItem.Value)
            {
                this.batchRequestItems.Value.Add(
                    new BatchRequestItem(HttpVerb.GET, true, objectUri, null, null));
                return null;
            }

            byte[] rawResponse = this.ClientConnection.DownloadData(objectUri, null);
            PagedResults<GraphObject> pagedResults = SerializationHelper.DeserializeJsonResponse<GraphObject>(
                Encoding.UTF8.GetString(rawResponse), objectUri);

            return pagedResults;
        }

        /// <summary>
        /// Get all target values for this link (single and multi-valued)
        /// </summary>
        /// <param name="graphObject">Directory object.</param>
        /// <param name="linkProperty">Link property.</param>
        /// <returns>Paged collection of results.</returns>
        /// <remarks>
        /// This is NOT a transitive lookup, just a single level lookup.
        /// </remarks>
        public virtual IList<GraphObject> GetAllDirectLinks(GraphObject graphObject, LinkProperty linkProperty)
        {
            Utils.ValidateGraphObject(graphObject, "graphObject");

            List<GraphObject> linkResults = new List<GraphObject>();

            PagedResults<GraphObject> pagedResults = null;

            while (true)
            {
                pagedResults = this.GetLinkedObjects(
                    graphObject, 
                    linkProperty, 
                    pagedResults == null ? null : pagedResults.PageToken, 
                    -1);

                linkResults.AddRange(pagedResults.Results);

                if (pagedResults.IsLastPage)
                {
                    break;
                }
            }

            return linkResults;
        }

        /// <summary>
        /// Add a link.
        /// </summary>
        /// <param name="sourceObject">Source directory object.</param>
        /// <param name="targetObject">Target directory object.</param>
        /// <param name="linkProperty">Link property.</param>
        [GraphMethod(true)]      
        public virtual void AddLink(
            GraphObject sourceObject, GraphObject targetObject, LinkProperty linkProperty)
        {
            Utils.ValidateGraphObject(sourceObject, "sourceObject");
            Utils.ValidateGraphObject(targetObject, "targetObject");

            Uri linkUri = Utils.GetRequestUri(
                this,
                sourceObject.GetType(),
                sourceObject.ObjectId,
                Constants.LinksFragment,
                Utils.GetLinkName(linkProperty));

            Uri targetObjectUri = Utils.GetRequestUri(
                this,
                targetObject.GetType(),
                targetObject.ObjectId);

            Dictionary<string, string> postParameters = new Dictionary<string, string>()
            {
                { "url", targetObjectUri.ToString() }
            };

            string requestJson = JsonConvert.SerializeObject(postParameters);

            bool isSingleValued = Utils.GetLinkAttribute(sourceObject.GetType(), linkProperty).IsSingleValued;
            string methodName = isSingleValued ? HttpVerb.PUT : HttpVerb.POST;
            if (this.returnBatchItem.Value)
            {
                this.batchRequestItems.Value.Add(new BatchRequestItem(methodName, true, linkUri, null, requestJson));
                return;
            }

            this.ClientConnection.UploadString(
                linkUri, methodName, requestJson, null, null);
        }

        /// <summary>
        /// Delete the link (navigation property).
        /// </summary>
        /// <param name="sourceObject">Source directory object.</param>
        /// <param name="targetObject">Target directory object.</param>
        /// <param name="linkProperty">Link name.</param>
        [GraphMethod(true)]
        public virtual void DeleteLink(
            GraphObject sourceObject, GraphObject targetObject, LinkProperty linkProperty)
        {
            Utils.ValidateGraphObject(sourceObject, "sourceObject");

            Uri deleteUri;

            bool isSingleValued = Utils.GetLinkAttribute(sourceObject.GetType(), linkProperty).IsSingleValued;

            if (isSingleValued)
            {
                // If the link is single valued, the target object id should not be part of the Uri.
                deleteUri = Utils.GetRequestUri(
                    this,
                    sourceObject.GetType(),
                    sourceObject.ObjectId,
                    Constants.LinksFragment,
                    Utils.GetLinkName(linkProperty));
            }
            else
            {
                Utils.ValidateGraphObject(targetObject, "targetObject");

                deleteUri = Utils.GetRequestUri(
                    this,
                    sourceObject.GetType(),
                    sourceObject.ObjectId,
                    Constants.LinksFragment,
                    Utils.GetLinkName(linkProperty),
                    targetObject.ObjectId);
            }

            if (this.returnBatchItem.Value)
            {
                this.batchRequestItems.Value.Add(new BatchRequestItem(HttpVerb.DELETE, true, deleteUri, null, null));
                return;
            }

            this.ClientConnection.DeleteRequest(deleteUri);
        }

        #endregion

        #region Stream properties

        /// <summary>
        /// Get the deferred stream property.
        /// </summary>
        /// <param name="graphObject">Graph object.</param>
        /// <param name="graphProperty">Property name.</param>
        /// <returns>Memory stream for the byte buffer.</returns>
        /// <param name="acceptType">Accept type header value.</param>
        public virtual Stream GetStreamProperty(
            GraphObject graphObject, GraphProperty graphProperty, string acceptType)
        {
            Utils.ValidateGraphObject(graphObject, "graphObject");
            Utils.ThrowIfNullOrEmpty(acceptType, "acceptType");

            Uri requestUri = Utils.GetRequestUri(
                this, graphObject.GetType(), graphObject.ObjectId, Utils.GetPropertyName(graphProperty));

            WebHeaderCollection additionalHeaders = new WebHeaderCollection();
            additionalHeaders[HttpRequestHeader.ContentType] = acceptType;
            byte[] buffer = this.ClientConnection.DownloadData(requestUri, additionalHeaders);

            if (buffer != null)
            {
                return new MemoryStream(buffer);
            }

            return new MemoryStream();
        }

        /// <summary>
        /// Set the deferred stream property.
        /// </summary>
        /// <param name="graphObject">Graph object.</param>
        /// <param name="graphProperty">Property name.</param>
        /// <param name="memoryStream">Memory stream.</param>
        /// <param name="contentType">Content type.</param>
        public virtual void SetStreamProperty(
            GraphObject graphObject, GraphProperty graphProperty, MemoryStream memoryStream, string contentType)
        {
            Utils.ValidateGraphObject(graphObject, "graphObject");
            Utils.ThrowIfNullOrEmpty(memoryStream, "memoryStream");
            Utils.ThrowIfNullOrEmpty(contentType, "contentType");

            Uri requestUri = Utils.GetRequestUri(
                this, graphObject.GetType(), graphObject.ObjectId, Utils.GetPropertyName(graphProperty));

            WebHeaderCollection additionalHeaders = new WebHeaderCollection();
            additionalHeaders[HttpRequestHeader.ContentType] = contentType;

            this.ClientConnection.UploadData(requestUri, HttpVerb.PUT, memoryStream.ToArray(), additionalHeaders);
        }

        #endregion

        #region Batch requests
        /// <summary>
        /// Executes a given list of graph methods as a batch.
        /// </summary>
        /// <param name="batchRequests">List of delegates representing various graph calls.</param>
        /// <returns>Batch response.</returns>
        /// <exception cref="ArgumentException">Invalid set of batch requests.</exception>
        public virtual IList<BatchResponseItem> ExecuteBatch(params Expression<Action>[] batchRequests)
        {
            if (batchRequests == null || batchRequests.Length < 1)
            {
                throw new ArgumentException("Invalid batch request. Should contain 1 to 5 items.");
            }

            if (batchRequests.Length > 5)
            {
                throw new ArgumentException("Invalid batch request. Should contain 1 to 5 items.");
            }

            try
            {
                this.returnBatchItem.Value = true;
                foreach (Expression<Action> batchRequest in batchRequests)
                {
                    MethodCallExpression methodCallExp = (MethodCallExpression) batchRequest.Body;
                    Utils.ThrowIfNull(methodCallExp, "batchRequest");
                    GraphMethodAttribute graphMethodAttribute =
                        Utils.GetCustomAttribute<GraphMethodAttribute>(methodCallExp.Method, false);
                    if (graphMethodAttribute == null || graphMethodAttribute.SupportsBatching == false)
                    {
                        string message = String.Format(
                            "Batching is not supported for {0}.",
                            methodCallExp.Method.Name);
                        throw new InvalidOperationException(message);
                    }

                    batchRequest.Compile()();
                }

                if (this.batchRequestItems.Value.Count != batchRequests.Length)
                {
                    throw new InvalidOperationException("One or more requests does not support batching.");
                }

                return this.ExecuteBatch(this.batchRequestItems.Value);
            }
            finally
            {
                this.returnBatchItem.Value = false;
                this.batchRequestItems.Value.Clear();
            }
        }

        /// <summary>
        /// Executes a batch request item.
        /// </summary>
        /// <param name="batchRequests">Batch request items.</param>
        /// <returns>Batch response.</returns>
        /// <remarks>
        /// http://www.odata.org/documentation/odata-version-3-0/batch-processing/#BatchRequestBody
        /// NOTE: Batch items are sensitive to the ordering. Queries have to be executed in the batch order.
        /// Changesets can be executed in any order within the changeset.
        /// It is also possible to refer to the result of one batch item in another.
        /// None of these are supported now.
        /// </remarks>
        /// <exception cref="ArgumentException">Invalid set of batch requests.</exception>
        public virtual IList<BatchResponseItem> ExecuteBatch(IList<BatchRequestItem> batchRequests)
        {
            if (batchRequests == null || batchRequests.Count < 1)
            {
                throw new ArgumentException("Invalid batch request. Should contain 1 to 5 items.");
            }

            if (batchRequests.Count > 5)
            {
                throw new ArgumentException("Invalid batch request. Should contain 1 to 5 items.");
            }

            string batchUri = String.Format(
                CultureInfo.InvariantCulture,
                "{0}/$batch?{1}={2}",
                this.AadGraphEndpoint,
                Constants.QueryParameterNameApiVersion,
                this.GraphApiVersion);

            StringBuilder sb = new StringBuilder();

            // Generate the Query or Action batch parts first
            foreach (BatchRequestItem requestItem in batchRequests)
            {
                if (requestItem.IsChangesetRequired)
                {
                    requestItem.RequestId = this.ClientRequestId;
                    Guid changeSetId = Guid.NewGuid();
                    requestItem.ChangeSetId = changeSetId;
                    sb.Append(requestItem.ToString());
                    sb.AppendFormat("--changeset_{0}--", changeSetId.ToString());
                    sb.AppendLine();
                }
                else
                {
                    requestItem.RequestId = this.ClientRequestId;
                    sb.Append(requestItem.ToString());
                }
            }

            sb.AppendFormat("--batch_{0}--", this.ClientRequestId.ToString());

            WebHeaderCollection additionalHeaders = new WebHeaderCollection();
            additionalHeaders[HttpRequestHeader.ContentType] = String.Format(
                CultureInfo.InvariantCulture,
                "multipart/mixed; boundary=batch_{0}",
                this.ClientRequestId.ToString());

            WebHeaderCollection responseHeaders = new WebHeaderCollection();
            string responseBody = this.ClientConnection.UploadString(
                batchUri, HttpVerb.POST, sb.ToString(), additionalHeaders, responseHeaders);

            return SerializationHelper.DeserializeBatchResponse(
                this.ClientConnection.GetContentTypeOfLastResponse(responseHeaders), 
                responseBody, 
                batchRequests);
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Add or update the directory object on the cloud.
        /// </summary>
        /// <param name="parent">Parent object type for containment type.</param>
        /// <param name="containment">Containment object.</param>
        /// <param name="isCreate">Is this create or update?</param>
        /// <returns>Created base entity object.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="parent"/> or <paramref name="containment"/> is <see langword="null" />
        /// </exception>
        private GraphObject AddOrUpdateContainment(GraphObject parent, GraphObject containment, bool isCreate)
        {
            Utils.ThrowIfNullOrEmpty(parent, "parent");
            Utils.ThrowIfNullOrEmpty(containment, "containment");

            containment.ValidateProperties(isCreate);

            Uri createUri = Utils.GetRequestUri(
                this,
                parent.GetType(),
                parent.ObjectId,
                containment.GetType(),
                isCreate ? String.Empty : containment.ObjectId);

            string requestJson = JsonConvert.SerializeObject(Utils.GetSerializableGraphObject(containment));

            string methodName = isCreate ? HttpVerb.POST : HttpVerb.PATCH;
            if (this.returnBatchItem.Value)
            {
                this.batchRequestItems.Value.Add(new BatchRequestItem(methodName, true, createUri, null, requestJson));
                return null;
            }

            string responseJson = this.ClientConnection.UploadString(
                createUri, methodName, requestJson, null, null);

            PagedResults<GraphObject> pagedResults =
                SerializationHelper.DeserializeJsonResponse<GraphObject>(responseJson, createUri);

            if (pagedResults == null ||
                pagedResults.Results == null ||
                pagedResults.Results.Count != 1)
            {
                throw new InvalidOperationException("Unable to deserialize the response");
            }

            containment.ChangedProperties.Clear();
            pagedResults.Results[0].ChangedProperties.Clear();
            return pagedResults.Results[0];
        }

        /// <summary>
        /// Add or update the directory object on the cloud.
        /// </summary>
        /// <param name="entity">Object to be added or updated.</param>
        /// <param name="isCreate">Is this create or update?</param>
        /// <returns>Created base entity object.</returns>
        private GraphObject AddOrUpdate(GraphObject entity, bool isCreate)
        {
            Utils.ThrowIfNullOrEmpty(entity, "entity");
            entity.ValidateProperties(isCreate);

            Uri createUri = Utils.GetRequestUri(
                this,
                entity.GetType(),
                isCreate ? String.Empty : entity.ObjectId);

            string requestJson = JsonConvert.SerializeObject(Utils.GetSerializableGraphObject(entity));

            string methodName = isCreate ? HttpVerb.POST : HttpVerb.PATCH;
            if (this.returnBatchItem.Value)
            {
                this.batchRequestItems.Value.Add(new BatchRequestItem(methodName, true, createUri, null, requestJson));
                return null;
            }

            string responseJson = this.ClientConnection.UploadString(
                createUri, methodName, requestJson, null, null);

            PagedResults<GraphObject> pagedResults =
                SerializationHelper.DeserializeJsonResponse<GraphObject>(responseJson, createUri);

            if (pagedResults == null ||
                pagedResults.Results == null ||
                pagedResults.Results.Count != 1)
            {
                throw new InvalidOperationException("Unable to deserialize the response");
            }

            entity.ChangedProperties.Clear();
            pagedResults.Results[0].ChangedProperties.Clear();
            return pagedResults.Results[0];
        }

        /// <summary>
        /// List the directory objects of the given type
        /// </summary>
        /// <param name="objectType">Directory object type.</param>
        /// <param name="linkToNextPage">Directory object type.</param>
        /// <param name="filter">OData filter.</param>
        /// <param name="listUri">Set to the list uri.</param>
        /// <returns>Paged collection of results.</returns>
        private string ListCore(Type objectType, string linkToNextPage, FilterGenerator filter, out Uri listUri)
        {
            listUri = Utils.GetListUri(objectType, this, linkToNextPage, filter);

            if (this.returnBatchItem.Value)
            {
                this.batchRequestItems.Value.Add(new BatchRequestItem(HttpVerb.GET, false, listUri, null, null));
                return null;
            }

            Logger.Instance.Info("Retrieving {0}", listUri);

            byte[] rawResponse = this.ClientConnection.DownloadData(listUri, null);

            if (rawResponse == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(rawResponse);
        }

        /// <summary>
        /// Get a single graph object. You can optionally expand and select properties.
        /// </summary>
        /// <typeparam name="T">Type of object to be searched for.</typeparam>
        /// <param name="objectId">Object id of the directory object.</param>
        /// <param name="filterGenerator">Filter generator.</param>
        /// <param name="requestUri">Set to the request uri.</param>
        /// <returns>Entity with the specified object id.</returns>
        private string GetCore(Type objectType, string objectId, FilterGenerator filterGenerator, out Uri requestUri)
        {
            requestUri = Utils.GetRequestUri(this, objectType, objectId);

            if (filterGenerator != null && filterGenerator.ExpandProperty != LinkProperty.None)
            {
                if (filterGenerator.QueryFilter != null)
                {
                    throw new ArgumentException("Filter expressions are not allowed when querying a single object.");
                }

                if (filterGenerator.OrderByProperty != GraphProperty.None)
                {
                    throw new ArgumentException("OrderBy is not allowed when querying a single object.");
                }

                UriBuilder uriBuilder = new UriBuilder(requestUri);
                Utils.BuildQueryFromFilter(uriBuilder, filterGenerator);
                requestUri = uriBuilder.Uri;
            }

            Logger.Instance.Info("Retrieving {0}", requestUri);

            if (this.returnBatchItem.Value)
            {
                this.batchRequestItems.Value.Add(new BatchRequestItem(HttpVerb.GET, false, requestUri, null, null));
                return null;
            }

            return Encoding.UTF8.GetString(this.ClientConnection.DownloadData(requestUri, null));
        }

        /// <summary>
        /// List the containment objects of the given type
        /// </summary>
        /// <param name="parent">Parent object type for containment type.</param>
        /// <param name="containmentType">Containment object type.</param>
        /// <param name="linkToNextPage">Directory object type.</param>
        /// <param name="filter">OData filter.</param>
        /// <param name="listUri">Set to the request uri.</param>
        /// <returns>Paged collection of results.</returns>
        private string ListContainmentsCore(
            GraphObject parent, Type containmentType, string linkToNextPage, FilterGenerator filter, out Uri listUri)
        {
            listUri = Utils.GetListUri(parent, containmentType, this, linkToNextPage, filter);
            if (this.returnBatchItem.Value)
            {
                this.batchRequestItems.Value.Add(new BatchRequestItem(HttpVerb.GET, false, listUri, null, null));
                return null;
            }

            Logger.Instance.Info("Retrieving {0}", listUri);

            byte[] rawResponse = this.ClientConnection.DownloadData(listUri, null);

            if (rawResponse == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(rawResponse);
        }

        #endregion
    }
}