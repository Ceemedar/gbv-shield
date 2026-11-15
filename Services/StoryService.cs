using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GBV_Shield.Services
{
    public class StoryService
    {
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri(
                "https://gbvshield-api-a9c5bmg9g2c9d8as.southafricanorth-01.azurewebsites.net/"
            )
        };

        public async Task AddStoryAsync(Story story)
        {
            var json = JsonSerializer.Serialize(story);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("api/stories", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<Story>> GetStoriesAsync()
        {
            var response = await _http.GetAsync("api/stories");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Story>>(json)!;
        }
    }
}
