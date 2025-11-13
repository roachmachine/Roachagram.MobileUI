namespace Roachagram.MobileUI.Services
{
    public interface IConnectivityService
    {
        bool IsConnected { get; }
        event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;
        NetworkAccess CurrentAccess { get; }
    }
}