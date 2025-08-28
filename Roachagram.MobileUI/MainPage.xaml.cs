using System.Text.RegularExpressions;
using Roachagram.MobileUI.Services;

namespace Roachagram.MobileUI
{
    // MainPage class represents the main user interface page of the application.
    // It inherits from ContentPage, which is a base class for pages in .NET MAUI.
    public partial class MainPage : ContentPage
    {
        // Maximum number of characters allowed in the input Entry.
        const int maxInputCharacters = 50;
        private readonly IRoachagramAPIService _roachagramAPIService;

        // Constructor for MainPage. Initializes the components of the page.
        public MainPage(IRoachagramAPIService roachagramAPIService)
        {
            _roachagramAPIService = roachagramAPIService;
            InitializeComponent();
            SubmitBtn.IsEnabled = false; // Disable the button initially
        }

        // Event handler for the Counter button click event.
        // Increments the counter and updates the button text accordingly.
        private async void OnSubmit(object? sender, EventArgs e)
        {
            // Announces the updated button text for accessibility purposes.
            SemanticScreenReader.Announce(SubmitBtn.Text);

            //get the text from the textbox
            string inputText = InputEntry.Text;

            //Take the text and call the api to get the anagrams and the fun description
            await _roachagramAPIService.GetAnagramsAsync(inputText);

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
    }
}