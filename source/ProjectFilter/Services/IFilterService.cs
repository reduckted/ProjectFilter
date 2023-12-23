using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace ProjectFilter.Services;


[Guid("ac90e7af-8562-37d5-8ccc-9d59e22be32b")]
public interface IFilterService {

    Task ApplyAsync(FilterOptions options);

}
