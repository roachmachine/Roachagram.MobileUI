using System;

namespace Roachagram.MobileUI.Models
{
    /// <summary>
    /// Represents a serialized form of an <see cref="Exception"/> suitable for telemetry transport or storage.
    /// Contains the exception type name, message, stack trace and origin information.
    /// </summary>
    public class SerializedException
    {
        /// <summary>
        /// Gets or sets the CLR type name of the exception (for example, "System.NullReferenceException").
        /// This is a fully-qualified type name useful for programmatic analysis.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the exception message that describes the error.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the stack trace information captured from the exception.
        /// May be null or empty if not available.
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Gets or sets the name of the application or object that caused the exception.
        /// This maps to <see cref="Exception.Source"/>.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the name of the method that threw the exception.
        /// This maps to <see cref="Exception.TargetSite"/> (method information serialized as a string).
        /// </summary>
        public string? TargetSite { get; set; }
    }

    /// <summary>
    /// Top-level Data Transfer Object for telemetry payloads sent to the telemetry endpoint.
    /// </summary>
    public class TelemetryDTO
    {
        /// <summary>
        /// Gets or sets the telemetry event type or category (for example, "Error", "Metric", "Trace").
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Gets or sets the short or friendly name of the exception type (for example, "NullReferenceException").
        /// Use this for display purposes where the fully-qualified <see cref="Type"/> is not required.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets a short, human-readable message associated with the telemetry event.
        /// For error events this typically mirrors the exception message; for traces or metrics it
        /// can contain a concise description or contextual detail. May be null or empty if no message is provided.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the serialized exception data associated with this telemetry event, if any.
        /// </summary>
        public SerializedException? SerializedException { get; set; }

        /// <summary>
        /// Gets or sets the additional contextual properties collected with this telemetry event.
        /// </summary>
        public required Dictionary<string, string> Properties { get; set; }
    }
}
