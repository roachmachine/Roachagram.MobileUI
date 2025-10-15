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
        private string roachagramResponse = string.Empty;

        // Constructor for MainPage. Initializes the components of the page.
        public MainPage(IRoachagramAPIService roachagramAPIService)
        {
            InitializeComponent();
            _roachagramAPIService = roachagramAPIService;    
            SubmitBtn.IsEnabled = false; // Disable the button initially
            RoachagramResponseView.IsVisible = false;
        }

        // Event handler for the Counter button click event.
        // calls the roachagram api to get anagrams
        // Updated OnSubmit method to use the 'busy' mode
        private async void OnSubmit(object? sender, EventArgs e)
        {
            await SetButtonBusyAsync(SubmitBtn, async () =>
            {
                //turn off and reset the results.
                RoachagramResponseView.IsVisible = false;
                RoachagramResponseView.Source = string.Empty;
                webviewBorder.BackgroundColor = Colors.Transparent;

                SemanticScreenReader.Announce(SubmitBtn.Text);

                string inputText = InputEntry.Text;

                if (_roachagramAPIService != null)
                {
                    roachagramResponse = await _roachagramAPIService.GetAnagramsAsync(inputText);
                    //roachagramResponse = "Hey there! Roachagram here, ready to dive into the delightful jumble of \"amandaroach.\" Let's see what tasty word treats you've cooked up!<br><br>1. <b>Dharana Coma</b>  <br>   - *Dharana* is a cool word from Sanskrit, meaning \"concentration\" or \"holding steady\" in meditation.  <br>   - *Coma* is that deep sleep or unconscious state. So this combo sounds like a zen nap!<br><br>2. <b>Drachma Anoa</b>  <br>   - *Drachma* was an ancient Greek currency—classic and historic!  <br>   - *Anoa* is a small buffalo native to Indonesia. Who knew we could mix money and wildlife?<br><br>3. <b>Armada Nacho</b>  <br>   - *Armada* means a fleet of warships—sounds fierce!  <br>   - *Nacho* is everyone's favorite cheesy snack. Naval feast, anyone?<br><br>4. <b>Aha Cardamon</b>  <br>   - *Aha* is that delightful exclamation when you get something.  <br>   - *Cardamon* (usually spelled \"cardamom\") is a spice that’s aromatic and flavorful. Spicy realization!<br><br>5. <b>Cardamon Aah</b>  <br>   - Same spice, different exclamation. \"Aah\" is a sigh of relief or pleasure.<br><br>6. <b>Mad Arcana Ho</b>  <br>   - *Arcana* refers to secrets or mysteries, often in tarot cards.  <br>   - \"Mad arcana\" sounds like crazy secrets! \"Ho\" is like a call or cheer.<br><br>7. <b>Carom Had Ana</b>  <br>   - *Carom* is a type of billiards shot where the cue ball hits two balls in succession.  <br>   - \"Had Ana\" could be a story snippet.<br><br>8. <b>Cha Mara Dona</b>  <br>   - This one’s a bit playful and less direct.  <br>   - *Cha* is a casual word for tea in some places.  <br>   - *Mara* and *Dona* are names or could hint at places.<br><br>9. <b>Ado Carman Ah</b>  <br>   - *Ado* means fuss or trouble.  <br>   - *Carman* is a surname or could mean a driver of a horse-drawn carriage in old times.  <br>   - \"Ah\" again, a sigh or realization.<br><br>10. <b>Anoa Mad Char</b>  <br>    - *Anoa* again, the buffalo.  <br>    - *Mad char* could imply crazy burnt stuff (char), or a fish called char. Wild!<br><br><b>Summary:</b>  <br>My faves have to be <b>Dharana Coma</b> for its zen vibes and exotic flair, and <b>Drachma Anoa</b> for mixing ancient currency with rare wildlife—talk about a worldly combo! Also, <b>Armada Nacho</b> just makes me hungry while imagining a fleet of cheesy ships sailing the snack seas. Keep those anagrams coming! Roachagram out! \U0001fab3✨";
                }
                else
                {
                    roachagramResponse = "API service is not available.";
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

                InputEntry.Text = string.Empty; // Clear the Entry after submission


                

                // Fade in the webviewBorder
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                RoachagramResponseView.IsVisible = true;             
                webviewBorder.Opacity = 0; // Ensure opacity is 0 before fade-in
                webviewBorder.BackgroundColor = Colors.White;
                webviewBorder.IsVisible = true; // Ensure the border is visible before fading
                await webviewBorder.FadeTo(1, 5000);
            });
        }

        // Event handler for the text changed event of an Entry control.
        // Ensures that the input text does not exceed the maximum allowed length.
        private void InputEntry_TextChanged(object sender, TextChangedEventArgs e)
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

        // Helper method to set the button in 'busy' mode with a spinning indicator
        private async Task SetButtonBusyAsync(Button button, Func<Task> action)
        {
            string originalText = button.Text;
            var originalButtonColor = button.BackgroundColor;
            button.IsEnabled = false;
            button.Text = "...";
            button.BackgroundColor = Colors.DarkGray;



            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            // Add the ActivityIndicator to the layout temporarily
            //if (button.Parent is Layout layout)
            //{
            //    layout.Children.Add(activityIndicator);
            //}

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

        private async void RoachagramResponseView_Navigated(object sender, WebNavigatedEventArgs e)
        {
            if (sender is WebView webView)
            {
                try
                {
                    //Use JavaScript to get the height of the content inside the WebView
                    string result = await webView.EvaluateJavaScriptAsync("document.body.scrollHeight");

                    if (double.TryParse(result, out double contentHeight))
                    {
                        // Set the WebView height dynamically based on the content height
                        webView.HeightRequest = contentHeight;

                        //Update the clip rectangle height
                        if (webView.Clip is RoundRectangleGeometry roundRect)
                        {
                            // Keep the same X, Y, Width, but update Height
                            var rect = roundRect.Rect;
                            roundRect.Rect = new Rect(rect.X, rect.Y, rect.Width, contentHeight + 200);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle any errors that might occur during JavaScript evaluation
                    Console.WriteLine($"Error adjusting WebView height: {ex.Message}");
                }
            }
        }
    }
}