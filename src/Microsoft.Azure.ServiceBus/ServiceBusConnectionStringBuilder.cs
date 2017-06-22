// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.ServiceBus
{
    using System;
    using System.Text;

    public class ServiceBusConnectionStringBuilder
    {
        const char KeyValueSeparator = '=';
        const char KeyValuePairDelimiter = ';';
        static readonly string EndpointScheme = "amqps";
        static readonly string EndpointFormat = EndpointScheme + "://{0}.servicebus.windows.net";
        static readonly string EndpointConfigName = "Endpoint";
        static readonly string SharedAccessKeyNameConfigName = "SharedAccessKeyName";
        static readonly string SharedAccessKeyConfigName = "SharedAccessKey";
        static readonly string SaslPlainUsernameConfigName = "SaslPlainUsername";
        static readonly string SaslPlainPasswordConfigName = "SaslPlainPassword";
        static readonly string EntityPathConfigName = "EntityPath";

        /// <summary>
        /// Instatiates a new <see cref="ServiceBusConnectionStringBuilder"/>.
        /// </summary>
        /// <param name="connectionString">Connection string for namespace or the entity.</param>
        public ServiceBusConnectionStringBuilder(string connectionString)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                this.ParseConnectionString(connectionString);
            }
        }

        /// <summary>
        /// Instantiates a new <see cref="ServiceBusConnectionStringBuilder"/>.
        /// </summary>
        /// <param name="namespaceName">Namespace name.</param>
        /// <param name="entityPath">Path to the entity.</param>
        /// <param name="sharedAccessKeyName">Shared access key name.</param>
        /// <param name="sharedAccessKey">Shared access key.</param>
        public ServiceBusConnectionStringBuilder(string namespaceName, string entityPath, string sharedAccessKeyName, string sharedAccessKey)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                throw Fx.Exception.ArgumentNullOrWhiteSpace(nameof(namespaceName));
            }
            if (string.IsNullOrWhiteSpace(sharedAccessKeyName) || string.IsNullOrWhiteSpace(sharedAccessKey))
            {
                throw Fx.Exception.ArgumentNullOrWhiteSpace(string.IsNullOrWhiteSpace(sharedAccessKeyName) ? nameof(sharedAccessKeyName) : nameof(sharedAccessKey));
            }

            if (namespaceName.Contains("."))
            {
                // It appears to be a fully qualified host name, use it.
                this.Endpoint = new Uri(EndpointScheme + "://" + namespaceName);
            }
            else
            {
                this.Endpoint = new Uri(EndpointFormat.FormatInvariant(namespaceName));
            }

            this.EntityPath = entityPath;
            this.SasKeyName = sharedAccessKeyName;
            this.SasKey = sharedAccessKey;
        }

        public Uri Endpoint { get; set; }

        /// <summary>
        /// Get the entity path value from the connection string
        /// </summary>
        public string EntityPath { get; set; }

        /// <summary>
        /// Get the shared access policy owner name from the connection string
        /// </summary>
        public string SasKeyName { get; set; }

        /// <summary>
        /// Get the shared access policy key value from the connection string
        /// </summary>
        /// <value>Shared Access Signature key</value>
        public string SasKey { get; set; }

        /// <summary>
        /// Get the SASL plain username value from the connection string
        /// </summary>
        /// <value>SASL plain username</value>
        public string SaslPlainUsername { get; set; }

        /// <summary>
        /// Get the SASL plain password from the connection string
        /// </summary>
        public string SaslPlainPassword { get; set; }

        /// <summary>
        /// Returns an interoperable connection string that can be used to connect to ServiceBus Namespace
        /// </summary>
        /// <returns>Namespace connection string</returns>
        public string GetNamespaceConnectionString()
        {
            StringBuilder connectionStringBuilder = new StringBuilder();
            if (this.Endpoint != null)
            {
                connectionStringBuilder.Append($"{EndpointConfigName}{KeyValueSeparator}{this.Endpoint}{KeyValuePairDelimiter}");
            }

            if (!string.IsNullOrWhiteSpace(this.SasKeyName))
            {
                connectionStringBuilder.Append($"{SharedAccessKeyNameConfigName}{KeyValueSeparator}{this.SasKeyName}{KeyValuePairDelimiter}");
            }

            if (!string.IsNullOrWhiteSpace(this.SasKey))
            {
                connectionStringBuilder.Append($"{SharedAccessKeyConfigName}{KeyValueSeparator}{this.SasKey}{KeyValuePairDelimiter}");
            }

            if (!string.IsNullOrWhiteSpace(this.SaslPlainUsername))
            {
                connectionStringBuilder.Append($"{SaslPlainUsernameConfigName}{KeyValueSeparator}{this.SaslPlainUsername}{KeyValuePairDelimiter}");
            }

            if (!string.IsNullOrWhiteSpace(this.SaslPlainPassword))
            {
                connectionStringBuilder.Append($"{SaslPlainPasswordConfigName}{KeyValueSeparator}{this.SaslPlainPassword}");
            }

            return connectionStringBuilder.ToString();
        }

        /// <summary>
        /// Returns an interoperable connection string that can be used to connect to the given ServiceBus Entity
        /// </summary>
        /// <returns>Entity connection string</returns>
        public string GetEntityConnectionString()
        {
            if (string.IsNullOrWhiteSpace(this.EntityPath))
            {
                throw Fx.Exception.ArgumentNullOrWhiteSpace(nameof(this.EntityPath));
            }

            return $"{this.GetNamespaceConnectionString()}{KeyValuePairDelimiter}{EntityPathConfigName}{KeyValueSeparator}{this.EntityPath}{KeyValuePairDelimiter}";
        }

        /// <summary>
        /// Returns an interoperable connection string that can be used to connect to ServiceBus Namespace
        /// </summary>
        /// <returns>The connection string</returns>
        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(this.EntityPath))
            {
                return this.GetNamespaceConnectionString();
            }

            return this.GetEntityConnectionString();
        }

        void ParseConnectionString(string connectionString)
        {
            // First split based on ';'
            string[] keyValuePairs = connectionString.Split(new[] { KeyValuePairDelimiter }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var keyValuePair in keyValuePairs)
            {
                // Now split based on the _first_ '='
                string[] keyAndValue = keyValuePair.Split(new[] { KeyValueSeparator }, 2);
                string key = keyAndValue[0].Trim();
                if (keyAndValue.Length != 2)
                {
                    throw Fx.Exception.Argument(nameof(connectionString), $"Value for the connection string parameter name '{key}' was not found.");
                }

                string value = keyAndValue[1].Trim();
                if (key.Equals(EndpointConfigName, StringComparison.OrdinalIgnoreCase))
                {
                    Uri uri = new Uri(value);
                    if ( !string.IsNullOrEmpty(uri.UserInfo))
                    {
                        string[] userInfo = uri.UserInfo.Split(':');
                        this.SaslPlainUsername = userInfo[0];
                        if (userInfo.Length > 1)
                        {
                            this.SaslPlainPassword = userInfo[1];
                        }
                    }
                    this.Endpoint = new UriBuilder(uri.Scheme, uri.Host, uri.Port, uri.PathAndQuery).Uri;
                }
                else if (key.Equals(EntityPathConfigName, StringComparison.OrdinalIgnoreCase))
                {
                    this.EntityPath = value;
                }
                else if (key.Equals(SharedAccessKeyNameConfigName, StringComparison.OrdinalIgnoreCase))
                {
                    this.SasKeyName = value;
                }
                else if (key.Equals(SharedAccessKeyConfigName, StringComparison.OrdinalIgnoreCase))
                {
                    this.SasKey = value;
                }
                else if (key.Equals(SaslPlainUsernameConfigName, StringComparison.OrdinalIgnoreCase))
                {
                    this.SaslPlainUsername = value;
                }
                else if (key.Equals(SaslPlainPasswordConfigName, StringComparison.OrdinalIgnoreCase))
                {
                    this.SaslPlainPassword = value;
                }
                else
                {
                    throw Fx.Exception.Argument(nameof(connectionString), $"Illegal connection string parameter name '{key}'");
                }
            }
        }
    }
}