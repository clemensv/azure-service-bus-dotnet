// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.ServiceBus.Amqp
{
    using Microsoft.Azure.Amqp;

    public class AmqpSendReceiveLinkCreator : AmqpLinkCreator
    {
        readonly EntityPath entityPath;
        readonly ServiceBusConnection serviceBusConnection;

        public AmqpSendReceiveLinkCreator(EntityPath entityPath, ServiceBusConnection serviceBusConnection, string[] requiredClaims, ICbsTokenProvider cbsTokenProvider, AmqpLinkSettings linkSettings)
            : base(entityPath, serviceBusConnection, requiredClaims, cbsTokenProvider, linkSettings)
        {
            this.entityPath = entityPath;
            this.serviceBusConnection = serviceBusConnection;
        }

        protected override AmqpObject OnCreateAmqpLink(AmqpConnection connection, AmqpLinkSettings linkSettings, AmqpSession amqpSession)
        {
            if ((linkSettings.Target as Azure.Amqp.Framing.Target) != null && 
                ((Azure.Amqp.Framing.Target)linkSettings.Target).Address.ToString() == string.Empty ) 
            {
                ((Azure.Amqp.Framing.Target)linkSettings.Target).Address = entityPath.Resolve(serviceBusConnection.ConnectionCapabilities.TopologyModel);
            }
            else if ((linkSettings.Source as Azure.Amqp.Framing.Source) != null &&
                ((Azure.Amqp.Framing.Source)linkSettings.Source).Address.ToString() == string.Empty)
            {
                ((Azure.Amqp.Framing.Source)linkSettings.Source).Address = entityPath.Resolve(serviceBusConnection.ConnectionCapabilities.TopologyModel);
            }
            AmqpObject link = (linkSettings.IsReceiver()) ? (AmqpObject)new ReceivingAmqpLink(linkSettings) : (AmqpObject)new SendingAmqpLink(linkSettings);
            linkSettings.LinkName = $"{connection.Settings.ContainerId};{connection.Identifier}:{amqpSession.Identifier}:{link.Identifier}";
            ((AmqpLink)link).AttachTo(amqpSession);
            return link;
        }
    }
}