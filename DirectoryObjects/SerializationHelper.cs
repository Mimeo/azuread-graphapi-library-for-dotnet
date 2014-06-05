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
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using Microsoft.Azure.ActiveDirectory.GraphClient.ErrorHandling;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Serializes the JSON response into a series of 
    /// </summary>
    public class SerializationHelper
    {
        /// <summary>
        /// List of available types.
        /// </summary>
        private static Type[] availableTypes = null;

        /// <summary>
        /// Type implementations for each OData (AAD) type.
        /// </summary>
        private static Dictionary<string, Type> typeImplementations = new Dictionary<string, Type>();

        /// <summary>
        /// Type implementations for each OData (AAD) type.
        /// </summary>
        private static Dictionary<string, Type> entitySetTypeMap = new Dictionary<string, Type>();

        /// <summary>
        /// Deserialize JSON response
        /// </summary>
        /// <typeparam name="T">Graph object to be deserialized into.</typeparam>
        /// <param name="json">Serialized JSON string.</param>
        /// <param name="requestUri">Request uri that returned the results.</param>
        /// <returns>Paged collection of results.</returns>
        public static PagedResults<T> DeserializeJsonResponse<T>(string json, Uri requestUri) where T : GraphObject
        {
            if (String.IsNullOrEmpty(json))
            {
                return null;
            }

            PopulateAvailableTypes();
            PagedResults<T> pagedResults = new PagedResults<T>();
            pagedResults.RequestUri = requestUri;

            JObject rootObject = JsonConvert.DeserializeObject(json) as JObject;

            if (rootObject == null)
            {
                throw new ArgumentException("Invalid json input.");
            }

            bool isGraphObject = false;
            bool isCollection = false;
            Type resultObjectType = null;
            if (rootObject[Constants.ODataMetadataKey] != null)
            {
                pagedResults.ODataMetadataType = rootObject[Constants.ODataMetadataKey].ToString();

                SerializationHelper.ParseMetadataType(
                    pagedResults.ODataMetadataType, out resultObjectType, out isGraphObject, out isCollection);
            }

            // Extract the next link if it exists
            JToken jtoken;
            if (rootObject.TryGetValue(Constants.ODataNextLink, out jtoken))
            {
                pagedResults.PageToken = jtoken.ToString();
            }

            // Check whether the json is an array or a single object.
            if (rootObject.TryGetValue(Constants.ODataValues, out jtoken))
            {
                // Even though a value attribute was found, there can be a single element inside the value
                if (isCollection)
                {
                    foreach (JToken valueItemToken in jtoken)
                    {
                        if (isGraphObject)
                        {
                            pagedResults.Results.Add(DeserializeGraphObject<T>(valueItemToken, resultObjectType));
                        }
                        else
                        {
                            // A collection of non directoryObject results.
                            pagedResults.MixedResults.Add(valueItemToken.ToString());
                        }
                    }
                }
                else
                {
                    pagedResults.MixedResults.Add(jtoken.ToString());
                }
            }
            else
            {
                if (isGraphObject)
                {
                    pagedResults.Results.Add(DeserializeGraphObject<T>(rootObject, resultObjectType));
                }
                else
                {
                    // Single value, non directoryObject result.
                    pagedResults.MixedResults.Add(rootObject.ToString());
                }
            }

            return pagedResults;
        }

        /// <summary>
        /// Deserializes a single directory object string based on the odata type.
        /// </summary>
        /// <typeparam name="T">Graph object to be deserialized into.</typeparam>
        /// <param name="dictionaryToken">JSON dictionary token.</param>
        /// <returns>Deserialized directory object.</returns>
        public static T DeserializeGraphObject<T>(JToken dictionaryToken, Type resultObjectType) where T : GraphObject
        {
            GraphObject graphObject = JsonConvert.DeserializeObject(
                dictionaryToken.ToString(),
                resultObjectType ?? typeof(T), 
                new AadJsonConverter()) as GraphObject;

            // Make sure that every object returned is of type GraphObject. 
            // The library does not understand any other type.
            Debug.Assert(graphObject != null);

            graphObject.TokenDictionary = dictionaryToken;

            graphObject.PropertiesMaterializedFromDeserialization = new List<string>(graphObject.ChangedProperties);

            // Clear all the properties being tracked for update.
            graphObject.ChangedProperties.Clear();
            T returnObject = graphObject as T;
            if (returnObject == null)
            {
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Unexpected type {0} obtained in response. Only objects of type {1} are expected.",
                    graphObject.GetType(),
                    typeof (T));
                throw new GraphException(message);
            }

            return returnObject;
        }

        /// <summary>
        /// Deserialize a batch response into individual response items.
        /// </summary>
        /// <param name="contentTypeHeader">Content type header.</param>
        /// <param name="responseString">Response string.</param>
        /// <param name="batchRequestItems">Batch request items.</param>
        /// <returns>Batch response items.</returns>
        public static List<BatchResponseItem> DeserializeBatchResponse(
            string contentTypeHeader, string responseString, IList<BatchRequestItem> batchRequestItems)
        {
            List<BatchResponseItem> batchResponseItems = new List<BatchResponseItem>();

            Utils.ThrowIfNullOrEmpty(contentTypeHeader, "contentTypeHeader");

            string[] splitContentTypeTokens = contentTypeHeader.Split(";".ToCharArray());
            string boundaryMarker = String.Empty;

            foreach (string contentTypeToken in splitContentTypeTokens)
            {
                if (contentTypeToken.Trim().StartsWith("boundary="))
                {
                    boundaryMarker = contentTypeToken.Trim().Substring("boundary=".Length);
                    break;
                }
            }

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(responseString)))
            {
                bool isCurrentBatchItemAFailure = false;
                int itemIndex = 0;
                using (StreamReader sr = new StreamReader(ms))
                {
                    WebHeaderCollection headers = null;

                    while (!sr.EndOfStream)
                    {
                        string currentLine = sr.ReadLine();

                        if (String.IsNullOrWhiteSpace(currentLine))
                        {
                            continue;
                        }

                        if (currentLine.StartsWith("HTTP/1.1 "))
                        {
                            // In a batch item, the http status code for successful changesets is also 200 OK.
                            isCurrentBatchItemAFailure = !currentLine.Contains("200 OK");
                            continue;
                        }

                        if (currentLine.StartsWith("{\"odata.metadata"))
                        {
                            PagedResults<GraphObject> pagedResults = SerializationHelper.DeserializeJsonResponse<GraphObject>(
                                currentLine, batchRequestItems[itemIndex].RequestUri);
                            itemIndex++;
                            BatchResponseItem batchResponseItem = new BatchResponseItem();
                            batchResponseItem.Failed = isCurrentBatchItemAFailure;
                            batchResponseItem.ResultSet = pagedResults;
                            batchResponseItems.Add(batchResponseItem);

                            // Add the response headers from the current batch to the response.
                            if (headers != null)
                            {
                                batchResponseItem.BatchHeaders.Add(headers);
                                headers = null;
                            }

                            continue;
                        }

                        if (currentLine.StartsWith("{\"odata.error"))
                        {
                            BatchResponseItem batchResponseItem = new BatchResponseItem();
                            batchResponseItem.Failed = isCurrentBatchItemAFailure;
                            batchResponseItem.Exception = ErrorResolver.ParseErrorMessageString(
                                HttpStatusCode.BadRequest, currentLine);
                            batchResponseItem.Exception.ResponseUri = batchRequestItems[itemIndex].RequestUri.ToString();
                            batchResponseItems.Add(batchResponseItem);

                            if (headers != null)
                            {
                                batchResponseItem.BatchHeaders.Add(headers);
                                headers = null;
                            }
                        }

                        string[] tokens = currentLine.Split(":".ToCharArray());
                        if (tokens != null && tokens.Length == 2)
                        {
                            if (headers == null)
                            {
                                headers = new WebHeaderCollection();
                            }

                            headers[tokens[0]] = tokens[1];
                        }
                    }
                }
            }

            return batchResponseItems;
        }

        /// <summary>
        /// Parse the OData metadata type to find out if this is a graph object, a collection etc..
        /// </summary>
        /// <param name="metadataKey">Metadata key.</param>
        /// <param name="targetObjectType">Set to the AAD object type retrieved from metadata.</param>
        /// <param name="isGraphObject">Set to whether this is a graph object.</param>
        /// <param name="isCollection">Set to whether this is a collection.</param>
        /// <remarks>
        /// We can either get a collection of directoryObject or a collection of some random values.
        /// For example, actions return a bool, list of string, guids etc... as response.
        /// We want to capture the list of objects or the single object.
        /// If the result is directoryObjects, we will populate the PagedResults.Results
        /// If the result is something else, we will poulate the PagedResults.MixedResults
        /// odata.metadata will look something like
        /// "https://graph.windows.net/4fd2b2f2-ea27-4fe5-a8f3-7b1a7c975f34/$metadata#directoryObjects"
        /// "https://graph.windows.net/4fd2b2f2-ea27-4fe5-a8f3-7b1a7c975f34/$metadata#directoryObjects/TenantDetail"
        /// "https://graph.windows.net/4fd2b2f2-ea27-4fe5-a8f3-7b1a7c975f34/$metadata#subscribedSkus"
        /// "https://graph.windows.net/4fd2b2f2-ea27-4fe5-a8f3-7b1a7c975f34/$metadata#Collection(Edm.String)"
        /// "https://graph.windows.net/4fd2b2f2-ea27-4fe5-a8f3-7b1a7c975f34/$metadata#directoryObjects/
        ///     Microsoft.WindowsAzure.ActiveDirectory.User/@Element"
        /// </remarks>
        public static void ParseMetadataType(
            string metadataKey, out Type targetObjectType, out bool isGraphObject, out bool isCollection)
        {
            isGraphObject = false;
            isCollection = false;
            targetObjectType = null;
            if (String.IsNullOrEmpty(metadataKey))
            {
                return;
            }

            string[] segments = metadataKey.Split(
                "/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            // https://graph.windows.net/contoso.com/metadata#directoryObjects/...
            if (segments.Length >= 4)
            {
                string typeSegment = segments[3];

                int indexOfHash = typeSegment.IndexOf('#');
                if (indexOfHash > 0)
                {
                    typeSegment = typeSegment.Substring(indexOfHash + 1);
                }

                SerializationHelper.TryGetTypeForEntitySet(typeSegment, out targetObjectType);
                isGraphObject = targetObjectType != null;

                // In case of entities, the metadata url will end with @Element if the result is a single value
                // In case of non-entities, the type name will start with Collection to indicate a  multi-valued result.
                if (isGraphObject)
                {
                    isCollection = !metadataKey.EndsWith("@Element");
                }
                else
                {
                    isCollection = typeSegment.StartsWith("Collection");
                }
            }            
        }

        /// <summary>
        /// Get the implentation type for the given aad type.
        /// </summary>
        /// <param name="entitySet">Entity set name.</param>
        /// <param name="implementationType">Set to the implementation type.</param>
        /// <returns>The implementing type.</returns>
        public static bool TryGetTypeForEntitySet(string entitySet, out Type implementationType)
        {
            return entitySetTypeMap.TryGetValue(entitySet, out implementationType);
        }

        /// <summary>
        /// Get the implentation type for the given aad type.
        /// </summary>
        /// <param name="aadType">AAD Odata type.</param>
        /// <param name="implementationType">Set to the implementation type.</param>
        /// <returns>The implementing type.</returns>
        public static bool TryGetImplementationForAadType(string aadType, out Type implementationType)
        {
            return typeImplementations.TryGetValue(aadType, out implementationType);
        }

        /// <summary>
        /// Populate the list of available types (one time only).
        /// </summary>
        private static void PopulateAvailableTypes()
        {
            if (SerializationHelper.availableTypes != null)
            {
                return;
            }

            SerializationHelper.availableTypes = Assembly.GetExecutingAssembly().GetTypes();
            
            // Scan through each type that has an Entity attribute.
            foreach (Type type in SerializationHelper.availableTypes)
            {
                EntityAttribute entityAttribute = Utils.GetCustomAttribute<EntityAttribute>(type, false);

                if (entityAttribute != null)
                {
                    if (!String.IsNullOrEmpty(entityAttribute.ODataType))
                    {
                        typeImplementations[entityAttribute.ODataType] = type;
                    }

                    if (!String.IsNullOrEmpty(entityAttribute.SetName))
                    {
                        entitySetTypeMap[entityAttribute.SetName] = type;
                    }
                }
            }
        }
    }
}
