using System.Collections.Generic;


namespace ProjectFilter.Services;


public class SolutionNodeSettings {

    public SolutionNodeSettings() {
        Children = new Dictionary<string, SolutionNodeSettings>();
    }


    public bool IsExpanded { get; set; }


    public Dictionary<string, SolutionNodeSettings> Children { get; }

}
