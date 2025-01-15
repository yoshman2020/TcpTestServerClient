using Microsoft.UI.Xaml.Controls;

using TcpTestClient.ViewModels;

namespace TcpTestClient.Views;

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
