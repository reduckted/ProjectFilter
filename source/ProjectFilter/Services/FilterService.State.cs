using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectFilter.Services {

    partial class FilterService {

        private class State {

            private readonly IWaitDialog _waitDialog;
            private readonly HashSet<Guid> _projectsToLoad;
            private readonly HashSet<Guid> _loadedProjects;
            private readonly HashSet<Guid> _projectsToUnload;
            private readonly HashSet<Guid> _unloadedProjects;
            private ThreadedWaitDialogProgressData _currentProgress;

            public State(IWaitDialog waitDialog, ThreadedWaitDialogProgressData initialProgress, IVsSolution4 solution) {
                _waitDialog = waitDialog;
                _currentProgress = initialProgress;
                Solution = solution;

                _projectsToLoad = new HashSet<Guid>();
                _loadedProjects = new HashSet<Guid>();

                _projectsToUnload = new HashSet<Guid>();
                _unloadedProjects = new HashSet<Guid>();

                ProjectsVisitedWhileLoading = new HashSet<Guid>();
                RequiresProjectDependencyCalculation = true;
            }


            public IVsSolution4 Solution { get; }


            public void AddProjectToLoad(Guid project) {
                _projectsToLoad.Add(project);
                UpdateProgressSteps();
            }


            public void AddProjectsToLoad(IEnumerable<Guid> projects) {
                _projectsToLoad.UnionWith(projects);
                UpdateProgressSteps();
            }


            public void OnProjectLoaded(Guid identifier) {
                _loadedProjects.Add(identifier);
                UpdateProgressSteps();
            }


            public void AddProjectsToUnload(IEnumerable<Guid> projects) {
                _projectsToUnload.UnionWith(projects);
                UpdateProgressSteps();
            }


            public void OnProjectUnloaded(Guid identifier) {
                _unloadedProjects.Add(identifier);
                UpdateProgressSteps();
            }


            public HashSet<Guid> ProjectsVisitedWhileLoading { get; }


            public bool RequiresProjectDependencyCalculation { get; set; }


            public bool IsCancellationRequested => _waitDialog.CancellationToken.IsCancellationRequested;


            public void SetProgressText(string text) {
                Report(new ThreadedWaitDialogProgressData(
                     _currentProgress.WaitMessage,
                     text,
                     _currentProgress.StatusBarText,
                     _currentProgress.IsCancelable,
                     _currentProgress.CurrentStep,
                     _currentProgress.TotalSteps
                 ));
            }


            private void UpdateProgressSteps() {
                int current;
                int total;


                total = _projectsToLoad.Count + _projectsToUnload.Count;
                current = _loadedProjects.Count + _unloadedProjects.Count;

                Report(new ThreadedWaitDialogProgressData(
                     _currentProgress.WaitMessage,
                     _currentProgress.ProgressText,
                     _currentProgress.StatusBarText,
                     _currentProgress.IsCancelable,
                     current,
                     total
                 ));
            }


            public IEnumerable<Guid> GetLoadedProjects() {
                return _loadedProjects.ToList();
            }


            private void Report(ThreadedWaitDialogProgressData progress) {
                _waitDialog.ReportProgress(progress);
                _currentProgress = progress;
            }

        }

    }

}
