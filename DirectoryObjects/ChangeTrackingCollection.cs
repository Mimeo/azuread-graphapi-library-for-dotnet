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
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// A collection of values. The collection tracks changes to its items and changes to itself, and
    /// provides notification when such changes occur. 
    /// </summary>
    /// <typeparam name="T">Type of value in the collection.</typeparam>
    public class ChangeTrackingCollection<T> : Collection<T>
    {
        /// <summary>
        /// Occurs when an item or the collection itself is just changed.
        /// </summary>
        internal event EventHandler CollectionChanged;
        
        /// <summary>
        /// Are there any values in this collection.
        /// </summary>
        /// <returns><see langword="true"/> if there are any items. <see langword="false"/> otherwise.</returns>
        public bool Any()
        {
            return this.Items.Any();
        }

        /// <summary>
        /// Are there any values in this collection that match the given value.
        /// </summary>
        /// <returns><see langword="true"/> if there are any matching items. 
        /// <see langword="false"/> otherwise.</returns>
        public bool Any(T value)
        {
            return this.Items.Contains(value);
        }

        /// <summary>
        /// Inserts an item into the <see cref="Collection{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which <paramref name="item"/> should be inserted.
        /// </param>
        /// <param name="item">
        /// The object to insert.
        /// </param>
        protected override void InsertItem(int index, T item)
        {
            Utils.ThrowIfNull(item, "item");
            base.InsertItem(index, item);
            this.OnItemInserted(item);
        }

        /// <summary>
        /// Removes an item at the specified index of the <see cref="Collection{T}"/>.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the item to remove.
        /// </param>
        protected override void RemoveItem(int index)
        {
            T removedItem = this[index];
            base.RemoveItem(index);
            this.OnItemRemoved(removedItem);
        }

        /// <summary>
        /// Replaces the item at the given index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the item to replace.
        /// </param>
        /// <param name="item">
        /// The new value for the item at the specified index.
        /// </param>
        protected override void SetItem(int index, T item)
        {
            Utils.ThrowIfNull(item, "item");
            T replacedItem = this[index];
            base.SetItem(index, item);
            this.OnItemSet(replacedItem, item);
        }

        /// <summary>
        /// Removes all items from the <see cref="Collection{T}"/>.
        /// </summary>
        protected override void ClearItems()
        {
            List<T> clearedItems = new List<T>(this);
            base.ClearItems();
            this.OnItemsCleared(clearedItems);
        }

        /// <summary>
        /// Called after <see langword="base"/>.<see cref="InsertItem"/> is called in <see cref="InsertItem"/>.
        /// </summary>
        /// <param name="insertedItem">The inserted item.</param>
        protected virtual void OnItemInserted(T insertedItem)
        {
            this.OnCollectionChanged();
        }

        /// <summary>
        /// Called after <see langword="base"/>.<see cref="RemoveItem"/> is called in <see cref="RemoveItem"/>.
        /// </summary>
        /// <param name="removedItem">The removed item.</param>
        protected virtual void OnItemRemoved(T removedItem)
        {
            this.OnCollectionChanged();
        }

        /// <summary>
        /// Called after <see langword="base"/>.<see cref="SetItem"/> is called in <see cref="SetItem"/>.
        /// </summary>
        /// <param name="replacedItem">The replaced item.</param>
        /// <param name="newItem">The new item.</param>
        protected virtual void OnItemSet(T replacedItem, T newItem)
        {
            this.OnCollectionChanged();
        }

        /// <summary>
        /// Called after <see langword="base"/>.<see cref="ClearItems"/> is called in <see cref="ClearItems"/>.
        /// </summary>
        /// <param name="clearedItems">The cleared items.</param>
        protected virtual void OnItemsCleared(IEnumerable<T> clearedItems)
        {
            this.OnCollectionChanged();
        }

        /// <summary>
        /// Called when the collection is changed.
        /// </summary>
        protected virtual void OnCollectionChanged()
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, EventArgs.Empty);
            }
        }
    }
}