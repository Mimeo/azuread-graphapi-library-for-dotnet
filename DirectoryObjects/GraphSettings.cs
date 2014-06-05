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

    /// <summary>
    /// Defines the settings used to control <see cref="GraphConnection"/>.
    /// </summary>
    public class GraphSettings
    {
        /// <summary>
        /// Indicates whether the client library should retry the requests for the configured exceptions.
        /// </summary>
        private bool isRetryEnabled = true;

        /// <summary>
        /// Exceptions for which the request should be retried.
        /// </summary>
        private HashSet<string> retryOnExceptions = new HashSet<string>
                {
                    "Microsoft.Azure.ActiveDirectory.GraphClient.ServiceUnavailableException",
                    "Microsoft.Azure.ActiveDirectory.GraphClient.InternalServerErrorException",
                };

        /// <summary>
        /// Wait time before retrying exceptions.
        /// </summary>
        private TimeSpan waitBeforeRetry = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Total number of tries after which retry should be stopped.
        /// </summary>
        private int totalAttempts = 3;

        /// <summary>
        /// Api version 
        /// </summary>
        private string apiVersion = Constants.DefaultApiVersion;

        /// <summary>
        /// Domain name for the graph api.
        /// </summary>
        private string graphDomainName = Constants.DefaultGraphApiDomainName;
        
        /// <summary>
        /// Gets or sets whether the client library should retry the requests for the configured exceptions.
        /// </summary>
        public bool IsRetryEnabled
        {
            get { return this.isRetryEnabled; }
            set { this.isRetryEnabled = value; }
        }

        /// <summary>
        /// Gets or sets the exceptions for which the request should be retried.
        /// </summary>
        public HashSet<string> RetryOnExceptions
        {
            get { return this.retryOnExceptions; }
            set { this.retryOnExceptions = value; }
        }

        /// <summary>
        /// Gets or sets the wait time before retrying exceptions.
        /// </summary>
        public TimeSpan WaitBeforeRetry
        {
            get { return this.waitBeforeRetry; }
            set { this.waitBeforeRetry = value; }
        }

        /// <summary>
        /// Total number of tries after which retry should be stopped.
        /// </summary>
        public int TotalAttempts
        {
            get { return this.totalAttempts; }
            set { this.totalAttempts = value; }
        }

        /// <summary>
        /// Api version 
        /// </summary>
        public string ApiVersion
        {
            get { return this.apiVersion; }
            set { this.apiVersion = value; }
        }

        /// <summary>
        /// Domain name for the graph api.
        /// </summary>
        public string GraphDomainName
        {
            get { return this.graphDomainName; }
            set { this.graphDomainName = value; }
        }
    }
}
