using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Moq;
using ProjectFilter.Services;
using System;
using Xunit;


namespace ProjectFilter.Helpers {

    [Collection(VisualStudioTests.CollectionName)]
    public class ServiceTest<T> where T : class, new() {

        private readonly GlobalServiceProvider _serviceProvider;
        private TestComponentModel? _componentModel;


        protected ServiceTest(GlobalServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
            _serviceProvider.Reset();

            AddService<ILogger>(Mock.Of<ILogger>());
        }


        protected T CreateService() {
            return Activator.CreateInstance<T>();
        }


        public void AddService<TService>(object implementation) {
            _serviceProvider.AddService(typeof(TService), implementation);
        }


        public void AddMefService<TService>(TService service) where TService : class {
            if (_componentModel is null) {
                _componentModel = new TestComponentModel();
                AddService<SComponentModel>(_componentModel);
            }

            _componentModel.AddService(service);
        }

    }

}
