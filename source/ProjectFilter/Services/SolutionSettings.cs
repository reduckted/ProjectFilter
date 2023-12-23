namespace ProjectFilter.Services;


public class SolutionSettings {

    public SolutionSettings() {
        LoadProjectDependencies = true;
        UseRegularExpressions = false;
        ExpandLoadedProjects = true;
    }


    public bool LoadProjectDependencies { get; set; }


    public bool UseRegularExpressions { get; set; }


    public bool ExpandLoadedProjects { get; set; }

}
