// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.ServiceBus.Amqp
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Amqp.Framing;

    public abstract class AmqpLinkCreator
    {
        readonly EntityPath entityPath;
        readonly ServiceBusConnection serviceBusConnection;
        readonly string[] requiredClaims;
        readonly ICbsTokenProvider cbsTokenProvider;
        readonly AmqpLinkSettings amqpLinkSettings;

        protected AmqpLinkCreator(EntityPath entityPath, ServiceBusConnection serviceBusConnection, string[] requiredClaims, ICbsTokenProvider cbsTokenProvider, AmqpLinkSettings amqpLinkSettings)
        {
            this.entityPath = entityPath;
            this.serviceBusConnection = serviceBusConnection;
            this.requiredClaims = requiredClaims;
            this.cbsTokenProvider = cbsTokenProvider;
            this.amqpLinkSettings = amqpLinkSettings;
        }

        public async Task<AmqpObject> CreateAndOpenAmqpLinkAsync()
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(this.serviceBusConnection.OperationTimeout);

            MessagingEventSource.Log.AmqpGetOrCreateConnectionStart();
            AmqpConnection connection = await this.serviceBusConnection.ConnectionManager.GetOrCreateAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            string resolvedEntityPath = this.entityPath.Resolve(this.serviceBusConnection.ConnectionCapabilities.TopologyModel);
            MessagingEventSource.Log.AmqpGetOrCreateConnectionStop(resolvedEntityPath, connection.ToString(), connection.State.ToString());

            if (cbsTokenProvider != null)
            {
                // Authenticate over CBS
                AmqpCbsLink cbsLink = connection.Extensions.Find<AmqpCbsLink>();
                Uri address = new Uri(this.serviceBusConnection.Endpoint, resolvedEntityPath);
                string audience = address.AbsoluteUri;
                string resource = address.AbsoluteUri;

                MessagingEventSource.Log.AmqpSendAuthenticanTokenStart(address, audience, resource, this.requiredClaims);
                await cbsLink.SendTokenAsync(this.cbsTokenProvider, address, audience, resource, this.requiredClaims, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                MessagingEventSource.Log.AmqpSendAuthenticanTokenStop();
            }

            AmqpSession session = null;
            try
            {
                // Create Session
                AmqpSessionSettings sessionSettings = new AmqpSessionSettings { Properties = new Fields() };
                session = connection.CreateSession(sessionSettings);
                await session.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                MessagingEventSource.Log.AmqpSessionCreationException(resolvedEntityPath, connection, exception);
                session?.Abort();
                throw;
            }

            try
            {
                // Create Link
                AmqpObject link = this.OnCreateAmqpLink(connection, this.amqpLinkSettings, session);
                await link.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                return link;
            }
            catch (Exception exception)
            {
                MessagingEventSource.Log.AmqpLinkCreationException(
                    resolvedEntityPath,
                    session,
                    connection,
                    exception);

                throw;
            }
        }

        protected abstract AmqpObject OnCreateAmqpLink(AmqpConnection connection, AmqpLinkSettings linkSettings, AmqpSession amqpSession);
    }
}