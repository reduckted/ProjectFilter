using Microsoft.VisualStudio.Shell;


namespace ProjectFilter.Services {

    public interface IWaitDialogFactory {

        IWaitDialog Create(string title, ThreadedWaitDialogProgressData progress);

    }
}
