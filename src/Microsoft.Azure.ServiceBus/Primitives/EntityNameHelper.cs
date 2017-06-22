// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.ServiceBus.Amqp;

namespace Microsoft.Azure.ServiceBus
{
    public static class EntityNameHelper
    {
        
        public static EntityPath FormatDeadLetterPath(string entityPath)
        {
            return new DeadletterQueuePath(entityPath);
        }
        
        public static EntityPath FormatSubscriptionPath(string topicPath, string subscriptionName)
        {
            return new SubscriptionPath(topicPath, subscriptionName);
        }
    }
}