using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace Microsoft.Azure.ServiceBus.ApiConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var qc = new QueueClient("Endpoint=amqp://clemensv:!!test111@localhost:5672","queue");
            qc.RegisterMessageHandler(QueueReceiver, new MessageHandlerOptions());

            for (int i = 0; i < 100; i++)
            {
                qc.SendAsync(new Message() { MessageId = Guid.NewGuid().ToString(), UserProperties = { { "Foo", "bar" } } }).GetAwaiter().GetResult();
            }

            var tc = new TopicClient("Endpoint=amqp://clemensv:!!test111@localhost:5672", "mytopic");
            var sc = new SubscriptionClient("Endpoint=amqp://clemensv:!!test111@localhost:5672", "mytopic", "subA");
            sc.RegisterMessageHandler(TopicReceiver, new MessageHandlerOptions());

            for (int i = 0; i < 100; i++)
            {
                tc.SendAsync(new Message() { MessageId = Guid.NewGuid().ToString(), UserProperties = { { "Foo", "bar" } } }).GetAwaiter().GetResult();
            }
            Console.ReadLine();
        }

        private static Task QueueReceiver(Message arg1, CancellationToken arg2)
        {
            Console.WriteLine("Q:"+arg1.MessageId);
            return Task.CompletedTask;
            
        }

        private static Task TopicReceiver(Message arg1, CancellationToken arg2)
        {
            Console.WriteLine("T:" + arg1.MessageId);
            return Task.CompletedTask;

        }
    }
}