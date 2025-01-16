namespace Lagrange.Core.Common;

public class Sentry
{
    // 必需的配置
    public string Dsn { get; set; } = string.Empty;

    // 环境相关
    public string Environment { get; set; } = "production";
    public string Release { get; set; } = string.Empty;

    // 性能和采样
    public double TracesSampleRate { get; set; } = 1.0;
    public float SampleRate { get; set; } = 1.0f;

    // 调试和诊断
    public bool Debug { get; set; } = false;
    public DiagnosticLevel DiagnosticLevel { get; set; } = DiagnosticLevel.Error;

    // 数据管理
    public bool AttachStacktrace { get; set; } = true;
    public int MaxBreadcrumbs { get; set; } = 100;

    // 会话跟踪
    public bool AutoSessionTracking { get; set; } = true;

    // 数据收集
    public bool SendDefaultPii { get; set; } = false;

    // 代理
    public ProxyOptions? Proxy { get; set; }
}

public enum DiagnosticLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}

public class ProxyOptions
{
    // 代理服务器地址
    public string? Host { get; set; }

    // 代理服务器端口
    public int? Port { get; set; }

    // 代理类型 (HTTP/SOCKS4/SOCKS5)
    public ProxyType Type { get; set; } = ProxyType.Http;

    // 代理认证信息（如果需要）
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public enum ProxyType
{
    Http,
    Socks4,
    Socks5
}