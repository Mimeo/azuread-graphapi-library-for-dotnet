// Copyright

namespace Microsoft.Azure.ActiveDirectory.GraphClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;

    public class ChangeTrackingFiniteSet<T> : ObservableCollection<T>
    {
        public HashSet<T> Domain { get; private set; }

        public ChangeTrackingFiniteSet(HashSet<T> domain) : base()
        {
            this.Domain = domain;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add || args.Action == NotifyCollectionChangedAction.Replace) 
            {
                this.ValidateItems(args.NewItems.Cast<T>());
            }

            base.OnCollectionChanged(args);
        }

        protected void ValidateItems(IEnumerable<T> items)
        {
            if (items.Any(item => this.Domain.Contains(item) == false))
            {
                throw new ArgumentOutOfRangeException("items", "All items must be in the domain of this set.");
            }
        }
    }
}
