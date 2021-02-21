using Microsoft.VisualStudio.Shell;
using System;
using System.Threading;


namespace ProjectFilter.Services {

    public interface IWaitDialog : IDisposable {

        CancellationToken CancellationToken { get; }


        void ReportProgress(ThreadedWaitDialogProgressData progress);

    }

}
