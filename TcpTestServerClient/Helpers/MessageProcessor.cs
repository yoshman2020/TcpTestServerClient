namespace TcpTestServerClient.Helpers;

/// <summary>
/// メッセージ処理
/// </summary>
public class MessageProcessor
{
    /// <summary>
    /// 返信メッセージ作成
    /// </summary>
    /// <param name="receivedMessage">受信メッセージ</param>
    /// <returns></returns>
    public static string CreateResponse(string receivedMessage)
    {
        // 返信用文字列を作成する処理をここに追加する
        return "Response: " + receivedMessage;
    }
}
