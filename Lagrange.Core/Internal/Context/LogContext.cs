using Lagrange.Core.Common;
using Lagrange.Core.Event;
using Lagrange.Core.Event.EventArg;

namespace Lagrange.Core.Internal.Context;

/// <summary>
/// Log context, all the logs will be dispatched to this context and then to the <see cref="BotLogEvent"/>.
/// </summary>
internal class LogContext : ContextBase
{
    private readonly EventInvoker _invoker;

    public LogContext(ContextCollection collection, BotKeystore keystore, BotAppInfo appInfo, BotDeviceInfo device,
        EventInvoker invoker)
        : base(collection, keystore, appInfo, device) => _invoker = invoker;

    public void LogDebug(string tag, string message)
    {
        _invoker.PostEvent(new BotLogEvent(tag, LogLevel.Debug, message));
        SentrySdk.AddBreadcrumb(
            message: message,
            category: tag,
            level: BreadcrumbLevel.Debug
        );
    }

    public void LogVerbose(string tag, string message)
    {
        _invoker.PostEvent(new BotLogEvent(tag, LogLevel.Verbose, message));
        SentrySdk.AddBreadcrumb(
            message: message,
            category: tag,
            level: BreadcrumbLevel.Info
        );
    }

    public void LogInfo(string tag, string message)
    {
        _invoker.PostEvent(new BotLogEvent(tag, LogLevel.Information, message));
        SentrySdk.AddBreadcrumb(
            message: message,
            category: tag,
            level: BreadcrumbLevel.Info
        );
    }

    public void LogWarning(string tag, string message)
    {
        _invoker.PostEvent(new BotLogEvent(tag, LogLevel.Warning, message));
        SentrySdk.AddBreadcrumb(
            message: message,
            category: tag,
            level: BreadcrumbLevel.Warning
        );
        SentrySdk.CaptureMessage(
            message: $"[{tag}] {message}",
            level: SentryLevel.Warning
        );
    }

    public void LogFatal(string tag, string message)
    {
        var logEvent = new BotLogEvent(tag, LogLevel.Fatal, message);
        _invoker.PostEvent(logEvent);

        var exception = new Exception($"[{tag}] {message}");

        var sentryEvent = new SentryEvent(exception) { Level = SentryLevel.Fatal, };

        sentryEvent.SetTag("tag", tag);

        SentrySdk.CaptureEvent(sentryEvent);
    }

    public void Log(string tag, LogLevel level, string message)
    {
        _invoker.PostEvent(new BotLogEvent(tag, level, message));

        var sentryLevel = level switch
        {
            LogLevel.Debug => SentryLevel.Debug,
            LogLevel.Verbose => SentryLevel.Debug,
            LogLevel.Information => SentryLevel.Info,
            LogLevel.Warning => SentryLevel.Warning,
            LogLevel.Fatal => SentryLevel.Fatal,
            _ => SentryLevel.Info
        };

        SentrySdk.AddBreadcrumb(
            message: message,
            category: tag,
            level: ConvertToBreakcrumbLevel(level)
        );

        if (level >= LogLevel.Warning)
        {
            SentrySdk.CaptureMessage(
                message: $"[{tag}] {message}",
                level: sentryLevel
            );
        }
    }

    private static BreadcrumbLevel ConvertToBreakcrumbLevel(LogLevel level) => level switch
    {
        LogLevel.Debug => BreadcrumbLevel.Debug,
        LogLevel.Verbose => BreadcrumbLevel.Info,
        LogLevel.Information => BreadcrumbLevel.Info,
        LogLevel.Warning => BreadcrumbLevel.Warning,
        LogLevel.Fatal => BreadcrumbLevel.Critical,
        _ => BreadcrumbLevel.Info
    };
}