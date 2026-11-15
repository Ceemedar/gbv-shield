using Microsoft.Maui.Controls;
using System;
using GBV_Shield.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GBV_Shield.Features.Stories
{
    public partial class AddStoryPage : ContentPage
    {
        private readonly StoryService _storyService;

        // Parameterless so Shell can construct it; we resolve the service ourselves
        public AddStoryPage()
        {
            InitializeComponent();
            _storyService = App.Services.GetRequiredService<StoryService>();
        }

        private async void OnPostStoryClicked(object sender, EventArgs e)
        {
            var author = string.IsNullOrWhiteSpace(AuthorEntry.Text) ? "Anonymous" : AuthorEntry.Text.Trim();
            var content = ContentEditor.Text?.Trim();

            if (string.IsNullOrWhiteSpace(content))
            {
                await DisplayAlertAsync("Error", "⚠️ Please enter your story before posting.", "OK");
                return;
            }

            var story = new Story
            {
                Author = author,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _storyService.AddStoryAsync(story);
                await DisplayAlertAsync("Success", "✅ Your story has been posted.", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to post story: {ex.Message}", "OK");
            }
        }
    }
}
