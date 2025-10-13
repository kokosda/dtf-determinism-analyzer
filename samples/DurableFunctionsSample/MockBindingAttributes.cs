using Microsoft.Azure.Functions.Worker;

namespace DurableFunctionsSample;

/// <summary>
/// Mock binding attributes for demonstration purposes.
/// These simulate Azure Functions binding attributes to show DFA0010 detection
/// without requiring the full Azure Functions binding packages.
/// 
/// In a real application, these would come from:
/// - Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs
/// - Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues  
/// - Microsoft.Azure.Functions.Worker.Extensions.Storage.Tables
/// - Microsoft.Azure.Functions.Worker.Extensions.ServiceBus
/// </summary>

[AttributeUsage(AttributeTargets.Parameter)]
public class BlobTriggerAttribute : Attribute
{
    public string BlobPath { get; }
    public BlobTriggerAttribute(string blobPath) => BlobPath = blobPath;
}

[AttributeUsage(AttributeTargets.Parameter)]
public class QueueTriggerAttribute : Attribute
{
    public string QueueName { get; }
    public QueueTriggerAttribute(string queueName) => QueueName = queueName;
}

[AttributeUsage(AttributeTargets.Parameter)]
public class TableAttribute : Attribute
{
    public string TableName { get; }
    public TableAttribute(string tableName) => TableName = tableName;
}

[AttributeUsage(AttributeTargets.Parameter)]
public class ServiceBusTriggerAttribute : Attribute
{
    public string TopicName { get; }
    public string SubscriptionName { get; }
    
    public ServiceBusTriggerAttribute(string topicName, string subscriptionName)
    {
        TopicName = topicName;
        SubscriptionName = subscriptionName;
    }
}

/// <summary>
/// Mock interface for Azure Functions table output collectors.
/// </summary>
public interface IAsyncCollector<T>
{
    Task AddAsync(T item, CancellationToken cancellationToken = default);
}

/// <summary>
/// Mock implementation of IAsyncCollector for demonstration.
/// </summary>
public class MockAsyncCollector<T> : IAsyncCollector<T>
{
    private readonly List<T> _items = new();

    public Task AddAsync(T item, CancellationToken cancellationToken = default)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }

    public IReadOnlyList<T> Items => _items.AsReadOnly();
}