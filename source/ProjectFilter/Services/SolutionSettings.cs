using System.Collections.Generic;


namespace ProjectFilter.Services;


public class SolutionSettings {

    public SolutionSettings() {
        LoadProjectDependencies = true;
        UseRegularExpressions = false;
        ExpandLoadedProjects = true;
        HideSolutionFoldersWithoutLoadedProjects = null;
        Nodes = [];
    }


    public bool LoadProjectDependencies { get; set; }


    public bool UseRegularExpressions { get; set; }


    public bool ExpandLoadedProjects { get; set; }


    public bool? HideSolutionFoldersWithoutLoadedProjects { get; set; }


    public Dictionary<string, SolutionNodeSettings> Nodes { get; }

}
