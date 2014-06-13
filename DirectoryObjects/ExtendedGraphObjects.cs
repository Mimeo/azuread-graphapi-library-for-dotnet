// Copyright

namespace Microsoft.Azure.ActiveDirectory.GraphClient
{
    public class Group<TExtensions> : Group where TExtensions : class, new()
    {
        public virtual TExtensions Extensions
        {
            get { return base.ExtensionsHost as TExtensions; }
            set { base.ExtensionsHost = value; }
        }
    }

    public class User<TExtensions> : User where TExtensions : class, new()
    {
        public virtual TExtensions Extensions
        {
            get { return base.ExtensionsHost as TExtensions; }
            set { base.ExtensionsHost = value; }
        }
    }
}
