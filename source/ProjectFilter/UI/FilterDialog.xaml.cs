using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;


namespace ProjectFilter.UI;


public partial class FilterDialog : DialogWindow {

    private static readonly object? ToolBarStyleKey = typeof(SearchControl)
        .GetProperty("ToolBarStyleKey", BindingFlags.Public | BindingFlags.Static)?
        .GetValue(null);


    public FilterDialog() {
        InitializeComponent();
    }


    private void OnLoaded(object sender, RoutedEventArgs e) {
        OnLoadedAsync((FilterDialogViewModel)DataContext).FileAndForget(
            nameof(FilterDialog),
            faultDescription: nameof(OnLoaded)
        );
    }


    private async Task OnLoadedAsync(FilterDialogViewModel vm) {
        IVsWindowSearchHostFactory factory;
        IVsWindowSearchHost host;


        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        factory = await VS.GetRequiredServiceAsync<SVsWindowSearchHostFactory, IVsWindowSearchHostFactory>();
        host = factory.CreateWindowSearchHost(SearchContainer);

        await vm.OnLoadedAsync(host);

        if (host.GetType().GetProperty("SearchControl")?.GetValue(host) is SearchControl control) {
            if (ToolBarStyleKey is not null) {
                control.SetResourceReference(StyleProperty, ToolBarStyleKey);
            }
        }
    }

}
