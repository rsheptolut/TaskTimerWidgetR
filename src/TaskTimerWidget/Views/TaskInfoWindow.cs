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
    /// A standalone, centered window for showing task details. Unlike a ContentDialog,
    /// it is not constrained to the narrow docked widget, so its content is never clipped.
    /// </summary>
    public sealed class TaskInfoWindow : Window
    {
        public TaskInfoWindow(string title, string details)
        {
            const int width = 460;
            const int height = 340;

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

            var detailsBlock = new TextBlock
            {
                Text = details,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220))
            };
            var scroller = new ScrollViewer
            {
                Content = detailsBlock,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(scroller, 1);
            root.Children.Add(scroller);

            var closeButton = new Button
            {
                Content = "Close",
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 88
            };
            closeButton.Click += (_, _) => Close();
            Grid.SetRow(closeButton, 2);
            root.Children.Add(closeButton);

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

            CenterOnScreen(width, height);
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
