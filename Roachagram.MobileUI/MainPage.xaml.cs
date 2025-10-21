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
        const int maxInputCharacters = 25;
        private const int TEXT_EASE_IN_SECONDS = 2000;
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

            string inputText = InputEntry?.Text ?? string.Empty;

            await SetButtonBusyAsync(SubmitBtn, async () =>
            {
                try
                {
                    //turn off and reset the results.
                    RoachagramResponseView.IsVisible = false;
                    RoachagramResponseView.Source = string.Empty;
                    webviewBorder.BackgroundColor = Colors.Transparent;

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
                                              background-color: white;
                                              color: black;
                                            }}
                                            #typedText {{
                                              font-family: monospace;
                                              white-space: pre-wrap;
                                            }}
                                          </style>
                                        </head>
                                        <body>
                                          <div id=""typedText""></div>

                                          <script>
    
                                            const text = `{roachagramResponse}`;
                                            const speed = 10;
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

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        RoachagramResponseView.Source = new HtmlWebViewSource
                        {
                            Html = $"{roachagramResponse}"
                        };
                    });

                    if (InputEntry != null)
                    {
                        InputEntry.Text = string.Empty;
                        InputEntry.IsEnabled = true;
                        InputEntry.Placeholder = "Enter words or a name";
                    }

                    // turn off the loading spinner and fade in the webviewBorder
                    LoadingIndicator.IsRunning = false;
                    LoadingIndicator.IsVisible = false;
                    RoachagramResponseView.IsVisible = true;
                    webviewBorder.Opacity = 0;
                    webviewBorder.BackgroundColor = Colors.White;
                    webviewBorder.IsVisible = true;
                    await webviewBorder.FadeTo(1, TEXT_EASE_IN_SECONDS, Easing.CubicIn);
                }
                catch (Exception ex)
                {
                    // Track exception and provide fallback response
                    _remoteTelemetryService?.TrackExceptionAsync(ex, props);

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

                        if (InputEntry != null)
                        {
                            InputEntry.IsEnabled = true;
                            InputEntry.Placeholder = "Enter words or a name";
                        }

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