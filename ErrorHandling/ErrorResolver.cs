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

namespace Microsoft.Azure.ActiveDirectory.GraphClient.ErrorHandling
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using Newtonsoft.Json;

    /// <summary>
    /// Helper class for resolving errors.
    /// </summary>
    public class ErrorResolver
    {
        /// <summary>
        /// Parses the error message into an exception object.
        /// </summary>
        /// <param name="webException">Web exception.</param>
        /// <returns>Aad exception object.</returns>
        public static GraphException ParseWebException(WebException webException)
        {
            GraphException graphException = null;
            HttpStatusCode statusCode = HttpStatusCode.Unused;
            string responseUri = String.Empty;

            if (webException == null)
            {
                throw new ArgumentNullException("webException");
            }

            if (webException.Response != null)
            {
                statusCode = ((HttpWebResponse) webException.Response).StatusCode;
                responseUri = webException.Response.ResponseUri.ToString();
                Stream responseStream = webException.Response.GetResponseStream();

                if (responseStream != null)
                {
                    string errorMessage;

                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        errorMessage = sr.ReadToEnd();
                    }

                    graphException = ErrorResolver.ParseErrorMessageString(statusCode, errorMessage);

                    if (webException.Response.Headers != null)
                    {
                        Utils.LogResponseHeaders(webException.Response.Headers);
                        graphException.ResponseHeaders = webException.Response.Headers;
                    }

                    webException.Response.Close();
                }
            }

            if (graphException == null)
            {
                graphException = new GraphException(statusCode, webException.Message);
            }

            graphException.ResponseUri = responseUri;
            return graphException;
        }

        /// <summary>
        /// Parses the error response string into an Aad exception.
        /// </summary>
        /// <param name="statusCode">Http status code.</param>
        /// <param name="errorMessage">ErrorResponse message.</param>
        /// <returns>Deserialized error message.</returns>
        public static GraphException ParseErrorMessageString(HttpStatusCode statusCode, string errorMessage)
        {
            try
            {
                ODataError oDataError = JsonConvert.DeserializeObject<ODataError>(errorMessage);

                if (oDataError != null && oDataError.Error != null)
                {
                    return ResolveErrorCode(statusCode, oDataError);
                }
            }
            catch (JsonReaderException jsonReaderException)
            {
                Logger.Instance.Error(jsonReaderException.Message);
                Logger.Instance.Error(errorMessage);
            }

            return new GraphException(statusCode, "Unknown", errorMessage);
        }

        /// <summary>
        /// Resolve the error code to an appropriate exception.
        /// </summary>
        /// <param name="statusCode">Http status code.</param>
        /// <param name="errorCode">ErrorResponse code.</param>
        /// <param name="errorMessage">ErrorResponse message.</param>
        /// <returns>Sub class of GraphException, based on the error code.</returns>
        public static GraphException ResolveErrorCode(
            HttpStatusCode statusCode, ODataError oDataError)
        {
            GraphException graphException = null;

            string errorCode = String.Empty;
            string errorMessage = String.Empty;
            List<ExtendedErrorValue> extendedErrorValues = null;

            if (oDataError != null && oDataError.Error != null)
            {
                errorCode = oDataError.Error.Code;

                if (oDataError.Error.Message != null)
                {
                    errorMessage = oDataError.Error.Message.MessageValue;
                }

                extendedErrorValues = oDataError.Error.Values;
            }

            if (ErrorCodes.ExceptionErrorCodeMap.ContainsKey(errorCode))
            {
                Type exceptionType = ErrorCodes.ExceptionErrorCodeMap[errorCode];
                ConstructorInfo constructorInfo = 
                    exceptionType.GetConstructor(new [] { typeof(HttpStatusCode), typeof(string) });

                if (constructorInfo != null)
                {
                    graphException = constructorInfo.Invoke(new object[] { statusCode, errorMessage }) as GraphException ??
                                   new GraphException(statusCode, errorCode, errorMessage);

                    graphException.Code = errorCode;
                    graphException.HttpStatusCode = statusCode;
                    graphException.ErrorMessage = errorMessage;
                }
            }

            if (graphException == null)
            {
                graphException = new GraphException(statusCode, errorCode, errorMessage);
            }

            if (extendedErrorValues != null)
            {
                graphException.ExtendedErrors = new Dictionary<string, string>();

                extendedErrorValues.ForEach(x => graphException.ExtendedErrors[x.Item] = x.Value);
            }

            graphException.ErrorResponse = oDataError;

            return graphException;
        }
    }
}
