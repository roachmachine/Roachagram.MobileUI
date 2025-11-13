namespace Roachagram.MobileUI.Services
{
    /// <summary>
    /// Provides a thin wrapper around the MAUI connectivity API to expose current
    /// network access and a connectivity changed event for use by the application.
    /// </summary>
    /// <remarks>
    /// This class delegates to <c>Connectivity.Current</c> from .NET MAUI and is intended
    /// to centralize connectivity checks and subscriptions so consumers can depend on
    /// the <c>IConnectivityService</c> abstraction.
    /// </remarks>
    public class ConnectivityService : IConnectivityService
    {
        /// <summary>
        /// Gets a value indicating whether the device currently has internet connectivity.
        /// </summary>
        /// <value>
        /// <see langword="true"/> when <c>Connectivity.Current.NetworkAccess</c> equals
        /// <c>NetworkAccess.Internet</c>; otherwise <see langword="false"/>.
        /// </value>
        public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        /// <summary>
        /// Gets the current <see cref="NetworkAccess"/> reported by the platform.
        /// </summary>
        /// <remarks>
        /// Consumers can use this property to inspect the precise access level (for example,
        /// <c>NetworkAccess.Local</c>, <c>NetworkAccess.ConstrainedInternet</c>, etc.).
        /// </remarks>
        public NetworkAccess CurrentAccess => Connectivity.Current.NetworkAccess;

        /// <summary>
        /// Occurs when the device's network connectivity changes.
        /// </summary>
        /// <remarks>
        /// Subscribing to this event attaches the handler to <c>Connectivity.Current.ConnectivityChanged</c>.
        /// Unsubscribing removes the handler from the underlying MAUI connectivity event.
        /// The event provides a <see cref="ConnectivityChangedEventArgs"/> instance that
        /// contains the previous and current network access information.
        /// </remarks>
        public event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged
        {
            add => Connectivity.Current.ConnectivityChanged += value;
            remove => Connectivity.Current.ConnectivityChanged -= value;
        }
    }
}