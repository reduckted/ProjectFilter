namespace ProjectFilter.Services {

    public interface IFilterService {

        void Apply(FilterOptions options);


        void ShowOnlyLoadedProjects();

    }

}
