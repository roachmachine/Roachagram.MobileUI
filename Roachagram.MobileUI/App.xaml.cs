namespace Roachagram.MobileUI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Ensure Current is not null before accessing UserAppTheme
            if (Current != null)
            {
                Current.UserAppTheme = AppTheme.Dark;
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}