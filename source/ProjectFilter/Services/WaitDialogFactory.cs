using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;


namespace ProjectFilter.Services {

    /// <summary>
    /// Wrapper around <see cref="IVsThreadedWaitDialogFactory"/> to work around threading problems in unit tests.
    /// </summary>
    public partial class WaitDialogFactory : IWaitDialogFactory {

        private readonly IAsyncServiceProvider _provider;


        public WaitDialogFactory(IAsyncServiceProvider provider) {
            _provider = provider;
        }


        public async Task<IWaitDialog> CreateAsync(string caption, ThreadedWaitDialogProgressData progress) {
            IVsThreadedWaitDialogFactory waitDialogFactory;


            await ExtensionThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            waitDialogFactory = await _provider.GetServiceAsync<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>();

            return new Dialog(waitDialogFactory.StartWaitDialog(caption, progress));
        }

    }

}
