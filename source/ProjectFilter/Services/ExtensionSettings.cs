using Community.VisualStudio.Toolkit;


namespace ProjectFilter.Services;


public class ExtensionSettings : BaseOptionModel<ExtensionSettings>, IExtensionSettings {

    protected override string CollectionName => "ProjectFilter_ed6f0249-446a-4ddf-a8e8-b545113ba58f";


    public ExtensionSettings() {
        LoadProjectDependencies = true;
        UseRegularExpressions = false;
    }


    public bool LoadProjectDependencies { get; set; }


    public bool UseRegularExpressions { get; set; }

}
