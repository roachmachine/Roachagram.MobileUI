using System.Text.RegularExpressions;
using Roachagram.MobileUI.Helpers;
using Roachagram.MobileUI.Services;

namespace Roachagram.MobileUI
{
    // MainPage class represents the main user interface page of the application.
    // It inherits from ContentPage, which is a base class for pages in .NET MAUI.
    public partial class MainPage : ContentPage
    {
        // Maximum number of characters allowed in the input Entry.
        const int maxInputCharacters = 15;
        private const int TEXT_EASE_IN_MILLISECONDS = 1000;
        private readonly IRoachagramAPIService? _roachagramAPIService;
        private readonly IRemoteTelemetryService? _remoteTelemetryService;
        private readonly IConnectivityService? _connectivityService;
        private string roachagramResponse = string.Empty;

        // Constructor for MainPage. Initializes the components of the page.
        // TelemetryClient is injected here.
        public MainPage(IRoachagramAPIService roachagramAPIService, IRemoteTelemetryService remoteTelemetryService, IConnectivityService connectivityService)
        {
            try
            {
                InitializeComponent();
                _roachagramAPIService = roachagramAPIService;
                _remoteTelemetryService = remoteTelemetryService;
                _connectivityService = connectivityService;

                // Subscribe to connectivity changes
                if (_connectivityService != null)
                {
                    _connectivityService.ConnectivityChanged += OnConnectivityChanged;
                }

                SubmitBtn.IsEnabled = false; // Disable the button initially
                RoachagramResponseView.IsVisible = false; //no results yet

                // Check initial connectivity state
                UpdateConnectionStatus();

            }
            catch (Exception ex)
            {
                _remoteTelemetryService?.TrackExceptionAsync(ex);
                throw;
            }
        }

        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateConnectionStatus();
        });
            }
            catch (Exception ex)
            {
                _remoteTelemetryService?.TrackExceptionAsync(ex);
            }
        }

        private void UpdateConnectionStatus()
        {
            try
            {
                bool isConnected = _connectivityService?.IsConnected ?? true;

                ConnectionBanner.IsVisible = !isConnected;

                // Optionally disable the submit button when offline
                SubmitBtn.IsEnabled = isConnected && !string.IsNullOrWhiteSpace(InputEntry?.Text);

                // Announce to screen reader
                if (!isConnected)
                {
                    SemanticScreenReader.Announce("Internet connection lost");
                }
            }
            catch (Exception ex)
            {
                _remoteTelemetryService?.TrackExceptionAsync(ex);
            }
        }

        protected override void OnDisappearing()
        {
            try
            {
                base.OnDisappearing();

                // Unsubscribe from connectivity changes
                if (_connectivityService != null)
                {
                    _connectivityService.ConnectivityChanged -= OnConnectivityChanged;
                }
            }
            catch (Exception ex)
            {
                _remoteTelemetryService?.TrackExceptionAsync(ex);
            }
        }

        /// <summary>
        /// Handles the Submit button activation. Executes the submission workflow:
        /// - captures the current input,
        /// - sets the button into a busy state via <see cref="SetButtonBusyAsync"/>,
        /// - calls the injected <see cref="IRoachagramAPIService"/> to fetch anagrams,
        /// - formats the API response with <see cref="TextFormatHelper"/>,
        /// - updates the UI (web view, loading indicator and animations) on the main thread,
        /// </summary>
        /// <param name="sender">The event sender (typically the submit <see cref="Button"/>).</param>
        /// <param name="e">Event arguments associated with the submit action.</param>
        /// <remarks>
        /// This method is asynchronous and returns void because it is an event handler.
        /// UI updates are dispatched to the main thread using <see cref="MainThread.BeginInvokeOnMainThread"/>.
        /// </remarks>
        private async void OnSubmit(object? sender, EventArgs e)
        {
            // Check connectivity before making API call
            if (_connectivityService?.IsConnected == false)
            {
                await DisplayAlert("No Connection",
                                 "Please check your internet connection and try again.",
                                 "OK");
                return;
            }

            //Use these properties for logging to Application Insights
            var props = new Dictionary<string, string>
            {
                ["Page"] = "MainPage",
                ["Handler"] = nameof(OnSubmit),
                ["AppVersion"] = AppInfo.VersionString,
                ["DeviceModel"] = DeviceInfo.Model,
                ["OS"] = $"{DeviceInfo.Platform} {DeviceInfo.VersionString}",
            };

            string inputText = InputEntry?.Text ?? string.Empty;

            await SetButtonBusyAsync(SubmitBtn, async () =>
            {
                try
                {
                    //turn off and reset the results.
                    RoachagramResponseView.IsVisible = false;
                    RoachagramResponseView.Source = string.Empty;

                    if (InputEntry != null)
                    {
                        InputEntry.Text = string.Empty;
                        InputEntry.IsEnabled = false;
                        InputEntry.Placeholder = "Anagramming...";
                    }

                    SemanticScreenReader.Announce(SubmitBtn.Text);
                    

                    if (_roachagramAPIService != null)
                    {
                        roachagramResponse = await _roachagramAPIService.GetAnagramsAsync(inputText);
                    }

                    //format the response to orthodox HTML
                    roachagramResponse = TextFormatHelper.DecodeApiString(roachagramResponse);
                    roachagramResponse = TextFormatHelper.ReplaceMarkdownBoldWithHtmlBold(roachagramResponse);
                    roachagramResponse = TextFormatHelper.BoldSectionAfterHashes(roachagramResponse);
                    roachagramResponse = TextFormatHelper.GetOriginalTextInput(inputText, roachagramResponse);
                    roachagramResponse = TextFormatHelper.RemoveSingleQuotes(roachagramResponse);


                    //set the html
                    roachagramResponse = $@"
                                        <!DOCTYPE html>
                                        <html>
                                        <head>
                                          <style>
                                            body {{
                                              padding: 20px;
                                              background-color: #f8e1b6;
                                              color: #478e99;
                                            }}
                                            #typedText {{
                                              font-family: monospace;
                                              white-space: pre-wrap;
                                             display: block;
                                             height: auto;
                                            }}
                                          </style>
                                        </head>
                                        <body>
                                          <div style=""padding-bottom: 20px;"" id=""typedText""></div>

                                          <script>
    
                                            const text = `{roachagramResponse}`;
                                            const speed = 15;
                                            let i = 0;
                                            let current = """";

                                            function typeWriter() {{
                                              if (i < text.length) {{
                                                current += text[i];
                                                document.getElementById(""typedText"").innerHTML = current;
                                                i++;
                                                setTimeout(typeWriter, speed);
                                              }}
                                            }}

                                            typeWriter();
                                          </script>
                                        </body>
                                        </html>
                                        ";

            // Update UI and perform fade-in animation on main thread
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                RoachagramResponseView.Source = new HtmlWebViewSource
                {
                    Html = $"{roachagramResponse}"
                };

                // Re-enable input and restore placeholder before fade so the UI is responsive.
                if (InputEntry != null)
                {
                    InputEntry.Text = string.Empty;
                    InputEntry.IsEnabled = true;
                    InputEntry.Placeholder = "Enter words or a name";
                }

                // Turn off the loading spinner
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;

                // Prepare and animate the web view opacity from 0 -> 1 with easing
                RoachagramResponseView.Opacity = 0;
                RoachagramResponseView.IsVisible = true;

                // FadeTo takes duration in milliseconds (uint) and an Easing. Use the existing TEXT_EASE_IN_SECONDS.
                await RoachagramResponseView.FadeTo(1, (uint)TEXT_EASE_IN_MILLISECONDS, Easing.CubicOut);
            });

            // No additional UI changes needed here because they were handled on the main thread above.
        }
        catch (Exception ex)
        {
            // Track exception and provide fallback response
            _remoteTelemetryService?.TrackExceptionAsync(ex, props);

            roachagramResponse = "An error occurred while fetching anagrams. Please try again.";

            // Ensure UI shows the error message and animate it the same way
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                RoachagramResponseView.Source = new HtmlWebViewSource
                {
                    Html = $@"
                        <html>
                            <head>
                                <style>
                                body {{
                                    background-color: #f8e1b6;
                                    color: #478e99;
                                    font-family: monospace;
                                    white-space: pre-wrap;
                                    display: block;
                                    text-align:center;
                                }}
                                </style>
                            </head>
                            <body>
                                <p><span style=""color:red;"">💥&nbsp;</span>{TextFormatHelper.CapitalizeWordsInQuotes(roachagramResponse)}</p>
                            </body>
                        </html>"
                };

                if (InputEntry != null)
                {
                    InputEntry.IsEnabled = true;
                    InputEntry.Placeholder = "Enter words or a name";
                }

                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;

                RoachagramResponseView.Opacity = 0;
                RoachagramResponseView.IsVisible = true;

                await RoachagramResponseView.FadeTo(1, (uint)TEXT_EASE_IN_MILLISECONDS, Easing.CubicOut);
            });
        }
    });
        }

        // Event handler for the text changed event of an Entry control.
        // Ensures that the input text does not exceed the maximum allowed length.
        private void InputEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Allow only English letters and spaces
                if (sender is Entry entry)
                {
                    string newText = Regex.Replace(e.NewTextValue, @"[^a-zA-Z\s]", string.Empty);
                    if (entry.Text != newText)
                    {
                        entry.Text = newText;
                    }
                }

                if (e.NewTextValue.Length > maxInputCharacters)
                {
                    // Truncate the text to the maximum allowed length.
                    InputEntry.Text = e.NewTextValue[..maxInputCharacters];
                }

                // Enable the button only if the Entry has a non-empty value
                SubmitBtn.IsEnabled = !string.IsNullOrWhiteSpace(InputEntry.Text);
            }
            catch (Exception ex)
            {
                // Track the exception for telemetry
                _remoteTelemetryService?.TrackExceptionAsync(ex);
            }
        }

        // Helper method to set the button in 'busy' mode with a spinning indicator
        private async Task SetButtonBusyAsync(Button button, Func<Task> action)
        {
            try
            {
                string originalText = button.Text;
                var originalButtonColor = button.BackgroundColor;
                button.IsEnabled = false;
                // Use a rocket emoji as the busy indicator
                button.Text = "🚀";
                button.Opacity = 0.7;

                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;

                try
                {
                    await action();
                }
                finally
                {
                    // Restore the button state
                    button.IsEnabled = true;
                    button.Text = originalText;
                    button.BackgroundColor = originalButtonColor;
                    button.Opacity = 1.0;
                }
            }
            catch (Exception ex)
            {
                // Track the exception for telemetry
                _remoteTelemetryService?.TrackExceptionAsync(ex);
            }
        }
    }
}