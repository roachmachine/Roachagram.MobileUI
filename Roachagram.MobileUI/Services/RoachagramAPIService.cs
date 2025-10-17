using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;

namespace Roachagram.MobileUI.Services
{
    /// <summary>
    /// Service class for interacting with the Roachagram API.
    /// Provides methods to fetch anagrams and manage device-specific identifiers.
    /// </summary>
    public class RoachagramAPIService(HttpClient httpClient, IConfiguration configuration, IRemoteTelemetryService remoteTelemetryService) : IRoachagramAPIService
    {
        // HttpClient instance used for making API requests.
        private readonly HttpClient _httpClient = httpClient;

        // Base URL for the API, retrieved from the configuration.
        private readonly string _apiBaseUrl = configuration["ApiBaseUrl"] ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing.");

        /// <summary>
        /// Fetches anagrams for the given input string from the API.
        /// </summary>
        /// <param name="input">The input string for which anagrams are to be fetched.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the API response as a string.</returns>
        /// <exception cref="ArgumentException">Thrown when the input is null or empty.</exception>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
        public async Task<string> GetAnagramsAsync(string input)
        {
            try
            {
                // Retrieve or create a unique device identifier.
                var device_uuid = await GetOrCreateDeviceUUIDAsync();

                // Ensure the "X-Device-UUID" header is set with the current device UUID.
                if (_httpClient.DefaultRequestHeaders.Contains("X-Device-ID"))
                {
                    _httpClient.DefaultRequestHeaders.Remove("X-Device-ID");
                }
                _httpClient.DefaultRequestHeaders.Add("X-Device-ID", device_uuid);

                // Validate the input string.
                if (string.IsNullOrWhiteSpace(input))
                    throw new ArgumentException("Input cannot be null or empty.", nameof(input));

                // Construct the API endpoint URL.
                var endpoint = $"{_apiBaseUrl}api/anagram?input={Uri.EscapeDataString(input)}";

                
                // Make the GET request to the API.
                var response = await _httpClient.GetAsync(endpoint);

                // Ensure the response indicates success.
                response.EnsureSuccessStatusCode();

                // Return the response content as a string.
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Retrieves the device UUID from secure storage, or creates a new one if it does not exist.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the device UUID.</returns>
        private async Task<string> GetOrCreateDeviceUUIDAsync()
        {
            try
            {
                // Attempt to retrieve an existing device UUID from secure storage.
                var device_uuid = await SecureStorage.GetAsync("device_uuid");
                if (!string.IsNullOrEmpty(device_uuid))
                {
                    return device_uuid;
                }

                // Generate a new UUID and store it securely.
                var newId = Guid.NewGuid().ToString();
                await SecureStorage.SetAsync("device_uuid", newId);
                return newId;
            }
            catch (Exception ex)
            {
                //log the message to app insights
                await remoteTelemetryService.TrackExceptionAsync(ex);

                // If secure storage is not available, fallback to a temporary UUID.
                return Guid.NewGuid().ToString();
            }
        }
    }
}