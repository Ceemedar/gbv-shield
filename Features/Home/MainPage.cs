using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace GBV_Shield.Features.Home
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnTileTapped(object sender, TappedEventArgs e)
        {
            if (sender is not Border tile) return;

            // Small tap animation for feedback
            await tile.ScaleToAsync(0.96, 100, Easing.CubicOut);
            await tile.ScaleToAsync(1, 100, Easing.CubicIn);

            var param = e.Parameter as string;
            if (string.IsNullOrWhiteSpace(param)) return;

            // Absolute tab routes like "//ChatPage" or "//SOSPage"
            if (param.StartsWith("//"))
            {
                await Shell.Current.GoToAsync(param);
                return;
            }

            // Fallback: try as a registered Shell route (e.g., "ShelterPage", "ReportPage")
            await Shell.Current.GoToAsync(param);
        }
    }
}
