using System;
using Microsoft.Maui.Controls;
using GBV_Shield.Services;

namespace GBV_Shield.Features.Auth
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _authService = new();

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            Busy.IsVisible = Busy.IsRunning = true;

            var email = EmailEntry?.Text?.Trim() ?? string.Empty;
            var password = PasswordEntry?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                Busy.IsVisible = Busy.IsRunning = false;
                await DisplayAlertAsync("Error", "Please enter email & password", "OK");
                return;
            }

            try
            {
                var user = await _authService.LoginAsync(email, password);
                Busy.IsVisible = Busy.IsRunning = false;

                if (user != null)
                {
                    ((App)Application.Current!).SetRootPage(new AppShell());
                }
                else
                {
                    await DisplayAlertAsync("Error", "❌ Invalid credentials or network error.", "OK");
                }
            }
            catch (Exception ex)
            {
                Busy.IsVisible = Busy.IsRunning = false;
                await DisplayAlertAsync("Error", $"❌ Login failed: {ex.Message}", "OK");
            }
        }

        private async void OnGoToRegisterClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }

        private async void OnQuickSendSOSClicked(object sender, EventArgs e)
        {
            try { await SosService.SendSosAsync(); }
            catch (Exception ex) { await DisplayAlertAsync("Error", $"Unable to send SOS: {ex.Message}", "OK"); }
        }

        private async void OnQuickCallHotlineClicked(object sender, EventArgs e)
        {
            try { await SosService.CallHotlineAsync(); }
            catch (Exception ex) { await DisplayAlertAsync("Error", $"Unable to call hotline: {ex.Message}", "OK"); }
        }
    }
}
