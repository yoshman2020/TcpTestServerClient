namespace TcpTestClient.Contracts.Services.Events;

/// <summary>
/// 切断原因
/// </summary>
public enum DisconnectionCause
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
    /// 画面を閉じる
    /// </summary>
    CloseWindow,
}
