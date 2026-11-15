using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using GBV_Shield.Services;
using Easing = Microsoft.Maui.Easing;

namespace GBV_Shield.Features.Common
{
    public partial class SplashPage : ContentPage
    {
        private readonly AuthService _auth;
        private readonly ReportsService _reports;

        public SplashPage(AuthService auth, ReportsService reports)
        {
            InitializeComponent();
            _auth = auth;
            _reports = reports;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var start = DateTime.UtcNow;

            var animationTask = RunAnimationsAsync();
            var workTask = DoStartupWorkAsync();

            await Task.WhenAll(animationTask, workTask);

            var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
            if (elapsed < 1500)
                await Task.Delay((int)(1500 - elapsed));

            var loggedIn = false;
            try { loggedIn = await workTask; } catch { loggedIn = false; }

            ((App)Application.Current!).SetRootPage(
                loggedIn
                    ? new AppShell()
                    : new NavigationPage(new Features.Auth.LoginPage()));
        }

        private async Task RunAnimationsAsync()
        {
            if (LogoImage != null)
            {
                await LogoImage.FadeToAsync(1, 400, Easing.CubicOut);
                await LogoImage.ScaleToAsync(1.1, 400, Easing.SpringOut);
                await LogoImage.ScaleToAsync(1.0, 300, Easing.SpringIn);
            }

            if (LoadingLabel != null)
                await LoadingLabel.FadeToAsync(1, 600, Easing.CubicIn);
        }

        private async Task<bool> DoStartupWorkAsync()
        {
            try
            {
                await _reports.SyncPendingReportsAsync();
                return await _auth.IsLoggedInAsync();
            }
            catch
            {
                // if anything fails during startup, keep app usable
                return false;
            }
        }
    }
}
