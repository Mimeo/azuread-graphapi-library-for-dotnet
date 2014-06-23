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
    using System.Reflection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// GraphObject is the base class for all graph entities.
    /// </summary>
    public partial class GraphObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphObject"/> class.
        /// </summary>
        internal GraphObject()
        {
            this.NonSerializedProperties = new Dictionary<string, object>();
            this.ChangedProperties = new HashSet<string>();
        }

        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        /// <summary>
        /// Gets or sets the object type.
        /// </summary>
        [JsonProperty("odata.type")]
        public string ODataTypeName { get; set; }

        /// <summary>
        /// Gets or sets the json string that was used to deserialize this object.
        /// </summary>
        public JToken TokenDictionary { get; set; }

        /// <summary>
        /// Gets or sets the list of properties that were materialized when the object was deserialized.
        /// </summary>
        public IList<string> PropertiesMaterializedFromDeserialization { get; set; }

        /// <summary>
        /// Gets or sets the list of properties that have been updated.
        /// </summary>
        public HashSet<string> ChangedProperties { get; set; }

        /// <summary>
        /// Gets or sets the properties that are serialized and do not have a declared property.
        /// </summary>
        public Dictionary<string, object> NonSerializedProperties { get; set; }

        /// <summary>
        /// Indexer that returns the value of a given property. This can be used to access extension property values.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Value of the property if the property exists, null otherwise.</returns>
        public object this[string propertyName]
        {
            get
            {
                if (this.NonSerializedProperties.ContainsKey(propertyName))
                {
                    return this.NonSerializedProperties[propertyName];
                }

                PropertyInfo propertyInfo = this.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Public | BindingFlags.IgnoreCase);
                if (propertyInfo != null)
                {
                    return propertyInfo.GetValue(this, null);
                }

                return null;
            }
        }

        /// <summary>
        /// Validates that the object is valid for create / update.
        /// </summary>
        /// <param name="isCreate">Is this object going to be created or is being updated?</param>
        /// <exception cref="PropertyValidationException">When any invalid properties are found.</exception>
        public virtual void ValidateProperties(bool isCreate)
        {
            if (isCreate)
            {
                if (!String.IsNullOrEmpty(this.ObjectId))
                {
                    throw new PropertyValidationException("ObjectId should be null for create.");
                }
            }
            else
            {
                if (String.IsNullOrEmpty(this.ObjectId))
                {
                    throw new PropertyValidationException("ObjectId should not be null for update.");
                }
            }

            foreach (string propertyName in this.ChangedProperties)
            {
                if (Utils.IsExtensionPropertyName(propertyName))
                {
                    continue;
                }

                if (Utils.GetLinkAttribute(this.GetType(), propertyName) != null)
                {
                    throw new PropertyValidationException("Link cannot be specified during add / update.");
                }
            }
        }

        /// <summary>
        /// Returns a JSON representation of the current object.
        /// </summary>
        /// <param name="updatedPropertiesOnly">Updated properties only?</param>
        /// <returns>JSON string.</returns>
        public virtual string ToJson(bool updatedPropertiesOnly)
        {
            if (updatedPropertiesOnly)
            {
                return JsonConvert.SerializeObject(Utils.GetSerializableGraphObject(this));
            }

            return JsonConvert.SerializeObject(this);
        }

        #region Extension properties support

        /// <summary>
        /// The backing store for extension properties.
        /// </summary>
        private IDictionary<string, object> extensions = new Dictionary<string, object>();

        /// <summary>
        /// Gets the value of the extension property identified by the given appId and friendlyName.
        /// </summary>
        /// <typeparam name="TValue">The type of the returned value.</typeparam>
        /// <param name="appId">The application identifier component of the full extension property name.</param>
        /// <param name="friendlyName">The friendly name component of the full extension property name.</param>
        /// <returns>The value of the extension property.</returns>
        public TValue GetExtension<TValue>(string appId, string friendlyName)
        {
            return (TValue)this.GetExtension(appId, friendlyName);
        }

        /// <summary>
        /// Gets the value of the extension property identified by the given appId and friendlyName.
        /// </summary>
        /// <param name="appId">The application identifier component of the full extension property name.</param>
        /// <param name="friendlyName">The friendly name component of the full extension property name.</param>
        /// <returns>The value of the extension property.</returns>
        public object GetExtension(string appId, string friendlyName)
        {
            return this.GetExtension(Utils.BuildExtensionPropertyName(appId, friendlyName));
        }

        /// <summary>
        /// Gets the value of the extension property identified by the given fullName.
        /// </summary>
        /// <typeparam name="TValue">The type of the returned value.</typeparam>
        /// <param name="fullName">The full name of the extension property.</param>
        /// <returns>The value of the extension property.</returns>
        public TValue GetExtension<TValue>(string fullName)
        {
            return (TValue)this.GetExtension(fullName);
        }

        /// <summary>
        /// Gets the value of the extension property identified by the given fullName.
        /// </summary>
        /// <param name="fullName">The full name of the extension property.</param>
        /// <returns>The value of the extension property.</returns>
        public object GetExtension(string fullName)
        {
            object result = null;
            this.extensions.TryGetValue(fullName, out result);
            return result;
        }

        /// <summary>
        /// Sets the value of the extension property identified by the given appId and friendlyName.
        /// </summary>
        /// <param name="appId">The application identifier component of the full extension property name.</param>
        /// <param name="friendlyName">The friendly name component of the full extension property name.</param>
        /// <param name="value">The new value of the extension property.</param>
        public void SetExtension(string appId, string friendlyName, object value)
        {
            this.SetExtension(Utils.BuildExtensionPropertyName(appId, friendlyName), value);
        }

        /// <summary>
        /// Sets the value of the extension property identified by the given fullName.
        /// </summary>
        /// <param name="fullName">The full name of the extension property.</param>
        /// <param name="value">The new value of the extension property.</param>
        public void SetExtension(string fullName, object value)
        {
            this.extensions[fullName] = value;
            this.ChangedProperties.Add(fullName);
        }

        #endregion
    }
}