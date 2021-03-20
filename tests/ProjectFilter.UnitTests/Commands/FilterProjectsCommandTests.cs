using Moq;
using ProjectFilter.Helpers;
using ProjectFilter.Services;
using System;
using System.ComponentModel.Design;
using System.Linq;
using Xunit;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Commands {

    public class FilterProjectsCommandTests : ServiceTest<FilterProjectsCommand> {

        private readonly Mock<IFilterService> _filterService;
        private FilterOptions? _options;


        public FilterProjectsCommandTests() {
            Mock<IMenuCommandService> commands;
            Mock<IFilterOptionsProvider> provider;


            commands = new Mock<IMenuCommandService>();
            commands.Setup((x) => x.AddCommand(It.IsAny<MenuCommand>()));

            provider = new Mock<IFilterOptionsProvider>();
            provider.Setup((x) => x.GetOptionsAsync()).ReturnsAsync(() => _options);

            _filterService = new Mock<IFilterService>();

            AddService<IMenuCommandService, IMenuCommandService>(commands.Object);
            AddService<IFilterOptionsProvider, IFilterOptionsProvider>(provider.Object);
            AddService<IFilterService, IFilterService>(_filterService.Object);
        }

        [Fact]
        public async Task AppliesTheProvidedOptions() {
            FilterProjectsCommand command;


            command = await CreateAsync();

            _options = new FilterOptions(
                Enumerable.Empty<Guid>(),
                Enumerable.Empty<Guid>(),
                false
            );

            await command!.ExecuteAsync();

            _filterService.Verify((x) => x.ApplyAsync(_options), Times.Once());
        }


        [Fact]
        public async Task DoesNotApplyTheOptionsWhenOptionsAreNotProvided() {
            FilterProjectsCommand command;


            command = await CreateAsync();

            await command!.ExecuteAsync();

            _filterService.Verify((x) => x.ApplyAsync(It.IsAny<FilterOptions>()), Times.Never());
        }

    }

}
