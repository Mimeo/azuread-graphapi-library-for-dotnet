// Copyright

namespace Microsoft.Azure.ActiveDirectory.GraphClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents an extension property definition.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [Entity("extensionProperties", "Microsoft.WindowsAzure.ActiveDirectory.ExtensionProperty")]
    public class ExtensionProperty : DirectoryObject
    {
        #region Private fields

        /// <summary>
        /// The valid types for extension property values.
        /// </summary>
        private static HashSet<string> validDataTypes = new HashSet<string>
        {
            "String",
            "Binary"
        };

        /// <summary>
        /// The set of directory object types that may be extended. 
        /// </summary>
        private static HashSet<string> validTargetObjectTypes = new HashSet<string>
        {
            "User",
            "Group",
            "TenantDetail",
            "Device",
            "Application",
            "ServicePrincipal"
        };

        #endregion

        /// <summary>
        /// Backing store for Name property.
        /// </summary>
        private string _name;

        /// <summary>
        /// Gets or sets the name of the extension property.
        /// </summary>
        /// <remarks>
        /// When creating a new extension property, the given name should be the friendly name of the property
        /// (e.g., "fooBar"). When the graph client retrieves an existing extension property, the name will be
        /// the full name of the property as stored in AAD (e.g., "extension_d8dde29f1095422e91537a6cb22a2f74_fooBar").
        /// </remarks>
        [JsonProperty("name")]
        public string Name
        {
            get { return this._name; }
            set
            {
                this._name = value;
                this.ChangedProperties.Add("Name");
            }
        }

        /// <summary>
        /// Backing store for DataType property.
        /// </summary>
        private string _dataType;

        /// <summary>
        /// Gets or sets the data type of the extension property.
        /// </summary>
        [JsonProperty("dataType")]
        public string DataType
        {
            get { return this._dataType; }
            set
            {
                if (value != null && ExtensionProperty.validDataTypes.Contains(value) == false)
                {
                    throw new ArgumentOutOfRangeException("value", "Value must be either 'String' or 'Binary'.");
                }
                this._dataType = value;
                this.ChangedProperties.Add("DataType");
            }
        }

        /// <summary>
        /// Backing store for TargetObjects property.
        /// </summary>
        private ChangeTrackingFiniteSet<string> _targetObjects;

        /// <summary>
        /// Value indicating if the event handler for notifying value changes on TargetObjects is registered.
        /// </summary>
        private bool _targetObjectsInitialized;

        /// <summary>
        /// Gets or sets the set of target object types of the extension property.
        /// </summary>
        [JsonProperty("targetObjects")]
        public ChangeTrackingFiniteSet<string> TargetObjects
        {
            get
            {
                if (this._targetObjects == null)
                {
                    this._targetObjects = new ChangeTrackingFiniteSet<string>(ExtensionProperty.validTargetObjectTypes);
                }
                if (!this._targetObjectsInitialized)
                {
                    this._targetObjects.CollectionChanged += (s, e) => this.ChangedProperties.Add("TargetObjects");
                    this._targetObjectsInitialized = true;
                }
                return this._targetObjects;
            }
            set
            {
                if (value.Domain.Any(item => ExtensionProperty.validTargetObjectTypes.Contains(item) == false))
                {
                    throw new ArgumentOutOfRangeException("Value must contain a subset of the domain of valid target object types.");
                }
                this._targetObjects = value;
                this.ChangedProperties.Add("TargetObjects");
            }
        }

        /// <summary>
        /// Creates a new instance of the ExtensionsProperty class.
        /// </summary>
        public ExtensionProperty() : this(null, null)
        { }

        /// <summary>
        /// Creates a new instance of the ExtensionsProperty class.
        /// </summary>
        /// <param name="name">The name of the extension property.</param>
        /// <param name="dataType">The data type of the extension property.</param>
        /// <param name="targetObjectTypes">The set of target object types of the extension property.</param>
        /// <seealso cref="Name"/>
        public ExtensionProperty(string name, string dataType, params string[] targetObjectTypes) : base()
        {
            this.Name = name;
            this.DataType = dataType;
            foreach (var type in targetObjectTypes)
            {
                this.TargetObjects.Add(type);
            }
        }
    }
}
