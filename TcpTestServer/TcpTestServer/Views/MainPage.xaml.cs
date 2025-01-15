using Microsoft.UI.Xaml.Controls;

using TcpTestServer.ViewModels;

namespace TcpTestServer.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }
}
