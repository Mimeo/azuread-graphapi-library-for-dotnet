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
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Key credentials helper.
    /// </summary>
    public class KeyCredentialsHelper
    {
        /// <summary>
        /// Creates a Symmetric key credential.
        /// </summary>
        /// <param name="startTime">Start time of the credential.</param>
        /// <param name="endTime">End time of the credential.</param>
        /// <param name="password">Credential value</param>
        /// <returns>Password credential object.</returns>
        public static PasswordCredential CreatePasswordCredential(
            DateTime startTime,
            DateTime endTime,
            string password)
        {
            Utils.ThrowIfNullOrEmpty(password, "password");

            ValidateStartAndEndTime(startTime, endTime);

            PasswordCredential passwordCredential = new PasswordCredential();
            passwordCredential.StartDate = startTime;
            passwordCredential.EndDate = endTime;
            passwordCredential.Value = password;

            return passwordCredential;
        }

        /// <summary>
        /// Creates a Symmetric key credential.
        /// </summary>
        /// <param name="startTime">Start time of the credential.</param>
        /// <param name="endTime">End time of the credential.</param>
        /// <param name="keyUsage">Key usage for the symmetric key.</param>
        /// <param name="credentialBlob">Credential value</param>
        /// <returns>Key credential object.</returns>
        public static KeyCredential CreateSymmetricKeyCredential(
            DateTime startTime,
            DateTime endTime,
            KeyUsage keyUsage,
            byte[] credentialBlob)
        {
            Utils.ThrowIfNullOrEmpty(credentialBlob, "credentialBlob");
            ValidateStartAndEndTime(startTime, endTime);

            KeyCredential keyCredential = new KeyCredential();

            keyCredential.StartDate = startTime;
            keyCredential.EndDate = endTime;
            keyCredential.Type = KeyType.Symmetric.ToString();
            keyCredential.Usage = keyUsage.ToString();
            keyCredential.Value = credentialBlob;

            return keyCredential;
        }

        /// <summary>
        /// Creates a Symmetric key credential.
        /// </summary>
        /// <param name="startTime">Start time.</param>
        /// <param name="endTime">End time of the credential.</param>
        /// <param name="keyUsage">Key usage of the symmetric key.</param>
        /// <param name="base64EncodedKeyValue">Credential value</param>
        /// <returns>Key credential.</returns>
        /// <exception cref="ArgumentException">Key is not a valid base64 encoded string.</exception>
        public static KeyCredential CreateSymmetricKeyCredential(
            DateTime startTime,
            DateTime endTime,
            KeyUsage keyUsage,
            string base64EncodedKeyValue)
        {
            Utils.ThrowIfNullOrEmpty(base64EncodedKeyValue, "base64EncodedKeyValue");

            byte[] keyBytes = null;

            try
            {
                keyBytes = Convert.FromBase64String(base64EncodedKeyValue);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Input was not a valid base64 encoded value.", "base64EncodedKeyValue");
            }
            
            return KeyCredentialsHelper.CreateSymmetricKeyCredential(
                startTime,
                endTime,
                keyUsage,
                keyBytes);
        }

        /// <summary>
        /// Create an X509 certificate based Asymmetric key credential.
        /// </summary>
        /// <param name="startTime">Start time.</param>
        /// <param name="endTime">End time of the credential.</param>
        /// <param name="certificate">Certificate credential.</param>
        /// <returns>Asymmetric key credential.</returns>
        public static KeyCredential CreateAsymmetricKeyCredential(
            DateTime startTime,
            DateTime endTime,
            X509Certificate2 certificate)
        {
            Utils.ThrowIfNullOrEmpty(certificate, "certificate");

            ValidateStartAndEndTime(startTime, endTime);

            KeyCredential keyCredential = new KeyCredential();

            keyCredential.StartDate = startTime;
            keyCredential.EndDate = endTime;
            keyCredential.Type = KeyType.AsymmetricX509Cert.ToString();
            keyCredential.Usage = KeyUsage.Verify.ToString();
            keyCredential.Value = certificate.GetRawCertData();

            return keyCredential;
        }

        /// <summary>
        /// Validates the start and end time values for a key credential.
        /// </summary>
        /// <param name="startTime">Start time.</param>
        /// <param name="endTime">End time of the credential.</param>
        /// <exception cref="ArgumentException">Start and end time combinations are not good.</exception>
        internal static void ValidateStartAndEndTime(DateTime startTime, DateTime endTime)
        {
            if (startTime.ToUniversalTime().CompareTo(endTime.ToUniversalTime()) > 0)
            {
                throw new ArgumentException("startTime must be less than end time");
            }

            if (endTime.ToUniversalTime().CompareTo(DateTime.UtcNow) < 0)
            {
                throw new ArgumentException("endTime must be less than the current time.");
            }
        }
    }
}
