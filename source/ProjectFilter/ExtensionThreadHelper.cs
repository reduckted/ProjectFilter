using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using System.Runtime.CompilerServices;


[assembly: InternalsVisibleTo("ProjectFilter.UnitTests")]


namespace ProjectFilter {

    public static class ExtensionThreadHelper {

        private static JoinableTaskFactory? _testingTaskFactory = null;


        public static JoinableTaskFactory JoinableTaskFactory {
            get { return _testingTaskFactory ?? ThreadHelper.JoinableTaskFactory; }
            internal set { _testingTaskFactory = value; }
        }

    }

}
