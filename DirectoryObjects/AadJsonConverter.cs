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
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Custom JSON converter for AAD types.
    /// Cannot be used for deserializing non AAD Types as it defaults all objects to GraphObject.
    /// </summary>
    public class AadJsonConverter : JsonConverter
    {
        /// <summary>
        /// Map from Aad type to list of properties.
        /// </summary>
        private static Dictionary<string, Dictionary<string, PropertyInfo>> aadToPropertyInfoMap = 
            new Dictionary<string, Dictionary<string, PropertyInfo>>();

        /// <summary>
        /// Can the current implementation handle this type?
        /// </summary>
        /// <param name="objectType">Object type/</param>
        /// <returns>
        /// <see langword="true"/> if the converter can handle the type.
        /// <see langword="false"/> otherwise.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            bool isGraphObject = typeof(GraphObject).IsAssignableFrom(objectType);

            return isGraphObject;
        }

        /// <summary>
        /// Read JSON string to the object.
        /// </summary>
        /// <param name="reader">JSON reader.</param>
        /// <param name="objectType">Object type.</param>
        /// <param name="existingValue">Existing value.</param>
        /// <param name="serializer">Json serializer</param>
        /// <returns>Deserialized object.</returns>
        /// <remarks>
        /// 1. Check if this is an array or a single element.
        ///     If Array, deserialize each element as DirectoryObject and return the list
        /// 2. Deserialize using the default property set
        /// 3. Find the non-deserialized properties and add them to the Dictionary.
        /// </remarks>
        public override object ReadJson(
            JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                JToken jsonToken = JArray.ReadFrom(reader);
                List<JToken> jsonTokens = jsonToken.ToList();

                // This converter can only handle an array of graph objects.
                // Not native types. When deserializing an expanded link, all linked objects will be
                // deserialized as GraphObject.
                ChangeTrackingCollection<GraphObject> resultObjects = new ChangeTrackingCollection<GraphObject>();

                AadJsonConverter jsonConverter = new AadJsonConverter();
                foreach (JToken arrayToken in jsonTokens)
                {
                    GraphObject resultElement = JsonConvert.DeserializeObject(
                        arrayToken.ToString(), typeof(GraphObject), jsonConverter) as GraphObject;

                    resultObjects.Add(resultElement);
                }

                return resultObjects;
            }

            // Load the JSON into a JsonObject so that we can inspect the odata.type property.
            Object resultObject;
            JObject jsonObject = JObject.Load(reader);
 
            List<JProperty> jsonProperties = jsonObject.Properties().ToList();
            JProperty odataTypeProperty = 
                jsonProperties.FirstOrDefault(x => x.Name == Constants.OdataTypeKey);

            // If there is odata.type value, use that to find the type to be deserialized into.
            // If not, use the type that was passed to the de-serializer.
            Type resultObjectType;

            if (odataTypeProperty != null && 
                SerializationHelper.TryGetImplementationForAadType(
                    odataTypeProperty.Value.ToString(), out resultObjectType) &&
                typeof(GraphObject).IsAssignableFrom(resultObjectType))
            {
                resultObject = Activator.CreateInstance(resultObjectType) as GraphObject;
            }
            else
            {
                resultObjectType = objectType;
                resultObject = Activator.CreateInstance(resultObjectType);
            }

            // Deserialize all known properties using the default JSON.NET serializer
            resultObject = JsonConvert.DeserializeObject(jsonObject.ToString(), resultObjectType);

            // TODO: If the odata type is null, should still try to deserialize additional values using
            // the graphObjectType.
            GraphObject graphObject = resultObject as GraphObject;
            if (graphObject != null && odataTypeProperty != null)
            {
                Dictionary<string, PropertyInfo> propertyNameToInfoMap =
                    this.GetPropertyInfosForAadType(odataTypeProperty.Value.ToString(), resultObjectType);

                foreach (JProperty jsonProperty in jsonProperties)
                {
                    PropertyInfo propertyInfo;
                    if (!propertyNameToInfoMap.TryGetValue(jsonProperty.Name, out propertyInfo))
                    {
                        graphObject.NonSerializedProperties[jsonProperty.Name] = jsonProperty.Value.ToString();
                    }
                }
            }

            return graphObject;
        }

        /// <summary>
        /// Serialize the object into JSON.
        /// </summary>
        /// <param name="writer">JSON writer.</param>
        /// <param name="value">PropertyValue to be serialized.</param>
        /// <param name="serializer">JSON serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                return;
            }

            ChangeTrackingCollection<GraphObject> graphObjects = value as ChangeTrackingCollection<GraphObject>;

            if (graphObjects == null)
            {
                if (!(value is IEnumerable))
                {
                    throw new ArgumentException("parameter 'value' is not of type IEnumerable.");
                }

                graphObjects = new ChangeTrackingCollection<GraphObject>();
                IEnumerable iEnumerableValue = value as IEnumerable;

                foreach (object valueObject in iEnumerableValue)
                {
                    GraphObject graphObject = valueObject as GraphObject;
                    if (graphObject == null)
                    {
                        throw new ArgumentException(
                            "Each value in the ChangeTrackingCollection should be of type GraphObject.");
                    }

                    graphObjects.Add(graphObject);
                }
            }

            if (graphObjects.Count > 0)
            {
                throw new ArgumentException("Updating links is not supported from entity.");
            }

            writer.WriteNull();
        }

        /// <summary>
        /// Get the list of defined properties for the aad type.
        /// </summary>
        /// <param name="graphMetadataType">Aad type whose properties are requested..</param>
        /// <param name="graphObjectType">Directory object.</param>
        /// <returns>All defined properties.</returns>
        private Dictionary<string, PropertyInfo> GetPropertyInfosForAadType(
            string graphMetadataType, Type graphObjectType)
        {
            if (String.IsNullOrEmpty(graphMetadataType))
            {
                throw new ArgumentNullException("graphMetadataType");
            }

            Dictionary<string, PropertyInfo> propertyNameToInfoMap;

            if (!aadToPropertyInfoMap.TryGetValue(graphMetadataType, out propertyNameToInfoMap))
            {
                PropertyInfo[] properties = graphObjectType.GetProperties();
                propertyNameToInfoMap = new Dictionary<string, PropertyInfo>();
                aadToPropertyInfoMap[graphMetadataType] = propertyNameToInfoMap;

                foreach (PropertyInfo propertyInfo in properties)
                {
                    JsonPropertyAttribute jsonPropertyAttribute = Utils.GetCustomAttribute<JsonPropertyAttribute>(
                        propertyInfo, false);

                    if (jsonPropertyAttribute != null)
                    {
                        propertyNameToInfoMap[jsonPropertyAttribute.PropertyName] = propertyInfo;
                    }
                }
            }

            return propertyNameToInfoMap;
        }
    }
}
