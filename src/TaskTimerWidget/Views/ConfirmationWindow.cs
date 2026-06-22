using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using Windows.Graphics;
using Windows.UI;

namespace TaskTimerWidget.Views
{
    /// <summary>
    /// Result of a <see cref="ConfirmationWindow"/> prompt, mirroring the three
    /// button slots of a ContentDialog.
    /// </summary>
    public enum ConfirmationResult
    {
        None,
        Primary,
        Secondary
    }

    /// <summary>
    /// A standalone, centered confirmation prompt. Unlike a ContentDialog, it is not
    /// constrained to the narrow docked widget, so its content and buttons are never
    /// clipped. Supports up to two action buttons plus a cancel/close button.
    /// </summary>
    public sealed class ConfirmationWindow : Window
    {
        private readonly TaskCompletionSource<ConfirmationResult> _completion = new();
        private bool _resultSet;

        private ConfirmationWindow(
            string title,
            string message,
            string primaryText,
            string? secondaryText,
            string closeText)
        {
            const int width = 460;

            var root = new Grid
            {
                Padding = new Thickness(20),
                RowSpacing = 16,
                Background = new SolidColorBrush(Color.FromArgb(255, 32, 32, 32))
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Colors.White)
            };
            Grid.SetRow(titleBlock, 0);
            root.Children.Add(titleBlock);

            var messageBlock = new TextBlock
            {
                Text = message,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220))
            };
            var scroller = new ScrollViewer
            {
                Content = messageBlock,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(scroller, 1);
            root.Children.Add(scroller);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8
            };

            var primaryButton = new Button
            {
                Content = primaryText,
                MinWidth = 88
            };
            primaryButton.Click += (_, _) => Complete(ConfirmationResult.Primary);
            buttonPanel.Children.Add(primaryButton);

            if (!string.IsNullOrEmpty(secondaryText))
            {
                var secondaryButton = new Button
                {
                    Content = secondaryText,
                    MinWidth = 88
                };
                secondaryButton.Click += (_, _) => Complete(ConfirmationResult.Secondary);
                buttonPanel.Children.Add(secondaryButton);
            }

            var closeButton = new Button
            {
                Content = closeText,
                MinWidth = 88
            };
            closeButton.Click += (_, _) => Complete(ConfirmationResult.None);
            buttonPanel.Children.Add(closeButton);

            Grid.SetRow(buttonPanel, 2);
            root.Children.Add(buttonPanel);

            Content = root;
            Title = title;

            var presenter = AppWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsAlwaysOnTop = true;
                presenter.IsResizable = true;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
            }

            Closed += (_, _) => Complete(ConfirmationResult.None);

            CenterOnScreen(width, EstimateHeight(message, secondaryText));
        }

        /// <summary>
        /// Shows a centered confirmation prompt and completes when the user chooses an option.
        /// </summary>
        public static Task<ConfirmationResult> ShowAsync(
            string title,
            string message,
            string primaryText,
            string? secondaryText,
            string closeText)
        {
            var window = new ConfirmationWindow(title, message, primaryText, secondaryText, closeText);
            window.Activate();
            return window._completion.Task;
        }

        private void Complete(ConfirmationResult result)
        {
            if (_resultSet)
            {
                return;
            }

            _resultSet = true;
            _completion.TrySetResult(result);
            Close();
        }

        private static int EstimateHeight(string message, string? secondaryText)
        {
            // Rough line-count based sizing so longer messages aren't cramped.
            var approxLines = (message.Length / 50) + message.Split('\n').Length + 1;
            var contentHeight = 120 + (approxLines * 20);
            return System.Math.Clamp(contentHeight, 200, 480);
        }

        private void CenterOnScreen(int width, int height)
        {
            AppWindow.Resize(new SizeInt32(width, height));

            var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            if (area != null)
            {
                var centerX = area.WorkArea.X + (area.WorkArea.Width - width) / 2;
                var centerY = area.WorkArea.Y + (area.WorkArea.Height - height) / 2;
                AppWindow.Move(new PointInt32(centerX, centerY));
            }
        }
    }
}
