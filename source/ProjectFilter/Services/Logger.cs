using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services {

    public class Logger : IAsyncInitializable, ILogger {

#nullable disable
        private IVsOutputWindow _output;
#nullable restore


        private IVsOutputWindowPane? _pane;


        public async Task InitializeAsync(IAsyncServiceProvider provider, CancellationToken cancellationToken) {
            _output = await provider.GetServiceAsync<SVsOutputWindow, IVsOutputWindow>();
        }


        public void WriteLine(string message) {
            ThreadHelper.ThrowIfNotOnUIThread();

            try {
                if (_pane == null) {
                    CreatePane();
                }

                if (_pane != null) {
                    _pane.OutputStringThreadSafe($"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}");
                }

            } catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex)) {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }


        private void CreatePane() {
            Guid identifier;


            ThreadHelper.ThrowIfNotOnUIThread();

            identifier = Guid.NewGuid();

            if (ErrorHandler.Succeeded(_output.CreatePane(ref identifier, Vsix.Name, 1, 1))) {
                _output.GetPane(ref identifier, out _pane);
            }
        }

    }

}
