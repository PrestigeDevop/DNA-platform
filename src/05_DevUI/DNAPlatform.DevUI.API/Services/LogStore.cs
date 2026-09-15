using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace DNAPlatform.DevUI.API.Services;

/// <summary>
/// A single log record. Kept in an in-memory ring buffer and exposed to the web UI
/// through <c>GET /api/logs</c>.
/// </summary>
public class LogEntry
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "Information";
    public string Category { get; set; } = "DNAPlatform";
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string Source { get; set; } = "backend";
}

/// <summary>
/// Read/write contract for the in-memory log buffer.
/// </summary>
public interface ILogStore
{
    void Add(LogEntry entry);
    IReadOnlyList<LogEntry> Get(long sinceId = 0, int limit = 500);
    void Clear();
    int Count { get; }
}

/// <summary>
/// Thread-safe, capped (ring-buffer style) log store. Older entries are dropped
/// once <see cref="Capacity"/> is exceeded so a long-running dev session cannot
/// exhaust memory.
/// </summary>
public class InMemoryLogStore : ILogStore
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private long _sequence;

    public int Capacity { get; }

    public InMemoryLogStore(int capacity = 2000)
    {
        Capacity = capacity < 1 ? 1 : capacity;
    }

    public int Count => _entries.Count;

    public void Add(LogEntry entry)
    {
        if (entry == null) return;

        entry.Id = System.Threading.Interlocked.Increment(ref _sequence);
        if (entry.Timestamp == default) entry.Timestamp = DateTime.UtcNow;
        entry.Source = string.IsNullOrWhiteSpace(entry.Source) ? "backend" : entry.Source;

        _entries.Enqueue(entry);

        // Trim the oldest entries once over capacity.
        while (_entries.Count > Capacity && _entries.TryDequeue(out _))
        {
            // discard
        }
    }

    public IReadOnlyList<LogEntry> Get(long sinceId = 0, int limit = 500)
    {
        var snapshot = _entries.ToArray();
        IEnumerable<LogEntry> filtered = sinceId > 0
            ? snapshot.Where(e => e.Id > sinceId)
            : snapshot;

        if (limit <= 0) limit = 200;

        return filtered.TakeLast(limit).ToArray();
    }

    public void Clear()
    {
        while (_entries.TryDequeue(out _))
        {
            // discard
        }
    }
}

/// <summary>
/// Logging provider that mirrors every log record into <see cref="ILogStore"/> so the
/// SvelteKit Logs page can display the very same lines that appear in the CLI console.
/// </summary>
public sealed class LogStoreLoggerProvider : ILoggerProvider
{
    private readonly ILogStore _store;
    private readonly ConcurrentDictionary<string, LogStoreLogger> _loggers = new();

    public LogStoreLoggerProvider(ILogStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name => new LogStoreLogger(name, _store));

    public void Dispose() => _loggers.Clear();
}

internal sealed class LogStoreLogger : ILogger
{
    private const int MaxMessageLength = 4000;

    private readonly string _category;
    private readonly ILogStore _store;

    public LogStoreLogger(string category, ILogStore store)
    {
        _category = string.IsNullOrWhiteSpace(category) ? "DNAPlatform" : category;
        _store = store;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (formatter == null) return;

        string message = formatter(state, exception) ?? string.Empty;
        if (message.Length > MaxMessageLength)
            message = message.Substring(0, MaxMessageLength) + " …(truncated)";

        _store.Add(new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = logLevel.ToString(),
            Category = _category,
            Message = message,
            Exception = exception?.ToString(),
            Source = "backend"
        });
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}