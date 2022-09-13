namespace Microsoft.eShopOnContainers.BuildingBlocks.EventBusRabbitMQ;

/// <summary>
/// 默认 RabbitMQ 持久化连接类
/// </summary>
public class DefaultRabbitMQPersistentConnection
    : IRabbitMQPersistentConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<DefaultRabbitMQPersistentConnection> _logger;
    private readonly int _retryCount;
    private IConnection _connection;
    public bool Disposed;

    readonly object _syncRoot = new();

    public DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory, ILogger<DefaultRabbitMQPersistentConnection> logger, int retryCount = 5)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryCount = retryCount;
    }

    public bool IsConnected => _connection is { IsOpen: true } && !Disposed;

    /// <summary>
    /// 创建并返回一个新的通道、会话和模型
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IModel CreateModel()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
        }

        return _connection.CreateModel();
    }

    public void Dispose()
    {
        if (Disposed) return;

        Disposed = true;

        try
        {
            _connection.ConnectionShutdown -= OnConnectionShutdown;
            _connection.CallbackException -= OnCallbackException;
            _connection.ConnectionBlocked -= OnConnectionBlocked;
            _connection.Dispose();
        }
        catch (IOException ex)
        {
            _logger.LogCritical(ex.ToString());
        }
    }
    /// <summary>
    /// 建立连接
    /// </summary>
    /// <returns></returns>
    public bool TryConnect()
    {
        _logger.LogInformation("RabbitMQ Client is trying to connect");

        lock (_syncRoot)
        {
            var policy = RetryPolicy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
                }
            );

            policy.Execute(() =>
            {
                _connection = _connectionFactory
                        .CreateConnection();
            });

            if (IsConnected)
            {
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;

                _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);

                return true;
            }
            else
            {
                _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

                return false;
            }
        }
    }
    /// <summary>
    /// 连接受阻
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        if (Disposed) return;

        _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

        TryConnect();
    }
    /// <summary>
    /// 当连接调用的回调中发生异常时发出信号
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void OnCallbackException(object sender, CallbackExceptionEventArgs e)
    {
        if (Disposed) return;

        _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

        TryConnect();
    }

    /// <summary>
    /// 当连接时关闭时
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="reason"></param>
    void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
    {
        if (Disposed) return;

        _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

        TryConnect();
    }
}
