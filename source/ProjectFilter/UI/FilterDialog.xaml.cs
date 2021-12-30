using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;


namespace ProjectFilter.UI;


public partial class FilterDialog : DialogWindow {

    private static ThemeResourceKey? _errorBorderBrushKey;


    /// <summary>
    /// This is the same as the resource key with the same name that is used in the internal "Find UI" control.
    /// </summary>
    public static ThemeResourceKey ErrorBorderBrushKey => _errorBorderBrushKey ??= new ThemeResourceKey(
        new Guid("4370282e-987e-4ac4-ad14-5ffed2ad1e14"),
        "ErrorBorder",
        ThemeResourceKeyType.BackgroundBrush
    );


    public FilterDialog() {
        InitializeComponent();
    }


    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e) {
        ((FilterDialogViewModel)DataContext).OnLoadedAsync().FileAndForget(nameof(FilterDialog), faultDescription: nameof(OnLoaded));
    }

}
