using System;
using System.Windows;

namespace VRN.Services;

public enum AppTheme { Dark, Light }

public class ThemeService
{
    private AppTheme _current = AppTheme.Dark;

    public AppTheme Current => _current;

    public void Apply(AppTheme theme)
    {
        _current = theme;
        var dicts = Application.Current.Resources.MergedDictionaries;
        dicts.Clear();

        string themeName = theme == AppTheme.Dark ? "DarkTheme" : "LightTheme";

        dicts.Add(new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/{themeName}.xaml")
        });
        dicts.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Themes/SharedStyles.xaml")
        });
    }

    public void Toggle() => Apply(_current == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
}
