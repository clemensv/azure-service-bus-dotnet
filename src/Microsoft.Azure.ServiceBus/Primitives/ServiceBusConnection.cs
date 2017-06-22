// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.ServiceBus
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Amqp;
    using Azure.Amqp;
    using Azure.Amqp.Transport;
    using Core;
    using Primitives;

    public abstract class ServiceBusConnection
    {
        static readonly Version AmqpVersion = new Version(1, 0, 0, 0);

        protected ServiceBusConnection(TimeSpan operationTimeout, RetryPolicy retryPolicy)
        {
            this.OperationTimeout = operationTimeout;
            this.RetryPolicy = retryPolicy;
        }

        public Uri Endpoint { get; set; }

        public AmqpConnectionCapabilities ConnectionCapabilities { get; private set; }

        /// <summary>
        /// OperationTimeout is applied in erroneous situations to notify the caller about the relevant <see cref="ServiceBusException"/>
        /// </summary>
        public TimeSpan OperationTimeout { get; set; }

        /// <summary>
        /// Get the retry policy instance that was created as part of this builder's creation.
        /// </summary>
        public RetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Get the shared access policy key value from the connection string
        /// </summary>
        /// <value>Shared Access Signature key</value>
        public string SasKey { get; set; }

        /// <summary>
        /// Get the shared access policy owner name from the connection string
        /// </summary>
        public string SasKeyName { get; set; }

        /// <summary>
        /// Get the SASL plain username from the connection string
        /// </summary>
        /// <value>SASL plain username</value>
        public string SaslPlainUsername { get; set; }

        /// <summary>
        /// Get the SASL plain password from the connection string
        /// </summary>
        public string SaslPlainPassword { get; set; }

        internal FaultTolerantAmqpObject<AmqpConnection> ConnectionManager { get; set; }

        public Task CloseAsync()
        {
            return this.ConnectionManager.CloseAsync();
        }

        protected void InitializeConnection(ServiceBusConnectionStringBuilder builder)
        {
            this.ConnectionCapabilities = new AmqpConnectionCapabilities();
            this.Endpoint = builder.Endpoint;
            this.SasKeyName = builder.SasKeyName;
            this.SasKey = builder.SasKey;
            if (!string.IsNullOrEmpty(builder.SaslPlainUsername) ||
                 !string.IsNullOrEmpty(builder.SaslPlainPassword))
            {
                this.SaslPlainUsername = builder.SaslPlainUsername;
                this.SaslPlainPassword = builder.SaslPlainPassword;
            }
            this.ConnectionManager = new FaultTolerantAmqpObject<AmqpConnection>(this.CreateConnectionAsync, CloseConnection);
        }

        static void CloseConnection(AmqpConnection connection)
        {
            MessagingEventSource.Log.AmqpConnectionClosed(connection);
            connection.SafeClose();
        }

        async Task<AmqpConnection> CreateConnectionAsync(TimeSpan timeout)
        {
            string hostName = this.Endpoint.Host;
            string networkHost = this.Endpoint.Host;
            int port = this.Endpoint.Port;
            bool secure = this.Endpoint.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase) || this.Endpoint.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase);
            string productName = null;
       
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            AmqpSettings amqpSettings = AmqpConnectionHelper.CreateAmqpSettings(
                amqpVersion: AmqpVersion,
                useSslStreamSecurity: secure &&  port != 5672,
                sslStreamUpgrade: secure && port == 5672,
                hasTokenProvider: this.SasKeyName != null,
                networkCredential: this.SaslPlainUsername != null ? new NetworkCredential(this.SaslPlainUsername, this.SaslPlainPassword) : null,
                forceTokenProvider : this.SasKeyName != null,
                useWebSockets : this.Endpoint.Scheme.StartsWith("ws", StringComparison.OrdinalIgnoreCase));

            TransportSettings tpSettings = AmqpConnectionHelper.CreateTcpTransportSettings(
                networkHost: networkHost,
                hostName: hostName,
                port: port,
                useSslStreamSecurity: true);

            AmqpTransportInitiator initiator = new AmqpTransportInitiator(amqpSettings, tpSettings);
            TransportBase transport = await initiator.ConnectTaskAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

            string containerId = Guid.NewGuid().ToString();
            AmqpConnectionSettings amqpConnectionSettings = AmqpConnectionHelper.CreateAmqpConnectionSettings(AmqpConstants.DefaultMaxFrameSize, containerId, hostName);
            AmqpConnection connection = new AmqpConnection(transport, amqpSettings, amqpConnectionSettings);
            await connection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

            this.ConnectionCapabilities.TopologyModel = TopologyModel.AzureServiceBus;
            if (connection.Settings.Properties != null)
            {
                if (connection.Settings.Properties.TryGetValue("product", out productName))
                {
                    if ( productName.StartsWith("apache-activemq-artemis", StringComparison.OrdinalIgnoreCase))
                    {
                        this.ConnectionCapabilities.TopologyModel = TopologyModel.Artemis;
                    }
                }
            }
            if (!String.IsNullOrEmpty(this.SasKeyName) && !String.IsNullOrEmpty(this.SasKey))
            {
                // Always create the CBS Link + Session
                AmqpCbsLink cbsLink = new AmqpCbsLink(connection);
                if (connection.Extensions.Find<AmqpCbsLink>() == null)
                {
                    connection.Extensions.Add(cbsLink);
                }
            }

            MessagingEventSource.Log.AmqpConnectionCreated(hostName, connection);

            return connection;
        }
    }
}