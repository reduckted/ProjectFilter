using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using ProjectFilter.Services;
using System;
using System.ComponentModel.Design;
using System.Threading;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Commands {

    public abstract class MenuCommandBase : IAsyncInitializable {

        private bool _running;


        protected MenuCommandBase(IAsyncServiceProvider provider) {
            Provider = provider;
        }


        public IAsyncServiceProvider Provider { get; }


        public async Task InitializeAsync(CancellationToken cancellationToken) {
            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            Initialize(await Provider.GetServiceAsync<IMenuCommandService, IMenuCommandService>());
        }


        protected abstract void Initialize(IMenuCommandService commandService);


        private protected void Execute(object sender, EventArgs e) {
            ExtensionThreadHelper.JoinableTaskFactory.RunAsync(async delegate {
                // Prevent the command from running if it's already running.
                // This can happen if the command is executed again while services
                // are being retrieved asynchronously from the previous execution.
                if (!_running) {
                    _running = true;

                    try {
                        await ExecuteAsync();

                    } catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex)) {
                        await (await Provider.GetServiceAsync<ILogger, ILogger>()).WriteLineAsync(ex.ToString());

                    } finally {
                        _running = false;
                    }
                }
            });
        }


        public abstract Task ExecuteAsync();

    }

}
