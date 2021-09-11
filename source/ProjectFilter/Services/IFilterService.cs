using System.Threading.Tasks;


namespace ProjectFilter.Services {

    public interface IFilterService {

        Task ApplyAsync(FilterOptions options);

    }

}
