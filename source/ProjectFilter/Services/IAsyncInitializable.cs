using System.Threading;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services {

    public interface IAsyncInitializable {

        Task InitializeAsync(IAsyncServiceProvider provider, CancellationToken cancellationToken);

    }

}
