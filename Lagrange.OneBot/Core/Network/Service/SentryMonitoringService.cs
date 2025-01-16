using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Reflection;
using Lagrange.Core.Common;

namespace Lagrange.OneBot.Core.Network.Service;

public class SentryMonitoringService : IHostedService
{
    private readonly ILogger<SentryMonitoringService> _logger;
    private readonly IConfiguration _configuration;
    private IDisposable? _sentryDisposable;
    private readonly Lagrange.Core.Common.Sentry _sentryConfig;

    public SentryMonitoringService(
        ILogger<SentryMonitoringService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _sentryConfig = new Lagrange.Core.Common.Sentry();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _configuration.GetSection("Sentry").Bind(_sentryConfig);

        if (string.IsNullOrWhiteSpace(_sentryConfig.Dsn))
        {
            _logger.LogWarning("Sentry service is disabled because DSN is not set");
            return Task.CompletedTask;
        }

        try
        {
            _sentryDisposable = SentrySdk.Init(options =>
            {
                // 必需的配置
                options.Dsn = _sentryConfig.Dsn;
                options.Debug = _sentryConfig.Debug;
                options.Environment = _sentryConfig.Environment;
                options.Release = !string.IsNullOrEmpty(_sentryConfig.Release)
                    ? _sentryConfig.Release
                    : Assembly.GetEntryAssembly()?
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                        .InformationalVersion ?? "unknown";

                // 采样率配置
                options.TracesSampleRate = _sentryConfig.TracesSampleRate;
                options.SampleRate = _sentryConfig.SampleRate;
                
                // options.ProfilesSampleRate = 1.0;
                // options.AddIntegration(new ProfilingIntegration(
                //     // During startup, wait up to 500ms to profile the app startup code. This could make launching the app a bit slower so comment it out if your prefer profiling to start asynchronously
                //     TimeSpan.FromMilliseconds(500)
                // ));

                // 诊断级别
                options.DiagnosticLevel = _sentryConfig.DiagnosticLevel switch
                {
                    DiagnosticLevel.Debug => SentryLevel.Debug,
                    DiagnosticLevel.Info => SentryLevel.Info,
                    DiagnosticLevel.Warning => SentryLevel.Warning,
                    DiagnosticLevel.Error => SentryLevel.Error,
                    DiagnosticLevel.Fatal => SentryLevel.Fatal,
                    _ => SentryLevel.Error
                };

                // 数据管理
                options.MaxBreadcrumbs = _sentryConfig.MaxBreadcrumbs;
                options.AttachStacktrace = _sentryConfig.AttachStacktrace;
                options.SendDefaultPii = _sentryConfig.SendDefaultPii;
                options.AutoSessionTracking = _sentryConfig.AutoSessionTracking;

                // 代理配置
                if (_sentryConfig.Proxy != null && !string.IsNullOrEmpty(_sentryConfig.Proxy.Host))
                {
                    var webProxy = new WebProxy
                    {
                        Address = new Uri($"http://{_sentryConfig.Proxy.Host}:{_sentryConfig.Proxy.Port}")
                    };

                    if (!string.IsNullOrEmpty(_sentryConfig.Proxy.Username))
                    {
                        webProxy.Credentials = new NetworkCredential(
                            _sentryConfig.Proxy.Username,
                            _sentryConfig.Proxy.Password
                        );
                    }

                    options.HttpProxy = webProxy;
                }
            });

            _logger.LogInformation("Sentry was successfully configured");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Sentry");
            _sentryConfig.Dsn = string.Empty;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _sentryDisposable?.Dispose();
            _logger.LogInformation("Sentry service has been stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping Sentry service");
        }

        return Task.CompletedTask;
    }

    public Lagrange.Core.Common.Sentry GetConfig() => _sentryConfig;
}