using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using TcpTestServer.Contracts.Services;
using TcpTestServer.Contracts.Services.Events;

namespace TcpTestServer.Services;

/// <summary>
/// TCPサービス
/// </summary>
/// <param name="logger">ロガー</param>
public partial class TcpService(ILogger<TcpService> logger) : ITcpService, IDisposable
{
    /// <summary>
    /// メッセージ受信時のイベント
    /// </summary>
    public event EventHandler<string>? MessageReceived;

    /// <summary>
    /// メッセージ送信時のイベント
    /// </summary>
    public event EventHandler<string>? MessageSend;

    /// <summary>
    /// 切断時のイベント
    /// </summary>
    public event EventHandler<DisconnectionCause>? Disconnected;

    /// <summary>
    /// 接続済み
    /// </summary>
    public bool IsConnected => _isConnected && _client?.Connected == true;

    /// <summary>
    /// ロガー
    /// </summary>
    private readonly ILogger<TcpService> _logger = logger;

    /// <summary>
    /// TCPリスナー
    /// </summary>
    private TcpListener? _listener;

    /// <summary>
    /// TCPクライアント
    /// </summary>
    private TcpClient? _client;

    /// <summary>
    /// ネットワークストリーム
    /// </summary>
    private NetworkStream? _stream;

    /// <summary>
    /// キャンセルトークン
    /// </summary>
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// 接続中
    /// </summary>
    private bool _isConnected;

    /// <summary>
    /// キャンセル済み
    /// </summary>
    private bool _isCanceled;

    /// <summary>
    /// タイムアウト（ミリ秒）
    /// </summary>
    private int _timeoutMsec;

    /// <summary>
    /// バッファサイズ
    /// </summary>
    private const int BufferSize = 1024;

    /// <summary>
    /// 接続
    /// </summary>
    /// <param name="hostName">ホスト名</param>
    /// <param name="port">ポート</param>
    /// <param name="timeoutSec">タイムアウト（秒）</param>
    /// <returns>接続状態</returns>
    /// <exception cref="Exception">その他エラー</exception>
    public async Task<ConnectionStatus> ConnectAsync(
        string hostName, int port, int timeoutSec)
    {
        _logger.LogDebug(
            @"Start hostName={hostName}, port={port}, timeoutSec={timeoutSec}",
            hostName, port, timeoutSec);
        try
        {
            // タイムアウト（ミリ秒）
            _timeoutMsec = timeoutSec * 1000;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            _cancellationTokenSource = new CancellationTokenSource();
            _isCanceled = false;
            _client = await _listener.AcceptTcpClientAsync(
                _cancellationTokenSource.Token);
            _client!.SendTimeout = _timeoutMsec;
            _client!.ReceiveTimeout = _timeoutMsec;
            _stream = _client.GetStream();
            _isConnected = true;
            _ = ReceiveMessageAsync();
        }
        catch (OperationCanceledException)
        {
            // Server自身が停止
            _logger.LogInformation("Server stopped");
            Disconnect(DisconnectionCause.ServerStopped);
            return ConnectionStatus.ServerStopped;
        }
        catch (SocketException socketEx)
        {
            // Clientが切断
            _logger.LogInformation("Client Disconnected");
            _logger.LogDebug(@"{Message}", socketEx.Message);
            Disconnect(DisconnectionCause.ClientDisconnected);
            return ConnectionStatus.ClientDisconnected;
        }
        catch (Exception ex)
        {
            // その他エラー
            _logger.LogError(ex, "", []);
            Disconnect(DisconnectionCause.ErrorOccured);
            throw;
        }
        _logger.LogDebug("End");
        return ConnectionStatus.Connected;
    }

    /// <summary>
    /// 再接続
    /// </summary>
    /// <param name="hostName">ホスト名</param>
    /// <param name="port">ポート</param>
    /// <param name="timeoutSec">タイムアウト（秒）</param>
    /// <param name="maxAttempts">再試行回数</param>
    /// <returns>接続状態</returns>
    public async Task<ConnectionStatus> ReconnectAsync(
        string hostName, int port, int timeoutSec, int maxAttempts = 3)
    {
        _logger.LogDebug(
            @"start hostName={hostName}, port={port}, timeoutSec={timeoutSec}, maxAttempts={maxAttempts}",
            hostName, port, timeoutSec, maxAttempts);
        var attempts = 0;
        while (attempts < maxAttempts && !_isConnected)
        {
            try
            {
                var ret = await ConnectAsync(hostName, port, timeoutSec);
                return ret;
            }
            catch (Exception)
            {
                attempts++;
                if (attempts == maxAttempts)
                {
                    throw;
                }

                await Task.Delay(1000 * attempts); // Exponential backoff
            }
        }
        _logger.LogDebug("End");
        return ConnectionStatus.ErrorOccured;
    }

    /// <summary>
    /// 切断
    /// </summary>
    /// <param name="disconnectionCause">切断原因</param>
    public void Disconnect(DisconnectionCause disconnectionCause)
    {
        _logger.LogDebug(@"Start disconnectionCause={disconnectionCause}",
            disconnectionCause);
        try
        {
            if (!_isCanceled)
            {
                _isConnected = false;
                _listener?.Stop();
                _cancellationTokenSource?.Cancel();
                _stream?.Dispose();
                _client?.Dispose();
                _cancellationTokenSource?.Dispose();
                _isCanceled = true;
                Disconnected?.Invoke(this, disconnectionCause);
            }
        }
        catch (ObjectDisposedException)
        {
            // 切断済み
            _logger.LogDebug("Already disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "", []);
        }
        _logger.LogDebug("End");
    }

    /// <summary>
    /// 受信
    /// </summary>
    /// <returns></returns>
    public async Task ReceiveMessageAsync()
    {
        _logger.LogDebug("Start");

        if (_stream == null || !_isConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        try
        {
            var buffer = new byte[BufferSize];

            while (_isConnected && _stream != null)
            {
                var bytesRead = await _stream!.ReadAsync(buffer,
                    _cancellationTokenSource!.Token);

                if (bytesRead == 0)
                {
                    // クライアントが切断
                    Disconnect(DisconnectionCause.ClientDisconnected);
                    _logger.LogDebug("End");
                    return;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // 受信したメッセージを通知
                MessageReceived?.Invoke(this, message);
            }
        }
        catch (OperationCanceledException)
        {
            // サーバーが停止
            _logger.LogDebug("Stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "", []);
            throw;
        }
        _logger.LogDebug("End");
    }

    /// <summary>
    /// 送信
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task SendMessageAsync(string message)
    {
        _logger.LogDebug(@"Start message={message}", message);
        if (_stream == null || !_isConnected)
        {
            throw new InvalidOperationException("Not connected");
        }
        try
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _stream!.WriteAsync(buffer, _cancellationTokenSource!.Token);

            // 送信したメッセージを通知
            MessageSend?.Invoke(this, message);
        }
        catch (OperationCanceledException)
        {
            // サーバーが停止
            _logger.LogDebug("Stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "", []);
            Disconnect(DisconnectionCause.ErrorOccured);
            throw;
        }
        _logger.LogDebug("End");
    }

    public void Dispose()
    {
        Disconnect(DisconnectionCause.CloseWindow);
        GC.SuppressFinalize(this);
    }
}