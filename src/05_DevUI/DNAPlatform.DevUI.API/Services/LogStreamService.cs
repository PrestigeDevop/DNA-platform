using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DNAPlatform.DevUI.API.Services;

namespace DNAPlatform.DevUI.API.Services;

/// <summary>
/// SSE event for log streaming.
/// </summary>
public class LogStreamEvent
{
    public string Event { get; set; } = "log";
    public LogEntry Data { get; set; } = new();
}

/// <summary>
/// Service that produces an SSE stream of log entries from the LogStore.
/// Uses Channels for efficient backpressure handling and subscribes to
/// LogStore.OnEntryAdded for immediate streaming.
/// </summary>
public class LogStreamService : IDisposable
{
    private readonly ILogStore _logStore;
    private readonly ILogger<LogStreamService> _logger;
    private readonly ConcurrentDictionary<Guid, Channel<LogEntry>> _subscribers = new();
    private bool _disposed;

    public LogStreamService(ILogStore logStore, ILogger<LogStreamService> logger)
    {
        _logStore = logStore;
        _logger = logger;

        // Subscribe to log store events for immediate broadcasting
        if (_logStore is InMemoryLogStore inMemoryStore)
        {
            inMemoryStore.OnEntryAdded += OnLogEntryAdded;
        }
    }

    private void OnLogEntryAdded(LogEntry entry)
    {
        if (_disposed) return;

        foreach (var (id, channel) in _subscribers)
        {
            if (!channel.Writer.TryWrite(entry))
            {
                // Channel full or closed, remove subscriber
                _subscribers.TryRemove(id, out _);
            }
        }
    }

    /// <summary>
    /// Creates a new subscription channel for a client.
    /// </summary>
    public ChannelReader<LogEntry> Subscribe(long sinceId = 0)
    {
        var channel = Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        var subscriptionId = Guid.NewGuid();
        _subscribers.TryAdd(subscriptionId, channel);

        // Send existing entries since the given ID
        var existingEntries = _logStore.Get(sinceId, 500);
        foreach (var entry in existingEntries)
        {
            channel.Writer.TryWrite(entry);
        }

        // Register cleanup when the reader completes
        _ = Task.Run(async () =>
        {
            await channel.Reader.Completion;
            _subscribers.TryRemove(subscriptionId, out _);
        });

        return channel.Reader;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_logStore is InMemoryLogStore inMemoryStore)
            {
                inMemoryStore.OnEntryAdded -= OnLogEntryAdded;
            }
            foreach (var channel in _subscribers.Values)
            {
                channel.Writer.Complete();
            }
            _subscribers.Clear();
        }
    }
}
