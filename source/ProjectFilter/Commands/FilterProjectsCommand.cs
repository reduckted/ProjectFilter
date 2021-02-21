using System;
using System.ComponentModel.Design;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using ProjectFilter.Services;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Commands {

    public sealed class FilterProjectsCommand : MenuCommandBase {

#nullable disable
        private IFilterOptionsProvider _optionsProvider;
        private IFilterService _filterService;
#nullable restore


        protected override async Task InitializeAsync(IAsyncServiceProvider provider, IMenuCommandService commandService, CancellationToken cancellationToken) {
            commandService.AddCommand(
                new OleMenuCommand(
                    Execute,
                    new CommandID(PackageGuids.ProjectFilterPackageCommandSet, PackageIds.FilterProjectsCommand),
                    false
                )
            );

            _optionsProvider = await provider.GetServiceAsync<IFilterOptionsProvider, IFilterOptionsProvider>();
            _filterService = await provider.GetServiceAsync<IFilterService, IFilterService>();
        }


        private void Execute(object sender, EventArgs e) {
            FilterOptions? options;


            options = _optionsProvider.GetOptions();

            if (options != null) {
                _filterService.Apply(options);
            }
        }

    }

}
