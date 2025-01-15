namespace TcpTestServerClient.Contracts.Services.Events;

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
    /// 自身が切断（停止）
    /// </summary>
    SelfDisconnected,

    /// <summary>
    /// 相手が切断（停止）
    /// </summary>
    OtherDisconnected,

    /// <summary>
    /// エラー発生
    /// </summary>
    ErrorOccured,

    /// <summary>
    /// 接続OK
    /// </summary>
    Connected,
}
