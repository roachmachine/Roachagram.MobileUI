using Microsoft.Maui.Controls.Shapes;
using Roachagram.MobileUI.Helpers;
using Roachagram.MobileUI.Services;
using System.Text.RegularExpressions;

namespace Roachagram.MobileUI
{
    // MainPage class represents the main user interface page of the application.
    // It inherits from ContentPage, which is a base class for pages in .NET MAUI.
    public partial class MainPage : ContentPage
    {
        // Maximum number of characters allowed in the input Entry.
        const int maxInputCharacters = 50;
        private readonly IRoachagramAPIService? _roachagramAPIService;
        private readonly IRemoteTelemetryService? _remoteTelemetryService;
        private string roachagramResponse = string.Empty;

        // Constructor for MainPage. Initializes the components of the page.
        // TelemetryClient is injected here.
        public MainPage(IRoachagramAPIService roachagramAPIService, IRemoteTelemetryService remoteTelemetryService)
        {

            try
            {
                InitializeComponent();
                _roachagramAPIService = roachagramAPIService;
                _remoteTelemetryService = remoteTelemetryService;
                SubmitBtn.IsEnabled = false; // Disable the button initially
                RoachagramResponseView.IsVisible = false; //no results yet
            }
            catch (Exception ex)
            {
                _remoteTelemetryService?.TrackExceptionAsync(ex);
                throw;
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
            //Use these proprties for logging to Application Insights
            var props = new Dictionary<string, string>
            {
                ["Page"] = "MainPage",
                ["Handler"] = nameof(OnSubmit),
                ["AppVersion"] = AppInfo.VersionString,
                ["DeviceModel"] = DeviceInfo.Model,
                ["OS"] = $"{DeviceInfo.Platform} {DeviceInfo.VersionString}",
            };

            var inputTextSnapshot = InputEntry?.Text ?? string.Empty;

            await SetButtonBusyAsync(SubmitBtn, async () =>
            {

                try
                {
                    //turn off and reset the results.
                    RoachagramResponseView.IsVisible = false;
                    RoachagramResponseView.Source = string.Empty;
                    webviewBorder.BackgroundColor = Colors.Transparent;

                    SemanticScreenReader.Announce(SubmitBtn.Text);

                    string inputText = InputEntry?.Text ?? string.Empty;

                    if (_roachagramAPIService != null)
                    {
                        roachagramResponse = await _roachagramAPIService.GetAnagramsAsync(inputText);
                    }

                    //format the response to orthodox HTML
                    roachagramResponse = TextFormatHelper.DecodeApiString(roachagramResponse);
                    roachagramResponse = TextFormatHelper.ReplaceMarkdownBoldWithHtmlBold(roachagramResponse);
                    roachagramResponse = TextFormatHelper.BoldSectionAfterHashes(roachagramResponse);
                    roachagramResponse = $@"
                                            <html>
                                              <head>
                                                <link href=""https://fonts.googleapis.com/css?family=Open+Sans:400,700&display=swap"" rel=""stylesheet"">
                                                <style>
                                                  body {{
                                                    color: black;
                                                    background-color: white;
                                                    font-family: 'Open Sans', Arial, sans-serif;
                                                    margin: 0;
                                                    padding: 10px;
                                                    border: 0;
                                                  }}
                                                </style>
                                              </head>
                                              <body>
                                                <p>{TextFormatHelper.CapitalizeWordsInQuotes(roachagramResponse)}</p>
                                              </body>
                                            </html>
                                            ";

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        RoachagramResponseView.Source = new HtmlWebViewSource
                        {
                            Html = $"{roachagramResponse}"
                        };
                    });

                    if (InputEntry != null)
                    {
                        // Clear the Entry after submission
                        InputEntry.Text = string.Empty; 
                    }

                    // turn off the loading spinner and fade in the webviewBorder
                    LoadingIndicator.IsRunning = false;
                    LoadingIndicator.IsVisible = false;
                    RoachagramResponseView.IsVisible = true;
                    webviewBorder.Opacity = 0; 
                    webviewBorder.BackgroundColor = Colors.White;
                    webviewBorder.IsVisible = true;
                    await webviewBorder.FadeTo(1, 5000, Easing.CubicIn);
                }
                catch (Exception ex)
                {
                    // Track exception and provide fallback response
                    _remoteTelemetryService?.TrackTraceAsync("Yo. We are testing the trace", props);
                    
                    roachagramResponse = "An error occurred while fetching anagrams. Please try again.";

                    // Ensure UI shows the error message
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        RoachagramResponseView.Source = new HtmlWebViewSource
                        {
                            Html = $@"
                                <html><body><p>{TextFormatHelper.CapitalizeWordsInQuotes(roachagramResponse)}</p></body></html>"
                        };
                        RoachagramResponseView.IsVisible = true;
                        LoadingIndicator.IsRunning = false;
                        LoadingIndicator.IsVisible = false;
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
                button.Text = "...";
                button.BackgroundColor = Colors.DarkGray;

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