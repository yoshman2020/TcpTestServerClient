namespace TcpTestServerClient.Contracts.Services.Events;

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
    /// 画面を閉じる
    /// </summary>
    CloseWindow,
}
