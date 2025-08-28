using Microsoft.Extensions.Configuration;

namespace Roachagram.MobileUI.Services
{
    public class RoachagramAPIService(HttpClient httpClient, IConfiguration configuration) : IRoachagramAPIService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly string _apiBaseUrl = configuration["ApiBaseUrl"] ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing.");

        public async Task<string> GetAnagramsAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input cannot be null or empty.", nameof(input));

            var endpoint = $"{_apiBaseUrl}/anagrams?input={Uri.EscapeDataString(input)}";
            var response = await _httpClient.GetAsync(endpoint);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
