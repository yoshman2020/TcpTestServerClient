using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using TcpTestServerClient.Contracts.Services;
using TcpTestServerClient.Contracts.Services.Events;

namespace TcpTestServerClient.Services;

/// <summary>
/// TCPサービス
/// </summary>
/// <param name="logger">ロガー</param>
public partial class TcpService(ILogger<TcpService> logger)
    : ITcpService, IDisposable
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
    /// サーバーであるか
    /// </summary>
    public bool IsServer
    {
        get;
        private set;
    }

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
    /// <param name="isServer">サーバーであるか</param>
    /// <param name="hostName">ホスト名</param>
    /// <param name="port">ポート</param>
    /// <param name="timeoutSec">タイムアウト（秒）</param>
    /// <param name="isStxEtxEnabled">STX、ETX使用</param>
    /// <returns>接続状態</returns>
    /// <exception cref="Exception">その他エラー</exception>
    public async Task<ConnectionStatus> ConnectAsync(bool isServer,
        string hostName, int port, int timeoutSec, bool isStxEtxEnabled)
    {
        _logger.LogDebug(
            @"Start isServer={isServer}, hostName={hostName}, port={port}, timeoutSec={timeoutSec}, isStxEtxEnabled={isStxEtxEnabled}",
            isServer, hostName, port, timeoutSec, isStxEtxEnabled);
        try
        {
            IsServer = isServer;
            // タイムアウト（ミリ秒）
            _timeoutMsec = timeoutSec * 1000;
            if (isServer)
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
            }
            else
            {
                _client = new()
                {
                    SendTimeout = _timeoutMsec,
                    ReceiveTimeout = _timeoutMsec,
                };
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _isCanceled = false;
            if (isServer)
            {
                _client = await _listener!.AcceptTcpClientAsync(
                _cancellationTokenSource.Token);
                _client!.SendTimeout = _timeoutMsec;
                _client!.ReceiveTimeout = _timeoutMsec;
            }
            else
            {
                await _client!.ConnectAsync(
                    hostName, port, _cancellationTokenSource.Token);
            }
            _stream = _client.GetStream();
            _isConnected = true;
            _ = ReceiveMessageAsync(isStxEtxEnabled);
        }
        catch (OperationCanceledException)
        {
            // 自身が切断（停止）
            _logger.LogInformation("Self disconnected");
            Disconnect(DisconnectionCause.SelfDisconnected);
            return ConnectionStatus.SelfDisconnected;
        }
        catch (SocketException socketEx)
        {
            // 相手が切断（停止）
            _logger.LogInformation("Other disconnected");
            _logger.LogDebug(@"{Message}", socketEx.Message);
            if (isServer)
            {
                Disconnect(DisconnectionCause.OtherDisconnected);
                return ConnectionStatus.OtherDisconnected;
            }
            else
            {
                // Serverが未開始
                Disconnect(DisconnectionCause.ServerNotStarted);
                return ConnectionStatus.ServerNotStarted;
            }
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
    /// <param name="isServer">サーバーであるか</param>
    /// <param name="hostName">ホスト名</param>
    /// <param name="port">ポート</param>
    /// <param name="timeoutSec">タイムアウト（秒）</param>
    /// <param name="isStxEtxEnabled">STX、ETX使用</param>
    /// <param name="maxAttempts">再試行回数</param>
    /// <returns>接続状態</returns>
    public async Task<ConnectionStatus> ReconnectAsync(bool isServer,
        string hostName, int port, int timeoutSec, bool isStxEtxEnabled,
        int maxAttempts = 3)
    {
        _logger.LogDebug(
            @"start isServer={isServer}, hostName={hostName}, port={port}, timeoutSec={timeoutSec}, isStxEtxEnabled={isStxEtxEnabled}, maxAttempts={maxAttempts}",
            isServer, hostName, port, timeoutSec, isStxEtxEnabled, maxAttempts);
        var attempts = 0;
        while (attempts < maxAttempts && !_isConnected)
        {
            try
            {
                var ret = await ConnectAsync(
                    isServer, hostName, port, timeoutSec, isStxEtxEnabled);
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
            _isConnected = false;
            _listener?.Stop();
            if (!_isCanceled)
            {
                _cancellationTokenSource?.Cancel();
                _stream?.Dispose();
                _client?.Dispose();
                _cancellationTokenSource?.Dispose();
                _isCanceled = true;
                Disconnected?.Invoke(this, disconnectionCause);
            }
            else
            {
                // 念のため
                _stream?.Dispose();
                _client?.Dispose();
                _cancellationTokenSource?.Dispose();
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
    /// <param name="isStxEtxEnabled">STX、ETX使用</param>
    /// <returns></returns>
    public async Task ReceiveMessageAsync(bool isStxEtxEnabled)
    {
        _logger.LogDebug("Start");

        if (_stream == null || !_isConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        try
        {
            var buffer = new byte[BufferSize];
            // メッセージを格納するためのバッファ
            var messageBuffer = new List<byte>();

            while (_isConnected && _stream != null)
            {
                var bytesRead = await _stream!.ReadAsync(buffer,
                    _cancellationTokenSource!.Token);

                if (bytesRead == 0)
                {
                    // 相手が切断（停止）
                    Disconnect(DisconnectionCause.OtherDisconnected);
                    _logger.LogDebug("End");
                    return;
                }

                if (isStxEtxEnabled)
                {
                    // STX、ETX使用
                    // 受信したバイトを messageBuffer に追加
                    for (var i = 0; i < bytesRead; i++)
                    {
                        var currentByte = buffer[i];

                        // STX（0x02）でメッセージの開始
                        if (currentByte == 0x02)
                        {
                            // STXが見つかったら、以前のメッセージをクリア
                            messageBuffer.Clear();
                        }
                        // ETX（0x03）でメッセージの終了
                        else if (currentByte == 0x03)
                        {
                            if (messageBuffer.Count > 0)
                            {
                                // 完全なメッセージを取得した場合
                                var message =
                                    Encoding.UTF8.GetString([.. messageBuffer]);
                                // 受信したメッセージを通知
                                MessageReceived?.Invoke(this, message);
                            }
                        }
                        else
                        {
                            // STXとETXの間のデータをバッファに追加
                            messageBuffer.Add(currentByte);
                        }
                    }
                }
                else
                {
                    // STX、ETX未使用
                    // 完全なメッセージを取得した場合
                    var messageSe =
                        Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    // 受信したメッセージを通知
                    MessageReceived?.Invoke(this, messageSe);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 自身が切断（停止）
            _logger.LogDebug("Self disconnected");
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
    /// <param name="message">送信メッセージ</param>
    /// <param name="isStxEtxEnabled">STX、ETX使用</param>
    /// <returns></returns>
    public async Task SendMessageAsync(string message, bool isStxEtxEnabled)
    {
        _logger.LogDebug(@"Start message={message}", message);
        if (_stream == null || !_isConnected)
        {
            throw new InvalidOperationException("Not connected");
        }
        try
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            // STX、ETXを使用する場合は追加
            var bufferSend = isStxEtxEnabled ? [0x02, .. buffer, 0x03] : buffer;
            await _stream!.WriteAsync(bufferSend, _cancellationTokenSource!.Token);

            // 送信したメッセージを通知
            MessageSend?.Invoke(this, message);
        }
        catch (OperationCanceledException)
        {
            // 自身が切断（停止）
            _logger.LogDebug("Self disconnected");
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