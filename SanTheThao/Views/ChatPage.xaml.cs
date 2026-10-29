using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _vm;

    public ChatPage(ChatViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        vm.ScrollToEnd += OnScrollToEnd;
    }

    private void OnScrollToEnd()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(120);   // chờ danh sách vẽ xong rồi mới cuộn
            if (_vm.Messages.Count > 0)
                MessageList.ScrollTo(_vm.Messages.Count - 1, position: ScrollToPosition.End, animate: false);
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.StartAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Stop();
    }
}