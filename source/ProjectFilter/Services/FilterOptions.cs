using System;
using System.Collections.Generic;
using System.Linq;


namespace ProjectFilter.Services {

    public class FilterOptions {

        public FilterOptions(
            IEnumerable<Guid> projectsToLoad,
            IEnumerable<Guid> projectsToUnload,
            bool loadProjectDependencies
        ) {
            ProjectsToLoad = projectsToLoad.ToList();
            ProjectsToUnload = projectsToUnload.ToList();
            LoadProjectDependencies = loadProjectDependencies;
        }


        public IReadOnlyCollection<Guid> ProjectsToLoad { get; }


        public IReadOnlyCollection<Guid> ProjectsToUnload { get; }


        public bool LoadProjectDependencies { get; }

    }

}
