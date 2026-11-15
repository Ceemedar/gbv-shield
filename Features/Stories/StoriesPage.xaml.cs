using Microsoft.Maui.Controls;
using System;
using GBV_Shield.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GBV_Shield.Features.Stories
{
    public partial class StoriesPage : ContentPage
    {
        private readonly StoryService _storyService;

        // Shell constructs pages with parameterless ctors, so resolve via App.Services
        public StoriesPage()
        {
            InitializeComponent();
            _storyService = App.Services.GetRequiredService<StoryService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                var stories = await _storyService.GetStoriesAsync();
                StoriesCollection.ItemsSource = stories;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to load stories: {ex.Message}", "OK");
            }
        }

        private async void OnAddStoryClicked(object sender, EventArgs e)
        {
            // navigate to add page
            await Navigation.PushAsync(new AddStoryPage());
        }
    }
}
