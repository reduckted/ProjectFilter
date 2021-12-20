using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;


namespace ProjectFilter.Services {

    /// <summary>
    /// Wrapper around <see cref="IVsThreadedWaitDialogFactory"/> to work around threading problems in unit tests.
    /// </summary>
    public partial class WaitDialogFactory : IWaitDialogFactory {

        public async Task<IWaitDialog> CreateAsync(string title, ThreadedWaitDialogProgressData progress) {
            IVsThreadedWaitDialogFactory waitDialogFactory;


            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            waitDialogFactory = (IVsThreadedWaitDialogFactory)await VS.Services.GetThreadedWaitDialogAsync();

            return new Dialog(waitDialogFactory.StartWaitDialog(title, progress));
        }

    }

}
