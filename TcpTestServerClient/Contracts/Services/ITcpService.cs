using TcpTestServerClient.Contracts.Services.Events;

namespace TcpTestServerClient.Contracts.Services;

/// <summary>
/// TCPサービスインターフェース
/// </summary>
public interface ITcpService
{
    /// <summary>
    /// メッセージ受信時のイベント
    /// </summary>
    event EventHandler<string>? MessageReceived;

    /// <summary>
    /// メッセージ送信時のイベント
    /// </summary>
    event EventHandler<string>? MessageSend;

    /// <summary>
    /// 切断時のイベント
    /// </summary>
    event EventHandler<DisconnectionCause>? Disconnected;

    /// <summary>
    /// 接続済み
    /// </summary>
    bool IsConnected
    {
        get;
    }

    /// <summary>
    /// サーバーであるか
    /// </summary>
    bool IsServer
    {
        get;
    }

    /// <summary>
    /// 接続
    /// </summary>
    /// <param name="isServer">サーバーであるか</param>
    /// <param name="hostName">ホスト名</param>
    /// <param name="port">ポート</param>
    /// <param name="timeoutSec">タイムアウト（秒）</param>
    /// <returns>接続状態</returns>
    Task<ConnectionStatus> ConnectAsync(bool isServer,
        string hostName, int port, int timeoutSec);

    /// <summary>
    /// 再接続
    /// </summary>
    /// <param name="isServer">サーバーであるか</param>
    /// <param name="hostName">ホスト名</param>
    /// <param name="port">ポート</param>
    /// <param name="timeoutSec">タイムアウト（秒）</param>
    /// <param name="maxAttempts">再試行回数</param>
    /// <returns>接続状態</returns>
    public Task<ConnectionStatus> ReconnectAsync(bool isServer,
        string hostName, int port, int timeoutSec,
        int maxAttempts = 3);

    /// <summary>
    /// 切断
    /// </summary>
    /// <param name="disconnectionCause">切断原因</param>
    void Disconnect(DisconnectionCause disconnectionCause);

    /// <summary>
    /// 送信
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task SendMessageAsync(string message);

    /// <summary>
    /// 受信
    /// </summary>
    /// <returns></returns>
    Task ReceiveMessageAsync();
}