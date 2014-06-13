// Copyright 

namespace Microsoft.Azure.ActiveDirectory.GraphClient
{
    using System;

    /// <summary>
    /// Marks a class property as representative of an extension property in Azure Active Ddirectory.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ExtensionPropertyAttribute : Attribute
    {
        public string ApplicationId { get; private set; }
        
        public string FriendlyName { get; private set; }

        public ExtensionPropertyAttribute(string applicationId) : this(applicationId, null)
        {
        }

        public ExtensionPropertyAttribute(string applicationId, string friendlyName)
        {
            this.ApplicationId = applicationId;
            this.FriendlyName = friendlyName;
        }
    }
}
