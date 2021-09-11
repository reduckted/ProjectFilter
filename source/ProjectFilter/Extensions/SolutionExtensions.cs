using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;


namespace ProjectFilter.Extensions {

    public static class SolutionExtensions {

        public static bool TryGetHierarchy(this IVsSolution4 solution, Guid identifier, out IVsHierarchy hierarchy) {
            if (solution is null) {
                throw new ArgumentNullException(nameof(solution));
            }

            ThreadHelper.ThrowIfNotOnUIThread();
            return ErrorHandler.Succeeded(((IVsSolution)solution).GetProjectOfGuid(identifier, out hierarchy));
        }


        public static bool TryGetIdentifier(this IVsSolution4 solution, IVsHierarchy hierarchy, out Guid identifier) {
            if (solution is null) {
                throw new ArgumentNullException(nameof(solution));
            }

            ThreadHelper.ThrowIfNotOnUIThread();
            return ErrorHandler.Succeeded(((IVsSolution)solution).GetGuidOfProject(hierarchy, out identifier));
        }


        public static string GetName(this IVsSolution4 solution, Guid identifier) {
            ThreadHelper.ThrowIfNotOnUIThread();

            TryGetHierarchy(solution, identifier, out IVsHierarchy hierarchy);

            if (HierarchyUtilities.TryGetHierarchyProperty(hierarchy, VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_Name, out string name)) {
                return name;
            }

            return "?";
        }

    }

}
