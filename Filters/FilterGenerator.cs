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
    using System.Collections.Generic;
    using System.Linq.Expressions;

    /// <summary>
    /// OData filter generator.
    /// </summary>
    public class FilterGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterGenerator"/> class.
        /// </summary>
        public FilterGenerator()
        {
            this.QueryArguments = new Dictionary<string, string>();
            this.ExpandProperty = LinkProperty.None;
            this.OrderByProperty = GraphProperty.None;
        }

        #region Well known query arguments

        /// <summary>
        /// Gets or sets the top value. (page size).
        /// Set it to le 0 to clear the value.
        /// </summary>
        public int Top
        {
            get
            {
                int topValue = -1;
                string topValueString;
                if (this.QueryArguments.TryGetValue(Constants.TopOperator, out topValueString))
                {
                    int.TryParse(topValueString, out topValue);
                }

                return topValue;
            }

            set
            {
                if (value > 0)
                {
                    this.QueryArguments[Constants.TopOperator] = value.ToString();
                }
                else
                {
                    if (this.QueryArguments.ContainsKey(Constants.TopOperator))
                    {
                        this.QueryArguments.Remove(Constants.TopOperator);
                    }
                }
            }
        }
    
        #endregion

        /// <summary>
        /// Gets or sets the filter expressions.
        /// </summary>
        public Expression QueryFilter { get; set; }

        /// <summary>
        /// Gets or sets the raw query filter.
        /// When specified, this will be added as the $filter query and the expression will not be evaluated.
        /// </summary>
        public string OverrideQueryFilter { get; set; }

        /// <summary>
        /// Gets the link property to be expanded.
        /// </summary>
        public LinkProperty ExpandProperty { get; set; }

        /// <summary>
        /// Gets or sets the property to sort on.
        /// </summary>
        public GraphProperty OrderByProperty { get; set; }

        /// <summary>
        /// Gets or sets the query argument value.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <returns>Query argument value.</returns>
        public string this[string name]
        {
            get
            {
                return this.QueryArguments[name];
            }

            set
            {
                this.QueryArguments[name] = value;
            }
        }

        /// <summary>
        /// Gets the current set of query parameter names.
        /// </summary>
        internal IEnumerable<string> Names
        {
            get
            {
                return this.QueryArguments.Keys;
            }
        }

        /// <summary>
        /// Gets or sets the filters.
        /// </summary>
        private IDictionary<string, string> QueryArguments { get; set; }
    }
}
