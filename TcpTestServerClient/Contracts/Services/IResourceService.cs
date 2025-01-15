namespace TcpTestServerClient.Contracts.Services;

/// <summary>
/// リソースサービス
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// リソース文字列取得
    /// </summary>
    /// <param name="resourceKey">リソースキー</param>
    /// <returns>リソース文字列</returns>
    string GetString(string resourceKey);

    void SetLanguage(string languageCode);

    string CurrentLanguage
    {
        get;
    }
}
