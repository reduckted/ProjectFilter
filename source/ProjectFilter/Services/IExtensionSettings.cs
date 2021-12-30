using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services;


public interface IExtensionSettings {

    bool LoadProjectDependencies { get; set; }


    bool UseRegularExpressions { get; set; }


    Task LoadAsync();


    Task SaveAsync();

}
