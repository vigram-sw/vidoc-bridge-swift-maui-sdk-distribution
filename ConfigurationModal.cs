using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MyMauiApp.Controls
{
    /// <summary>
    /// A simple modal overlay control for MAUI applications.
    /// Displays a semi-transparent overlay with an activity indicator and message label.
    /// Useful for showing "processing" or "loading" states to the user.
    /// </summary>
    public class ConfigurationModal
    {
        private readonly ContentPage _parentPage; // The page on which the modal is displayed
        private readonly Grid _overlay;            // The semi-transparent overlay container
        private readonly Label _messageLabel;      // Label to display the message text
        private readonly ActivityIndicator _indicator; // Activity indicator (spinner)

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigurationModal"/> for a given parent page.
        /// Adds the overlay to the parent page's content.
        /// </summary>
        /// <param name="parentPage">The page on which the modal overlay should appear.</param>
        public ConfigurationModal(ContentPage parentPage)
        {
            _parentPage = parentPage;

            // Initialize the message label with default text and styling
            _messageLabel = new Label
            {
                Text = "Processing...",
                FontSize = 16,
                TextColor = Colors.Black,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            // Initialize the activity indicator (spinner)
            _indicator = new ActivityIndicator
            {
                Color = Colors.Black,
                IsRunning = true,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            // Create the overlay grid
            _overlay = new Grid
            {
                BackgroundColor = Colors.Black.MultiplyAlpha(0.5f), // Semi-transparent background
                IsVisible = false, // Hidden by default
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Children =
                {
                    // Create a white framed container for the indicator and message
                    new Frame
                    {
                        BackgroundColor = Colors.White,
                        CornerRadius = 12,
                        Padding = 20,
                        Margin = new Thickness(20, 0),
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Center,
                        Content = new StackLayout
                        {
                            Spacing = 10,
                            Children =
                            {
                                _indicator,
                                _messageLabel
                            }
                        }
                    }
                }
            };

            // Add the overlay to the parent page
            if (_parentPage.Content is Layout layout)
            {
                layout.Children.Add(_overlay);
            }
            else
            {
                // Wrap existing content in a grid if it's not a layout
                var grid = new Grid();
                grid.Children.Add(_parentPage.Content);
                grid.Children.Add(_overlay);
                _parentPage.Content = grid;
            }
        }

        /// <summary>
        /// Shows the modal overlay with a message.
        /// </summary>
        /// <param name="message">The message to display. Default is "Processing...".</param>
        public void Show(string message = "Processing...")
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _messageLabel.Text = message;
                _indicator.IsRunning = true;
                _overlay.IsVisible = true; // Make overlay visible
            });
        }

        /// <summary>
        /// Updates the message text displayed on the modal overlay.
        /// </summary>
        /// <param name="message">The new message text.</param>
        public void UpdateMessage(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _messageLabel.Text = message;
            });
        }

        /// <summary>
        /// Hides the modal overlay.
        /// </summary>
        public void Dismiss()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _overlay.IsVisible = false; // Hide overlay
            });
        }
    }
}
