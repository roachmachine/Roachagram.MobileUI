using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Roachagram.MobileUI.Models;

namespace Roachagram.MobileUI.Services
{
    /// <summary>
    /// Defines methods for sending telemetry (trace and exception) to a remote telemetry endpoint.
    /// </summary>
    public interface IRemoteTelemetryService
    {
        /// <summary>
        /// Sends a trace (informational) telemetry event to the remote telemetry service.
        /// </summary>
        /// <param name="name">The trace message or name describing the trace.</param>
        /// <param name="properties">Optional key/value properties to include with the trace. May be <c>null</c>.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send operation.</returns>
        Task TrackTraceAsync(string name, Dictionary<string, string>? properties = null);

        /// <summary>
        /// Sends exception telemetry to the remote telemetry service.
        /// </summary>
        /// <param name="message">The exception to record and send.</param>
        /// <param name="properties">Optional key/value properties to include with the exception telemetry. May be <c>null</c>.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send operation.</returns>
        Task TrackExceptionAsync(Exception message, Dictionary<string, string>? properties = null);
    }
}

namespace Roachagram.MobileUI.Services
{
    /// <summary>
    /// Implementation of <see cref="IRemoteTelemetryService"/> that posts telemetry payloads to a configured remote HTTP endpoint.
    /// </summary>
    /// <remarks>
    /// This service constructs a telemetry payload (<see cref="TelemetryDTO"/>) for traces and exceptions
    /// The endpoint is built from the configuration key <c>ApiBaseUrl</c> and the relative path <c>api/telemetry</c>.
    /// </remarks>
    public class RemoteTelemetryService(HttpClient http, IConfiguration config) : IRemoteTelemetryService
    {
        /// <summary>
        /// Fully-qualified telemetry endpoint URL (constructed from configuration <c>ApiBaseUrl</c>).
        /// </summary>
        private readonly string _endpoint = new Uri(new Uri(config["ApiBaseUrl"]!), "api/telemetry").ToString();

        /// <summary>
        /// Sends a trace telemetry event to the remote telemetry endpoint.
        /// </summary>
        /// <param name="message">A short, human-readable trace message.</param>
        /// <param name="properties">Optional contextual properties to include with the trace. May be <c>null</c>.</param>
        /// <returns>A <see cref="Task"/> that completes when the HTTP POST has been initiated.</returns>
        public async Task TrackTraceAsync(string message, Dictionary<string, string>? properties = null)
        {
            var dto = new Models.TelemetryDTO
            {
                Type = "trace",
                Name = "remote trace",
                Message = message,
                Properties = properties ?? []
            };

            var json = System.Text.Json.JsonSerializer.Serialize(dto);
            await http.PostAsJsonAsync(_endpoint, dto);

        }

        /// <summary>
        /// Sends an exception telemetry event to the remote telemetry endpoint.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> instance to serialize and send.</param>
        /// <param name="properties">Optional contextual properties to include with the exception telemetry. May be <c>null</c>.</param>
        /// <returns>A <see cref="Task"/> that completes when the HTTP POST has been initiated.</returns>
        public async Task TrackExceptionAsync(Exception exception, Dictionary<string, string>? properties = null)
        {
            //just the basics
            var serializedExceptionObj = new SerializedException
            {
                Type = exception.GetType().FullName,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                Source = exception.Source,
                TargetSite = exception.TargetSite?.ToString()
            };

            var dto = new TelemetryDTO
            {
                Type = "exception",
                Name = "remote exception",
                Message = exception.Message,
                Properties = properties ?? [],
                SerializedException = serializedExceptionObj
            };

            var json = System.Text.Json.JsonSerializer.Serialize(dto);
            await http.PostAsJsonAsync(_endpoint, dto);
        }
    }
}