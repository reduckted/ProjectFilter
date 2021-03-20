using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;


namespace ProjectFilter.UI {

    public partial class FilterDialog : DialogWindow {

        public FilterDialog() {
            InitializeComponent();
        }


        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e) {
            ((FilterDialogViewModel)DataContext).OnLoadedAsync().FileAndForget(nameof(FilterDialog), faultDescription: nameof(OnLoaded));
        }

    }

}
