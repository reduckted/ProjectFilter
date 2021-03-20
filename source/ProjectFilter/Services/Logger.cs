using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services {

    public class Logger : ILogger {

        private readonly IAsyncServiceProvider _provider;
        private IVsOutputWindow? _output;
        private IVsOutputWindowPane? _pane;


        public Logger(IAsyncServiceProvider provider) {
            _provider = provider;
        }


        public async Task WriteLineAsync(string message) {
            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try {
                if (_output == null) {
                    _output = await _provider.GetServiceAsync<SVsOutputWindow, IVsOutputWindow>();
                }

                if (_pane == null) {
                    Guid identifier;


                    identifier = Guid.NewGuid();

                    if (ErrorHandler.Succeeded(_output.CreatePane(ref identifier, Vsix.Name, 1, 1))) {
                        _output.GetPane(ref identifier, out _pane);
                    }
                }

                if (_pane != null) {
                    _pane.OutputStringThreadSafe($"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}");
                }

            } catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex)) {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

    }

}
