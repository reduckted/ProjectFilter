using System.Threading;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services {

    public interface IAsyncInitializable {

        Task InitializeAsync(CancellationToken cancellationToken);

    }

}
