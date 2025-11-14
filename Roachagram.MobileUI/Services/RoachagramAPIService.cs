using Microsoft.Extensions.Configuration;
using Polly;

namespace Roachagram.MobileUI.Services
{
    /// <summary>
    /// Service class for interacting with the Roachagram API.
    /// Provides methods to fetch anagrams and manage device-specific identifiers.
    /// </summary>
    public class RoachagramAPIService : IRoachagramAPIService
    {
        /// <summary>
        /// HttpClient instance used for making API requests.
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Base URL for the API, retrieved from the configuration.
        /// </summary>
        private readonly string _apiBaseUrl;

        private readonly IRemoteTelemetryService remoteTelemetryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoachagramAPIService"/> class.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> used for API requests.</param>
        /// <param name="configuration">The application configuration containing API settings.</param>
        /// <param name="remoteTelemetryService">The telemetry service for logging exceptions and traces.</param>
        /// <exception cref="InvalidOperationException">Thrown when the API base URL is missing in configuration.</exception>
        public RoachagramAPIService(HttpClient httpClient, IConfiguration configuration, IRemoteTelemetryService remoteTelemetryService)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["ApiBaseUrl"] ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing.");
            this.remoteTelemetryService = remoteTelemetryService;
        }

        /// <summary>
        /// Fetches anagrams for the given input string from the API.
        /// </summary>
        /// <param name="input">The input string for which anagrams are to be fetched.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the API response as a string.
        /// </returns>
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

                // Define a retry policy for transient HTTP errors.
                var retryPolicy = Policy
                    .Handle<HttpRequestException>()
                    .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

                var response = await retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(endpoint));

                response.EnsureSuccessStatusCode();
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
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the device UUID.
        /// </returns>
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
                // Log the exception to remote telemetry.
                await remoteTelemetryService.TrackExceptionAsync(ex);

                // If secure storage is not available, fallback to a temporary UUID.
                return Guid.NewGuid().ToString();
            }
        }
    }
}