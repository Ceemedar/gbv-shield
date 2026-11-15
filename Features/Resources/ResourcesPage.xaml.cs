using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using GBV_Shield.Services;
using System;
using System.Threading.Tasks;

namespace GBV_Shield.Features.Resources
{
    public partial class ResourcesPage : ContentPage
    {
        private readonly AiClient _aiClient = new AiClient();

        public ResourcesPage()
        {
            InitializeComponent();
        }

        private async Task SafeLaunchAsync(string uri, string errorMessage)
        {
            try
            {
                await Launcher.Default.OpenAsync(uri);
            }
            catch
            {
                await DisplayAlertAsync("Error", errorMessage, "OK");
            }
        }

        private async void OnAskAIClicked(object sender, EventArgs e)
        {
            var question = AiInput.Text?.Trim();

            if (string.IsNullOrWhiteSpace(question))
            {
                await DisplayAlertAsync("AI Assistant", "Please type a question first.", "OK");
                return;
            }

            AiResponse.Text = "Thinking...";

            try
            {
                var reply = await _aiClient.AskAsync(
                    question,
                    city: "Johannesburg",
                    country: "South Africa");

                AiResponse.Text = reply;
            }
            catch
            {
                AiResponse.Text = "Could not get AI response right now.";
            }
        }

        // 📖 Legal Aid
        private async void OnLegalAidWebsiteClicked(object sender, EventArgs e)
            => await SafeLaunchAsync("http://www.legal-aid.co.za/", "Could not open Legal Aid website.");

        private async void OnLegalAidCallClicked(object sender, EventArgs e)
            => await SafeLaunchAsync("tel:0800110110", "Could not start Legal Aid call.");

        // 🧑‍⚕️ GBV Command Centre
        private async void OnGBVHelplineCallClicked(object sender, EventArgs e)
            => await SafeLaunchAsync("tel:0800428428", "Could not start GBV Helpline call.");

        private async void OnGBVWhatsAppClicked(object sender, EventArgs e)
            => await SafeLaunchAsync("https://wa.me/27828213445", "Could not open WhatsApp chat.");

        private async void OnGBVUssdClicked(object sender, EventArgs e)
            => await SafeLaunchAsync("tel:*120*7867%23", "Could not start USSD code.");

        private async void OnGBVWebsiteClicked(object sender, EventArgs e)
            => await SafeLaunchAsync("https://gbv.org.za/", "Could not open GBV Helpline website.");

        // 🚨 Emergency 112
        private async void OnEmergencyCallClicked(object sender, EventArgs e)
            => await SafeLaunchAsync("tel:112", "Could not start emergency call.");
    }
}
