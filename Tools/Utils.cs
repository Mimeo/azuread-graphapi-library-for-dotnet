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
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Web;
    using Newtonsoft.Json;

    /// <summary>
    /// Utility methods for request / response processing.
    /// </summary>
    public class Utils
    {
        /// <summary>
        /// Gets the object that needs to be sent to graph containing only updated properties.
        /// </summary>
        /// <param name="graphObject">Graph object to be enquired for changed properties.</param>
        /// <returns>List of key value pairs of changed property names and values.</returns>
        public static IDictionary<string, object> GetSerializableGraphObject(GraphObject graphObject)
        {
            Utils.ThrowIfNull(graphObject, "graphObject");
            IDictionary<string, object> serializableGraphObject = 
                new Dictionary<string, object>(graphObject.ChangedProperties.Count);
            foreach (string changedProperty in graphObject.ChangedProperties)
            {
                PropertyInfo propertyInfo = graphObject.GetType().GetProperty(changedProperty);
                JsonPropertyAttribute jsonPropertyName = 
                    Utils.GetCustomAttribute<JsonPropertyAttribute>(propertyInfo, true);
                serializableGraphObject.Add(jsonPropertyName.PropertyName, propertyInfo.GetValue(graphObject, null));
            }

            return serializableGraphObject;
        }

        /// <summary>
        /// Log the popular response headers.
        /// </summary>
        /// <param name="webHeaders">Web headers.</param>
        public static void LogResponseHeaders(WebHeaderCollection webHeaders)
        {
            if (webHeaders != null)
            {
                Logger.Instance.Info(
                    "{0}: {1}",
                    Constants.HeaderAadClientRequestId,
                    webHeaders[Constants.HeaderAadClientRequestId]);
                Logger.Instance.Info(
                    "{0}: {1}",
                    Constants.HeaderAadRequestId,
                    webHeaders[Constants.HeaderAadRequestId]);
                Logger.Instance.Info(
                    "{0}: {1}",
                    Constants.HeaderAadServerName,
                    webHeaders[Constants.HeaderAadServerName]);
            }
        }

        /// <summary>
        /// Gets the uri for listing the directory objects.
        /// </summary>
        /// <param name="parent">Parent object if the list is for containment type.</param>
        /// <param name="graphConnection">Call context.</param>
        /// <param name="nextLink">Link to the next set of results.</param>
        /// <param name="objectType">Directory object type.</param>
        /// <param name="filter">Filter expression generator.</param>
        /// <returns>Uri for listing the directory objects.</returns>
        /// <exception cref="UriFormatException">Invalid format for next link.</exception>
        /// <exception cref="ArgumentNullException">Invalid call context or object type.</exception>
        public static Uri GetListUri(
            GraphObject parent,
            Type objectType, 
            GraphConnection graphConnection, 
            string nextLink, 
            FilterGenerator filter)
        {
            Utils.ThrowIfNullOrEmpty(graphConnection, "graphConnection");
            Utils.ThrowIfNullOrEmpty(objectType, "objectType");

            if (filter == null)
            {
                // Generate a dummy filter
                // Makes it easy to add the api-version parameter.
                filter = new FilterGenerator();
            }

            // Get the entity attribute.
            EntityAttribute entityAttribute = Utils.GetCustomAttribute<EntityAttribute>(objectType, true);

            // Build a base uri for both paged and non paged queries.
            UriBuilder uriBuilder;
            string baseUri = graphConnection.AadGraphEndpoint;

            if (!String.IsNullOrEmpty(nextLink))
            {
                string formattedNextLink = String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/{1}",
                    baseUri,
                    nextLink);
                uriBuilder = new UriBuilder(formattedNextLink);
            }
            else
            {
                if (parent != null)
                {
                    baseUri = String.Format(
                        "{0}/{1}/{2}",
                        baseUri,
                        Utils.GetCustomAttribute<EntityAttribute>(parent.GetType(), true).SetName,
                        parent.ObjectId);
                }
                
                baseUri = String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/{1}",
                    baseUri,
                    entityAttribute.SetName);
                uriBuilder = new UriBuilder(baseUri);
            }

            filter[Constants.QueryParameterNameApiVersion] = graphConnection.GraphApiVersion;
            Utils.BuildQueryFromFilter(uriBuilder, filter);

            return uriBuilder.Uri;
        }

        /// <summary>
        /// Gets the uri for listing the directory objects.
        /// </summary>
        /// <param name="graphConnection">Call context.</param>
        /// <param name="nextLink">Link to the next set of results.</param>
        /// <param name="objectType">Directory object type.</param>
        /// <param name="filter">Filter expression generator.</param>
        /// <returns>Uri for listing the directory objects.</returns>
        /// <exception cref="UriFormatException">Invalid format for next link.</exception>
        /// <exception cref="ArgumentNullException">Invalid call context or object type.</exception>
        public static Uri GetListUri(
            Type objectType,
            GraphConnection graphConnection,
            string nextLink,
            FilterGenerator filter)
        {
            return GetListUri(null, objectType, graphConnection, nextLink, filter);
        }

        /// <summary>
        /// Gets the uri for listing the directory objects.
        /// </summary>
        /// <param name="graphConnection">Call context.</param>
        /// <param name="nextLink">Link to the next set of results.</param>
        /// <typeparam name="T">Directory object type.</typeparam>
        /// <param name="filter">Filter expression generator.</param>
        /// <returns>Uri for listing the directory objects.</returns>
        /// <exception cref="UriFormatException">Invalid format for next link.</exception>
        /// <exception cref="ArgumentNullException">Invalid filter.</exception>
        public static Uri GetListUri<T>(
            GraphConnection graphConnection, string nextLink, FilterGenerator filter) where T : DirectoryObject
        {
            return GetListUri(typeof(T), graphConnection, nextLink, filter);
        }

        /// <summary>
        /// Get the Uri that refers to a single directory object.
        /// </summary>
        /// <typeparam name="T">Type of the directory object.</typeparam>
        /// <param name="graphConnection">Call context.</param>
        /// <param name="objectId">Object id (Optional).</param>
        /// <param name="fragments">Optional fragments.</param>
        /// <returns>Uri to refer to a specific object.</returns>
        public static Uri GetRequestUri<T>(
            GraphConnection graphConnection, 
            string objectId,
            params string[] fragments) where T : DirectoryObject
        {
            return Utils.GetRequestUri(
                graphConnection, typeof(T), objectId, null, -1, fragments);
        }

        /// <summary>
        /// Get the Uri that refers to a single directory object.
        /// </summary>
        /// <typeparam name="T">Type of the directory object.</typeparam>
        /// <param name="graphConnection">Call context.</param>
        /// <param name="objectId">Object id (Optional).</param>
        /// <param name="nextLink">Optional next link.</param>
        /// <param name="top">Optional top parameter.</param>
        /// <param name="fragments">Optional fragments.</param>
        /// <returns>Uri to refer to a specific object.</returns>
        public static Uri GetRequestUri<T>(
            GraphConnection graphConnection,
            string objectId,
            string nextLink,
            int top,
            params string[] fragments) where T : DirectoryObject
        {
            return Utils.GetRequestUri(
                graphConnection, typeof(T), objectId, nextLink, top, fragments);
        }

        /// <summary>
        /// Get the object uri for the specified type.
        /// </summary>
        /// <param name="graphConnection">Call context.</param>
        /// <param name="typeOfEntityObject">Type of object.</param>
        /// <param name="objectId">Object id (optional)</param>
        /// <param name="fragments">Optional url fragments.</param>
        /// <returns>Request uri.</returns>
        public static Uri GetRequestUri(
            GraphConnection graphConnection,
            Type typeOfEntityObject,
            string objectId,
            params string[] fragments)
        {
            return Utils.GetRequestUri(
                graphConnection, typeOfEntityObject, objectId, null, -1, fragments);
        }

        /// <summary>
        /// Get the object uri for the specified type.
        /// </summary>
        /// <param name="graphConnection">Call context.</param>
        /// <param name="typeOfEntityObject">Type of object.</param>
        /// <param name="objectId">Object id (optional)</param>
        /// <param name="fragments">Optional url fragments.</param>
        /// <returns>Request uri.</returns>
        public static Uri GetRequestUri(
            GraphConnection graphConnection,
            Type parentType,
            string parentObjectId,
            Type containmentType,
            string containmentObjectId,
            params string[] fragments)
        {
            EntityAttribute containmentEntityAttribute = Utils.GetCustomAttribute<EntityAttribute>(containmentType, true);
            StringBuilder containmentUriEntry =  new StringBuilder(containmentEntityAttribute.SetName);
            if (!string.IsNullOrEmpty(containmentObjectId))
            {
                containmentUriEntry.AppendFormat("/{0}", containmentObjectId);
            }

            IList<string> fragmentsList = fragments == null ? new List<string>() : fragments.ToList();
            fragmentsList.Add(containmentUriEntry.ToString());

            return Utils.GetRequestUri(
                graphConnection, parentType, parentObjectId, null, -1, fragmentsList.ToArray());
        }


        /// <summary>
        /// Get the object uri for the specified type.
        /// </summary>
        /// <param name="graphConnection">Call context.</param>
        /// <param name="typeOfEntityObject">Type of object.</param>
        /// <param name="objectId">Object id (optional)</param>
        /// <param name="nextLink">Optional next link.</param>
        /// <param name="top">Optional top (less than zero is ignored)</param>
        /// <param name="fragments">Optional url fragments.</param>
        /// <returns>Request uri.</returns>
        public static Uri GetRequestUri(
            GraphConnection graphConnection,
            Type typeOfEntityObject,
            string objectId,
            string nextLink,
            int top,
            params string[] fragments)
        {
            StringBuilder sb = new StringBuilder();
            string baseUri = graphConnection.AadGraphEndpoint;

            if (!String.IsNullOrEmpty(nextLink))
            {
                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{0}/{1}",
                    baseUri,
                    nextLink);
            }
            else
            {
                if (typeOfEntityObject != null)
                {
                    // TODO: Cache the EntityAttribute
                    EntityAttribute entityAttribute = Utils.GetCustomAttribute<EntityAttribute>(typeOfEntityObject, true);

                    sb.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "{0}/{1}",
                        baseUri,
                        entityAttribute.SetName);
                }
                else
                {
                    sb.Append(baseUri);

                }

                if (!String.IsNullOrEmpty(objectId))
                {
                    sb.AppendFormat(
                        CultureInfo.InvariantCulture, "/{0}", objectId);
                }

                foreach (string fragment in fragments)
                {
                    sb.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "/{0}",
                        fragment);
                }
            }

            UriBuilder uriBuilder = new UriBuilder(sb.ToString());

            Utils.AddQueryParameter(
                uriBuilder,
                Constants.QueryParameterNameApiVersion,
                graphConnection.GraphApiVersion,
                true);

            if (top > 0)
            {
                Utils.AddQueryParameter(
                    uriBuilder,
                    Constants.TopOperator,
                    top.ToString(),
                    true);
            }

            return uriBuilder.Uri;
        }

        /// <summary>
        /// Adds a query parameter to a URI.
        /// </summary>
        /// <param name="uriBuilder">Uri builder.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter to be added.</param>
        /// <param name="overwriteExisting">
        /// If true, then overwrite the parameter of the same name.
        /// </param>
        public static void AddQueryParameter(UriBuilder uriBuilder, string name, string value, bool overwriteExisting)
        {
            NameValueCollection queryArguments = HttpUtility.ParseQueryString(uriBuilder.Query);

            if (String.IsNullOrEmpty(queryArguments[Constants.QueryParameterNameApiVersion]) || overwriteExisting)
            {
                queryArguments[name] = value;
            }

            uriBuilder.Query = ToQueryString(queryArguments);
        }

        /// <summary>
        /// Constructs a query string based on the key value pairs.
        /// </summary>
        /// <param name="queryArguments">A collection of key value pairs.</param>
        /// <returns>The URI-encoded query string.</returns>
        /// <remarks>
        /// The default .NET ToQueryString has issues with localized characters.
        /// </remarks>
        public static string ToQueryString(NameValueCollection queryArguments)
        {
            if (queryArguments == null || queryArguments.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            foreach (string key in queryArguments)
            {
                string encodedKey = key == null ? string.Empty : Uri.EscapeDataString(key) + "=";
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }

                string[] values = queryArguments.GetValues(key);
                if (values == null || values.Length == 0)
                {
                    sb.Append(encodedKey);
                }
                else
                {
                    for (int j = 0; j < values.Length; j++)
                    {
                        if (j > 0)
                        {
                            sb.Append("&");
                        }

                        sb.Append(encodedKey);
                        string value = values[j];
                        if (!string.IsNullOrEmpty(value))
                        {
                            sb.Append(Uri.EscapeDataString(value));
                        }
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Build the query parameters from the filter.
        /// </summary>
        /// <param name="uriBuilder">Uri builder.</param>
        /// <param name="filter">FilterGenerator values.</param>
        public static void BuildQueryFromFilter(UriBuilder uriBuilder, FilterGenerator filter)
        {
            foreach (string queryArgumentName in filter.Names)
            {
                Utils.AddQueryParameter(
                    uriBuilder,
                    queryArgumentName,
                    filter[queryArgumentName],
                    true);
            }

            if (filter.ExpandProperty != LinkProperty.None)
            {
                Utils.AddQueryParameter(
                    uriBuilder,
                    Constants.ExpandOperator,
                    Utils.GetLinkName(filter.ExpandProperty),
                    true);
            }

            if (filter.OrderByProperty != GraphProperty.None)
            {
                Utils.AddQueryParameter(
                    uriBuilder,
                    Constants.OrderByOperator,
                    Utils.GetPropertyName(filter.OrderByProperty),
                    true);
            }

            string filterQueryArgument;
            
            // Check if a Raw filter is specified.
            if (!String.IsNullOrEmpty(filter.OverrideQueryFilter))
            {
                if (filter.QueryFilter != null)
                {
                    throw new InvalidOperationException(
                        "Both QueryFilter and OverrideQueryFilter cannot be used at the same time.");
                }

                filterQueryArgument = filter.OverrideQueryFilter;

                const string filterOperator = Constants.FilterOperator + "=";
                                        
                if (filterQueryArgument.StartsWith(filterOperator))
                {
                    filterQueryArgument = filterQueryArgument.Substring(filterOperator.Length);
                }
            }
            else
            {
                filterQueryArgument = Utils.GetFilterQueryString(filter.QueryFilter);                
            }

            if (!String.IsNullOrEmpty(filterQueryArgument))
            {
                Utils.AddQueryParameter(
                    uriBuilder,
                    Constants.FilterOperator,
                    filterQueryArgument,
                    true);
            }
        }

        /// <summary>
        /// Get the filter query url argument from the expression tree.
        /// </summary>
        /// <param name="expression">Filter expression.</param>
        /// <returns>OData filter query string.</returns>
        /// <exception cref="ArgumentException">Unsupported expression.</exception>
        public static string GetFilterQueryString(Expression expression)
        {
            if (expression == null)
            {
                return String.Empty;
            }

            BinaryExpression binaryExpression = expression as BinaryExpression;

            if (binaryExpression != null)
            {
                ExpressionHelper.ValidateBinaryExpression(binaryExpression);

                switch (binaryExpression.NodeType)
                {
                    // If this is a leaf node, return the OData filter expression for the binary leaf expression.
                    case ExpressionType.Equal:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThanOrEqual:
                        ExpressionHelper.ValidateLeafExpression(binaryExpression);
                        return String.Format(
                            CultureInfo.InvariantCulture,
                            "{0} {1} {2}",
                            ExpressionHelper.GetPropertyName(binaryExpression),
                            ExpressionHelper.GetODataOperator(binaryExpression.NodeType),
                            ExpressionHelper.GetPropertyValue(binaryExpression));
                    case ExpressionType.And:
                    case ExpressionType.Or:
                        ExpressionHelper.ValidateConjunctiveExpression(binaryExpression);
                        return String.Format(
                            CultureInfo.InvariantCulture,
                            "({0}) {1} ({2})",
                            Utils.GetFilterQueryString(binaryExpression.Left),
                            ExpressionHelper.GetODataConjuctiveOperator(binaryExpression.NodeType),
                            Utils.GetFilterQueryString(binaryExpression.Right));
                    default:
                        throw new ArgumentException("Unsupported binary expression.");
                }
            }

            MethodCallExpression methodCallExpression = expression as MethodCallExpression;

            if (methodCallExpression != null)
            {
                if (methodCallExpression.Method == ExpressionHelper.StartsWithMethodInfo)
                {
                    if (methodCallExpression.Object == null)
                    {
                        throw new ArgumentException("Unsupported StartsWith expression.");
                    }

                    string propertyName =
                        ExpressionHelper.GetPropertyName(methodCallExpression.Object as MemberExpression);

                    string propertyValue =
                        ExpressionHelper.GetPropertyValue(methodCallExpression.Arguments[0] as ConstantExpression);

                    return String.Format(
                        CultureInfo.InvariantCulture,
                        "startswith({0},{1})",
                        propertyName,
                        propertyValue);
                }

                // Any can come in 3 flavors
                // Any value is present
                // Any value matches the argument
                // Any value startswith the argument
                // Currently, only the second one is supported.
                if (methodCallExpression.Method.Name.Equals("Any") &&
                    methodCallExpression.Arguments.Count == 1 &&
                    methodCallExpression.Method.ReturnType == typeof (bool) &&
                    methodCallExpression.Arguments[0].NodeType == ExpressionType.Constant)
                {
                    string propertyName =
                        ExpressionHelper.GetPropertyName(methodCallExpression.Object as MemberExpression);

                    string propertyValue =
                        ExpressionHelper.GetPropertyValue(methodCallExpression.Arguments[0] as ConstantExpression);

                    return String.Format(
                        CultureInfo.InvariantCulture,
                        "{0}/any(c:c eq {1})",
                        propertyName,
                        propertyValue);                    
                }
            }

            throw new ArgumentException("Unsupported expression.");
        }


        /// <summary>
        /// HexToBinDecode the raw access token into a claim dictionary.
        /// </summary>
        /// <param name="accessToken">Access token.</param>
        /// <returns>Tenant id, extracted from the access token.</returns>
        public static string GetTenantId(string accessToken)
        {
            Utils.ThrowIfNullOrEmpty(accessToken, "accessToken");

            string[] tokenFragments = accessToken.Split(".".ToCharArray());

            if (tokenFragments.Length != 3)
            {
                throw new ArgumentException("accessToken");
            }

            string paddedToken = tokenFragments[1];

            switch (paddedToken.Length % 4)
            {
                case 0:
                    break;
                case 1:
                    throw new ArgumentException("accessToken");
                case 2:
                    paddedToken = paddedToken + "==";
                    break;
                case 3:
                    paddedToken = paddedToken + "=";
                    break;
            }

            string tenantId = Constants.CommonTenantName;

            try
            {
                string decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(paddedToken));
                Dictionary<string, object> claims = JsonConvert.DeserializeObject<Dictionary<string, object>>(decodedToken);

                if (claims == null || !claims.ContainsKey(Constants.ClaimNameTenantId))
                {
                    throw new ArgumentException("Invalid accessToken. Tenant id claim was not found.");
                }

                tenantId = claims[Constants.ClaimNameTenantId] as string;
            }
            catch (JsonReaderException jsonReaderException)
            {
                Logger.Instance.Warning("Unable to parse the token");
            }

            return tenantId;
        }

        /// <summary>
        /// Gets a custom attribute from the source type.
        /// </summary>
        /// <typeparam name="T">Type of the attribute.</typeparam>
        /// <param name="sourceType">Source type to select the attribute from.</param>
        /// <param name="isRequired">Is this a required property?</param>
        /// <returns>Custom attribute.</returns>
        public static T GetCustomAttribute<T>(Type sourceType, bool isRequired) where T : Attribute
        {
            Utils.ThrowIfNull(sourceType, "sourceType");

            object[] customAttributes = sourceType.GetCustomAttributes(typeof(T), false);

            if (customAttributes == null || customAttributes.Length != 1)
            {
                if (isRequired)
                {
                    throw new ArgumentException("T", "Unable to get details about this property from the proxy.");
                }

                return default(T);
            }

            T customAttribute = customAttributes[0] as T;

            if (customAttribute == null && isRequired)
            {
                throw new ArgumentException("T", "Unable to get details about this property from the proxy.");
            }

            return customAttribute;
        }

        /// <summary>
        /// Gets a custom attribute from a property.
        /// </summary>
        /// <typeparam name="T">Type of the attribute.</typeparam>
        /// <param name="propertyInfo">Property info to select the attribute from.</param>
        /// <param name="isRequired">Is this property required?</param>
        /// <returns>Custom attribute.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyInfo"/> is null.</exception>
        /// <exception cref="ArgumentException">The property is not a valid one for this operation.</exception>
        public static T GetCustomAttribute<T>(PropertyInfo propertyInfo, bool isRequired) where T : Attribute
        {
            Utils.ThrowIfNull(propertyInfo, "propertyInfo");

            object[] customAttributes = propertyInfo.GetCustomAttributes(typeof(T), false);

            if (customAttributes == null || customAttributes.Length != 1)
            {
                if (isRequired)
                {
                    throw new ArgumentException("T", "Unable to get details about this entity from the proxy.");
                }

                return default(T);
            }

            T customAttribute = customAttributes[0] as T;

            if (customAttribute == null && isRequired)
            {
                throw new ArgumentException("T", "Unable to get details about this entity from the proxy.");
            }

            return customAttribute;
        }

        /// <summary>
        /// Gets a custom attribute from a method.
        /// </summary>
        /// <typeparam name="T">Type of the attribute.</typeparam>
        /// <param name="propertyInfo">Method info to select the attribute from.</param>
        /// <param name="isRequired">Is this property required?</param>
        /// <returns>Custom attribute.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="methodInfo"/> is null.</exception>
        /// <exception cref="ArgumentException">The property is not a valid one for this operation.</exception>
        public static T GetCustomAttribute<T>(MethodInfo methodInfo, bool isRequired) where T : Attribute
        {
            Utils.ThrowIfNull(methodInfo, "methodInfo");

            object[] customAttributes = methodInfo.GetCustomAttributes(typeof(T), false);

            if (customAttributes == null || customAttributes.Length != 1)
            {
                if (isRequired)
                {
                    throw new ArgumentException("T", "Unable to get information about this method from the proxy.");
                }

                return default(T);
            }

            T customAttribute = customAttributes[0] as T;

            if (customAttribute == null && isRequired)
            {
                throw new ArgumentException("T", "Unable to get information about this method from the proxy.");
            }

            return customAttribute;
        }


        /// <summary>
        /// Validate that the graph object is not null and has a valid objectid.
        /// </summary>
        /// <param name="graphObject">Graph object.</param>
        /// <param name="parameterName">Name of the parameter, will be quoted in the message.</param>
        /// <exception cref="ArgumentNullException">Graph object is null.</exception>
        /// <exception cref="ArgumentException">If the graph object is invalid.</exception>
        public static void ValidateGraphObject(GraphObject graphObject, string parameterName)
        {
            Utils.ThrowIfNullOrEmpty(graphObject, parameterName);
            Utils.ThrowArgumentExceptionIfNullOrEmpty(graphObject.ObjectId, parameterName);
        }

        /// <summary>
        /// Helper method to throw if the object is null.
        /// </summary>
        /// <param name="toBeChecked">Valued to be checked.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        public static void ThrowIfNull(object toBeChecked, string parameterName)
        {
            if (toBeChecked == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        /// <summary>
        /// Helper method to throw if the object is null.
        /// </summary>
        /// <param name="toBeChecked">Valued to be checked.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        public static void ThrowIfNullOrEmpty(object toBeChecked, string parameterName)
        {
            if (toBeChecked is string)
            {
                string toBeCheckedString = toBeChecked as string;

                if (String.IsNullOrEmpty(toBeCheckedString))
                {
                    throw new ArgumentNullException(parameterName);
                }

                return;
            }

            if (toBeChecked == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            Array toBeCheckedArray = toBeChecked as Array;
            if (toBeCheckedArray != null && toBeCheckedArray.Length == 0)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        /// <summary>
        /// Helper method to throw if the object is null.
        /// </summary>
        /// <param name="toBeChecked">Valued to be checked.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        public static void ThrowArgumentExceptionIfNullOrEmpty(object toBeChecked, string parameterName)
        {
            if (toBeChecked is string)
            {
                string toBeCheckedString = toBeChecked as string;

                if (String.IsNullOrEmpty(toBeCheckedString))
                {
                    throw new ArgumentException(parameterName + " is invalid");
                }

                return;
            }

            if (toBeChecked == null)
            {
                throw new ArgumentException(parameterName + " is invalid");
            }
        }

        /// <summary>
        /// Lower case the first letter of the graph property name.
        /// </summary>
        /// <param name="graphProperty">Graph property.</param>
        /// <returns>First letter will be lower case.</returns>
        public static string GetPropertyName(GraphProperty graphProperty)
        {
            if (graphProperty == GraphProperty.None || PropertyNameMap.NameMap.Count <= (int) graphProperty)
            {                
                throw new ArgumentException("Invalid graph property");
            }

            return PropertyNameMap.NameMap[(int) graphProperty];
        }

        /// <summary>
        /// Lower case the first letter of the link property name.
        /// </summary>
        /// <param name="linkProperty">Link property.</param>
        /// <returns>First letter will be lower case.</returns>
        public static string GetLinkName(LinkProperty linkProperty)
        {
            if (linkProperty == LinkProperty.None || LinkNameMap.NameMap.Count <= (int) linkProperty)
            {
                throw new ArgumentException("Invalid link property");
            }

            return LinkNameMap.NameMap[(int) linkProperty];
        }

        /// <summary>
        /// Gets the link attribute for the link property.
        /// </summary>
        /// <param name="entityType">Entity type.</param>
        /// <param name="linkProperty">Link property.</param>
        /// <returns>Link attribute.</returns>
        public  static LinkAttribute GetLinkAttribute(Type entityType, LinkProperty linkProperty)
        {            
            LinkAttribute linkAttribute = Utils.GetLinkAttribute(entityType, linkProperty.ToString());
            Utils.ThrowIfNull(linkAttribute, "Unable to lookup link information in the proxy.");

            return linkAttribute;
        }

        /// <summary>
        /// Gets the link attribute for the link property.
        /// </summary>
        /// <param name="entityType">Entity type.</param>
        /// <param name="linkProperty">Link property.</param>
        /// <returns>Link attribute.</returns>
        public static LinkAttribute GetLinkAttribute(Type entityType, string linkProperty)
        {
            Utils.ThrowIfNullOrEmpty(entityType, "entityType");
            PropertyInfo propertyInfo = entityType.GetProperty(linkProperty);
            Utils.ThrowIfNullOrEmpty(propertyInfo, "linkProperty");

            object[] customAttributes = propertyInfo.GetCustomAttributes(typeof(LinkAttribute), true);

            if (customAttributes.Length != 1)
            {
                return null;
            }

            return customAttributes[0] as LinkAttribute;
        }

        /// <summary>
        /// Encode the byte array into a hex string.
        /// </summary>
        /// <param name="bytes">Input byte array.</param>
        /// <returns>Hex encoded string.</returns>
        /// <exception cref="ArgumentNullException"><see paramref="bytes" />is <see langword="null"/>.</exception>
        public static string BinToHexEncode(IEnumerable<byte> bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Decode the hex string into a byte array.
        /// </summary>
        /// <param name="hexString">Hex encoded string.</param>
        /// <returns>Decoded byte array.</returns>
        /// <exception cref="ArgumentNullException">Input is null.</exception>
        /// <exception cref="ArgumentException">Input cannot be converted (wrong format, wrong length, etc)</exception>
        public static byte[] HexToBinDecode(string hexString)
        {
            if (hexString == null)
            {
                throw new ArgumentNullException("hexString");
            }

            byte[] bytes = new byte[hexString.Length >> 1];

            try
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hexString.Substring(i << 1, 2), 16);
                }
            }
            catch (FormatException e)
            {
                // Convert the FormatException to ArgumentException, so caller can just deal with
                // one type of exception. It is very unlikely that caller wants to handle 
                // those different types of exceptions differently.
                // Note that string.Substring can throw ArgumentOutOfRangeException, which is 
                // also a type of ArgumentException. 
                throw new ArgumentException("Wrong input format.", "hexString", e);
            }

            return bytes;
        }

    }
}