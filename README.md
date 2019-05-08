# Using Shared Access Signature (SAS) authentication with Service Bus Subscriptions
Shared Access Signature (SAS) authentication allows applications to authenticate to  Azure Service Bus using an access key configured on the namespace or the entity with specific rights associated with it. This key can then be used to generate a Shared Access Signature token that clients can use to authenticate to Service Bus.
Authorization rules for SAS can be configured on a Service Bus namespace or a queue, topic, relay or notification hub. Configuration of authorization rules for SAS on subscriptions is currently not supported. However, you can use an access key configured on a namespace or a topic to generate SAS tokens that are scoped to a subscription. Since the token is scoped to a subscription, it can be shared with clients whose access needs to be limited to a given subscription only.
This sample demonstrates the use of Shared Access Signature authentication and authorization with Service Bus subscriptions. It includes:
* Operations on the Service Bus namespace to create a Service Bus topic and a subscription. 
* Operations on the Service Bus namespace to send a message to a topic. 
* Operations on a Service Bus subscription to receive messages using a SAS token. 

