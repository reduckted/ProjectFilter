using System.Threading.Tasks;


namespace ProjectFilter.Services {

    public interface IFilterOptionsProvider {

        Task<FilterOptions?> GetOptionsAsync();

    }

}
