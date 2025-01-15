using Microsoft.UI.Xaml.Data;

namespace TcpTestServerClient.Helpers;

/// <summary>
/// Boolを反転する
/// </summary>
public partial class BoolInvertConverter : IValueConverter
{
    /// <summary>
    /// 反転する
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="language"></param>
    /// <returns></returns>
    public object Convert(
        object value, Type targetType, object parameter, string language)
    {
        return value is not bool isOn || !isOn;
    }

    /// <summary>
    /// 戻す（未使用）
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="language"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public object ConvertBack(
        object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
