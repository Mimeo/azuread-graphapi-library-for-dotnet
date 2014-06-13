// copyright

namespace Microsoft.Azure.ActiveDirectory.GraphClient
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;

    public class ExtensionsHostInfo
    {
        public Type ExtensionsHostType { get; private set; }

        public IDictionary<string, PropertyInfo> ExtensionsMap { get; private set; }

        public ExtensionsHostInfo(Type extensionsHostType)
        {
            this.ExtensionsHostType = extensionsHostType;
            this.ExtensionsMap = this.BuildExtensionsMap(extensionsHostType);
        }

        private IDictionary<string, PropertyInfo> BuildExtensionsMap(Type extensionsHostType)
        {
            var map = new Dictionary<string, PropertyInfo>();

            foreach (var info in extensionsHostType.GetProperties(BindingFlags.Public))
            {
                var extensionProperty = Utils.GetCustomAttribute<ExtensionPropertyAttribute>(info, false);
                if (extensionProperty != null)
                {
                    map[this.BuildExtensionPropertyFullName(extensionProperty, info.Name)] = info;
                }
            }

            return map;
        }

        private string BuildExtensionPropertyFullName(ExtensionPropertyAttribute source, string defaultName)
        {
            var id = source.ApplicationId.Replace("-", String.Empty);
            var name = source.FriendlyName;

            if (String.IsNullOrEmpty(name)) 
            {
                name = defaultName.Substring(0, 1).ToLowerInvariant() + defaultName.Substring(1);
            }

            return String.Format(CultureInfo.InvariantCulture, "extension_{0}_{1}", id, name);
        }
    }
}
