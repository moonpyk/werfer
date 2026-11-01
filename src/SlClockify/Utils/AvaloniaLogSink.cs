using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Logging;
using Microsoft.Extensions.Logging;

namespace SlClockify.Utils;

/// <summary>
///     Forwards Avalonia's own logs (bindings, layout, platform...) to Microsoft.Extensions.Logging,
///     one category per area, e.g. "Avalonia.Binding".
/// </summary>
public class AvaloniaLogSink(
    ILoggerFactory loggerFactory
) : ILogSink
{
    private ILoggerFactory LoggerFactory { get; } = loggerFactory;

    // Avalonia checks IsEnabled very often, a dozen areas at most so this stays small
    private ConcurrentDictionary<string, ILogger> Loggers { get; } = new();

    /// <inheritdoc />
    public bool IsEnabled(LogEventLevel level, string area)
    {
        return GetLogger(area).IsEnabled(ToLogLevel(level));
    }

    /// <inheritdoc />
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        Log(level, area, source, messageTemplate, []);
    }

    /// <inheritdoc />
    [SuppressMessage("Performance", "CA1873", Justification = "Is a log forwarder")]
    [SuppressMessage("Usage", "CA2254", Justification = "Is a log forwarder")]
    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem", Justification = "Is a log forwarder")]
    public void Log(
        LogEventLevel level,
        string area,
        object? source,
        string messageTemplate,
        params object?[] propertyValues
    )
    {
        var logger = GetLogger(area);

        try
        {
            // Avalonia templates use named placeholders, which ILogger also understands; the source tells which control logged
            logger.Log(
                ToLogLevel(level),
                "{Source}: " + messageTemplate,
                [source, .. propertyValues]
            );
        }
        catch (FormatException e)
        {
            // Fewer values than placeholders makes ILogger throw, which would surface inside Avalonia: log the raw parts instead
            logger.Log(
                ToLogLevel(level),
                e,
                "{Source}: {Template} {Values}",
                source,
                messageTemplate,
                string.Join(", ", propertyValues)
            );
        }
    }

    private ILogger GetLogger(string area)
    {
        // Static lambda with the factory passed as state: no delegate allocated per call
        return Loggers.GetOrAdd(
            area,
            static (a, factory) => factory.CreateLogger($"Avalonia.{a}"),
            LoggerFactory
        );
    }

    private static LogLevel ToLogLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Information => LogLevel.Information,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            _ => LogLevel.Critical
        };
    }
}
