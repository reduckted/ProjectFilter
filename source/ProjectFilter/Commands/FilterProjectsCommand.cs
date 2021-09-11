using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using ProjectFilter.Services;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Commands {

    [Command(PackageIds.FilterProjectsCommand)]
    public sealed class FilterProjectsCommand : BaseCommand<FilterProjectsCommand> {

        protected async override Task ExecuteAsync(OleMenuCmdEventArgs e) {
            IFilterOptionsProvider optionsProvider;
            IFilterService filterService;
            FilterOptions? options;


            optionsProvider = await VS.GetRequiredServiceAsync<IFilterOptionsProvider, IFilterOptionsProvider>();
            filterService = await VS.GetRequiredServiceAsync<IFilterService, IFilterService>();

            options = await optionsProvider.GetOptionsAsync();

            if (options != null) {
                await filterService.ApplyAsync(options);
            }
        }

    }

}
