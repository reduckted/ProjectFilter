using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using ProjectFilter.Services;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Commands {

    public sealed class FilterProjectsCommand : MenuCommandBase {

        public FilterProjectsCommand(IAsyncServiceProvider provider) : base(provider) { }


        protected override void Initialize(IMenuCommandService commandService) {
            commandService.AddCommand(
                new OleMenuCommand(
                    Execute,
                    new CommandID(PackageGuids.ProjectFilterPackageCommandSet, PackageIds.FilterProjectsCommand),
                    false
                )
            );
        }


        public override async Task ExecuteAsync() {
            IFilterOptionsProvider optionsProvider;
            IFilterService filterService;
            FilterOptions? options;


            optionsProvider = await Provider.GetServiceAsync<IFilterOptionsProvider, IFilterOptionsProvider>();
            filterService = await Provider.GetServiceAsync<IFilterService, IFilterService>();

            options = await optionsProvider.GetOptionsAsync();

            if (options != null) {
                await filterService.ApplyAsync(options);
            }
        }

    }

}
