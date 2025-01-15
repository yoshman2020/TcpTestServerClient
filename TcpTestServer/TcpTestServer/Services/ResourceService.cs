using Microsoft.Windows.ApplicationModel.Resources;
using TcpTestServer.Contracts.Services;

namespace TcpTestServer.Services;

/// <summary>
/// リソースサービス
/// </summary>
public class ResourceService : IResourceService
{
    /// <summary>
    /// リソースマネージャー
    /// </summary>
    private readonly ResourceManager _resourceManager;

    /// <summary>
    /// リソースコンテキスト
    /// </summary>
    private readonly ResourceContext _resourceContext;

    /// <summary>
    /// リソースマップ
    /// </summary>
    private readonly ResourceMap _resourceMap;

    /// <summary>
    /// リソースサービス
    /// </summary>
    public ResourceService()
    {
        _resourceManager = new Microsoft.Windows.ApplicationModel.Resources
            .ResourceManager();
        _resourceContext = _resourceManager.CreateResourceContext();
        _resourceMap = _resourceManager.MainResourceMap.GetSubtree("Resources");
    }

    /// <summary>
    /// リソース文字列取得
    /// </summary>
    /// <param name="resourceKey">リソースキー</param>
    /// <returns>リソース文字列</returns>
    public string GetString(string resourceKey)
    {
        try
        {
            var replaced = resourceKey.Replace(".", "/");
            return _resourceMap.GetValue(
                replaced, _resourceContext).ValueAsString;
        }
        catch
        {
            return resourceKey;
        }
    }

    /// <summary>
    /// 言語設定
    /// </summary>
    /// <param name="languageCode"></param>
    public void SetLanguage(string languageCode)
    {
        _resourceContext.QualifierValues["Language"] = languageCode;
    }

    /// <summary>
    /// 現在の言語
    /// </summary>
    public string CurrentLanguage =>
        _resourceContext.QualifierValues["Language"];
}
