using Microsoft.UI.Xaml.Controls;

using TcpTestServerClient.ViewModels;

namespace TcpTestServerClient.Views;

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
