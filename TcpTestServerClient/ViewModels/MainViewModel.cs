using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using TcpTestServerClient.Contracts.Services;
using TcpTestServerClient.Contracts.Services.Events;
using TcpTestServerClient.Helpers;

namespace TcpTestServerClient.ViewModels;

/// <summary>
/// メインビューモデル
/// </summary>
public partial class MainViewModel : ObservableValidator
{
    /// <summary>
    /// TCPサービス
    /// </summary>
    private readonly ITcpService _tcpService;

    /// <summary>
    /// ロガー
    /// </summary>
    private readonly ILogger<MainViewModel> _logger;

    /// <summary>
    /// リソースサービス
    /// </summary>
    private readonly IResourceService _resourceService;

    /// <summary>
    /// ホスト名
    /// </summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
#pragma warning disable MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT
    private string? hostName = "localhost";

    /// <summary>
    /// ポート
    /// </summary>
    [ObservableProperty]
    private int port = 56789;

    /// <summary>
    /// タイムアウト（秒）
    /// </summary>
    [ObservableProperty]
    private int timeoutSec = 3;

    /// <summary>
    /// STX、ETX使用
    /// </summary>
    [ObservableProperty]
    private bool isStxEtxEnabled = true;

    /// <summary>
    /// サーバーであるか
    /// </summary>
    [ObservableProperty]
    private bool isServer = true;

    /// <summary>
    /// 接続可
    /// </summary>
    [ObservableProperty]
    private bool isConnectEnabled;

    /// <summary>
    /// 切断可
    /// </summary>
    [ObservableProperty]
    private bool isDisconnectEnabled;

    /// <summary>
    /// 受信メッセージ
    /// </summary>
    [ObservableProperty]
    private string? receivedMessage;

    /// <summary>
    /// 送信メッセージ
    /// </summary>
    [ObservableProperty]
    private string? sendMessage;

    /// <summary>
    /// 送信可
    /// </summary>
    [ObservableProperty]
    private bool isSendEnabled;

    /// <summary>
    /// 情報メッセージ表示
    /// </summary>
    [ObservableProperty]
    private bool isInfoMessageOpen;

    /// <summary>
    /// 情報メッセージタイトル
    /// </summary>
    [ObservableProperty]
    private string? infoMessageTitle;

    /// <summary>
    /// 情報メッセージ
    /// </summary>
    [ObservableProperty]
    private string? infoMessage;

    /// <summary>
    /// 情報メッセージ変更
    /// </summary>
    /// <param name="value"></param>
    partial void OnInfoMessageChanged(string? value)
    {
        IsInfoMessageOpen = !string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// 情報メッセージ重大度
    /// </summary>
    [ObservableProperty]
    private InfoBarSeverity infoMessageSeverity = InfoBarSeverity.Informational;

    /// <summary>
    /// ログメッセージ
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> logMessages = [];
#pragma warning restore MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT

    /// <summary>
    /// メインビューモデル
    /// </summary>
    /// <param name="tcpService">TCPサービス</param>
    /// <param name="logger">ロガー</param>
    /// <param name="resourceService">リソースサービス</param>
    public MainViewModel(
        ITcpService tcpService,
        ILogger<MainViewModel> logger,
        IResourceService resourceService)
    {
        // TCPサービス
        _tcpService = tcpService;
        _tcpService!.MessageReceived += TcpService_MessageReceived;
        _tcpService!.MessageSend += TcpService_MessageSend;
        _tcpService!.Disconnected += TcpService_Disconnected;

        // ロガー
        _logger = logger;

        // リソースサービス
        _resourceService = resourceService;

        ErrorsChanged += MainViewModel_ErrorsChanged;
        // 接続可
        IsConnectEnabled = true;
        IsDisconnectEnabled = false;
        // 送信不可
        IsSendEnabled = false;

        _logger.LogDebug("Initialized");
    }

    /// <summary>
    /// メッセージ受信後の処理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void TcpService_MessageReceived(object? sender, string e)
    {
        _logger.LogDebug("Start");
        try
        {
            var received = _resourceService.GetString("Received");
            LogMessages.Add($"{received}{e}");
            ReceivedMessage = e;

            if (IsServer)
            {
                // 送信可
                IsSendEnabled = true;

                // 返信メッセージ作成
                var response = MessageProcessor.CreateResponse(ReceivedMessage);

                // TcpServiceで送信
                await _tcpService.SendMessageAsync(response, IsStxEtxEnabled);
            }
            else
            {
                var send = _resourceService.GetString("Send");
                ShowInfoMessage(
                    $"{send}{SendMessage}\n{received}{ReceivedMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "", []);
            ShowError(ex);
        }
        _logger.LogDebug("End");
    }

    /// <summary>
    /// メッセージ送信後の処理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TcpService_MessageSend(object? sender, string e)
    {
        _logger.LogDebug(@"Start message={e}", e);
        try
        {
            var send = _resourceService.GetString("Send");
            LogMessages.Add($"{send}{e}");
            SendMessage = e;

            if (IsServer)
            {
                var received = _resourceService.GetString("Received");
                ShowInfoMessage(
                    $"{received}{ReceivedMessage}\n{send}{SendMessage}");
            }
            else
            {
                ShowInfoMessage($"{send}{SendMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "", []);
            ShowError(ex);
        }
        _logger.LogDebug("End");
    }


    /// <summary>
    /// 切断時の処理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="disconnectionCause">切断原因</param>
    private void TcpService_Disconnected(
        object? sender, DisconnectionCause disconnectionCause)
    {
        _logger.LogDebug(@"Start disconnectionCause={disconnectionCause}",
            disconnectionCause);
        try
        {
            // 接続可
            IsConnectEnabled = true;
            IsDisconnectEnabled = false;
            // 送信不可
            IsSendEnabled = false;
            switch (disconnectionCause)
            {
                case DisconnectionCause.ServerNotStarted:
                    // サーバー未開始
                    var errorKey = "Error_Connect";
                    ShowError(errorKey);
                    AddLogMessage(errorKey);
                    break;
                case DisconnectionCause.SelfDisconnected:
                    // 自身が切断（停止）
                    var successKey = IsServer ? "Stopped" : "Disconnected";
                    ShowSuccess(successKey);
                    AddLogMessage(successKey);
                    break;
                case DisconnectionCause.OtherDisconnected:
                    // 相手が切断（停止）
                    var warningKey = IsServer ? "Disconnected" : "Stopped";
                    ShowWarning(warningKey);
                    AddLogMessage(warningKey);
                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "", []);
            ShowError(ex);
        }
        _logger.LogDebug("End");
    }

    /// <summary>
    /// エラー変更
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MainViewModel_ErrorsChanged(
        object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
    {
        _logger.LogTrace("Start");
        try
        {
            if (HasErrors)
            {
                InfoMessageTitle = "Error";
                InfoMessage = GetErrors()?.FirstOrDefault()?.ErrorMessage;
                InfoMessageSeverity = InfoBarSeverity.Error;
            }
            else
            {
                InfoMessage = "";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "", []);
            ShowError(ex);
        }
        _logger.LogTrace("End");
    }

    /// <summary>
    /// 接続
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (HasErrors)
        {
            return;
        }
        _logger.LogDebug("Start");
        try
        {
            if (IsServer)
            {
                var localHostName = Dns.GetHostName();
                var localhost = await Dns.GetHostEntryAsync(localHostName);
                HostName = localhost.AddressList
                    .Where(a => !a.IsIPv6LinkLocal)
                    .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                    .FirstOrDefault()?.ToString() ?? "";
                var successKey = "Started";
                ShowSuccess(successKey);
                AddLogMessage(successKey);
            }
            // 接続不可
            IsConnectEnabled = false;
            IsDisconnectEnabled = true;
            IsSendEnabled = false;
            var connectionStatus = await _tcpService.ConnectAsync(IsServer,
                HostName!, Port, TimeoutSec, IsStxEtxEnabled);
            switch (connectionStatus)
            {
                case ConnectionStatus.ServerNotStarted:
                    // サーバー未開始による場合はTcpService_Disconnectedで通知
                    break;
                case ConnectionStatus.SelfDisconnected:
                    // 自身が切断（停止）による場合はTcpService_Disconnectedで通知
                    break;
                case ConnectionStatus.OtherDisconnected:
                    // 相手が切断（停止）による場合はTcpService_Disconnectedで通知
                    break;
                case ConnectionStatus.ErrorOccured:
                    // エラー発生
                    var errorKey = "Error_Connect";
                    ShowError(errorKey);
                    AddLogMessage(errorKey);
                    break;
                case ConnectionStatus.Connected:
                    // 接続OK
                    var connectedKey = "Connected";
                    ShowSuccess(connectedKey);
                    AddLogMessage(connectedKey);
                    // 送信可
                    IsSendEnabled = true;
                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
            _logger.LogError(ex, "", []);
        }
        _logger.LogDebug("End");
    }

    /// <summary>
    /// 送信
    /// </summary>
    [RelayCommand]
    private async Task SendAsync()
    {
        _logger.LogDebug("Start");
        try
        {
            if (string.IsNullOrEmpty(SendMessage))
            {
                return;
            }

            if (!_tcpService.IsConnected)
            {
                await _tcpService.ReconnectAsync(IsServer,
                    HostName!, Port, TimeoutSec, IsStxEtxEnabled);
            }

            await _tcpService.SendMessageAsync(SendMessage, IsStxEtxEnabled);
        }
        catch (Exception ex)
        {
            ShowError(ex);
            _logger.LogError(ex, "", []);
        }
        _logger.LogDebug("End");
    }

    /// <summary>
    /// 切断
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void Disconnect()
    {
        _logger.LogDebug("Start");
        try
        {
            DisconnectAndInit(DisconnectionCause.SelfDisconnected);
        }
        catch (Exception ex)
        {
            ShowError(ex);
            _logger.LogError(ex, "", []);
        }
        _logger.LogDebug("End");
    }

    /// <summary>
    /// 切断とボタン初期化
    /// </summary>
    /// <param name="disconnectionCause">切断原因</param>
    private void DisconnectAndInit(DisconnectionCause disconnectionCause)
    {
        _tcpService.Disconnect(disconnectionCause);
        // 接続可
        IsConnectEnabled = true;
        IsDisconnectEnabled = false;
        // 送信不可
        IsSendEnabled = false;
    }

    /// <summary>
    /// 情報表示
    /// </summary>
    /// <param name="message">メッセージ</param>
    private void ShowInfoMessage(string message)
    {
        InfoMessageTitle = "";
        InfoMessage = message;
        InfoMessageSeverity = InfoBarSeverity.Informational;
    }

    ///// <summary>
    ///// 情報表示
    ///// </summary>
    ///// <param name="resourceKey">リソースキー</param>
    //private void ShowInfo(string resourceKey)
    //{
    //    InfoMessageTitle = "";
    //    var message = _resourceService.GetString(resourceKey);
    //    InfoMessage = message;
    //    InfoMessageSeverity = InfoBarSeverity.Informational;
    //}

    /// <summary>
    /// 成功表示
    /// </summary>
    /// <param name="resourceKey">リソースキー</param>
    private void ShowSuccess(string resourceKey)
    {
        InfoMessageTitle = "";
        var message = _resourceService.GetString(resourceKey);
        InfoMessage = message;
        InfoMessageSeverity = InfoBarSeverity.Success;
    }

    /// <summary>
    /// 警告表示
    /// </summary>
    /// <param name="resourceKey">リソースキー</param>
    private void ShowWarning(string resourceKey)
    {
        InfoMessageTitle = "";
        var message = _resourceService.GetString(resourceKey);
        InfoMessage = message;
        InfoMessageSeverity = InfoBarSeverity.Warning;
    }

    /// <summary>
    /// エラー表示
    /// </summary>
    /// <param name="resourceKey">リソースキー</param>
    private void ShowError(string resourceKey)
    {
        var title = _resourceService.GetString("Error");
        InfoMessageTitle = title;
        var message = _resourceService.GetString(resourceKey);
        InfoMessage = message;
        InfoMessageSeverity = InfoBarSeverity.Error;

        DisconnectAndInit(DisconnectionCause.ErrorOccured);
    }

    /// <summary>
    /// エラー表示
    /// </summary>
    /// <param name="e">エラー</param>
    private void ShowError(Exception e)
    {
        var title = _resourceService.GetString("Error");
        InfoMessageTitle = title;
        InfoMessage = e.Message;
        InfoMessageSeverity = InfoBarSeverity.Error;
        LogMessages.Add(e.Message);

        DisconnectAndInit(DisconnectionCause.ErrorOccured);
    }

    /// <summary>
    /// ログメッセージ追加
    /// </summary>
    /// <param name="resourceKey">リソースキー</param>
    private void AddLogMessage(string resourceKey)
    {
        var message = _resourceService.GetString(resourceKey);
        LogMessages.Add(message);
    }
}
