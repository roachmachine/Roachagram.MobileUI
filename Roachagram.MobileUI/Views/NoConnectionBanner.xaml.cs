namespace Roachagram.MobileUI.Views
{
    public partial class NoConnectionBanner : ContentView
    {
        public NoConnectionBanner()
        {
            InitializeComponent();
        }

        // Optional: expose an event so pages can react when a manual refresh is requested
        public event EventHandler? RefreshRequested;

        internal void OnRefreshClicked(object? sender, EventArgs e)
        {
            // Raise an event so parent view/viewmodel can react if desired
            RefreshRequested?.Invoke(this, EventArgs.Empty);

            // Perform a quick connectivity check and hide the banner if internet is available
            var access = Connectivity.Current.NetworkAccess;
            if (access == NetworkAccess.Internet)
            {
                // Hide the banner (the IsVisible binding in XAML references the view itself)
                NoConnectionView.IsVisible = false;
            }
            else
            {
                // Keep visible; you could add a small feedback animation or Toast here
                // Ensure the refresh button is visible while disabling briefly to avoid rapid taps
                RefreshButton.IsEnabled = false;
                RefreshButton.BackgroundColor = Colors.Maroon;
                RefreshButton.Text = "Checking";

                Dispatcher.StartTimer(TimeSpan.FromSeconds(2), () =>
                {
                    RefreshButton.IsEnabled = true;
                    RefreshButton.BackgroundColor = Colors.Red;
                    RefreshButton.Text = "Retry";
                    return false;
                });
            }
        }
    }
}