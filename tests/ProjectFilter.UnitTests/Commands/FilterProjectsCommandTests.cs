using Moq;
using ProjectFilter.Helpers;
using ProjectFilter.Services;
using System;
using System.ComponentModel.Design;
using System.Linq;
using Xunit;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Commands {

    public class FilterProjectsCommandTests : InitializableTest<FilterProjectsCommand> {

        private readonly Mock<IFilterService> _filterService;
        private MenuCommand? _command;
        private FilterOptions? _options;


        public FilterProjectsCommandTests() {
            Mock<IMenuCommandService> commands;
            Mock<IFilterOptionsProvider> provider;


            commands = new Mock<IMenuCommandService>();

            commands
                .Setup((x) => x.AddCommand(It.IsAny<MenuCommand>()))
                .Callback((MenuCommand command) => _command = command);

            provider = new Mock<IFilterOptionsProvider>();
            provider.Setup((x) => x.GetOptions()).Returns(() => _options);

            _filterService = new Mock<IFilterService>();

            AddService<IMenuCommandService, IMenuCommandService>(commands.Object);
            AddService<IFilterOptionsProvider, IFilterOptionsProvider>(provider.Object);
            AddService<IFilterService, IFilterService>(_filterService.Object);
        }

        [Fact]
        public async Task AppliesTheProvidedOptions() {
            await CreateAsync();
            VerifyCommand();

            _options = new FilterOptions(
                Enumerable.Empty<Guid>(),
                Enumerable.Empty<Guid>(),
                false
            );

            _command!.Invoke();

            _filterService.Verify((x) => x.Apply(_options), Times.Once());
        }


        [Fact]
        public async Task DoesNotApplyTheOptionsWhenOptionsAreNotProvided() {
            await CreateAsync();
            VerifyCommand();

            _command!.Invoke();

            _filterService.Verify((x) => x.Apply(It.IsAny<FilterOptions>()), Times.Never());
        }


        private void VerifyCommand() {
            Assert.NotNull(_command);

            Assert.Equal(
                new CommandID(PackageGuids.ProjectFilterPackageCommandSet, PackageIds.FilterProjectsCommand),
                _command!.CommandID
            );
        }

    }

}
