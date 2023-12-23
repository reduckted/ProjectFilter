using System.Runtime.InteropServices;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services;


[Guid("73af9b6e-11f4-3040-828d-d0927c1b1e73")]
public interface IExtensionSettings {

    bool LoadProjectDependencies { get; set; }


    bool UseRegularExpressions { get; set; }


    bool ExpandLoadedProjects { get; set; }


    Task LoadAsync();


    Task SaveAsync();

}
