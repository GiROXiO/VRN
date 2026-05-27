using System.Globalization;
using System.Threading;
using System.Windows;
using VRN.Services;

namespace VRN;

public partial class App : Application
{
    public static ThemeService ThemeService { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        // Auto-detect system language; fall back to EN
        var culture = CultureInfo.CurrentUICulture;
        bool isSpanish = culture.TwoLetterISOLanguageName == "es";
        var targetCulture = isSpanish
            ? new CultureInfo("es")
            : CultureInfo.InvariantCulture;

        Thread.CurrentThread.CurrentCulture = targetCulture;
        Thread.CurrentThread.CurrentUICulture = targetCulture;

        base.OnStartup(e);

        // Apply dark theme (already in XAML, but ensure ThemeService is in sync)
        ThemeService.Apply(AppTheme.Dark);
    }
}
