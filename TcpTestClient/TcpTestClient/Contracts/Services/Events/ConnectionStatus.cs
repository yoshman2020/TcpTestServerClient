namespace TcpTestClient.Contracts.Services.Events;

/// <summary>
/// 接続状態
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// サーバー未開始
    /// </summary>
    ServerNotStarted,

    /// <summary>
    /// サーバー停止
    /// </summary>
    ServerStopped,

    /// <summary>
    /// クライアント切断
    /// </summary>
    ClientDisconnected,

    /// <summary>
    /// エラー発生
    /// </summary>
    ErrorOccured,

    /// <summary>
    /// 接続OK
    /// </summary>
    Connected,
}
