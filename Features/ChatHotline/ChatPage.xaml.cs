using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace GBV_Shield.Features.ChatHotline
{
    public partial class ChatPage : ContentPage
    {
        private readonly ChatViewModel _viewModel;

        public ChatPage()
        {
            InitializeComponent();
            _viewModel = App.Services.GetRequiredService<ChatViewModel>();
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.StartAsync();
        }

        protected override void OnDisappearing()
        {
            _viewModel.Stop();
            base.OnDisappearing();
        }
    }
}
