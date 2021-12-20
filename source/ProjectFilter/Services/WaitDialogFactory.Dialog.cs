using Microsoft.VisualStudio.Shell;
using System.Threading;


namespace ProjectFilter.Services;


partial class WaitDialogFactory {

    private class Dialog : IWaitDialog {

        private readonly ThreadedWaitDialogHelper.Session _session;


        public Dialog(ThreadedWaitDialogHelper.Session session) {
            _session = session;
        }


        public CancellationToken CancellationToken => _session.UserCancellationToken;


        public void ReportProgress(ThreadedWaitDialogProgressData progress) {
            _session.Progress.Report(progress);
        }


        public void Dispose() {
            _session.Dispose();
        }

    }

}
