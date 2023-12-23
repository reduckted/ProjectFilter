using System.Collections.Generic;


namespace ProjectFilter.Services;


public class SolutionSettings {

    public SolutionSettings() {
        LoadProjectDependencies = true;
        UseRegularExpressions = false;
        ExpandLoadedProjects = true;
        Nodes = new Dictionary<string, SolutionNodeSettings>();
    }


    public bool LoadProjectDependencies { get; set; }


    public bool UseRegularExpressions { get; set; }


    public bool ExpandLoadedProjects { get; set; }


    public Dictionary<string, SolutionNodeSettings> Nodes { get; }

}
