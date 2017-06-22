// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.ServiceBus
{
    using System;
    using Microsoft.Azure.ServiceBus.Amqp;

    public class EntityPath
    {
        public const string PathDelimiter = @"/";
        public const string Subscriptions = "Subscriptions";
        public const string SubQueuePrefix = "$";
        public const string DeadLetterQueueSuffix = "DeadLetterQueue";
        public const string DeadLetterQueueName = SubQueuePrefix + DeadLetterQueueSuffix;

        string entityPath;

        public EntityPath(string entityPath)
        {
            if (string.IsNullOrWhiteSpace(entityPath))
            {
                throw Fx.Exception.ArgumentNullOrWhiteSpace(entityPath);
            }
            this.entityPath = entityPath;
        }

        public virtual string Resolve(TopologyModel topologyModel)
        {
            return entityPath;
        }

        public override string ToString()
        {
            return entityPath;
        }
    }

    public class QueuePath : EntityPath
    {
        public QueuePath(string queueName) : base(queueName)
        {
        }
    }

    public class DeadletterQueuePath : QueuePath
    {
        public DeadletterQueuePath(string baseQueueName) : base(baseQueueName)
        {
        }

        public override string Resolve(TopologyModel topologyModel)
        {
            if (topologyModel == TopologyModel.Artemis)
            {
                return "DLQ";
            }
            return base.Resolve(topologyModel) + "/$deadletter";
        }
    }

    public class TopicPath : EntityPath
    {
        public TopicPath(string topicName):base(topicName)
        {
            
        }
    }

    public class SubscriptionPath : TopicPath
    {
        string subscriptionName;

        public SubscriptionPath(string topicName, string subscriptionName) : base(topicName)
        {
            this.subscriptionName = subscriptionName;
        }

        public string SubscriptionName { get => subscriptionName; }

        public override string Resolve(TopologyModel topologyModel)
        {
            if ( topologyModel == TopologyModel.Artemis)
            {
                return base.Resolve(topologyModel) + "::" + SubscriptionName;
            }
            return base.Resolve(topologyModel) + "/Subscriptions/" + SubscriptionName;
        }
    }

    public class SubscriptionDeadletterPath : SubscriptionPath
    {
        public SubscriptionDeadletterPath(string topicName, string subscriptionName) : base(topicName, subscriptionName)
        {
        }

        public override string Resolve(TopologyModel topologyModel)
        {
            if (topologyModel == TopologyModel.Artemis)
            {
                return "DLQ";
            }
            return base.Resolve(topologyModel);
        }
    }

    public class EntityManagementPath : EntityPath
    {
        EntityPath innerEntityPath;

        public EntityManagementPath(EntityPath innerEntityPath):base("$")
        {
            this.innerEntityPath = innerEntityPath;    
        }

        public override string Resolve(TopologyModel topologyModel)
        {
            return innerEntityPath.Resolve(topologyModel) + "/" + AmqpClientConstants.ManagementAddress;
        }

        public override string ToString()
        {
            return innerEntityPath.ToString();
        }
    }
}
