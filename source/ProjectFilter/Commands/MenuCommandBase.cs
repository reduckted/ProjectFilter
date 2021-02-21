using Microsoft.VisualStudio.Shell;
using ProjectFilter.Services;
using System.ComponentModel.Design;
using System.Threading;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Commands {

    public abstract class MenuCommandBase : IAsyncInitializable {

        public async Task InitializeAsync(IAsyncServiceProvider provider, CancellationToken cancellationToken) {
            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await InitializeAsync(
                provider,
                await provider.GetServiceAsync<IMenuCommandService, IMenuCommandService>(),
                cancellationToken
            );
        }


        protected abstract Task InitializeAsync(
            IAsyncServiceProvider provider,
            IMenuCommandService commandService,
            CancellationToken cancellationToken
        );

    }

}
