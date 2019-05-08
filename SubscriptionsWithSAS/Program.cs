//---------------------------------------------------------------------------------
// Microsoft (R)  Windows Azure SDK
// Software Development Kit
// 
// Copyright (c) Microsoft Corporation. All rights reserved.  
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE. 
//---------------------------------------------------------------------------------

namespace Microsoft.Samples.SubscriptionsWithSAS
{
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Management;
    using Microsoft.Azure.ServiceBus.Primitives;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        internal static string nsConnectionString;
        internal const string topicPath = "contosoT";
        internal const string subscriptionName = "sasSubscription";
        internal static SharedAccessAuthorizationRule contosoTListenRule;

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            // The connection string for the RootManageSharedAccessKey can be accessed from the Azure portal 
            // by selecting the SB namespace and clicking on "Connection Information"
            Console.Write("Enter your connection string for the RootManageSharedAccessKey for your Service Bus namespace: ");
            nsConnectionString = Console.ReadLine();

            var cxn = new ServiceBusConnectionStringBuilder(nsConnectionString);
            ///////////////////////////////////////////////////////////////////////////////////////
            // Create a topic with a SAS Listen rule and an associated subscription
            ///////////////////////////////////////////////////////////////////////////////////////
            ManagementClient nm = new ManagementClient(cxn);
            contosoTListenRule = new SharedAccessAuthorizationRule("contosoTListenKey",
                new[] { AccessRights.Listen });
            
            TopicDescription td = new TopicDescription(topicPath);
            td.AuthorizationRules.Add(contosoTListenRule);
            if ((await nm.TopicExistsAsync(topicPath)))
            {
                await nm.DeleteTopicAsync(topicPath);
            }
            await nm.CreateTopicAsync(td);
            await nm.CreateSubscriptionAsync(topicPath, subscriptionName);
        
            ///////////////////////////////////////////////////////////////////////////////////////
            // Send a message to the topic
            // Note that this uses the connection string for RootManageSharedAccessKey 
            // configured on the namespace root
            ///////////////////////////////////////////////////////////////////////////////////////

            TopicClient tc = new TopicClient(cxn.GetNamespaceConnectionString(), topicPath, RetryPolicy.Default);
            Message sentMessage = CreateHelloMessage();
            await tc.SendAsync(sentMessage);
            Console.WriteLine("Sent Hello message to topic: ID={0}, Body={1}.", sentMessage.MessageId, Encoding.UTF8.GetString(sentMessage.Body));

            ///////////////////////////////////////////////////////////////////////////////////////
            // Generate a SAS token scoped to a subscription using the SAS rule with 
            // a Listen right configured on the Topic & TTL of 1 day
            ///////////////////////////////////////////////////////////////////////////////////////
            ServiceBusConnectionStringBuilder csBuilder = new ServiceBusConnectionStringBuilder(nsConnectionString);
            string subscriptionUri = new Uri(new Uri(csBuilder.Endpoint), topicPath + "/subscriptions/" + subscriptionName).ToString();

            // This is how you acquire a token in an STS to pass it out to a client.
            var tp = TokenProvider.CreateSharedAccessSignatureTokenProvider(contosoTListenRule.KeyName, contosoTListenRule.PrimaryKey);
            var subscriptionToken = await tp.GetTokenAsync(subscriptionUri, TimeSpan.FromMinutes(180));

            Console.WriteLine($"Acquired token: {subscriptionToken.TokenValue}");

            ///////////////////////////////////////////////////////////////////////////////////////
            // Use the SAS token scoped to a subscription to receive the messages
            ///////////////////////////////////////////////////////////////////////////////////////

            var are = new AutoResetEvent(false);
            SubscriptionClient sc = new SubscriptionClient(csBuilder.Endpoint, topicPath, subscriptionName, TokenProvider.CreateSharedAccessSignatureTokenProvider(subscriptionToken.TokenValue));
            sc.RegisterMessageHandler(async (m, c) =>
               {
                   Console.WriteLine("Received message from subscription: ID = {0}, Body = {1}.", m.MessageId, Encoding.UTF8.GetString(m.Body));
                   await sc.CompleteAsync(m.SystemProperties.LockToken);
                   are.Set();
               }, 
               new MessageHandlerOptions(async (e) => 
               {
                   Console.WriteLine($"Exception: {e.Exception.ToString()}");
               }){ AutoComplete = false });
            

            are.WaitOne();
            await sc.CloseAsync();

            ///////////////////////////////////////////////////////////////////////////////////////
            // Clean-up
            ///////////////////////////////////////////////////////////////////////////////////////
            await nm.DeleteTopicAsync(topicPath);
        }

        private static Message CreateHelloMessage()
        {
            Message helloMessage = new Message(Encoding.UTF8.GetBytes("Hello, Service Bus!"));
            helloMessage.MessageId = "SAS-Sample-Message";
            return helloMessage;
        }
    }
}
