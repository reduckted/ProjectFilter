using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services {

    /// <summary>
    /// Wrapper around <see cref="IVsThreadedWaitDialogFactory"/> to work around threading problems in unit tests.
    /// </summary>
    public partial class WaitDialogFactory : IAsyncInitializable, IWaitDialogFactory {

#nullable disable
        private IVsThreadedWaitDialogFactory _waitDialogFactory;
#nullable restore


        public async Task InitializeAsync(IAsyncServiceProvider provider, CancellationToken cancellationToken) {
            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _waitDialogFactory = await provider.GetServiceAsync<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>();
        }


        public IWaitDialog Create(string caption, ThreadedWaitDialogProgressData progress) {
            return new Dialog(_waitDialogFactory.StartWaitDialog(caption, progress));
        }

    }

}
