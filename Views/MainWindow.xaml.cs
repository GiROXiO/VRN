using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VRN.ViewModels;

namespace VRN.Views;

public partial class MainWindow : Window
{
    private MainViewModel _vm = null!;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel(App.ThemeService);
        DataContext = _vm;

        // Subscribe to progress changes to animate the progress bar
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ProgressPercent))
                AnimateProgressBar(_vm.ProgressPercent);
        };
    }

    // ── Custom title bar ──────────────────────────────────────────────────────
    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            ToggleMaximize();
        else
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
        => ToggleMaximize();

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();

    private void ToggleMaximize()
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    // ── Progress bar width animation ──────────────────────────────────────────
    private void AnimateProgressBar(int percent)
    {
        // Find the fill border inside the terminal panel
        var fill = FindProgressFill();
        if (fill == null) return;

        // Get the parent track width
        if (fill.Parent is not Grid trackGrid) return;
        double trackWidth = trackGrid.ActualWidth;
        if (trackWidth <= 0) return;

        double targetWidth = trackWidth * percent / 100.0;

        var anim = new DoubleAnimation
        {
            To             = targetWidth,
            Duration       = TimeSpan.FromMilliseconds(280),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        fill.BeginAnimation(FrameworkElement.WidthProperty, anim);
    }

    private Border? FindProgressFill()
    {
        // Walk the visual tree to find the named fill border
        return FindChild<Border>(this, "ProgressFill");
    }

    private static T? FindChild<T>(DependencyObject parent, string childName) where T : FrameworkElement
    {
        if (parent == null) return null;
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T fe && fe.Name == childName)
                return fe;
            var result = FindChild<T>(child, childName);
            if (result != null) return result;
        }
        return null;
    }
}
